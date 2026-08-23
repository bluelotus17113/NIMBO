using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Da cuerpo a los adornos de la isla: un banco, una farola, una estatua.
    /// </summary>
    /// <remarks>
    /// Una malla por tipo, no una por adorno del catálogo. Son treinta y dos fichas
    /// y modelar treinta y dos cosas a mano sería un mes de trabajo para que el
    /// jugador vea, desde la cámara de la isla, siete siluetas distintas de todas
    /// formas. Lo que sí cambia por adorno es el color, que sale de su propio
    /// identificador: dos bancos distintos se distinguen, y siempre del mismo modo.
    ///
    /// Las mallas se comparten entre todas las piezas del mismo tipo. Con veinte
    /// adornos por zona y diez zonas eso son doscientas piezas: generar una malla
    /// por pieza llenaría la memoria de copias idénticas para nada.
    /// </remarks>
    public static class DecorMeshBuilder
    {
        private static readonly Dictionary<DecorKind, Mesh> Cache = new();

        /// <summary>La malla de ese tipo de adorno, ya centrada sobre el suelo.</summary>
        public static Mesh For(DecorKind kind)
        {
            if (Cache.TryGetValue(kind, out var cached) && cached != null) return cached;

            var mesh = Build(kind);
            Cache[kind] = mesh;
            return mesh;
        }

        /// <summary>
        /// A qué tipo de adorno pertenece una malla, si es que pertenece a alguno.
        /// </summary>
        /// <remarks>
        /// La mirilla inversa del catálogo. La usa el barrido de adornos de la cámara
        /// (<c>IslandCamera</c>) para reconocer los cuerpos colocados sin depender de
        /// nombres de objeto: la igualdad es de referencia contra esta misma caché, así
        /// que una malla nueva con forma de adorno no cuela y un adorno renombrado no
        /// se pierde.
        /// </remarks>
        public static bool TryKindOf(Mesh mesh, out DecorKind kind)
        {
            foreach (var pair in Cache)
            {
                if (pair.Value != mesh) continue;
                kind = pair.Key;
                return true;
            }

            kind = default;
            return false;
        }

        /// <summary>
        /// Las esferas con las que la cámara rodea ese tipo de adorno, en coordenadas
        /// locales (el origen es la hierba, como en las mallas).
        /// </summary>
        /// <remarks>
        /// Solo lo que sobresale del recorrido pivote→cámara. Ese segmento nunca baja
        /// de ~1,5 m —el pivote va a 1,2 m (la cabeza del protagonista) y la pose más
        /// baja que permiten los topes del rig (3,5 m a 5°) deja la cámara a 1,5—, así
        /// que asiento (1,30 m de alto), planta (1,42) y valla (1,00) devuelven falso:
        /// registrarlas sería una entrada más que mirar para no apartar nunca la
        /// cámara.
        ///
        /// Las que sí van llevan sus esferas escritas a mano y no salen del bounds:
        /// farola y estatua son un palo fino con una bola arriba, y su bounds pide una
        /// esfera de radio medio metro centrada a media altura que ni toca la bola —
        /// justo la parte en la que la cámara se mete al pasar pegado.
        /// </remarks>
        public static bool TryCameraSpheres(DecorKind kind,
                                            out (Vector3 centre, float radius)[] spheres)
        {
            switch (kind)
            {
                case DecorKind.Light:
                    spheres = new[]
                    {
                        (new Vector3(0f, 1.65f, 0f), 0.30f),   // el mástil (0,12 de radio)
                        (new Vector3(0f, 3.35f, 0f), 0.34f),   // la bola de la lámpara
                    };
                    return true;

                case DecorKind.Statue:
                    spheres = new[]
                    {
                        (new Vector3(0f, 1.65f, 0f), 0.55f),   // la columna (0,42 abajo)
                        (new Vector3(0f, 2.72f, 0f), 0.52f),   // el remate
                    };
                    return true;

                case DecorKind.Sign:
                    spheres = new[]
                    {
                        (new Vector3(0f, 1.75f, 0f), 0.75f),   // la tabla
                        (new Vector3(0f, 0.75f, 0f), 0.20f),   // el poste
                    };
                    return true;

                case DecorKind.Water:
                    // Fuente: columna y plato alto; la taza ancha queda por debajo del
                    // recorrido y no hace falta.
                    spheres = new[] { (new Vector3(0f, 1.10f, 0f), 0.85f) };
                    return true;

                default:
                    spheres = null;
                    return false;
            }
        }

        /// <summary>
        /// El color de un adorno concreto. Sale de su identificador, así que el
        /// mismo banco es del mismo color en todas las partidas y en todas las islas.
        /// </summary>
        public static Color ColorOf(string catalogId, DecorKind kind)
        {
            var baseColour = BaseColour(kind);

            // Un desvío pequeño de tono y claridad: lo justo para distinguir dos
            // piezas del mismo tipo sin que ninguna se salga de la paleta cálida
            // que tiene el resto de la isla.
            Color.RGBToHSV(baseColour, out float h, out float s, out float v);

            uint hash = 2166136261u;
            for (int i = 0; i < catalogId.Length; i++)
            {
                hash ^= catalogId[i];
                hash *= 16777619u;
            }

            h = Mathf.Repeat(h + ((hash & 0xFF) / 255f - 0.5f) * 0.09f, 1f);
            v = Mathf.Clamp(v + (((hash >> 8) & 0xFF) / 255f - 0.5f) * 0.18f, 0.28f, 0.97f);

            return Color.HSVToRGB(h, s, v);
        }

        private static Color BaseColour(DecorKind kind) => kind switch
        {
            DecorKind.Seat => ToonPalette.TrunkBrown,
            DecorKind.Light => new Color32(0xF2, 0xC9, 0x6B, 255),
            DecorKind.Statue => new Color32(0xCB, 0xC3, 0xB6, 255),
            DecorKind.Plant => ToonPalette.LeafGreen,
            DecorKind.Sign => new Color32(0xD8, 0xA9, 0x76, 255),
            DecorKind.Fence => new Color32(0xA9, 0x86, 0x63, 255),
            _ => ToonPalette.Water,
        };

        private static Mesh Build(DecorKind kind) => kind switch
        {
            DecorKind.Seat => Seat(),
            DecorKind.Light => Light(),
            DecorKind.Statue => Statue(),
            DecorKind.Plant => Plant(),
            DecorKind.Sign => Sign(),
            DecorKind.Fence => Fence(),
            _ => Fountain(),
        };

        // ── Las siete siluetas ───────────────────────────────────────────────
        //
        // Todas se construyen con el origen en el suelo (Y = 0 es la hierba), para
        // que colocarlas sea poner la posición de la zona y nada más. Si alguna
        // tuviera el origen en su centro, se hundiría hasta la cintura.

        private static Mesh Seat()
        {
            var parts = new List<(Mesh, Matrix4x4)>
            {
                (MeshShapes.Box(new Vector3(2.2f, 0.18f, 0.7f)), Move(0f, 0.55f, 0f)),
                (MeshShapes.Box(new Vector3(2.2f, 0.7f, 0.16f)), Move(0f, 0.95f, -0.3f)),
                (MeshShapes.Box(new Vector3(0.16f, 0.55f, 0.6f)), Move(-0.9f, 0.28f, 0f)),
                (MeshShapes.Box(new Vector3(0.16f, 0.55f, 0.6f)), Move(0.9f, 0.28f, 0f)),
            };
            return MeshShapes.Combine(parts, "adorno_asiento");
        }

        private static Mesh Light()
        {
            var parts = new List<(Mesh, Matrix4x4)>
            {
                (MeshShapes.Cylinder(10, 0.28f, 0.36f, 0.22f), Move(0f, 0.11f, 0f)),
                (MeshShapes.Cylinder(8, 0.09f, 0.12f, 3.1f), Move(0f, 1.65f, 0f)),
                (MeshShapes.Sphere(10, 8, new Vector3(0.42f, 0.5f, 0.42f)), Move(0f, 3.35f, 0f)),
            };
            return MeshShapes.Combine(parts, "adorno_farola");
        }

        private static Mesh Statue()
        {
            var parts = new List<(Mesh, Matrix4x4)>
            {
                (MeshShapes.Box(new Vector3(1.5f, 0.5f, 1.5f)), Move(0f, 0.25f, 0f)),
                (MeshShapes.Box(new Vector3(1.1f, 0.3f, 1.1f)), Move(0f, 0.65f, 0f)),
                (MeshShapes.Cylinder(10, 0.3f, 0.42f, 1.7f), Move(0f, 1.65f, 0f)),
                (MeshShapes.Sphere(12, 10, new Vector3(0.44f, 0.5f, 0.44f)), Move(0f, 2.72f, 0f)),
            };
            return MeshShapes.Combine(parts, "adorno_estatua");
        }

        private static Mesh Plant()
        {
            // Tres bolas desiguales: un arbusto simétrico se lee como una pelota,
            // y con la cámara alta lo que se ve de una planta es justo su silueta.
            var parts = new List<(Mesh, Matrix4x4)>
            {
                (MeshShapes.Cylinder(8, 0.12f, 0.16f, 0.5f), Move(0f, 0.25f, 0f)),
                (MeshShapes.Sphere(12, 9, new Vector3(0.85f, 0.7f, 0.85f)), Move(0f, 0.85f, 0f)),
                (MeshShapes.Sphere(10, 8, new Vector3(0.55f, 0.48f, 0.55f)), Move(0.38f, 1.18f, 0.16f)),
                (MeshShapes.Sphere(10, 8, new Vector3(0.48f, 0.42f, 0.48f)), Move(-0.34f, 1.1f, -0.2f)),
            };
            return MeshShapes.Combine(parts, "adorno_planta");
        }

        private static Mesh Sign()
        {
            var parts = new List<(Mesh, Matrix4x4)>
            {
                (MeshShapes.Cylinder(8, 0.08f, 0.1f, 1.5f), Move(0f, 0.75f, 0f)),
                (MeshShapes.Box(new Vector3(1.4f, 0.9f, 0.12f)), Move(0f, 1.75f, 0f)),
            };
            return MeshShapes.Combine(parts, "adorno_cartel");
        }

        private static Mesh Fence()
        {
            var parts = new List<(Mesh, Matrix4x4)>();

            // Cuatro postes y dos travesaños. Las vallas se ponen en fila, así que
            // el tramo mide justo dos metros y encaja consigo mismo.
            for (int i = 0; i < 4; i++)
            {
                float x = -0.9f + i * 0.6f;
                parts.Add((MeshShapes.Box(new Vector3(0.12f, 1f, 0.12f)), Move(x, 0.5f, 0f)));
            }
            parts.Add((MeshShapes.Box(new Vector3(2f, 0.1f, 0.08f)), Move(0f, 0.85f, 0f)));
            parts.Add((MeshShapes.Box(new Vector3(2f, 0.1f, 0.08f)), Move(0f, 0.45f, 0f)));

            return MeshShapes.Combine(parts, "adorno_valla");
        }

        private static Mesh Fountain()
        {
            var parts = new List<(Mesh, Matrix4x4)>
            {
                (MeshShapes.Cylinder(18, 1.75f, 1.75f, 0.45f), Move(0f, 0.22f, 0f)),
                (MeshShapes.Cylinder(18, 1.5f, 1.5f, 0.12f), Move(0f, 0.5f, 0f)),
                (MeshShapes.Cylinder(10, 0.2f, 0.3f, 1.1f), Move(0f, 1f, 0f)),
                (MeshShapes.Cylinder(12, 0.75f, 0.5f, 0.16f), Move(0f, 1.6f, 0f)),
            };
            return MeshShapes.Combine(parts, "adorno_fuente");
        }

        private static Matrix4x4 Move(float x, float y, float z) =>
            Matrix4x4.Translate(new Vector3(x, y, z));

        /// <summary>Suelta las mallas guardadas. La llama el mundo al descargarse.</summary>
        public static void Clear()
        {
            foreach (var mesh in Cache.Values)
            {
                if (mesh == null) continue;

                // Destroy no hace nada fuera de modo juego y encima suelta un error:
                // en el editor y en las pruebas tiene que ser DestroyImmediate.
                if (Application.isPlaying) Object.Destroy(mesh);
                else Object.DestroyImmediate(mesh);
            }
            Cache.Clear();
        }
    }
}
