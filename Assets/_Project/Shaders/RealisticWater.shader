Shader "Eldara/RealisticWater"
{
    Properties
    {
        _DeepColor ("Deep Water Color", Color) = (0.03, 0.18, 0.32, 0.90)
        _ShallowColor ("Shallow / Crystal Color", Color) = (0.15, 0.60, 0.75, 0.65)
        _FresnelColor ("Fresnel Sky Color", Color) = (0.80, 0.92, 1.0, 1.0)
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.96
        _WaveSpeed ("Wave Speed", Float) = 1.4
        _WaveScale ("Wave Scale", Float) = 7.0
        _WaveStrength ("Wave Ripple Normal Strength", Range(0.0, 1.0)) = 0.45
        _RippleFrequency ("Concentric Ripple Frequency", Float) = 24.0
        _RippleSpeed ("Concentric Ripple Speed", Float) = 2.8
        _RippleStrength ("Concentric Ripple Strength", Range(0.0, 1.0)) = 0.40
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent+50" 
        }
        LOD 300
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 positionOS   : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FresnelColor;
                float _Smoothness;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveStrength;
                float _RippleFrequency;
                float _RippleSpeed;
                float _RippleStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float t = _Time.y * _WaveSpeed;

                // 1. Gelombang Konsentris melingkar dari tengah sumur (tali / ember mencelup air)
                float2 localXZ = input.positionOS.xz;
                float distCenter = length(localXZ);
                float ripplePhase = distCenter * _RippleFrequency - _Time.y * _RippleSpeed;
                float rippleFalloff = exp(-distCenter * 1.5);
                float rippleSlope = sin(ripplePhase) * rippleFalloff * _RippleStrength;
                
                float2 rippleNormXZ = float2(0.0, 0.0);
                if (distCenter > 0.001)
                {
                    rippleNormXZ = (localXZ / distCenter) * rippleSlope * 0.18;
                }

                // 2. Gelombang Permukaan Alami Multi-Arah (Multi-directional organic ripples)
                float2 uv1 = input.positionOS.xz * _WaveScale + float2(t * 0.7, t * 0.5);
                float2 uv2 = input.positionOS.xz * (_WaveScale * 1.4) + float2(-t * 0.6, t * 0.8);
                float2 uv3 = input.positionOS.xz * (_WaveScale * 2.1) + float2(t * 0.4, -t * 0.5);

                float waveX = (cos(uv1.x) * sin(uv1.y) * 0.5 +
                               cos(uv2.x * 1.3) * sin(uv2.y * 1.3) * 0.35 +
                               cos(uv3.x * 1.8) * sin(uv3.y * 1.8) * 0.15) * _WaveStrength;

                float waveZ = (sin(uv1.x) * cos(uv1.y) * 0.5 +
                               sin(uv2.x * 1.3) * cos(uv2.y * 1.3) * 0.35 +
                               sin(uv3.x * 1.8) * cos(uv3.y * 1.8) * 0.15) * _WaveStrength;

                // Normal Perturbasi Gabungan
                float3 normalWS = normalize(float3(waveX + rippleNormXZ.x, 1.0, waveZ + rippleNormXZ.y));

                // Arah pandang kamera
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                float NdotV = saturate(dot(normalWS, viewDirWS));

                // 3. Efek Fresnel (Pantulan Langit & Permukaan Air)
                float fresnel = pow(1.0 - NdotV, 3.5);

                // 4. Gradien Warna Kedalaman Air (Deep Emerald Abyss to Crystal Clear Turquoise)
                float depthFactor = saturate(distCenter / 0.85);
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, (1.0 - NdotV * 0.4) * 0.7 + depthFactor * 0.3);

                // 5. Pencahayaan Utama (Matahari / Bulan)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = normalize(mainLight.direction);

                float NdotL = saturate(dot(normalWS, lightDir));
                float3 diffuseLight = mainLight.color * (NdotL * mainLight.shadowAttenuation);

                // Pantulan Spekular Matahari / Bulan
                float3 halfDir = normalize(lightDir + viewDirWS);
                float specPower = lerp(40.0, 256.0, _Smoothness);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, specPower) * _Smoothness * 2.0 * mainLight.shadowAttenuation;
                float3 specLight = mainLight.color * specular;

                // 6. Pantulan Spekular Cahaya Lentera Di Atas Sumur (Point Lights / Additional Lights)
                float3 additionalSpec = float3(0.0, 0.0, 0.0);
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float3 addLightDir = normalize(addLight.direction);
                    float3 addHalf = normalize(addLightDir + viewDirWS);
                    float addNdotH = saturate(dot(normalWS, addHalf));
                    float addSpec = pow(addNdotH, 96.0) * _Smoothness * 2.8;
                    additionalSpec += addLight.color * (addSpec * addLight.distanceAttenuation * addLight.shadowAttenuation);
                }
                #endif

                // 7. Pantulan Lingkungan / Langit (Hemisphere Ambient)
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                float3 envReflection = SampleSH(reflectDir) * 1.3 + _FresnelColor.rgb * 0.15;

                // 8. Efek Kilau Caustics Air Bawah Permukaan
                float caustic = pow(saturate(sin(input.positionOS.x * 16.0 + t * 2.2) * cos(input.positionOS.z * 16.0 - t * 1.8)), 3.0) * 0.18;

                // Komposisi Warna Akhir
                float3 ambientLight = SampleSH(normalWS) * 0.6 + float3(0.04, 0.08, 0.12);
                float3 finalColor = waterColor * (diffuseLight + ambientLight + caustic);
                finalColor += envReflection * fresnel * _FresnelColor.rgb;
                finalColor += specLight + additionalSpec;

                // Transparansi Air Jernih yang Realistis
                float finalAlpha = saturate(_DeepColor.a * (0.65 + 0.35 * (1.0 - NdotV)) + fresnel * 0.4);

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

