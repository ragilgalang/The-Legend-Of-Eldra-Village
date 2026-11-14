#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Perbaikan desain desa Eldara (partial dari SceneSetupTool agar bisa memakai helper private-nya).
/// Isi: hutan alami berumpun, titik pohon alun-alun yang tidak menabrak Balai Desa,
/// sumur desa, lampu di jalan ekspansi, dan kabut yang tidak "menelan" pandangan jauh.
/// </summary>
public static partial class SceneSetupTool
{
    // ─────────────────────────────────────────────────────────────────────
    //  KONSTANTA TATA LETAK (sesuaikan bila ukuran Balai Desa berubah)
    // ─────────────────────────────────────────────────────────────────────

    // Dari screenshot: atap Balai Desa ±12.5 m, jalan lingkar alun-alun di ±20 m.
    // Pohon lama di ±11 m menimpa sudut atap Balai. Posisi baru ada di celah antara Balai dan jalan lingkar.
    const float PlazaTreeOffset = 15.5f;

    static readonly Vector3[] PlazaTreeSpots =
    {
        new Vector3(-PlazaTreeOffset, 0f,  PlazaTreeOffset),
        new Vector3( PlazaTreeOffset, 0f,  PlazaTreeOffset),
        new Vector3(-PlazaTreeOffset, 0f, -PlazaTreeOffset),
        new Vector3( PlazaTreeOffset, 0f, -PlazaTreeOffset),
    };

    // Ruas jalan ekspansi yang tidak lurus di sumbu (agar hutan tidak tumbuh di atas jalan)
    static readonly Vector2[][] BendRoadSegments =
    {
        new[] { new Vector2( 4.2f, 34f), new Vector2(14f,  44f) },
        new[] { new Vector2(14f,  44f), new Vector2(14f,  88f) },
        new[] { new Vector2(14f,  88f), new Vector2( 0f, 105f) },
    };

    // ─────────────────────────────────────────────────────────────────────
    //  PASS DESAIN AKHIR  (panggil setelah SetupZoneTriggers())
    // ─────────────────────────────────────────────────────────────────────
    static void ApplyVillageDesignPass()
    {
        var old = GameObject.Find("--- VILLAGE DETAILS ---");
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject("--- VILLAGE DETAILS ---");

        // 1. Kabut: Exp2 menjaga jarak dekat tetap jernih dan memudar halus di kejauhan.
        //    (Linear 75–380 m membuat Scene view dari atas tampak abu-abu pucat.)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0042f;

        // 2. Sumur desa di ruang kosong sekitar Balai Desa
        PlaceVillageWell(root.transform);

        // 3. Lampu di jalan ekspansi di luar benteng
        PlaceRoadLamps(root.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HUTAN ALAMI: berumpun, ada padang kosong, spesies mengelompok
    // ─────────────────────────────────────────────────────────────────────
    static void BuildNaturalForest(
        Transform root,
        Material bark, Material birchBark,
        Material summerLeaf, Material springLeaf, Material autumnLeaf,
        Material slavicMat, string[] bushModels, string[] rockModels)
    {
        const float cell = 7.5f;     // jarak minimum antar-kandidat (jittered grid = tanpa tumpukan pohon)
        const float inner = 84f;     // di luar pagar benteng (pagar di 80m)
        const float outer = 160f;    // batas zona ekspansi

        float seedA = Random.Range(0f, 1000f); // kerapatan
        float seedB = Random.Range(0f, 1000f); // spesies
        int treeCount = 0;

        for (float gx = -outer; gx < outer; gx += cell)
        for (float gz = -outer; gz < outer; gz += cell)
        {
            float x = gx + Random.Range(0.15f, 0.85f) * cell;
            float z = gz + Random.Range(0.15f, 0.85f) * cell;

            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (edge < inner) continue;
            if (IsReservedSpot(x, z)) continue;

            float roadDist = DistanceToRoads(x, z);
            if (roadDist < 6f) continue;

            // Gumpalan hutan besar + detail halus
            float big  = Mathf.PerlinNoise(seedA + x * 0.018f, seedA + z * 0.018f);
            float fine = Mathf.PerlinNoise(seedA + x * 0.09f,  seedA + z * 0.09f);
            float density = big * 0.75f + fine * 0.25f;

            // Dekat benteng dan dekat jalan lebih jarang (transisi lembut, bukan tembok pohon)
            float edgeFade = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(inner, inner + 18f, edge));
            float roadFade = Mathf.Lerp(0.20f, 1f, Mathf.InverseLerp(6f, 20f, roadDist));
            float chance = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 0.62f, density)) * edgeFade * roadFade;

            var pos = new Vector3(x, 0f, z);

            if (Random.value <= chance)
            {
                float scale = Mathf.Lerp(0.95f, 1.5f, density) * Random.Range(0.9f, 1.15f);
                float species = Mathf.PerlinNoise(seedB + x * 0.011f, seedB + z * 0.011f); // spesies mengelompok

                if (species < 0.34f)       RealisticTreeBuilder.BuildPineTree(pos, scale, root, bark, summerLeaf);
                else if (species < 0.62f)  RealisticTreeBuilder.BuildOakTree(pos, scale, root, bark, summerLeaf);
                else if (species < 0.78f)  RealisticTreeBuilder.BuildBirchTree(pos, scale, root, birchBark, springLeaf);
                else                       RealisticTreeBuilder.BuildAutumnOakTree(pos, scale, root, bark, autumnLeaf);
                treeCount++;

                if (Random.value < 0.35f && bushModels != null && bushModels.Length > 0)
                {
                    string b = bushModels[Random.Range(0, bushModels.Length)];
                    var bp = pos + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));
                    var bush = InstantiateArtModel(b, bp, Quaternion.Euler(0, Random.Range(0, 360f), 0),
                        Vector3.one * Random.Range(0.8f, 1.3f), root, slavicMat);
                    if (bush)
                    {
                        var sw = bush.AddComponent<TreeWindSway>();
                        sw.swayIntensity = 1.1f;
                        sw.swaySpeed = 1.7f;
                    }
                }

                if (density > 0.55f && Random.value < 0.10f) PlaceForestRock(root, pos, rockModels, slavicMat);
            }
            else if (density < 0.38f && Random.value < 0.05f)
            {
                // Padang terbuka: sesekali ada batu besar agar tidak kosong melompong
                PlaceForestRock(root, pos, rockModels, slavicMat);
            }
        }

        Debug.Log($"[Eldara] Hutan alami: {treeCount} pohon.");
    }

    static void PlaceForestRock(Transform root, Vector3 pos, string[] rockModels, Material slavicMat)
    {
        if (rockModels == null || rockModels.Length == 0) return;
        string r = rockModels[Random.Range(0, rockModels.Length)];
        var rp = pos + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
        InstantiateArtModel(r, rp, Quaternion.Euler(0, Random.Range(0, 360f), 0),
            Vector3.one * Random.Range(1.2f, 2.5f), root, slavicMat);
    }

    // Area yang harus bebas pohon (sungai, tambang, dst.)
    static bool IsReservedSpot(float x, float z)
    {
        if (Mathf.Abs(x + 148f) < 6f && Mathf.Abs(z) < 64f) return true;               // Sungai Barat
        var p = new Vector2(x, z);
        if (Vector2.Distance(p, new Vector2( 100f, 9f)) < 13f) return true;            // Tambang bijih besi
        if (Vector2.Distance(p, new Vector2(-100f, 9f)) < 13f) return true;            // Area balok kayu
        return false;
    }

    static float DistanceToRoads(float x, float z)
    {
        var p = new Vector2(x, z);
        float best = Mathf.Min(Mathf.Abs(x), Mathf.Abs(z)); // 4 jalan sumbu utama
        foreach (var s in BendRoadSegments)
            best = Mathf.Min(best, DistanceToSegment(p, s[0], s[1]));
        return best;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SUMUR & LAMPU
    // ─────────────────────────────────────────────────────────────────────
    static void PlaceVillageWell(Transform parent)
    {
        // Jangan buat sumur ganda bila sumur utama di pusat alun-alun sudah ada
        if (GameObject.Find("Village_Stone_Well") != null) return;

        for (float r = 14f; r <= 18f; r += 2f)
        {
            for (int a = 0; a < 8; a++)
            {
                float ang = (a * 45f + 22.5f) * Mathf.Deg2Rad; // hindari sumbu jalan & diagonal pohon
                var pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                if (!IsAreaFree(pos, 2.2f)) continue;
                BuildVillageWell(parent, pos);
                return;
            }
        }
        Debug.LogWarning("[Eldara] Tidak ada ruang kosong untuk sumur desa — dilewati.");
    }

    static void PlaceRoadLamps(Transform parent)
    {
        var axes = new (Vector3 dir, Vector3 side)[]
        {
            (Vector3.forward, Vector3.right),
            (Vector3.back,    Vector3.right),
            (Vector3.right,   Vector3.forward),
            (Vector3.left,    Vector3.forward),
        };

        foreach (var (dir, side) in axes)
        {
            int i = 0;
            for (float d = 100f; d <= 156f; d += 18f, i++)
            {
                float sign = (i % 2 == 0) ? 1f : -1f; // selang-seling kiri/kanan
                var pos = dir * d + side * sign * 3.6f;
                if (!IsAreaFree(pos, 0.6f)) continue;

                Vector3 toRoad = -side * sign;
                float yaw = Mathf.Atan2(toRoad.x, toRoad.z) * Mathf.Rad2Deg; // lengan lampu menghadap jalan
                AddTraditionalVillageLamp(parent, pos, yaw);

                // Hemat performa: bayangan realtime per lampu tidak perlu
                var lamp = parent.GetChild(parent.childCount - 1);
                var l = lamp.GetComponentInChildren<Light>();
                if (l) l.shadows = LightShadows.None;
            }
        }
    }

    static bool IsAreaFree(Vector3 center, float radius)
    {
        Physics.SyncTransforms();
        var hits = Physics.OverlapBox(center + Vector3.up * 2f, new Vector3(radius, 1.5f, radius),
            Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
            if (h.name != "Ground") return false;
        return true;
    }
}
#endif

