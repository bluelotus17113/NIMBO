using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI
{
    /// <summary>
    /// La paleta y los estilos de toda la interfaz, en un sitio.
    /// Cada color sale de <c>Docs/Contratos/estilo_ui.md</c> y de ningún otro lado.
    /// </summary>
    /// <remarks>
    /// La hoja de estilos se construye en código y no en un <c>.uss</c> a propósito:
    /// un fichero de texto plano lo puede editar cualquiera de los agentes en paralelo,
    /// y un asset de Unity, no. El coste es escribir los estilos a mano; la ventaja es
    /// que la interfaz entera cabe en el mismo flujo de trabajo que el resto.
    /// </remarks>
    public static class UiTheme
    {
        // ═══════════════════════════════════════════════════════════════════
        //  Fondos y superficies — Docs/Contratos/estilo_ui.md §1
        // ═══════════════════════════════════════════════════════════════════

        public static readonly Color Cream     = (Color)new Color32(0xFF, 0xF8, 0xF0, 255);
        public static readonly Color CreamDeep = (Color)new Color32(0xF6, 0xEC, 0xE0, 255);
        public static readonly Color Sky       = (Color)new Color32(0xBD, 0xE3, 0xF2, 255);
        public static readonly Color SkySoft   = (Color)new Color32(0xDC, 0xF0, 0xF7, 255);

        // ═══════════════════════════════════════════════════════════════════
        //  Tinta — §1
        // ═══════════════════════════════════════════════════════════════════

        public static readonly Color Ink      = (Color)new Color32(0x5C, 0x4A, 0x42, 255);
        public static readonly Color InkSoft  = (Color)new Color32(0x9A, 0x8B, 0x82, 255);
        public static readonly Color InkFaint = (Color)new Color32(0xC9, 0xBD, 0xB4, 255);

        // ═══════════════════════════════════════════════════════════════════
        //  Acentos pastel — §1
        // ═══════════════════════════════════════════════════════════════════

        public static readonly Color Peach    = (Color)new Color32(0xFF, 0xC7, 0xA8, 255);
        public static readonly Color PeachDeep = (Color)new Color32(0xF0, 0xA4, 0x7D, 255);
        public static readonly Color Mint     = (Color)new Color32(0xB8, 0xE6, 0xC8, 255);
        public static readonly Color Lavender = (Color)new Color32(0xD6, 0xC7, 0xF0, 255);
        public static readonly Color Butter   = (Color)new Color32(0xFC, 0xE8, 0xA8, 255);
        public static readonly Color Rose     = (Color)new Color32(0xF5, 0xC0, 0xCB, 255);
        public static readonly Color Sage     = (Color)new Color32(0xCB, 0xDD, 0xB4, 255);

        // ═══════════════════════════════════════════════════════════════════
        //  Compatibilidad con las pantallas que ya existen
        //  (referencian estos nombres y no puedo tocarlas)
        // ═══════════════════════════════════════════════════════════════════

        public static readonly Color Accent     = Peach;
        public static readonly Color AccentDeep = PeachDeep;
        public static readonly Color Panel      = Cream;
        public static readonly Color PanelDark  = CreamDeep;
        public static readonly Color Cloud      = SkySoft;

        // Una barra por necesidad, con su color propio para leerlas de un vistazo.
        public static readonly Color Hunger  = Peach;
        public static readonly Color Energy  = Butter;
        public static readonly Color Social  = Mint;
        public static readonly Color Hygiene = Sky;
        public static readonly Color Mood    = Lavender;

        public static readonly Color Critical = Rose;
        public static readonly Color Low      = Butter;

        // ═══════════════════════════════════════════════════════════════════
        //  Superficies que reaccionan y velo — §1
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Crema un punto más hundido: el ratón encima o el dedo apretando.</summary>
        public static readonly Color CreamPress = (Color)new Color32(0xEA, 0xDF, 0xD1, 255);

        /// <summary>
        /// El velo que se echa sobre la isla cuando se abre el menú. Es tinta, no negro:
        /// apaga el 3D sin ensuciarlo de gris.
        /// </summary>
        public static readonly Color Scrim = new Color(0.23f, 0.18f, 0.16f, 0.55f);

        // ═══════════════════════════════════════════════════════════════════
        //  Radios — §2
        // ═══════════════════════════════════════════════════════════════════

        public const int RadiusPanel = 28;
        public const int RadiusCard  = 18;
        public const int RadiusPill  = 999;

        public const int Radius = 14; // el que ya usaban las pantallas viejas
        public const int Gap    = 10;

        // ═══════════════════════════════════════════════════════════════════
        //  Escala de espacio — §3
        //  Todo margen y todo relleno sale de aquí. Media docena de números en
        //  vez de un valor inventado por pantalla: es lo que hace que quince
        //  pantallas distintas parezcan la misma mano.
        // ═══════════════════════════════════════════════════════════════════

        public const int SpaceXS = 4;
        public const int SpaceS  = 8;
        public const int SpaceM  = 12;
        public const int SpaceL  = 16;
        public const int SpaceXL = 24;
        public const int Space2XL = 32;
        //  Clases de los botones — las pinta NimboRuntimeTheme.tss
        // ═══════════════════════════════════════════════════════════════════

        public const string ClassAction    = "nimbo-btn-action";
        public const string ClassSecondary = "nimbo-btn-secondary";
        public const string ClassDisabled  = "nimbo-btn-disabled";

        // ═══════════════════════════════════════════════════════════════════
        //  Métodos públicos — mismas firmas que antes
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>El color de una barra según lo llena que esté. El rosa solo en crítico.</summary>
        public static Color BarColor(Color baseColor, float normalized) =>
            normalized <= 0.15f ? Critical :
            normalized <= 0.35f ? Low :
            baseColor;

        /// <summary>Un panel con esquinas redondeadas, el bloque básico de todo.</summary>
        public static VisualElement Card(string name = null)
        {
            var card = new VisualElement { name = name };
            var s = card.style;
            s.backgroundColor = Cream;
            s.paddingLeft = s.paddingRight = s.paddingTop = s.paddingBottom = 16;
            s.marginBottom = Gap;
            SetRadius(card, RadiusCard);
            return card;
        }

        public static void SetRadius(VisualElement element, float radius)
        {
            var s = element.style;
            s.borderTopLeftRadius = s.borderTopRightRadius =
                s.borderBottomLeftRadius = s.borderBottomRightRadius = radius;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Movimiento y estados — §7
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Deja que el elemento llegue a su color, su tamaño y su sitio en vez de
        /// aparecer ya puesto. Es lo único que distingue una interfaz que responde de
        /// una que da saltos: nada se mueve más de lo que dura un parpadeo.
        /// </summary>
        public static void Animate(VisualElement element, int milliseconds = 140)
        {
            var s = element.style;
            s.transitionProperty = new List<StylePropertyName>
            {
                "background-color", "color", "opacity", "scale", "translate",
            };
            s.transitionDuration = new List<TimeValue>
            {
                new TimeValue(milliseconds, TimeUnit.Millisecond),
            };
            s.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseOutCubic),
            };
        }

        /// <summary>El ratón encima cambia el fondo, y al salir vuelve.</summary>
        public static void Hoverable(VisualElement element, Color rest, Color hover)
        {
            element.RegisterCallback<MouseEnterEvent>(_ => element.style.backgroundColor = hover);
            element.RegisterCallback<MouseLeaveEvent>(_ => element.style.backgroundColor = rest);
        }

        /// <summary>
        /// Al apretar, el elemento se hunde un poco. Sin esto un botón de UI Toolkit no
        /// acusa el clic de ninguna manera y parece que la pulsación se ha perdido.
        /// </summary>
        public static void Pressable(VisualElement element, float scale = 0.96f)
        {
            var small = new Scale(new Vector3(scale, scale, 1f));
            var normal = new Scale(Vector3.one);

            element.RegisterCallback<PointerDownEvent>(_ => element.style.scale = small);
            element.RegisterCallback<PointerUpEvent>(_ => element.style.scale = normal);
            element.RegisterCallback<PointerLeaveEvent>(_ => element.style.scale = normal);
        }

        /// <summary>Las tres cosas juntas: es lo que lleva cualquier cosa pulsable.</summary>
        private static void MakeInteractive(VisualElement element, Color rest, Color hover)
        {
            Animate(element, 120);
            Hoverable(element, rest, hover);
            Pressable(element);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Piezas compartidas
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// La cabecera de un panel: su título y la cruz de cerrar.
        /// </summary>
        /// <remarks>
        /// Existe para que las ocho pantallas la tengan idéntica —antes cada una se
        /// pintaba la suya a mano y se iban separando— y para que el menú pueda
        /// esconder la cruz cuando el panel va dentro de él y no suelto por ahí: la
        /// busca por el nombre <c>cerrar</c>.
        /// </remarks>
        public static VisualElement Header(string title, System.Action onClose)
        {
            var head = new VisualElement { name = "cabecera" };
            var s = head.style;
            s.flexDirection = FlexDirection.Row;
            s.justifyContent = Justify.SpaceBetween;
            s.alignItems = Align.Center;
            s.marginBottom = SpaceM;

            head.Add(Title(title));
            if (onClose != null) head.Add(Close(onClose));
            return head;
        }

        /// <summary>La cruz de cerrar: redonda, discreta y siempre en el mismo sitio.</summary>
        public static Button Close(System.Action onClose)
        {
            var button = new Button(onClose) { text = "×", name = "cerrar" };
            var s = button.style;
            s.width = s.height = 30;
            s.backgroundColor = CreamDeep;
            s.color = InkSoft;
            s.fontSize = 20;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingTop = s.paddingBottom = s.paddingLeft = s.paddingRight = 0;
            s.marginLeft = s.marginRight = s.marginTop = s.marginBottom = 0;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 0;
            s.unityTextAlign = TextAnchor.MiddleCenter;
            SetRadius(button, RadiusPill);
            MakeInteractive(button, CreamDeep, CreamPress);
            return button;
        }

        /// <summary>
        /// Un punto de color: el icono de una sección. Formas geométricas de la paleta,
        /// que es lo que manda el contrato — un emoji se ve distinto en cada sistema.
        /// </summary>
        public static VisualElement Dot(Color color, int size = 12, bool round = true)
        {
            var dot = new VisualElement();
            var s = dot.style;
            s.width = s.height = size;
            s.backgroundColor = color;
            s.flexShrink = 0;
            SetRadius(dot, round ? RadiusPill : 4);
            return dot;
        }

        public static Label Title(string text)
        {
            var label = new Label(text);
            label.style.color = Ink;
            label.style.fontSize = 20;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6;
            return label;
        }

        public static Label Body(string text, bool soft = false)
        {
            var label = new Label(text);
            label.style.color = soft ? InkSoft : Ink;
            label.style.fontSize = soft ? 13 : 14;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        /// <summary>El botón principal: fondo melocotón, texto tinta. Uno por pantalla.</summary>
        /// <remarks>
        /// El fondo y el borde NO se fijan aquí a propósito. En UI Toolkit un estilo
        /// inline gana a cualquier pseudoclase de la hoja de estilos, así que pintar
        /// aquí el fondo mataría el <c>:hover</c>, el <c>:active</c> y el
        /// <c>:focus</c> que define <c>NimboRuntimeTheme.tss</c> — y con ello los
        /// estados de todos los botones del juego, porque todos salen de esta
        /// fábrica. La clase manda; lo que no cambia con el estado (texto, relleno,
        /// radio) sí puede quedar inline.
        /// </remarks>
        public static Button Action(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList(ClassAction);
            var s = button.style;
            s.color = Ink;
            s.fontSize = 15;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingTop = s.paddingBottom = 8;
            s.paddingLeft = s.paddingRight = 16;
            s.marginLeft = s.marginRight = 0;
            SetRadius(button, Radius);
            MakeInteractive(button, Peach, PeachDeep);
            return button;
        }

        /// <summary>
        /// Una barra de necesidad con su etiqueta. Devuelve el relleno para poder
        /// actualizarlo sin reconstruir la fila entera en cada fotograma.
        /// </summary>
        public static VisualElement NeedBar(string label, Color color, out VisualElement fill)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;

            var name = new Label(label);
            name.style.color = InkSoft;
            name.style.fontSize = 13;
            name.style.width = 74;
            row.Add(name);

            var track = new VisualElement();
            track.style.flexGrow = 1;
            track.style.height = 12;
            track.style.backgroundColor = InkFaint;
            SetRadius(track, 6);
            track.style.overflow = Overflow.Hidden;

            fill = new VisualElement();
            fill.style.height = 12;
            fill.style.width = Length.Percent(50);
            fill.style.backgroundColor = color;
            SetRadius(fill, 6);
            track.Add(fill);

            row.Add(track);
            return row;
        }

        /// <summary>
        /// Una etiqueta pequeña de color, para el tipo de personalidad o el estado.
        /// </summary>
        public static Label Chip(string text, Color color)
        {
            var chip = new Label(text);
            var s = chip.style;
            s.backgroundColor = color;
            s.color = Ink;
            s.fontSize = 12;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingLeft = s.paddingRight = 10;
            s.paddingTop = s.paddingBottom = 4;
            s.marginRight = 6;
            SetRadius(chip, RadiusPill);
            return chip;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Nuevos métodos — encargo §1
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Botón secundario: fondo crema profundo, texto tinta. El resto de las acciones.</summary>
        /// <remarks>
        /// Mismo porqué que <see cref="Action"/>: el fondo lo pone la clase en
        /// <c>NimboRuntimeTheme.tss</c>, no el inline, o no habrá estados.
        /// </remarks>
        public static Button Secondary(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList(ClassSecondary);
            var s = button.style;
            s.color = Ink;
            s.fontSize = 15;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingTop = s.paddingBottom = 8;
            s.paddingLeft = s.paddingRight = 16;
            s.marginLeft = s.marginRight = 0;
            SetRadius(button, Radius);
            MakeInteractive(button, CreamDeep, CreamPress);
            return button;
        }

        /// <summary>
        /// Botón apagado: se queda en su sitio pero no se puede pulsar.
        /// Ver lo que aún no puedes hacer es información, y esconderlo la borra.
        /// </summary>
        /// <remarks>
        /// Lleva clase propia y no las de los botones vivos: aunque hoy un elemento
        /// deshabilitado no recibe puntero ni foco, si algún día Unity cambiara eso,
        /// un botón apagado con clase de vivo volvería a prometer que se le puede
        /// pulsar. El texto apagado sí queda inline porque ninguna pseudoclase lo
        /// toca — y es lo que hace que se lea como apagado.
        /// </remarks>
        public static Button Disabled(string text)
        {
            var button = new Button() { text = text };
            button.AddToClassList(ClassDisabled);
            var s = button.style;
            s.color = InkFaint;
            s.fontSize = 15;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingTop = s.paddingBottom = 8;
            s.paddingLeft = s.paddingRight = 16;
            s.marginLeft = s.marginRight = 0;
            SetRadius(button, Radius);
            button.SetEnabled(false);
            return button;
        }

        /// <summary>
        /// Una fila de lista. Si <paramref name="alternate"/> es true, lleva fondo
        /// <c>CreamDeep</c> para separar visualmente sin bordes.
        /// </summary>
        public static VisualElement Row(bool alternate)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 12;
            row.style.paddingTop = row.style.paddingBottom = 8;
            if (alternate) row.style.backgroundColor = CreamDeep;
            return row;
        }

        /// <summary>
        /// Una cápsula de radio completo. Para contadores, etiquetas y estados.
        /// </summary>
        public static Label Pill(string text, Color color)
        {
            var pill = new Label(text);
            var s = pill.style;
            s.backgroundColor = color;
            s.color = Ink;
            s.fontSize = 12;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingLeft = s.paddingRight = 12;
            s.paddingTop = s.paddingBottom = 4;
            s.marginRight = 6;
            SetRadius(pill, RadiusPill);
            return pill;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Piezas del menú — §8
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Deja la barra de desplazamiento en la paleta del juego.
        /// </summary>
        /// <remarks>
        /// La que trae UI Toolkit de fábrica es gris de editor, con sus dos flechitas y
        /// su borde: en una pantalla de crema y melocotón canta como una uña rota. Y no
        /// se puede arreglar con estilos sueltos porque las piezas de dentro tienen sus
        /// propios nombres, así que hay que ir a buscarlas una a una.
        /// </remarks>
        /// <summary>Lo ancha que es una barra de desplazamiento.</summary>
        private const int ScrollThickness = 8;

        public static void StyleScroll(ScrollView scroll)
        {
            scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            // Estas listas son de arriba abajo y de nada más. Sin esto aparecía una
            // barra horizontal cruzando el pie del panel por unos píxeles de sobra que
            // nadie va a querer ver nunca.
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Dress(scroll.verticalScroller);

            static void Dress(Scroller scroller)
            {
                if (scroller == null) return;

                // Las flechas de los extremos sobran: nadie desplaza una lista de dos
                // en dos píxeles, y ocupan más que la barra entera.
                scroller.lowButton.style.display = DisplayStyle.None;
                scroller.highButton.style.display = DisplayStyle.None;

                var slider = scroller.slider;
                if (slider == null) return;

                slider.style.marginLeft = slider.style.marginRight = 2;
                slider.style.width = ScrollThickness;

                var tracker = slider.Q("unity-tracker");
                if (tracker != null)
                {
                    tracker.style.backgroundColor = CreamDeep;
                    tracker.style.borderTopWidth = tracker.style.borderBottomWidth =
                        tracker.style.borderLeftWidth = tracker.style.borderRightWidth = 0;

                    // Radio a la mitad del ancho y no el de cápsula: 999 sobre algo de
                    // diez de ancho y setecientos de alto no da una barra redondeada,
                    // da un huso que cruza el panel de arriba abajo.
                    SetRadius(tracker, ScrollThickness * 0.5f);
                }

                var dragger = slider.Q("unity-dragger");
                if (dragger != null)
                {
                    dragger.style.backgroundColor = InkFaint;
                    dragger.style.borderTopWidth = dragger.style.borderBottomWidth =
                        dragger.style.borderLeftWidth = dragger.style.borderRightWidth = 0;
                    dragger.style.width = ScrollThickness;
                    SetRadius(dragger, ScrollThickness * 0.5f);
                    Animate(dragger, 120);
                    Hoverable(dragger, InkFaint, InkSoft);
                }
            }
        }

        /// <summary>Texto de apoyo pequeño: la tecla de un atajo, una aclaración.</summary>
        public static Label Caption(string text)
        {
            var label = new Label(text);
            label.style.color = InkFaint;
            label.style.fontSize = 12;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        /// <summary>
        /// La ventana del menú: crema, muy redonda y con lo que se salga recortado.
        /// </summary>
        public static VisualElement Window(string name = null)
        {
            var window = new VisualElement { name = name };
            var s = window.style;
            s.backgroundColor = Cream;
            s.flexDirection = FlexDirection.Row;
            s.overflow = Overflow.Hidden;
            SetRadius(window, RadiusPanel);
            return window;
        }

        /// <summary>
        /// Una pestaña de la columna del menú: su punto de color y su nombre.
        /// </summary>
        /// <remarks>
        /// No es un <c>Button</c> porque un botón de UI Toolkit dibuja su propio texto y
        /// aquí hacen falta dos cosas dentro —el punto y el nombre—, así que se monta a
        /// mano sobre un elemento normal que escucha el clic.
        /// </remarks>
        public static VisualElement RailTab(string label, Color tone, System.Action onClick,
                                            out Label caption)
        {
            var tab = new VisualElement { name = $"pestana-{label}" };
            var s = tab.style;
            s.flexDirection = FlexDirection.Row;
            s.alignItems = Align.Center;
            s.paddingLeft = s.paddingRight = SpaceM;
            s.paddingTop = s.paddingBottom = 10;
            s.marginBottom = SpaceXS;
            s.backgroundColor = Color.clear;
            SetRadius(tab, Radius);
            Animate(tab, 120);
            Pressable(tab, 0.98f);

            // El ratón encima solo aclara las que no están elegidas: la elegida ya está
            // en crema y aclararla más la haría parpadear al pasar por encima.
            tab.userData = false;
            tab.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!(bool)tab.userData) tab.style.backgroundColor = CreamPress;
            });
            tab.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!(bool)tab.userData) tab.style.backgroundColor = Color.clear;
            });

            tab.Add(Dot(tone));

            caption = new Label(label);
            caption.style.color = InkSoft;
            caption.style.fontSize = 15;
            caption.style.unityFontStyleAndWeight = FontStyle.Bold;
            caption.style.marginLeft = 10;
            tab.Add(caption);

            tab.RegisterCallback<ClickEvent>(_ => onClick());
            return tab;
        }

        /// <summary>Pinta una pestaña como elegida o como una más de la lista.</summary>
        public static void SetTabSelected(VisualElement tab, Label caption, bool selected)
        {
            tab.userData = selected;
            tab.style.backgroundColor = selected ? Cream : Color.clear;
            caption.style.color = selected ? Ink : InkSoft;

            // La elegida se queda cuadrada por la derecha para pegarse al panel y
            // parecer la misma superficie. Es lo que dice, sin escribirlo, que lo que
            // hay al lado es justo lo que has pulsado.
            var s = tab.style;
            s.borderTopRightRadius = s.borderBottomRightRadius = selected ? 0 : Radius;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Texto — la voz del juego también es tema
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// «minuto» o «minutos» según la cantidad. El plural se decide aquí y en
        /// ningún otro sitio.
        /// </summary>
        /// <remarks>
        /// «hace 1 minutos» salió de escribir el condicional a mano: al sexto sitio
        /// le sale bien y al séptimo se olvida. Si algún día hay que doblar el juego,
        /// este es el único lugar donde el español manda.
        /// </remarks>
        public static string Plural(int cantidad, string singular, string plural = null) =>
            cantidad == 1 ? singular : (plural ?? singular + "s");

        /// <summary>
        /// Cuánto queda para algo, con las palabras del juego. Un plazo en cristiano.
        /// </summary>
        /// <remarks>
        /// Los tramos comparan **minutos** y no horas redondeadas: redondear antes de
        /// comparar hacía que con 61-89 minutos dijera «menos de una hora», y quien
        /// se fía pierde el encargo. El redondeo a horas llega después, y solo para
        /// elegir la palabra («unas N horas»), que ahí un minuto arriba o abajo no
        /// cambia lo que el jugador necesita saber.
        /// </remarks>
        public static string Plazo(long minutosRestantes)
        {
            if (minutosRestantes <= 0) return "Se le ha pasado el momento.";
            if (minutosRestantes < 60) return "Queda menos de una hora.";

            int horas = Mathf.RoundToInt(minutosRestantes / 60f);
            return horas <= 1 ? "Queda sobre una hora." : $"Quedan unas {horas} horas.";
        }
    }
}
