using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Chronicle
{
    /// <summary>
    /// La crónica de la aldea: lo que ha ido pasando entre los vecinos, por días.
    /// </summary>
    /// <remarks>
    /// Va **en el menú y no en el mundo**, y eso es una decisión, no una comodidad.
    /// Leer la vida social es un gesto de sentarse a mirar; un cartel que salta mientras
    /// riegas interrumpe justo lo que estabas haciendo y encima obliga a leer rápido. Lo
    /// del mundo —las burbujas, las caras— es refuerzo; el canal principal es esto.
    ///
    /// Y existe porque la aldea vivía a ciegas: `RomanceStageChanged`, `BabyBorn`,
    /// `WeddingAnnounced` y `ConflictStageChanged` se publicaban y **no los escuchaba
    /// nadie**. Dos vecinos podían conocerse, salir, prometerse y casarse sin que el
    /// jugador tuviera por dónde enterarse.
    ///
    /// Lo más reciente arriba. Una crónica que empieza por el día 1 obliga a bajar hasta
    /// el final para saber qué pasó anoche, que es lo único que se viene a mirar.
    /// </remarks>
    public sealed class ChroniclePanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly ScrollView _list;
        private readonly Label _headline;

        /// <summary>
        /// Lo que mandó la última vez que se levantó la lista. El ciclo lento de
        /// UiRoot llama a <see cref="Refresh"/> con el panel abierto; sin firma se
        /// reconstruiría cada 0,4 s para enseñar siempre lo mismo, que es justo lo
        /// que hacía que la ficha se borrara sus propios mensajes.
        /// </summary>
        private string _signature;

        public ChroniclePanel()
        {
            Root = UiTheme.Card("cronica");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 520;
            Root.style.maxHeight = Length.Percent(92);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Lo que se cuenta"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _headline = UiTheme.Body("", soft: true);
            _headline.style.marginBottom = 10;
            Root.Add(_headline);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            Root.Add(_list);
        }

        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            Rebuild();

            // Lo pintado queda firmado: si no entra nada nuevo, el primer Refresh no
            // reconstruye por sorpresa lo que acaba de levantarse.
            _signature = Firma();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>
        /// Refresco barato: firma el número de líneas y el día, y solo reconstruye
        /// si cambió algo.
        /// </summary>
        /// <remarks>
        /// Dos cosas dejan la crónica vieja mientras se mira: que entre una línea
        /// nueva y que amanezca —lo que ayer decía «Hoy» tiene que decir «Ayer»
        /// aunque no haya pasado nada—. Por eso el día entra en la firma junto al
        /// recuento. La última línea va también, por si algún día la crónica deja de
        /// ser de solo añadir: es lo único que distingue dos listas de igual longitud.
        /// </remarks>
        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<IChronicleService>(out _))
            {
                _signature = null;
                return;
            }

            string candidata = Firma();
            if (candidata == _signature) return;
            _signature = candidata;
            Rebuild();
        }

        /// <summary>Lo que dicta el estado del servicio y del reloj, en una cadena.</summary>
        private string Firma()
        {
            if (!ServiceRegistry.TryGet<IChronicleService>(out var chronicle))
                return "";

            var entries = chronicle.Entries;
            int today = ServiceRegistry.TryGet<GameClock>(out var clock) ? clock.Day : 0;

            string firma = $"{entries.Count}|{today}";
            if (entries.Count > 0)
            {
                var ultima = entries[entries.Count - 1];
                firma += $"|{ultima.Day}|{ultima.Text}";
            }

            return firma;
        }

        private void Rebuild()
        {
            _list.Clear();

            if (!ServiceRegistry.TryGet<IChronicleService>(out var chronicle))
            {
                _headline.text = "";
                _list.Add(UiTheme.Body("La crónica no está disponible.", soft: true));
                return;
            }

            var entries = chronicle.Entries;
            if (entries.Count == 0)
            {
                _headline.text = "";
                _list.Add(UiTheme.Body(
                    "Todavía no ha pasado nada digno de contarse. Dale unos días.", soft: true));
                return;
            }

            int today = ServiceRegistry.TryGet<GameClock>(out var clock) ? clock.Day : 0;
            _headline.text = entries.Count == 1
                ? "Una cosa que contar"
                : $"{entries.Count} cosas, de lo más reciente a lo más viejo";

            // De atrás hacia delante, agrupando por día: la cabecera se pone al cambiar
            // de día, así que recorrer al revés da los días en orden decreciente solo.
            int lastDay = int.MinValue;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry.Day != lastDay)
                {
                    lastDay = entry.Day;
                    _list.Add(DayHeader(entry.Day, today));
                }
                _list.Add(Line(entry.Text));
            }
        }

        /// <summary>
        /// La cabecera de un día. «Hoy» y «ayer» en palabras, porque leer «día 47» no le
        /// dice a nadie si eso fue esta mañana o hace tres semanas.
        /// </summary>
        private static VisualElement DayHeader(int day, int today)
        {
            string text =
                today <= 0 ? $"Día {day}" :
                day == today ? "Hoy" :
                day == today - 1 ? "Ayer" :
                today - day < 7 ? $"Hace {today - day} días" :
                                  $"Día {day}";

            var label = UiTheme.Body(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 12;
            label.style.marginBottom = 4;
            label.style.color = UiTheme.InkSoft;
            return label;
        }

        private static VisualElement Line(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.paddingLeft = row.style.paddingRight = 12;
            row.style.paddingTop = row.style.paddingBottom = 8;
            row.style.marginBottom = 3;
            row.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(row, UiTheme.Radius);

            // Una marca de color a la izquierda, del tono que le pega a lo que cuenta.
            // Hace de icono sin ser un icono y sin depender de ninguna fuente, igual que
            // en la pantalla de logros.
            var mark = new VisualElement();
            mark.style.width = 6;
            mark.style.minHeight = 18;
            mark.style.marginRight = 10;
            mark.style.marginTop = 2;
            mark.style.backgroundColor = ColourOf(text);
            UiTheme.SetRadius(mark, UiTheme.RadiusPill);
            row.Add(mark);

            var label = UiTheme.Body(text);
            label.style.flexGrow = 1;
            label.style.whiteSpace = WhiteSpace.Normal;
            row.Add(label);

            return row;
        }

        /// <summary>
        /// El color de la línea, sacado de lo que dice.
        /// </summary>
        /// <remarks>
        /// Mirar el texto es feo y lo sé. La alternativa era guardar un tipo en cada
        /// línea, y eso obliga a tocar el `ChronicleEntry`, el guardado y las seis
        /// plantillas del tablón para pintar una barrita de seis píxeles. Cuando la
        /// crónica tenga filtros por tipo habrá que hacerlo bien; hoy no los tiene.
        /// </remarks>
        private static Color ColourOf(string text)
        {
            if (text.Contains("casan") || text.Contains("casado") || text.Contains("Boda")
                || text.Contains("boda") || text.Contains("matrimonio"))
                return UiTheme.Rose;

            if (text.Contains("nacido") || text.Contains("bienvenid") || text.Contains("padres"))
                return UiTheme.Butter;

            if (text.Contains("discut") || text.Contains("pelea") || text.Contains("morros")
                || text.Contains("enemistad") || text.Contains("tirantez")
                || text.Contains("ni se hablan"))
                return UiTheme.PeachDeep;

            if (text.Contains("pareja") || text.Contains("gusta") || text.Contains("coladit")
                || text.Contains("prometid") || text.Contains("mano"))
                return UiTheme.Mint;

            if (text.Contains("nivel") || text.Contains("Legendari"))
                return UiTheme.Sky;

            return UiTheme.Lavender;
        }
    }
}
