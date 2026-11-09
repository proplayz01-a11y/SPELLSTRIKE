Shader "SPELLSTRIKE/MenuV2/PortalInwardSwirl"
{
    Properties
    {
        [PerRendererData] _MainTex ("Portal Source", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HoverFactor ("Hover Activity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
               "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Keep the original portal outline stationary. Animate only
                // inner UV samples to avoid the rejected "spinning PNG" look.
                float2 d = (i.uv - 0.5) * 2.0;
                float radius = length(d);
                float interior = 1.0 - smoothstep(0.40, 0.86, radius);
                float t = _Time.y;
                float theta = atan2(d.y, d.x);
                float flow = t * (0.12 + 0.85 * _HoverFactor);
                float angularOffset = flow * (1.55 - radius * 0.8)
                                    + radius * 0.28 * sin(t * 0.36);
                float inwardRipple = (0.008 + 0.016 * _HoverFactor) *
                                     sin(radius * 26.0 + t * (1.2 + _HoverFactor * 2.6));
                float sampledRadius = max(0.0, radius - inwardRipple * interior);
                float2 warpedUV = 0.5 + sampledRadius * 0.5 *
                    float2(cos(theta + angularOffset), sin(theta + angularOffset));
                fixed4 fixedEdge = tex2D(_MainTex, i.uv);
                fixed4 movingCenter = tex2D(_MainTex, warpedUV);
                fixed4 result = lerp(fixedEdge, movingCenter, interior * 0.92);
                result.a *= fixedEdge.a;
                result.rgb *= 1.0 + _HoverFactor * interior * 0.16;
                return saturate(result) * i.color * _Color;
            }
            ENDCG
        }
    }
}
