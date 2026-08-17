using System;

namespace Nimbo.Data.World
{
    /// <summary>
    /// Una línea de la crónica de la isla: qué pasó y qué día.
    /// </summary>
    /// <remarks>
    /// **Guarda el texto ya escrito, no los identificadores.** Es un registro
    /// histórico: si se guardaran los ids y se resolvieran los nombres al pintar, la
    /// línea de un habitante que se fue de la isla aparecería vacía, y la de un bebé
    /// contaría su nombre de hoy y no el del día que nació. Lo que pasó, pasó con los
    /// nombres de entonces.
    /// </remarks>
    [Serializable]
    public class ChronicleEntry
    {
        /// <summary>Día de juego en que ocurrió.</summary>
        public int Day;

        /// <summary>La frase, ya con los nombres puestos.</summary>
        public string Text = "";

        public ChronicleEntry() { }

        public ChronicleEntry(int day, string text)
        {
            Day = day;
            Text = text;
        }
    }
}
