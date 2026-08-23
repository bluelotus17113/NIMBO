using Nimbo.Art.World;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// La costura del borde: por cada sector del contorno, el labio del prado
    /// tiene que existir en la malla de la roca. Sin huecos.
    /// </summary>
    /// <remarks>
    /// Esta es la prueba que habría cazado la rendija de 2,8 m: la roca repetía
    /// por su cuenta el contorno del prado y los dos bordes divergían —el labio
    /// cae entre −4,68 y −1,72, el anillo 0 flotaba entre −1,86 y +1,86— dejando
    /// banda abierta a la altura de los ojos, justo donde pescan los vecinos.
    ///
    /// Se comprueba con las llamadas exactas de producción, incluido el descuido
    /// que lo destapó: la isla del jugador siembra el prado con semilla 31 y
    /// llama a la roca sin semilla (WorldView.BuildHomeIsland). La costura no
    /// puede depender de que el llamador acierte con los parámetros; si alguien
    /// vuelve a separar los dos bordes, esto se pone rojo con el sector y la
    /// distancia en la mano.
    /// </remarks>
    public class FaldaDelBordeTests
    {
        private const int Segmentos = 48;   // los defaults de IslandMeshBuilder
        private const float Tolerancia = 0.01f;

        [Test]
        public void LaRocaNacePegadaAlLabioDelPrado_EnLaIslaDeLaAldea()
        {
            // WorldView.BuildIsland: radio 100, profundidad 62, sin más parámetros.
            var prado = IslandMeshBuilder.BuildSurface(100f);
            var roca = IslandMeshBuilder.BuildUnderside(100f, 62f);

            Cose(prado, roca);
        }

        [Test]
        public void LaRocaNacePegadaAlLabioDelPrado_EnLaIslaDelJugador()
        {
            // WorldView.BuildHomeIsland: HomeRadius 45, profundidad ×0,6, prado con
            // semilla 31 y roca SIN semilla. Tal cual está en producción: si un día
            // esa llamada pasa la semilla, esta prueba sigue valiendo igual.
            var prado = IslandMeshBuilder.BuildSurface(45f, seed: 31u);
            var roca = IslandMeshBuilder.BuildUnderside(45f, 62f * 0.6f);

            Cose(prado, roca);
        }

        /// <summary>Por cada sector, el vértice exterior del prado existe en la roca.</summary>
        private static void Cose(Mesh prado, Mesh roca)
        {
            var labio = prado.vertices;  // centro + anillos; el labio son los últimos Segmentos
            var falda = roca.vertices;

            Assert.Greater(labio.Length, Segmentos, "el prado no tiene ni un anillo completo");

            for (int seg = 0; seg < Segmentos; seg++)
            {
                var vertice = labio[labio.Length - Segmentos + seg];

                float masCerca = float.MaxValue;
                foreach (var candidato in falda)
                    masCerca = Mathf.Min(masCerca, Vector3.Distance(vertice, candidato));

                Assert.Less(masCerca, Tolerancia,
                            $"sector {seg}: el labio del prado ({vertice}) no está en la " +
                            $"roca; lo más cercano queda a {masCerca:0.000} m: hueco abierto");
            }

            Object.DestroyImmediate(prado);
            Object.DestroyImmediate(roca);
        }
    }
}
