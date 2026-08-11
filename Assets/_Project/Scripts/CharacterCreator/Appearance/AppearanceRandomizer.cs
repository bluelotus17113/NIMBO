using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.CharacterCreator.Appearance
{
    /// <summary>
    /// Genera y mezcla caras. Es el único sitio donde se decide qué aspecto tiene un
    /// habitante que el jugador no ha modelado a mano.
    /// </summary>
    public static class AppearanceRandomizer
    {
        // Catálogos de Nimbo.Art. Aquí solo se guardan índices: si el arte añade
        // peinados, se sube el número y no hay que tocar nada más.
        public const int HeadShapes = 6;
        public const int EyeStyles = 12;
        public const int BrowStyles = 8;
        public const int NoseStyles = 8;
        public const int MouthStyles = 10;
        // Los que se sortean son solo los de salida: un vecino que aparece solo
        // no puede lucir un peinado que el jugador todavía no ha desbloqueado.
        public const int HairStyles = Data.Islanders.HairStyles.BaseCount;
        public const int GlassesStyles = 7;   // el 0 es «ninguna»
        public const int FacialHairStyles = 6; // el 0 es «ninguno»

        private static readonly Color32[] SkinTones =
        {
            new Color32(255, 224, 196, 255), new Color32(242, 205, 178, 255),
            new Color32(224, 180, 148, 255), new Color32(198, 148, 112, 255),
            new Color32(164, 116, 84, 255),  new Color32(126, 88, 62, 255),
            new Color32(94, 64, 46, 255),    new Color32(66, 44, 32, 255),
        };

        private static readonly Color32[] HairColors =
        {
            new Color32(38, 30, 28, 255),   new Color32(72, 48, 36, 255),
            new Color32(116, 78, 48, 255),  new Color32(168, 124, 68, 255),
            new Color32(214, 178, 108, 255), new Color32(198, 96, 60, 255),
            new Color32(146, 146, 152, 255), new Color32(232, 232, 236, 255),
            // Los tres últimos son de fantasía: la isla flotante se lo permite.
            new Color32(126, 108, 196, 255), new Color32(88, 172, 168, 255),
            new Color32(214, 122, 168, 255),
        };

        private static readonly Color32[] EyeColors =
        {
            new Color32(74, 52, 40, 255),   new Color32(112, 78, 44, 255),
            new Color32(76, 112, 88, 255),  new Color32(72, 108, 156, 255),
            new Color32(108, 126, 140, 255), new Color32(148, 96, 60, 255),
        };

        public static AppearanceData Random(ref Rng rng)
        {
            var a = AppearanceData.Default;

            a.HeadShape = rng.Range(0, HeadShapes);
            a.HeadWidth = rng.Range(0.25f, 0.75f);
            a.HeadHeight = rng.Range(0.25f, 0.75f);
            a.SkinTone = SkinTones[rng.Range(0, SkinTones.Length)];

            a.EyeStyle = rng.Range(0, EyeStyles);
            a.EyeSize = rng.Range(0.3f, 0.85f);
            a.EyeSpacing = rng.Range(0.3f, 0.7f);
            a.EyeHeight = rng.Range(0.35f, 0.65f);
            a.EyeTilt = rng.Range(-0.5f, 0.5f);
            a.EyeColor = EyeColors[rng.Range(0, EyeColors.Length)];

            a.BrowStyle = rng.Range(0, BrowStyles);
            a.BrowThickness = rng.Range(0.2f, 0.8f);
            a.BrowHeight = rng.Range(0.35f, 0.7f);
            a.BrowTilt = rng.Range(-0.6f, 0.6f);

            a.NoseStyle = rng.Range(0, NoseStyles);
            a.NoseSize = rng.Range(0.25f, 0.7f);
            a.NoseHeight = rng.Range(0.4f, 0.6f);

            a.MouthStyle = rng.Range(0, MouthStyles);
            a.MouthWidth = rng.Range(0.3f, 0.75f);
            a.MouthHeight = rng.Range(0.35f, 0.65f);

            a.HairStyle = rng.Range(0, HairStyles);
            a.HairColor = HairColors[rng.Range(0, HairColors.Length)];
            a.BrowColor = Darken(a.HairColor, 0.85f);

            a.BodyHeight = rng.Range(0.25f, 0.8f);
            a.BodyBuild = rng.Range(0.25f, 0.8f);

            // Los extras son la excepción, no la norma: si la mitad de la isla lleva
            // gafas dejan de decir nada del personaje.
            a.GlassesStyle = rng.Chance(0.22f) ? rng.Range(1, GlassesStyles) : 0;
            a.FacialHairStyle = rng.Chance(0.18f) ? rng.Range(1, FacialHairStyles) : 0;
            a.Blush = rng.Range(0f, 0.5f);
            a.Freckles = rng.Chance(0.25f) ? rng.Range(0.3f, 0.9f) : 0f;

            return a;
        }

        /// <summary>
        /// La cara de un hijo: cada rasgo se hereda entero de uno de los dos padres,
        /// y los deslizadores caen entre los suyos con un poco de margen.
        /// </summary>
        /// <remarks>
        /// Se hereda rasgo a rasgo en vez de promediarlo todo porque promediar dos
        /// caras da siempre una cara media, y el parecido es lo que hace gracia: hay
        /// que poder decir «tiene los ojos de su madre».
        /// </remarks>
        public static AppearanceData Blend(in AppearanceData a, in AppearanceData b, ref Rng rng)
        {
            var c = AppearanceData.Default;

            c.HeadShape = rng.Chance(0.5f) ? a.HeadShape : b.HeadShape;
            c.EyeStyle = rng.Chance(0.5f) ? a.EyeStyle : b.EyeStyle;
            c.BrowStyle = rng.Chance(0.5f) ? a.BrowStyle : b.BrowStyle;
            c.NoseStyle = rng.Chance(0.5f) ? a.NoseStyle : b.NoseStyle;
            c.MouthStyle = rng.Chance(0.5f) ? a.MouthStyle : b.MouthStyle;
            c.HairStyle = rng.Chance(0.5f) ? a.HairStyle : b.HairStyle;

            c.SkinTone = MixColor(a.SkinTone, b.SkinTone, rng.Range(0.3f, 0.7f));
            c.HairColor = rng.Chance(0.5f) ? a.HairColor : b.HairColor;
            c.EyeColor = rng.Chance(0.5f) ? a.EyeColor : b.EyeColor;
            c.BrowColor = Darken(c.HairColor, 0.85f);

            c.HeadWidth = Between(a.HeadWidth, b.HeadWidth, ref rng);
            c.HeadHeight = Between(a.HeadHeight, b.HeadHeight, ref rng);
            c.EyeSize = Between(a.EyeSize, b.EyeSize, ref rng);
            c.EyeSpacing = Between(a.EyeSpacing, b.EyeSpacing, ref rng);
            c.EyeHeight = Between(a.EyeHeight, b.EyeHeight, ref rng);
            c.EyeTilt = BetweenSigned(a.EyeTilt, b.EyeTilt, ref rng);
            c.BrowThickness = Between(a.BrowThickness, b.BrowThickness, ref rng);
            c.BrowHeight = Between(a.BrowHeight, b.BrowHeight, ref rng);
            c.BrowTilt = BetweenSigned(a.BrowTilt, b.BrowTilt, ref rng);
            c.NoseSize = Between(a.NoseSize, b.NoseSize, ref rng);
            c.NoseHeight = Between(a.NoseHeight, b.NoseHeight, ref rng);
            c.MouthWidth = Between(a.MouthWidth, b.MouthWidth, ref rng);
            c.MouthHeight = Between(a.MouthHeight, b.MouthHeight, ref rng);

            // Un recién llegado a la isla es bajito y menudo, venga de quien venga.
            c.BodyHeight = 0.1f;
            c.BodyBuild = 0.35f;

            c.Freckles = rng.Chance(0.5f) ? a.Freckles : b.Freckles;
            c.Blush = Mathf.Clamp01(Mathf.Max(a.Blush, b.Blush) + 0.2f);
            c.GlassesStyle = 0;
            c.FacialHairStyle = 0;

            return c;
        }

        /// <summary>
        /// Un valor entre los dos, con margen para salirse un poco por los lados: sin
        /// ese margen ningún hijo podría tener nunca la nariz más grande que sus padres.
        /// </summary>
        private static float Between(float a, float b, ref Rng rng) =>
            Mathf.Clamp01(Spread(a, b, ref rng));

        /// <summary>Igual, para los deslizadores que van de −1 a 1 y no de 0 a 1.</summary>
        private static float BetweenSigned(float a, float b, ref Rng rng) =>
            Mathf.Clamp(Spread(a, b, ref rng), -1f, 1f);

        private static float Spread(float a, float b, ref Rng rng)
        {
            float lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            float slack = (hi - lo) * 0.25f + 0.05f;
            return rng.Range(lo - slack, hi + slack);
        }

        private static Color32 MixColor(Color32 a, Color32 b, float t) =>
            Color32.Lerp(a, b, t);

        private static Color32 Darken(Color32 c, float factor) => new Color32(
            (byte)(c.r * factor), (byte)(c.g * factor), (byte)(c.b * factor), c.a);
    }
}
