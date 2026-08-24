using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// El decorado de fiesta: mástiles con banderines y farolillos alrededor del sitio
    /// donde pasa el evento.
    /// </summary>
    /// <remarks>
    /// Todo se genera aquí, como el resto del arte del proyecto: ni un modelo
    /// importado. Las mallas se construyen nuevas en cada fiesta y las destruye
    /// <see cref="FestivalDecor"/> al recoger el decorado — son unos dos mil
    /// vértices y rehacerlos cuesta menos que mantener una caché viva entre escenas.
    ///
    /// Los materiales no se crean aquí: se piden a <see cref="ToonPalette"/>, que los
    /// comparte y cachea por color. Los banderines usan los tres colores de las flores
    /// del prado (ToonPalette.Flowers), que es la paleta festiva que la isla ya tiene;
    /// los farolillos, el amarillo cálido de la bombilla de las farolas
    /// (DecorMeshBuilder.BaseColour). Que lo nuevo parezca de la misma isla es cosa de
    /// reutilizar sus colores, no de inventar otros.
    /// </remarks>
    public static class FestivalDecorBuilder
    {
        /// <summary>Radio del anillo de mástiles, en metros.</summary>
        /// <remarks>
        /// Tiene que caber dentro de las zonas donde hoy hay decorado: la plaza mide 18
        /// m de radio y el escenario 16 (IslandLayout.FirstIsland), y a ambos hay que
        /// dejarles margen para el edificio central. Diez cumple en las dos con holgura.
        /// Si mañana una zona bajara de once metros, este número se saldría al prado —
        /// y lo correcto entonces sería pedirle el radio a la isla, que hoy Nimbo.Art
        /// no puede leer (no ve Nimbo.Island); DecoradoDeFiestaTests vigila que el
        /// anillo siga cabiendo para que el fallo sea ruidoso y no un seto flotando
        /// sobre la hierba de al lado.
        /// </remarks>
        public const float RingRadius = 10f;

        /// <summary>Mástiles por anillo. Seis dan tramos de ~10,5 m: largos para
        /// tender una cuerda sin que le sobre cuerda, cortos para que no parezca un
        /// cercado.</summary>
        private const int PoleCount = 6;

        private const float PoleHeight = 3.2f;
        private const float StringY = 3.05f;

        /// <summary>Cuánto cae la cuerda a media luz. Una catenaria tensa se lee como
        /// un error de colocación; esta comba sí se ve colgada.</summary>
        private const float Sag = 0.8f;

        private const int FlagsPerSpan = 8;

        /// <summary>
        /// Levanta el decorado alrededor del origen de <paramref name="root"/>.
        /// </summary>
        /// <param name="created">
        /// Sale lleno de las mallas que este método ha estrenado, para que quien las
        /// cuelgue pueda destruirlas al recoger el decorado. No incluye las macetas,
        /// cuya malla es la compartida del catálogo (DecorMeshBuilder) y no es suya.
        /// </param>
        public static void Build(Transform root, List<Mesh> created)
        {
            var wood = new List<(Mesh mesh, Matrix4x4 transform)>();
            var lanterns = new List<(Mesh mesh, Matrix4x4 transform)>();
            var flags = new[]
            {
                new List<(Mesh mesh, Matrix4x4 transform)>(),
                new List<(Mesh mesh, Matrix4x4 transform)>(),
                new List<(Mesh mesh, Matrix4x4 transform)>(),
            };

            // El triángulo del banderín es uno solo para todos: cada color combina su
            // lista con este molde y luego se destruye, porque CombineMeshes copia.
            Mesh flagMould = FlagMould();

            for (int i = 0; i < PoleCount; i++)
            {
                float step = Mathf.PI * 2f / PoleCount;
                var top = new Vector3(Mathf.Cos(i * step) * RingRadius, StringY,
                                      Mathf.Sin(i * step) * RingRadius);
                var nextTop = new Vector3(Mathf.Cos((i + 1) * step) * RingRadius, StringY,
                                          Mathf.Sin((i + 1) * step) * RingRadius);

                wood.Add((Pole(), Matrix4x4.Translate(
                    new Vector3(top.x, PoleHeight * 0.5f, top.z))));

                HangSpan(wood, lanterns, flags, flagMould, top, nextTop, i);
            }

            Place(root, "mástiles_y_cuerdas", MeshShapes.Combine(wood, "fiesta_madera"),
                  ToonPalette.Solid(new Color32(0xA9, 0x86, 0x63, 255)), created);
            Place(root, "farolillos", MeshShapes.Combine(lanterns, "fiesta_farolillos"),
                  ToonPalette.Solid(new Color32(0xF2, 0xC9, 0x6B, 255)), created);

            for (int colour = 0; colour < flags.Length; colour++)
            {
                if (flags[colour].Count == 0) continue;
                Place(root, $"banderines_{colour}",
                      MeshShapes.Combine(flags[colour], $"fiesta_banderines_{colour}"),
                      ToonPalette.Solid(ToonPalette.Flowers[colour]), created);
            }

            // Cuatro macetas entre los mástiles, con la malla del catálogo: el anillo
            // vacío se lee desde la cámara alta como un corral; con algo a ras de
            // suelo dentro, se lee como un sitio donde la gente se para.
            for (int i = 0; i < 4; i++)
            {
                float angle = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                var pot = new GameObject($"maceta_{i}");
                pot.transform.SetParent(root, worldPositionStays: false);
                pot.transform.localPosition =
                    new Vector3(Mathf.Cos(angle) * (RingRadius - 2.5f), 0f,
                                Mathf.Sin(angle) * (RingRadius - 2.5f));

                AddMesh(pot, DecorMeshBuilder.For(DecorKind.Plant),
                        ToonPalette.Solid(DecorMeshBuilder.ColorOf($"fiesta_maceta_{i}",
                                                                  DecorKind.Plant)));
            }

            // El molde ya está copiado dentro de los combinados de banderines. Mismo
            // cuidado que DecorMeshBuilder.Clear: Destroy no hace nada fuera de modo
            // juego y encima suelta un error, y este builder también lo llama el
            // editor desde sus pruebas.
            if (Application.isPlaying) Object.Destroy(flagMould);
            else Object.DestroyImmediate(flagMould);
        }

        /// <summary>Tiende la cuerda de un mástil al siguiente y la llena.</summary>
        private static void HangSpan(List<(Mesh, Matrix4x4)> wood,
                                     List<(Mesh, Matrix4x4)> lanterns,
                                     List<(Mesh, Matrix4x4)>[] flags, Mesh flagMould,
                                     Vector3 from, Vector3 to, int span)
        {
            // Parábola en vez de catenaria: a esta escala no se distinguen y así la
            // altura de cualquier punto sale de una cuenta, sin tablas.
            Vector3 Point(float t)
            {
                return Vector3.Lerp(from, to, t) -
                       Vector3.up * (Sag * 4f * t * (1f - t));
            }

            // La cuerda va en cuatro tramos rectos: una sola caja inclinada marcaría
            // las puntas en el aire, lejos de donde los mástiles dicen que debería
            // estar.
            for (int k = 0; k < 4; k++)
            {
                var a = Point(k / 4f);
                var b = Point((k + 1) / 4f);
                var span3 = b - a;
                wood.Add((MeshShapes.Box(new Vector3(0.07f, 0.07f, span3.magnitude + 0.06f)),
                          Matrix4x4.TRS((a + b) * 0.5f,
                                        Quaternion.LookRotation(span3.normalized),
                                        Vector3.one)));
            }

            for (int f = 0; f < FlagsPerSpan; f++)
            {
                float t = (f + 0.5f) / FlagsPerSpan;
                var at = Point(t);
                var dir = (to - from).normalized;

                // El banderín cuelga plano siguiendo la cuerda: su normal horizontal,
                // perpendicular al tramo, para que desde la cámara se vea cara y no canto.
                var facing = Quaternion.LookRotation(new Vector3(-dir.z, 0f, dir.x));

                flags[(span + f) % flags.Length].Add(
                    (flagMould, Matrix4x4.TRS(at, facing, Vector3.one)));
            }

            // Un farolillo a media cuerda siempre, y en los tramos alternos otro a un
            // tercio o a dos tercios: desigual a propósito, igual que se tienden.
            AddLantern(lanterns, wood, Point(0.5f));
            float extra = span % 2 == 0 ? 0.25f : 0.75f;
            AddLantern(lanterns, wood, Point(extra));
        }

        private static void AddLantern(List<(Mesh, Matrix4x4)> lanterns,
                                       List<(Mesh, Matrix4x4)> wood, Vector3 at)
        {
            // Bracito corto del que cuelga, para que no flote pegado a la cuerda.
            wood.Add((MeshShapes.Box(new Vector3(0.03f, 0.32f, 0.03f)),
                      Matrix4x4.Translate(at + Vector3.down * 0.16f)));

            lanterns.Add((MeshShapes.Sphere(8, 6, new Vector3(0.34f, 0.40f, 0.34f)),
                          Matrix4x4.Translate(at + Vector3.down * 0.48f)));
        }

        /// <summary>El banderín: un triángulo que cuelga, visible por las dos caras.
        /// </summary>
        /// <remarks>
        /// Dos triángulos con el devanado opuesto y normales escritas a mano, y no un
        /// material de doble cara: el material de la isla es opaco de una cara, y un
        /// banderín que desaparece según desde dónde andes se lee como parpadeo.
        /// </remarks>
        private static Mesh FlagMould()
        {
            var mesh = new Mesh { name = "fiesta_banderin" };

            mesh.vertices = new[]
            {
                new Vector3(-0.30f, 0f, 0f), new Vector3(0.30f, 0f, 0f),
                new Vector3(0f, -0.62f, 0f),
                new Vector3(-0.30f, 0f, 0f), new Vector3(0.30f, 0f, 0f),
                new Vector3(0f, -0.62f, 0f),
            };
            mesh.normals = new[]
            {
                new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f),
            };
            mesh.triangles = new[] { 0, 1, 2, 5, 4, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>El mástil: un palo con base algo más ancha, clavado en el suelo.
        /// </summary>
        private static Mesh Pole() =>
            MeshShapes.Cylinder(6, 0.06f, 0.09f, PoleHeight);

        private static void Place(Transform root, string name, Mesh mesh, Material material,
                                  List<Mesh> created)
        {
            created.Add(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(root, worldPositionStays: false);
            AddMesh(go, mesh, material);
        }

        /// <summary>Sin colisionador, como todos los adornos: rodear un mástil fino con
        /// el cuerpo del vecino es trabajo de navegación que nadie pidió.</summary>
        private static void AddMesh(GameObject go, Mesh mesh, Material material)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }
    }
}
