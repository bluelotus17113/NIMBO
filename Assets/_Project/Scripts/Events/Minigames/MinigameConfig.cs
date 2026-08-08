using UnityEngine;

namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Los números de los tres minijuegos que un diseñador querría tocar.
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameConfig", menuName = "Isla Nimbo/Configuración de minijuegos")]
    public sealed class MinigameConfig : ScriptableObject
    {
        [Header("Cooking")]
        [Tooltip("Pasos extra sobre la dificultad: total = CookingBaseSteps + difficulty.")]
        [Min(0)] public int CookingBaseSteps = 2;

        [Tooltip("Fallos que aguanta el jugador antes de que se queme la receta.")]
        [Min(1)] public int CookingMaxFailures = 3;

        [Tooltip("Puntos al acertar un paso.")]
        [Min(0)] public int CookingCorrectScore = 100;

        [Tooltip("Puntos que se restan al fallar un paso.")]
        [Min(0)] public int CookingWrongPenalty = 25;

        [Header("Fishing")]
        [Tooltip("Distancia inicial base.")]
        [Min(1)] public int FishingBaseDistance = 20;

        [Tooltip("Distancia extra por nivel de dificultad.")]
        [Min(0)] public int FishingDistancePerDifficulty = 10;

        [Tooltip("Cuánto se acerca el pez al recoger.")]
        [Min(1)] public int FishingReelDistance = 12;

        [Tooltip("Cuánta tensión suma recoger.")]
        [Min(0)] public int FishingReelTension = 15;

        [Tooltip("Cuánta tensión quita aguantar.")]
        [Min(1)] public int FishingWaitTensionDrop = 20;

        [Tooltip("Tensión máxima del sedal antes de romperse.")]
        [Min(1)] public int FishingMaxTension = 100;

        [Tooltip("Tirón base del pez cada turno.")]
        [Min(0)] public int FishingBasePull = 5;

        [Tooltip("Aleatoriedad del tirón (0 a este valor).")]
        [Min(0)] public int FishingPullRange = 8;

        [Tooltip("Tirón extra por nivel de dificultad.")]
        [Min(0)] public int FishingPullPerDifficulty = 2;

        [Header("Rhythm")]
        [Tooltip("Notas base.")]
        [Min(1)] public int RhythmBaseNotes = 4;

        [Tooltip("Notas extra por nivel de dificultad.")]
        [Min(0)] public int RhythmNotesPerDifficulty = 2;

        [Tooltip("Ventana de Perfect en ms (±).")]
        [Min(0)] public int RhythmPerfectWindowMs = 50;

        [Tooltip("Ventana de Good en ms (±).")]
        [Min(0)] public int RhythmGoodWindowMs = 120;

        [Tooltip("Puntos por Perfect.")]
        [Min(0)] public int RhythmPerfectScore = 100;

        [Tooltip("Puntos por Good.")]
        [Min(0)] public int RhythmGoodScore = 50;

        [Tooltip("Bonus por nota en la racha máxima.")]
        [Min(0)] public int RhythmComboBonus = 50;

        [Tooltip("Intervalo base entre notas en ms.")]
        [Min(0)] public int RhythmBaseIntervalMs = 400;

        [Tooltip("Aleatoriedad del intervalo (0 a este valor).")]
        [Min(0)] public int RhythmIntervalRangeMs = 300;

        [Tooltip("Mínimo para la primera nota en ms.")]
        [Min(0)] public int RhythmFirstNoteMinMs = 300;

        [Tooltip("Aleatoriedad de la primera nota (0 a este valor).")]
        [Min(0)] public int RhythmFirstNoteRangeMs = 300;

        [Header("Rewards")]
        [Tooltip("coins = points / CoinsPerPoint.")]
        [Min(1)] public int CoinsPerPoint = 10;

        [Tooltip("xp = points / XpPerPoint.")]
        [Min(1)] public int XpPerPoint = 5;
    }
}
