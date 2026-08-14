using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Simulation.Wardrobe
{
    /// <summary>
    /// La ropa de los habitantes: regalarla, guardarla y ponérsela.
    /// </summary>
    /// <remarks>
    /// Vestir es la otra mitad del bucle de regalos, junto a la comida. La diferencia
    /// es que la ropa **se queda puesta**: el jugador la ve cada vez que mira a ese
    /// habitante, y por eso es la recompensa que más se recuerda de las tiendas.
    ///
    /// Los datos (<c>Wardrobe</c> y <c>EquippedOutfit</c>) estaban en
    /// <see cref="IslanderData"/> desde el principio, pero nadie los tocaba. Esto es
    /// lo que los pone a funcionar.
    /// </remarks>
    public sealed class WardrobeService
    {
        private readonly IIslanderRegistry _registry;
        private readonly ISimulationService _simulation;
        private readonly IPersonalityService _personalities;

        public WardrobeService(IIslanderRegistry registry, ISimulationService simulation,
                               IPersonalityService personalities)
        {
            _registry = registry;
            _simulation = simulation;
            _personalities = personalities;
        }

        /// <summary>
        /// Le da una prenda: pasa a su armario y se la pone si le gusta más que la
        /// que lleva. Devuelve false si no se le puede dar.
        /// </summary>
        public bool Give(string islanderId, string catalogId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return false;
            if (string.IsNullOrEmpty(catalogId)) return false;

            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return false;

            var item = economy.GetItem(catalogId);
            if (item == null || item.Category != ItemCategory.Clothing)
            {
                Debug.LogWarning($"WardrobeService: {catalogId} no es ropa");
                return false;
            }
            if (!economy.Inventory.Remove(catalogId)) return false;

            Receive(islanderId, catalogId);

            float liking = Liking(islander, catalogId);

            var behaviour = _personalities.For(islander.Personality);
            var reaction = liking >= 0.65f ? PersonalityReaction.GiftLoved
                         : liking <= 0.35f ? PersonalityReaction.GiftDisliked
                         : PersonalityReaction.GiftNeutral;

            _simulation.ShowEmotion(islanderId, behaviour.ReactTo(reaction), 5f);
            _simulation.ApplyHappiness(islanderId, (liking - 0.4f) * 24f);

            EventBus.Publish(new ItemGifted(catalogId, islanderId,
                                            liking >= 0.65f ? 1 : liking <= 0.35f ? -1 : 0));
            return true;
        }

        /// <summary>
        /// La prenda entra en su armario, y se la pone si le gusta más que la que
        /// lleva. No toca ningún inventario ni le cambia la cara.
        /// </summary>
        /// <remarks>
        /// Separado de <see cref="Give"/> porque hay dos formas de darle ropa y cada
        /// una la saca de un sitio distinto: la tienda tira de la despensa y regalar
        /// por la isla tira de la mochila. Lo único que comparten es esto —que acaba
        /// puesta— y por eso es lo único que está aquí.
        /// </remarks>
        public void Receive(string islanderId, string catalogId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            if (string.IsNullOrEmpty(catalogId)) return;

            if (!islander.Wardrobe.Contains(catalogId)) islander.Wardrobe.Add(catalogId);

            if (Liking(islander, catalogId) >= Liking(islander, islander.EquippedOutfit))
                islander.EquippedOutfit = catalogId;
        }

        /// <summary>Le pone una prenda que ya tiene. False si no está en su armario.</summary>
        public bool Wear(string islanderId, string catalogId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return false;
            if (!islander.Wardrobe.Contains(catalogId)) return false;

            islander.EquippedOutfit = catalogId;
            _simulation.ShowEmotion(islanderId, Emotion.Happy, 3f);
            return true;
        }

        public IReadOnlyList<string> WardrobeOf(string islanderId) =>
            _registry.TryGet(islanderId, out var islander)
                ? islander.Wardrobe
                : System.Array.Empty<string>();

        /// <summary>
        /// Cuánto le gusta una prenda, de 0 a 1.
        /// </summary>
        /// <remarks>
        /// Se deriva del id y de su personalidad, y es estable: la misma prenda le
        /// gusta lo mismo hoy que dentro de un mes. Si se sorteara cada vez, el
        /// jugador no podría aprender qué le va a cada habitante, y aprender eso es
        /// exactamente lo que hace que los conozca.
        /// </remarks>
        public float Liking(IslanderData islander, string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return 0f;

            // El gusto base sale del par habitante-prenda, así que es suyo y de nadie más.
            var rng = Rng.FromSeed($"{islander.Id}|{catalogId}");
            float baseTaste = rng.Range(0.15f, 0.85f);

            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return baseTaste;

            var item = economy.GetItem(catalogId);
            if (item == null) return baseTaste;

            // Y luego la personalidad inclina: a un expresivo le tira lo llamativo, y
            // a un práctico lo cómodo. El precio hace de aproximación de «llamativo».
            float flashy = Mathf.Clamp01(item.Price / 120f);
            float taste = baseTaste
                        + islander.Personality.Expression * flashy * 0.25f
                        - islander.Personality.Outlook * (0.5f - flashy) * 0.15f;

            return Mathf.Clamp01(taste);
        }

        /// <summary>
        /// La prenda que más le gusta de las que tiene. La usa la IA para que se
        /// cambie solo de vez en cuando.
        /// </summary>
        public string FavouriteOf(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return null;

            string best = islander.EquippedOutfit;
            float bestLiking = Liking(islander, best);

            for (int i = 0; i < islander.Wardrobe.Count; i++)
            {
                float liking = Liking(islander, islander.Wardrobe[i]);
                if (liking <= bestLiking) continue;
                bestLiking = liking;
                best = islander.Wardrobe[i];
            }
            return best;
        }
    }
}
