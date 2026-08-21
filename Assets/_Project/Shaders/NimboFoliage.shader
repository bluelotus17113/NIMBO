// La vegetación: hierba, copas de árbol y matas.
//
// Es el shader de la isla (Nimbo/Toon) con las tres cosas que la vegetación
// necesita y el resto no:
//
//   Viento en el vértice. Ver NimboWind en NimboAnime.hlsl. Va también en el pase
//   de sombras: si la hierba se mueve y su sombra no, la sombra la delata.
//
//   Dos caras. Una brizna es una lámina; vista por detrás, con la cara de atrás
//   descartada, desaparece. Se dibujan las dos con la **misma** normal: ver la nota
//   en el fragmento, que es donde está el motivo y no es el obvio.
//
//   Raíz oscura. La brizna se apaga hacia abajo y la copa hacia dentro. En la
//   referencia es un degradado en coordenadas de objeto; aquí viene escrito en el
//   vértice, que sale más barato y aguanta que la malla se gire.
//
// **Lo que la malla tiene que traer escrito en el color del vértice:**
//   r = oclusión — 0 metido dentro del bulto, 1 a la intemperie
//   g = máscara de viento — 0 en la raíz, 1 en la punta
//   b = degradado raíz-punta para el tono
//   a = semilla de la mata, para que dos matas vecinas no sean del mismo verde
//
// Sin ese contrato la vegetación sale plana y quieta, no rota: el color del
// vértice sin escribir es negro, y negro significa raíz, sombra y sin viento.

Shader "Nimbo/Foliage"
{
    Properties
    {
        _BaseMap("Recorte", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)

        _BandDeep("Banda profunda", Color) = (0.19, 0.43, 0.35, 1)
        _BandShadow("Banda de sombra", Color) = (0.21, 0.49, 0.27, 1)
        _BandLit("Banda de luz", Color) = (0.33, 0.62, 0.26, 1)
        _BandHigh("Banda de sol", Color) = (0.62, 0.80, 0.41, 1)

        // El follaje lleva el salto más ancho que el resto, y no es capricho. En la
        // referencia la copa son cuarenta tarjetas de hojas recortadas: el borde
        // entre luz y sombra nunca es una línea porque lo rompen las hojas. Aquí la
        // copa es un bulto liso, así que con el salto estrecho salía una raya de
        // dientes de sierra cruzando el árbol, siguiendo las aristas de los
        // triángulos. El ancho hace de hojas.
        _EdgeLow("Salto a la luz", Range(0, 1)) = 0.42
        _SoftLow("Ancho del salto", Range(0.002, 0.5)) = 0.22
        // El follaje entra en la banda de sol más tarde que el suelo. Una brizna
        // hereda la normal del suelo, así que le llega la misma luz; con el mismo
        // umbral y su pizca de variación por mata, media hierba cruzaba a la banda
        // alta y el prado salía de matas blanquecinas sobre verde. La banda de sol
        // se le reserva a lo que de verdad mira al sol: el lomo de una copa.
        _EdgeHigh("Salto al sol", Range(0, 1)) = 0.93
        _SoftHigh("Ancho del segundo", Range(0.002, 0.5)) = 0.07

        // Cuánta luz llega a lo que no mira al sol. Sin esto, la cara de sombra de
        // cualquier bulto cae a cero y se pinta toda del mismo color: la copa del
        // Árbol Nimbo salía de un solo verde apagado de arriba abajo, porque desde
        // la plaza se le ve la cara norte entera y el sol viene de detrás. En la
        // referencia esto no hace falta porque la rampa se alimenta del render, que
        // ya trae el rebote; aquí se alimenta del coseno pelado y hay que sumárselo.
        _AmbientLift("Luz de relleno", Range(0, 0.6)) = 0.22

        _Variation("Variación de color", Range(0, 1)) = 0.36
        _VariationScale("Tamaño de la mancha", Float) = 12
        _VariaCool("Tinte de la mancha fría", Vector) = (0.62, 1.12, 1.30, 1)
        _VariaWarm("Tinte de la mancha cálida", Vector) = (1.55, 1.28, 0.95, 1)
        _ClumpVariation("Variación por mata", Range(0, 0.5)) = 0.05
        _RootDarken("Oscurecido de la raíz", Range(0, 1)) = 0.16
        _UseVertexAO("Usa la oclusión del vértice", Range(0, 1)) = 1

        _WindStrength("Fuerza del viento", Range(0, 2)) = 0.22
        _WindScale("Tamaño de la racha", Float) = 9
        _WindSpeed("Velocidad de la racha", Float) = 0.5
        _WindDirection("Hacia dónde sopla", Vector) = (0.82, 0.57, 0, 0)

        _RimColor("Color del contraluz", Color) = (0.86, 0.94, 1, 1)
        _RimPower("Cierre del contraluz", Range(0.5, 8)) = 2.6
        _RimStrength("Fuerza del contraluz", Range(0, 1)) = 0.22
        _ShadowStrength("Fuerza de la sombra", Range(0, 1)) = 0.85

        _Cutoff("Recorte", Range(0, 1)) = 0.5
        [HideInInspector] _AlphaClip("__clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _BandDeep;
            half4 _BandShadow;
            half4 _BandLit;
            half4 _BandHigh;
            float4 _VariaCool;
            float4 _VariaWarm;
            half4 _RimColor;
            float4 _WindDirection;
            half _EdgeLow;
            half _SoftLow;
            half _EdgeHigh;
            half _SoftHigh;
            half _AmbientLift;
            half _Variation;
            float _VariationScale;
            half _ClumpVariation;
            half _RootDarken;
            half _UseVertexAO;
            float _WindStrength;
            float _WindScale;
            float _WindSpeed;
            half _RimPower;
            half _RimStrength;
            half _ShadowStrength;
            half _Cutoff;
            half _AlphaClip;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "NimboAnime.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  colour     : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                half4  colour     : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += NimboWind(positionWS, input.colour.g, _WindDirection.xy,
                                        _WindScale, _WindSpeed, _WindStrength);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.colour = input.colour;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 recorte = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                #ifdef _ALPHATEST_ON
                    clip(recorte.a * _BaseColor.a - _Cutoff);
                #endif

                // **La normal no se le da la vuelta a la cara de atrás, y esto costó
                // una tanda de capturas.** Lo normal es voltearla, porque en una
                // lámina la normal de verdad apunta a un lado. Pero aquí la normal no
                // es la de la geometría: es la del **suelo**, escrita a propósito para
                // que el césped se ilumine como una superficie. Volteada, una brizna
                // vista por detrás pasaba a tener la normal mirando al suelo, se
                // quedaba sin sol y caía a la banda profunda: el prado salió de
                // briznas azul verdosas sobre un suelo verde claro, como un cepillo
                // mojado. La normal escrita vale para las dos caras.
                float3 normalWS = normalize(input.normalWS);
                float3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light main = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                half ndl = saturate(dot(normalWS, main.direction));
                half atten = lerp(1.0h, main.shadowAttenuation, _ShadowStrength);

                // El relleno entra antes de la rampa, no después: lo que decide de
                // qué banda es un píxel es el nivel, y una cara sin sol tiene que
                // caer en la de sombra, no por debajo de ella.
                half level = (ndl * (1.0h - _AmbientLift) + _AmbientLift) * atten;

                half mottle = NimboVariation(input.positionWS, _VariationScale);
                level += (mottle - 0.5h) * 0.12h;
                level += (input.colour.a - 0.5h) * _ClumpVariation * 2.0h;
                level = saturate(level + (input.colour.b - 1.0h) * _RootDarken);

                NimboBands bands;
                bands.deep = _BandDeep.rgb;
                bands.shadow = _BandShadow.rgb;
                bands.lit = _BandLit.rgb;
                bands.high = _BandHigh.rgb;
                bands.edgeLow = _EdgeLow;
                bands.softLow = _SoftLow;
                bands.edgeHigh = _EdgeHigh;
                bands.softHigh = _SoftHigh;

                half occlusion = lerp(1.0h, input.colour.r, _UseVertexAO);
                half3 colour = NimboShade(bands, level, occlusion);
                colour = NimboMottle(colour, _VariaCool.rgb, _VariaWarm.rgb, mottle, _Variation);

                half rim = pow(1.0h - saturate(dot(normalWS, viewWS)), _RimPower);
                colour += _RimColor.rgb * (rim * _RimStrength * (0.30h + 0.70h * ndl));

                colour = MixFog(colour, input.fogFactor);
                return half4(colour, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            // **La cara de delante se descarta al proyectar la sombra, y esto es
            // importante.** Una copa es un volumen cerrado: si el mapa de sombras
            // guarda su cara más cercana al sol, esa misma cara sale sombreada por sí
            // misma al pintarla y la copa entera cae a la banda oscura. Se vio en la
            // primera captura: el árbol salió de un solo verde apagado de arriba
            // abajo. Guardando la cara de atrás, la profundidad queda un diámetro más
            // lejos y la de delante se ve limpia. La sombra que proyecta no cambia:
            // sale del otro lado del mismo bulto.
            //
            // La hierba no se entera de esto porque no proyecta sombra ninguna.
            Cull Front

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "NimboAnime.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  colour     : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // El mismo empujón que en el pase de color, o la sombra se queda
                // quieta debajo de una hierba que se mueve.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += NimboWind(positionWS, input.colour.g, _WindDirection.xy,
                                        _WindScale, _WindSpeed, _WindStrength);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a
                         * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NimboAnime.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  colour     : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += NimboWind(positionWS, input.colour.g, _WindDirection.xy,
                                        _WindScale, _WindSpeed, _WindStrength);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a
                         * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
