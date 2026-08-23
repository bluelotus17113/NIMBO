using System.Collections.Generic;
using Nimbo.Art.Materials;
using Nimbo.Core.Util;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Siembra el prado: matas de hierba y manchas de flores sobre la superficie.
    /// </summary>
    /// <remarks>
    /// Hasta ahora el prado era el propio disco de la isla pintado de verde. Desde la
    /// cámara del juego eso es una alfombra, y a ras de suelo es un linóleo: no hay
    /// una sola brizna. Todo el estilo de la referencia empieza aquí, porque lo que
    /// da el aire de fondo pintado no es el color del suelo sino que el suelo tenga
    /// pelo y que ese pelo se mueva.
    ///
    /// **Las matas se siembran sobre los triángulos de la malla, no con rayos.** La
    /// superficie es un disco ondulado del que ya tenemos los vértices; preguntarle
    /// al motor por el suelo nueve mil veces cuesta nueve mil trazados y además
    /// depende de que el colisionador ya esté puesto, que es una carrera que este
    /// proyecto ya perdió una vez con los nodos de recoger.
    ///
    /// **La normal de la brizna es casi la del suelo.** Es el truco que en la
    /// referencia hace un modificador de transferencia de datos: si cada brizna
    /// se sombrea por su cuenta, un prado es ruido; heredando la normal del suelo,
    /// el césped se ilumina como **una** superficie y encima queda pegado al color
    /// del terreno, que es el punto 3 de la lámina («terrain — synchronized with
    /// grass»). Se le deja una pizca de la suya para que no sea del todo plano.
    /// </remarks>
    public static class Meadow
    {
        /// <summary>Una mata puesta en el prado.</summary>
        public readonly struct Tuft
        {
            public readonly Vector3 Position;
            public readonly Vector3 Normal;
            public readonly float Yaw;
            public readonly float Size;
            public readonly float Seed;

            public Tuft(Vector3 position, Vector3 normal, float yaw, float size, float seed)
            {
                Position = position; Normal = normal; Yaw = yaw; Size = size; Seed = seed;
            }
        }

        /// <summary>Un círculo donde no se siembra: una casa, el tronco grande, la plaza.</summary>
        public readonly struct KeepOut
        {
            public readonly Vector3 Centre;
            public readonly float Radius;
            public KeepOut(Vector3 centre, float radius) { Centre = centre; Radius = radius; }
        }

        /// <summary>
        /// Cuántas briznas lleva una mata.
        /// </summary>
        /// <remarks>
        /// La referencia lleva cuatro y aquí van seis. No es por llevar la contraria:
        /// allí la siembra es densísima y las matas se tocan; aquí hay una isla de
        /// cien metros que cubrir con un presupuesto, así que las matas van separadas
        /// y cada una tiene que leerse como un manojo por sí sola. Con cuatro se veían
        /// briznas sueltas repartidas por un suelo liso.
        /// </remarks>
        private const int BladesPerTuft = 6;

        /// <summary>Y cuántos tramos cada brizna: tres cuadros, seis triángulos.</summary>
        private const int BladeSegments = 3;

        /// <summary>En cuántos trozos se parte el prado, para que se pueda descartar por partes.</summary>
        private const int Sectors = 8;
        private const int Rings = 3;

        // ── siembra ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reparte <paramref name="count"/> matas por la superficie, en proporción al
        /// área de cada triángulo.
        /// </summary>
        /// <remarks>
        /// Por área y no por triángulo: los anillos de fuera de un disco tienen
        /// triángulos mucho más grandes que los de dentro, y repartiendo a partes
        /// iguales el centro quedaba como una alfombra y el borde pelado.
        /// </remarks>
        public static List<Tuft> Sow(Mesh surface, int count, uint seed,
                                     IReadOnlyList<KeepOut> keepOut = null,
                                     Vector3 worldOffset = default, float clumping = 0.55f)
        {
            var tufts = new List<Tuft>(count);
            if (surface == null || count <= 0) return tufts;

            var vertices = surface.vertices;
            var triangles = surface.triangles;
            int faces = triangles.Length / 3;
            if (faces == 0) return tufts;

            // Tabla acumulada de áreas: sortear una vez por mata sale a un binario
            // sobre esta tabla en vez de a un recorrido de mil trescientos triángulos.
            var cumulative = new float[faces];
            float total = 0f;
            for (int f = 0; f < faces; f++)
            {
                var a = vertices[triangles[f * 3]];
                var b = vertices[triangles[f * 3 + 1]];
                var c = vertices[triangles[f * 3 + 2]];
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                cumulative[f] = total;
            }
            if (total <= 0f) return tufts;

            var rng = new Rng(seed);

            // Se sortean más sitios de los que se van a usar y se aceptan por el mismo
            // ruido que pinta las manchas del suelo. La hierba sale así a rodales —
            // espesa donde el prado se aclara, rala donde se oscurece— en vez de
            // repartida por igual, y de paso los rodales coinciden con las manchas de
            // color en vez de contradecirlas. Un césped uniforme se lee como moqueta;
            // lo que dice «esto ha crecido solo» es que haya sitios.
            int attempts = count * 3;
            for (int i = 0; i < attempts && tufts.Count < count; i++)
            {
                int f = FaceFor(cumulative, rng.Range(0f, total));

                var a = vertices[triangles[f * 3]];
                var b = vertices[triangles[f * 3 + 1]];
                var c = vertices[triangles[f * 3 + 2]];

                // Coordenadas baricéntricas repartidas de verdad: con dos números al
                // azar sin doblar, los puntos se amontonan en una esquina.
                float u = rng.NextFloat(), v = rng.NextFloat();
                if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                var point = a + (b - a) * u + (c - a) * v;

                if (Blocked(keepOut, point)) continue;

                float density = 1f - clumping
                              + clumping * ValueNoise.Variation(point + worldOffset, 11f) * 2f;
                if (rng.NextFloat() > density) continue;

                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (normal.y < 0f) normal = -normal;

                tufts.Add(new Tuft(point, normal,
                                   rng.Range(0f, 360f),
                                   rng.Range(0.72f, 1.15f),
                                   rng.NextFloat()));
            }

            return tufts;
        }

        private static int FaceFor(float[] cumulative, float pick)
        {
            int low = 0, high = cumulative.Length - 1;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (cumulative[mid] < pick) low = mid + 1; else high = mid;
            }
            return low;
        }

        private static bool Blocked(IReadOnlyList<KeepOut> keepOut, Vector3 point)
        {
            if (keepOut == null) return false;
            for (int i = 0; i < keepOut.Count; i++)
            {
                var flat = new Vector2(point.x - keepOut[i].Centre.x, point.z - keepOut[i].Centre.z);
                if (flat.sqrMagnitude < keepOut[i].Radius * keepOut[i].Radius) return true;
            }
            return false;
        }

        // ── malla ────────────────────────────────────────────────────────────

        /// <summary>
        /// Teje un puñado de matas en una sola malla.
        /// </summary>
        /// <remarks>
        /// El color del vértice lleva el contrato que pide Nimbo/Foliage: rojo la
        /// oclusión, verde la máscara de viento, azul el degradado de la raíz a la
        /// punta y alfa la semilla de la mata. Cada brizna es un tramo de tres cuadros
        /// que se estrecha y se tumba: recta y del mismo ancho parece una púa, y un
        /// prado de púas es un cepillo.
        /// </remarks>
        public static Mesh Weave(IReadOnlyList<Tuft> tufts, int from, int count, string name)
        {
            int rows = BladeSegments + 1;
            int perBlade = rows * 2;
            int blades = Mathf.Max(0, count) * BladesPerTuft;

            var vertices = new List<Vector3>(blades * perBlade);
            var normals = new List<Vector3>(vertices.Capacity);
            var colours = new List<Color>(vertices.Capacity);
            var uv = new List<Vector2>(vertices.Capacity);
            var indices = new List<int>(blades * BladeSegments * 6);

            for (int t = from; t < from + count && t < tufts.Count; t++)
            {
                var tuft = tufts[t];
                var rng = new Rng((uint)(t * 2654435761u) ^ 0x517Cu);

                var up = tuft.Normal;
                var right = Vector3.Normalize(Vector3.Cross(up, Vector3.forward).sqrMagnitude > 0.001f
                    ? Vector3.Cross(up, Vector3.forward)
                    : Vector3.Cross(up, Vector3.right));
                var ahead = Vector3.Cross(right, up);

                for (int b = 0; b < BladesPerTuft; b++)
                {
                    float yaw = (tuft.Yaw + b * (360f / BladesPerTuft)
                                 + rng.Range(-26f, 26f)) * Mathf.Deg2Rad;
                    var lean = right * Mathf.Cos(yaw) + ahead * Mathf.Sin(yaw);

                    float height = tuft.Size * rng.Range(0.46f, 0.74f);
                    float width = tuft.Size * rng.Range(0.038f, 0.056f);

                    // Menos tumbada que antes. Con la curva fuerte, una mata era un
                    // puñado de arcos que se abrían y se veía el suelo por el medio;
                    // más recta, las briznas se tapan unas a otras y el manojo se
                    // lee macizo.
                    float bend = rng.Range(0.20f, 0.42f);

                    // Las briznas no salen del mismo punto: una mata es un manojo.
                    var root = tuft.Position + lean * (tuft.Size * rng.Range(0.02f, 0.14f));

                    int start = vertices.Count;
                    for (int r = 0; r < rows; r++)
                    {
                        float k = (float)r / BladeSegments;

                        // Se dobla en cuadrática y por eso pierde altura al final:
                        // una brizna que se tumba sin acortarse se estira como goma.
                        var centre = root
                                   + up * (height * (k - bend * k * k * 0.35f))
                                   + lean * (height * bend * k * k);

                        float halfWidth = width * Mathf.Lerp(1f, 0.18f, Mathf.Pow(k, 0.8f));
                        var side = Vector3.Cross(lean, up).normalized * halfWidth;

                        // Casi la del suelo. Ver la nota de arriba: es lo que hace que
                        // el césped se lea como una superficie y no como ruido verde.
                        var normal = Vector3.Normalize(up * 0.86f + lean * 0.14f);

                        // Rojo: la base de la mata está tapada por sus propias briznas.
                        // **Empieza en 0,78 y no más abajo.** La oclusión no oscurece:
                        // mezcla con la banda profunda, que es el verde azulado del
                        // fondo de una copa. Empezando en 0,42, más de la mitad de cada
                        // brizna era ese verde de fondo y el prado salió de briznas
                        // oscuras sobre un suelo claro, como un cepillo.
                        var colour = new Color(Mathf.Lerp(0.78f, 1f, k),
                                               Mathf.Pow(k, 1.3f),
                                               Mathf.Lerp(0.55f, 1f, k),
                                               tuft.Seed);

                        vertices.Add(centre - side); normals.Add(normal);
                        colours.Add(colour); uv.Add(new Vector2(0f, k));

                        vertices.Add(centre + side); normals.Add(normal);
                        colours.Add(colour); uv.Add(new Vector2(1f, k));
                    }

                    for (int r = 0; r < BladeSegments; r++)
                    {
                        int lower = start + r * 2;
                        indices.Add(lower); indices.Add(lower + 2); indices.Add(lower + 1);
                        indices.Add(lower + 1); indices.Add(lower + 2); indices.Add(lower + 3);
                    }
                }
            }

            return Assemble(name, vertices, normals, colours, uv, indices);
        }

        /// <summary>Cuántos pétalos tiene una flor.</summary>
        private const int Petals = 5;

        /// <summary>
        /// Las flores: una roseta de cinco pétalos, pequeña y de lado.
        /// </summary>
        /// <remarks>
        /// **Solo donde el prado se aclara.** El sitio se decide con el mismo ruido
        /// que pinta las manchas de color en el shader (<see cref="ValueNoise"/>), así
        /// que las flores caen dentro de las manchas claras en vez de repartidas por
        /// igual. Repartidas por igual no son un prado con flores: son confeti.
        ///
        /// **Roseta y no tarjeta cuadrada.** El primer intento eran cuadrados grandes
        /// inclinados casi de canto, y de cerca el prado parecía sembrado de papelitos
        /// de colores: un rectángulo girado al azar no se lee como una flor desde
        /// ningún ángulo. Con cinco pétalos y puestas de lado, la silueta dice «flor»
        /// aun midiendo cinco centímetros, que es lo que mide.
        /// </remarks>
        public static Mesh Bloom(IReadOnlyList<Tuft> tufts, Vector3 worldOffset,
                                 float threshold, float variationScale, uint seed, string name)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var uv = new List<Vector2>();
            var indices = new List<int>();

            var rng = new Rng(seed);

            for (int t = 0; t < tufts.Count; t++)
            {
                var tuft = tufts[t];
                if (ValueNoise.Variation(tuft.Position + worldOffset, variationScale) < threshold) continue;
                if (!rng.Chance(0.30f)) continue;

                var up = tuft.Normal;
                float spin = rng.Range(0f, 360f) * Mathf.Deg2Rad;
                var right = new Vector3(Mathf.Cos(spin), 0f, Mathf.Sin(spin));
                var ahead = Vector3.Cross(up, right).normalized;

                // De lado, no tumbada: la cámara dejó de mirar desde arriba. La
                // premisa vieja —«inclinarla más la hacía desaparecer de canto»—
                // valía para el picado de 15 m / 48°; con la tercera persona a
                // 5,5 m / 18° una roseta casi horizontal ES lo que desaparece de
                // canto: vista del horizonte proyecta sen(inclinación), y el 13°
                // medio de antes son la cuarta parte de su cara. El rango 55–85°
                // le da a la cámara baja entre el 82 % y el 100 %. El techo no
                // llega a 90° porque el plano general (150 m / 45°) sigue existiendo
                // mientras no hay protagonista, y desde arriba una vertical caería
                // a cos(90°) = 0.
                var tilt = Quaternion.AngleAxis(rng.Range(55f, 85f), right);
                var faceUp = tilt * up;
                right = tilt * right;
                ahead = tilt * ahead;

                float size = tuft.Size * rng.Range(0.055f, 0.085f);

                // A media brizna: una flor que asome por encima de la hierba se ve
                // flotando, y metida del todo no se ve.
                var centre = tuft.Position + up * (tuft.Size * rng.Range(0.22f, 0.40f));

                int start = vertices.Count;
                var heart = new Color(1f, 0.72f, 0.62f, rng.NextFloat());

                vertices.Add(centre); normals.Add(faceUp);
                colours.Add(heart); uv.Add(new Vector2(0.5f, 0.5f));

                // Punta de pétalo y valle, alternados: es lo que dibuja el borde
                // recortado de la flor con la mitad de vértices que un pétalo entero.
                for (int i = 0; i < Petals * 2; i++)
                {
                    float angle = i * Mathf.PI / Petals;
                    float reach = size * (i % 2 == 0 ? 1f : 0.44f);

                    vertices.Add(centre + right * (Mathf.Cos(angle) * reach)
                                        + ahead * (Mathf.Sin(angle) * reach));
                    normals.Add(faceUp);
                    colours.Add(new Color(1f, 0.80f, 1f, heart.a));
                    uv.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f,
                                       Mathf.Sin(angle) * 0.5f + 0.5f));
                }

                for (int i = 0; i < Petals * 2; i++)
                {
                    indices.Add(start);
                    indices.Add(start + 1 + i);
                    indices.Add(start + 1 + (i + 1) % (Petals * 2));
                }
            }

            return vertices.Count == 0
                ? null
                : Assemble(name, vertices, normals, colours, uv, indices);
        }

        private static Mesh Assemble(string name, List<Vector3> vertices, List<Vector3> normals,
                                     List<Color> colours, List<Vector2> uv, List<int> indices)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ── montaje ──────────────────────────────────────────────────────────

        /// <summary>
        /// Deja el prado montado bajo <paramref name="parent"/>: la hierba en trozos y
        /// las flores en tres mallas, una por color.
        /// </summary>
        /// <remarks>
        /// **En trozos y no en una sola malla.** Una malla de nueve mil matas es una
        /// caja de cien metros: o se dibuja entera o nada, y se dibuja entera siempre
        /// aunque tengas la cara pegada a una pared. Partida en veinticuatro sectores,
        /// el motor descarta los que quedan detrás de la cámara sin que hagamos nada.
        ///
        /// **La hierba no proyecta sombra, a propósito.** La sombra de una brizna no se
        /// ve desde ninguna cámara de este juego, y dibujar el prado otra vez para el
        /// mapa de sombras es duplicar el coste de lo más caro que hay en pantalla. Sí
        /// la **recibe**: que la sombra del árbol caiga sobre la hierba es justo lo que
        /// hace que la hierba parezca estar debajo del árbol.
        /// </remarks>
        public static void Build(Transform parent, Mesh surface, float radius, uint seed,
                                 int tuftCount, Color grass, Vector3 worldOffset,
                                 IReadOnlyList<KeepOut> keepOut = null)
        {
            var tufts = Sow(surface, tuftCount, seed, keepOut, worldOffset);
            if (tufts.Count == 0) return;

            var root = new GameObject("Prado").transform;
            root.SetParent(parent, worldPositionStays: false);

            var buckets = new List<Tuft>[Sectors * Rings];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = new List<Tuft>();

            foreach (var tuft in tufts)
            {
                float angle = Mathf.Atan2(tuft.Position.z, tuft.Position.x) + Mathf.PI;
                int sector = Mathf.Clamp((int)(angle / (Mathf.PI * 2f) * Sectors), 0, Sectors - 1);

                float distance = new Vector2(tuft.Position.x, tuft.Position.z).magnitude;
                int ring = Mathf.Clamp((int)(distance / Mathf.Max(radius, 0.001f) * Rings), 0, Rings - 1);

                buckets[ring * Sectors + sector].Add(tuft);
            }

            var material = ToonPalette.Foliage(grass);

            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i].Count == 0) continue;
                var mesh = Weave(buckets[i], 0, buckets[i].Count, $"hierba_{i}");
                AddPiece(root, $"hierba_{i}", mesh, material);
            }

            // Las flores van aparte: son otro color y hay tres, así que meterlas en la
            // malla de la hierba obligaría a partir cada trozo en cuatro submallas.
            for (int c = 0; c < ToonPalette.Flowers.Length; c++)
            {
                var mesh = Bloom(tufts, worldOffset, 0.60f + c * 0.05f, 11f,
                                 seed * 977u + (uint)c, $"flores_{c}");
                if (mesh == null) continue;

                AddPiece(root, $"flores_{c}", mesh,
                         ToonPalette.Foliage(ToonPalette.Flowers[c], windStrength: 0.16f,
                                             rootDarken: 0.10f));
            }
        }

        private static void AddPiece(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }
    }
}
