namespace Nimbo.Core.Audio
{
    /// <summary>
    /// El humor de la música: qué suena de fondo ahora mismo.
    /// </summary>
    /// <remarks>
    /// Tres y no más. Cada uno tiene que ser reconocible **sin mirar la pantalla**, y
    /// con seis matices ninguno lo es: media hora jugando y todos suenan al mismo
    /// ambiente que se movía un poco. Con tres, oyes que se ha hecho de noche.
    /// </remarks>
    public enum MusicMood
    {
        /// <summary>De día y sin nada montado. El fondo de siempre.</summary>
        Calm = 0,

        /// <summary>Se ha hecho de noche: más lento, más grave y más callado.</summary>
        Night = 1,

        /// <summary>Hay algo en marcha en la aldea. Más rápido y más brillante.</summary>
        Party = 2,
    }

    /// <summary>Qué debería sonar, dado lo que está pasando.</summary>
    /// <remarks>
    /// Función pura a propósito: la regla se puede probar sin abrir un `AudioSource`,
    /// que en batchmode no suena de todas formas. Lo que hay que proteger es la
    /// decisión —una fiesta a las once de la noche suena a fiesta, no a noche—, no
    /// que Unity sepa reproducir un clip.
    /// </remarks>
    public static class MusicMoods
    {
        /// <summary>Antes de esta hora todavía es de noche.</summary>
        public const int DawnHour = 6;

        /// <summary>A partir de esta hora ya es de noche.</summary>
        public const int DuskHour = 21;

        public static MusicMood For(int hour, bool eventRunning)
        {
            // La fiesta gana a la hora. Un concierto acaba a las diez y hasta que
            // termina sigue siendo una fiesta, aunque fuera sea de noche: si el fondo
            // se apagara a mitad del concierto, parecería que se ha roto algo.
            if (eventRunning) return MusicMood.Party;

            return hour < DawnHour || hour >= DuskHour ? MusicMood.Night : MusicMood.Calm;
        }
    }
}
