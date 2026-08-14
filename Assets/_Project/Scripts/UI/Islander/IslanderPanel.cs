using System.Collections.Generic;
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
    /// </remarks>
    public sealed class IslanderPanel
    {
        private readonly JobSection _job = new JobSection();
        private readonly VisualElement _requests;
        private readonly VisualElement _relationships;
        private readonly Label _name;
        private readonly Label _mood;
        private readonly Label _level;
        private readonly Label _personality;
        private readonly Label _activity;

        private readonly Dictionary<NeedKind, VisualElement> _needFills = new();

        private string _islanderId;

        public VisualElement Root { get; }

        public IslanderPanel()
        {
            Root = new VisualElement { name = "ficha-habitante" };
            Root.style.width = 380;
            Root.style.marginRight = UiTheme.Gap;
            Root.style.display = DisplayStyle.None;

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

            Root.Add(card);

            var requestCard = UiTheme.Card();
            requestCard.Add(UiTheme.Title("Te está pidiendo"));
            _requests = new VisualElement();
            requestCard.Add(_requests);
            Root.Add(requestCard);

            Root.Add(_job.Root);

            var socialCard = UiTheme.Card();
            socialCard.Add(UiTheme.Title("Con quién anda"));
            _relationships = new VisualElement();
            socialCard.Add(_relationships);
            Root.Add(socialCard);
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
            _job.Refresh(_islanderId);
            RefreshRelationships(islander, registry);
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
            _requests.Clear();
            if (!ServiceRegistry.TryGet<IRequestService>(out var service)) return;

            bool any = false;
            foreach (var request in service.OpenFor(islander.Id))
            {
                any = true;
                _requests.Add(BuildRequestRow(request, service));
            }

            if (!any) _requests.Add(UiTheme.Body("Ahora mismo, nada. Está a gusto.", soft: true));
        }

        private VisualElement BuildRequestRow(IslanderRequest request, IRequestService service)
        {
            var row = new VisualElement();
            row.style.marginBottom = 12;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 4;
            header.Add(UiTheme.Chip(KindName(request.Kind), PriorityColor(request.Priority)));
            header.Add(UiTheme.Body($"+{request.CoinReward} nimbos", soft: true));
            row.Add(header);

            var line = UiTheme.Body($"«{request.Line}»");
            line.style.marginBottom = 6;
            row.Add(line);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;

            string id = request.RequestId;
            var accept = UiTheme.Action("Ayudarle", () => { service.Resolve(id); Refresh(); });
            accept.style.marginRight = 8;
            buttons.Add(accept);

            var refuse = UiTheme.Action("Ahora no", () => { service.Refuse(id); Refresh(); });
            refuse.style.backgroundColor = UiTheme.InkSoft;
            buttons.Add(refuse);

            row.Add(buttons);
            return row;
        }

        private void RefreshRelationships(IslanderData islander, IIslanderRegistry registry)
        {
            _relationships.Clear();

            bool any = false;
            foreach (var record in islander.Relationships.Records)
            {
                if (record.Friendship == FriendshipStage.Stranger &&
                    record.Conflict == ConflictStage.None) continue;

                // El protagonista tiene ficha en su agenda pero no está en el censo, así
                // que hay que nombrarlo aparte. Sin esto la fila se saltaba en silencio
                // y lo único que no salía en «con quién anda» era contigo.
                string who = record.OtherId == SocialIds.Player
                    ? PlayerName()
                    : registry.TryGet(record.OtherId, out var other)
                        ? other.Identity.ShortName
                        : null;

                if (who == null) continue;

                any = true;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var name = UiTheme.Body(who);
                name.style.width = 110;
                row.Add(name);
                row.Add(UiTheme.Chip(StatusOf(record), StatusColor(record)));
                _relationships.Add(row);
            }

            if (!any) _relationships.Add(UiTheme.Body("Todavía no conoce a nadie.", soft: true));
        }

        /// <summary>Cómo se llama el protagonista en la lista de un vecino.</summary>
        private static string PlayerName()
        {
            if (!ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var player)) return "Tú";

            string name = player.State?.DisplayName;
            return string.IsNullOrEmpty(name) ? "Tú" : $"{name} (tú)";
        }

        /// <summary>Una sola etiqueta por relación: manda lo más fuerte que esté pasando.</summary>
        private static string StatusOf(in RelationshipRecord record) => record.Romance switch
        {
            RomanceStage.Married => "casados",
            RomanceStage.Engaged => "prometidos",
            RomanceStage.Dating => "saliendo",
            RomanceStage.Crush => "le gusta",
            RomanceStage.Separated => "rotos",
            _ => record.Conflict switch
            {
                ConflictStage.Feud => "enemistados",
                ConflictStage.Quarrel => "reñidos",
                ConflictStage.Tension => "tirantes",
                _ => record.Friendship switch
                {
                    FriendshipStage.BestFriend => "inseparables",
                    FriendshipStage.CloseFriend => "buenos amigos",
                    FriendshipStage.Friend => "amigos",
                    _ => "se conocen",
                },
            },
        };

        private static Color StatusColor(in RelationshipRecord record)
        {
            if (record.Romance is RomanceStage.Married or RomanceStage.Engaged
                or RomanceStage.Dating or RomanceStage.Crush) return UiTheme.Mood;
            if (record.Conflict >= ConflictStage.Quarrel) return UiTheme.Critical;
            if (record.Conflict == ConflictStage.Tension) return UiTheme.Low;
            if (record.Friendship >= FriendshipStage.Friend) return UiTheme.Social;
            return UiTheme.InkSoft;
        }

        private static Color PriorityColor(RequestPriority priority) => priority switch
        {
            RequestPriority.Critical => UiTheme.Critical,
            RequestPriority.High => UiTheme.Low,
            RequestPriority.Normal => UiTheme.Accent,
            _ => UiTheme.InkSoft,
        };

        private static string KindName(RequestKind kind) => kind switch
        {
            RequestKind.Food => "comida",
            RequestKind.Object => "un objeto",
            RequestKind.Clothes => "ropa",
            RequestKind.Advice => "consejo",
            RequestKind.Favor => "un favor",
            RequestKind.Complaint => "una queja",
            RequestKind.SocialIntro => "conocer a alguien",
            RequestKind.Activity => "hacer algo",
            RequestKind.IslandBuilding => "mejorar la isla",
            RequestKind.Confession => "declararse",
            _ => "hacer las paces",
        };

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
