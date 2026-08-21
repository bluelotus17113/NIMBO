using System.Collections.Generic;
using Nimbo.Core.Util;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Piedras: pocas caras, planas y con esquinas.
    /// </summary>
    /// <remarks>
    /// Las de antes eran esferas achatadas, y una esfera con sombreado suave es un
    /// guijarro de plástico se pinte como se pinte. En un fondo de anime una roca se
    /// dibuja con planos: dos o tres caras al sol, dos o tres en sombra y una arista
    /// limpia entre ellas. Eso no sale del color, sale de la malla.
    ///
    /// Por eso aquí cada triángulo lleva sus propios vértices con la normal de la
    /// cara. Es más memoria —tres vértices por triángulo en vez de compartirlos— y a
    /// cambio la piedra tiene aristas de verdad, que es lo único que se le pide.
    /// </remarks>
    public static class RockMeshBuilder
    {
        /// <summary>
        /// Un pedrusco de <paramref name="segments"/>×<paramref name="rings"/> caras,
        /// abollado con ruido.
        /// </summary>
        /// <param name="squash">Cuánto se aplasta: 1 es una bola, 0,6 una piedra.</param>
        public static Mesh Boulder(uint seed, int segments = 8, int rings = 5,
                                   float squash = 0.68f, float roughness = 0.28f)
        {
            // Primero la superficie compartida, para que la costura del cilindro
            // cierre: el ruido va sobre la dirección, así que los dos bordes de la
            // costura piden el mismo número y coinciden solos.
            var grid = new Vector3[(rings + 1) * (segments + 1)];

            for (int ring = 0; ring <= rings; ring++)
            {
                float phi = Mathf.PI * ring / rings;
                float sinPhi = Mathf.Sin(phi), cosPhi = Mathf.Cos(phi);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float theta = Mathf.PI * 2f * seg / segments;
                    var dir = new Vector3(sinPhi * Mathf.Cos(theta), cosPhi,
                                          sinPhi * Mathf.Sin(theta));

                    float lumps = Noise(dir, 2.1f, seed) * 0.65f + Noise(dir, 4.9f, seed + 17u) * 0.35f;
                    float swell = 1f + (lumps - 0.5f) * 2f * roughness;

                    grid[ring * (segments + 1) + seg] =
                        new Vector3(dir.x * swell, dir.y * swell * squash, dir.z * swell);
                }
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<int>();

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-10f) return;
                normal.Normalize();

                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                normals.Add(normal); normals.Add(normal); normals.Add(normal);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(0f, 1f));
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
            }

            for (int ring = 0; ring < rings; ring++)
            for (int seg = 0; seg < segments; seg++)
            {
                var a = grid[ring * (segments + 1) + seg];
                var b = grid[(ring + 1) * (segments + 1) + seg];
                var c = grid[ring * (segments + 1) + seg + 1];
                var d = grid[(ring + 1) * (segments + 1) + seg + 1];

                Face(a, b, c);
                Face(c, b, d);
            }

            var mesh = new Mesh { name = "roca" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Noise(Vector3 dir, float frequency, uint seed)
        {
            float offset = seed * 0.0137f;
            float a = ValueNoise.At(dir.x * frequency + offset, dir.z * frequency - offset);
            float b = ValueNoise.At(dir.y * frequency - offset, dir.x * frequency + offset * 0.5f);
            return (a + b) * 0.5f;
        }
    }
}
