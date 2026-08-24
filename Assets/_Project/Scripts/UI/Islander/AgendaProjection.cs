using System.Collections.Generic;
using Nimbo.Data.Islanders;

namespace Nimbo.UI.Islander
{
    /// <summary>Un tramo del día de un vecino: entre qué horas y qué se espera de él.</summary>
    public readonly struct AgendaBlock
    {
        /// <summary>Hora de inicio, incluida.</summary>
        public readonly int StartHour;

        /// <summary>Hora de fin, excluida. Puede envolver: el sueño va de 23 a 7.</summary>
        public readonly int EndHour;

        public readonly string Text;

        public AgendaBlock(int startHour, int endHour, string text)
        {
            StartHour = startHour;
            EndHour = endHour;
            Text = text;
        }

        /// <summary>¿Está este tramo pasando ahora mismo? Aguanta el envolvimiento del sueño.</summary>
        public bool Includes(int hour)
        {
            if (StartHour < EndHour) return hour >= StartHour && hour < EndHour;
            return hour >= StartHour || hour < EndHour;
        }
    }

    /// <summary>
    /// El día de un vecino, proyectado desde lo que ya decide <c>IslanderBrain</c>.
    /// </summary>
    /// <remarks>
    /// **Por qué proyecta y no lee.** La simulación no guarda ninguna agenda: el cerebro
    /// decide hora a hora reaccionando a las necesidades (<c>IslanderBrain.Decide</c>,
    /// IslanderBrain.cs:58), y vive en Nimbo.Simulation, que esta interfaz no ve. Lo que
    /// sí es estable y público son sus dos reglas fijas: la franja de sueño
    /// (<c>GameClock.IsSleepingHours</c>, GameClock.cs:59 — de 23 a 7) y adónde va cada
    /// uno cuando no le urge nada (<c>WanderByPersonality</c>, IslanderBrain.cs:121-139,
    /// con su umbral 0,3 y su orden Actitud → Visión → Energía). Sobre eso se proyecta.
    ///
    /// Lo que NO proyecta son las interrupciones por necesidad —hambre, aseo, sueño
    /// fuerte— porque sus ritmos viven en <c>NeedsConfig</c> (Nimbo.Simulation), editable
    /// desde el inspector: copiarlos aquí era copiar números que un diseñador puede
    /// tocar sin avisar. Por eso la tarjeta avisa de que puede dejarlo todo, sin decir
    /// cuándo. Cuando exista <c>IAgendaService</c> (ver Informes/informe-agenda.md),
    /// esta clase se muda a Simulation y pasa a leer los ritmos de verdad.
    /// </remarks>
    public static class AgendaProjection
    {
        /// <summary>Copia del umbral de WanderByPersonality (IslanderBrain.cs:126-128).</summary>
        internal const float WanderThreshold = 0.3f;

        /// <summary>Los bordes de IsSleepingHours (GameClock.cs:59).</summary>
        public const int WakeHour = 7;
        public const int SleepHour = 23;

        /// <summary>La línea que explica que la agenda es un hábito, no una promesa.</summary>
        public const string Caveat =
            "Si le entra hambre o mucho sueño, lo deja todo y va a resolverlo.";

        /// <summary>
        /// La frase que resume su carácter antes de listar horas: es lo que hace que dos
        /// vecinos se distingan de un vistazo, antes de leer una sola fila.
        /// </summary>
        public static string Intro(in PersonalityProfile p)
        {
            if (p.Attitude > WanderThreshold)
                return "La gente es su plan: donde haya alguien, ahí estará.";
            if (p.Outlook > WanderThreshold)
                return "Los días tranquilos los pasa al aire libre.";
            if (p.Energy > WanderThreshold)
                return "No sabe estar quieto: busca movimiento.";
            return "Va a su aire: poco ruido y poca gente.";
        }

        /// <summary>Dónde se le encuentra cuando no le urge nada. Misma precedencia que el cerebro.</summary>
        public static string Haunt(in PersonalityProfile p)
        {
            if (p.Attitude > WanderThreshold)
                return "se le ve por la plaza, que es donde se junta la gente";
            if (p.Outlook > WanderThreshold)
                return "se le ve por el parque, entre árboles";
            if (p.Energy > WanderThreshold)
                return "se le ve por los sitios de ocio";
            return "ronda las tiendas o directamente se queda en casa";
        }

        /// <summary>
        /// Su día entero en tramos: primero el rato libre, luego el sueño. Dos filas,
        /// no veinticuatro: lo que importa de una agenda es el ritmo, no cada hora.
        /// </summary>
        public static List<AgendaBlock> Day(in PersonalityProfile p, bool hasHome)
        {
            string sleep = hasHome
                ? "Duerme en su casa."
                : "No tiene casa: duerme donde le pille.";

            return new List<AgendaBlock>
            {
                new AgendaBlock(WakeHour, SleepHour,
                    $"Haciendo su vida: {Haunt(p)}."),
                new AgendaBlock(SleepHour, WakeHour, sleep),
            };
        }
    }
}
