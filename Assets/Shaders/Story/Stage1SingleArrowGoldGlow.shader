Shader "SPELLSTRIKE/Story/Stage1SingleArrowGoldGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Arrow Sprite", 2D) = "white" {}
        _GlowStrength ("Soft Edge Glow", Range(0, 2)) = 1.1
        _GlowSpread ("Glow UV Radius", Range(0.001, 0.025)) = 0.009
        _CoreGain ("Core Brightness", Range(1, 3)) = 1.45
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }
        Pass
        {
            Name "OneSpriteGoldGlow"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _GlowStrength;
                float _GlowSpread;
                float _CoreGain;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half core = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                float2 dx = float2(_GlowSpread, 0);
                float2 dy = float2(0, _GlowSpread);
                half edge = 0;
                edge = max(edge, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + dx).a);
                edge = max(edge, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - dx).a);
                edge = max(edge, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + dy).a);
                edge = max(edge, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - dy).a);
                half halo = saturate(edge - core);
                // A soft halo drawn IN the same sprite quad. No second
                // enlarged arrow or Z-fighting. HDR vertex gold remains
                // bloom-ready without altering the separate tutorial arrows.
                half alpha = saturate((core + halo * _GlowStrength * 0.45) * input.color.a);
                // Keep high-intensity arrows GOLD, not white. Bloom and
                // tone mapping otherwise wash a 5x RGB gold tint to grey.
                half3 warmGold = half3(0.64, 0.30, 0.08);
                half3 rgb = input.color.rgb * warmGold *
                    (core * _CoreGain + halo * _GlowStrength);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
