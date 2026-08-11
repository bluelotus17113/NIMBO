using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Housing;
using Nimbo.Data.Save;

namespace Nimbo.Housing
{
    /// <summary>
    /// Implementación de <see cref="IHomeUpgradeService"/>: ampliar la casa de un habitante.
    /// Dos niveles de pago que agrandan la rejilla y remapean el suelo sin tocar los muebles.
    /// </summary>
    public class HomeUpgradeService : IHomeUpgradeService
    {
        // Nivel 0: 8×8 (serie), Nivel 1: 11×11, Nivel 2: 14×14
        static readonly int[] Sizes = { 8, 11, 14 };
        // Lo que cuesta pasar de este nivel al siguiente
        static readonly long[] Prices = { 1200, 3500 };

        readonly SaveGame _save;
        readonly IIslanderRegistry _registry;
        readonly IEconomyService _economy;

        public HomeUpgradeService(SaveGame save, IIslanderRegistry registry, IEconomyService economy)
        {
            _save = save;
            _registry = registry;
            _economy = economy;
        }

        // ---------------------------------------------------------------- IHomeUpgradeService

        public int MaxLevel => Sizes.Length - 1;

        public int LevelOf(string islanderId)
        {
            var record = FindHomeRecord(islanderId);
            return record?.UpgradeLevel ?? 0;
        }

        public long PriceOf(int level)
        {
            if (level < 0 || level >= Prices.Length) return 0;
            return Prices[level];
        }

        public int SizeOfLevel(int level)
        {
            if (level < 0) return Sizes[0];
            if (level >= Sizes.Length) return Sizes[Sizes.Length - 1];
            return Sizes[level];
        }

        public UpgradeRejection CanUpgrade(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander))
                return UpgradeRejection.UnknownIslander;

            if (!islander.Home.HasHome)
                return UpgradeRejection.NoHome;

            var record = _save.FindHome(islander.Home.BuildingId, islander.Home.UnitIndex);
            if (record == null)
                return UpgradeRejection.NoHome;

            if (record.UpgradeLevel >= MaxLevel)
                return UpgradeRejection.MaxedOut;

            long price = Prices[record.UpgradeLevel];
            if (_economy.Wallet.Coins < price)
                return UpgradeRejection.NotEnoughCoins;

            return UpgradeRejection.Ok;
        }

        public bool Upgrade(string islanderId)
        {
            if (CanUpgrade(islanderId) != UpgradeRejection.Ok) return false;

            // Volvemos a resolver el habitante y el HomeRecord aunque CanUpgrade ya lo
            // hizo: así el método se protege solo y no depende de que el llamante haya
            // llamado a CanUpgrade antes.
            if (!_registry.TryGet(islanderId, out var islander)) return false;

            var record = _save.FindHome(islander.Home.BuildingId, islander.Home.UnitIndex);
            if (record == null) return false;

            long price = Prices[record.UpgradeLevel];

            // Si el cobro falla no se toca nada: ni el suelo, ni los muebles, ni el nivel
            if (!_economy.TrySpend(price, "ampliación de casa")) return false;

            int oldLevel = record.UpgradeLevel;
            int newLevel = oldLevel + 1;
            int newSize = Sizes[newLevel];

            // La habitación crece hacia el este y el sur para que las coordenadas de lo
            // que ya había no cambien. Eso obliga a reconstruir FloorTiles cambiando el
            // índice plano porque y * Width + x depende de Width.
            RemapFloor(record.Layout, newSize);

            record.Layout.Width = newSize;
            record.Layout.Height = newSize;
            record.UpgradeLevel = newLevel;

            EventBus.Publish(new HomeUpgraded(islanderId, newLevel, newSize));

            return true;
        }

        // -------------------------------------------------------------------------- internals

        /// <summary>
        /// Reconstruye <see cref="RoomLayout.FloorTiles"/> para el nuevo tamaño.
        /// Las casillas viejas conservan sus coordenadas (x,y); las nuevas se
        /// rellenan con el acabado por defecto.
        /// </summary>
        /// <remarks>
        /// No se puede simplemente cambiar Width y añadir casillas al final: el índice
        /// es y * Width + x, así que al cambiar Width todos los índices cambian de
        /// significado. Hay que construir una lista nueva entera.
        /// </remarks>
        static void RemapFloor(RoomLayout room, int newSize)
        {
            int oldWidth = room.Width;
            int oldHeight = room.Height;
            var oldTiles = room.FloorTiles;

            int newCellCount = newSize * newSize;
            var newTiles = new List<string>(newCellCount);

            const string defaultFinish = "floor_madera_clara";

            // Rellenar todo con el acabado por defecto y luego sobreescribir con lo viejo
            for (int i = 0; i < newCellCount; i++)
                newTiles.Add(defaultFinish);

            for (int y = 0; y < oldHeight; y++)
            {
                for (int x = 0; x < oldWidth; x++)
                {
                    int oldIndex = y * oldWidth + x;
                    // Si la lista vieja vino más corta de lo esperado, la casilla se
                    // queda con el acabado por defecto y no se revienta
                    if (oldIndex >= oldTiles.Count) continue;

                    int newIndex = y * newSize + x;
                    newTiles[newIndex] = oldTiles[oldIndex];
                }
            }

            room.FloorTiles = newTiles;
        }

        HomeRecord FindHomeRecord(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return null;
            if (!islander.Home.HasHome) return null;
            return _save.FindHome(islander.Home.BuildingId, islander.Home.UnitIndex);
        }
    }
}
