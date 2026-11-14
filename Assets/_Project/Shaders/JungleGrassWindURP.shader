// Shader rumput bergoyang untuk Universal Render Pipeline (URP).
// Membaca parameter angin global dari JungleWind.cs (_JungleWind, _JungleWindParams).
// Vertex color: R = bobot lentur (akar 0 -> ujung 1), G = fase acak, B = kekakuan acak.
Shader "Jungle/GrassWindURP"
{
    Properties
    {
        _BaseColor ("Warna Akar", Color) = (0.05, 0.18, 0.05, 1)
        _TipColor  ("Warna Ujung", Color) = (0.33, 0.60, 0.17, 1)
        _WindBend  ("Lentur Angin (meter, angin penuh)", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float  _WindBend;
            CBUFFER_END

            float4 _JungleWind;        // xy = arah, z = kekuatan, w = kecepatan gelombang
            float4 _JungleWindParams;  // x = frekuensi, y = hembusan, z = waktu, w = kecepatan hembusan

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 color      : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            // Rumus sama persis dengan JungleWind.Evaluate() di C#.
            float3 WindOffset(float3 posWS, float bendW, float phase, float stiff)
            {
                float2 dir  = normalize(_JungleWind.xy + 1e-5);
                float2 perp = float2(-dir.y, dir.x);
                float strength = _JungleWind.z;
                float speed    = _JungleWind.w;
                float freq     = _JungleWindParams.x;
                float gust     = _JungleWindParams.y;
                float t        = _JungleWindParams.z;
                float gustSpd  = _JungleWindParams.w;

                float along = dot(posWS.xz, dir);
                float wave  = sin(along * freq - t * speed)
                            + 0.5 * sin(dot(posWS.xz, perp) * freq * 1.7 - t * speed * 1.6 + phase * 6.2831);
                float gustF = 1.0 + gust * sin(along * 0.05 - t * gustSpd);
                float push  = strength * gustF * (1.0 + 0.35 * wave);

                float k = bendW * bendW * _WindBend * lerp(1.3, 0.7, stiff);   // ujung melentur paling jauh
                float3 off = float3(dir.x, 0.0, dir.y) * push * k;
                off.y = -abs(push) * k * 0.35;                                 // bilah merunduk, panjang terjaga
                return off;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS);
                posWS += WindOffset(posWS, IN.color.r, IN.color.g, IN.color.b);
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color      = IN.color;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, IN.color.r);
                albedo *= 0.8 + 0.4 * IN.color.g;                    // variasi warna per bilah

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 n = normalize(IN.normalWS);
                float ndl = saturate(dot(n, mainLight.direction) * 0.5 + 0.5);   // half-lambert, lembut
                float3 col = albedo * (mainLight.color * ndl * mainLight.shadowAttenuation + SampleSH(n));
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
