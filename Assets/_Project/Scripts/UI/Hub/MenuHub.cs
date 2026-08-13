using System.Collections.Generic;
using Nimbo.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Hub
{
    /// <summary>
    /// El menú del juego: una sola ventana con una columna de secciones a la
    /// izquierda y la pantalla que toque a la derecha.
    /// </summary>
    /// <remarks>
    /// Antes cada pantalla tenía su botón encendido en la esquina de la isla —once
    /// a la vez, más la fila de vecinos— y el juego se veía por los huecos. Ahora
    /// solo hay reloj arriba y barra abajo; lo demás vive aquí dentro y se abre con
    /// una tecla.
    ///
    /// El menú no reconstruye ninguna pantalla: adopta las que ya existen. Cada
    /// sección guarda el <c>Root</c> de su panel y le quita el aspecto de tarjeta
    /// suelta (fondo, relleno y la cruz de cerrar), porque dentro de la ventana ese
    /// marco lo pone la ventana. Así una pantalla nueva entra en el menú con una
    /// línea y sigue funcionando igual de suelta si algún día hace falta.
    /// </remarks>
    public sealed class MenuHub
    {
        private const int RailWidth = 214;

        // Lo pide la más ancha que entra aquí, que es la mochila: ocho huecos de 84 con
        // sus márgenes. Con más, las cortas quedan perdidas en medio de un campo de
        // crema; con menos, la rejilla se parte en una fila más.
        private const int BodyWidth = 740;

        private sealed class Section
        {
            public string Label;
            public VisualElement Tab;
            public Label Caption;
            public VisualElement Content;

            /// <summary>Lo que el menú enciende y apaga: el panel, o lo que lo envuelve.</summary>
            public VisualElement Host;

            public System.Action Open;
            public System.Action Close;
        }

        public VisualElement Root { get; }
        public bool IsOpen { get; private set; }
        public string Current => _current?.Label;

        private readonly VisualElement _scrim;
        private readonly VisualElement _window;
        private readonly VisualElement _rail;
        private readonly VisualElement _railTabs;
        private readonly VisualElement _railFooter;
        private readonly VisualElement _body;
        private readonly List<Section> _sections = new List<Section>();

        private readonly VisualElement _modeSlot;
        private readonly Label _modeTitle;
        private readonly Label _hint;

        private Section _current;
        private Button _systemButton;
        private System.Action _onClosed;

        public MenuHub()
        {
            Root = new VisualElement { name = "menu" };
            var r = Root.style;
            r.position = Position.Absolute;
            r.left = r.right = r.top = r.bottom = 0;
            r.alignItems = Align.Center;
            r.justifyContent = Justify.Center;
            r.display = DisplayStyle.None;

            _scrim = new VisualElement { name = "velo" };
            var v = _scrim.style;
            v.position = Position.Absolute;
            v.left = v.right = v.top = v.bottom = 0;
            v.backgroundColor = UiTheme.Scrim;
            v.opacity = 0f;
            UiTheme.Animate(_scrim, 160);
            _scrim.RegisterCallback<ClickEvent>(_ => Close());
            Root.Add(_scrim);

            _window = UiTheme.Window("ventana-menu");
            var w = _window.style;

            // Alto fijo y no «hasta donde llegue»: con el alto por contenido, la ventana
            // crecía y menguaba a cada sección —la de hacer cosas es larga y la del mapa
            // corta— y el menú entero daba un salto al cambiar de pestaña.
            w.height = Length.Percent(78);
            w.opacity = 0f;
            UiTheme.Animate(_window, 180);
            Root.Add(_window);

            // --- columna de secciones ---
            _rail = new VisualElement { name = "columna" };
            var rail = _rail.style;
            rail.width = RailWidth;
            rail.flexShrink = 0;
            rail.backgroundColor = UiTheme.CreamDeep;
            rail.paddingLeft = UiTheme.SpaceM;
            rail.paddingTop = rail.paddingBottom = UiTheme.SpaceL;

            // Sin hueco por la derecha: la pestaña elegida tiene que llegar al borde del
            // panel para fundirse con él.
            rail.paddingRight = 0;

            var railTitle = UiTheme.Body("Menú", soft: true);
            railTitle.style.marginLeft = UiTheme.SpaceM;
            railTitle.style.marginBottom = UiTheme.SpaceM;
            _rail.Add(railTitle);

            _railTabs = new VisualElement();
            _rail.Add(_railTabs);

            // Lo que empuja el pie de la columna hasta abajo del todo.
            var push = new VisualElement();
            push.style.flexGrow = 1;
            push.style.minHeight = UiTheme.SpaceL;
            _rail.Add(push);

            _railFooter = new VisualElement();
            _railFooter.style.marginRight = UiTheme.SpaceM;
            _rail.Add(_railFooter);

            _modeTitle = UiTheme.Body("Aquí puedes", soft: true);
            _modeTitle.style.marginLeft = UiTheme.SpaceM;
            _modeTitle.style.marginBottom = UiTheme.SpaceS;
            _modeTitle.style.display = DisplayStyle.None;
            _railFooter.Add(_modeTitle);

            _modeSlot = new VisualElement();
            _railFooter.Add(_modeSlot);

            _hint = UiTheme.Caption("Esc para cerrar");
            _hint.style.marginLeft = UiTheme.SpaceM;
            _hint.style.marginTop = UiTheme.SpaceM;
            _railFooter.Add(_hint);

            _window.Add(_rail);

            // --- lado del contenido ---
            var right = new VisualElement();
            right.style.width = BodyWidth;
            right.style.flexShrink = 0;
            right.style.paddingLeft = right.style.paddingRight = UiTheme.SpaceXL;
            right.style.paddingTop = right.style.paddingBottom = UiTheme.SpaceXL;

            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.FlexEnd;
            top.Add(UiTheme.Close(Close));
            right.Add(top);

            // El cuerpo no se desplaza: lo hace cada sección por dentro. Con las dos
            // cosas desplazándose salían dos barras pegadas la una a la otra —la del
            // menú y la de la lista— y no había forma de saber cuál movía qué.
            _body = new VisualElement();
            _body.style.flexGrow = 1;
            _body.style.overflow = Overflow.Hidden;
            UiTheme.Animate(_body, 160);
            right.Add(_body);

            _window.Add(right);
        }

        /// <summary>Qué hacer cuando el menú se cierre, sea por donde sea.</summary>
        public void OnClosed(System.Action action) => _onClosed = action;

        /// <summary>
        /// Mete una pantalla en el menú. <paramref name="onOpen"/> es lo que ya hacía
        /// su botón de antes, y <paramref name="onClose"/> lo que hacía su cruz.
        /// </summary>
        public void Add(string label, Color tone, VisualElement content,
                        System.Action onOpen, System.Action onClose)
        {
            var section = new Section
            {
                Label = label,
                Content = content,
                Open = onOpen,
                Close = onClose,
            };

            section.Tab = UiTheme.RailTab(label, tone, () => Select(section), out section.Caption);
            _railTabs.Add(section.Tab);

            Adopt(content);
            section.Host = Wrap(content);

            // Apagada de entrada. El menú gobierna qué sección se ve y no se fía de que
            // cada panel se esconda solo: la de vecinos no es un panel con
            // <c>Hide</c> sino una lista montada aquí, y sin esto se quedaba encendida
            // debajo de todas las demás.
            section.Host.style.display = DisplayStyle.None;

            _body.Add(section.Host);
            _sections.Add(section);
        }

        /// <summary>
        /// Vacía el pie de la columna. Se llama antes de volver a poner los modos que
        /// tocan, que cambian según estés en la calle o dentro de casa.
        /// </summary>
        public void ClearModes()
        {
            _modeSlot.Clear();
            _modeTitle.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Añade un modo al pie de la columna.
        /// </summary>
        /// <remarks>
        /// Los modos no son secciones: se comen la pantalla entera y dejan solo su
        /// menú, así que no pintan en la lista de arriba. Van aquí abajo y solo los
        /// que se puedan usar donde estás — en la calle, construir y decorar; dentro
        /// de casa, amueblar y nada más.
        ///
        /// Pulsar uno cierra el menú antes de entrar: el modo manda sobre la pantalla
        /// y dejar la ventana abierta encima sería tener dos cosas mandando.
        /// </remarks>
        public void AddMode(string label, System.Action onPressed)
        {
            var button = UiTheme.Secondary(label, () =>
            {
                Close();
                onPressed();
            });

            button.name = $"modo-{label}";
            button.style.marginBottom = UiTheme.SpaceS;
            _modeSlot.Add(button);
            _modeTitle.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Envuelve la sección en algo que se pueda desplazar, salvo que el panel ya
        /// traiga su propia lista desplazable.
        /// </summary>
        /// <remarks>
        /// Las pantallas largas —hacer cosas, logros— ya tienen dentro una lista que
        /// crece hasta donde le dejen; envolverlas otra vez pondría una barra dentro de
        /// otra. Las cortas no tienen ninguna, y sin envolverlas se cortarían por abajo
        /// el día que la ventana quede pequeña.
        /// </remarks>
        private static VisualElement Wrap(VisualElement content)
        {
            if (content.Q<ScrollView>() != null)
            {
                // Que ocupe el alto entero del cuerpo. Estos paneles traían un
                // «hasta el 92 % de la pantalla» de cuando flotaban sueltos sobre la
                // isla, y dentro de la ventana eso dejaba un palmo de crema vacía
                // debajo de la lista.
                content.style.maxHeight = StyleKeyword.None;
                content.style.flexGrow = 1;
                return content;
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            UiTheme.StyleScroll(scroll);
            scroll.Add(content);
            return scroll;
        }

        /// <summary>
        /// El botón de abajo del todo, el que no depende de dónde estés: la pausa y
        /// los ajustes, que viven en la capa de encima.
        /// </summary>
        /// <remarks>
        /// Existe por lo mismo que el botón del menú en el reloj: la pausa solo se
        /// abría con Escape, y una tecla que no está escrita en ninguna parte es una
        /// tecla que la mitad de la gente no encuentra. No es un sitio nuevo para nada
        /// —es la misma pantalla—, es una puerta más a la misma.
        /// </remarks>
        public void SetSystemButton(string label, System.Action onPressed)
        {
            if (_systemButton == null)
            {
                _systemButton = UiTheme.Secondary(label, () => { });
                _systemButton.name = "boton-sistema";
                _systemButton.style.marginTop = UiTheme.SpaceS;
                _railFooter.Insert(_railFooter.IndexOf(_hint), _systemButton);
            }

            _systemButton.text = label;
            _systemButton.clickable = new Clickable(() =>
            {
                Close();
                onPressed();
            });
        }

        /// <summary>
        /// Le quita a un panel el aspecto de tarjeta suelta: dentro de la ventana el
        /// fondo y el margen los pone la ventana, y la cruz de cerrar ya está arriba.
        /// </summary>
        private static void Adopt(VisualElement content)
        {
            var s = content.style;
            s.backgroundColor = Color.clear;
            s.paddingLeft = s.paddingRight = s.paddingTop = s.paddingBottom = 0;
            s.marginLeft = s.marginRight = s.marginTop = s.marginBottom = 0;
            s.width = StyleKeyword.Auto;

            // Solo la cruz de su propia cabecera, que es su primer hijo. Buscarla por
            // todo el panel escondería también la de una ficha de dentro —la del vecino
            // que estás mirando—, y esa sí hace falta para volver a la lista.
            if (content.childCount == 0) return;

            var head = content[0];
            if (head.name != "cabecera") return;

            var close = head.Q<Button>("cerrar");
            if (close != null) close.style.display = DisplayStyle.None;
        }

        public void Toggle(string label = null)
        {
            if (IsOpen && (label == null || label == Current)) Close();
            else Open(label);
        }

        public void Open(string label = null)
        {
            var wanted = Find(label) ?? _current ?? (_sections.Count > 0 ? _sections[0] : null);
            if (wanted == null) return;

            bool wasOpen = IsOpen;
            IsOpen = true;
            Root.style.display = DisplayStyle.Flex;
            Select(wanted);

            if (wasOpen) return;

            EventBus.Publish(new MenuOpened(true));

            // El estado de partida hay que dejarlo puesto un fotograma antes de mover
            // nada: si se pone el destino en el mismo fotograma que el display, la
            // transición no tiene de dónde salir y la ventana aparece de golpe.
            _window.style.opacity = 0f;
            _window.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1f));
            _window.style.translate = new Translate(0, 14);
            _scrim.style.opacity = 0f;

            Root.schedule.Execute(() =>
            {
                _scrim.style.opacity = 1f;
                _window.style.opacity = 1f;
                _window.style.scale = new Scale(Vector3.one);
                _window.style.translate = new Translate(0, 0);
            }).ExecuteLater(0);
        }

        public void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            _scrim.style.opacity = 0f;
            _window.style.opacity = 0f;
            _window.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1f));
            _window.style.translate = new Translate(0, 14);

            // Se apaga cuando ha terminado de irse, no antes: apagarlo ya cortaría la
            // animación en seco y sería como no tenerla.
            Root.schedule.Execute(() =>
            {
                if (!IsOpen) Root.style.display = DisplayStyle.None;
            }).ExecuteLater(190);

            _current?.Close?.Invoke();
            _onClosed?.Invoke();
            EventBus.Publish(new MenuOpened(false));
        }

        /// <summary>Pasa a otra sección: cierra la de antes y abre la nueva.</summary>
        private void Select(Section section)
        {
            if (_current == section)
            {
                section.Host.style.display = DisplayStyle.Flex;
                section.Open?.Invoke();
                return;
            }

            if (_current != null)
            {
                _current.Close?.Invoke();
                _current.Host.style.display = DisplayStyle.None;
                UiTheme.SetTabSelected(_current.Tab, _current.Caption, false);
            }

            _current = section;
            UiTheme.SetTabSelected(section.Tab, section.Caption, true);
            section.Host.style.display = DisplayStyle.Flex;
            section.Open?.Invoke();

            // El contenido entra con un desvanecido corto. Cambiar de sección de golpe
            // se lee como un salto y no como haber ido a otro sitio.
            _body.style.opacity = 0f;
            Root.schedule.Execute(() => _body.style.opacity = 1f).ExecuteLater(0);
        }

        /// <summary>Va a la sección siguiente o a la anterior. Las flechas del menú.</summary>
        public void Step(int direction)
        {
            if (!IsOpen || _sections.Count == 0) return;

            int index = _current == null ? 0 : _sections.IndexOf(_current);
            index = (index + direction + _sections.Count) % _sections.Count;
            Select(_sections[index]);
        }

        private Section Find(string label)
        {
            if (label == null) return null;

            for (int i = 0; i < _sections.Count; i++)
                if (_sections[i].Label == label) return _sections[i];

            return null;
        }
    }
}
