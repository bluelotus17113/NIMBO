using System;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;

namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Minijuego de ritmo por turnos. Una secuencia de notas con su momento
    /// ideal; el jugador manda el desfase en ms y se puntúa según la
    /// cercanía.
    /// </summary>
    /// <remarks>
    /// Dificultad 1→5: más notas (6→14). Las ventanas de puntuación no
    /// cambian porque la dificultad ya sube con la cantidad de notas.
    /// </remarks>
    public sealed class RhythmGame : IMinigame
    {
        // ── estado interno ────────────────────────────────────────────────
        int _currentNote;
        int _totalNotes;
        int _score;
        int _perfects;
        int _goods;
        int _misses;
        int _currentCombo;
        int _maxCombo;
        int _difficulty;
        bool _started;
        bool _finished;

        // datos pre-generados
        int[] _noteTimings; // momento ideal de cada nota en ms

        // último resultado
        HitRating _lastRating;

        MinigameConfig _cfg;

        // ── defaults ──────────────────────────────────────────────────────
        const int DefaultBaseNotes = 4;
        const int DefaultNotesPerDiff = 2;
        const int DefaultPerfectWindow = 50;
        const int DefaultGoodWindow = 120;
        const int DefaultPerfectScore = 100;
        const int DefaultGoodScore = 50;
        const int DefaultComboBonus = 50;
        const int DefaultBaseInterval = 400;
        const int DefaultIntervalRange = 300;
        const int DefaultFirstNoteMin = 300;
        const int DefaultFirstNoteRange = 300;

        // ── propiedades públicas para la UI ───────────────────────────────

        public int CurrentNoteIndex => _currentNote;
        public int TotalNotes => _totalNotes;
        public int Score => _score;
        public int Perfects => _perfects;
        public int Goods => _goods;
        public int Misses => _misses;
        public int CurrentCombo => _currentCombo;
        public int MaxCombo => _maxCombo;
        public HitRating LastRating => _lastRating;
        public int CurrentNoteTimingMs => _started && !_finished && _currentNote < _totalNotes
            ? _noteTimings[_currentNote] : 0;
        public bool IsOver => _finished;

        public RhythmGame(MinigameConfig config = null) => _cfg = config;

        // ── IMinigame ─────────────────────────────────────────────────────

        public void Start(uint seed, int difficulty)
        {
            _difficulty = Math.Clamp(difficulty, 1, 5);

            int baseNotes = _cfg?.RhythmBaseNotes ?? DefaultBaseNotes;
            int notesPerDiff = _cfg?.RhythmNotesPerDifficulty ?? DefaultNotesPerDiff;
            _totalNotes = baseNotes + _difficulty * notesPerDiff;

            _currentNote = 0;
            _score = 0;
            _perfects = 0;
            _goods = 0;
            _misses = 0;
            _currentCombo = 0;
            _maxCombo = 0;
            _started = true;
            _finished = false;
            _lastRating = HitRating.None;

            var rng = new Rng(seed);
            int baseInterval = _cfg?.RhythmBaseIntervalMs ?? DefaultBaseInterval;
            int intervalRange = _cfg?.RhythmIntervalRangeMs ?? DefaultIntervalRange;
            int firstMin = _cfg?.RhythmFirstNoteMinMs ?? DefaultFirstNoteMin;
            int firstRange = _cfg?.RhythmFirstNoteRangeMs ?? DefaultFirstNoteRange;

            _noteTimings = new int[_totalNotes];
            int t = firstMin + rng.Range(0, firstRange + 1);
            _noteTimings[0] = t;

            for (int i = 1; i < _totalNotes; i++)
            {
                t += baseInterval + rng.Range(0, intervalRange + 1);
                _noteTimings[i] = t;
            }
        }

        /// <summary>
        /// <paramref name="input"/>: desfase en milisegundos respecto al
        /// momento ideal de la nota actual. Negativo = llegó pronto,
        /// positivo = llegó tarde.
        /// </summary>
        public void Step(int input)
        {
            if (!_started || _finished) return;
            if (_currentNote >= _totalNotes) return;

            int offset = input; // ya es el desfase en ms
            int absOffset = offset < 0 ? -offset : offset;

            int perfectWindow = _cfg?.RhythmPerfectWindowMs ?? DefaultPerfectWindow;
            int goodWindow = _cfg?.RhythmGoodWindowMs ?? DefaultGoodWindow;
            int perfectScore = _cfg?.RhythmPerfectScore ?? DefaultPerfectScore;
            int goodScore = _cfg?.RhythmGoodScore ?? DefaultGoodScore;

            if (absOffset <= perfectWindow)
            {
                _lastRating = HitRating.Perfect;
                _score += perfectScore;
                _perfects++;
                _currentCombo++;
            }
            else if (absOffset <= goodWindow)
            {
                _lastRating = HitRating.Good;
                _score += goodScore;
                _goods++;
                _currentCombo++;
            }
            else
            {
                _lastRating = HitRating.Miss;
                _misses++;
                _currentCombo = 0;
            }

            if (_currentCombo > _maxCombo)
                _maxCombo = _currentCombo;

            _currentNote++;

            if (_currentNote >= _totalNotes)
            {
                // bonus por racha máxima
                int comboBonus = _cfg?.RhythmComboBonus ?? DefaultComboBonus;
                _score += _maxCombo * comboBonus;
                _finished = true;
            }
        }

        public MinigameResult Finish()
        {
            _finished = true;
            bool victory = _perfects + _goods > _misses;

            int coins = _score / (_cfg?.CoinsPerPoint ?? 10);
            int xp = _score / (_cfg?.XpPerPoint ?? 5);

            return new MinigameResult(_score, _perfects + _goods, _misses, coins, xp, victory, _difficulty);
        }
    }
}
