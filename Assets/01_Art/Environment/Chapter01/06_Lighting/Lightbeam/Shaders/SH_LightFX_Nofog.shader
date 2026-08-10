Shader "Environment/LightFX/NoFogLightBeam"
{
    Properties
    {
        [MainTexture] _BaseMap("Beam Texture", 2D) = "white" {}
        [HDR] _BaseColor("Beam Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Float) = 2
        _Opacity("Opacity", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "LightBeam"

            Blend SrcAlpha One
            ZWrite Off
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
                float4 _BaseColor;
                float _Intensity;
                float _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv =
                    TRANSFORM_TEX(input.uv, _BaseMap);

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
                    textureSample.a *
                    _BaseColor.a *
                    _Opacity;

                half3 color =
                    textureSample.rgb *
                    _BaseColor.rgb *
                    _Intensity;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}