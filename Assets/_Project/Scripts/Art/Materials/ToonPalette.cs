using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Art.Materials
{
    /// <summary>
    /// Fabrica y reparte los materiales del juego. Todos salen del shader de URP,
    /// configurados para que la isla se vea de juguete y no de simulador.
    /// </summary>
    /// <remarks>
    /// Los materiales se cachean por color: una isla con doce habitantes y ochenta
    /// muebles genera cientos de peticiones del mismo blanco crema, y crear un
    /// material por cada una rompe el envío por lotes y multiplica los draw calls.
    /// </remarks>
    public static class ToonPalette
    {
        private static readonly Dictionary<int, Material> Cache = new Dictionary<int, Material>(64);
        private static Shader _lit;

        // Verde hierba, roca de debajo y el azul de las nubes que rodean la isla.
        public static readonly Color Grass = new Color32(0x8C, 0xC6, 0x63, 255);
        public static readonly Color GrassDark = new Color32(0x6E, 0xA8, 0x4C, 255);
        public static readonly Color Rock = new Color32(0x9B, 0x8A, 0x7A, 255);
        public static readonly Color RockDeep = new Color32(0x6E, 0x60, 0x55, 255);
        public static readonly Color Path = new Color32(0xE0, 0xCF, 0xA8, 255);
        public static readonly Color Water = new Color32(0x6F, 0xC4, 0xE8, 255);
        public static readonly Color TrunkBrown = new Color32(0x8B, 0x63, 0x42, 255);
        public static readonly Color LeafGreen = new Color32(0x63, 0xB0, 0x5A, 255);
        public static readonly Color CloudWhite = new Color32(0xFA, 0xFC, 0xFF, 255);
        public static readonly Color WallCream = new Color32(0xF4, 0xE7, 0xD2, 255);
        public static readonly Color RoofRed = new Color32(0xD9, 0x72, 0x62, 255);

        private static Shader Lit
        {
            get
            {
                if (_lit != null) return _lit;

                _lit = Shader.Find("Universal Render Pipeline/Lit");
                if (_lit == null)
                {
                    // Sin URP activo el juego seguiría corriendo pero todo saldría
                    // magenta, y eso confunde más que un aviso claro.
                    Debug.LogError("ToonPalette: no se encontró el shader de URP. " +
                                   "¿Está el pipeline asignado en Graphics?");
                    _lit = Shader.Find("Standard");
                }
                return _lit;
            }
        }

        /// <summary>Un material opaco de ese color, reutilizado si ya se pidió antes.</summary>
        public static Material Solid(Color color, float smoothness = 0.12f)
        {
            int key = ((Color32)color).GetHashCode() * 397 ^ Mathf.RoundToInt(smoothness * 100f);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var material = new Material(Lit) { name = $"Nimbo_{ColorUtility.ToHtmlStringRGB(color)}" };
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetFloat(MetallicId, 0f);
            material.enableInstancing = true;

            Cache[key] = material;
            return material;
        }

        /// <summary>Un material con textura, para las caras. Estos no se cachean: cada cara es única.</summary>
        public static Material Textured(Texture2D texture)
        {
            var material = new Material(Lit) { name = $"Nimbo_{texture.name}" };
            material.SetTexture(BaseMapId, texture);
            material.SetColor(BaseColorId, Color.white);
            material.SetFloat(SmoothnessId, 0.05f);
            material.SetFloat(MetallicId, 0f);
            return material;
        }

        public static void ClearCache()
        {
            foreach (var material in Cache.Values)
                if (material != null) Object.Destroy(material);
            Cache.Clear();
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    }
}
