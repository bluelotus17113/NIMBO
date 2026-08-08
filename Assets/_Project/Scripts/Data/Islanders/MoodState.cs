using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// Lo que se le ve en la cara. Es la única capa que el jugador lee de un vistazo,
    /// así que la lista es corta a propósito: si dos emociones se dibujan igual, sobra una.
    /// </summary>
    public enum Emotion
    {
        Neutral = 0,
        Happy = 1,
        Ecstatic = 2,
        Sad = 3,
        Angry = 4,
        Sleepy = 5,
        Hungry = 6,
        Bored = 7,
        Surprised = 8,
        Love = 9,
        Proud = 10,
        Worried = 11,
    }

    /// <summary>
    /// El ánimo de un habitante. <see cref="Happiness"/> es la media móvil lenta —
    /// lo que siente en general — y <see cref="Emotion"/> es el pico de ahora mismo,
    /// que caduca. Separarlos evita que un regalo puntual le cambie la vida.
    /// </summary>
    [Serializable]
    public struct MoodState
    {
        public const float Min = 0f;
        public const float Max = 100f;

        [Range(Min, Max)] public float Happiness;
        public Emotion Emotion;

        /// <summary>Segundos de juego que le quedan a la emoción antes de volver a la de fondo.</summary>
        public float EmotionTimer;

        public static MoodState Fresh => new MoodState
        {
            Happiness = 65f, Emotion = Emotion.Neutral, EmotionTimer = 0f,
        };

        /// <summary>La emoción que le corresponde por su felicidad de fondo, sin picos.</summary>
        public Emotion BaselineEmotion =>
            Happiness >= 85f ? Emotion.Ecstatic :
            Happiness >= 60f ? Emotion.Happy :
            Happiness >= 35f ? Emotion.Neutral :
            Happiness >= 15f ? Emotion.Sad :
                               Emotion.Worried;

        public bool IsExpressing => EmotionTimer > 0f;
    }
}
