using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Data.Islanders;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// La geometría que solo se ve de cerca: un casquete sin fondo y una cara que
    /// flota sobre la mejilla no se delatan desde la cámara cenital vieja, pero sí
    /// desde detrás y desde abajo, que es desde donde la cámara mira ahora.
    /// </summary>
    /// <remarks>
    /// Una prueba no puede decir si el pelo es bonito; sí puede decir que el cacillo
    /// está cerrado —que no existe ni un borde con una sola cara— y que el parche de
    /// la cara abraza el cráneo en vez de flotar junto a él.
    /// </remarks>
    public class ChibiCercaTests
    {
        /// <summary>
        /// Cuenta cuántos triángulos comparten cada arista, identificando los
        /// vértices por POSICIÓN y descartando los triángulos sin superficie.
        /// En una malla estanca todas las aristas que quedan valen 2; una con 1 es
        /// un borde abierto, y eso era exactamente el cacillo.
        /// </summary>
        /// <remarks>
        /// Lo de los triángulos sin superficie no es un detalle: en el polo de la
        /// rejilla esférica el anillo entero colapsa en un punto y cada celda de la
        /// primera fila genera una astilla de dos vértices coincidentes. Esas
        /// astillas no acotan nada —no son cara— pero su arista viva la compartirían
        /// cuatro triángulos si contaran, y la prueba daría roja sobre una malla
        /// sana. Fue exactamente el primer fallo de esta prueba.
        /// </remarks>
        private static Dictionary<(Vector3Int, Vector3Int), int> Aristas(Mesh mesh)
        {
            var tris = mesh.triangles;
            var verts = mesh.vertices;
            var claves = new Vector3Int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
                claves[i] = new Vector3Int(
                    Mathf.RoundToInt(verts[i].x * 2000f),
                    Mathf.RoundToInt(verts[i].y * 2000f),
                    Mathf.RoundToInt(verts[i].z * 2000f));

            var aristas = new Dictionary<(Vector3Int, Vector3Int), int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                var a = claves[tris[t]];
                var b = claves[tris[t + 1]];
                var c = claves[tris[t + 2]];
                if (a == b || b == c || a == c) continue;   // astilla del polo: no es cara

                // Arista sin dirección: las dos caras que la comparten deben caer
                // en la misma clave, así que se ordena el par. Lexicográfico y sin
                // empates: un hash que igualara los dos extremos dejaría cada
                // dirección con su propia clave y la arista contaría 1+1.
                Cuenta(aristas, a, b);
                Cuenta(aristas, b, c);
                Cuenta(aristas, c, a);
            }
            return aristas;
        }

        private static void Cuenta(Dictionary<(Vector3Int, Vector3Int), int> aristas,
                                   Vector3Int a, Vector3Int b)
        {
            var clave = a.x != b.x ? (a.x < b.x ? (a, b) : (b, a))
                      : a.y != b.y ? (a.y < b.y ? (a, b) : (b, a))
                      : a.z <= b.z ? (a, b) : (b, a);
            aristas.TryGetValue(clave, out int n);
            aristas[clave] = n + 1;
        }

        [Test]
        public void ElCasqueteEstaCerradoPorAbajo()
        {
            // Las cinco coberturas que se usan de verdad en ChibiMeshBuilder: rapado
            // 0,34 (:355), pelo 0,44 (:356) y 0,52 (:357), gorro 0,55 (:302) y capucha
            // 0,62 (:260).
            //
            // Estaban puestas solo dos, con un comentario que las llamaba «los dos
            // extremos» — y el extremo bajo de verdad, el rapado, no se probaba. Lo cazó
            // el verificador. La topología del cierre no depende de la cobertura, así que
            // la aserción no ganaba ni perdía fuerza; lo que había era un comentario que
            // mentía sobre su propio alcance, que es peor que no tenerlo: el que venga a
            // añadir un peinado nuevo lo lee y cree que ya está cubierto.
            foreach (var coverage in new[] { 0.34f, 0.44f, 0.52f, 0.55f, 0.62f })
            {
                var cap = MeshShapes.SphericalCap(20, 10, coverage);
                try
                {
                    foreach (var arista in Aristas(cap))
                        Assert.AreEqual(2, arista.Value,
                            $"con cobertura {coverage}, una arista queda sin pareja: " +
                            "el casquete sigue abierto por abajo");

                    // El cierre son «segments» triángulos más: campana + disco.
                    Assert.AreEqual(20 * 10 * 2 + 20, cap.triangles.Length / 3,
                        "cerrar no debía costar más que un abanico contra el borde");

                    // Y el cierre mira hacia abajo: si mirara arriba, desde abajo se
                    // vería del revés.
                    bool hayFondo = false;
                    foreach (var normal in cap.normals)
                        if (normal.y < -0.99f) { hayFondo = true; break; }
                    Assert.IsTrue(hayFondo,
                        "el disco de cierre no trae normales hacia abajo");
                }
                finally
                {
                    Object.DestroyImmediate(cap);
                }
            }
        }

        [Test]
        public void LaCaraAbrazaLaCabezaYEscondeElBorde()
        {
            var meshes = ChibiMeshBuilder.Build(AppearanceData.Default);
            try
            {
                // Espejo de ChibiMeshBuilder.HeadCentreOf: las proporciones de la
                // cabeza no son públicas porque fuera de la malla nadie las necesita,
                // pero afirmar «el borde está bajo la piel» exige saber dónde está la
                // piel. Si algún día se retunean, esta copia se actualiza con ellas.
                float height = Mathf.Lerp(0.95f, 1.35f, AppearanceData.Default.BodyHeight);
                float headSize = height * 0.42f;
                float hw = headSize * Mathf.Lerp(0.9f, 1.12f, AppearanceData.Default.HeadWidth);
                float hh = headSize * Mathf.Lerp(0.92f, 1.1f, AppearanceData.Default.HeadHeight);
                float hc = height * 0.24f + height * 0.30f + hh * 0.44f;

                // Semiejes del elipsoide de la cabeza, igual que en BuildFaceQuad.
                float ax = hw * 0.5f, ay = hh * 0.5f, az = hw * 0.47f;

                var verts = meshes.Face.vertices;
                var uvs = meshes.Face.uv;
                Assert.AreEqual(verts.Length, uvs.Length, "la cara perdió sus UV");

                for (int i = 0; i < verts.Length; i++)
                {
                    float fx = verts[i].x / ax;
                    float fy = (verts[i].y - hc) / ay;
                    float fz = verts[i].z / az;
                    float f = fx * fx + fy * fy + fz * fz;

                    bool esBorde = uvs[i].x <= 0f || uvs[i].x >= 1f ||
                                   uvs[i].y <= 0f || uvs[i].y >= 1f;
                    if (esBorde)
                        Assert.Less(f, 0.99f,
                            $"el vértice {i} del borde de la cara no queda bajo la " +
                            "piel: desde ángulos rasantes se verá el contorno del parche");
                    else
                        Assert.That(f, Is.InRange(0.85f, 1.06f),
                            $"el vértice {i} ni abraza el cráneo ni flota junto a él " +
                            $"(f={f:F3}): la cara se ha despegado o se ha hundido");
                }
            }
            finally
            {
                Object.DestroyImmediate(meshes.Hair);
                Object.DestroyImmediate(meshes.Skin);
                Object.DestroyImmediate(meshes.Clothes);
                Object.DestroyImmediate(meshes.Face);
            }
        }
    }
}
