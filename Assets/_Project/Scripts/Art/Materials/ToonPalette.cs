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

        /// <summary>
        /// El cristal de un escaparate: azul pálido, opaco y algo más brillante.
        /// </summary>
        /// <remarks>
        /// Opaco a propósito. Un cristal transparente enseñaría el interior vacío de la
        /// caja que es el edificio; lo que hace falta es que se lea «ventana» de un
        /// vistazo, y para eso basta el tono del cielo reflejado.
        /// </remarks>
        public static readonly Color Glass = new Color32(0xC7, 0xE4, 0xF2, 255);

        private static Shader Lit
        {
            get
            {
                if (_lit != null) return _lit;

                // El de la isla primero; URP/Lit es el paracaídas. Si el shader
                // propio se queda fuera del empaquetado, el juego se ve más soso
                // pero se ve, que es mejor que magenta.
                _lit = Shader.Find("Nimbo/Toon") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (_lit == null)
                {
                    // En un ejecutable esto significa casi siempre que el shader se
                    // quedó fuera del empaquetado: como todos los materiales se crean
                    // en runtime, ningún asset lo referencia y Unity lo descarta. Se
                    // arregla con RenderPipelineSetup.EnsureAlwaysIncludedShaders.
                    Debug.LogError("ToonPalette: no está el shader URP/Lit. En el editor, " +
                                   "revisa el pipeline en Graphics; en un build, añádelo a " +
                                   "Always Included Shaders (Isla Nimbo > Configurar URP).");

                    _lit = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                }
                return _lit;
            }
        }

        /// <summary>
        /// Un material opaco de ese color, reutilizado si ya se pidió antes.
        /// </summary>
        /// <remarks>
        /// Mate del todo por defecto. Con el brillo que traía, cualquier superficie
        /// ancha y poco curvada —el hombro, la tapa de una mesa, el tejado— cogía una
        /// franja blanca de reflejo que se leía como una pieza aparte: en la primera
        /// captura de los muñecos parecía que llevaran un plato al cuello. Un juego de
        /// colores planos no quiere reflejos especulares en ningún sitio.
        /// </remarks>
        public static Material Solid(Color color, float smoothness = 0f)
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

        /// <summary>
        /// El material de una cara: con textura y transparente, porque los rasgos se
        /// pintan sobre la esfera de la cabeza y todo lo demás tiene que dejarla ver.
        /// </summary>
        /// <remarks>
        /// No se cachea — cada cara es única — y hay que configurar la mezcla a mano:
        /// en URP, poner Surface Type a transparente desde código es esto, y si se
        /// olvida alguna de las líneas el resultado es una cara opaca o una invisible.
        /// </remarks>
        public static Material Textured(Texture2D texture)
        {
            var material = new Material(Lit) { name = $"Nimbo_{texture.name}" };
            material.SetTexture(BaseMapId, texture);
            material.SetColor(BaseColorId, Color.white);
            material.SetFloat(SmoothnessId, 0.05f);
            material.SetFloat(MetallicId, 0f);

            material.SetFloat(SurfaceId, 1f);                 // 1 = transparente
            material.SetFloat(BlendId, 0f);                   // alfa clásico
            material.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat(DstBlendId, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat(ZWriteId, 0f);
            material.SetFloat(AlphaClipId, 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");

            return material;
        }

        public static void ClearCache()
        {
            foreach (var material in Cache.Values) Discard(material);
            Cache.Clear();
        }

        /// <summary>
        /// Destruye de la forma que toque. <c>Destroy</c> no hace nada fuera de modo
        /// juego y además suelta un error: en el editor y en los tests hay que usar
        /// <c>DestroyImmediate</c>, y sin esto los materiales se quedaban colgados.
        /// </summary>
        private static void Discard(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Object.Destroy(asset);
            else Object.DestroyImmediate(asset);
        }


        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
    }
}
