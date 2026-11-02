Shader "SPELLSTRIKE/UI/BeveledGoldHoverReflection"
{
    Properties
    {
        [PerRendererData] _MainTex ("Reflectivity Alpha Mask", 2D) = "white" {}
        _Tint ("Reflection Tint", Color) = (1, 0.93, 0.72, 1)
        _Phase ("Clockwise Border Progress", Range(0,1)) = 0
        _SweepWidth ("Sweep Band Width", Range(0.001,0.2)) = 0.034
        _Softness ("Sweep Feather", Range(0.001,0.1)) = 0.022
        _Intensity ("Moving Intensity", Range(0,4)) = 0
        _AmbientIntensity ("Resting Hover Intensity", Range(0,0.5)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float4 worldPosition : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _Tint, _ClipRect;
            float _Phase, _SweepWidth, _Softness, _Intensity, _AmbientIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // Arc-length approximation on the four edges of the 872x172 card.
            // Gold-mask alpha confines the sweep to the actual curved bevel silhouette.
            // Top-left -> top-right -> bottom-right -> bottom-left -> top-left.
            float BorderProgress(float2 uv)
            {
                float2 p = uv * float2(872.0,172.0);
                float distTop = 172.0 - p.y;
                float distRight = 872.0 - p.x;
                float distBottom = p.y;
                float distLeft = p.x;
                float perimeter = 2.0 * (872.0 + 172.0);
                float d = min(min(distTop,distRight),min(distBottom,distLeft));
                if (d == distTop) return p.x / perimeter;
                if (d == distRight) return (872.0 + (172.0-p.y)) / perimeter;
                if (d == distBottom) return (872.0+172.0+(872.0-p.x)) / perimeter;
                return (872.0+172.0+872.0+p.y) / perimeter;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float reflectivity = tex2D(_MainTex, i.uv).a; // ALPHA, NOT RGB.
                float progress = BorderProgress(i.uv);
                float d = abs(frac(progress - _Phase + 0.5) - 0.5);
                float band = 1.0 - smoothstep(_SweepWidth * 0.5, _SweepWidth * 0.5 + _Softness, d);
                float opacity = reflectivity * (_AmbientIntensity + band * _Intensity) * i.color.a * _Tint.a;
                #ifdef UNITY_UI_CLIP_RECT
                    opacity *= UnityGet2DClipping(i.worldPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(opacity - 0.001);
                #endif
                return fixed4(_Tint.rgb, saturate(opacity));
            }
            ENDCG
        }
    }
}
