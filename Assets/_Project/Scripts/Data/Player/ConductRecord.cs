using System;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Data.Player
{
    /// <summary>
    /// Cómo se comporta el protagonista, medido por lo que hace.
    /// </summary>
    /// <remarks>
    /// El protagonista no tiene personalidad elegida y no debe tenerla: la pone quien
    /// juega, con lo que hace. Pero el cortejo necesita compatibilidad, y la
    /// compatibilidad necesita cuatro ejes. De ahí esto — el nombre lleva la intención:
    /// es **conducta observada**, no un carácter que se rellena en un menú.
    ///
    /// Tres decisiones sostienen que esto funcione, y cada una arregla una forma
    /// concreta de fallar:
    ///
    /// **1. Proporciones, nunca cantidades.** Ningún eje puede salir de un total. Si la
    /// Energía fuera «metros corridos al día», quien juega tres horas sería enérgico y
    /// quien juega veinte minutos sería calmado: estaríamos midiendo cuánto rato juega,
    /// no cómo es. Cada eje es la proporción entre dos conductas que **compiten por el
    /// mismo momento** —correr en vez de andar, estar con alguien en vez de estar
    /// solo—, así que la sesión larga y la corta dan el mismo perfil.
    ///
    /// **2. Encogimiento, para que el arranque no mienta.** El primer día no hay
    /// muestras, y cero **no es neutro**: un perfil de cuatro ceros resuelve al tipo 0,
    /// que es un arquetipo concreto —calmado, reservado, independiente y práctico— y no
    /// «el promedio». Con <c>raw · n/(n+K)</c> un eje con poca información tira a cero
    /// él solo, sin casos especiales.
    ///
    /// **3. Decaimiento, no historial.** Una media de los últimos siete días obliga a
    /// guardar siete días de contadores y hace que el perfil **salte** cuando un día se
    /// cae de la ventana. Con una media exponencial de vida media larga son ocho floats
    /// y la personalidad **deriva** en vez de dar saltos.
    ///
    /// Y el decaimiento es, de regalo, la defensa contra el farmeo sin una sola regla
    /// anti-farmeo: mover un eje a propósito cuesta unas dos semanas de juego, más que
    /// los diez días de espera tras un rechazo. Farmear el eje sale estrictamente peor
    /// que cortejar a alguien compatible. No hay que prohibirlo, basta con que sea el
    /// camino lento.
    /// </remarks>
    [Serializable]
    public class ConductRecord
    {
        /// <summary>
        /// Cuántas muestras hacen falta para creerse un eje del todo.
        /// </summary>
        /// <remarks>
        /// Una muestra es **un momento**: una interacción social, un objeto fabricado,
        /// o un minuto de andar por ahí. Los ejes que se miden en tiempo llegan en
        /// minutos y no en segundos justamente por esto: con segundos, cuarenta ya se
        /// juntan antes de cruzar el prado y la Energía quedaría decidida en el primer
        /// paseo mientras la Expresión seguiría pidiendo cuarenta conversaciones. Los
        /// cuatro ejes tienen que tardar parecido en creerse, o la confianza de uno no
        /// significa lo mismo que la de otro.
        /// </remarks>
        public const float ShrinkK = 40f;

        /// <summary>Días de juego en los que una muestra pierde la mitad de su peso.</summary>
        public const float HalflifeDays = 14f;

        /// <summary>Por debajo de esto, la pantalla dice «todavía no está claro».</summary>
        public const float ConfidentAt = ShrinkK * 0.5f;

        public float[] Positive = new float[4];
        public float[] Total = new float[4];

        /// <summary>Apunta un momento: hacia el lado positivo del eje, o hacia el otro.</summary>
        public void Note(PersonalityAxis axis, bool towardPositive, float weight = 1f)
        {
            if (weight <= 0f) return;

            int i = (int)axis;
            Fill();

            Total[i] += weight;
            if (towardPositive) Positive[i] += weight;
        }

        /// <summary>Pasa un día: todo pesa un poco menos.</summary>
        public void Decay()
        {
            Fill();
            float factor = Mathf.Pow(0.5f, 1f / HalflifeDays);

            for (int i = 0; i < 4; i++)
            {
                Positive[i] *= factor;
                Total[i] *= factor;
            }
        }

        /// <summary>Cuántas muestras tiene ese eje, ya decaídas.</summary>
        public float SamplesOf(PersonalityAxis axis)
        {
            Fill();
            return Total[(int)axis];
        }

        /// <summary>De 0 a 1: cuánto hay que fiarse de ese eje.</summary>
        public float ConfidenceOf(PersonalityAxis axis) =>
            Mathf.Clamp01(SamplesOf(axis) / ConfidentAt);

        /// <summary>
        /// La proporción pura, sin encoger: lo que dice la conducta y nada más.
        /// </summary>
        /// <remarks>
        /// Esto es lo que **no puede depender de cuánto rato juegue nadie**. Media hora
        /// y tres horas haciendo lo mismo dan exactamente el mismo número aquí. Lo que
        /// sí cambia con el rato es cuánto hay que fiarse, y de eso se encarga
        /// <see cref="AxisOf"/>.
        /// </remarks>
        public float RawAxisOf(PersonalityAxis axis)
        {
            Fill();
            int i = (int)axis;

            float n = Total[i];
            return n <= 0f ? 0f : Positive[i] / n * 2f - 1f;   // de proporción a [-1, +1]
        }

        /// <summary>Un eje suelto, ya encogido por lo poco o mucho que se sabe.</summary>
        public float AxisOf(PersonalityAxis axis)
        {
            float n = SamplesOf(axis);
            if (n <= 0f) return 0f;

            return RawAxisOf(axis) * (n / (n + ShrinkK));
        }

        /// <summary>
        /// Los cuatro ejes juntos, para poder medir compatibilidad.
        /// </summary>
        /// <remarks>
        /// **Este perfil no pasa nunca por <c>Compatibility.Full</c> ni por nada que lea
        /// su <c>TypeIndex</c>.** Solo por <c>Compatibility.Between</c>, que trabaja con
        /// los ejes. El protagonista tiene ejes; no tiene tipo, y pedirle uno lo
        /// convertiría en uno de los dieciséis arquetipos justo cuando la gracia es que
        /// no lo sea.
        /// </remarks>
        public PersonalityProfile AsProfile() => new PersonalityProfile
        {
            Energy = AxisOf(PersonalityAxis.Energy),
            Expression = AxisOf(PersonalityAxis.Expression),
            Attitude = AxisOf(PersonalityAxis.Attitude),
            Outlook = AxisOf(PersonalityAxis.Outlook),
        };

        /// <summary>Los arrays de una partida vieja vienen vacíos o cortos.</summary>
        private void Fill()
        {
            if (Positive == null || Positive.Length < 4) Positive = new float[4];
            if (Total == null || Total.Length < 4) Total = new float[4];
        }
    }
}
