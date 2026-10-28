Shader "SPELLSTRIKE/Stage2/CoastalWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.10, 0.76, 0.78, 1)
        _DeepColor ("Deep Color", Color) = (0.015, 0.22, 0.42, 1)
        _FoamTint ("Highlight Tint", Color) = (0.70, 0.98, 1.00, 1)
        _ShallowAlpha ("Shallow Alpha", Range(0, 1)) = 0.48
        _DeepAlpha ("Deep Alpha", Range(0, 1)) = 0.78
        _DepthDistance ("Depth Blend Distance", Range(0.5, 40)) = 10
        _DepthCurve ("Depth Contrast Curve", Range(0.35, 2)) = 0.72
        _WaveScale ("Wave Scale", Range(0.005, 0.2)) = 0.045
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.22
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.55
        _SpecularStrength ("Specular Strength", Range(0, 3)) = 1.05
        _SpecularPower ("Specular Power", Range(8, 256)) = 72
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.2
        _RefractionStrength ("Refraction Strength", Range(0, 0.05)) = 0.008
        [Toggle(_STAGE2_DEPTH)] _UseDepth ("Use Scene Depth", Float) = 1
        [Toggle(_STAGE2_REFRACTION)] _UseRefraction ("Use Refraction", Float) = 1
    }

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
            Name "Stage2CoastalWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma shader_feature_local_fragment _STAGE2_DEPTH
            #pragma shader_feature_local_fragment _STAGE2_REFRACTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamTint;
                float _ShallowAlpha;
                float _DeepAlpha;
                float _DepthDistance;
                float _DepthCurve;
                float _WaveScale;
                float _WaveStrength;
                float _WaveSpeed;
                float _SpecularStrength;
                float _SpecularPower;
                float _FresnelPower;
                float _RefractionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.screenPos = ComputeScreenPos(pos.positionCS);
                return output;
            }

            float3 GetWaterNormal(float3 positionWS, out float waveSignal)
            {
                float2 p = positionWS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;

                float2 d1 = normalize(float2(0.86, 0.51));
                float2 d2 = normalize(float2(-0.42, 0.91));
                float2 d3 = normalize(float2(0.18, -0.98));

                float ph1 = dot(p, d1) * 6.28318 + t;
                float ph2 = dot(p, d2) * 8.79645 - t * 1.31;
                float ph3 = dot(p, d3) * 12.56637 + t * 0.73;

                float w1 = sin(ph1);
                float w2 = sin(ph2);
                float w3 = sin(ph3);
                waveSignal = (w1 + w2 * 0.55 + w3 * 0.25) / 1.8;

                float2 grad = cos(ph1) * d1 * 1.0;
                grad += cos(ph2) * d2 * 0.70;
                grad += cos(ph3) * d3 * 0.35;
                grad *= _WaveStrength;

                return normalize(float3(-grad.x, 1.0, -grad.y));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float waveSignal;
                float3 normalWS = GetWaterNormal(input.positionWS, waveSignal);
                float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirWS)), _FresnelPower);

                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                float depth01;

                #if defined(_STAGE2_DEPTH)
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float surfaceEyeDepth = max(0.0, -TransformWorldToView(input.positionWS).z);
                    float waterThickness = max(0.0, sceneEyeDepth - surfaceEyeDepth);
                    float normalizedDepth = saturate(waterThickness / max(_DepthDistance, 0.001));
                    // Stage 2 spans roughly 0-16 world units below the waterline.
                    // A sub-linear curve preserves clear shallow water while making
                    // the 2-12 unit range visibly transition toward deep blue.
                    depth01 = pow(normalizedDepth, max(_DepthCurve, 0.001));
                #else
                    depth01 = saturate(0.32 + waveSignal * 0.10 + fresnel * 0.30);
                #endif

                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);

                #if defined(_STAGE2_REFRACTION)
                    float2 refractUV = screenUV + normalWS.xz * (_RefractionStrength * (1.0 - depth01 * 0.65));
                    half3 sceneColor = SampleSceneColor(refractUV);
                    waterColor = lerp(sceneColor, waterColor, 0.42 + depth01 * 0.38);
                #endif

                Light mainLight = GetMainLight();
                float3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDir)), _SpecularPower) * _SpecularStrength;
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                waterColor *= (0.72 + ndotl * 0.20);
                waterColor += ambient * 0.08;
                waterColor += mainLight.color * spec;
                waterColor = lerp(waterColor, _FoamTint.rgb, fresnel * 0.22);

                float alpha = lerp(_ShallowAlpha, _DeepAlpha, depth01);
                alpha = saturate(alpha + fresnel * 0.12);
                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
}
