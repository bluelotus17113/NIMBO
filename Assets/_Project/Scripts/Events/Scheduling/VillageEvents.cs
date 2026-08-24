using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;

namespace Nimbo.Events.Scheduling
{
    /// <summary>
    /// El jugador pone una fiesta en el calendario (§15.3).
    /// </summary>
    /// <remarks>
    /// Envuelve al planificador en vez de sustituirlo: el sorteo diario sigue como
    /// estaba y esto solo se salta la espera, igual que <c>RequestService.Raise</c> con
    /// las peticiones. Una isla donde solo pasan cosas porque el jugador las paga deja
    /// de estar viva.
    /// </remarks>
    public sealed class VillageEvents : IVillageEvents
    {
        private readonly EventScheduler _scheduler;
        private readonly GameClock _clock;

        /// <summary>Lo que cuesta montarla. ⚙️</summary>
        /// <remarks>
        /// Una merienda son unas dos jornadas de sueldos de la aldea y un festival unas
        /// diez. Caro a propósito: si organizar saliera barato, el jugador pondría una
        /// fiesta cada día y las fiestas dejarían de ser fiestas.
        /// </remarks>
        private const long SmallCost = 300;
        private const long FestivalCost = 1200;

        private readonly List<HostableEvent> _hostable = new List<HostableEvent>();

        public VillageEvents(EventScheduler scheduler, GameClock clock)
        {
            _scheduler = scheduler;
            _clock = clock;

            foreach (var def in EventCalendar.All)
            {
                // Los disparados no se piden: un cumpleaños llega cuando llega, y pagar
                // por uno sería comprarle el cumpleaños a alguien.
                if (def.IsTriggered) continue;

                _hostable.Add(new HostableEvent(def.Id, def.DisplayName, def.Description,
                                                def.IsFestival ? FestivalCost : SmallCost,
                                                def.IsFestival));
            }
        }

        public IReadOnlyList<HostableEvent> Hostable => _hostable;

        public string ActiveEventId => _scheduler.ActiveEvent?.Id ?? "";

        public string ActiveEventZoneId => _scheduler.ActiveEvent?.RequiredZone ?? "";

        public HostRefusal CanHost(string eventId)
        {
            var def = EventCalendar.ById(eventId);
            if (def == null || def.IsTriggered) return HostRefusal.UnknownEvent;

            if (_scheduler.ActiveEvent != null) return HostRefusal.AlreadyRunning;

            var needed = def.IsFestival ? Unlock.HostFestivals : Unlock.HostEvents;
            if (ServiceRegistry.TryGet<IPlayerProgression>(out var progression) &&
                !progression.IsUnlocked(needed))
                return HostRefusal.NotUnlocked;

            // Los requisitos del propio evento siguen mandando: pagar no hace aparecer
            // vecinos ni abre un escenario que no está construido.
            if (!MeetsRequirements(def)) return HostRefusal.NotYet;

            long cost = def.IsFestival ? FestivalCost : SmallCost;
            if (ServiceRegistry.TryGet<IEconomyService>(out var economy) &&
                economy.Wallet.Coins < cost)
                return HostRefusal.NotEnoughCoins;

            return HostRefusal.Ok;
        }

        public bool Host(string eventId)
        {
            if (CanHost(eventId) != HostRefusal.Ok) return false;

            var def = EventCalendar.ById(eventId);
            long cost = def.IsFestival ? FestivalCost : SmallCost;

            // Se cobra antes de montarla, y si el cobro falla no se monta: al revés
            // saldría una fiesta gratis cada vez que el monedero mintiera.
            if (ServiceRegistry.TryGet<IEconomyService>(out var economy) &&
                !economy.TrySpend(cost, $"organizar {def.DisplayName}"))
                return false;

            // A la hora en que ese evento puede empezar y no a la de ahora: un festival
            // de tarde puesto a las siete de la mañana se acabaría antes de que nadie
            // saliera de casa.
            int hour = System.Math.Max(_clock.Hour, def.StartHour);
            if (hour >= def.EndHour) hour = def.StartHour;

            return _scheduler.TryStartEvent(eventId, hour);
        }

        /// <summary>Lo que el evento pide de la isla: vecinos, nivel y zona.</summary>
        private static bool MeetsRequirements(EventDefinition def)
        {
            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            var island = ServiceRegistry.Get<IIslandService>();

            if (def.MinIslanders > (registry?.Count ?? 0)) return false;
            if (def.MinIslandLevel > (island?.State.Level ?? 0)) return false;

            if (!string.IsNullOrEmpty(def.RequiredZone))
                return island != null && island.IsUnlocked(def.RequiredZone);

            return true;
        }
    }
}
