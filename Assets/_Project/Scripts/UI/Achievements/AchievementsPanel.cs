using System.Collections.Generic;
using System.Text;
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
        private readonly VisualElement _filterRow;
        private readonly Label _headline;
        private IAchievementService _service;
        private AchievementKind? _filter;

        /// <summary>
        /// Lo que mandó la última vez que se levantó la lista. El ciclo lento de
        /// UiRoot llama a <see cref="Refresh"/> con el panel abierto; sin firma,
        /// cuarenta y pico filas se reconstruirían cada 0,4 s aunque nadie consiguiera
        /// nada — el derroche que hizo que la ficha se comiera sus propios mensajes.
        /// </summary>
        private string _signature;

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

            // La fila se reconstruye en cada cambio de filtro para que el chip activo
            // quede pintado: un filtro sin estado visible obliga a pulsar para saber
            // qué lista estás mirando.
            _filterRow = new VisualElement { name = "filtros-logros" };
            _filterRow.style.flexDirection = FlexDirection.Row;
            _filterRow.style.flexWrap = Wrap.Wrap;
            _filterRow.style.marginBottom = 8;
            Root.Add(_filterRow);
            BuildFilters();

            _list = new ScrollView();
            UiTheme.StyleScroll(_list);
            _list.style.flexGrow = 1;
            Root.Add(_list);
        }

        /// <summary>Cambia el filtro y repinta la fila para que se vea cuál está activo.</summary>
        /// <remarks>
        /// **Público porque lo conduce una prueba**, no por un atajo futuro. El comentario
        /// original decía «y cualquier atajo futuro que quiera dejar la lista ya filtrada
        /// al abrirla», un uso que nadie ha pedido; el verificador marcó con razón que eso
        /// no justifica ampliar la superficie del panel, y yo lo cerré sin mirar quién
        /// llamaba. `FiltrosVisiblesTests.cs:41,57,59` llama, y el proyecto entero dejó de
        /// compilar — Unity sale sin ejecutar una prueba y sin escribir XML, que es el
        /// fallo silencioso de siempre con otra cara.
        ///
        /// La lección va aquí y no en el mensaje del commit: **buscar llamadores incluye
        /// `Tests/`**. Un `rg` sobre `Scripts/` da cero y parece que nadie lo usa.
        /// </remarks>
        public void SetFilter(AchievementKind? kind)
        {
            _filter = kind;
            BuildFilters();
            Rebuild();
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _service)) return;

            Root.style.display = DisplayStyle.Flex;
            Rebuild();

            // Lo pintado queda firmado: si nadie consigue nada, el primer Refresh no
            // reconstruye por sorpresa lo que acaba de levantarse.
            _signature = Firma();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>
        /// Refresco barato: firma el progreso de todo el catálogo y solo reconstruye
        /// si algo cambió de verdad.
        /// </summary>
        /// <remarks>
        /// Un logro se consigue con la pantalla abierta más de lo que parece —el
        /// propio juego sigue corriendo detrás— y la cabecera «N de M» se quedaba
        /// vieja para siempre. El filtro entra en la firma porque cambia lo pintado,
        /// aunque sus clics ya reconstruyan por su cuenta.
        /// </remarks>
        public void Refresh()
        {
            if (_service == null && !ServiceRegistry.TryGet(out _service))
            {
                _signature = null;
                return;
            }

            string candidata = Firma();
            if (candidata == _signature) return;
            _signature = candidata;
            Rebuild();
        }

        /// <summary>Lo que dicta el estado del servicio y del filtro, en una cadena.</summary>
        private string Firma()
        {
            if (_service == null) return "";

            var catalog = _service.Catalog;
            var firma = new StringBuilder()
                .Append(_filter).Append('|').Append(_service.UnlockedCount);

            for (int i = 0; i < catalog.Count; i++)
            {
                var progress = _service.ProgressOf(catalog[i].AchievementId);
                firma.Append('|').Append(progress.Current)
                     .Append('/').Append(progress.Goal)
                     .Append(progress.Unlocked ? '+' : '.');
            }

            return firma.ToString();
        }

        /// <remarks>
        /// Mismo patrón que las pestañas del crafteo (CraftPanel.BuildStations): la fila
        /// entera se vuelve a construir y el activo lleva fondo melocotón. El inline
        /// pisa el <c>:hover</c> del chip activo a sabiendas —ya está marcado, no hace
        /// falta que la hoja lo vuelva a decir—; los inactivos conservan los suyos.
        /// </remarks>
        private void BuildFilters()
        {
            _filterRow.Clear();
            Add("Todo", null);
            foreach (AchievementKind kind in System.Enum.GetValues(typeof(AchievementKind)))
                Add(KindName(kind), kind);

            void Add(string text, AchievementKind? kind)
            {
                var chip = UiTheme.Secondary(text, () => SetFilter(kind));
                chip.style.fontSize = 12;
                chip.style.paddingTop = chip.style.paddingBottom = 3;
                chip.style.paddingLeft = chip.style.paddingRight = 10;
                chip.style.marginRight = 4;
                chip.style.marginBottom = 4;
                if (kind == _filter) chip.style.backgroundColor = UiTheme.Peach;
                _filterRow.Add(chip);
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
