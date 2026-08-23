using System.Collections.Generic;
using System.Text;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Data.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// La ficha de un habitante: cómo está, quién es y qué te está pidiendo.
    /// </summary>
    /// <remarks>
    /// Es la pantalla donde el jugador decide, así que las peticiones van arriba del
    /// todo y las barras debajo. Al revés se lee como un panel de estadísticas, y esto
    /// no va de estadísticas: va de una persona que quiere algo.
    ///
    /// Todo cuelga de un <c>ScrollView</c> porque la ficha entera mide más de lo que da
    /// la pantalla: sin él, lo que se salía por abajo era justo la tarjeta de
    /// relaciones, y la vida social —la prioridad declarada del proyecto— quedaba
    /// cortada. Todos los demás paneles de lista llevan scroll; este es el último.
    ///
    /// El refresco corre cada 0,4 s desde <c>UiRoot</c>. Las barras y los textos se
    /// tocan siempre (escribir texto no derriba nada); las listas con botones solo se
    /// reconstruyen cuando su firma cambia, para no comerse los clics ni borrar avisos.
    /// </remarks>
    public sealed class IslanderPanel
    {
        private readonly ScrollView _scroll;
        private readonly JobSection _job = new JobSection();
        private readonly HomeSection _home = new HomeSection();
        private readonly SocialSection _social = new SocialSection();
        private readonly TastesSection _tastes = new TastesSection();
        private readonly IslandSocialCard _socialMap = new IslandSocialCard();
        private readonly VisualElement _requests;
        private readonly VisualElement _relationships;
        private readonly Label _name;
        private readonly Label _mood;
        private readonly Label _level;
        private readonly Label _personality;
        private readonly Label _activity;

        private readonly Dictionary<NeedKind, VisualElement> _needFills = new();

        /// <summary>Lo que se enseñó la última vez en peticiones y relaciones.</summary>
        private string _requestsSignature;
        private string _relationshipsSignature;

        private string _islanderId;

        public VisualElement Root { get; }

        public IslanderPanel()
        {
            Root = new VisualElement { name = "ficha-habitante" };
            Root.style.width = 380;
            Root.style.marginRight = UiTheme.Gap;
            Root.style.display = DisplayStyle.None;

            _scroll = new ScrollView();
            _scroll.style.flexGrow = 1;
            Root.Add(_scroll);

            var card = UiTheme.Card();

            _name = UiTheme.Title("—");
            card.Add(_name);

            var chips = new VisualElement();
            chips.style.flexDirection = FlexDirection.Row;
            chips.style.marginBottom = 10;
            _personality = UiTheme.Chip("—", UiTheme.Mood);
            _level = UiTheme.Chip("Nivel 1", UiTheme.InkSoft);
            chips.Add(_personality);
            chips.Add(_level);
            card.Add(chips);

            _mood = UiTheme.Body("—", soft: true);
            card.Add(_mood);
            _activity = UiTheme.Body("—", soft: true);
            _activity.style.marginBottom = 10;
            card.Add(_activity);

            AddNeedBar(card, NeedKind.Hunger, "Hambre", UiTheme.Hunger);
            AddNeedBar(card, NeedKind.Energy, "Energía", UiTheme.Energy);
            AddNeedBar(card, NeedKind.Social, "Compañía", UiTheme.Social);
            AddNeedBar(card, NeedKind.Hygiene, "Aseo", UiTheme.Hygiene);

            _scroll.Add(card);

            var requestCard = UiTheme.Card();
            requestCard.Add(UiTheme.Title("Te está pidiendo"));
            _requests = new VisualElement();
            requestCard.Add(_requests);
            _scroll.Add(requestCard);

            // Lo social por delante del trabajo y de la casa: la ficha va de una
            // persona que quiere algo, y lo primero que uno viene a hacer aquí es
            // decirle algo. Los gustos van justo detrás: es la otra cosa que puedes
            // hacerle —darle algo— y la que estaba sin nombre en toda la interfaz.
            _scroll.Add(_social.Root);
            _scroll.Add(_tastes.Root);
            _scroll.Add(_job.Root);
            _scroll.Add(_home.Root);

            var socialCard = UiTheme.Card();
            socialCard.Add(UiTheme.Title("Con quién anda"));
            _relationships = new VisualElement();
            socialCard.Add(_relationships);
            _scroll.Add(socialCard);

            // Y el mismo grafo para toda la isla: parejas, prometidos, casados y riñas
            // abiertas, sin abrir las fichas de uno en uno.
            _scroll.Add(_socialMap.Root);
        }

        private void AddNeedBar(VisualElement parent, NeedKind kind, string label, Color color)
        {
            parent.Add(UiTheme.NeedBar(label, color, out var fill));
            _needFills[kind] = fill;
        }

        public void Show(string islanderId)
        {
            _islanderId = islanderId;
            Root.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide()
        {
            _islanderId = null;
            Root.style.display = DisplayStyle.None;
        }

        public bool IsShowing => _islanderId != null;

        public void Refresh()
        {
            if (_islanderId == null) return;
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;
            if (!registry.TryGet(_islanderId, out var islander)) { Hide(); return; }

            _name.text = islander.Identity.DisplayName;
            _level.text = $"Nivel {islander.Progression.Level}";
            _mood.text = $"{EmotionName(islander.Mood.Emotion)} · {Mathf.RoundToInt(islander.Mood.Happiness)}% de ánimo";
            _activity.text = ActivityName(islander.Activity);

            if (ServiceRegistry.TryGet<IPersonalityService>(out var personalities))
            {
                var behaviour = personalities.For(islander.Personality);
                _personality.text = behaviour.DisplayName;
            }

            foreach (var pair in _needFills)
            {
                float value = islander.Needs[pair.Key];
                pair.Value.style.width = Length.Percent(value);
                pair.Value.style.backgroundColor = UiTheme.BarColor(BaseColor(pair.Key), value / 100f);
            }

            RefreshRequests(islander);
            _social.Refresh(_islanderId);
            _tastes.Refresh(_islanderId);
            _job.Refresh(_islanderId);
            _home.Refresh(_islanderId);
            RefreshRelationships(islander, registry);
            _socialMap.Refresh();
        }

        private static Color BaseColor(NeedKind kind) => kind switch
        {
            NeedKind.Hunger => UiTheme.Hunger,
            NeedKind.Energy => UiTheme.Energy,
            NeedKind.Social => UiTheme.Social,
            _ => UiTheme.Hygiene,
        };

        private void RefreshRequests(IslanderData islander)
        {
            if (!ServiceRegistry.TryGet<IRequestService>(out var service))
            {
                _requestsSignature = null;
                _requests.Clear();
                return;
            }

            // Una pasada para leer lo que hay y otra para pintarlo: entre medias se
            // compara la firma, y si no cambió nada, no se toca ni un botón.
            var open = new List<IslanderRequest>(service.OpenFor(islander.Id));

            var firma = new StringBuilder();
            for (int i = 0; i < open.Count; i++)
            {
                var request = open[i];
                var demand = service.DemandOf(request.RequestId);
                firma.Append(request.RequestId).Append('|').Append(request.Kind)
                     .Append('|').Append(request.Priority).Append('|')
                     .Append(request.CoinReward).Append('|').Append(request.Line)
                     .Append('|').Append(demand.WantsAnItem).Append('|')
                     .Append(demand.CatalogId).Append('|').Append(demand.Quantity);

                var options = service.OptionsFor(request.RequestId);
                for (int o = 0; o < options.Count; o++) firma.Append('|').Append(options[o]);
                firma.Append('\n');
            }

            string clave = firma.ToString();
            if (clave == _requestsSignature) return;
            _requestsSignature = clave;

            _requests.Clear();
            for (int i = 0; i < open.Count; i++)
            {
                // La fila la pinta el tablón: es la misma información y las mismas
                // decisiones, y tenerla escrita dos veces era pedir que se separasen.
                _requests.Add(Requests.RequestRow.Build(open[i], service, Refresh));
            }

            if (open.Count == 0)
                _requests.Add(UiTheme.Body("Ahora mismo, nada. Está a gusto.", soft: true));
        }

        private void RefreshRelationships(IslanderData islander, IIslanderRegistry registry)
        {
            var records = islander.Relationships.Records;

            var firma = new StringBuilder();
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.Friendship == FriendshipStage.Stranger &&
                    record.Conflict == ConflictStage.None) continue;
                if (NameOf(registry, record.OtherId) == null) continue;

                firma.Append(record.OtherId).Append('|').Append(record.Friendship)
                     .Append('|').Append(record.Conflict).Append('|')
                     .Append(record.Romance).Append('\n');
            }

            string clave = firma.ToString();
            if (clave == _relationshipsSignature) return;
            _relationshipsSignature = clave;

            _relationships.Clear();

            bool any = false;
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.Friendship == FriendshipStage.Stranger &&
                    record.Conflict == ConflictStage.None) continue;

                // El protagonista tiene ficha en su agenda pero no está en el censo, así
                // que hay que nombrarlo aparte. Sin esto la fila se saltaba en silencio
                // y lo único que no salía en «con quién anda» era contigo.
                string who = NameOf(registry, record.OtherId);
                if (who == null) continue;

                any = true;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var name = UiTheme.Body(who);
                name.style.width = 110;
                row.Add(name);
                row.Add(UiTheme.Chip(SocialLabels.StatusOf(record),
                                     SocialLabels.StatusColor(record)));
                _relationships.Add(row);
            }

            if (!any) _relationships.Add(UiTheme.Body("Todavía no conoce a nadie.", soft: true));
        }

        /// <summary>
        /// El nombre con que una relación sale en la lista: el vecino si está en el
        /// censo, el protagonista si eres tú, y nulo si no es nadie conocido.
        /// </summary>
        private static string NameOf(IIslanderRegistry registry, string islanderId) =>
            islanderId == SocialIds.Player
                ? SocialLabels.PlayerName()
                : registry.TryGet(islanderId, out var other) ? other.Identity.ShortName : null;

        private static string EmotionName(Emotion emotion) => emotion switch
        {
            Emotion.Ecstatic => "Radiante",
            Emotion.Happy => "Contento",
            Emotion.Neutral => "Tranquilo",
            Emotion.Sad => "Triste",
            Emotion.Angry => "Enfadado",
            Emotion.Sleepy => "Con sueño",
            Emotion.Hungry => "Hambriento",
            Emotion.Bored => "Aburrido",
            Emotion.Surprised => "Sorprendido",
            Emotion.Love => "Enamorado",
            Emotion.Proud => "Orgulloso",
            _ => "Preocupado",
        };

        private static string ActivityName(IslanderActivity activity) => activity switch
        {
            IslanderActivity.Sleeping => "Está durmiendo.",
            IslanderActivity.Eating => "Está comiendo.",
            IslanderActivity.Bathing => "Se está aseando.",
            IslanderActivity.Socializing => "Está de charla.",
            IslanderActivity.Playing => "Está jugando.",
            IslanderActivity.Working => "Está trabajando.",
            IslanderActivity.Shopping => "Está de compras.",
            IslanderActivity.Walking => "Va de camino a algún sitio.",
            IslanderActivity.Moping => "Está enfurruñado.",
            _ => "No está haciendo nada en particular.",
        };
    }
}
