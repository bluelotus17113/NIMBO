using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Achievements
{
    /// <summary>
    /// Lo que la isla se ha ido apuntando de lo que has hecho.
    /// </summary>
    /// <remarks>
    /// Se enseñan todos, también los que están lejísimos, porque ver lo que aún no
    /// has hecho es la mitad de la gracia de una lista de logros. Los escondidos son
    /// la excepción: de esos se ve que existen y nada más, que si no se estropean.
    ///
    /// La lista se reconstruye al abrirla y no cada fotograma: son cuarenta y pico
    /// filas y nadie consigue un logro con la pantalla de logros delante.
    /// </remarks>
    public sealed class AchievementsPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly ScrollView _list;
        private readonly Label _headline;
        private IAchievementService _service;
        private AchievementKind? _filter;

        public AchievementsPanel()
        {
            Root = UiTheme.Card("logros");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 560;
            Root.style.maxHeight = Length.Percent(92);

            Root.Add(UiTheme.Header("Lo que llevas hecho", Hide));

            _headline = UiTheme.Body("", soft: true);
            _headline.style.marginBottom = 10;
            Root.Add(_headline);

            Root.Add(BuildFilters());

            _list = new ScrollView();
            UiTheme.StyleScroll(_list);
            _list.style.flexGrow = 1;
            Root.Add(_list);
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _service)) return;

            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        private VisualElement BuildFilters()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 8;

            Add("Todo", null);
            foreach (AchievementKind kind in System.Enum.GetValues(typeof(AchievementKind)))
                Add(KindName(kind), kind);

            return row;

            void Add(string text, AchievementKind? kind)
            {
                var chip = UiTheme.Secondary(text, () => { _filter = kind; Rebuild(); });
                chip.style.fontSize = 12;
                chip.style.paddingTop = chip.style.paddingBottom = 3;
                chip.style.paddingLeft = chip.style.paddingRight = 10;
                chip.style.marginRight = 4;
                chip.style.marginBottom = 4;
                row.Add(chip);
            }
        }

        private static string KindName(AchievementKind kind) => kind switch
        {
            AchievementKind.Life => "Vida",
            AchievementKind.Social => "Vecinos",
            AchievementKind.Home => "Casa",
            AchievementKind.Money => "Nimbos",
            AchievementKind.Play => "Juegos",
            AchievementKind.Island => "Isla",
            _ => "Rarezas",
        };

        private static Color KindColour(AchievementKind kind) => kind switch
        {
            AchievementKind.Life => UiTheme.Peach,
            AchievementKind.Social => UiTheme.Mint,
            AchievementKind.Home => UiTheme.Butter,
            AchievementKind.Money => UiTheme.PeachDeep,
            AchievementKind.Play => UiTheme.Sky,
            AchievementKind.Island => UiTheme.Sage,
            _ => UiTheme.Lavender,
        };

        private void Rebuild()
        {
            _list.Clear();
            if (_service == null) return;

            var catalog = _service.Catalog;
            _headline.text = $"{_service.UnlockedCount} de {catalog.Count} conseguidos";

            // Primero lo conseguido más reciente no: primero lo que está a punto.
            // Ver «te faltan dos» arriba del todo es lo que hace volver a jugar; una
            // lista ordenada por catálogo entierra eso entre lo imposible.
            var rows = new List<(AchievementDefinition def, AchievementProgress progress)>();
            for (int i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                if (_filter.HasValue && def.Kind != _filter.Value) continue;
                rows.Add((def, _service.ProgressOf(def.AchievementId)));
            }

            rows.Sort((a, b) =>
            {
                if (a.progress.Unlocked != b.progress.Unlocked)
                    return a.progress.Unlocked ? 1 : -1;
                return b.progress.Normalized.CompareTo(a.progress.Normalized);
            });

            for (int i = 0; i < rows.Count; i++)
                _list.Add(BuildRow(rows[i].def, rows[i].progress, i % 2 == 1));

            if (rows.Count == 0)
                _list.Add(UiTheme.Body("Nada por aquí todavía.", soft: true));
        }

        private VisualElement BuildRow(AchievementDefinition def, AchievementProgress progress,
                                       bool alternate)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 12;
            row.style.paddingTop = row.style.paddingBottom = 10;
            row.style.marginBottom = 4;
            if (alternate) row.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(row, UiTheme.Radius);

            // La marca de color a la izquierda hace de icono sin ser un icono: dice
            // de qué familia es de un vistazo y no depende de ninguna fuente.
            var mark = new VisualElement();
            mark.style.width = 10;
            mark.style.height = 40;
            mark.style.marginRight = 12;
            mark.style.backgroundColor = progress.Unlocked
                ? KindColour(def.Kind)
                : UiTheme.InkFaint;
            UiTheme.SetRadius(mark, UiTheme.RadiusPill);
            row.Add(mark);

            var text = new VisualElement();
            text.style.flexGrow = 1;
            text.style.marginRight = 8;

            bool veiled = def.Hidden && !progress.Unlocked;

            var name = UiTheme.Body(veiled ? "Algo que no te voy a contar" : def.DisplayName);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            if (!progress.Unlocked) name.style.color = UiTheme.InkSoft;
            text.Add(name);

            text.Add(UiTheme.Body(
                veiled ? "Se descubre solo, haciendo lo que haces." : def.Description,
                soft: true));

            // Barra solo en los que llevan cuenta: en uno de «pasó o no pasó» una
            // barra al 0 % no informa de nada y hace la lista más ruidosa.
            if (def.Goal > 1 && !progress.Unlocked && !veiled)
                text.Add(BuildBar(progress, KindColour(def.Kind)));

            row.Add(text);

            var tag = progress.Unlocked
                ? UiTheme.Pill("hecho", KindColour(def.Kind))
                : UiTheme.Pill($"{def.Reward}", UiTheme.CreamDeep);
            tag.style.marginRight = 0;
            row.Add(tag);

            return row;
        }

        private static VisualElement BuildBar(AchievementProgress progress, Color colour)
        {
            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.alignItems = Align.Center;
            wrap.style.marginTop = 6;

            var track = new VisualElement();
            track.style.flexGrow = 1;
            track.style.height = 8;
            track.style.backgroundColor = UiTheme.InkFaint;
            track.style.overflow = Overflow.Hidden;
            UiTheme.SetRadius(track, 4);

            var fill = new VisualElement();
            fill.style.height = 8;
            fill.style.width = Length.Percent(Mathf.Clamp01(progress.Normalized) * 100f);
            fill.style.backgroundColor = colour;
            UiTheme.SetRadius(fill, 4);
            track.Add(fill);
            wrap.Add(track);

            var count = UiTheme.Body($"{progress.Current}/{progress.Goal}", soft: true);
            count.style.marginLeft = 8;
            count.style.fontSize = 12;
            wrap.Add(count);

            return wrap;
        }
    }
}
