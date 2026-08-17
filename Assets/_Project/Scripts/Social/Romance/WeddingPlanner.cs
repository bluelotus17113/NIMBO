using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Save;
using Nimbo.Data.Social;

namespace Nimbo.Social.Romance
{
    /// <summary>
    /// Lleva a las parejas del compromiso a la boda y de la boda a la familia, solas.
    /// </summary>
    /// <remarks>
    /// **Esto es lo que le faltaba a la rama romántica para tener final.**
    /// <see cref="RomanceEvaluator"/> llevaba a dos habitantes del flechazo al
    /// compromiso sin ayuda de nadie, y ahí se paraba: <c>TryMarry</c> y
    /// <c>TryHaveBaby</c> estaban escritos y **no los llamaba nadie**. Una pareja se
    /// prometía y se quedaba prometida para siempre. Los tests pasaban, porque probaban
    /// que casarse funciona — no que llegue a ocurrir.
    ///
    /// Por qué revisa la lista cada día en vez de escuchar
    /// <see cref="RomanceStageChanged"/>: porque hay partidas guardadas con parejas
    /// prometidas desde hace semanas. Un planificador que solo reaccione al aviso no
    /// las vería nunca —el aviso ya pasó— y esas parejas seguirían congeladas. Mirando
    /// la lista, la primera vez que corre las recoge a todas.
    ///
    /// La boda **no la decide el jugador y no la puede impedir**. Se avisa con
    /// <see cref="NoticeDays"/> días para que pueda ir; si no va, se casan igual y se
    /// lo cuentan. Ver `Docs/01_GDD.md` §13.1.
    /// </remarks>
    public sealed class WeddingPlanner : IDisposable
    {
        /// <summary>Días prometidos antes de que se ponga fecha. ⚙️</summary>
        private const int DaysEngagedBeforeBooking = 5;

        /// <summary>Días entre el anuncio y la boda. ⚙️</summary>
        private const int NoticeDays = 3;

        /// <summary>Días casados antes de que pueda venir un bebé. ⚙️</summary>
        private const int DaysMarriedBeforeBaby = 10;

        /// <summary>
        /// Cuántos habitantes caben en la isla antes de que dejen de nacer bebés. ⚙️
        /// </summary>
        /// <remarks>
        /// El tope está aquí y no en el censo porque es una regla de esta mecánica: un
        /// bebé que nace es un vecino que hay que alojar y al que hay que dar trabajo, y
        /// una isla que se llena sola deja al jugador sin sitio para los habitantes que
        /// él quiera crear. Los doce son los del GDD §4.1.
        /// </remarks>
        private const int IslandPopulationCap = 12;

        private readonly SaveGame _save;
        private readonly IIslanderRegistry _registry;
        private readonly ISocialService _social;
        private readonly ISimulationService _simulation;
        private readonly Action<DayPassed> _onDay;

        public WeddingPlanner(SaveGame save, IIslanderRegistry registry,
                              ISocialService social, ISimulationService simulation)
        {
            _save = save;
            _registry = registry;
            _social = social;
            _simulation = simulation;

            _onDay = OnDayPassed;
            EventBus.Subscribe(_onDay);
        }

        public void Dispose() => EventBus.Unsubscribe(_onDay);

        /// <summary>Las parejas que lleva ahora mismo. Para la crónica y los tests.</summary>
        public IReadOnlyList<WeddingBooking> Bookings => _save.Weddings;

        /// <summary>
        /// Un día en la vida amorosa de la aldea.
        /// </summary>
        /// <remarks>
        /// Público para que los tests no tengan que fabricar el aviso, y porque al
        /// volver de estar fuera el reloj publica un <see cref="DayPassed"/> por cada
        /// día saltado: tiene que poder llamarse muchas veces seguidas y hacer lo mismo.
        ///
        /// El orden de los cuatro pasos importa. Limpiar va primero para que una pareja
        /// que rompió anoche no se case esta mañana. Celebrar va antes de apuntar para
        /// que nadie estrene compromiso y boda el mismo día.
        /// </remarks>
        public void AdvanceDay(int day)
        {
            DropStaleBookings();
            CelebrateDueWeddings(day);
            BookRipeEngagements(day);
            DeliverBabies(day);
        }

        private void OnDayPassed(DayPassed evt) => AdvanceDay(evt.Day);

        // ── 1. limpiar ───────────────────────────────────────────────────────

        /// <summary>
        /// Quita las entradas de parejas que ya no van a ningún sitio: rompieron, se
        /// separaron o uno de los dos se fue de la isla.
        /// </summary>
        private void DropStaleBookings()
        {
            for (int i = _save.Weddings.Count - 1; i >= 0; i--)
            {
                var booking = _save.Weddings[i];

                if (!_registry.TryGet(booking.AId, out _) || !_registry.TryGet(booking.BId, out _))
                {
                    _save.Weddings.RemoveAt(i);
                    continue;
                }

                var stage = StageBetween(booking.AId, booking.BId);

                // Casada y apuntada: se queda, que le toca bebé. Prometida: sigue en
                // camino. Cualquier otra cosa —rompieron, se separaron, volvieron a
                // salir sin más— y la entrada sobra.
                bool stillOnTrack = booking.IsMarried
                    ? stage == RomanceStage.Married
                    : stage == RomanceStage.Engaged || stage == RomanceStage.Married;

                if (!stillOnTrack) _save.Weddings.RemoveAt(i);
            }
        }

        // ── 2. celebrar ──────────────────────────────────────────────────────

        private void CelebrateDueWeddings(int day)
        {
            for (int i = 0; i < _save.Weddings.Count; i++)
            {
                var booking = _save.Weddings[i];
                if (booking.IsMarried || !booking.HasDate) continue;
                if (day < booking.WeddingDay) continue;

                // Si a estas alturas no se dejan casar, la fecha se cae y vuelven a la
                // cola: DropStaleBookings ya se llevó a los que rompieron, así que esto
                // solo salta si la afinidad se les enfrió justo por debajo del umbral.
                if (!_social.TryMarry(booking.AId, booking.BId))
                {
                    booking.WeddingDay = 0;
                    booking.EngagedSinceDay = day;
                    continue;
                }

                booking.MarriedOnDay = day;
                EventBus.Publish(new WeddingHeld(booking.AId, booking.BId));
            }
        }

        // ── 3. poner fecha ───────────────────────────────────────────────────

        /// <summary>
        /// Busca parejas prometidas por su cuenta y las apunta. A las que ya llevan
        /// <see cref="DaysEngagedBeforeBooking"/> días, les pone fecha y lo anuncia.
        /// </summary>
        private void BookRipeEngagements(int day)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                string partner = _social.PartnerOf(islander.Id);
                if (string.IsNullOrEmpty(partner)) continue;

                // Los dos tienen que estar prometidos: uno solo es un compromiso a
                // medias, y casar a alguien que aún no ha dicho sí es justo lo que
                // este juego no hace.
                if (StageBetween(islander.Id, partner) != RomanceStage.Engaged) continue;
                if (StageBetween(partner, islander.Id) != RomanceStage.Engaged) continue;

                var booking = Find(islander.Id, partner);
                if (booking == null)
                {
                    // Recién vistos. La cuenta empieza hoy incluso si llevaban
                    // prometidos desde antes de que existiera este planificador: así al
                    // cargar una partida vieja no se casan doce parejas de golpe.
                    _save.Weddings.Add(WeddingBooking.Between(islander.Id, partner, day));
                    continue;
                }

                if (booking.HasDate || booking.IsMarried) continue;
                if (day - booking.EngagedSinceDay < DaysEngagedBeforeBooking) continue;

                booking.WeddingDay = day + NoticeDays;
                EventBus.Publish(new WeddingAnnounced(booking.AId, booking.BId, booking.WeddingDay));
            }
        }

        // ── 4. bebés ─────────────────────────────────────────────────────────

        /// <summary>
        /// Un hijo a las parejas casadas que llevan tiempo y tienen casa donde meterlo.
        /// </summary>
        /// <remarks>
        /// Que haga falta la casa ampliada es el enganche con la gestión: el jugador no
        /// decide que nazca nadie, pero si nunca amplía casas, la aldea no crece. Ver
        /// GDD §13.1 y §16.
        ///
        /// El servicio de ampliaciones se pide al registro y no por el constructor
        /// porque es opcional: en los tests de romance no hay casas, y ahí el requisito
        /// se salta en vez de bloquear todos los nacimientos.
        /// </remarks>
        private void DeliverBabies(int day)
        {
            if (_registry.Count >= IslandPopulationCap) return;

            ServiceRegistry.TryGet<IHomeUpgradeService>(out var upgrades);

            for (int i = 0; i < _save.Weddings.Count; i++)
            {
                var booking = _save.Weddings[i];
                if (!booking.IsMarried) continue;
                if (day - booking.MarriedOnDay < DaysMarriedBeforeBaby) continue;

                if (upgrades != null &&
                    upgrades.LevelOf(booking.AId) < 1 && upgrades.LevelOf(booking.BId) < 1)
                    continue;

                string child = _social.TryHaveBaby(booking.AId, booking.BId);
                if (string.IsNullOrEmpty(child)) continue;

                // La entrada se retira: un hijo por pareja, y el que quiera más pasa por
                // otra ampliación. Sin esto, una pareja casada pariría cada día que
                // cumpliese los requisitos y la isla se llenaría en dos semanas.
                _save.Weddings.RemoveAt(i);

                _simulation.ApplyHappiness(booking.AId, 20f);
                _simulation.ApplyHappiness(booking.BId, 20f);
                return;   // uno por día es bastante
            }
        }

        // ── ayudantes ────────────────────────────────────────────────────────

        private RomanceStage StageBetween(string fromId, string toId) =>
            _social.GetRelationship(fromId, toId).Romance;

        private WeddingBooking Find(string one, string other)
        {
            for (int i = 0; i < _save.Weddings.Count; i++)
                if (_save.Weddings[i].Is(one, other)) return _save.Weddings[i];
            return null;
        }
    }
}
