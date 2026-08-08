using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Island
{
    /// <summary>Lo que el árbol da al hablar con él.</summary>
    public enum TreeGiftKind
    {
        Coins = 0,
        Item = 1,
        Advice = 2,   // una pista sobre un habitante
        Joke = 3,
    }

    /// <summary>Lo que el árbol dio esta vez.</summary>
    public readonly struct TreeGift
    {
        public readonly TreeGiftKind Kind;
        public readonly string Text;
        public readonly int Coins;
        public readonly string CatalogId;

        public TreeGift(TreeGiftKind kind, string text, int coins = 0, string catalogId = null)
        {
            Kind = kind; Text = text; Coins = coins; CatalogId = catalogId;
        }
    }

    /// <summary>
    /// El Árbol Nimbo: se le habla una vez al día y da algo.
    /// </summary>
    /// <remarks>
    /// Es el ritual de apertura de sesión. Su valor no está en lo que da — unas
    /// monedas — sino en que da una razón para abrir el juego todos los días y en que
    /// las pistas dirigen la atención hacia habitantes que el jugador tiene olvidados.
    ///
    /// El árbol crece con el nivel de la isla, del 1 al 10, y lo que da escala con él.
    /// </remarks>
    public sealed class NimboTree
    {
        private const string LastDayFlag = "arbol_ultimo_dia_";

        private readonly SaveGame _save;
        private readonly GameClock _clock;
        private readonly IIslanderRegistry _registry;

        private Rng _rng = Rng.FromTime();

        public NimboTree(SaveGame save, GameClock clock, IIslanderRegistry registry)
        {
            _save = save;
            _clock = clock;
            _registry = registry;
        }

        /// <summary>De 1 a 10: brote al principio, árbol gigante al final.</summary>
        public int GrowthStage => Mathf.Clamp(_save.Island.Level, 1, IslandState.MaxLevel);

        /// <summary>Escala del árbol para el arte, de 0.35 a 1.</summary>
        public float GrowthScale => Mathf.Lerp(0.35f, 1f, (GrowthStage - 1) / 9f);

        public bool CanTalkToday => !_save.HasFlag(LastDayFlag + _clock.Day);

        /// <summary>
        /// Habla con el árbol. Devuelve false por el segundo intento del mismo día:
        /// la gracia es que sea una vez al día, no una máquina de monedas.
        /// </summary>
        public bool TryTalk(out TreeGift gift)
        {
            if (!CanTalkToday)
            {
                gift = new TreeGift(TreeGiftKind.Joke,
                    "El Árbol Nimbo ya te ha contado lo suyo por hoy. Vuelve mañana.");
                return false;
            }

            // Se marca el día antes de dar nada: si algo falla al entregar el regalo,
            // lo peor que pasa es que el jugador pierda uno, no que los gane infinitos.
            _save.SetFlag(LastDayFlag + _clock.Day);
            CleanOldFlags();

            gift = RollGift();
            Deliver(gift);
            return true;
        }

        private TreeGift RollGift()
        {
            int roll = _rng.Range(0, 100);

            if (roll < 45) return CoinGift();
            if (roll < 65) return ItemGift();
            if (roll < 85) return AdviceGift();
            return JokeGift();
        }

        private TreeGift CoinGift()
        {
            // Escala con el árbol: 20-40 al principio, 110-200 con la isla al máximo.
            int low = 20 * GrowthStage;
            int high = low * 2;
            int coins = _rng.Range(low, high);

            return new TreeGift(TreeGiftKind.Coins,
                $"El Árbol Nimbo deja caer {coins} nimbos entre las hojas.", coins);
        }

        private TreeGift ItemGift()
        {
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return CoinGift();

            var pool = new System.Collections.Generic.List<string>();
            foreach (var item in economy.ItemsOfCategory(ItemCategory.Food))
                if (item.UnlockLevel <= GrowthStage * 5) pool.Add(item.CatalogId);

            if (pool.Count == 0) return CoinGift();

            string id = pool[_rng.Range(0, pool.Count)];
            var definition = economy.GetItem(id);

            return new TreeGift(TreeGiftKind.Item,
                $"Entre las raíces aparece algo: {definition?.DisplayName ?? id}.",
                catalogId: id);
        }

        /// <summary>
        /// La pista señala a quien peor está. Es la función de verdad del árbol:
        /// que el jugador se entere de a quién tiene abandonado.
        /// </summary>
        private TreeGift AdviceGift()
        {
            var all = _registry.All;
            if (all.Count == 0) return JokeGift();

            Data.Islanders.IslanderData saddest = null;
            for (int i = 0; i < all.Count; i++)
                if (saddest == null || all[i].Mood.Happiness < saddest.Mood.Happiness)
                    saddest = all[i];

            string name = saddest.Identity.ShortName;
            string text = saddest.Mood.Happiness < 40f
                ? $"El árbol susurra: «{name} lleva unos días con la cabeza baja. ¿Te has fijado?»"
                : $"El árbol susurra: «Hoy {name} está de buen ánimo. Aprovecha para presentarle a alguien.»";

            return new TreeGift(TreeGiftKind.Advice, text);
        }

        private TreeGift JokeGift()
        {
            string[] jokes =
            {
                "El árbol se queda callado un rato largo. Luego dice: «bonito día».",
                "«¿Sabes por qué las nubes no pagan alquiler? Porque siempre están de paso.»",
                "El árbol cruje. Podría ser una risa. Podría ser el viento.",
                "«Llevo aquí más que la isla. La isla llegó después, ¿sabías?»",
                "«Si un habitante se muda y nadie lo ve irse, ¿hace ruido la puerta?»",
                "El árbol te ofrece una hoja. Es solo una hoja, pero la da con cariño.",
            };
            return new TreeGift(TreeGiftKind.Joke, jokes[_rng.Range(0, jokes.Length)]);
        }

        private void Deliver(in TreeGift gift)
        {
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return;

            if (gift.Coins > 0) economy.AddCoins(gift.Coins, "Árbol Nimbo");
            if (!string.IsNullOrEmpty(gift.CatalogId))
            {
                economy.Inventory.Add(gift.CatalogId);
                EventBus.Publish(new ItemAcquired(gift.CatalogId, 1));
            }
        }

        /// <summary>
        /// Borra las marcas de días viejos. Sin esto, una partida larga acumularía una
        /// marca por día jugado y el guardado crecería sin motivo.
        /// </summary>
        private void CleanOldFlags()
        {
            for (int i = _save.Flags.Count - 1; i >= 0; i--)
            {
                var flag = _save.Flags[i];
                if (!flag.StartsWith(LastDayFlag)) continue;
                if (flag == LastDayFlag + _clock.Day) continue;
                _save.Flags.RemoveAt(i);
            }
        }
    }
}
