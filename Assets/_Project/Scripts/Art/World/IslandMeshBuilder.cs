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
        /// <summary>
        /// El labio del último prado construido: sus vértices exteriores de verdad,
        /// no una repetición de sus fórmulas. <see cref="BuildUnderside"/> lo consume
        /// para nacer pegado a ese labio exacto.
        /// </summary>
        /// <remarks>
        /// Un depósito a un solo uso y no un parámetro del constructor porque quien
        /// llama no sabe de la costura: WorldView construye prado y roca con dos
        /// llamadas independientes, y en la isla del jugador le pasa a la roca una
        /// semilla distinta de la del prado sin saber que importaba. Repetir aquí las
        /// fórmulas del contorno es lo que abrió la rendija de 2,8 m: basta cambiar
        /// un parámetro en un solo lado para que los dos bordes vuelvan a divergir.
        /// Los pares de llamadas son inmediatos (WorldView.BuildIsland,
        /// BuildHomeIsland y Screenshotter), así que «el último prado» es siempre
        /// «el prado de esta isla».
        /// </remarks>
        private static Vector3[] _labioDelPrado;

        /// <summary>La superficie: un disco con el borde irregular y ondulaciones suaves.</summary>
        public static Mesh BuildSurface(float radius, int rings = 14, int segments = 48, uint seed = 7)
        {
            var vertices = new List<Vector3>((rings + 1) * segments + 1);
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(rings * segments * 6);

            FillSurface(radius, rings, segments, seed, vertices, uv, triangles);

            return Build("isla_superficie", vertices, uv, triangles);
        }

        /// <summary>
        /// Rellena la geometría del prado. También es de donde sale el borde que
        /// cose la roca de <see cref="BuildUnderside"/>: al terminar deja el anillo
        /// exterior en <see cref="_labioDelPrado"/> para que la roca nazca de esos
        /// vértices y no de fórmulas repetidas.
        /// </summary>
        private static void FillSurface(float radius, int rings, int segments, uint seed,
                                        List<Vector3> vertices, List<Vector2> uv,
                                        List<int> triangles)
        {
            var edge = EdgeProfile(segments, seed);

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
                    float height = MeadowHeight(angle, t);

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

            // El anillo exterior, tal cual ha nacido: es lo que la roca cogerá para
            // soldarse. Copia, no referencia: las listas mueren con este método.
            _labioDelPrado = vertices.GetRange(vertices.Count - segments, segments).ToArray();
        }

        /// <summary>
        /// El contorno irregular del borde: una sola copia de la fórmula y su
        /// suavizado. Prado y roca la comparten; antes cada uno tiraba la suya con
        /// la misma semilla, y bastaba un parámetro cambiado a medias para abrirlos.
        /// </summary>
        private static float[] EdgeProfile(int segments, uint seed)
        {
            var rng = new Rng(seed);
            var edge = new float[segments];
            for (int i = 0; i < segments; i++) edge[i] = rng.Range(0.86f, 1f);
            Smooth(edge, passes: 3);
            return edge;
        }

        /// <summary>
        /// La altura del prado según el ángulo y la distancia al centro. La roca la
        /// consulta para saber dónde cuelga el labio cuando construye sin prado al
        /// que cosecharse (capturas sueltas, pruebas).
        /// </summary>
        private static float MeadowHeight(float angle, float t)
        {
            return Mathf.Sin(angle * 3f) * 0.9f * t
                 + Mathf.Cos(angle * 5f + 1.3f) * 0.6f * t
                 - Mathf.Pow(t, 6f) * 3.2f;
        }

        /// <summary>
        /// La roca de debajo: nace soldada al labio del último prado construido y
        /// se estrecha hasta una punta. Es la silueta que dice «esto flota».
        /// </summary>
        /// <remarks>
        /// El anillo 0 no se calcula: son los vértices exteriores del prado, leídos
        /// de verdad. Antes la roca repetía por su cuenta el contorno del prado y
        /// los dos bordes divergían: el labio cae entre −4,68 y −1,72 y el anillo 0
        /// flotaba entre −1,86 y +1,86, con sectores de hasta ~2,8 m de banda
        /// abierta por la que se ve a través de la isla a la altura de los ojos,
        /// justo donde pescan los vecinos (78 % del radio o más). Soldar los
        /// bordes cierra el volumen sin una sola cara nueva; una falda colgante no
        /// valía aquí porque hay sectores donde el anillo 0 sube por encima del
        /// labio y la banda se plegaría sobre sí misma.
        ///
        /// La soldadura se estira hacia abajo y se disuelve con (1−t)²: la roca
        /// cuelga desde la altura real de cada sector del labio en vez de arrancar
        /// recta debajo de un borde que baja hasta −4,7 m.
        ///
        /// Sin prado reciente del que cosecharse (capturas sueltas, pruebas),
        /// reconstruye el labio con las mismas funciones que usa el prado: mismo
        /// resultado mientras prado y roca se pidan con los mismos parámetros.
        /// </remarks>
        public static Mesh BuildUnderside(float radius, float depth, int rings = 10,
                                          int segments = 48, uint seed = 7)
        {
            // Cosecha de un solo uso: cogido y liberado, para que dos islas
            // construidas seguidas no se crucen los labios.
            var labio = _labioDelPrado;
            _labioDelPrado = null;
            bool cosido = labio != null && labio.Length == segments;

            // El perfil lo dicta el prado de verdad — también su semilla, aunque a
            // esta roca le llegara otra por parámetro: la isla del jugador siembra
            // el prado con la 31 y a la roca la llama sin semilla, la 7 por
            // defecto. Medir el radio del labio es leer la semilla sin conocerla.
            float[] perfil = cosido ? null : EdgeProfile(segments, seed);
            var edge = new float[segments];
            for (int seg = 0; seg < segments; seg++)
            {
                edge[seg] = cosido
                    ? Mathf.Sqrt(labio[seg].x * labio[seg].x + labio[seg].z * labio[seg].z) / radius
                    : perfil[seg];
            }

            var jag = new float[segments];
            var jagRng = new Rng(seed * 31 + 1);
            for (int i = 0; i < segments; i++) jag[i] = jagRng.Range(0.75f, 1.25f);

            var grid = new Vector3[rings * segments];

            // Anillo 0: el labio, tal cual. Aquí se cierra el volumen.
            if (cosido)
            {
                for (int seg = 0; seg < segments; seg++) grid[seg] = labio[seg];
            }
            else
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    float angle = (float)seg / segments * Mathf.PI * 2f;
                    float r = radius * edge[seg];
                    grid[seg] = new Vector3(Mathf.Cos(angle) * r, MeadowHeight(angle, 1f),
                                            Mathf.Sin(angle) * r);
                }
            }

            for (int ring = 1; ring < rings; ring++)
            {
                float t = (float)ring / (rings - 1);
                // Cuadrática: se estrecha despacio al principio y deprisa al final,
                // que es como se rompe la roca de verdad.
                float shrink = 1f - t * t * 0.94f;

                for (int seg = 0; seg < segments; seg++)
                {
                    float angle = (float)seg / segments * Mathf.PI * 2f;
                    float r = radius * edge[seg] * shrink * Mathf.Lerp(1f, jag[seg], t * 0.55f);

                    // Estrías: la roca entra y sale por franjas verticales. Antes era
                    // un cono liso y desde el aire la isla parecía apoyada en un aro
                    // de cartón marrón; con las estrías y el sombreado plano se le ven
                    // los planos, que es como se dibuja un peñasco. Se estiran con t
                    // para que no muevan el anillo soldado: en el labio la roca es
                    // exactamente el prado y nada la perturba.
                    r *= 1f + (ValueNoise.At(seg * 0.63f + seed, t * 3.1f) - 0.5f) * 0.16f * t;

                    // Desde la altura real del labio en este sector, disolviéndose
                    // con (1−t)²: en la punta manda el perfil cuadrático de siempre,
                    // junto al labio manda el prado.
                    float labioY = cosido ? labio[seg].y : MeadowHeight(angle, 1f);
                    float y = -Mathf.Pow(t, 1.4f) * depth + labioY * (1f - t) * (1f - t);

                    y += (ValueNoise.At(seg * 0.41f - seed, t * 2.2f) - 0.5f) * depth * 0.06f * t;

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
