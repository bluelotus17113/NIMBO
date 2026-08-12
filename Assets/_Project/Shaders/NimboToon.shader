// El shader de toda la isla. Ver ToonPalette, que es quien lo reparte.
//
// Lo que hace distinto a URP/Lit, y por qué:
//
//   Luz envuelta. Una esfera con Lambert se apaga de golpe en el ecuador y queda
//   media cara plana y muerta. Envolviendo la luz —que llegue algo más allá del
//   borde— la cabeza de un muñeco vuelve a leerse redonda. Es lo que hace que
//   estos juegos parezcan de plástico blando y no de piedra.
//
//   Sombra con color. Una sombra que solo resta luz se ve gris, y el gris es lo
//   que separa un juego amable de uno realista. Aquí la parte a la sombra se tiñe
//   de violeta frío en vez de oscurecerse a secas: es la diferencia entre una
//   tarde y un eclipse.
//
//   Contraluz. Un hilo de cielo en el borde de las siluetas. Despega las cosas
//   del fondo sin tener que dibujarles un contorno.
//
//   Nada de reflejos. Un plano ancho —un tejado, una mesa, un hombro— cogía una
//   franja blanca que se leía como una pieza suelta.

Shader "Nimbo/Toon"
{
    Properties
    {
        _BaseMap("Textura", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Smoothness("Brillo", Range(0, 1)) = 0

        _Wrap("Envoltura de la luz", Range(0, 1)) = 0.45
        // Casi no oscurece: solo enfría. Con 0,60 la cara en sombra perdía cuarenta
        // por ciento de luz y la aldea entera se veía de cemento.
        _ShadowTint("Tinte de la sombra", Color) = (0.88, 0.90, 1.0, 1)
        _RimColor("Color del contraluz", Color) = (0.86, 0.94, 1, 1)
        _RimPower("Cierre del contraluz", Range(0.5, 8)) = 3.2
        _RimStrength("Fuerza del contraluz", Range(0, 1)) = 0.16

        // Cuánto apaga una sombra. A uno, lo que está a la sombra pierde el sol
        // entero y el prado se parte en dos verdes muy distintos; en estos juegos
        // la sombra baja la luz un punto y no la quita.
        _ShadowStrength("Fuerza de la sombra", Range(0, 1)) = 0.72

        // Las escribe ToonPalette para el material de las caras, que es el único
        // transparente. Ocultas porque no se tocan a mano.
        [HideInInspector] _Surface("__surface", Float) = 0
        [HideInInspector] _Blend("__blend", Float) = 0
        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _ZWrite("__zwrite", Float) = 1
        [HideInInspector] _AlphaClip("__clip", Float) = 0
        [HideInInspector] _Cutoff("__cutoff", Float) = 0.5
        [HideInInspector] _Metallic("__metallic", Float) = 0
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
            half4 _ShadowTint;
            half4 _RimColor;
            half _Smoothness;
            half _Wrap;
            half _RimPower;
            half _RimStrength;
            half _ShadowStrength;
            half _Cutoff;
            half _Surface;
            half _Blend;
            half _SrcBlend;
            half _DstBlend;
            half _ZWrite;
            half _AlphaClip;
            half _Metallic;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                float3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light main = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // La luz envuelve el borde en vez de cortarse en el ecuador, y
                // después se suaviza para que no quede un filo donde acaba.
                half ndl = dot(normalWS, main.direction);
                half wrapped = saturate((ndl + _Wrap) / (1.0h + _Wrap));
                wrapped = wrapped * wrapped * (3.0h - 2.0h * wrapped);

                half atten = lerp(1.0h, main.shadowAttenuation, _ShadowStrength);
                half lit = wrapped * atten;

                // Lo que está a la sombra no se apaga: se enfría. El tinte se
                // aplica al rebote, que es la única luz que le llega.
                half3 ambient = SampleSH(normalWS);
                half3 tint = lerp(_ShadowTint.rgb, half3(1.0h, 1.0h, 1.0h), lit);

                half3 color = albedo.rgb * (ambient * tint + main.color * lit);

                // Un brillo suave, y solo si el material lo pide: el cristal de un
                // escaparate lo quiere y una pared no.
                if (_Smoothness > 0.001h)
                {
                    half3 halfway = normalize(main.direction + viewWS);
                    half spec = pow(saturate(dot(normalWS, halfway)), exp2(_Smoothness * 7.0h + 2.0h));
                    color += main.color * spec * _Smoothness * lit;
                }

                half rim = pow(1.0h - saturate(dot(normalWS, viewWS)), _RimPower);
                color += _RimColor.rgb * (rim * _RimStrength * (0.35h + 0.65h * wrapped));

                color = MixFog(color, input.fogFactor);
                return half4(color, albedo.a);
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
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
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
