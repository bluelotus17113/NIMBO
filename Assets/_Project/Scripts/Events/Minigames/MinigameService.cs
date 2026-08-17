using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Events.Scheduling;
using UnityEngine;

namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Quién empieza los minijuegos, dónde acaban sus puntos y qué se lleva el jugador.
    /// </summary>
    /// <remarks>
    /// Los tres estaban escritos, probados y **sin construir por nadie**: lógica pura
    /// sin arranque, sin pantalla y sin premio. Aquí está la mitad que faltaba.
    ///
    /// Cada uno cobra en la moneda que le pega, y eso no es un detalle de balance: es lo
    /// que evita que sean tres máquinas de nimbos con temática distinta.
    ///
    /// - **La cocina** no da monedas: da el plato. Es la forma de hacer las recetas de
    ///   cocina, no un añadido a ellas, y por eso los ingredientes se gastan gane o
    ///   pierda. Perder no deja sin comer —sale engrudo— pero sí sin la receta buena.
    /// - **La pesca** da lo pescado y unas monedas. Si el pez escapa no da nada, y eso
    ///   es lo que hace que decidir entre recoger y aguantar sea decidir.
    /// - **El ritmo** da monedas pocas y, si suena mientras hay concierto, **ánimo a
    ///   todo el que esté allí**. Tocar bien en la fiesta del pueblo tiene que notarse
    ///   en el pueblo, no en tu monedero.
    /// </remarks>
    public sealed class MinigameService : IMinigameService
    {
        private readonly MinigameConfig _config;
        private readonly EventScheduler _scheduler;

        private readonly CookingGame _cooking;
        private readonly FishingGame _fishing;
        private readonly RhythmGame _rhythm;

        private readonly List<string> _actions = new List<string>();

        private IMinigame _running;
        private MinigameKind _kind;
        private Rng _rng = Rng.FromTime();

        /// <summary>Milisegundos desde que empezó la canción. Solo lo usa el ritmo.</summary>
        private float _elapsedMs;

        /// <summary>Lo último que se pescó o se cocinó, para que la pantalla lo cuente.</summary>
        public string LastPrize { get; private set; } = "";

        /// <summary>Lo que se lleva un buen concierto por encima de lo que ya reparte.</summary>
        private const float ConcertBonusHappiness = 8f;

        /// <summary>Una de cada tantas veces, del sedal sale una bota.</summary>
        private const float BootChance = 0.12f;

        private const string BootId = "food_bota_vieja";
        private const string MashId = "food_engrudo";

        private static readonly string[] Fish =
        {
            "food_pez_nube",          // lo corriente
            "food_pez_farol",         // se paga mejor
            "food_anguila_de_viento", // el bueno
        };

        private static readonly string[] CookingActions = { "Cortar", "Remover", "Sazonar" };
        private static readonly string[] FishingActions = { "Recoger", "Aguantar" };
        private static readonly string[] RhythmActions = { "¡Ahora!" };

        /// <param name="scheduler">
        /// Para saber si hay concierto puesto. Puede faltar: entonces tocar es ensayar.
        /// </param>
        public MinigameService(MinigameConfig config = null, EventScheduler scheduler = null)
        {
            _config = config;
            _scheduler = scheduler;

            _cooking = new CookingGame(config);
            _fishing = new FishingGame(config);
            _rhythm = new RhythmGame(config);
        }

        // ── el ciclo ─────────────────────────────────────────────────────────

        public MinigameKind? Running => _running == null ? null : _kind;
        public string Context { get; private set; }
        public bool IsOver => _running != null && _running.IsOver;

        public bool Start(MinigameKind kind, int difficulty, string context = null)
        {
            if (_running != null) return false;

            difficulty = Mathf.Clamp(difficulty, 1, 5);
            Context = context;
            LastPrize = "";
            _elapsedMs = 0f;
            _kind = kind;

            switch (kind)
            {
                case MinigameKind.Cooking:
                    // Sin receta no hay nada que cocinar: la cocina no es un juego
                    // suelto, es cómo se hacen las recetas de cocina.
                    if (!TryGetRecipe(context, out var recipe)) return false;
                    _cooking.RecipeName = recipe.DisplayName;
                    _running = _cooking;
                    break;

                case MinigameKind.Fishing:
                    _running = _fishing;
                    break;

                default:
                    _running = _rhythm;
                    break;
            }

            _running.Start((uint)_rng.Range(1, int.MaxValue), difficulty);
            return true;
        }

        public void Step(int input)
        {
            if (_running == null || _running.IsOver) return;

            // El ritmo no recibe el botón: recibe **cuánto te has desviado** del momento
            // bueno, que es lo único que puntúa.
            if (_kind == MinigameKind.Rhythm)
            {
                _rhythm.Step(Mathf.RoundToInt(_elapsedMs) - _rhythm.CurrentNoteTimingMs);
                return;
            }

            _running.Step(input);
        }

        public void Tick(float deltaSeconds)
        {
            if (_running == null || _running.IsOver) return;
            if (_kind != MinigameKind.Rhythm) return;

            _elapsedMs += deltaSeconds * 1000f;

            // La nota que ya no se puede acertar se cuenta como fallada sola. Sin esto,
            // quedarse quieto congela la canción y el juego de ritmo deja de ir con el
            // ritmo: bastaría con esperar a tener ganas.
            int good = _config?.RhythmGoodWindowMs ?? 120;
            if (_elapsedMs > _rhythm.CurrentNoteTimingMs + good)
                _rhythm.Step(int.MaxValue / 2);
        }

        public void Abandon()
        {
            // Sin premio y sin castigo. La cocina no ha gastado nada todavía —los
            // ingredientes se cobran al cerrar—, así que dejarlo a medias sale gratis.
            _running = null;
            Context = null;
            LastPrize = "";
        }

        public MinigameResult Finish()
        {
            if (_running == null) return default;

            var result = _running.Finish();
            var kind = _kind;
            string context = Context;

            _running = null;
            Context = null;

            switch (kind)
            {
                case MinigameKind.Cooking: PayCooking(result, context); break;
                case MinigameKind.Fishing: PayFishing(result); break;
                default: PayRhythm(result); break;
            }

            return result;
        }

        // ── los premios ──────────────────────────────────────────────────────

        /// <summary>
        /// El plato, o el engrudo. Los ingredientes se gastan igual.
        /// </summary>
        /// <remarks>
        /// Lo cobra el crafteo y no esto, aunque sea una línea más larga: los
        /// ingredientes de una receta se descuentan en un solo sitio, o el día que el
        /// crafteo cambie de reglas la cocina se quedará con las de antes.
        /// </remarks>
        private void PayCooking(in MinigameResult result, string recipeId)
        {
            if (!ServiceRegistry.TryGet<ICraftingService>(out var crafting)) return;
            if (!crafting.TryGetRecipe(recipeId, out var recipe)) return;

            string output = result.IsVictory ? null : MashId;
            if (crafting.Craft(recipeId, CraftStation.Kitchen, output) != CraftError.Ok) return;

            LastPrize = result.IsVictory ? recipe.OutputId : MashId;
        }

        /// <summary>Lo pescado y unas monedas. Si escapó, nada.</summary>
        private void PayFishing(in MinigameResult result)
        {
            if (!result.IsVictory) return;

            string caught = _rng.Chance(BootChance) ? BootId : FishFor(result.Points);

            if (ServiceRegistry.TryGet<IInventoryService>(out var bag) &&
                bag.TryStore(caught, 1, out int leftover) != StoreResult.UnknownItem &&
                leftover == 0)
            {
                LastPrize = caught;
            }

            if (result.Coins > 0 && ServiceRegistry.TryGet<IEconomyService>(out var economy))
                economy.AddCoins(result.Coins, "pesca en el embarcadero");
        }

        /// <summary>Cuanto más limpia la pelea, mejor lo que sale del sedal.</summary>
        private static string FishFor(int points) =>
            points >= 700 ? Fish[2] :
            points >= 400 ? Fish[1] :
                            Fish[0];

        /// <summary>
        /// Monedas pocas y, si hay concierto, ánimo para todos los que están.
        /// </summary>
        /// <remarks>
        /// El concierto ya reparte ánimo por su cuenta cuando termina, toque el jugador
        /// o no. Esto es un extra, no un sustituto: la fiesta del pueblo no depende de
        /// que te apetezca jugar, que es la regla del proyecto —**no castiga por no
        /// entrar**—; lo que hace tocar bien es que se note más.
        /// </remarks>
        private void PayRhythm(in MinigameResult result)
        {
            if (result.Coins > 0 && ServiceRegistry.TryGet<IEconomyService>(out var economy))
                economy.AddCoins(result.Coins, "un buen concierto");

            if (!result.IsVictory || !ConcertRunning) return;
            if (!ServiceRegistry.TryGet<ISimulationService>(out var simulation)) return;
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;

            var all = registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                simulation.ApplyHappiness(all[i].Id, ConcertBonusHappiness);
                simulation.ShowEmotion(all[i].Id, Emotion.Happy, 4f);
            }
        }

        /// <summary>¿Hay concierto puesto ahora mismo?</summary>
        public bool ConcertRunning
        {
            get
            {
                var active = _scheduler?.ActiveEvent;
                return active != null && (active.Id == "concert" || active.Id == "talent_show");
            }
        }

        // ── lo que la pantalla necesita ──────────────────────────────────────

        public string Headline => _kind switch
        {
            MinigameKind.Cooking when _running != null => _cooking.CurrentHint,
            MinigameKind.Fishing when _running != null =>
                $"Lo tienes a {_fishing.Distance} · el sedal va al {_fishing.Tension}%",
            MinigameKind.Rhythm when _running != null => RhythmHeadline(),
            _ => "",
        };

        private string RhythmHeadline() => _rhythm.LastRating switch
        {
            HitRating.Perfect => "¡Clavada!",
            HitRating.Good => "Casi",
            HitRating.Miss => "Se te ha ido",
            _ => "Atento a la siguiente",
        };

        public string Detail => _kind switch
        {
            MinigameKind.Cooking when _running != null =>
                $"{_cooking.RecipeName} · paso {_cooking.CurrentStepNumber + 1} de " +
                $"{_cooking.TotalSteps} · te quedan {_cooking.RemainingFailures} fallos",

            MinigameKind.Fishing when _running != null =>
                _fishing.LastFishPull > 0
                    ? $"El último tirón te ha costado {_fishing.LastFishPull} de sedal"
                    : "Aguanta o recoge, según cómo tire",

            MinigameKind.Rhythm when _running != null =>
                $"Nota {_rhythm.CurrentNoteIndex + 1} de {_rhythm.TotalNotes} · " +
                $"racha de {_rhythm.CurrentCombo}",

            _ => "",
        };

        public IReadOnlyList<string> Actions
        {
            get
            {
                _actions.Clear();
                if (_running == null) return _actions;

                var source = _kind switch
                {
                    MinigameKind.Cooking => CookingActions,
                    MinigameKind.Fishing => FishingActions,
                    _ => RhythmActions,
                };
                _actions.AddRange(source);
                return _actions;
            }
        }

        public float Progress => _kind switch
        {
            MinigameKind.Cooking when _running != null && _cooking.TotalSteps > 0 =>
                _cooking.CurrentStepNumber / (float)_cooking.TotalSteps,

            MinigameKind.Fishing when _running != null && _fishing.InitialDistance > 0 =>
                1f - _fishing.Distance / (float)_fishing.InitialDistance,

            MinigameKind.Rhythm when _running != null && _rhythm.TotalNotes > 0 =>
                _rhythm.CurrentNoteIndex / (float)_rhythm.TotalNotes,

            _ => 0f,
        };

        public int MillisecondsToBeat =>
            _running != null && _kind == MinigameKind.Rhythm
                ? _rhythm.CurrentNoteTimingMs - Mathf.RoundToInt(_elapsedMs)
                : 0;

        private static bool TryGetRecipe(string recipeId, out Recipe recipe)
        {
            recipe = default;
            return !string.IsNullOrEmpty(recipeId)
                && ServiceRegistry.TryGet<ICraftingService>(out var crafting)
                && crafting.TryGetRecipe(recipeId, out recipe);
        }
    }
}
