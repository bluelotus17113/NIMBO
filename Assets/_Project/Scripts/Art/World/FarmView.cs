using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Farming;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Dibuja el huerto: la tierra y lo que crece en ella.
    /// </summary>
    /// <remarks>
    /// Una casilla es una losa plana más, si hay algo sembrado, una planta encima.
    /// Cada cultivo del catálogo tiene su propia silueta y su propio color de fruto:
    /// un huerto en el que todo es el mismo cilindro verde no se lee a tres metros,
    /// y mirar la parcela y saber qué hay plantado sin acercarse es media mitad del
    /// placer de tenerla. La silueta va antes que el color porque el color se lo
    /// lleva la luz y la forma no.
    ///
    /// La planta entera crece: un brote es la versión en pequeño de lo que será, no
    /// un tallo genérico que luego se sustituye. Así la siembra ya avisa de qué hay
    /// en la casilla, y lo que cambia al madurar es solo el fruto.
    ///
    /// Solo se rehace la casilla que cambia. Reconstruir las cuarenta y ocho cada vez
    /// que se riega una sería tirar cuarenta y siete mallas para nada.
    /// </remarks>
    public sealed class FarmView : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> _tiles = new();

        private IFarmingService _farm;
        private Transform _root;

        private static Mesh _slab;

        // Las mallas de cada cultivo, construidas una vez y compartidas por las
        // cuarenta y ocho casillas. Estáticas como _slab: generan una por partida,
        // no una por casilla ni una por FarmView.
        private static readonly Dictionary<string, CropLook> Looks = new();

        private void OnEnable()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<TileChanged>(OnTileChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<TileChanged>(OnTileChanged);
        }

        private void OnGameLoaded(GameLoaded _)
        {
            if (!ServiceRegistry.TryGet(out _farm)) return;

            _root = new GameObject("Huerto").transform;
            _root.SetParent(transform, worldPositionStays: false);

            BuildFence();

            for (int y = 0; y < _farm.Height; y++)
                for (int x = 0; x < _farm.Width; x++)
                    Rebuild(x, y);
        }

        private void OnTileChanged(TileChanged evt) => Rebuild(evt.X, evt.Y);

        private static int Key(int x, int y) => y * 100 + x;

        private void Rebuild(int x, int y)
        {
            if (_farm == null || _root == null) return;

            int key = Key(x, y);
            if (_tiles.TryGetValue(key, out var old) && old != null) Destroy(old);

            var tile = _farm.TileAt(x, y);
            if (tile == null) return;

            FarmPlot.CentreOf(x, y, _farm.Width, _farm.Height, out float wx, out float wz);

            var go = new GameObject($"casilla_{x}_{y}");
            go.transform.SetParent(_root, worldPositionStays: false);
            go.transform.localPosition = new Vector3(wx, 0.06f, wz);

            AddPart(go.transform, "tierra", Slab(), ToonPalette.Solid(SoilColour(tile)));

            if (tile.State == TileState.Planted || tile.State == TileState.Ready)
                AddPlant(go.transform, tile);

            _tiles[key] = go;
        }

        /// <summary>
        /// El color de la tierra dice en qué estado está sin necesidad de ningún icono.
        /// La regada es más oscura, que es lo que hace la tierra mojada de verdad.
        /// </summary>
        private static Color SoilColour(FarmTile tile)
        {
            if (tile.State == TileState.Wild) return ToonPalette.Grass;

            var dry = new Color32(0x8A, 0x6A, 0x4C, 255);
            var wet = new Color32(0x5C, 0x42, 0x2E, 255);
            return tile.Watered ? wet : dry;
        }

        private void AddPlant(Transform parent, FarmTile tile)
        {
            // Cuánto ha crecido, de 0 a 1. Sin la ficha del cultivo se asume a medias:
            // vale más una planta un poco equivocada que un hueco sin nada.
            float grown = 0.5f;
            if (_farm.TryGetCrop(tile.SeedId, out var crop) && crop.DaysToGrow > 0)
                grown = Mathf.Clamp01(tile.GrowthDays / (float)crop.DaysToGrow);

            bool ready = tile.State == TileState.Ready;

            // Escala uniforme y no solo vertical: un brote proporcional se lee como
            // «la misma planta, más pequeña», mientras que un tallo que solo estira
            // en alto se lee como una púa.
            var plant = new GameObject("planta");
            plant.transform.SetParent(parent, worldPositionStays: false);
            plant.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, ready ? 1f : grown);

            var look = LookFor(tile.SeedId);
            var parts = ready ? look.Ready : look.Growing;
            for (int i = 0; i < parts.Length; i++)
                AddPart(plant.transform, parts[i]);
        }

        /// <summary>
        /// El aspecto de ese cultivo, construido una vez y reutilizado por todas las
        /// casillas. Los ids van escritos y son los del catálogo
        /// (<c>Resources/Config/catalogo_cultivos.json</c>): cualquier id sin forma
        /// asignada —una partida vieja, un cultivo futuro— cae en la planta genérica
        /// en vez de en un hueco o una excepción.
        /// </summary>
        private static CropLook LookFor(string seedId)
        {
            if (Looks.TryGetValue(seedId, out var existing)) return existing;

            var look = seedId switch
            {
                "seed_nimbocereza" => Nimbocereza(),
                "seed_alasauce" => Alasauce(),
                "seed_cristalovento" => CristalDeViento(),
                "seed_cetrella" => Cetrella(),
                "seed_nimbocalabaza" => Nimbocalabaza(),
                "seed_musgolante" => Musgolante(),
                "seed_raisnube" => Raiznube(),
                "seed_azurflor" => Azurflor(),
                "seed_trigoeter" => TrigoEter(),
                "seed_sombraliquida" => SombraLiquida(),
                "seed_esporaalba" => EsporaAlba(),
                "seed_vidrioestelar" => VidrioEstelar(),
                _ => Generica(),
            };
            Looks[seedId] = look;
            return look;
        }

        // ── los doce ─────────────────────────────────────────────────────────

        /// <summary>Cerezo enano: tronco, copa y, al madurar, cerezas rosadas colgadas.</summary>
        private static CropLook Nimbocereza()
        {
            var tronco = new Part("tronco",
                MeshShapes.Cylinder(6, 0.05f, 0.09f, 0.55f),
                ToonPalette.Solid(ToonPalette.TrunkBrown),
                new Vector3(0f, 0.27f, 0f), Quaternion.identity, Vector3.one);

            var copa = new Part("copa",
                FoliageMeshBuilder.Weave(FoliageMeshBuilder.CrownLobes(0.30f, 101u, 5),
                                         new Vector3(0f, -0.06f, 0f), 102u, "copa_cereza",
                                         segments: 12, rings: 8),
                ToonPalette.Foliage(ToonPalette.LeafGreen),
                new Vector3(0f, 0.66f, 0f), Quaternion.identity, Vector3.one);

            // «Del color del cielo al atardecer»: rosa profundo. Antes todos los
            // frutos eran el mismo salmón; este se distingue de él y del naranja
            // de la calabaza y de la vaina del alasauce.
            var cerezas = new Part("cerezas",
                Merge("cerezas_cereza",
                    (MeshShapes.Sphere(8, 6), new Vector3(0.17f, 0.52f, 0.10f),
                     Quaternion.identity, Vector3.one * 0.14f),
                    (MeshShapes.Sphere(8, 6), new Vector3(-0.15f, 0.45f, -0.13f),
                     Quaternion.identity, Vector3.one * 0.13f),
                    (MeshShapes.Sphere(8, 6), new Vector3(0.03f, 0.38f, 0.19f),
                     Quaternion.identity, Vector3.one * 0.12f)),
                ToonPalette.Solid(new Color32(0xC9, 0x50, 0x6B, 255)));

            return new CropLook(new[] { tronco, copa }, new[] { tronco, copa, cerezas });
        }

        /// <summary>Mata alta con hojas en forma de ala y, al madurar, vainas picantes.</summary>
        private static CropLook Alasauce()
        {
            var tallo = new Part("tallo",
                MeshShapes.Cylinder(6, 0.03f, 0.05f, 0.72f),
                ToonPalette.Solid(new Color32(0x56, 0x90, 0x4A, 255)),
                new Vector3(0f, 0.36f, 0f), Quaternion.identity, Vector3.one);

            // Cuatro alas escalonadas, dos de frente y dos de perfil: de un lado es
            // una flecha y desde el otro una escalera. Ningún otro cultivo del
            // catálogo se le parece ni por error.
            var alas = new Part("alas",
                Merge("alas_alasauce",
                    (MeshShapes.Capsule(8, 4), new Vector3(0.17f, 0.40f, 0f),
                     Rot(0f, 0f, -38f), new Vector3(0.30f, 0.44f, 0.07f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(-0.17f, 0.48f, 0f),
                     Rot(0f, 0f, 38f), new Vector3(0.28f, 0.40f, 0.07f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(0f, 0.56f, 0.17f),
                     Rot(0f, 90f, -36f), new Vector3(0.26f, 0.38f, 0.07f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(0f, 0.64f, -0.17f),
                     Rot(0f, 90f, 36f), new Vector3(0.24f, 0.34f, 0.07f))),
                ToonPalette.Solid(new Color32(0x6F, 0xB2, 0x5A, 255)));

            var vainas = new Part("vainas",
                Merge("vainas_alasauce",
                    (MeshShapes.Capsule(8, 4), new Vector3(0.13f, 0.50f, 0f),
                     Rot(0f, 0f, -24f), new Vector3(0.09f, 0.30f, 0.09f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(-0.13f, 0.44f, 0f),
                     Rot(0f, 0f, 24f), new Vector3(0.08f, 0.26f, 0.08f))),
                ToonPalette.Solid(new Color32(0xC7, 0x50, 0x2E, 255)));

            return new CropLook(new[] { tallo, alas }, new[] { tallo, alas, vainas });
        }

        /// <summary>Prismas de cristal; al madurar, una esquirla más alta y más viva.</summary>
        private static CropLook CristalDeViento()
        {
            // Piedra y no sólido: la rampa dura dibuja las aristas del prisma, que
            // es justo lo que hace que se lea cristal y no estalagmita de cera.
            var prismas = new Part("prismas",
                Merge("prismas_cristal",
                    (MeshShapes.Cylinder(5, 0.02f, 0.13f, 0.78f), new Vector3(0f, 0.39f, 0f),
                     Rot(6f, 0f, 4f), Vector3.one),
                    (MeshShapes.Cylinder(5, 0.02f, 0.09f, 0.50f), new Vector3(0.18f, 0.25f, 0.08f),
                     Rot(14f, 0f, -18f), Vector3.one),
                    (MeshShapes.Cylinder(5, 0.02f, 0.08f, 0.42f), new Vector3(-0.17f, 0.21f, -0.06f),
                     Rot(-10f, 0f, 20f), Vector3.one),
                    (MeshShapes.Cylinder(5, 0.02f, 0.06f, 0.30f), new Vector3(0.02f, 0.15f, -0.20f),
                     Rot(-18f, 0f, 0f), Vector3.one)),
                ToonPalette.Stone(new Color32(0xA9, 0xDC, 0xEC, 255)));

            var esquirla = new Part("esquirla",
                MeshShapes.Cylinder(5, 0.01f, 0.07f, 0.34f),
                ToonPalette.Solid(new Color32(0x7F, 0xD8, 0xF0, 255), 0.4f),
                new Vector3(0.05f, 0.88f, 0.03f), Rot(22f, 0f, -14f), Vector3.one);

            return new CropLook(new[] { prismas }, new[] { prismas, esquirla });
        }

        /// <summary>Una seta azul grande; al madurar, luz propia bajo el sombrero.</summary>
        private static CropLook Cetrella()
        {
            var pie = new Part("pie",
                MeshShapes.Cylinder(7, 0.085f, 0.115f, 0.34f),
                ToonPalette.Solid(new Color32(0xBF, 0xE0, 0xEE, 255)),
                new Vector3(0f, 0.17f, 0f), Quaternion.identity, Vector3.one);

            var sombrero = new Part("sombrero",
                MeshShapes.Sphere(10, 7, new Vector3(0.52f, 0.30f, 0.52f)),
                ToonPalette.Solid(new Color32(0x4F, 0x86, 0xC8, 255)),
                new Vector3(0f, 0.37f, 0f), Quaternion.identity, Vector3.one);

            var luz = new Part("luz",
                MeshShapes.Sphere(8, 6, Vector3.one * 0.13f),
                ToonPalette.Solid(new Color32(0x8F, 0xF0, 0xFF, 255), 0.5f),
                new Vector3(0f, 0.22f, 0f), Quaternion.identity, Vector3.one);

            return new CropLook(new[] { pie, sombrero }, new[] { pie, sombrero, luz });
        }

        /// <summary>
        /// Calabaza flotante. Aquí la planta ES el fruto, así que lo que avisa de la
        /// cosecha es el color: verde mientras crece, naranja cuando pesa.
        /// </summary>
        private static CropLook Nimbocalabaza()
        {
            Part[] De(Color32 piel, Color32 rabito)
            {
                // Flota unos centímetros: el hueco bajo el cuerpo la separa de toda
                // raíz y de todo bulbo del resto del huerto.
                var cuerpo = new Part("calabaza",
                    MeshShapes.Sphere(11, 8, new Vector3(0.66f, 0.46f, 0.66f)),
                    ToonPalette.Solid(piel),
                    new Vector3(0f, 0.30f, 0f), Quaternion.identity, Vector3.one);
                var rabo = new Part("rabito",
                    MeshShapes.Cylinder(5, 0.035f, 0.05f, 0.09f),
                    ToonPalette.Solid(rabito),
                    new Vector3(0f, 0.55f, 0f), Rot(10f, 0f, 6f), Vector3.one);
                return new[] { cuerpo, rabo };
            }

            return new CropLook(
                De(new Color32(0x7F, 0xA2, 0x4A, 255), new Color32(0x5E, 0x7A, 0x38, 255)),
                De(new Color32(0xD9, 0x82, 0x2E, 255), new Color32(0x6B, 0x5A, 0x43, 255)));
        }

        /// <summary>Cojín dorado y bajo; al madurar, borlas más claras encima.</summary>
        private static CropLook Musgolante()
        {
            var lobes = new List<FoliageMeshBuilder.Lobe>
            {
                new(new Vector3(0f, 0.05f, 0f), new Vector3(0.30f, 0.09f, 0.30f)),
                new(new Vector3(0.20f, 0.04f, 0.10f), new Vector3(0.16f, 0.07f, 0.16f)),
                new(new Vector3(-0.19f, 0.04f, -0.08f), new Vector3(0.15f, 0.06f, 0.15f)),
                new(new Vector3(0.05f, 0.03f, -0.20f), new Vector3(0.13f, 0.06f, 0.13f)),
                new(new Vector3(-0.08f, 0.03f, 0.19f), new Vector3(0.12f, 0.06f, 0.12f)),
            };

            var cojin = new Part("cojin",
                FoliageMeshBuilder.Weave(lobes, new Vector3(0f, -0.12f, 0f), 201u,
                                         "musgo", segments: 12, rings: 8),
                ToonPalette.Foliage(new Color32(0xC9, 0xA8, 0x3C, 255)));

            var borlas = new Part("borlas",
                Merge("borlas_musgo",
                    (MeshShapes.Sphere(7, 5), new Vector3(0.14f, 0.15f, 0.06f),
                     Quaternion.identity, Vector3.one * 0.10f),
                    (MeshShapes.Sphere(7, 5), new Vector3(-0.12f, 0.14f, -0.10f),
                     Quaternion.identity, Vector3.one * 0.09f),
                    (MeshShapes.Sphere(7, 5), new Vector3(0.00f, 0.13f, 0.18f),
                     Quaternion.identity, Vector3.one * 0.08f)),
                ToonPalette.Solid(new Color32(0xF2, 0xCC, 0x55, 255)));

            return new CropLook(new[] { cojin }, new[] { cojin, borlas });
        }

        /// <summary>Raíz que asoma con penacho de briznas; al madurar, una bola de nube.</summary>
        private static CropLook Raiznube()
        {
            var raiz = new Part("raiz",
                MeshShapes.Cylinder(7, 0.16f, 0.045f, 0.26f),
                ToonPalette.Solid(new Color32(0xE3, 0xD2, 0xA0, 255)),
                new Vector3(0f, 0.11f, 0f), Quaternion.identity, Vector3.one);

            var penacho = new Part("penacho",
                Blades(301u, 1, 0.02f, 0.85f, "penacho_raiznube"),
                ToonPalette.Foliage(new Color32(0xB9, 0xD4, 0xBC, 255)),
                new Vector3(0f, 0.20f, 0f), Quaternion.identity, Vector3.one);

            var nube = new Part("nube",
                MeshShapes.Sphere(9, 7, new Vector3(0.30f, 0.20f, 0.30f)),
                ToonPalette.Solid(ToonPalette.CloudWhite),
                new Vector3(0f, 0.82f, 0f), Quaternion.identity, Vector3.one);

            return new CropLook(new[] { raiz, penacho }, new[] { raiz, penacho, nube });
        }

        /// <summary>Capullo cerrado mientras crece; al madurar, roseta de seis pétalos.</summary>
        private static CropLook Azurflor()
        {
            var tallo = new Part("tallo",
                Merge("tallo_azurflor",
                    (MeshShapes.Cylinder(6, 0.025f, 0.04f, 0.58f), new Vector3(0f, 0.29f, 0f),
                     Quaternion.identity, Vector3.one),
                    (MeshShapes.Capsule(8, 4), new Vector3(0.10f, 0.12f, 0f),
                     Rot(0f, 0f, -30f), new Vector3(0.20f, 0.10f, 0.05f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(-0.10f, 0.18f, 0f),
                     Rot(0f, 0f, 30f), new Vector3(0.18f, 0.09f, 0.05f))),
                ToonPalette.Solid(new Color32(0x58, 0x96, 0x5A, 255)));

            // Cerrada mientras crece: un capullo no dice todavía qué flor es, pero sí
            // que ahí hay algo distinto de una brizna.
            var zafiro = ToonPalette.Solid(new Color32(0x3E, 0x5F, 0xD8, 255));
            var capullo = new Part("capullo",
                MeshShapes.Sphere(8, 6, new Vector3(0.16f, 0.26f, 0.16f)),
                zafiro, new Vector3(0f, 0.68f, 0f), Quaternion.identity, Vector3.one);

            var corazon = new Part("corazon",
                MeshShapes.Sphere(8, 6, new Vector3(0.10f, 0.08f, 0.10f)),
                ToonPalette.Solid(new Color32(0xF0, 0xC8, 0x4C, 255)),
                new Vector3(0f, 0.62f, 0f), Quaternion.identity, Vector3.one);

            // Seis pétalos tumbados alrededor del corazón: la misma roseta que las
            // flores del prado, pero en alto y grande para distinguirse de ellas.
            var petalos = new List<(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale)>(6);
            for (int k = 0; k < 6; k++)
            {
                float ang = k * 60f * Mathf.Deg2Rad;
                petalos.Add((MeshShapes.Capsule(8, 4),
                    new Vector3(Mathf.Cos(ang) * 0.14f, 0.62f, Mathf.Sin(ang) * 0.14f),
                    Rot(0f, -k * 60f, 90f),
                    new Vector3(0.055f, 0.17f, 0.025f)));
            }
            var flor = new Part("petalos", Merge("petalos_azurflor", petalos.ToArray()), zafiro);

            return new CropLook(new[] { tallo, capullo }, new[] { tallo, corazon, flor });
        }

        /// <summary>Espigas altas y pálidas; al madurar, granos dorados en la punta.</summary>
        private static CropLook TrigoEter()
        {
            var canas = new Part("canas",
                Blades(401u, 2, 0.14f, 1.30f, "canas_trigo"),
                ToonPalette.Foliage(new Color32(0xC9, 0xC4, 0x8E, 255)));

            var espigas = new Part("espigas",
                Merge("espigas_trigo",
                    (MeshShapes.Capsule(8, 4), new Vector3(0f, 0.92f, 0f),
                     Rot(8f, 0f, 6f), new Vector3(0.06f, 0.20f, 0.06f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(0.08f, 0.84f, 0.05f),
                     Rot(-6f, 0f, -14f), new Vector3(0.05f, 0.17f, 0.05f)),
                    (MeshShapes.Capsule(8, 4), new Vector3(-0.07f, 0.82f, -0.06f),
                     Rot(4f, 0f, 16f), new Vector3(0.05f, 0.16f, 0.05f))),
                ToonPalette.Solid(new Color32(0xE8, 0xC8, 0x78, 255)));

            return new CropLook(new[] { canas }, new[] { canas, espigas });
        }

        /// <summary>Mata oscura con un lóbulo que gotea; al madurar, orbe de tinta brillante.</summary>
        private static CropLook SombraLiquida()
        {
            var lobes = new List<FoliageMeshBuilder.Lobe>
            {
                new(new Vector3(0f, 0.16f, 0f), new Vector3(0.26f, 0.18f, 0.26f)),
                new(new Vector3(0.16f, 0.10f, 0.10f), new Vector3(0.14f, 0.12f, 0.14f)),
                new(new Vector3(-0.15f, 0.09f, -0.09f), new Vector3(0.12f, 0.10f, 0.12f)),

                // El goteo: un lóbulo que se sale del montón hacia abajo y hacia
                // fuera. Es lo que separa esta mata de un arbusto cualquiera.
                new(new Vector3(0.30f, 0.04f, 0.14f), new Vector3(0.10f, 0.13f, 0.10f)),
            };

            var mata = new Part("mata",
                FoliageMeshBuilder.Weave(lobes, new Vector3(0f, -0.10f, 0f), 501u,
                                         "sombra", segments: 12, rings: 8),
                ToonPalette.Foliage(new Color32(0x37, 0x2E, 0x44, 255)));

            // Brillante y casi negro: el brillo dice «líquido» y la oscuridad, «vale».
            var tinta = new Part("tinta",
                MeshShapes.Sphere(9, 7, Vector3.one * 0.17f),
                ToonPalette.Solid(new Color32(0x16, 0x12, 0x1F, 255), 0.8f),
                new Vector3(0.09f, 0.24f, 0.05f), Quaternion.identity, Vector3.one);

            return new CropLook(new[] { mata }, new[] { mata, tinta });
        }

        /// <summary>Corrito de setas claras; al madurar, polvo luminoso sobre la alta.</summary>
        private static CropLook EsporaAlba()
        {
            // Tres setas desiguales fundidas en una sola malla: el corrito se lee
            // como un grupo aunque sea un solo objeto. Frente a la cetrella —una
            // seta única, azul y grande— la familia de hongos queda partida en dos
            // siluetas claras.
            var setas = new Part("setas",
                Merge("setas_espora",
                    (MeshShapes.Cylinder(6, 0.05f, 0.07f, 0.20f), new Vector3(0f, 0.10f, 0f),
                     Quaternion.identity, Vector3.one),
                    (MeshShapes.Sphere(8, 6, new Vector3(0.24f, 0.14f, 0.24f)),
                     new Vector3(0f, 0.22f, 0f), Quaternion.identity, Vector3.one),
                    (MeshShapes.Cylinder(6, 0.04f, 0.06f, 0.14f), new Vector3(0.16f, 0.07f, 0.10f),
                     Quaternion.identity, Vector3.one),
                    (MeshShapes.Sphere(8, 6, new Vector3(0.18f, 0.11f, 0.18f)),
                     new Vector3(0.16f, 0.155f, 0.10f), Quaternion.identity, Vector3.one),
                    (MeshShapes.Cylinder(6, 0.04f, 0.055f, 0.12f), new Vector3(-0.14f, 0.06f, -0.09f),
                     Quaternion.identity, Vector3.one),
                    (MeshShapes.Sphere(8, 6, new Vector3(0.16f, 0.10f, 0.16f)),
                     new Vector3(-0.14f, 0.135f, -0.09f), Quaternion.identity, Vector3.one)),
                ToonPalette.Solid(new Color32(0xEF, 0xE8, 0xD0, 255)));

            var polvo = new Part("polvo",
                MeshShapes.Sphere(7, 5, Vector3.one * 0.09f),
                ToonPalette.Solid(new Color32(0xFF, 0xEF, 0xA8, 255)),
                new Vector3(0f, 0.33f, 0f), Quaternion.identity, Vector3.one);

            return new CropLook(new[] { setas }, new[] { setas, polvo });
        }

        /// <summary>Briznas bajas; al madurar, esferas de vidrio apoyadas en la tierra.</summary>
        private static CropLook VidrioEstelar()
        {
            var brotes = new Part("brotes",
                Blades(601u, 1, 0.03f, 0.62f, "brotes_vidrio"),
                ToonPalette.Foliage(new Color32(0x74, 0xB4, 0x5C, 255)));

            // Vidrio aquí es un color claro con mucho reflejo, no transparencia:
            // un material transparente enseñaría la tierra a través de la esfera,
            // y lo que hace falta es que se lea «bola de cristal» de un vistazo.
            var esferas = new Part("esferas",
                Merge("esferas_vidrio",
                    (MeshShapes.Sphere(9, 7), new Vector3(0.12f, 0.07f, 0.08f),
                     Quaternion.identity, Vector3.one * 0.14f),
                    (MeshShapes.Sphere(9, 7), new Vector3(-0.11f, 0.06f, 0.02f),
                     Quaternion.identity, Vector3.one * 0.11f),
                    (MeshShapes.Sphere(9, 7), new Vector3(0.02f, 0.05f, -0.13f),
                     Quaternion.identity, Vector3.one * 0.09f)),
                ToonPalette.Solid(new Color32(0xCF, 0xEA, 0xF6, 255), 0.85f));

            return new CropLook(new[] { brotes }, new[] { brotes, esferas });
        }

        /// <summary>
        /// La planta genérica de antes: tallo recto y fruto salmón. Es el cajón de
        /// silla para cualquier semilla sin forma asignada, partidas viejas incluidas.
        /// </summary>
        private static CropLook Generica()
        {
            var tallo = new Part("tallo",
                MeshShapes.Cylinder(7, 0.12f, 0.2f, 1f),
                ToonPalette.Solid(new Color32(0x5F, 0xA8, 0x4A, 255)),
                new Vector3(0f, 0.5f, 0f), Quaternion.identity, new Vector3(0.7f, 1f, 0.7f));

            var fruto = new Part("fruto",
                MeshShapes.Sphere(9, 7),
                ToonPalette.Solid(new Color32(0xE8, 0x7A, 0x64, 255)),
                new Vector3(0f, 0.95f, 0f), Quaternion.identity, Vector3.one * 0.34f);

            return new CropLook(new[] { tallo }, new[] { tallo, fruto });
        }

        // ── piezas comunes ───────────────────────────────────────────────────

        /// <summary>Una pieza de planta: malla ya colocada, con su material.</summary>
        private readonly struct Part
        {
            public readonly string Name;
            public readonly Mesh Mesh;
            public readonly Material Material;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public Part(string name, Mesh mesh, Material material,
                        Vector3 position = default, Quaternion rotation = default,
                        Vector3? scale = null)
            {
                Name = name; Mesh = mesh; Material = material;
                Position = position;
                Rotation = rotation == default ? Quaternion.identity : rotation;
                Scale = scale ?? Vector3.one;
            }
        }

        /// <summary>Las dos caras de un cultivo: creciendo y con el fruto listo.</summary>
        private sealed class CropLook
        {
            public readonly Part[] Growing;
            public readonly Part[] Ready;

            public CropLook(Part[] growing, Part[] ready)
            {
                Growing = growing; Ready = ready;
            }
        }

        /// <summary>Une piezas de escultura —sin color de vértice— en una sola malla.</summary>
        private static Mesh Merge(string name,
                                  params (Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale)[] pieces)
        {
            var parts = new List<(Mesh mesh, Matrix4x4 transform)>(pieces.Length);
            foreach (var piece in pieces)
                parts.Add((piece.mesh, Matrix4x4.TRS(piece.pos, piece.rot, piece.scale)));
            return MeshShapes.Combine(parts, name);
        }

        /// <summary>
        /// Briznas tejidas con <see cref="Meadow.Weave"/>, que ya escribe el contrato
        /// de color de vértice que exige Nimbo/Foliage: oclusión, viento, degradado y
        /// semilla. Sin él la mata sale plana y quieta.
        /// </summary>
        private static Mesh Blades(uint seed, int tufts, float spread, float size, string name)
        {
            var rng = new Rng(seed);
            var list = new List<Meadow.Tuft>(tufts);
            for (int i = 0; i < tufts; i++)
                list.Add(new Meadow.Tuft(
                    new Vector3(rng.Range(-spread, spread), 0f, rng.Range(-spread, spread)),
                    Vector3.up, rng.Range(0f, 360f), size, rng.NextFloat()));
            return Meadow.Weave(list, 0, list.Count, name);
        }

        private static Quaternion Rot(float x, float y, float z) => Quaternion.Euler(x, y, z);

        /// <summary>Una valla baja alrededor: sin ella la parcela parece tierra suelta.</summary>
        private void BuildFence()
        {
            float w = _farm.Width * FarmPlot.TileSize;
            float h = _farm.Height * FarmPlot.TileSize;
            var material = ToonPalette.Solid(new Color32(0xA9, 0x86, 0x63, 255));

            Post(-w / 2f - 0.3f, 0f, 0.25f, h + 0.6f);
            Post(w / 2f + 0.3f, 0f, 0.25f, h + 0.6f);
            Post(0f, -h / 2f - 0.3f, w + 0.6f, 0.25f);
            Post(0f, h / 2f + 0.3f, w + 0.6f, 0.25f);

            void Post(float dx, float dz, float sx, float sz)
            {
                var go = new GameObject("valla");
                go.transform.SetParent(_root, worldPositionStays: false);
                go.transform.localPosition =
                    new Vector3(FarmPlot.CentreX + dx, 0.2f, FarmPlot.CentreZ + dz);

                // Sin colisionador: es decorativa. Con ella sólida, el jugador tendría
                // que rodear la parcela para llegar a sus propias casillas.
                AddPart(go.transform, "tramo", MeshShapes.Box(new Vector3(sx, 0.4f, sz)),
                        material);
            }
        }

        private static void AddPart(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddPart(Transform parent, in Part part)
        {
            var go = new GameObject(part.Name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = part.Position;
            go.transform.localRotation = part.Rotation;
            go.transform.localScale = part.Scale;
            go.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = part.Material;
        }

        // La losa se comparte entre las cuarenta y ocho casillas: es idéntica en
        // todas y generar una por casilla llenaría la memoria de copias. Las mallas
        // de los doce cultivos van en Looks, con el mismo criterio.

        private static Mesh Slab() =>
            _slab ??= MeshShapes.Box(new Vector3(FarmPlot.TileSize * 0.94f, 0.1f,
                                                 FarmPlot.TileSize * 0.94f));

        private void OnDestroy()
        {
            Discard(_slab);
            _slab = null;

            // Un mismo mesh puede estar en Growing y en Ready: el conjunto evita
            // destruirlo dos veces.
            var seen = new HashSet<Mesh>();
            foreach (var look in Looks.Values)
            {
                foreach (var part in look.Growing)
                    if (part.Mesh != null && seen.Add(part.Mesh)) Discard(part.Mesh);
                foreach (var part in look.Ready)
                    if (part.Mesh != null && seen.Add(part.Mesh)) Discard(part.Mesh);
            }
            Looks.Clear();
        }

        private static void Discard(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
        }
    }
}
