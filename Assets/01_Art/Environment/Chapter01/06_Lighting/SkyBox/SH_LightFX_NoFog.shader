Shader "Custom/LightFX_NoFog"
{
    Properties
    {
        [MainTexture] _BaseMap ("Beam Texture", 2D) = "white" {}
        [HDR] _BaseColor ("HDR Color", Color) = (1, 0.6, 0.25, 1)
        _Intensity ("Intensity", Range(0, 20)) = 2
        _Opacity ("Opacity", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "LightFX_NoFog"
            Tags { "LightMode"="UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Intensity;
                half _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv =
                    input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );

                half alpha =
                    textureSample.a * _BaseColor.a * _Opacity;

                half3 color =
                    textureSample.rgb *
                    _BaseColor.rgb *
                    _Intensity *
                    alpha;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}