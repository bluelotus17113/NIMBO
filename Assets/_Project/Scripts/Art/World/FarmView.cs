using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Farming;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Dibuja el huerto: la tierra y lo que crece en ella.
    /// </summary>
    /// <remarks>
    /// Una casilla es una losa plana más, si hay algo sembrado, una planta encima. La
    /// planta cambia de tamaño y color según lo crecida que esté, que a la distancia a
    /// la que se juega dice más que cualquier detalle: de un vistazo se ve qué falta
    /// por regar y qué está para recoger.
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
        private static Mesh _sprout;

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
            float height = Mathf.Lerp(0.25f, 1.15f, ready ? 1f : grown);

            var stem = new GameObject("planta");
            stem.transform.SetParent(parent, worldPositionStays: false);

            AddPart(stem.transform, "tallo", Sprout(),
                    ToonPalette.Solid(ready ? new Color32(0x5F, 0xA8, 0x4A, 255)
                                            : new Color32(0x7C, 0xC0, 0x62, 255)));
            stem.transform.localScale = new Vector3(0.7f, height, 0.7f);

            // Lo que está para recoger lleva fruto: es la señal que el jugador busca
            // desde lejos, y con solo el tamaño del tallo no se distingue de lo que
            // aún le falta un día.
            if (!ready) return;

            var fruit = new GameObject("fruto");
            fruit.transform.SetParent(parent, worldPositionStays: false);
            fruit.transform.localPosition = new Vector3(0f, height * 0.95f, 0f);
            AddPart(fruit.transform, "pieza", Sphere(),
                    ToonPalette.Solid(new Color32(0xE8, 0x7A, 0x64, 255)));
            fruit.transform.localScale = Vector3.one * 0.34f;
        }

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

        // Las tres mallas se comparten entre las cuarenta y ocho casillas: son
        // idénticas y generar una por casilla llenaría la memoria de copias.

        private static Mesh Slab() =>
            _slab ??= MeshShapes.Box(new Vector3(FarmPlot.TileSize * 0.94f, 0.1f,
                                                 FarmPlot.TileSize * 0.94f));

        private static Mesh Sprout() =>
            _sprout ??= MeshShapes.Cylinder(7, 0.12f, 0.2f, 1f);

        private static Mesh _sphere;
        private static Mesh Sphere() => _sphere ??= MeshShapes.Sphere(9, 7);

        private void OnDestroy()
        {
            foreach (var mesh in new[] { _slab, _sprout, _sphere })
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
            _slab = _sprout = _sphere = null;
        }
    }
}
