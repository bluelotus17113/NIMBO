namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Cuál de los tres.</summary>
    public enum MinigameKind
    {
        Cooking = 0,
        Fishing = 1,
        Rhythm = 2,
    }

    /// <summary>
    /// El minijuego que se está jugando ahora mismo, si es que hay alguno.
    /// </summary>
    /// <remarks>
    /// Los tres minijuegos son lógica pura por turnos: se empiezan, se les manda un
    /// número por cada cosa que hace el jugador y se cierran devolviendo puntos. Lo que
    /// no tenían era **quién los empieza, dónde se juegan y qué pasa con lo que dan**, y
    /// eso es lo que hay aquí: uno solo a la vez, y el premio se aplica al cerrar.
    ///
    /// Uno solo a la vez porque los tres se juegan en un sitio concreto —la cocina de tu
    /// casa, el embarcadero, el escenario— y no se puede estar en dos.
    ///
    /// El servicio no dibuja nada. La pantalla le pregunta el estado y le manda las
    /// pulsaciones; si algún día hay otra forma de jugarlos, esto no cambia.
    /// </remarks>
    public interface IMinigameService
    {
        /// <summary>Cuál está en marcha, o <c>null</c> si ninguno.</summary>
        MinigameKind? Running { get; }

        /// <summary>El de cocina, mientras dure, está haciendo esta receta.</summary>
        string Context { get; }

        /// <summary>Ha llegado a su final y solo falta cerrarlo.</summary>
        bool IsOver { get; }

        /// <summary>
        /// Empieza uno. Falso si ya hay otro en marcha o si falta algo para jugarlo.
        /// </summary>
        /// <param name="difficulty">De 1 a 5. Los tres la interpretan a su manera.</param>
        /// <param name="context">Para la cocina, el id de la receta. Los otros no lo usan.</param>
        bool Start(MinigameKind kind, int difficulty, string context = null);

        /// <summary>Una acción del jugador. Qué significa el número lo decide cada juego.</summary>
        void Step(int input);

        /// <summary>
        /// Lo cierra y **aplica el premio**: monedas, lo que se haya pescado o cocinado,
        /// y el ánimo que reparta.
        /// </summary>
        /// <remarks>
        /// Es aquí y no en la pantalla porque cerrar sin cobrar es la forma más fácil de
        /// que un minijuego se quede otra vez a medias: la lógica estaba escrita y
        /// probada, y lo que faltaba era exactamente esto.
        /// </remarks>
        MinigameResult Finish();

        /// <summary>Lo deja a medias sin premio. Para cuando el jugador cierra la pantalla.</summary>
        void Abandon();

        // ── lo que la pantalla necesita para pintar ──────────────────────────

        /// <summary>La línea grande: la pista de la receta, la tensión del sedal, la nota.</summary>
        string Headline { get; }

        /// <summary>Lo de debajo: en qué paso va, cuánto queda.</summary>
        string Detail { get; }

        /// <summary>Las acciones que puede pulsar ahora, en el orden en que se enseñan.</summary>
        System.Collections.Generic.IReadOnlyList<string> Actions { get; }

        /// <summary>De 0 a 1, lo que lleva hecho. Para la barra de progreso.</summary>
        float Progress { get; }

        /// <summary>Lo que se llevó del último que terminó: el pez, el plato. Vacío si nada.</summary>
        string LastPrize { get; }

        /// <summary>
        /// ¿Hay concierto ahora mismo? Es lo que decide si se puede subir a tocar.
        /// </summary>
        /// <remarks>
        /// Vive aquí porque es lo único que hace falta saber de fuera sobre el calendario
        /// de eventos, y quien pregunta —el cartel del escenario— no tiene por qué
        /// conocer el módulo de eventos entero para una respuesta de sí o no.
        /// </remarks>
        bool ConcertRunning { get; }

        /// <summary>
        /// Del ritmo: cuántos milisegundos faltan para el momento bueno de la nota.
        /// </summary>
        /// <remarks>
        /// Negativo si ya se ha pasado. Los otros dos devuelven cero: no van con el
        /// reloj, y por eso la pantalla solo mueve la aguja cuando esto tiene sentido.
        /// </remarks>
        int MillisecondsToBeat { get; }

        /// <summary>Del ritmo: hace correr el reloj interno. Los otros lo ignoran.</summary>
        void Tick(float deltaSeconds);
    }
}
