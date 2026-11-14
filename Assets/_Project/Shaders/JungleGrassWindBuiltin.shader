// Shader rumput bergoyang untuk Built-in Render Pipeline (surface shader).
// Membaca parameter angin global dari JungleWind.cs (_JungleWind, _JungleWindParams).
// Vertex color: R = bobot lentur (akar 0 -> ujung 1), G = fase acak, B = kekakuan acak.
Shader "Jungle/GrassWindBuiltin"
{
    Properties
    {
        _BaseColor ("Warna Akar", Color) = (0.05, 0.18, 0.05, 1)
        _TipColor  ("Warna Ujung", Color) = (0.33, 0.60, 0.17, 1)
        _WindBend  ("Lentur Angin (meter, angin penuh)", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow fullforwardshadows
        #pragma target 3.0

        fixed4 _BaseColor;
        fixed4 _TipColor;
        float  _WindBend;

        float4 _JungleWind;        // xy = arah, z = kekuatan, w = kecepatan gelombang
        float4 _JungleWindParams;  // x = frekuensi, y = hembusan, z = waktu, w = kecepatan hembusan

        struct Input
        {
            float4 color : COLOR;
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

            float k = bendW * bendW * _WindBend * lerp(1.3, 0.7, stiff);
            float3 off = float3(dir.x, 0.0, dir.y) * push * k;
            off.y = -abs(push) * k * 0.35;
            return off;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 off = WindOffset(posWS, v.color.r, v.color.g, v.color.b);
            v.vertex.xyz += mul((float3x3)unity_WorldToObject, off);
            v.normal = float3(0, 1, 0);   // normal ke atas supaya pencahayaan rumput lembut dari dua sisi
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = lerp(_BaseColor.rgb, _TipColor.rgb, IN.color.r) * (0.8 + 0.4 * IN.color.g);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
