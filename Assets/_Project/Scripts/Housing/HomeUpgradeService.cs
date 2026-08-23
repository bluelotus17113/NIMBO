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

        /// <summary>La obra, por nivel. Es lo que hay que traer de la isla. ⚙️</summary>
        /// <remarks>
        /// Las cantidades salen de lo que rinde un nodo: un roble da 3 de madera y una
        /// roca 2 de piedra, así que la primera ampliación son unas diez talas y diez
        /// picadas — una tarde larga o tres cortas, y sin que haga falta entrar a
        /// diario. La segunda pide savia, que solo dan el cedro ámbar y el musgo
        /// flotante, para que el segundo nivel obligue a mirar dónde crece qué en vez
        /// de a repetir lo mismo más veces.
        /// </remarks>
        static readonly MaterialCost[][] Materials =
        {
            new[] { new MaterialCost("mat_madera", 30), new MaterialCost("mat_piedra", 20) },
            new[] { new MaterialCost("mat_madera", 80), new MaterialCost("mat_piedra", 60),
                    new MaterialCost("mat_savia", 10) },
        };

        static readonly MaterialCost[] NoMaterials = new MaterialCost[0];

        readonly SaveGame _save;
        readonly IIslanderRegistry _registry;
        readonly IEconomyService _economy;
        readonly IInventoryService _bag;

        public HomeUpgradeService(SaveGame save, IIslanderRegistry registry,
                                  IEconomyService economy, IInventoryService bag)
        {
            _save = save;
            _registry = registry;
            _economy = economy;
            _bag = bag;
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

        public IReadOnlyList<MaterialCost> MaterialsFor(int level)
        {
            if (level < 0 || level >= Materials.Length) return NoMaterials;
            return Materials[level];
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

            if (!HasMaterials(record.UpgradeLevel))
                return UpgradeRejection.NotEnoughMaterials;

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

            // Y si falla la obra se devuelven las monedas. Pasar por aquí con material
            // de menos no debería ocurrir —CanUpgrade acaba de contarlo—, pero quedarse
            // sin monedas y sin ampliación es el único fallo de esta operación que el
            // jugador no podría deshacer, así que se cubre igual.
            if (!TryTakeMaterials(record.UpgradeLevel))
            {
                _economy.AddCoins(price, "ampliación de casa cancelada");
                return false;
            }

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

        // ---------------------------------------------------------------- tu cabaña

        public int PlayerLevel => _save?.Player?.HomeLevel ?? 0;

        public UpgradeRejection CanUpgradePlayerHome()
        {
            if (_save?.Player == null) return UpgradeRejection.NoHome;

            int level = _save.Player.HomeLevel;
            if (level >= MaxLevel) return UpgradeRejection.MaxedOut;

            if (_economy.Wallet.Coins < Prices[level]) return UpgradeRejection.NotEnoughCoins;
            if (!HasMaterials(level)) return UpgradeRejection.NotEnoughMaterials;

            return UpgradeRejection.Ok;
        }

        /// <summary>
        /// Amplía tu cabaña, con las mismas reglas que la de un vecino.
        /// </summary>
        /// <remarks>
        /// Todo o nada, igual que la de ellos: si la obra falla después de cobrar, se
        /// devuelven las monedas. Quedarse sin nimbos y sin ampliación es el único
        /// fallo de esta operación que el jugador no puede deshacer.
        ///
        /// No publica <c>HomeUpgraded</c>: ese aviso lleva el id de un habitante y la
        /// crónica lo usa para escribir su nombre. Tu cabaña la ves tú al entrar, y
        /// «han ampliado la casa de Nimbo» contado por la aldea sonaría a que lo ha
        /// hecho otro.
        /// </remarks>
        public bool UpgradePlayerHome()
        {
            if (CanUpgradePlayerHome() != UpgradeRejection.Ok) return false;

            var player = _save.Player;
            int level = player.HomeLevel;
            long price = Prices[level];

            if (!_economy.TrySpend(price, "ampliación de tu cabaña")) return false;

            if (!TryTakeMaterials(level))
            {
                _economy.AddCoins(price, "ampliación de tu cabaña cancelada");
                return false;
            }

            int newSize = Sizes[level + 1];

            RemapFloor(player.Home, newSize);
            player.Home.Width = newSize;
            player.Home.Height = newSize;
            player.HomeLevel = level + 1;

            return true;
        }

        // -------------------------------------------------------------------------- internals

        /// <summary>¿Lleva encima toda la obra de ese nivel?</summary>
        bool HasMaterials(int level)
        {
            var costs = MaterialsFor(level);
            if (costs.Count == 0) return true;

            // Sin mochila no se puede pedir obra, así que se deja pasar. Es el caso de
            // los tests que montan el servicio sin inventario.
            if (_bag == null) return true;

            for (int i = 0; i < costs.Count; i++)
                if (_bag.CountOf(costs[i].CatalogId) < costs[i].Quantity) return false;

            return true;
        }

        /// <summary>
        /// Gasta la obra. Todo o nada: si a mitad de la lista falta algo, devuelve lo
        /// que ya había sacado.
        /// </summary>
        /// <remarks>
        /// Cuenta primero y saca después, en dos pasadas. Sacar mientras cuentas deja
        /// media obra pagada cuando el último material no llega, y de ahí no se vuelve
        /// sin escribir el mismo bucle al revés.
        /// </remarks>
        bool TryTakeMaterials(int level)
        {
            var costs = MaterialsFor(level);
            if (costs.Count == 0 || _bag == null) return true;

            if (!HasMaterials(level)) return false;

            for (int i = 0; i < costs.Count; i++)
            {
                if (_bag.TryTake(costs[i].CatalogId, costs[i].Quantity)) continue;

                // Alguien tocó la mochila entre el recuento y ahora. Se devuelve lo
                // sacado hasta aquí y se sale sin ampliar.
                for (int back = 0; back < i; back++)
                    _bag.TryStore(costs[back].CatalogId, costs[back].Quantity, out _);
                return false;
            }

            return true;
        }

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

            // El acabado de serie vive en RoomLayout y es un id real del catálogo:
            // rellenar con un id inventado pintaba las casillas nuevas de un suelo
            // que nadie puede comprar ni ver.
            string defaultFinish = RoomLayout.DefaultFloor;

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
