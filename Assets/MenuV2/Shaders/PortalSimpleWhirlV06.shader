Shader "SPELLSTRIKE/MenuV2/PortalSimpleWhirlV06"
{
    Properties
    {
        [PerRendererData] _MainTex ("Portal Source", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HoverFactor ("Hover Activity", Range(0,1)) = 0
        _FlowPhase ("Hover Driven Rotation", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True"
               "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _HoverFactor;
            float _FlowPhase;
            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
                fixed4 color:COLOR;
            };
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float2 uv:TEXCOORD0;
                fixed4 color:COLOR;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.color=v.color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Rotate a single existing swirl field. NO radial ripple,
                // sinusoidal angular shear, cloning or creation of spiral arms.
                // The still portal boundary never rotates like a flat sticker.
                float2 d = (i.uv - .5) * 2.0;
                float radius = length(d);
                float inner = 1.0 - smoothstep(.63, .89, radius);
                float angle = _FlowPhase * .80;
                float c = cos(angle), s = sin(angle);
                float2 rotated = .5 + float2(
                    d.x * c - d.y * s,
                    d.x * s + d.y * c) * .5;
                // Sample only ONE version of the spiral at any pixel. Mixing
                // original and moving frames produced overlapping/doubled
                // spiral arms during the old hover fade.
                float2 singleSwirlUV = lerp(i.uv, rotated, inner);
                fixed4 result = tex2D(_MainTex, singleSwirlUV);
                result.a *= tex2D(_MainTex, i.uv).a;
                result.rgb *= 1.0 + .10 * _HoverFactor * inner;
                return saturate(result) * i.color * _Color;
            }
            ENDCG
        }
    }
}
