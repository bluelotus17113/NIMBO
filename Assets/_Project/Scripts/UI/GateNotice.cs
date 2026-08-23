using UnityEngine.UIElements;

namespace Nimbo.UI
{
    /// <summary>
    /// La franja fija donde la isla explica lo que aún está cerrado.
    /// </summary>
    /// <remarks>
    /// Las explicaciones de bloqueo se escriben en <see cref="Gates"/> (Gates.cs:36) y
    /// se cuelgan de <c>.tooltip</c> en siete sitios (SocialSection.cs:160/208/246/287,
    /// JobSection.cs:160, EventsPanel.cs:119, CraftPanel.cs:158). Ese canal no existe
    /// en runtime: medido contra el juego real (Informes/informe-tema.md §4), en
    /// 6000.5.5f1 nadie despacha <c>TooltipEvent</c> fuera del editor — cero eventos
    /// con el elemento encendido y cero con el apagado. Estaban escritas para nadie.
    ///
    /// Esta franja no espera ningún gesto, y es decisión medida y no gusto. Las otras
    /// dos formas clásicas de decirlo quedan descartadas así: enseñarlo pegado al
    /// botón exige hover o foco —el hover es puntero, y un botón apagado con
    /// <c>SetEnabled(false)</c> no puede tomar el foco, medido en
    /// AvisosEnLaIslaTests—; y decírselo al intentar pulsarlo exige un gesto de
    /// activación que hoy no existe hacia esta interfaz más que con ratón (el proyecto
    /// va sobre el sistema de entrada antiguo, sin navegación de foco por teclado ni
    /// mando). Una franja fija se alcanza por existir: vale igual con ratón, con
    /// teclado, con mando o sin tocar nada, que era justo lo que el tooltip no cumplía.
    ///
    /// Se alimenta sola: cada 0,25 s recorre el árbol vivo y enseña la primera
    /// explicación visible que encuentra, con cuenta si hay más. El ritmo es el mismo
    /// orden de magnitud que el refresco lento de listas de UiRoot (0,4 s), donde ya
    /// se estableció que nadie nota ese latido; el coste es un pase por unos pocos
    /// cientos de nodos cuatro veces por segundo.
    ///
    /// No toca ni mueve nada del árbol que mira: se cuelga de la raíz como el cartel
    /// de logros, en posición absoluta, y no atrapa clics. Los siete sitios siguen
    /// escribiendo donde siempre; aquí solo se lee.
    /// </remarks>
    public sealed class GateNotice
    {
        /// <summary>Para encontrarla montada entre todo lo demás.</summary>
        public const string RootName = "aviso_puertas";

        /// <summary>Cada cuánto se mira el árbol. Un latido, no un reflejo.</summary>
        private const float ScanSeconds = 0.25f;

        /// <summary>
        /// Distancia al fondo: deja sitio a la fila de acciones, la fila de
        /// habitantes y la barra (unas 240 px peores caso, con el aviso delante).
        /// </summary>
        private const int BottomOffset = 250;

        public VisualElement Root { get; }

        /// <summary>Lo que se lee ahora mismo, para no reescribir lo igual.</summary>
        private string _shown = "";

        // Sin readonly a propósito: se rellena en BuildCard(), y un campo que solo
        // toca el constructor no puede asignarse desde un método al que el
        // constructor llama.
        private Label _text;
        private VisualElement _tree;
        private float _sinceScan;

        public GateNotice() => Root = BuildCard();

        /// <summary>Dónde mirar y colgarse. Lo llama el anfitrión al (re)montarse.</summary>
        public void ColocarEn(VisualElement tree)
        {
            _tree = tree;
            if (Root.parent != tree) tree.Add(Root);
        }

        /// <summary>Lo que la franja dice ahora, ya con la cuenta si la hay.</summary>
        public string Texto => _text.text;

        /// <summary>Si hay algo que leer a estas horas.</summary>
        public bool Visible => Root.style.display.value == DisplayStyle.Flex;

        private VisualElement BuildCard()
        {
            var card = new VisualElement { name = RootName };
            var s = card.style;
            s.position = Position.Absolute;
            s.bottom = BottomOffset;
            s.left = Length.Percent(50);
            s.translate = new Translate(Length.Percent(-50), 0);
            s.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(card, UiTheme.RadiusCard);
            s.paddingLeft = s.paddingRight = 14;
            s.paddingTop = s.paddingBottom = 9;
            s.maxWidth = 480;

            // El filete rosa dice «esto es un cierre» sin gritarlo: los avisos de
            // bloqueo llevan rosa en toda la interfaz (UiTheme.Rose en chips de
            // carencia) y los dos tooltips informativos que también reviva esta
            // franja —nombres de adornos y puntos del mapa— comparten canal sin
            // mentir con una etiqueta de error que no les corresponde.
            s.borderLeftWidth = 3;
            s.borderLeftColor = UiTheme.Rose;

            // Decorativo: informa, no pide clics. Que no se trague los de lo que
            // tape — mismo porqué que AchievementToast.cs:58.
            card.pickingMode = PickingMode.Ignore;

            _text = UiTheme.Body("");
            _text.style.fontSize = 13;
            card.Add(_text);

            card.style.display = DisplayStyle.None;
            return card;
        }

        /// <summary>Lo llama el anfitrión cada fotograma, con el tiempo sin escalar.</summary>
        public void Tick(float unscaledDeltaTime)
        {
            if (_tree == null || Root.panel == null) return;

            _sinceScan += unscaledDeltaTime;
            if (_sinceScan < ScanSeconds) return;
            _sinceScan = 0f;

            Revisar();
        }

        private void Revisar()
        {
            string primera = null;
            int total = 0;
            Buscar(_tree, ref primera, ref total);

            if (primera == null)
            {
                if (_shown.Length == 0) return;
                _shown = "";
                Root.style.display = DisplayStyle.None;
                return;
            }

            string texto = total > 1 ? $"{primera}  ·  {total - 1} más" : primera;
            if (texto == _shown) return;

            _shown = texto;
            _text.text = texto;
            Root.style.display = DisplayStyle.Flex;
        }

        private static void Buscar(VisualElement element, ref string primera, ref int total)
        {
            // Lo escondido no habla: cada panel guarda sus explicaciones hasta que
            // alguien lo abre, y la franja habla del contexto que se ve, no de todo
            // lo que existe.
            if (element.resolvedStyle.display == DisplayStyle.None) return;

            string explicacion = element.tooltip;
            if (!string.IsNullOrWhiteSpace(explicacion))
            {
                total++;
                if (primera == null) primera = explicacion;
            }

            for (int i = 0; i < element.childCount; i++)
                Buscar(element[i], ref primera, ref total);
        }
    }
}
