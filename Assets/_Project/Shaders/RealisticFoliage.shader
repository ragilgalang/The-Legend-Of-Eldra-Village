Shader "Eldara/RealisticFoliage"
{
    Properties
    {
        _BaseColor ("Color Tint", Color) = (1, 1, 1, 1)
        _BaseMap ("Leaf Texture (Optional)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.3
        _Translucency ("Translucency Subsurface", Range(0.0, 1.0)) = 0.4
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry" 
        }
        LOD 300
        Cull Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
                float _Translucency;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // GPU Wind Micro-Flutter: helai daun bergetar lembut ditiup angin tanpa terlepas dari dahan
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float windPhase = _Time.y * 3.6 + worldPos.x * 2.2 + worldPos.z * 1.8 + input.positionOS.y * 2.5;
                float flutterX = sin(windPhase) * 0.022;
                float flutterY = cos(windPhase * 1.25) * 0.015;
                float flutterZ = sin(windPhase * 0.85 + 1.2) * 0.022;

                // Daun di posisi lebih tinggi atau menjauh dari sumbu batang bergetar lebih nyata
                float heightFactor = saturate(input.positionOS.y / 6.0);
                input.positionOS.xyz += float3(flutterX, flutterY, flutterZ) * (0.35 + 0.65 * heightFactor);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // Gabungkan warna vertex (blade + vein) dengan tekstur & color tint
                float4 albedo = input.color * _BaseColor * texColor;

                float3 normalWS = normalize(input.normalWS);
                
                // Shadow koordinat
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 lightDir = normalize(mainLight.direction);
                
                // Two-sided lighting (pencahayaan tembus pandang dedaunan / translucency)
                float nDotL = saturate(dot(normalWS, lightDir));
                float backDotL = saturate(dot(-normalWS, lightDir)) * _Translucency;
                float directTerm = max(nDotL, backDotL) * mainLight.shadowAttenuation;

                float3 directLight = mainLight.color * directTerm;
                
                // Ambient lighting / sky hemisphere
                float3 ambient = SampleSH(normalWS) * 0.85 + float3(0.08, 0.12, 0.06);

                float3 finalRgb = albedo.rgb * (directLight + ambient);

                return float4(finalRgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float windPhase = _Time.y * 3.6 + worldPos.x * 2.2 + worldPos.z * 1.8 + input.positionOS.y * 2.5;
                float flutterX = sin(windPhase) * 0.022;
                float flutterY = cos(windPhase * 1.25) * 0.015;
                float flutterZ = sin(windPhase * 0.85 + 1.2) * 0.022;
                float heightFactor = saturate(input.positionOS.y / 6.0);
                input.positionOS.xyz += float3(flutterX, flutterY, flutterZ) * (0.35 + 0.65 * heightFactor);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            float4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}

