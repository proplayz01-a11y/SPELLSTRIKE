Shader "SPELLSTRIKE/Stage2/WaterSplashParticle"
{
    Properties { _Tint ("Tint", Color) = (0.75, 0.96, 1, 1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color; return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*2.0-1.0;
                float r=length(p);
                float core=1.0-smoothstep(0.15,0.92,r);
                float rim=1.0-smoothstep(0.68,1.0,r);
                float a=saturate(core*0.75+rim*0.35);
                half alpha=_Tint.a*i.color.a*a;
                return half4(_Tint.rgb*i.color.rgb*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
