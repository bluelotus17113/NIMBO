using Nimbo.Data.Islanders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Creator
{
    /// <summary>
    /// El muñeco del creador: un chibi plano que se viste mientras eliges.
    /// </summary>
    /// <remarks>
    /// No es la malla 3D de <c>IslanderView</c> y no pretende serlo: Nimbo.UI no ve
    /// Nimbo.Art, así que aquí se dibuja con cajas de UI Toolkit lo mismo que el arte
    /// cuenta con triángulos — piel, pelo, ropa y pieza extra—. Lo que sí es igual es
    /// la regla de color, copiada de <c>OutfitLook.From</c>: ranura «outfit» pinta
    /// torso y piernas con la paleta, ranura «hat» añade una pieza sin tocar la ropa,
    /// y los accesorios no se dibujan (a esta altura una pulsera son cuatro píxeles).
    /// La diferencia es que aquí el accesorio no finge: se nombra debajo del muñeco.
    ///
    /// Si un día el arte ofrece una vista previa de verdad —un RenderTexture del
    /// chibi real— este elemento se sustituye por esa imagen y nada más cambia: el
    /// panel solo le pasa el borrador y la prenda.
    /// </remarks>
    public sealed class CreatorMannequin
    {
        // La ropa de siempre del muñeco, cuando no lleva prenda del catálogo. Un
        // neutro cálido y su sombra: si fuera un color bonito competiría con las
        // prendas de verdad, y lo que tiene que leerse es «sin estrenar».
        private static readonly Color32 BaseCloth = new Color32(0xC7, 0xBA, 0xAB, 255);
        private static readonly Color32 BaseLegs = new Color32(0x8F, 0x83, 0x77, 255);

        private const float CenterX = 65f;

        private readonly VisualElement _root;
        private readonly VisualElement _head;
        private readonly VisualElement _hair;
        private readonly VisualElement _torso;
        private readonly VisualElement _armLeft;
        private readonly VisualElement _armRight;
        private readonly VisualElement _legLeft;
        private readonly VisualElement _legRight;
        private readonly VisualElement _hatZone;
        private readonly Label _badge;

        public VisualElement Root => _root;

        public CreatorMannequin()
        {
            _root = new VisualElement { name = "maniqui" };
            var s = _root.style;
            s.width = 130;
            s.height = 196;
            s.marginRight = 12;
            s.flexShrink = 0;

            _hair = Part(40, 18, 50, 22);
            RoundTop(_hair, 24, 10);

            _head = Part(43, 28, 44, 44);
            UiTheme.SetRadius(_head, 22);

            _torso = Part(45, 74, 40, 44);
            UiTheme.SetRadius(_torso, 12);

            _armLeft = Part(31, 76, 11, 34);
            _armRight = Part(88, 76, 11, 34);
            UiTheme.SetRadius(_armLeft, 6);
            UiTheme.SetRadius(_armRight, 6);

            _legLeft = Part(49, 120, 13, 30);
            _legRight = Part(68, 120, 13, 30);
            UiTheme.SetRadius(_legLeft, 5);
            UiTheme.SetRadius(_legRight, 5);

            _hatZone = new VisualElement();
            var hz = _hatZone.style;
            hz.left = 0; hz.top = 0; hz.width = 130; hz.height = 30;
            _hatZone.pickingMode = PickingMode.Ignore;
            _root.Add(_hatZone);

            _badge = UiTheme.Body("", soft: true);
            var b = _badge.style;
            b.position = Position.Absolute;
            b.left = 0; b.right = 0; b.top = 168;
            b.unityTextAlign = TextAnchor.MiddleCenter;
            b.whiteSpace = WhiteSpace.Normal;
            _root.Add(_badge);
        }

        /// <summary>
        /// Viste el muñeco con lo que hay ahora en el borrador. Barato a propósito:
        /// se llama en cada movimiento de deslizador y solo toca estilos.
        /// </summary>
        public void Refresh(in AppearanceData appearance, GarmentOption worn)
        {
            _head.style.backgroundColor = (Color)appearance.SkinTone;
            _hair.style.backgroundColor = (Color)appearance.HairColor;

            // Estatura y complexión, para que mover esos deslizadores también se vea
            // aquí y no solo al llegar a la isla.
            float legHeight = 24f + Mathf.Clamp01(appearance.BodyHeight) * 16f;
            _legLeft.style.height = legHeight;
            _legRight.style.height = legHeight;

            float torsoWidth = 36f + Mathf.Clamp01(appearance.BodyBuild) * 20f;
            _torso.style.width = torsoWidth;
            _torso.style.left = CenterX - torsoWidth * 0.5f;
            _armLeft.style.left = CenterX - torsoWidth * 0.5f - 13;
            _armRight.style.left = CenterX + torsoWidth * 0.5f + 2;

            _badge.text = "";
            _hatZone.Clear();

            if (worn == null)
            {
                PaintBody(BaseCloth, BaseLegs);
                return;
            }

            switch (worn.Slot)
            {
                case "outfit":
                    PaintBody(worn.MainColor, worn.SecondColor);
                    break;

                case "hat":
                    // Igual que en IslanderView: el sombrero no viste el cuerpo.
                    PaintBody(BaseCloth, BaseLegs);
                    BuildHat(worn);
                    break;

                default:
                    // Accesorio: honestidad en vez de pintura. Nombrarlo es más
                    // información que teñirle una muñeca de otro color.
                    PaintBody(BaseCloth, BaseLegs);
                    _badge.text = $"Lleva además: {worn.DisplayName}";
                    break;
            }
        }

        private void PaintBody(Color cloth, Color legs)
        {
            _torso.style.backgroundColor = cloth;
            _armLeft.style.backgroundColor = cloth;
            _armRight.style.backgroundColor = cloth;
            _legLeft.style.backgroundColor = legs;
            _legRight.style.backgroundColor = legs;
        }

        /// <summary>
        /// La pieza de cabeza, con la misma regla de familias que
        /// <c>OutfitLook.PieceOf</c>: lo que contiene «gorro» es ceñido y lo demás
        /// lleva ala. El catálogo de nivel 1 solo trae gorra y diadema, así que hoy
        /// siempre sale con ala; la otra familia está porque la regla es una sola.
        /// </summary>
        private void BuildHat(GarmentOption worn)
        {
            bool ceñido = worn.CatalogId != null && worn.CatalogId.Contains("gorro");
            var color = worn.MainColor;

            if (ceñido)
            {
                var capucha = Part(42, 10, 46, 20);
                RoundTop(capucha, 22, 22);
                capucha.style.backgroundColor = color;
                _hatZone.Add(capucha);
                return;
            }

            var copa = Part(43, 8, 44, 16);
            RoundTop(copa, 18, 18);
            copa.style.backgroundColor = color;
            _hatZone.Add(copa);

            var ala = Part(32, 23, 66, 7);
            ala.style.backgroundColor = color;
            UiTheme.SetRadius(ala, 4);
            _hatZone.Add(ala);
        }

        private static VisualElement Part(float x, float y, float w, float h)
        {
            var part = new VisualElement();
            var s = part.style;
            s.position = Position.Absolute;
            s.left = x; s.top = y; s.width = w; s.height = h;
            part.pickingMode = PickingMode.Ignore;
            return part;
        }

        private static void RoundTop(VisualElement element, float topLeftTopRight,
                                     float bottom)
        {
            var s = element.style;
            s.borderTopLeftRadius = s.borderTopRightRadius = topLeftTopRight;
            s.borderBottomLeftRadius = s.borderBottomRightRadius = bottom;
        }
    }
}
