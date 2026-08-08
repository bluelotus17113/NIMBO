using System.Collections.Generic;
using Nimbo.Core.Util;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Construye la isla flotante: el prado de arriba y la roca que cuelga por debajo.
    /// </summary>
    /// <remarks>
    /// El borde no es un círculo: se le mete ruido para que parezca un trozo de tierra
    /// arrancado y no una tarta. El mismo ruido decide el perfil de la roca de abajo,
    /// que es lo que da la silueta de isla flotante.
    /// </remarks>
    public static class IslandMeshBuilder
    {
        /// <summary>La superficie: un disco con el borde irregular y ondulaciones suaves.</summary>
        public static Mesh BuildSurface(float radius, int rings = 14, int segments = 48, uint seed = 7)
        {
            var rng = new Rng(seed);
            var edge = new float[segments];
            for (int i = 0; i < segments; i++) edge[i] = rng.Range(0.86f, 1f);
            Smooth(edge, passes: 3);

            var vertices = new List<Vector3>((rings + 1) * segments + 1);
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(rings * segments * 6);

            vertices.Add(Vector3.zero);
            uv.Add(new Vector2(0.5f, 0.5f));

            for (int ring = 1; ring <= rings; ring++)
            {
                float t = (float)ring / rings;
                for (int seg = 0; seg < segments; seg++)
                {
                    float angle = (float)seg / segments * Mathf.PI * 2f;
                    float r = radius * t * edge[seg];

                    // Ondulación suave del terreno, y el borde siempre cae un poco
                    // para que la hierba se doble hacia el vacío.
                    float height = Mathf.Sin(angle * 3f) * 0.9f * t
                                 + Mathf.Cos(angle * 5f + 1.3f) * 0.6f * t
                                 - Mathf.Pow(t, 6f) * 3.2f;

                    vertices.Add(new Vector3(Mathf.Cos(angle) * r, height, Mathf.Sin(angle) * r));
                    uv.Add(new Vector2(Mathf.Cos(angle) * t * 0.5f + 0.5f,
                                       Mathf.Sin(angle) * t * 0.5f + 0.5f));
                }
            }

            for (int seg = 0; seg < segments; seg++)
            {
                int next = (seg + 1) % segments;
                triangles.Add(0); triangles.Add(1 + next); triangles.Add(1 + seg);
            }

            for (int ring = 1; ring < rings; ring++)
            for (int seg = 0; seg < segments; seg++)
            {
                int next = (seg + 1) % segments;
                int inner = 1 + (ring - 1) * segments;
                int outer = 1 + ring * segments;

                triangles.Add(inner + seg); triangles.Add(inner + next); triangles.Add(outer + seg);
                triangles.Add(inner + next); triangles.Add(outer + next); triangles.Add(outer + seg);
            }

            return Build("isla_superficie", vertices, uv, triangles);
        }

        /// <summary>
        /// La roca de debajo: el mismo borde, estrechándose hasta una punta. Es la
        /// silueta que dice «esto flota» de un vistazo.
        /// </summary>
        public static Mesh BuildUnderside(float radius, float depth, int rings = 10,
                                          int segments = 48, uint seed = 7)
        {
            var rng = new Rng(seed);
            var edge = new float[segments];
            for (int i = 0; i < segments; i++) edge[i] = rng.Range(0.86f, 1f);
            Smooth(edge, passes: 3);

            var jag = new float[segments];
            var jagRng = new Rng(seed * 31 + 1);
            for (int i = 0; i < segments; i++) jag[i] = jagRng.Range(0.75f, 1.25f);

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            for (int ring = 0; ring < rings; ring++)
            {
                float t = (float)ring / (rings - 1);
                // Cuadrática: se estrecha despacio al principio y deprisa al final,
                // que es como se rompe la roca de verdad.
                float shrink = 1f - t * t * 0.94f;
                float y = -Mathf.Pow(t, 1.4f) * depth;

                for (int seg = 0; seg < segments; seg++)
                {
                    float angle = (float)seg / segments * Mathf.PI * 2f;
                    float r = radius * edge[seg] * shrink * Mathf.Lerp(1f, jag[seg], t * 0.55f);
                    vertices.Add(new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
                    uv.Add(new Vector2((float)seg / segments, 1f - t));
                }
            }

            int tip = vertices.Count;
            vertices.Add(new Vector3(0f, -depth * 1.16f, 0f));
            uv.Add(new Vector2(0.5f, 0f));

            for (int ring = 0; ring < rings - 1; ring++)
            for (int seg = 0; seg < segments; seg++)
            {
                int next = (seg + 1) % segments;
                int upper = ring * segments, lower = (ring + 1) * segments;

                triangles.Add(upper + seg); triangles.Add(lower + seg); triangles.Add(upper + next);
                triangles.Add(upper + next); triangles.Add(lower + seg); triangles.Add(lower + next);
            }

            int last = (rings - 1) * segments;
            for (int seg = 0; seg < segments; seg++)
            {
                int next = (seg + 1) % segments;
                triangles.Add(last + seg); triangles.Add(tip); triangles.Add(last + next);
            }

            return Build("isla_roca", vertices, uv, triangles);
        }

        /// <summary>Un árbol de copa redonda. El del centro de la plaza es el Árbol Nimbo.</summary>
        public static (Mesh trunk, Mesh crown) BuildTree(float height, float crownRadius)
        {
            var trunk = Chibi.MeshShapes.Cylinder(8, height * 0.05f, height * 0.08f, height * 0.55f);
            var crown = Chibi.MeshShapes.Sphere(16, 12, new Vector3(1f, 0.86f, 1f));

            var scaled = new List<(Mesh, Matrix4x4)>
            {
                (crown, Matrix4x4.TRS(new Vector3(0f, height * 0.78f, 0f),
                                      Quaternion.identity, Vector3.one * crownRadius * 2f)),
                (crown, Matrix4x4.TRS(new Vector3(crownRadius * 0.55f, height * 0.6f, crownRadius * 0.2f),
                                      Quaternion.identity, Vector3.one * crownRadius * 1.25f)),
                (crown, Matrix4x4.TRS(new Vector3(-crownRadius * 0.5f, height * 0.62f, -crownRadius * 0.3f),
                                      Quaternion.identity, Vector3.one * crownRadius * 1.35f)),
            };

            var trunkPlaced = Chibi.MeshShapes.Combine(
                new List<(Mesh, Matrix4x4)> { (trunk, Matrix4x4.Translate(new Vector3(0f, height * 0.275f, 0f))) },
                "arbol_tronco");

            return (trunkPlaced, Chibi.MeshShapes.Combine(scaled, "arbol_copa"));
        }

        /// <summary>Suaviza el borde para que las irregularidades no queden a picos.</summary>
        private static void Smooth(float[] values, int passes)
        {
            int n = values.Length;
            for (int pass = 0; pass < passes; pass++)
            {
                var copy = (float[])values.Clone();
                for (int i = 0; i < n; i++)
                    values[i] = (copy[(i - 1 + n) % n] + copy[i] * 2f + copy[(i + 1) % n]) * 0.25f;
            }
        }

        private static Mesh Build(string name, List<Vector3> vertices, List<Vector2> uv,
                                  List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
