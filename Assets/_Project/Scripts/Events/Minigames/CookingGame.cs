using System;
using Nimbo.Core.Util;

namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Minijuego de cocina por turnos. El jugador sigue los pasos de una receta
    /// eligiendo entre tres acciones. Tres fallos y se quema.
    /// </summary>
    /// <remarks>
    /// Dificultad 1→5: 3→7 pasos. El resto de parámetros no cambian porque los
    /// pasos ya son el multiplicador de dificultad natural.
    /// </remarks>
    public sealed class CookingGame : IMinigame
    {
        // ── estado interno ────────────────────────────────────────────────
        int _currentStep;
        int _totalSteps;
        int _remainingFailures;
        int _score;
        int _successes;
        int _failures;
        int _difficulty;
        bool _started;
        bool _finished;

        // datos pre-generados en Start
        int[] _correctActions; // por paso, 0=Chop 1=Stir 2=Season
        string[] _hints;

        // último resultado (para que la UI pueda mostrarlo)
        int _lastStepAction;
        bool _lastStepCorrect;

        MinigameConfig _cfg;

        // ── pools de pistas ───────────────────────────────────────────────
        static readonly string[] ChopHints = { "It needs cutting", "The pieces are too big", "Grab the knife", "Something must be chopped" };
        static readonly string[] StirHints = { "It needs mixing", "Keep the pot moving", "Grab the spoon", "Don't let it stick" };
        static readonly string[] SeasonHints = { "It tastes a bit bland", "Needs more flavor", "Grab the spices", "Something is missing" };

        static readonly string[] RecipeNames = { "Vegetable Stew", "Mushroom Risotto", "Spicy Curry", "Fish Soup", "Herb Omelette" };

        // ── defaults ──────────────────────────────────────────────────────

        const int DefaultBaseSteps = 2;
        const int DefaultMaxFailures = 3;
        const int DefaultCorrectScore = 100;
        const int DefaultWrongPenalty = 25;

        // ── propiedades públicas para la UI ───────────────────────────────

        public int CurrentStepNumber => _currentStep;
        public int TotalSteps => _totalSteps;
        public int RemainingFailures => _remainingFailures;
        public string CurrentHint => _started && !_finished && _currentStep < _totalSteps
            ? _hints[_currentStep] : "";
        public string RecipeName { get; private set; }
        public int Score => _score;
        public bool LastStepCorrect => _lastStepCorrect;
        public int LastStepAction => _lastStepAction;
        public bool IsOver => _finished;

        public CookingGame(MinigameConfig config = null) => _cfg = config;

        // ── IMinigame ─────────────────────────────────────────────────────

        public void Start(uint seed, int difficulty)
        {
            _difficulty = Math.Clamp(difficulty, 1, 5);
            _totalSteps = (_cfg?.CookingBaseSteps ?? DefaultBaseSteps) + _difficulty;
            int maxFailures = _cfg?.CookingMaxFailures ?? DefaultMaxFailures;

            _currentStep = 0;
            _remainingFailures = maxFailures;
            _score = 0;
            _successes = 0;
            _failures = 0;
            _started = true;
            _finished = false;

            var rng = new Rng(seed);

            // nombre de receta
            RecipeName = RecipeNames[rng.Range(0, RecipeNames.Length)];

            // acciones correctas por paso
            _correctActions = new int[_totalSteps];
            for (int i = 0; i < _totalSteps; i++)
                _correctActions[i] = rng.Range(0, 3);

            // pistas
            _hints = new string[_totalSteps];
            for (int i = 0; i < _totalSteps; i++)
            {
                _hints[i] = PickHint(_correctActions[i], ref rng);
            }
        }

        /// <summary>
        /// <paramref name="input"/>: 0 = Chop, 1 = Stir, 2 = Season.
        /// </summary>
        public void Step(int input)
        {
            if (!_started || _finished) return;
            if (_currentStep >= _totalSteps) return;

            int action = Math.Clamp(input, 0, 2);
            int correct = _correctActions[_currentStep];
            bool hit = action == correct;

            int correctScore = _cfg?.CookingCorrectScore ?? DefaultCorrectScore;
            int wrongPenalty = _cfg?.CookingWrongPenalty ?? DefaultWrongPenalty;

            _lastStepAction = correct;
            _lastStepCorrect = hit;

            if (hit)
            {
                _score += correctScore;
                _successes++;
            }
            else
            {
                _score = System.Math.Max(0, _score - wrongPenalty);
                _failures++;
                _remainingFailures--;
            }

            _currentStep++;

            if (_remainingFailures <= 0 || _currentStep >= _totalSteps)
                _finished = true;
        }

        public MinigameResult Finish()
        {
            _finished = true;
            bool victory = _remainingFailures > 0 && _currentStep >= _totalSteps;

            int coins = _score / (_cfg?.CoinsPerPoint ?? 10);
            int xp = _score / (_cfg?.XpPerPoint ?? 5);

            return new MinigameResult(_score, _successes, _failures, coins, xp, victory, _difficulty);
        }

        // ── helpers ───────────────────────────────────────────────────────

        static string PickHint(int action, ref Rng rng)
        {
            return action switch
            {
                0 => ChopHints[rng.Range(0, ChopHints.Length)],
                1 => StirHints[rng.Range(0, StirHints.Length)],
                _ => SeasonHints[rng.Range(0, SeasonHints.Length)],
            };
        }
    }
}
