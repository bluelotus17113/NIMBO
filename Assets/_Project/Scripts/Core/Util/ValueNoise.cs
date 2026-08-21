using UnityEngine;

namespace Nimbo.Core.Util
{
    /// <summary>
    /// El mismo ruido que usan los shaders de la isla, en C#.
    /// </summary>
    /// <remarks>
    /// Está copiado a mano de <c>NimboAnime.hlsl</c>, y esa copia es a propósito: el
    /// shader pinta las manchas de color del prado y este decide dónde se siembran
    /// las flores, y las flores tienen que caer **en** las manchas claras. Si cada
    /// uno tuviera su ruido, las flores saldrían repartidas por igual y el prado
    /// perdería lo único que lo hace un prado y no una alfombra: que tiene sitios.
    ///
    /// Las dos versiones tienen que dar el mismo número; hay una prueba que lo
    /// comprueba contra valores anotados a mano, para que si alguien toca una de las
    /// dos salte por algún lado.
    /// </remarks>
    public static class ValueNoise
    {
        /// <summary>La parte decimal, como la de HLSL: siempre positiva.</summary>
        private static float Frac(float x) => x - Mathf.Floor(x);

        private static float Hash(float px, float py)
        {
            float x = Frac(px * 127.1f);
            float y = Frac(py * 311.7f);

            float dot = x * (x + 34.23f) + y * (y + 34.23f);
            x += dot;
            y += dot;

            return Frac(x * y * 95.43f);
        }

        /// <summary>Ruido de valor entre 0 y 1, suave entre celdas.</summary>
        public static float At(float px, float py)
        {
            float ix = Mathf.Floor(px), iy = Mathf.Floor(py);
            float fx = px - ix, fy = py - iy;

            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash(ix, iy);
            float b = Hash(ix + 1f, iy);
            float c = Hash(ix, iy + 1f);
            float d = Hash(ix + 1f, iy + 1f);

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>
        /// Las manchas del prado: dos octavas, la ancha manda y la corta le rompe el
        /// borde. Es lo que el shader llama <c>NimboVariation</c>.
        /// </summary>
        public static float Variation(Vector3 positionWS, float scale)
        {
            float s = Mathf.Max(scale, 0.001f);
            float px = positionWS.x / s, py = positionWS.z / s;
            return At(px, py) * 0.68f + At(px * 2.7f, py * 2.7f) * 0.32f;
        }
    }
}
