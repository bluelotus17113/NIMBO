using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine.UIElements;

namespace Nimbo.UI.Village
{
    /// <summary>
    /// Montar una fiesta en la aldea (§15.3).
    /// </summary>
    /// <remarks>
    /// Es la mejor herramienta social que tiene el jugador y la única que no toca la
    /// autonomía de nadie: no empareja a dos personas, monta el sitio donde puedan
    /// conocerse. De cada fiesta sale como mucho un flechazo, el de la pareja que mejor
    /// pega, y sale solo.
    ///
    /// Se enseñan todas, incluidas las que aún no puedes pagar o desbloquear, con el
    /// motivo escrito. Un calendario que crece solo sin decir por qué es un calendario
    /// que no se mira.
    /// </remarks>
    public sealed class EventsPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly ScrollView _list;
        private readonly Label _headline;

        public EventsPanel()
        {
            Root = UiTheme.Card("fiestas");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 520;
            Root.style.maxHeight = Length.Percent(92);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Organizar algo"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _headline = UiTheme.Body("", soft: true);
            _headline.style.marginBottom = 10;
            _headline.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_headline);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            Root.Add(_list);
        }

        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        public void Rebuild()
        {
            _list.Clear();

            if (!ServiceRegistry.TryGet<IVillageEvents>(out var events))
            {
                _headline.text = "Esto no está disponible.";
                return;
            }

            _headline.text = string.IsNullOrEmpty(events.ActiveEventId)
                ? "En una fiesta la gente coincide, y de ahí sale algo."
                : "Ya hay algo en marcha. Cuando acabe, otra cosa.";

            var hostable = events.Hostable;
            for (int i = 0; i < hostable.Count; i++)
                _list.Add(Row(events, hostable[i]));
        }

        private VisualElement Row(IVillageEvents events, in HostableEvent fiesta)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 12;
            row.style.paddingTop = row.style.paddingBottom = 10;
            row.style.marginBottom = 4;
            row.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(row, UiTheme.Radius);

            var text = new VisualElement();
            text.style.flexGrow = 1;
            text.style.marginRight = 8;

            var name = UiTheme.Body(fiesta.DisplayName);
            name.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            text.Add(name);

            var desc = UiTheme.Body(fiesta.Description, soft: true);
            desc.style.whiteSpace = WhiteSpace.Normal;
            text.Add(desc);
            text.Add(UiTheme.Body($"{fiesta.Cost} nimbos", soft: true));
            row.Add(text);

            var verdict = events.CanHost(fiesta.Id);
            if (verdict == HostRefusal.Ok)
            {
                string id = fiesta.Id;
                var button = UiTheme.Action("Montarla", () =>
                {
                    events.Host(id);
                    Rebuild();
                });
                row.Add(button);
                return row;
            }

            var locked = UiTheme.Chip(Short(verdict), UiTheme.InkFaint);
            locked.tooltip = Excuse(verdict, fiesta);
            row.Add(locked);
            return row;
        }

        private static string Short(HostRefusal refusal) => refusal switch
        {
            HostRefusal.NotUnlocked => "aún no",
            HostRefusal.NotEnoughCoins => "no llega",
            HostRefusal.AlreadyRunning => "hay algo puesto",
            HostRefusal.NotYet => "la isla no da",
            _ => "no",
        };

        private static string Excuse(HostRefusal refusal, in HostableEvent fiesta) => refusal switch
        {
            HostRefusal.NotUnlocked =>
                Gates.Short(fiesta.IsFestival ? Unlock.HostFestivals : Unlock.HostEvents) +
                " para organizar esto.",
            HostRefusal.NotEnoughCoins => $"Cuesta {fiesta.Cost} nimbos.",
            HostRefusal.AlreadyRunning => "Ya hay algo en marcha. Una cosa cada vez.",
            HostRefusal.NotYet => "Faltan vecinos, nivel de isla o el sitio donde hacerlo.",
            _ => "Ahora mismo no.",
        };
    }
}
