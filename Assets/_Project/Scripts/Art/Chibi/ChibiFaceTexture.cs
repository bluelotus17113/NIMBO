using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.Chibi
{
    /// <summary>
    /// Dibuja la cara de un habitante en una textura, con su emoción actual.
    /// </summary>
    /// <remarks>
    /// La cara se pinta y no se modela porque tiene que cambiar de expresión doce
    /// veces distintas sin recalcular geometría, y porque a la distancia de la cámara
    /// de la isla dos ojos dibujados se leen mejor que dos ojos modelados.
    ///
    /// La textura se reutiliza al cambiar de emoción: se reescriben los píxeles del
    /// mismo objeto en vez de crear uno nuevo, o doce habitantes parpadeando dejarían
    /// basura para el recolector cada pocos segundos.
    /// </remarks>
    public sealed class ChibiFaceTexture
    {
        public const int Size = 128;

        private readonly Texture2D _texture;
        private readonly Color32[] _pixels = new Color32[Size * Size];
        private readonly AppearanceData _appearance;

        private Emotion _drawn = (Emotion)(-1);

        public Texture2D Texture => _texture;

        public ChibiFaceTexture(in AppearanceData appearance, string ownerName)
        {
            _appearance = appearance;
            _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                name = $"cara_{ownerName}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Draw(Emotion.Neutral);
        }

        public void Draw(Emotion emotion)
        {
            if (emotion == _drawn) return;
            _drawn = emotion;

            var skin = (Color32)new Color(_appearance.SkinTone.r / 255f, _appearance.SkinTone.g / 255f,
                                          _appearance.SkinTone.b / 255f, 1f);
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = skin;

            DrawBlush();
            DrawEyes(emotion);
            DrawBrows(emotion);
            DrawMouth(emotion);
            DrawFreckles();

            _texture.SetPixels32(_pixels);
            _texture.Apply(updateMipmaps: false);
        }

        // La cara ocupa el centro de la textura; en UV, x crece a la derecha y
        // y hacia arriba, así que el eje vertical va invertido respecto al array.
        private void Set(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size) return;
            _pixels[y * Size + x] = color;
        }

        private void FillEllipse(float cx, float cy, float rx, float ry, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + rx));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + ry));

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, color);
            }
        }

        private void StrokeLine(float x0, float y0, float x1, float y1, float thickness, Color32 color)
        {
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2f) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                FillEllipse(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t),
                            thickness, thickness, color);
            }
        }

        private float EyeX(int side) => Size * (0.5f + side * Mathf.Lerp(0.12f, 0.22f, _appearance.EyeSpacing));
        private float EyeY => Size * Mathf.Lerp(0.50f, 0.62f, _appearance.EyeHeight);

        private void DrawEyes(Emotion emotion)
        {
            float radius = Size * Mathf.Lerp(0.045f, 0.085f, _appearance.EyeSize);
            var iris = _appearance.EyeColor;
            var white = new Color32(252, 252, 255, 255);
            var outline = new Color32(48, 38, 40, 255);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = EyeX(side), y = EyeY;

                // Los ojos cerrados son una raya: sirve para dormido y para reírse.
                if (emotion is Emotion.Sleepy or Emotion.Ecstatic)
                {
                    StrokeLine(x - radius, y, x + radius, y, radius * 0.28f, outline);
                    continue;
                }

                if (emotion == Emotion.Angry)
                {
                    FillEllipse(x, y, radius * 1.05f, radius * 0.72f, white);
                    FillEllipse(x, y, radius * 0.5f, radius * 0.5f, iris);
                    continue;
                }

                float stretch = emotion == Emotion.Surprised ? 1.25f : 1f;
                FillEllipse(x, y, radius * stretch, radius * 1.15f * stretch, white);
                FillEllipse(x, y - radius * 0.08f, radius * 0.58f * stretch,
                            radius * 0.66f * stretch, iris);
                FillEllipse(x, y - radius * 0.2f, radius * 0.3f, radius * 0.34f, outline);

                // El brillo es lo que separa una cara viva de un muñeco de plástico.
                FillEllipse(x + radius * 0.28f, y + radius * 0.34f,
                            radius * 0.2f, radius * 0.2f, white);

                if (emotion == Emotion.Love)
                    FillEllipse(x, y, radius * 0.34f, radius * 0.34f,
                                new Color32(232, 96, 128, 255));
            }
        }

        private void DrawBrows(Emotion emotion)
        {
            var color = _appearance.BrowColor;
            float thickness = Size * Mathf.Lerp(0.008f, 0.018f, _appearance.BrowThickness);
            float width = Size * 0.07f;
            float baseY = EyeY + Size * Mathf.Lerp(0.07f, 0.12f, _appearance.BrowHeight);

            // El ángulo de las cejas es lo que más dice de la emoción: enfadado hacia
            // dentro y abajo, preocupado hacia dentro y arriba.
            float inner = emotion switch
            {
                Emotion.Angry => -Size * 0.035f,
                Emotion.Worried or Emotion.Sad => Size * 0.03f,
                Emotion.Surprised => Size * 0.02f,
                _ => 0f,
            };
            float lift = emotion == Emotion.Surprised ? Size * 0.03f : 0f;

            for (int side = -1; side <= 1; side += 2)
            {
                float x = EyeX(side);
                float innerX = x - side * width, outerX = x + side * width;
                StrokeLine(innerX, baseY + inner + lift, outerX, baseY + lift, thickness, color);
            }
        }

        private void DrawMouth(Emotion emotion)
        {
            var color = new Color32(_appearance.LipColor.r, _appearance.LipColor.g,
                                    _appearance.LipColor.b, 255);
            float cx = Size * 0.5f;
            float cy = Size * Mathf.Lerp(0.26f, 0.36f, _appearance.MouthHeight);
            float width = Size * Mathf.Lerp(0.06f, 0.12f, _appearance.MouthWidth);

            switch (emotion)
            {
                case Emotion.Ecstatic:
                case Emotion.Happy:
                    // Sonrisa: un arco de puntos, más marcado cuanto más contento.
                    float depth = emotion == Emotion.Ecstatic ? 0.55f : 0.32f;
                    for (float t = -1f; t <= 1f; t += 0.04f)
                        FillEllipse(cx + t * width, cy - (1f - t * t) * width * depth,
                                    Size * 0.012f, Size * 0.012f, color);
                    if (emotion == Emotion.Ecstatic)
                        FillEllipse(cx, cy - width * 0.28f, width * 0.62f, width * 0.34f, color);
                    break;

                case Emotion.Sad:
                case Emotion.Worried:
                    for (float t = -1f; t <= 1f; t += 0.04f)
                        FillEllipse(cx + t * width * 0.8f, cy + (1f - t * t) * width * 0.3f,
                                    Size * 0.012f, Size * 0.012f, color);
                    break;

                case Emotion.Surprised:
                case Emotion.Hungry:
                    FillEllipse(cx, cy, width * 0.42f, width * 0.55f, color);
                    break;

                case Emotion.Angry:
                    StrokeLine(cx - width * 0.7f, cy - width * 0.1f,
                               cx + width * 0.7f, cy + width * 0.15f, Size * 0.013f, color);
                    break;

                default:
                    StrokeLine(cx - width * 0.55f, cy, cx + width * 0.55f, cy, Size * 0.011f, color);
                    break;
            }
        }

        private void DrawBlush()
        {
            if (_appearance.Blush <= 0.02f) return;

            byte alpha = (byte)(90 * _appearance.Blush);
            var blush = new Color32(240, 150, 150, 255);
            float y = EyeY - Size * 0.12f;

            for (int side = -1; side <= 1; side += 2)
                BlendEllipse(EyeX(side) + side * Size * 0.02f, y,
                             Size * 0.075f, Size * 0.045f, blush, alpha);
        }

        private void DrawFreckles()
        {
            if (_appearance.Freckles <= 0.02f) return;

            var color = new Color32(196, 132, 96, 255);
            byte alpha = (byte)(150 * _appearance.Freckles);
            float y = EyeY - Size * 0.10f;

            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 5; i++)
            {
                float x = EyeX(side) + side * (Size * 0.01f + i * Size * 0.016f);
                float dy = y + (i % 2 == 0 ? Size * 0.012f : -Size * 0.008f);
                BlendEllipse(x, dy, Size * 0.009f, Size * 0.009f, color, alpha);
            }
        }

        /// <summary>Mezcla en vez de pisar: colorete y pecas van sobre el tono de piel.</summary>
        private void BlendEllipse(float cx, float cy, float rx, float ry, Color32 color, byte alpha)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + rx));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + ry));
            float t = alpha / 255f;

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d > 1f) continue;

                float strength = t * (1f - d);   // se difumina hacia el borde
                int i = y * Size + x;
                _pixels[i] = Color32.Lerp(_pixels[i], color, strength);
            }
        }
    }
}
