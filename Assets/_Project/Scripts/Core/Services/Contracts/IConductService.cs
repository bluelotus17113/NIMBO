using Nimbo.Data.Islanders;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Cómo se comporta el protagonista, para poder medir con quién pega.
    /// </summary>
    /// <remarks>
    /// El protagonista no elige personalidad —la pone quien juega, con lo que hace— y
    /// aun así el cortejo necesita compatibilidad. Esto es el puente: cuatro ejes que
    /// salen de la conducta y no de un menú.
    ///
    /// Lo apuntan sitios que **ya estaban haciendo ese trabajo**: el cuerpo del jugador
    /// ya distingue si corre o anda para elegir la velocidad, y el buscador de objetivo
    /// ya recorre a los vecinos con sus posiciones en su cadencia lenta. Contar no
    /// añade ni una iteración a ninguno de los dos.
    /// </remarks>
    public interface IConductService
    {
        /// <summary>
        /// Apunta un momento en un eje.
        /// </summary>
        /// <param name="towardPositive">
        /// Verdadero hacia el lado de arriba del eje: enérgico, expresivo, sociable,
        /// soñador.
        /// </param>
        /// <param name="weight">
        /// Cuánto cuenta. Para lo que dura —correr, estar acompañado— son segundos; para
        /// lo que pasa de golpe —craftear, abrazar— es uno.
        /// </param>
        void Note(PersonalityAxis axis, bool towardPositive, float weight = 1f);

        /// <summary>Los cuatro ejes de ahora mismo, ya encogidos por falta de muestras.</summary>
        PersonalityProfile Profile { get; }

        /// <summary>De 0 a 1, cuánto hay que fiarse de ese eje.</summary>
        float ConfidenceOf(PersonalityAxis axis);
    }
}
