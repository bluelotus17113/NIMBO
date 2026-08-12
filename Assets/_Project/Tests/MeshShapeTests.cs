using Nimbo.Art.Chibi;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que las cajas se vean por fuera.
    /// </summary>
    /// <remarks>
    /// Una caja del revés no se nota: la silueta es la misma, porque al descartar la
    /// cara de delante se ve la de atrás por dentro, y está justo detrás. Lo que
    /// cambia es la luz — se sombrea con la normal de la cara contraria — y eso se
    /// lee como «esta pared está rara», no como «esta pared no está».
    ///
    /// Estuvo así desde el principio y aguantó porque las habitaciones se miran
    /// desde dentro, y una caja del revés mirada desde dentro se ve perfecta.
    /// </remarks>
    public class MeshShapeTests
    {
        [Test]
        public void LasCarasDeUnaCajaMiranHaciaFuera()
        {
            var mesh = MeshShapes.Box(new Vector3(2f, 3f, 4f));
            var vertices = mesh.vertices;
            var normals = mesh.normals;

            for (int i = 0; i < vertices.Length; i++)
            {
                // Cada vértice está en una esquina, así que su posición ya apunta
                // hacia fuera del centro. Si la normal mira al lado contrario, esa
                // cara está del revés.
                Assert.Greater(Vector3.Dot(normals[i], vertices[i]), 0f,
                    $"El vértice {i} en {vertices[i]} tiene la normal {normals[i]}, "
                    + "que apunta hacia dentro de la caja.");
            }
        }

        [Test]
        public void UnaCajaSeVeDesdeFueraYNoDesdeDentro()
        {
            var mesh = MeshShapes.Box(Vector3.one);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]];
                Vector3 b = vertices[triangles[t + 1]];
                Vector3 c = vertices[triangles[t + 2]];

                // Unity dibuja un triángulo cuando se le ve en el sentido de las
                // agujas del reloj, y eso es lo mismo que decir que este producto
                // vectorial apunta hacia el que mira.
                Vector3 cara = Vector3.Cross(b - a, c - a);
                Vector3 centro = (a + b + c) / 3f;

                Assert.Greater(Vector3.Dot(cara, centro), 0f,
                    $"El triángulo {t / 3} está enrollado al revés: se ve desde dentro.");
            }
        }
    }
}
