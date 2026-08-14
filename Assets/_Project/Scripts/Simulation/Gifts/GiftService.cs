using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Simulation.Wardrobe;
using UnityEngine;

namespace Nimbo.Simulation.Gifts
{
    /// <summary>
    /// Darle a un vecino lo que llevas en la mano.
    /// </summary>
    /// <remarks>
    /// Es la costura que faltaba entre las dos mitades del juego. Recorrer la isla
    /// llenaba la mochila y la mochila solo servía para vender o craftear; convivir
    /// era el pilar número uno del diseño y no tenía forma de tocar nada de lo que
    /// recogías. Ahora la orquídea etérea —cuya propia ficha dice «quien consigue
    /// cogerla sin que se deshaga puede regalarla»— se puede regalar de verdad.
    ///
    /// El gusto por cada cosa es estable y sale del par habitante-objeto, igual que en
    /// <see cref="WardrobeService.Liking"/>. Es lo que hace que se pueda aprender qué
    /// le va a cada uno; si se sorteara cada vez, regalar sería tirar una moneda y
    /// conocerlos no significaría nada.
    /// </remarks>
    public sealed class GiftService : IGiftService
    {
        /// <summary>Por encima de esto le encanta; por debajo del otro, no le gusta.</summary>
        private const float LovedFrom = 0.65f;
        private const float DislikedUpTo = 0.35f;

        private readonly IIslanderRegistry _registry;
        private readonly ISimulationService _simulation;
        private readonly IPersonalityService _personalities;
        private readonly IInventoryService _inventory;
        private readonly WardrobeService _wardrobe;

        public GiftService(IIslanderRegistry registry, ISimulationService simulation,
                           IPersonalityService personalities, IInventoryService inventory,
                           WardrobeService wardrobe)
        {
            _registry = registry;
            _simulation = simulation;
            _personalities = personalities;
            _inventory = inventory;
            _wardrobe = wardrobe;
        }

        /// <summary>
        /// Todo se regala menos las herramientas.
        /// </summary>
        /// <remarks>
        /// Darle un pico a alguien sería quedarse sin poder picar, y la azada no se
        /// vuelve a comprar: es la única forma de romper la partida regalando. Piedras
        /// y madera sí se dan — a casi nadie le hará gracia, y esa es la broma.
        /// </remarks>
        public bool IsGiftable(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return false;
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return false;

            var item = economy.GetItem(catalogId);
            return item != null && item.Category != ItemCategory.Tool;
        }

        public float LikingOf(string islanderId, string catalogId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return 0f;
            return Liking(islander, catalogId);
        }

        public GiftResult OfferHeld(string islanderId, out int opinion)
        {
            opinion = 0;

            if (!_registry.TryGet(islanderId, out var islander)) return GiftResult.UnknownIslander;

            var held = _inventory.InHand;
            if (held.Quantity <= 0 || string.IsNullOrEmpty(held.CatalogId))
                return GiftResult.EmptyHanded;

            string catalogId = held.CatalogId;
            if (!IsGiftable(catalogId)) return GiftResult.NotGiftable;

            // El tope diario se gasta antes de descontar nada, y ese orden importa: si
            // se quitara primero de la mochila y luego resultara que hoy ya le habías
            // dado algo, el objeto se habría perdido a cambio de nada. Al revés no hay
            // riesgo — mover la afinidad no toca la mochila, así que el descuento de
            // después no puede fallar.
            if (!ServiceRegistry.TryGet<ISocialService>(out var social)) return GiftResult.UnknownIslander;
            if (!social.PlayerInteract(islanderId, SocialInteraction.Gift))
                return GiftResult.AlreadyToday;

            _inventory.TryConsumeSelected(1);

            float liking = Liking(islander, catalogId);
            opinion = liking >= LovedFrom ? 1 : liking <= DislikedUpTo ? -1 : 0;

            // La ropa se la pone; lo demás se lo queda y ya está. No hay despensa de
            // habitante donde meterlo, y tampoco hace falta: lo que se recuerda de un
            // regalo es la cara que puso.
            if (IsClothing(catalogId)) _wardrobe.Receive(islanderId, catalogId);

            var reaction = opinion > 0 ? PersonalityReaction.GiftLoved
                         : opinion < 0 ? PersonalityReaction.GiftDisliked
                         : PersonalityReaction.GiftNeutral;

            var behaviour = _personalities.For(islander.Personality);
            _simulation.ShowEmotion(islanderId, behaviour.ReactTo(reaction), 5f);
            _simulation.ApplyHappiness(islanderId, (liking - 0.4f) * 24f);

            EventBus.Publish(new ItemGifted(catalogId, islanderId, opinion));
            return GiftResult.Ok;
        }

        private static bool IsClothing(string catalogId)
        {
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return false;
            var item = economy.GetItem(catalogId);
            return item != null && item.Category == ItemCategory.Clothing;
        }

        /// <summary>
        /// Cuánto le gusta, de 0 a 1. Cada categoría se pregunta a quien sepa.
        /// </summary>
        /// <remarks>
        /// La comida tiene lista de lo que le encanta y lo que odia, escrita al crear
        /// al habitante: preguntarle a un sorteo cuando ya hay una respuesta escrita
        /// sería tirar el trabajo del creador de personajes. La ropa la valora el
        /// armario, que además sabe de precios y de lo llamativo. Lo demás no lo sabe
        /// nadie, así que se sortea una vez y se queda sorteado para siempre.
        /// </remarks>
        private float Liking(IslanderData islander, string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return 0f;

            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy))
                return StableTaste(islander, catalogId);

            var item = economy.GetItem(catalogId);
            if (item == null) return StableTaste(islander, catalogId);

            if (item.Category == ItemCategory.Clothing) return _wardrobe.Liking(islander, catalogId);

            if (item.Category == ItemCategory.Food)
            {
                return islander.Tastes.OpinionOf(catalogId) switch
                {
                    > 0 => 0.9f,
                    < 0 => 0.12f,
                    _ => 0.55f,   // comer siempre gusta un poco
                };
            }

            // Un puñado de fibra no es un regalo. Se puede dar, pero parte de más
            // abajo: lo que hace ilusión es lo que costó traer, no lo que sobraba.
            float floor = item.Category == ItemCategory.Material ? 0.05f : 0.15f;
            return Mathf.Clamp01(floor + StableTaste(islander, catalogId) * 0.75f);
        }

        private static float StableTaste(IslanderData islander, string catalogId)
        {
            var rng = Rng.FromSeed($"regalo|{islander.Id}|{catalogId}");
            return rng.Range(0.15f, 0.9f);
        }
    }
}
