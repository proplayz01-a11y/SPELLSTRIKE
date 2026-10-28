Shader "SPELLSTRIKE/VFX/BasicChargeEnergyMobileSafe"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half edge = saturate(1.0h - abs((half)input.uv.y * 2.0h - 1.0h));
                half2 p = (half2)input.uv.xy * 2.0h - 1.0h;
                half disc = saturate(1.0h - dot(p, p));
                half ribbon = edge * edge;
                half glow = disc * disc * disc;
                half useDisc = step(0.5h, (half)input.uv.z);
                half alpha = saturate(lerp(ribbon, glow, useDisc) * input.color.a);
                return half4(input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
