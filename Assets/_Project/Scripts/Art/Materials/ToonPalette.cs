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
        private static readonly Dictionary<int, Material> FoliageCache = new Dictionary<int, Material>(16);
        private static Shader _lit;
        private static Shader _foliage;

        // Verde hierba, roca de debajo y el azul de las nubes que rodean la isla.
        //
        // Los verdes vienen medidos de las referencias de estilo (AnimeGrass y
        // AnimeTree): el prado tira a amarillo y la copa a azul, que es lo que hace
        // que un árbol se recorte contra la hierba sin necesidad de contorno. Los
        // pasteles de antes se parecían demasiado entre sí y la isla era una sola
        // mancha verde clara.
        public static readonly Color Grass = new Color32(0x63, 0xB0, 0x48, 255);
        public static readonly Color GrassDark = new Color32(0x3F, 0x84, 0x38, 255);
        public static readonly Color Rock = new Color32(0x7C, 0x71, 0x66, 255);
        public static readonly Color RockDeep = new Color32(0x6E, 0x60, 0x55, 255);
        public static readonly Color Path = new Color32(0xDF, 0xCF, 0xA6, 255);
        public static readonly Color Water = new Color32(0x6F, 0xC4, 0xE8, 255);
        public static readonly Color TrunkBrown = new Color32(0x6B, 0x5A, 0x43, 255);
        public static readonly Color LeafGreen = new Color32(0x4C, 0x94, 0x40, 255);

        /// <summary>Los tres colores de las flores del prado, de la lámina.</summary>
        public static readonly Color[] Flowers =
        {
            new Color32(0xFA, 0xFA, 0xF2, 255),   // blanca
            new Color32(0xF5, 0xC9, 0x4C, 255),   // amarilla
            new Color32(0xF0, 0x8F, 0xB4, 255),   // rosa
        };
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

        /// <summary>
        /// Los cuatro colores con los que se pinta una superficie.
        /// </summary>
        /// <remarks>
        /// No es un color y sus versiones más oscuras: son cuatro colores distintos,
        /// como los elige un pintor de fondos. Ver <see cref="BandsOf"/>.
        /// </remarks>
        public readonly struct Bands
        {
            public readonly Color Deep, Shadow, Lit, High;

            public Bands(Color deep, Color shadow, Color lit, Color high)
            {
                Deep = deep; Shadow = shadow; Lit = lit; High = high;
            }
        }

        /// <summary>
        /// Las cuatro bandas de un color, con los giros medidos de la referencia.
        /// </summary>
        /// <remarks>
        /// La rampa del follaje de AnimeTree tiene cuatro colores, y al mirarlos en
        /// tono-saturación-claridad resultaron ser una progresión regular: el tono
        /// gira 23° hacia el cian en la sombra y 51° en el fondo, y 23° hacia el
        /// amarillo en el sol; la claridad va por 0,695 · 0,790 · 1 · 1,284. Con
        /// esos seis números se reconstruyen los cuatro verdes del original clavados,
        /// y se le pueden pedir a cualquier otro color.
        ///
        /// El giro solo va hacia el frío si el color **es** verde. Un tronco no: la
        /// corteza de la referencia va al revés —del 40° al 30°, hacia el rojo— y
        /// empujar una madera hacia el cian la deja de color ceniza. Son dos casos
        /// porque hay dos referencias, no porque haya una teoría.
        ///
        /// Y cuando la claridad ya no puede subir más, el sol se abre desaturando en
        /// vez de recortar: si no, un color claro pierde el tono en la banda alta y
        /// aparece una calva blanca donde da el sol.
        /// </remarks>
        public static Bands BandsOf(Color baseColour)
        {
            Color.RGBToHSV(baseColour, out float h, out float s, out float v);

            bool green = h >= 0.18f && h <= 0.48f;
            float turn = green ? 0.064f : -0.028f;

            return new Bands(
                deep: Shift(h + turn * 2.22f, s * 0.98f, v * 0.695f),
                shadow: Shift(h + turn, s, v * 0.790f),
                lit: baseColour,
                high: Shift(h - turn, s * 0.847f, v * 1.284f));
        }

        private static Color Shift(float h, float s, float v)
        {
            if (v > 1f) { s /= v; v = 1f; }
            return Color.HSVToRGB(Mathf.Repeat(h, 1f), Mathf.Clamp01(s), Mathf.Clamp01(v));
        }

        /// <summary>Deja escritas las cuatro bandas de ese color en el material.</summary>
        /// <remarks>
        /// Y con ellas la pareja de la mancha, que son los dos extremos de la rampa
        /// divididos por el color de luz: multiplicando por ellos, una zona del prado
        /// se va hacia el verde azulado del fondo y otra hacia el amarillo del sol sin
        /// tener que elegir dos colores más a mano. Es «choose the right pair» de la
        /// lámina, resuelto por construcción.
        /// </remarks>
        private static void ApplyBands(Material material, Color colour)
        {
            var bands = BandsOf(colour);
            material.SetColor(BandDeepId, bands.Deep);
            material.SetColor(BandShadowId, bands.Shadow);
            material.SetColor(BandLitId, bands.Lit);
            material.SetColor(BandHighId, bands.High);

            // La pareja no son los dos extremos crudos: son la banda de sombra y un
            // 45 % del camino hacia la de sol. Con los extremos enteros, la mancha
            // clara multiplicaba el rojo por casi cuatro —en lineal, la banda alta de
            // un verde saturado es cuatro veces la de luz— y el prado salía a
            // manchas amarillas quemadas. Media distancia deja la pareja de la lámina
            // —fría y oscura contra cálida y clara— sin salirse del verde.
            material.SetVector(VariaCoolId, Ratio(bands.Shadow, bands.Lit));
            material.SetVector(VariaWarmId, Ratio(Color.Lerp(bands.Lit, bands.High, 0.45f),
                                                  bands.Lit));
        }

        /// <summary>
        /// Un color dividido por otro, para usarlo de multiplicador.
        /// </summary>
        /// <remarks>
        /// **En lineal y como vector, no como color.** Las dos cosas por el mismo
        /// motivo: un color que se manda a un material se convierte de gama a lineal
        /// por el camino, y esto no es un color sino una razón entre dos. Convertida,
        /// un 1,30 llegaría al shader como 1,77 y la mancha saldría el doble de fuerte
        /// de lo que dice el número.
        ///
        /// Acotada entre un tercio y el triple: un color casi negro en un canal —el
        /// rojo de un verde saturado, sin ir más lejos— da una división enorme, y sin
        /// tope la mancha salía fosforescente en ese canal.
        /// </remarks>
        private static Vector4 Ratio(Color numerator, Color denominator)
        {
            var a = numerator.linear;
            var b = denominator.linear;

            float R(float x, float y) => Mathf.Clamp(x / Mathf.Max(y, 0.004f), 0.35f, 2.2f);
            return new Vector4(R(a.r, b.r), R(a.g, b.g), R(a.b, b.b), 1f);
        }

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
            ApplyBands(material, color);
            material.enableInstancing = true;

            Cache[key] = material;
            return material;
        }

        /// <summary>
        /// Piedra: el mismo material, con el salto de la rampa duro.
        /// </summary>
        /// <remarks>
        /// El suelo de la referencia de los árboles tiene la rampa con las dos paradas
        /// en el **mismo** sitio: un corte, sin degradado ninguno. Es lo que dibuja
        /// una arista de piedra. Con el salto suave que lleva todo lo demás, una roca
        /// facetada pierde justo lo que la hace una roca.
        /// </remarks>
        public static Material Stone(Color colour)
        {
            int key = ((Color32)colour).GetHashCode() * 397 ^ 0x51A7;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var material = new Material(Lit)
            {
                name = $"NimboPiedra_{ColorUtility.ToHtmlStringRGB(colour)}",
            };

            material.SetColor(BaseColorId, colour);
            material.SetFloat(SmoothnessId, 0f);
            material.SetFloat(MetallicId, 0f);
            ApplyBands(material, colour);
            material.SetFloat(SoftLowId, 0.02f);
            material.SetFloat(SoftHighId, 0.03f);
            material.enableInstancing = true;

            Cache[key] = material;
            return material;
        }

        /// <summary>
        /// El material de la vegetación: se mueve con el viento y se ve por las dos
        /// caras.
        /// </summary>
        /// <remarks>
        /// No se cachea junto a los sólidos porque no es el mismo shader, y sí por
        /// color, que es lo que importa: un prado son miles de matas del mismo verde.
        ///
        /// La malla tiene que traer el color del vértice escrito —oclusión, máscara
        /// de viento, degradado y semilla—; ver el encabezado de Nimbo/Foliage. Una
        /// malla sin ese color sale plana y quieta.
        /// </remarks>
        public static Material Foliage(Color colour, float windStrength = 0.22f,
                                       float rootDarken = 0.30f)
        {
            int key = ((Color32)colour).GetHashCode() * 131
                    ^ Mathf.RoundToInt(windStrength * 1000f) * 7
                    ^ Mathf.RoundToInt(rootDarken * 1000f);
            if (FoliageCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Vegetation;
            var material = new Material(shader)
            {
                name = $"NimboHoja_{ColorUtility.ToHtmlStringRGB(colour)}",
            };

            material.SetColor(BaseColorId, colour);
            ApplyBands(material, colour);
            material.SetFloat(WindStrengthId, windStrength);
            material.SetFloat(RootDarkenId, rootDarken);
            material.enableInstancing = true;

            FoliageCache[key] = material;
            return material;
        }

        /// <summary>
        /// El shader de la vegetación, con el de la isla de paracaídas.
        /// </summary>
        /// <remarks>
        /// Si este no llega al empaquetado se pierden el viento y las dos caras, pero
        /// la hierba se sigue viendo y del color que toca. Quedarse sin nada sería
        /// peor: un prado pelado se lee como un fallo de carga.
        /// </remarks>
        private static Shader Vegetation
        {
            get
            {
                if (_foliage != null) return _foliage;
                _foliage = Shader.Find("Nimbo/Foliage") ?? Lit;
                return _foliage;
            }
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

            // Bandas neutras: los rasgos ya vienen pintados en la textura y el color
            // de la piel lo pone la esfera de detrás. Lo que hacen aquí las bandas es
            // que un ojo a la sombra se apague igual que la mejilla que tiene al lado.
            ApplyBands(material, Color.white);
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

            foreach (var material in FoliageCache.Values) Discard(material);
            FoliageCache.Clear();
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


        private static readonly int BandDeepId = Shader.PropertyToID("_BandDeep");
        private static readonly int BandShadowId = Shader.PropertyToID("_BandShadow");
        private static readonly int BandLitId = Shader.PropertyToID("_BandLit");
        private static readonly int BandHighId = Shader.PropertyToID("_BandHigh");
        private static readonly int VariaCoolId = Shader.PropertyToID("_VariaCool");
        private static readonly int VariaWarmId = Shader.PropertyToID("_VariaWarm");
        private static readonly int SoftLowId = Shader.PropertyToID("_SoftLow");
        private static readonly int SoftHighId = Shader.PropertyToID("_SoftHigh");
        private static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
        private static readonly int RootDarkenId = Shader.PropertyToID("_RootDarken");

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
