using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Farming;

namespace Nimbo.Farming
{
    /// <summary>
    /// El huerto: una parcela de 8×6 junto a la casa.
    /// Sin estaciones, sin muerte de plantas, sin penalización por no regar.
    /// </summary>
    /// <remarks>
    /// Implementa <see cref="IFarmingService"/> al pie de la letra. Las reglas
    /// importantes que no se negocian:
    /// <list type="bullet">
    /// <item>Una planta sin regar no se muere: no crece ese día y ya.</item>
    /// <item><c>AdvanceDay</c> puede llamarse muchas veces seguidas sin romperse.</item>
    /// <item>Recoger con la mochila llena devuelve <c>InventoryFull</c> y deja la
    /// planta lista para otro intento.</item>
    /// </list>
    /// </remarks>
    public class FarmingService : IFarmingService
    {
        readonly CropCatalog _catalog;
        readonly FarmState _state;
        readonly IInventoryService _inventory;

        public int Width => _state.Width;
        public int Height => _state.Height;

        public IReadOnlyList<CropDefinition> Crops => _catalog.Crops;

        public FarmingService(CropCatalog catalog, FarmState state, IInventoryService inventory)
        {
            _catalog = catalog;
            _state = state;
            _inventory = inventory;

            // Si el huerto viene vacío (partida nueva), lo sembramos con casillas Wild.
            if (_state.Tiles.Count == 0)
            {
                for (int y = 0; y < _state.Height; y++)
                {
                    for (int x = 0; x < _state.Width; x++)
                    {
                        _state.Tiles.Add(new FarmTile
                        {
                            X = x,
                            Y = y,
                            State = TileState.Wild,
                            SeedId = "",
                            GrowthDays = 0,
                            Watered = false,
                        });
                    }
                }
            }
        }

        public FarmTile TileAt(int x, int y)
        {
            var idx = IndexOf(x, y);
            if (idx < 0) return null;
            return _state.Tiles[idx];
        }

        public bool TryGetCrop(string seedId, out CropDefinition crop)
        {
            return _catalog.TryGetCrop(seedId, out crop);
        }

        // ── Till ────────────────────────────────────────────────────────────

        public FarmError Till(int x, int y)
        {
            var idx = IndexOf(x, y);
            if (idx < 0) return FarmError.OutOfBounds;

            var tile = _state.Tiles[idx];

            // Solo se labra lo salvaje. Si ya está labrada o tiene algo, no pasa nada.
            if (tile.State == TileState.Wild)
            {
                tile.State = TileState.Tilled;
                tile.SeedId = "";
                tile.GrowthDays = 0;
                tile.Watered = false;
                EventBus.Publish(new TileChanged(x, y));
            }

            return FarmError.Ok;
        }

        // ── Plant ───────────────────────────────────────────────────────────

        public FarmError Plant(int x, int y, string seedId)
        {
            var idx = IndexOf(x, y);
            if (idx < 0) return FarmError.OutOfBounds;

            var tile = _state.Tiles[idx];

            if (tile.State != TileState.Tilled)
                return FarmError.NotTilled;

            if (!_catalog.TryGetCrop(seedId, out var crop))
                return FarmError.UnknownSeed;

            // Comprobar que lleva la semilla antes de gastarla.
            if (_inventory.CountOf(seedId) <= 0)
                return FarmError.NoSeed;

            // Gastar la semilla de la mochila.
            _inventory.TryTake(seedId, 1);

            tile.State = TileState.Planted;
            tile.SeedId = seedId;
            tile.GrowthDays = 0;
            tile.Watered = false;

            EventBus.Publish(new TileChanged(x, y));
            return FarmError.Ok;
        }

        // ── Water ───────────────────────────────────────────────────────────

        public FarmError Water(int x, int y)
        {
            var idx = IndexOf(x, y);
            if (idx < 0) return FarmError.OutOfBounds;

            var tile = _state.Tiles[idx];

            // Solo se riega lo que tiene algo creciendo o listo para recoger.
            if (tile.State != TileState.Planted && tile.State != TileState.Ready)
                return FarmError.NothingPlanted;

            tile.Watered = true;
            EventBus.Publish(new TileChanged(x, y));
            return FarmError.Ok;
        }

        // ── AdvanceDay ──────────────────────────────────────────────────────

        public void AdvanceDay()
        {
            // Primera pasada: lo regado crece.
            for (int i = 0; i < _state.Tiles.Count; i++)
            {
                var tile = _state.Tiles[i];

                if (tile.State == TileState.Planted && tile.Watered)
                {
                    tile.GrowthDays++;

                    if (_catalog.TryGetCrop(tile.SeedId, out var crop)
                        && tile.GrowthDays >= crop.DaysToGrow)
                    {
                        tile.State = TileState.Ready;
                        EventBus.Publish(new TileChanged(tile.X, tile.Y));
                    }
                }
                // Lo no regado no avanza y no se muere: sencillamente no crece hoy.
            }

            // Segunda pasada: a todas se les seca el riego.
            for (int i = 0; i < _state.Tiles.Count; i++)
            {
                _state.Tiles[i].Watered = false;
            }
        }

        // ── Harvest ─────────────────────────────────────────────────────────

        public int Harvest(int x, int y, out FarmError error)
        {
            var idx = IndexOf(x, y);
            if (idx < 0)
            {
                error = FarmError.OutOfBounds;
                return 0;
            }

            var tile = _state.Tiles[idx];

            if (tile.State != TileState.Ready)
            {
                error = FarmError.NotReady;
                return 0;
            }

            if (!_catalog.TryGetCrop(tile.SeedId, out var crop))
            {
                error = FarmError.UnknownSeed;
                return 0;
            }

            // Intentar meterlo en la mochila. Si no cabe, no se recoge.
            var result = _inventory.TryStore(crop.CropId, crop.Yield, out int leftover);

            if (result == StoreResult.Full || leftover > 0)
            {
                // No cupo nada (o no cupo todo): la planta se queda como está.
                // Así el jugador puede hacer sitio y volver a por ella.
                error = FarmError.InventoryFull;
                return 0;
            }

            int harvested = crop.Yield;

            if (crop.Regrows)
            {
                // Vuelve a Planted con la mitad de días de crecimiento acumulados.
                tile.State = TileState.Planted;
                tile.GrowthDays /= 2;
                tile.Watered = false;
            }
            else
            {
                // La casilla queda labrada y vacía.
                tile.State = TileState.Tilled;
                tile.SeedId = "";
                tile.GrowthDays = 0;
                tile.Watered = false;
            }

            // Publicar siempre después de cambiar el estado.
            EventBus.Publish(new TileChanged(x, y));
            EventBus.Publish(new CropHarvested(crop.CropId, harvested));

            error = FarmError.Ok;
            return harvested;
        }

        // ── Clear ───────────────────────────────────────────────────────────

        public FarmError Clear(int x, int y)
        {
            var idx = IndexOf(x, y);
            if (idx < 0) return FarmError.OutOfBounds;

            var tile = _state.Tiles[idx];

            // Arranca lo que haya y deja labrado, sin devolver nada.
            tile.State = TileState.Tilled;
            tile.SeedId = "";
            tile.GrowthDays = 0;
            tile.Watered = false;

            EventBus.Publish(new TileChanged(x, y));
            return FarmError.Ok;
        }

        // ── helpers ─────────────────────────────────────────────────────────

        /// <summary>Convierte (x, y) a índice lineal, o -1 si fuera de rango.</summary>
        int IndexOf(int x, int y)
        {
            if (x < 0 || x >= _state.Width || y < 0 || y >= _state.Height)
                return -1;
            return y * _state.Width + x;
        }
    }
}
