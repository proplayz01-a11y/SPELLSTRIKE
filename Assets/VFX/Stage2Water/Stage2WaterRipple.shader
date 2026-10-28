Shader "SPELLSTRIKE/Stage2/WaterRipple"
{
    Properties
    {
        _Color ("Color", Color) = (0.78, 0.97, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _RingRadius ("Ring Radius", Range(0.1, 0.5)) = 0.39
        _RingWidth ("Ring Width", Range(0.005, 0.2)) = 0.095
    }
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
                half4 _Color;
                float _Opacity;
                float _RingRadius;
                float _RingWidth;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float d=distance(i.uv,float2(0.5,0.5));
                float ring=1.0-smoothstep(_RingWidth*0.45,_RingWidth,abs(d-_RingRadius));
                float edge=1.0-smoothstep(0.46,0.5,d);
                half alpha=_Color.a*ring*edge*_Opacity;
                return half4(_Color.rgb*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
