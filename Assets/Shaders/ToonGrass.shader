// ═════════════════════════════════════════════════════════════════════════════
// SpellStrike/ToonGrass.shader
// URP Stylized Toon Grass — Full Implementation
// Equivalent to the Shader Graph tutorial by the YouTube video
//
// FEATURES:
//   ✓ Depth-based near/far color gradient
//   ✓ Height-based bottom/top color blend per blade
//   ✓ Terrain render texture blending
//   ✓ Shadow attenuation (custom HLSL MainLight)
//   ✓ Idle grass sway (gradient noise vertex animation)
//   ✓ Wind wave texture (color + vertex — God of War style)
//   ✓ GPU Instancing ready
//   ✓ Alpha clipping for grass transparency
// ═════════════════════════════════════════════════════════════════════════════

Shader "SpellStrike/ToonGrass"
{
    Properties
    {
        // ── Grass Texture ──────────────────────────────────────────────────
        [Header(Grass Texture)]
        _MainTex            ("Blade Texture",           2D)         = "white" {}
        _AlphaClip          ("Alpha Clip Threshold",    Range(0,1)) = 0.5

        // ── Depth Color (Near / Far from camera) ──────────────────────────
        [Header(Depth Color)]
        _NearColor          ("Near Color",              Color)      = (0.78, 0.90, 0.30, 1)
        _FarColor           ("Far Color",               Color)      = (0.10, 0.45, 0.12, 1)
        _NearFarRange       ("Near Far Range (X=near Y=far)", Vector) = (10, 25, 0, 0)

        // ── Height Blend (Bottom / Top of blade) ──────────────────────────
        [Header(Height Blend)]
        _BottomColor        ("Bottom Color",            Color)      = (0.02, 0.15, 0.04, 1)
        _HeightBlend        ("Height Blend",            Range(0,3)) = 1.0

        // ── Terrain Color ─────────────────────────────────────────────────
        [Header(Terrain Blending)]
        _TerrainColor       ("Terrain Render Texture",  2D)         = "black" {}
        _TerrainSize        ("Terrain Size",            Float)      = 50.0
        _TerrainOffset      ("Terrain Offset",          Float)      = 0.0
        _TerrainPower       ("Terrain Color Power",     Range(0,1)) = 0.25
        [Toggle] _UseTerrainColor ("Use Terrain Color", Float)      = 1.0

        // ── Shadow ────────────────────────────────────────────────────────
        [Header(Shadow)]
        _ShadowColor        ("Shadow Color",            Color)      = (0.02, 0.08, 0.03, 1)

        // ── Idle Grass Sway ───────────────────────────────────────────────
        [Header(Idle Sway)]
        _WindSpeed          ("Sway Speed",              Range(0,1)) = 0.1
        _WindIntensity      ("Sway Intensity",          Range(0,1)) = 0.2

        // ── Wind Wave Texture ─────────────────────────────────────────────
        [Header(Wind Wave)]
        _WindTex            ("Wind Texture",            2D)         = "white" {}
        _WindNoiseScale     ("Wind Noise Scale",        Vector)     = (40, 100, 0, 0)
        _WindNoiseSpeed     ("Wind Noise Speed",        Range(0,5)) = 1.0
        _WindNoiseContrast  ("Wind Noise Contrast (X=edge1 Y=edge2)", Vector) = (0.3, 0.7, 0, 0)
        _WindHeight         ("Wind Height (blade top affected)", Range(0,2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "TransparentCutout"
            "Queue"             = "AlphaTest"
            "RenderPipeline"    = "UniversalPipeline"
            "IgnoreProjector"   = "True"
        }

        // No backface culling — grass blades visible from both sides
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   GrassVert
            #pragma fragment GrassFrag
            #pragma target   3.5

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Properties (CBUFFER for instancing) ───────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4  _MainTex_ST;
                float   _AlphaClip;

                float4  _NearColor;
                float4  _FarColor;
                float4  _NearFarRange;      // x = near, y = far

                float4  _BottomColor;
                float   _HeightBlend;

                float4  _TerrainColor_ST;
                float   _TerrainSize;
                float   _TerrainOffset;
                float   _TerrainPower;
                float   _UseTerrainColor;

                float4  _ShadowColor;

                float   _WindSpeed;
                float   _WindIntensity;

                float4  _WindTex_ST;
                float4  _WindNoiseScale;    // x = scaleX, y = scaleY
                float   _WindNoiseSpeed;
                float4  _WindNoiseContrast; // x = edge1, y = edge2
                float   _WindHeight;
            CBUFFER_END

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_TerrainColor);   SAMPLER(sampler_TerrainColor);
            TEXTURE2D(_WindTex);        SAMPLER(sampler_WindTex);

            // ── Structs ───────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;    // world pos for depth + terrain + shadow
                float  bladeY       : TEXCOORD2;    // local Y for height blend
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Helpers ───────────────────────────────────────────────────

            // Simple hash-based pseudo gradient noise (no Texture needed)
            // Approximates Unity's Gradient Noise node
            float2 GradientNoiseHash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float GradientNoise(float2 uv, float scale)
            {
                uv *= scale;
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(dot(GradientNoiseHash(i + float2(0,0)), f - float2(0,0)),
                                 dot(GradientNoiseHash(i + float2(1,0)), f - float2(1,0)), u.x),
                            lerp(dot(GradientNoiseHash(i + float2(0,1)), f - float2(0,1)),
                                 dot(GradientNoiseHash(i + float2(1,1)), f - float2(1,1)), u.x),
                            u.y);
            }

            // ── Vertex ────────────────────────────────────────────────────
            Varyings GrassVert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // ── Store local Y before any transform (height blend) ──
                OUT.bladeY = IN.positionOS.y;

                // ─────────────────────────────────────────────────────────
                // WIND SYSTEM 1: Idle Grass Sway (Gradient Noise)
                // Only affects X and Z axes, driven by time + UV
                // The uv.y = 0 at root, 1 at tip — blade tip sways more
                // ─────────────────────────────────────────────────────────
                float  time         = _Time.y;
                float2 swayOffset   = float2(time * _WindSpeed, 0.0);
                float2 swayUV       = IN.uv + swayOffset;
                float  swayNoise    = GradientNoise(swayUV, 5.0);

                // Scale sway by blade tip (IN.uv.y) so root stays planted
                float  tipFactor    = IN.uv.y;
                float3 swayMove     = float3(swayNoise, 0.0, swayNoise)
                                      * _WindIntensity
                                      * tipFactor;

                // ─────────────────────────────────────────────────────────
                // WIND SYSTEM 2: Wind Wave Texture (world-space, LOD)
                // Animates world XZ through wind texture
                // ─────────────────────────────────────────────────────────
                float3 posWS_pre    = TransformObjectToWorld(IN.positionOS.xyz);

                float2 windTexUV    = float2(posWS_pre.x / _WindNoiseScale.x,
                                             posWS_pre.z / _WindNoiseScale.y);
                float  windTime     = (time / 20.0) * _WindNoiseSpeed;
                windTexUV          += float2(windTime, 0.0);

                // Sample wind texture at LOD 0 (required for vertex shader)
                float  windSample   = SAMPLE_TEXTURE2D_LOD(
                                        _WindTex, sampler_WindTex,
                                        windTexUV, 0).r;

                // Smooth step contrast control
                float  windWave     = smoothstep(
                                        _WindNoiseContrast.x,
                                        _WindNoiseContrast.y,
                                        windSample);

                // Negate + height control: affects upper blade portion
                float  windEffect   = -windWave * _WindHeight * tipFactor;

                float3 waveMove     = float3(windEffect, 0.0, windEffect * 0.4);

                // ─────────────────────────────────────────────────────────
                // Combine both wind systems with original position
                // ─────────────────────────────────────────────────────────
                float3 animatedPos  = IN.positionOS.xyz + swayMove + waveMove;

                float4 posOS_anim   = float4(animatedPos, 1.0);
                OUT.positionHCS     = TransformObjectToHClip(animatedPos);
                OUT.positionWS      = TransformObjectToWorld(animatedPos);
                OUT.uv              = TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            // ── Fragment ──────────────────────────────────────────────────
            half4 GrassFrag(Varyings IN) : SV_Target
            {
                // ── Alpha clipping from grass texture ──────────────────
                half4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(texSample.a - _AlphaClip);

                // ─────────────────────────────────────────────────────────
                // SYSTEM 1: DEPTH COLOR
                // Lerp between NearColor and FarColor based on camera distance
                // ─────────────────────────────────────────────────────────
                float  camDist      = length(_WorldSpaceCameraPos - IN.positionWS);
                float  depthT       = smoothstep(_NearFarRange.x, _NearFarRange.y, camDist);
                depthT              = 1.0 - depthT;  // flip: near = 1, far = 0
                half3  depthColor   = lerp(_FarColor.rgb, _NearColor.rgb, depthT);

                // ─────────────────────────────────────────────────────────
                // SYSTEM 2: HEIGHT BLEND
                // Lerp between BottomColor and TopColor (depthColor) along blade Y
                // ─────────────────────────────────────────────────────────
                float  objectScaleY = length(float3(
                                        unity_ObjectToWorld[0].y,
                                        unity_ObjectToWorld[1].y,
                                        unity_ObjectToWorld[2].y));
                float  heightGrad   = smoothstep(0.0, _HeightBlend, IN.bladeY / max(objectScaleY, 0.001));
                half3  heightColor  = lerp(_BottomColor.rgb, depthColor, heightGrad);

                // ─────────────────────────────────────────────────────────
                // SYSTEM 3: TERRAIN COLOR BLEND
                // Sample the orthographic render texture using world XZ
                // ─────────────────────────────────────────────────────────
                float2 terrainUV    = float2(IN.positionWS.x, IN.positionWS.z)
                                      / _TerrainSize + _TerrainOffset;
                terrainUV          += 0.5; // center offset (terrain at world 0,0)

                half3  terrainSample = SAMPLE_TEXTURE2D(
                                        _TerrainColor, sampler_TerrainColor,
                                        terrainUV).rgb;

                // Top color = terrain dominant (25% topColor, 75% terrain)
                half3  topColor     = lerp(heightColor, terrainSample, 1.0 - _TerrainPower);

                // Toggle between terrain blend and bottom-color-only mode
                half3  blendedColor = lerp(heightColor, topColor, _UseTerrainColor);

                // ─────────────────────────────────────────────────────────
                // SYSTEM 4: SHADOW ATTENUATION
                // Use shadow coord to get main light shadow
                // Lerp between (ShadowColor × TerrainColor) and TerrainColor
                // ─────────────────────────────────────────────────────────
                float4 shadowCoord  = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight    = GetMainLight(shadowCoord);
                float  shadowAtten  = mainLight.shadowAttenuation;

                half3  shadowMix    = _ShadowColor.rgb * terrainSample;
                half3  litColor     = lerp(shadowMix, blendedColor, shadowAtten);

                // ─────────────────────────────────────────────────────────
                // SYSTEM 5: WIND COLOR (wind wave affects grass color)
                // Same wind texture sample — adds bright wind color to tips
                // ─────────────────────────────────────────────────────────
                float2 windColorUV  = float2(IN.positionWS.x / _WindNoiseScale.x,
                                             IN.positionWS.z / _WindNoiseScale.y);
                float  windColorT   = (_Time.y / 20.0) * _WindNoiseSpeed;
                windColorUV        += float2(windColorT, 0.0);

                float  windColorSample = SAMPLE_TEXTURE2D(
                                            _WindTex, sampler_WindTex,
                                            windColorUV).r;

                float  windColorWave = smoothstep(
                                        _WindNoiseContrast.x,
                                        _WindNoiseContrast.y,
                                        windColorSample);

                // Saturate and multiply by height blend so only tips get colored
                float  windColorMask = saturate(windColorWave) * heightGrad;

                // Add wind color (brightens the blade where wind passes)
                half3  finalColor   = litColor + (windColorMask * 0.15);

                // Multiply by texture tint
                finalColor         *= texSample.rgb;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        // ── Shadow Caster Pass (DISABLED — matches tutorial: cast shadows OFF) ──
        // Per the tutorial, grass should NOT cast shadows on itself
        // If you want grass to cast shadows on terrain, uncomment this pass:
        /*
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        */
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    //CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}
