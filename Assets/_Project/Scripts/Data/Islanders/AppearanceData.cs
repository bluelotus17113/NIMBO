using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// El aspecto de un habitante, entero en números. Nimbo.Art construye la malla
    /// chibi a partir de esto y de nada más: si un rasgo no está aquí, no se ve.
    /// </summary>
    /// <remarks>
    /// Todos los deslizadores van en [0, 1] salvo los que dicen otra cosa, y los
    /// "…Style" son índices a catálogos de <c>Nimbo.Art</c>. Guardar índices y no
    /// referencias a mallas es lo que permite dejar el arte para el final.
    /// </remarks>
    [Serializable]
    public struct AppearanceData
    {
        // --- cabeza -------------------------------------------------------
        public int HeadShape;              // índice de catálogo
        [Range(0f, 1f)] public float HeadWidth;
        [Range(0f, 1f)] public float HeadHeight;
        public Color32 SkinTone;

        // --- ojos ---------------------------------------------------------
        public int EyeStyle;
        [Range(0f, 1f)] public float EyeSize;
        [Range(0f, 1f)] public float EyeSpacing;
        [Range(0f, 1f)] public float EyeHeight;
        [Range(-1f, 1f)] public float EyeTilt;
        public Color32 EyeColor;

        // --- cejas --------------------------------------------------------
        public int BrowStyle;
        [Range(0f, 1f)] public float BrowThickness;
        [Range(0f, 1f)] public float BrowHeight;
        [Range(-1f, 1f)] public float BrowTilt;
        public Color32 BrowColor;

        // --- nariz y boca -------------------------------------------------
        public int NoseStyle;
        [Range(0f, 1f)] public float NoseSize;
        [Range(0f, 1f)] public float NoseHeight;

        public int MouthStyle;
        [Range(0f, 1f)] public float MouthWidth;
        [Range(0f, 1f)] public float MouthHeight;
        public Color32 LipColor;

        // --- pelo ---------------------------------------------------------
        public int HairStyle;
        public Color32 HairColor;

        // --- cuerpo -------------------------------------------------------
        [Range(0f, 1f)] public float BodyHeight;   // 0 = bajito, 1 = alto
        [Range(0f, 1f)] public float BodyBuild;    // 0 = delgado, 1 = ancho

        // --- extras (banderas) --------------------------------------------
        public int GlassesStyle;   // 0 = ninguno
        public int FacialHairStyle;
        [Range(0f, 1f)] public float Blush;
        [Range(0f, 1f)] public float Freckles;
        public Color32 GlassesColor;

        /// <summary>Un aspecto neutro y legible, el punto de partida del creador.</summary>
        public static AppearanceData Default => new AppearanceData
        {
            HeadShape = 0, HeadWidth = 0.5f, HeadHeight = 0.5f,
            SkinTone = new Color32(242, 205, 178, 255),
            EyeStyle = 0, EyeSize = 0.5f, EyeSpacing = 0.5f, EyeHeight = 0.5f, EyeTilt = 0f,
            EyeColor = new Color32(74, 52, 40, 255),
            BrowStyle = 0, BrowThickness = 0.5f, BrowHeight = 0.5f, BrowTilt = 0f,
            BrowColor = new Color32(58, 40, 32, 255),
            NoseStyle = 0, NoseSize = 0.4f, NoseHeight = 0.5f,
            MouthStyle = 0, MouthWidth = 0.5f, MouthHeight = 0.5f,
            LipColor = new Color32(198, 108, 108, 255),
            HairStyle = 0, HairColor = new Color32(58, 40, 32, 255),
            BodyHeight = 0.5f, BodyBuild = 0.5f,
            GlassesStyle = 0, FacialHairStyle = 0, Blush = 0.2f, Freckles = 0f,
            GlassesColor = new Color32(45, 45, 50, 255),
        };
    }
}
