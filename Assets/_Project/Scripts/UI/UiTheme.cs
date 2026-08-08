using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI
{
    /// <summary>
    /// La paleta y los estilos de toda la interfaz, en un sitio.
    /// </summary>
    /// <remarks>
    /// La hoja de estilos se construye en código y no en un <c>.uss</c> a propósito:
    /// un fichero de texto plano lo puede editar cualquiera de los agentes en paralelo,
    /// y un asset de Unity, no. El coste es escribir los estilos a mano; la ventaja es
    /// que la interfaz entera cabe en el mismo flujo de trabajo que el resto.
    /// </remarks>
    public static class UiTheme
    {
        // Cielo y nubes: la isla flota, y la interfaz tiene que sonar a eso.
        public static readonly Color Sky = new Color32(0x7E, 0xC8, 0xE3, 255);
        public static readonly Color Cloud = new Color32(0xFA, 0xFC, 0xFF, 255);
        public static readonly Color Panel = new Color32(0xFF, 0xFF, 0xFF, 0xF2);
        public static readonly Color PanelDark = new Color32(0x2B, 0x3A, 0x4A, 0xF2);
        public static readonly Color Ink = new Color32(0x2B, 0x3A, 0x4A, 255);
        public static readonly Color InkSoft = new Color32(0x6B, 0x7D, 0x8F, 255);
        public static readonly Color Accent = new Color32(0xFF, 0xB0, 0x4A, 255);
        public static readonly Color AccentDeep = new Color32(0xE8, 0x8C, 0x1F, 255);

        // Una barra por necesidad, con su color propio para leerlas de un vistazo.
        public static readonly Color Hunger = new Color32(0xF2, 0x8B, 0x5C, 255);
        public static readonly Color Energy = new Color32(0xF5, 0xD0, 0x5E, 255);
        public static readonly Color Social = new Color32(0x7A, 0xC9, 0x8B, 255);
        public static readonly Color Hygiene = new Color32(0x74, 0xB9, 0xE8, 255);
        public static readonly Color Mood = new Color32(0xE0, 0x8A, 0xC4, 255);

        public static readonly Color Critical = new Color32(0xE0, 0x50, 0x50, 255);
        public static readonly Color Low = new Color32(0xE8, 0x92, 0x3C, 255);

        public const int Radius = 14;
        public const int Gap = 10;

        /// <summary>El color de una barra según lo llena que esté. El rojo solo en rojo.</summary>
        public static Color BarColor(Color baseColor, float normalized) =>
            normalized <= 0.15f ? Critical :
            normalized <= 0.35f ? Low :
            baseColor;

        /// <summary>Un panel con esquinas redondeadas y sombra, el bloque básico de todo.</summary>
        public static VisualElement Card(string name = null)
        {
            var card = new VisualElement { name = name };
            var s = card.style;
            s.backgroundColor = Panel;
            s.paddingLeft = s.paddingRight = s.paddingTop = s.paddingBottom = 14;
            s.marginBottom = Gap;
            SetRadius(card, Radius);
            return card;
        }

        public static void SetRadius(VisualElement element, float radius)
        {
            var s = element.style;
            s.borderTopLeftRadius = s.borderTopRightRadius =
                s.borderBottomLeftRadius = s.borderBottomRightRadius = radius;
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
            label.style.fontSize = 14;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        public static Button Action(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            var s = button.style;
            s.backgroundColor = Accent;
            s.color = Color.white;
            s.fontSize = 15;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingTop = s.paddingBottom = 8;
            s.paddingLeft = s.paddingRight = 16;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 0;
            s.marginLeft = s.marginRight = 0;
            SetRadius(button, 10);
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
            track.style.backgroundColor = (Color)new Color32(0xE4, 0xEA, 0xF0, 255);
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

        /// <summary>Una etiqueta pequeña de color, para el tipo de personalidad o el estado.</summary>
        public static Label Chip(string text, Color color)
        {
            var chip = new Label(text);
            var s = chip.style;
            s.backgroundColor = color;
            s.color = Color.white;
            s.fontSize = 12;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.paddingLeft = s.paddingRight = 10;
            s.paddingTop = s.paddingBottom = 4;
            s.marginRight = 6;
            SetRadius(chip, 9);
            return chip;
        }
    }
}
