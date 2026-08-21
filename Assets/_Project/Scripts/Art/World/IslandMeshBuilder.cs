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

            var grid = new Vector3[rings * segments];

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

                    // Estrías: la roca entra y sale por franjas verticales. Antes era
                    // un cono liso y desde el aire la isla parecía apoyada en un aro
                    // de cartón marrón; con las estrías y el sombreado plano se le ven
                    // los planos, que es como se dibuja un peñasco.
                    r *= 1f + (ValueNoise.At(seg * 0.63f + seed, t * 3.1f) - 0.5f) * 0.16f;
                    y += (ValueNoise.At(seg * 0.41f - seed, t * 2.2f) - 0.5f) * depth * 0.06f;

                    grid[ring * segments + seg] =
                        new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
                }
            }

            var tip = new Vector3(0f, -depth * 1.16f, 0f);

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            // Cada cara con sus propios vértices: es lo que le da aristas. Ver
            // RockMeshBuilder, que hace lo mismo con los pedruscos sueltos.
            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(0f, 1f));
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }

            for (int ring = 0; ring < rings - 1; ring++)
            for (int seg = 0; seg < segments; seg++)
            {
                int next = (seg + 1) % segments;
                var a = grid[ring * segments + seg];
                var b = grid[(ring + 1) * segments + seg];
                var c = grid[ring * segments + next];
                var d = grid[(ring + 1) * segments + next];

                Face(a, b, c);
                Face(c, b, d);
            }

            int last = (rings - 1) * segments;
            for (int seg = 0; seg < segments; seg++)
                Face(grid[last + seg], tip, grid[last + (seg + 1) % segments]);

            return Build("isla_roca", vertices, uv, triangles);
        }

        /// <summary>
        /// Un árbol. El del centro de la plaza es el Árbol Nimbo.
        /// </summary>
        /// <remarks>
        /// El tronco ya no es un tubo: se estrecha, se ensancha al pie y echa tres
        /// ramas que se meten en la copa. Las ramas no se ven casi —la copa las
        /// tapa— y hacen falta igual: sin ellas, la copa flota sobre un palo y se le
        /// nota. Con ellas, el ojo da por hecho que hay un árbol debajo.
        ///
        /// La copa la teje <see cref="FoliageMeshBuilder"/>, que es quien sabe el
        /// truco de las normales. Aquí solo se dice dónde va.
        /// </remarks>
        public static (Mesh trunk, Mesh crown) BuildTree(float height, float crownRadius,
                                                         uint seed = 5u)
        {
            var rng = new Rng(seed);

            float crownHeight = height * 0.74f;
            var parts = new List<(Mesh, Matrix4x4)>
            {
                // El fuste, cónico.
                (Chibi.MeshShapes.Cylinder(9, height * 0.030f, height * 0.058f, height * 0.66f),
                 Matrix4x4.Translate(new Vector3(0f, height * 0.33f, 0f))),

                // El pie: un árbol se ensancha donde toca el suelo, y ese ensanche es
                // lo que le quita el aire de poste clavado.
                (Chibi.MeshShapes.Cylinder(9, height * 0.058f, height * 0.098f, height * 0.11f),
                 Matrix4x4.Translate(new Vector3(0f, height * 0.055f, 0f))),
            };

            for (int i = 0; i < 3; i++)
            {
                float angle = i * (Mathf.PI * 2f / 3f) + rng.Range(-0.35f, 0.35f);
                float lift = height * rng.Range(0.50f, 0.62f);
                float reach = crownRadius * rng.Range(0.34f, 0.52f);

                var direction = new Vector3(Mathf.Cos(angle), 1.15f, Mathf.Sin(angle)).normalized;
                var branch = Chibi.MeshShapes.Cylinder(6, height * 0.010f, height * 0.026f,
                                                       reach * 2f);

                parts.Add((branch, Matrix4x4.TRS(
                    new Vector3(direction.x * reach, lift + direction.y * reach, direction.z * reach),
                    Quaternion.FromToRotation(Vector3.up, direction),
                    Vector3.one)));
            }

            var lobes = FoliageMeshBuilder.CrownLobes(crownRadius, seed);
            for (int i = 0; i < lobes.Count; i++)
                lobes[i] = new FoliageMeshBuilder.Lobe(
                    lobes[i].Centre + Vector3.up * crownHeight, lobes[i].Radii);

            // El corazón, más abajo que el centro de la copa: las normales salen de
            // aquí, y bajándolo la copa se ilumina por arriba y el envés queda en
            // sombra, que es como se dibuja una copa.
            var pivot = new Vector3(0f, crownHeight - crownRadius * 0.34f, 0f);

            return (Chibi.MeshShapes.Combine(parts, "arbol_tronco"),
                    FoliageMeshBuilder.Weave(lobes, pivot, seed, "arbol_copa"));
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
