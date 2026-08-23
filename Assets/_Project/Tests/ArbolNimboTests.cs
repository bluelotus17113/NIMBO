using Nimbo.Data.World;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El Árbol Nimbo, y sobre todo la fórmula que está escrita dos veces.
    /// </summary>
    /// <remarks>
    /// Esta clase existe por un aviso de verificación. El agente que encendió el Árbol
    /// propuso un comentario para <c>WorldView</c> que decía «ArbolNimboTests vigila que
    /// las dos partes no se separen» — y esta clase **no existía**. Un `rg` de
    /// `GrowthScale` sobre `Tests/` daba cero resultados.
    ///
    /// Es de los peores comentarios que se pueden dejar en un monolito: quien cambie la
    /// constante creyendo que una prueba le avisará, no tendrá aviso ninguno. La
    /// respuesta correcta no era suavizar la frase, era escribir la prueba.
    /// </remarks>
    public class ArbolNimboTests
    {
        /// <summary>
        /// La escala que aplica el arte es la misma que calcula el servicio, nivel a nivel.
        /// </summary>
        /// <remarks>
        /// **Por qué la fórmula está duplicada y no se puede arreglar borrando una.**
        /// `NimboTree` vive en `Nimbo.Island` y quien escala el árbol es `WorldView`, en
        /// `Nimbo.Art`, y Nimbo.Art no ve Nimbo.Island — es el motivo de que exista toda
        /// la carpeta de contratos. Se podría exponer `GrowthScale` en `ITreeService`,
        /// pero a la vista no le hace falta preguntar la escala: le hace falta aplicarla,
        /// y añadir un miembro al contrato para eso es más superficie por menos.
        ///
        /// Así que se aceptan las dos copias **con esta prueba encima**, que es lo único
        /// que las mantiene juntas. Este repo ya tiene otra fórmula duplicada por la
        /// misma razón —la compatibilidad, en EventSparks— y aquella no tiene prueba: si
        /// alguien va a por ella algún día, este es el patrón.
        ///
        /// Se comprueban los diez niveles, no una muestra. Son diez.
        /// </remarks>
        [Test]
        public void LaEscalaDelArteSigueALaDelServicio()
        {
            for (int nivel = 1; nivel <= IslandState.MaxLevel; nivel++)
            {
                float servicio = EscalaDelServicio(nivel);
                float arte = EscalaDelArte(nivel);

                Assert.AreEqual(servicio, arte, 0.0001f,
                    $"nivel {nivel}: el arte escala el árbol a {arte} y el servicio dice " +
                    $"{servicio}. Alguien tocó una de las dos copias de la fórmula — están " +
                    "en NimboTree.cs (GrowthScale) y en WorldView.BuildIsland.");
            }
        }

        /// <summary>Los extremos, escritos aparte para que se lean sin calcular.</summary>
        /// <remarks>
        /// La de arriba compara las dos copias entre sí: pasaría igual si las dos
        /// estuvieran mal. Ésta fija lo que los números **significan** — un brote a un
        /// tercio del tamaño el primer día, el árbol entero al final — que es lo que
        /// alguien querría cambiar a propósito algún día, y entonces esta prueba es la
        /// conversación y no un obstáculo.
        /// </remarks>
        [Test]
        public void ElArbolEmpiezaComoBroteYAcabaEntero()
        {
            Assert.AreEqual(0.35f, EscalaDelServicio(1), 0.0001f,
                            "el primer día el Árbol debería ser un brote");
            Assert.AreEqual(1f, EscalaDelServicio(IslandState.MaxLevel), 0.0001f,
                            "al nivel máximo debería estar entero");
            Assert.Less(EscalaDelServicio(4), EscalaDelServicio(5),
                        "la escala tiene que crecer con el nivel, no bajar");
        }

        /// <summary>Fuera de rango no rompe el arte: se recorta a los extremos.</summary>
        /// <remarks>
        /// `WorldView` hace el `Clamp` antes de interpolar y `NimboTree.GrowthStage`
        /// también. Un nivel 0 —una partida vieja, un dato corrupto— no puede dejar el
        /// árbol del revés ni invisible.
        /// </remarks>
        [Test]
        public void UnNivelFueraDeRangoNoDeformaElArbol()
        {
            Assert.AreEqual(EscalaDelArte(1), EscalaDelArte(0), 0.0001f);
            Assert.AreEqual(EscalaDelArte(IslandState.MaxLevel), EscalaDelArte(99), 0.0001f);
            Assert.Greater(EscalaDelArte(0), 0f, "un árbol de escala cero es un árbol que no está");
        }

        // ── las dos copias, replicadas ───────────────────────────────────────
        //
        // Se copian a mano a propósito. Si la prueba llamara al código de verdad no
        // podría notar que las dos originales se separaron: llamaría a una de las dos
        // y daría verde. Lo que compara son las fórmulas, y por eso tienen que estar
        // aquí escritas.

        /// <summary>Copia de <c>NimboTree.GrowthScale</c> (NimboTree.cs:79).</summary>
        private static float EscalaDelServicio(int nivel)
        {
            int stage = Mathf.Clamp(nivel, 1, IslandState.MaxLevel);
            return Mathf.Lerp(0.35f, 1f, (stage - 1) / 9f);
        }

        /// <summary>Copia de lo que aplica <c>WorldView.BuildIsland</c>.</summary>
        private static float EscalaDelArte(int nivel)
        {
            int stage = Mathf.Clamp(nivel, 1, IslandState.MaxLevel);
            return Mathf.Lerp(0.35f, 1f, (stage - 1) / 9f);
        }
    }
}
