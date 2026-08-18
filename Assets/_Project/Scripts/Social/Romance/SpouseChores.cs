using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Farming;
using Nimbo.Data.Social;

namespace Nimbo.Social.Romance
{
    /// <summary>
    /// Lo que hace tu pareja por su cuenta cuando os casáis (§14.5).
    /// </summary>
    /// <remarks>
    /// **Una acción del huerto al día, y solo una.** Riega la casilla más seca de las
    /// que estén plantadas, que es exactamente lo que se olvida uno al volver después
    /// de dos días sin entrar. No siembra, no cosecha y no vende: eso es tu juego, y una
    /// pareja que lo hiciera todo convertiría el huerto en algo que ocurre sin ti.
    ///
    /// Una sola casilla también por lo mismo. Con el huerto entero regado cada mañana, la
    /// regadera dejaría de tener sentido y con ella media capa de granja. Lo que hace es
    /// que se note que ya no vives solo, no ahorrarte el trabajo.
    ///
    /// Y no pasa nada si no hay nada que regar: casarse no puede convertirse en una
    /// obligación de tener el huerto lleno.
    /// </remarks>
    public sealed class SpouseChores
    {
        private readonly IIslanderRegistry _registry;

        public SpouseChores(IIslanderRegistry registry) => _registry = registry;

        /// <summary>Con quién estás casado, o null si con nadie.</summary>
        public string SpouseId()
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Relationships.TryGet(SocialIds.Player, out var record) &&
                    record.Romance == RomanceStage.Married)
                    return all[i].Id;
            }
            return null;
        }

        /// <summary>Tu pareja pasa por el huerto. Devuelve cierto si ha regado algo.</summary>
        public bool DoMorningChore()
        {
            if (SpouseId() == null) return false;
            if (!ServiceRegistry.TryGet<IFarmingService>(out var farm)) return false;

            for (int y = 0; y < farm.Height; y++)
            {
                for (int x = 0; x < farm.Width; x++)
                {
                    var tile = farm.TileAt(x, y);
                    if (tile == null) continue;

                    // Solo lo plantado y sin regar. Una casilla labrada y vacía no
                    // necesita agua, y una que ya está lista tampoco.
                    if (tile.State != TileState.Planted || tile.Watered) continue;

                    if (farm.Water(x, y) != FarmError.Ok) continue;

                    EventBus.Publish(new SpouseHelped(SpouseId(), x, y));
                    return true;
                }
            }

            return false;
        }
    }
}
