using System;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;

namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Minijuego de pesca por turnos. El pez tira con una fuerza que cambia
    /// cada turno; el jugador recoge o aguanta. Si la tensión llega a 100 el
    /// pez escapa. Se gana al acercar el pez a distancia 0.
    /// </summary>
    /// <remarks>
    /// Dificultad 1→5: más distancia inicial y tirones más fuertes.
    /// </remarks>
    public sealed class FishingGame : IMinigame
    {
        // ── estado interno ────────────────────────────────────────────────
        int _distance;
        int _tension;
        int _score;
        int _successes; // veces que recogió
        int _turn;
        int _difficulty;
        int _initialDistance;
        bool _started;
        bool _finished;
        bool _escaped;
        int _lastFishPull;

        // datos pre-generados
        int[] _fishPulls; // buffer grande de tirones

        MinigameConfig _cfg;

        // ── defaults ──────────────────────────────────────────────────────
        const int DefaultBaseDistance = 20;
        const int DefaultDistancePerDiff = 10;
        const int DefaultReelDistance = 12;
        const int DefaultReelTension = 15;
        const int DefaultWaitDrop = 20;
        const int DefaultMaxTension = 100;
        const int DefaultBasePull = 5;
        const int DefaultPullRange = 8;
        const int DefaultPullPerDiff = 2;
        const int PullBufferSize = 200;

        // ── propiedades públicas para la UI ───────────────────────────────

        public int Distance => _distance;
        public int Tension => _tension;
        public int InitialDistance => _initialDistance;
        public int Score => _score;
        public int LastFishPull => _lastFishPull;
        public int Turn => _turn;
        public bool IsOver => _finished;

        public FishingGame(MinigameConfig config = null) => _cfg = config;

        // ── IMinigame ─────────────────────────────────────────────────────

        public void Start(uint seed, int difficulty)
        {
            _difficulty = Math.Clamp(difficulty, 1, 5);

            int baseDist = _cfg?.FishingBaseDistance ?? DefaultBaseDistance;
            int distPerDiff = _cfg?.FishingDistancePerDifficulty ?? DefaultDistancePerDiff;
            _initialDistance = baseDist + _difficulty * distPerDiff;
            _distance = _initialDistance;

            int maxTension = _cfg?.FishingMaxTension ?? DefaultMaxTension;
            _tension = 0;
            _score = 0;
            _successes = 0;
            _turn = 0;
            _started = true;
            _finished = false;
            _escaped = false;
            _lastFishPull = 0;

            // pre-generar tirones
            var rng = new Rng(seed);
            int basePull = _cfg?.FishingBasePull ?? DefaultBasePull;
            int pullRange = _cfg?.FishingPullRange ?? DefaultPullRange;
            int pullPerDiff = _cfg?.FishingPullPerDifficulty ?? DefaultPullPerDiff;

            _fishPulls = new int[PullBufferSize];
            for (int i = 0; i < PullBufferSize; i++)
                _fishPulls[i] = basePull + rng.Range(0, pullRange + 1) + _difficulty * pullPerDiff;
        }

        /// <summary>
        /// <paramref name="input"/>: 0 = Reel (recoger), 1 = Wait (aguantar).
        /// </summary>
        public void Step(int input)
        {
            if (!_started || _finished) return;

            int maxTension = _cfg?.FishingMaxTension ?? DefaultMaxTension;
            int reelDist = _cfg?.FishingReelDistance ?? DefaultReelDistance;
            int reelTension = _cfg?.FishingReelTension ?? DefaultReelTension;
            int waitDrop = _cfg?.FishingWaitTensionDrop ?? DefaultWaitDrop;

            // 1. El pez tira
            int pull = _turn < _fishPulls.Length ? _fishPulls[_turn] : 5;
            _lastFishPull = pull;
            _tension += pull;

            if (_tension >= maxTension)
            {
                _tension = maxTension;
                _finished = true;
                _escaped = true;
                return;
            }

            // 2. El jugador actúa
            int action = Math.Clamp(input, 0, 1);

            if (action == 0) // Reel
            {
                _distance -= reelDist;
                _tension += reelTension;
                _successes++;
            }
            else // Wait
            {
                _tension = System.Math.Max(0, _tension - waitDrop);
            }

            _turn++;

            // 3. Resolver
            if (_tension >= maxTension)
            {
                _tension = maxTension;
                _finished = true;
                _escaped = true;
                return;
            }

            if (_distance <= 0)
            {
                _distance = 0;
                _finished = true;

                // puntuación: progreso + bonus por poca tensión
                _score = _initialDistance * 10 + (maxTension - _tension) * 2;
            }
        }

        public MinigameResult Finish()
        {
            _finished = true;

            if (_escaped)
            {
                // crédito parcial por lo que avanzó
                int progress = _initialDistance - _distance;
                _score = progress * 5;
            }

            int coins = _score / (_cfg?.CoinsPerPoint ?? 10);
            int xp = _score / (_cfg?.XpPerPoint ?? 5);
            int failures = _escaped ? 1 : 0;

            return new MinigameResult(_score, _successes, failures, coins, xp, !_escaped, _difficulty);
        }
    }
}
