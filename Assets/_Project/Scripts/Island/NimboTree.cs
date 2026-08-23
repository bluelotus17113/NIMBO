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
    /// Que haya razón para volver mañana la dan tres cosas: el regalo cambia cada día
    /// (sale de la semilla del día, no del azar del momento), los chistes rotan sin
    /// repetir hasta agotar la lista, y la racha — días seguidos visitándolo —
    /// engorda las monedas y el propio árbol la nombra en cuanto pasa de tres.
    ///
    /// El árbol crece con el nivel de la isla, del 1 al 10, y lo que da escala con él.
    /// </remarks>
    public sealed class NimboTree : ITreeService
    {
        private const string LastDayFlag = "arbol_ultimo_dia_";

        /// <summary>
        /// La racha se guarda como bandera con el número dentro («arbol_racha_4») y no
        /// como campo nuevo del guardado: <see cref="SaveGame.Flags"/> ya documenta que
        /// ahí viven «contadores sueltos… rachas», y serializar un entero más por esto
        /// es peso muerto. Solo hay una a la vez: al escribirla se borran las viejas.
        /// </summary>
        private const string StreakFlag = "arbol_racha_";

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

        /// <summary>Días seguidos hablando con él, hoy incluido. Cero si nunca se le habló.</summary>
        public int Streak => ReadStreak();

        /// <summary>
        /// Lo que la racha multiplica las monedas: +10% por día seguido, con techo en
        /// ×1,5 al quinto. Techo porque un bonus sin límite convertiría el ritual en la
        /// mejor fuente de ingresos de la isla, y entonces faltar un día se sentiría
        /// como un castigo económico y no como lo que es: una cita perdida. Y el suelo
        /// en ×1 porque sin racha el número tiene que ser neutro, no un castigo.
        /// </summary>
        public float StreakMultiplier => 1f + Mathf.Clamp(Streak - 1, 0, 5) * 0.1f;

        /// <summary>
        /// Habla con el árbol. Devuelve false por el segundo intento del mismo día:
        /// la gracia es que sea una vez al día, no una máquina de monedas.
        /// </summary>
        public bool TryTalk(out TreeGift gift)
        {
            if (!CanTalkToday)
            {
                gift = RefusalGift();

                // También se anuncia el «ya hablamos hoy»: un árbol que no contesta
                // nada al segundo intento se lee como un árbol roto, no como un árbol
                // que ya dio lo suyo.
                EventBus.Publish(new TreeSpoke(gift.Text, 0, null));
                return false;
            }

            // Se marca el día antes de dar nada: si algo falla al entregar el regalo,
            // lo peor que pasa es que el jugador pierda uno, no que los gane infinitos.
            _save.SetFlag(LastDayFlag + _clock.Day);
            CleanOldFlags();
            WriteStreak(CameYesterday ? ReadStreak() + 1 : 1);

            // El regalo del día sale de la semilla del día y no del reloj del sistema:
            // el mismo día da lo mismo aunque se cierre y se abra el juego, y las
            // pruebas pueden afirmar sobre un contenido que no cambia debajo de ellas.
            _rng = Rng.FromSeed("arbol_" + _clock.Day);

            gift = WithStreak(RollGift());
            Deliver(gift);

            // Por el bus y no por retorno: quien lo pinta es la interfaz, que vive en
            // otro ensamblado y no conoce TreeGift.
            EventBus.Publish(new TreeSpoke(gift.Text, gift.Coins, gift.CatalogId));
            return true;
        }

        /// <summary>
        /// La forma que ve la vista: el texto fuera, los tipos de la isla dentro.
        /// </summary>
        /// <remarks>
        /// Existe por el grafo de ensamblados: <c>Nimbo.Art</c> no ve
        /// <c>Nimbo.Island</c>, así que no puede nombrar <c>TreeGift</c>. Devolver el
        /// texto ya compuesto es más barato que subir el tipo a Core solo para esto.
        /// </remarks>
        bool ITreeService.TryTalk(out string text)
        {
            bool spoke = TryTalk(out TreeGift gift);
            text = gift.Text;
            return spoke;
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
            // Y con la racha encima: venir cada día paga, pero poco a poco (§racha).
            int low = Mathf.RoundToInt(20 * GrowthStage * StreakMultiplier);
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

        private static readonly string[] Jokes =
        {
            "El árbol se queda callado un rato largo. Luego dice: «bonito día».",
            "«¿Sabes por qué las nubes no pagan alquiler? Porque siempre están de paso.»",
            "El árbol cruje. Podría ser una risa. Podría ser el viento.",
            "«Llevo aquí más que la isla. La isla llegó después, ¿sabías?»",
            "«Si un habitante se muda y nadie lo ve irse, ¿hace ruido la puerta?»",
            "El árbol te ofrece una hoja. Es solo una hoja, pero la da con cariño.",
        };

        /// <summary>
        /// El chiste del día sale por rotación y no al azar. Con seis chistes y uno
        /// cada pocas visitas, el azar repetía antes de agotar la lista — con seis
        /// opciones, al tercer chiste ya es más probable repetir que estrenar — y el
        /// árbol se quedaba sin nada nuevo que decir. Rotando por día, no repite
        /// hasta haber dado la vuelta a todos.
        /// </summary>
        public static string JokeOfTheDay(int day) => Jokes[day % Jokes.Length];

        private TreeGift JokeGift() =>
            new TreeGift(TreeGiftKind.Joke, JokeOfTheDay(_clock.Day));

        /// <summary>
        /// Lo que ya se ha dicho hoy se ha dicho: el texto de rechazo también viaja,
        /// porque negarse en silencio es lo mismo que no estar ahí.
        /// </summary>
        private TreeGift RefusalGift()
        {
            string text = "El Árbol Nimbo ya te ha contado lo suyo por hoy. Vuelve mañana.";
            if (Streak >= 3) text += $" La racha va por {Streak} días.";
            return new TreeGift(TreeGiftKind.Joke, text);
        }

        /// <summary>
        /// A partir del tercer día seguido, el árbol nombra la racha. Es lo que
        /// convierte una tabla de bonus en un hábito: el jugador vuelve mañana para
        /// oír el número subir, no para cobrar un 10% más de nimbos.
        /// </summary>
        private TreeGift WithStreak(in TreeGift gift)
        {
            if (Streak < 3) return gift;

            return new TreeGift(gift.Kind,
                $"{gift.Text} El árbol añade: «llevas {Streak} días sin faltar a la cita».",
                gift.Coins, gift.CatalogId);
        }

        /// <summary>Vino a hablarle ayer. Es lo que mantiene viva la racha.</summary>
        private bool CameYesterday => _save.HasFlag(LastDayFlag + (_clock.Day - 1));

        private int ReadStreak()
        {
            for (int i = 0; i < _save.Flags.Count; i++)
            {
                string flag = _save.Flags[i];
                if (!flag.StartsWith(StreakFlag)) continue;
                return int.TryParse(flag.Substring(StreakFlag.Length), out int streak)
                    ? streak : 0;
            }
            return 0;
        }

        private void WriteStreak(int streak)
        {
            CleanStreakFlags();
            _save.SetFlag(StreakFlag + streak);
        }

        private void CleanStreakFlags()
        {
            for (int i = _save.Flags.Count - 1; i >= 0; i--)
                if (_save.Flags[i].StartsWith(StreakFlag)) _save.Flags.RemoveAt(i);
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
        /// <remarks>
        /// Deja la de hoy y la de ayer, ni una más: la de ayer es la que dice a
        /// <see cref="CameYesterday"/> si la racha continúa o empieza. Borrarla —como
        /// hacía la versión anterior— habría hecho imposible saberlo, y limpiar menos
        /// que eso acumularía marcas sin fin.
        /// </remarks>
        private void CleanOldFlags()
        {
            int yesterday = _clock.Day - 1;
            for (int i = _save.Flags.Count - 1; i >= 0; i--)
            {
                var flag = _save.Flags[i];
                if (!flag.StartsWith(LastDayFlag)) continue;
                if (flag == LastDayFlag + _clock.Day) continue;
                if (flag == LastDayFlag + yesterday) continue;
                _save.Flags.RemoveAt(i);
            }
        }
    }
}
