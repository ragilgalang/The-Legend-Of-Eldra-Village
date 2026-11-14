#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

/// <summary>
/// PORTAL + JUNGLE BIOME untuk Legenda Desa Eldara.
///
///  - Portal Desa  : gerbang batu berlumut di barat laut alun-alun (-9, 0, 21), menghadap ke jalan utara.
///  - Jungle Biome : pusat (350, 0, 350), luas ±250 m — hutan rimba lebat, kuil bertingkat, kolam,
///                   tanaman bercahaya, sulur, satwa, NPC, resource, dan portal kembali ke desa.
///
/// Cara pakai:
///  A) Otomatis: tambahkan  SetupJungleBiome(slavicMat, grassTuftMat);  di SetupScene() setelah SetupMegaExpansion().
///  B) Tanpa membangun ulang desa: menu  Eldara > Tambah Portal Jungle Biome.
/// </summary>
public static partial class SceneSetupTool
{
    // ─────────────────────────────────────────────────────────────────────
    //  KONFIGURASI
    // ─────────────────────────────────────────────────────────────────────
    static readonly Vector3 JungleCenter      = new Vector3(350f, 0f, 350f);
    static readonly Vector3 VillagePortalPos  = new Vector3(-9f, 0f, 21f);
    const float VillagePortalYaw = 90f;        // menghadap timur (ke North Avenue)

    // Bukaan portal: lebar 2.8 m, tinggi lurus 3.0 m + lengkung di atas (jauh di atas tinggi player 2 m)
    const float PortalOpeningHalfWidth = 1.4f;
    const float PortalOpeningHeight    = 3.0f;
    const float PortalFloorY           = 0.2f; // permukaan lantai portal (tangga 0.1 + 0.1)

    const string JungleArtFolder = "Assets/_Project/Art/JungleBiome";

    [MenuItem("Eldara/Tambah Portal Jungle Biome")]
    public static void AddJunglePortalOnly()
    {
        if (Application.isPlaying)
        {
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Stop!", "Hentikan Play Mode dulu.", "OK");
            return;
        }

        Random.InitState(20261115);
        var slavicMat = AssetMaterialSetup.SetupSlavicMaterial();
        var grassMat  = AssetMaterialSetup.SetupGrassMaterial();
        SetupJungleBiome(slavicMat, grassMat);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Portal Jungle Dibangun!",
                "Portal ada di barat laut alun-alun desa (dekat jalan utara).\n" +
                "Jalan masuk ke gerbang dari arah jalan, lalu Anda tiba di Jungle Biome.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────────────────────────────────
    static void SetupJungleBiome(Material slavicMat, Material grassMat)
    {
        // Bersihkan hasil build sebelumnya
        foreach (string nm in new[] { "--- JUNGLE BIOME ---", "--- JUNGLE PORTAL (VILLAGE) ---" })
        {
            var old = GameObject.Find(nm);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(nm); }
        }

        var villageRoot = new GameObject("--- JUNGLE PORTAL (VILLAGE) ---");
        var jungleRoot  = new GameObject("--- JUNGLE BIOME ---");

        // Material
        var barkMat      = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var birchBarkMat = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: true);
        var summerLeaf   = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var springLeaf   = AssetMaterialSetup.SetupProceduralFoliageMaterial("SPRING");

        var stoneMat  = JungleMat("M_Jungle_MossyStone", new Color(0.36f, 0.42f, 0.34f), 0.08f);
        var darkStone = JungleMat("M_Jungle_DarkStone",  new Color(0.20f, 0.24f, 0.21f), 0.10f);
        var runeMat   = JungleMat("M_Jungle_Rune",       new Color(0.35f, 1.00f, 0.60f), 0.30f, new Color(0.15f, 1f, 0.5f) * 3f);
        var vineMat   = JungleMat("M_Jungle_Vine",       new Color(0.12f, 0.38f, 0.10f), 0.15f);
        var groundMat = JungleMat("M_Jungle_Ground",     new Color(0.13f, 0.30f, 0.10f), 0.05f);
        var glowMat   = JungleMat("M_Jungle_GlowPlant",  new Color(0.50f, 1.00f, 0.90f), 0.40f, new Color(0.1f, 0.9f, 0.8f) * 3f);
        var woodMat   = JungleMat("M_Jungle_Wood",       new Color(0.30f, 0.20f, 0.12f), 0.15f);

        var tex     = GetOrCreatePortalTexture();
        var energy1 = GetOrCreatePortalEnergyMat("M_Portal_Energy_1", tex, new Color(0.75f, 1f, 0.85f, 0.85f), new Vector2(1f, 1f));
        var energy2 = GetOrCreatePortalEnergyMat("M_Portal_Energy_2", tex, new Color(0.30f, 1f, 0.60f, 0.60f), new Vector2(1.7f, 1.3f));
        var glowCol = new Color(0.3f, 1f, 0.6f);

        // ── Titik kedatangan ──
        Quaternion vRot = Quaternion.Euler(0f, VillagePortalYaw, 0f);
        Vector3 vFwd = vRot * Vector3.forward;
        var villageArrival = new GameObject("Arrival_Village").transform;
        villageArrival.SetParent(villageRoot.transform);
        villageArrival.position = VillagePortalPos + vFwd * 5.5f + Vector3.up * 1.1f;
        villageArrival.rotation = vRot;

        Vector3 jPortalPos = JungleCenter + new Vector3(0f, 0f, -80f);
        var jungleArrival = new GameObject("Arrival_Jungle").transform;
        jungleArrival.SetParent(jungleRoot.transform);
        jungleArrival.position = jPortalPos + Vector3.forward * 5.5f + Vector3.up * 1.1f;
        jungleArrival.rotation = Quaternion.identity;

        // ── Portal di desa ──
        var vGate = BuildJunglePortalGate(villageRoot.transform, "Portal_To_Jungle", VillagePortalPos, VillagePortalYaw,
            stoneMat, darkStone, runeMat, vineMat, energy1, energy2, glowCol, "Gerbang Rimba Belantara");
        vGate.destination = jungleArrival;

        Vector3 npcOff = vRot * new Vector3(3.8f, 0f, 3.2f);
        SpawnNPC(villageRoot.transform, "NPC_PenjagaPortal", VillagePortalPos + npcOff,
            Quaternion.LookRotation(new Vector3(-npcOff.x, 0f, -npcOff.z)), Color.white,
            "Gerbang kuno ini terbuka ke Rimba Belantara di timur jauh. Udara di sana lembap dan penuh bahaya, " +
            "tapi tanaman langka dan reruntuhan kuil menanti. Melangkahlah ke cahaya hijau itu jika kamu berani!");

        // ── Jungle biome ──
        BuildJungleBiome(jungleRoot.transform, jPortalPos, jungleArrival, slavicMat, grassMat,
            barkMat, birchBarkMat, summerLeaf, springLeaf,
            stoneMat, darkStone, runeMat, vineMat, groundMat, glowMat, woodMat, energy1, energy2, glowCol, out var jGate);
        jGate.destination = villageArrival;

        // Daftarkan zona Jungle ke ChunkRenderDistance agar render optimal & langsung muncul saat teleport
        var crd = ChunkRenderDistance.Instance ?? FindFirstObjectByType<ChunkRenderDistance>();
        if (crd != null)
        {
            crd.RegisterZone("Hutan Rimba Belantara", jungleRoot, JungleCenter, 220f);
            crd.UpdateAll(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PORTAL GATE (gerbang batu + energi)
    // ─────────────────────────────────────────────────────────────────────
    static PortalGate BuildJunglePortalGate(Transform parent, string name, Vector3 pos, float yaw,
        Material stone, Material dark, Material rune, Material vine,
        Material energy1, Material energy2, Color glowColor, string label)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var t = root.transform;

        float a  = PortalOpeningHalfWidth;
        float h0 = PortalOpeningHeight;
        float fy = PortalFloorY;
        float R  = a + 0.45f; // radius tengah busur batu (tebal 0.9 m)

        // Alas bertingkat (tiap anak tangga <= 0.1 m, mudah dilalui)
        JungleBox(t, "Platform_Tier1", new Vector3(0f, 0.05f, 0f), new Vector3(6.6f, 0.10f, 4.6f), stone);
        JungleBox(t, "Platform_Tier2", new Vector3(0f, 0.10f, 0f), new Vector3(5.2f, 0.20f, 3.2f), dark);

        // Dua pilar + alas pilar
        foreach (float sx in new[] { -1f, 1f })
        {
            JungleBox(t, "Pillar",     new Vector3(sx * R, fy + h0 * 0.5f, 0f), new Vector3(0.9f, h0, 1.1f), stone);
            JungleBox(t, "PillarBase", new Vector3(sx * R, fy + 0.25f, 0f),     new Vector3(1.2f, 0.5f, 1.4f), dark);
        }

        // Busur setengah lingkaran dari 14 balok batu
        const int N = 14;
        float segLen = Mathf.PI * R / N * 1.12f;
        for (int i = 0; i < N; i++)
        {
            float ang = Mathf.PI * (i + 0.5f) / N;
            Vector3 p = new Vector3(Mathf.Cos(ang) * R, fy + h0 + Mathf.Sin(ang) * R, 0f);
            JungleBox(t, "ArchStone", p, new Vector3(0.9f, segLen, 1.1f), (i % 2 == 0) ? stone : dark,
                new Vector3(0f, 0f, ang * Mathf.Rad2Deg));

            // Rune bercahaya di muka depan & belakang busur
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 rp = new Vector3(Mathf.Cos(ang) * R, fy + h0 + Mathf.Sin(ang) * R, sz * 0.56f);
                JungleBox(t, "ArchRune", rp, new Vector3(0.14f, segLen * 0.45f, 0.04f), rune,
                    new Vector3(0f, 0f, ang * Mathf.Rad2Deg), false);
            }
        }
        JungleBox(t, "Keystone", new Vector3(0f, fy + h0 + R, 0f), new Vector3(1.2f, 1.1f, 1.3f), dark);

        // Rune pada pilar
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                for (int k = 0; k < 4; k++)
                    JungleBox(t, "PillarRune", new Vector3(sx * R, fy + 0.9f + k * 0.65f, sz * 0.56f),
                        new Vector3(0.12f, 0.34f, 0.04f), rune, default, false);

        // Sulur menjuntai di pilar (hanya visual)
        for (int v = 0; v < 6; v++)
        {
            float sx = (v % 2 == 0) ? -1f : 1f;
            float len = Random.Range(1.6f, 3.2f);
            JungleCyl(t, "Vine", new Vector3(sx * (R + Random.Range(-0.3f, 0.3f)), fy + h0 + 0.2f - len * 0.5f, 0.6f),
                0.04f, len, vine);
        }

        // Selaput energi: 2 lapisan, tekstur digulirkan oleh PortalGate
        var mesh = CreatePortalOpeningMesh(a, h0);
        var l1 = CreateMeshObject("Energy_Layer1", t, mesh, energy1, Color.green);
        l1.transform.localPosition = new Vector3(0f, fy, 0.02f);
        var l2 = CreateMeshObject("Energy_Layer2", t, mesh, energy2, Color.green);
        l2.transform.localPosition = new Vector3(0f, fy, -0.02f);
        foreach (var go in new[] { l1, l2 })
        {
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // Bola cahaya berputar di dalam bukaan
        var pivot = new GameObject("OrbitPivot");
        pivot.transform.SetParent(t, false);
        pivot.transform.localPosition = new Vector3(0f, fy + h0 * 0.6f, 0f);
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            JungleSphere(pivot.transform, "Orb", new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * 1.05f, 0.11f, rune);
        }

        // Cahaya portal
        var lightGo = new GameObject("PortalGlow");
        lightGo.transform.SetParent(t, false);
        lightGo.transform.localPosition = new Vector3(0f, fy + h0 * 0.7f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.intensity = 2.5f;
        light.range = 16f;
        light.shadows = LightShadows.None;

        // Papan nama (terbaca dari depan & belakang)
        float labelY = fy + h0 + R + 1.5f;
        for (int side = 0; side < 2; side++)
        {
            var lg = new GameObject("Label");
            lg.transform.SetParent(t, false);
            lg.transform.localPosition = new Vector3(0f, labelY, side == 0 ? 0.7f : -0.7f);
            lg.transform.localRotation = Quaternion.Euler(0f, side == 0 ? 180f : 0f, 0f);
            var tmp = lg.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 6f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.7f, 1f, 0.8f);
            tmp.rectTransform.sizeDelta = new Vector2(14f, 2f);
        }

        // Komponen portal + trigger (menutupi seluruh bukaan)
        var gate = root.AddComponent<PortalGate>();
        var box = root.GetComponent<BoxCollider>();
        box.isTrigger = true;
        float trigH = h0 + 0.6f;
        box.center = new Vector3(0f, fy + trigH * 0.5f, 0f);
        box.size = new Vector3(a * 2f - 0.3f, trigH, 0.6f);

        gate.portalLabel = label;
        gate.energyRenderers = new Renderer[] { l1.GetComponent<Renderer>(), l2.GetComponent<Renderer>() };
        gate.scrollSpeeds = new Vector2[] { new Vector2(0.03f, 0.22f), new Vector2(-0.05f, -0.16f) };
        gate.glowLight = light;
        gate.orbitPivot = pivot.transform;
        return gate;
    }

    /// <summary>Mesh bukaan portal: persegi panjang + setengah lingkaran di atas, double-sided.</summary>
    static Mesh CreatePortalOpeningMesh(float halfW, float straightH, int arcSegs = 24)
    {
        var perim = new List<Vector2>();
        perim.Add(new Vector2(halfW, 0f));
        perim.Add(new Vector2(halfW, straightH));
        for (int i = 1; i < arcSegs; i++)
        {
            float ang = Mathf.PI * i / arcSegs;
            perim.Add(new Vector2(Mathf.Cos(ang) * halfW, straightH + Mathf.Sin(ang) * halfW));
        }
        perim.Add(new Vector2(-halfW, straightH));
        perim.Add(new Vector2(-halfW, 0f));

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        void AddFace(float z, Vector3 normal, bool flip)
        {
            int start = verts.Count;
            Vector2 c = new Vector2(0f, straightH * 0.5f);
            verts.Add(new Vector3(c.x, c.y, z)); norms.Add(normal); uvs.Add(c * 0.5f);
            foreach (var p in perim) { verts.Add(new Vector3(p.x, p.y, z)); norms.Add(normal); uvs.Add(p * 0.5f); }
            int n = perim.Count;
            for (int i = 0; i < n; i++)
            {
                int p0 = start + 1 + i;
                int p1 = start + 1 + (i + 1) % n;
                if (!flip) { tris.Add(start); tris.Add(p1); tris.Add(p0); }
                else       { tris.Add(start); tris.Add(p0); tris.Add(p1); }
            }
        }
        AddFace(0f, Vector3.back, false);
        AddFace(0f, Vector3.forward, true);

        var mesh = new Mesh { name = "PortalOpening" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  JUNGLE BIOME
    // ─────────────────────────────────────────────────────────────────────
    static void BuildJungleBiome(Transform root, Vector3 jPortalPos, Transform jungleArrival,
        Material slavicMat, Material grassMat,
        Material barkMat, Material birchBarkMat, Material summerLeaf, Material springLeaf,
        Material stoneMat, Material darkStone, Material runeMat, Material vineMat,
        Material groundMat, Material glowMat, Material woodMat,
        Material energy1, Material energy2, Color glowCol, out PortalGate returnGate)
    {
        Vector3 C = JungleCenter;
        Vector3 templeCenter = C + new Vector3(0f, 0f, 60f);
        Vector2 pondXZ  = new Vector2(C.x - 38f, C.z - 5f);
        Vector2 temple2 = new Vector2(templeCenter.x, templeCenter.z);
        Vector2 portal2 = new Vector2(jPortalPos.x, jPortalPos.z);

        // Tanah gelap rimba tropis (persis seperti di JungleBiomeGenerator)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Jungle_Ground";
        ground.transform.SetParent(root);
        ground.transform.position = C + Vector3.up * 0.02f;
        ground.transform.localScale = new Vector3(26f, 1f, 26f);
        ground.GetComponent<Renderer>().sharedMaterial = JungleTree.MakeMaterial(new Color(0.16f, 0.12f, 0.07f), 0.05f);
        Object.DestroyImmediate(ground.GetComponent<Collider>());

        // Portal kembali ke desa
        returnGate = BuildJunglePortalGate(root, "Portal_Return_To_Village", jPortalPos, 0f,
            stoneMat, darkStone, runeMat, vineMat, energy1, energy2, glowCol, "Kembali ke Desa Eldara");

        // Jalan batu kuno: portal -> kuil
        var roadMat = GetOrCreateVillageRoadMaterial();
        CreateContinuousPavedRoad(root, "Jungle_Ancient_Path",
            new Vector3(C.x, 0f, jPortalPos.z + 4.5f), new Vector3(C.x, 0f, templeCenter.z - 14f - 2.2f),
            3.4f, roadMat, 2.5f);

        // Kuil bertingkat
        BuildJungleTemple(root, templeCenter, stoneMat, darkStone, runeMat, vineMat, slavicMat);

        // Kolam rimba
        var waterMesh = CreateTessellatedWaterMesh("Jungle_Pond_Mesh", 12f, 0.04f, 5, 40);
        var pond = CreateMeshObject("Jungle_Pond", root, waterMesh, GetOrCreateRealisticWaterMaterial(), new Color(0.1f, 0.4f, 0.4f));
        pond.transform.position = new Vector3(pondXZ.x, 0f, pondXZ.y);
        for (int i = 0; i < 10; i++)
        {
            float ang = i / 10f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float r = 12.5f + Random.Range(0f, 1.5f);
            Vector3 rp = new Vector3(pondXZ.x + Mathf.Cos(ang) * r, 0f, pondXZ.y + Mathf.Sin(ang) * r);
            InstantiateArtModel("EA03_Environment_Rock_Head_01a", rp, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                Vector3.one * Random.Range(1.3f, 2.2f), root, slavicMat);
        }
        for (int i = 0; i < 9; i++) // daun teratai
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(2f, 10f);
            var pad = JungleCyl(root, "LilyPad", new Vector3(pondXZ.x + Mathf.Cos(ang) * r, 0.06f, pondXZ.y + Mathf.Sin(ang) * r),
                0.5f, 0.02f, vineMat);
            pad.transform.SetParent(root);
        }

        // ── POHON RIMBA PROSEDURAL (JungleTree & JungleBush — persis seperti di JungleBiomeGenerator) ──
        if (!root.GetComponent<JungleWind>())
            root.gameObject.AddComponent<JungleWind>();

        // 1) Pohon Raksasa Purba (Pusat Rimba) dengan akar banir besar
        Vector3 giantPos = C + new Vector3(-42f, 0f, 55f);
        SpawnProceduralJungleTree(root, "AncientGiantTree", giantPos, 48f, 2.6f, 40, new Color(0.10f, 0.44f, 0.12f), 12345);

        var placed = new List<Vector2>();
        int treeSeed = 777;

        // 2) Gugusan Pohon Raksasa (Giant Trees) — tinggi 38-52m, batang besar berakar banir
        int giantCount = 0;
        int attempts = 0;
        while (giantCount < 20 && attempts < 2500)
        {
            attempts++;
            float x = C.x + Random.Range(-110f, 110f);
            float z = C.z + Random.Range(-110f, 110f);
            var p2 = new Vector2(x, z);

            if (Mathf.Abs(x - C.x) < 7f && z > jPortalPos.z - 8f && z < templeCenter.z - 8f) continue; // sterilkan jalan setapak
            if (Vector2.Distance(p2, pondXZ) < 16f) continue;
            if (Vector2.Distance(p2, temple2) < 28f) continue;
            if (Vector2.Distance(p2, portal2) < 14f) continue;
            if (Vector2.Distance(p2, new Vector2(giantPos.x, giantPos.z)) < 16f) continue;

            bool tooClose = false;
            foreach (var q in placed) if ((q - p2).sqrMagnitude < 144f) { tooClose = true; break; }
            if (tooClose) continue;

            float h = Random.Range(38f, 52f);
            float r = Random.Range(1.8f, 2.6f);
            Color leafCol = Color.Lerp(new Color(0.08f, 0.38f, 0.10f), new Color(0.20f, 0.52f, 0.14f), Random.value);
            SpawnProceduralJungleTree(root, $"GiantTree_{giantCount}", new Vector3(x, 0f, z), h, r, Random.Range(30, 42), leafCol, treeSeed + giantCount * 37);
            placed.Add(p2);
            giantCount++;
        }

        // 3) Pohon Menengah (Mid Trees) — tinggi 20-32m, tajuk hijau awan bertingkat
        int midCount = 0;
        attempts = 0;
        while (midCount < 50 && attempts < 3500)
        {
            attempts++;
            float x = C.x + Random.Range(-115f, 115f);
            float z = C.z + Random.Range(-115f, 115f);
            var p2 = new Vector2(x, z);

            if (Mathf.Abs(x - C.x) < 5.5f && z > jPortalPos.z - 6f && z < templeCenter.z - 10f) continue;
            if (Vector2.Distance(p2, pondXZ) < 14f) continue;
            if (Vector2.Distance(p2, temple2) < 24f) continue;
            if (Vector2.Distance(p2, portal2) < 12f) continue;

            bool tooClose = false;
            foreach (var q in placed) if ((q - p2).sqrMagnitude < 49f) { tooClose = true; break; }
            if (tooClose) continue;

            float h = Random.Range(20f, 32f);
            float r = Random.Range(0.85f, 1.35f);
            Color leafCol = Color.Lerp(new Color(0.12f, 0.44f, 0.14f), new Color(0.24f, 0.58f, 0.18f), Random.value);
            SpawnProceduralJungleTree(root, $"MidTree_{midCount}", new Vector3(x, 0f, z), h, r, Random.Range(18, 26), leafCol, treeSeed + 1000 + midCount * 23);
            placed.Add(p2);
            midCount++;
        }

        // 4) Semak Rimba Prosedural Lumpy (JungleBush dari JungleTree.LumpySphere) — 75 semak
        for (int i = 0; i < 75; i++)
        {
            Vector3 bp = JungleRandomOffPath(C, jPortalPos, templeCenter, 115f);
            SpawnProceduralJungleBush(root, $"JungleBush_{i}", bp, treeSeed + 2000 + i * 17);
        }

        // Semak, batu, rumput, bunga
        string[] bushModels = { "EA03_Nature_Bush_03b", "EA03_Nature_Bush_01c", "EA03_Nature_Bush_01b" };
        string[] rockModels = { "EA03_Environment_Rock_Head_01a", "EA03_Environment_Rock_Head_01b",
                                "EA03_Environment_Rock_CatHead_01a", "EA03_Environment_Rock_CatHead_01b" };
        for (int i = 0; i < 90; i++)
        {
            Vector3 p = JungleRandomOffPath(C, jPortalPos, templeCenter, 118f);
            InstantiateArtModel(bushModels[Random.Range(0, bushModels.Length)], p,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.3f, 2.2f), root, slavicMat);
        }
        for (int i = 0; i < 30; i++)
        {
            Vector3 p = JungleRandomOffPath(C, jPortalPos, templeCenter, 118f);
            InstantiateArtModel(rockModels[Random.Range(0, rockModels.Length)], p,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.5f, 3f), root, slavicMat);
        }
        if (grassMat != null)
        {
            for (int i = 0; i < 80; i++)
            {
                Vector3 p = JungleRandomOffPath(C, jPortalPos, templeCenter, 120f);
                RealisticGrassBuilder.BuildGrassClump(p, 12, 5f, root, grassMat);
                if (i % 2 == 0) RealisticFlowerBuilder.BuildFlowerClump(p, 5, 4f, root);
            }
        }

        // Tanaman bercahaya (aksen malam hari)
        for (int i = 0; i < 14; i++)
        {
            Vector3 p = JungleRandomOffPath(C, jPortalPos, templeCenter, 100f);
            var plant = new GameObject("GlowPlant");
            plant.transform.SetParent(root);
            plant.transform.position = p;
            JungleCyl(plant.transform, "Stem", new Vector3(0f, 0.45f, 0f), 0.05f, 0.9f, vineMat);
            JungleSphere(plant.transform, "Bulb", new Vector3(0f, 1.0f, 0f), 0.22f, glowMat);
            var lg = new GameObject("GlowLight");
            lg.transform.SetParent(plant.transform, false);
            lg.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.3f, 1f, 0.9f);
            l.intensity = 1.4f;
            l.range = 7f;
            l.shadows = LightShadows.None;
        }

        // Cahaya atmosfer rimba
        var amb = new GameObject("Jungle_AmbientLight");
        amb.transform.SetParent(root);
        amb.transform.position = C + new Vector3(0f, 40f, 0f);
        var al = amb.AddComponent<Light>();
        al.type = LightType.Point;
        al.color = new Color(0.5f, 1f, 0.6f);
        al.intensity = 1.2f;
        al.range = 220f;

        // Resource
        PlaceArtResource(root, "JungleHerb_1", "EA03_Nature_Bush_03b", "jungle_herb", "Tekan E untuk memetik Herbal Rimba",
            new Vector3(pondXZ.x + 6f, 0f, pondXZ.y + 14f), Quaternion.identity, Vector3.one * 0.9f, slavicMat, new Color(0.4f, 0.9f, 0.3f));
        PlaceArtResource(root, "JungleHerb_2", "EA03_Nature_Bush_03b", "jungle_herb", "Tekan E untuk memetik Herbal Rimba",
            new Vector3(C.x + 14f, 0f, C.z - 20f), Quaternion.identity, Vector3.one * 0.9f, slavicMat, new Color(0.4f, 0.9f, 0.3f));
        PlaceArtResource(root, "JungleHerb_3", "EA03_Nature_Bush_03b", "jungle_herb", "Tekan E untuk memetik Herbal Rimba",
            new Vector3(C.x - 16f, 0f, C.z + 30f), Quaternion.identity, Vector3.one * 0.9f, slavicMat, new Color(0.4f, 0.9f, 0.3f));
        PlaceArtResource(root, "AncientRelic", "EA03_Environment_Rock_CatHead_01a", "ancient_relic", "Tekan E untuk mengambil Relik Kuno",
            templeCenter + new Vector3(0f, 6.1f, 0f), Quaternion.identity, Vector3.one * 0.5f, slavicMat, new Color(0.3f, 1f, 0.6f));

        // NPC penjelajah di dekat portal
        SpawnNPC(root, "NPC_PenjelajahRimba", jungleArrival.position + new Vector3(4.5f, -1.1f, 2f),
            Quaternion.Euler(0f, -120f, 0f), Color.white,
            "Syukurlah ada orang lain! Kuil di utara dijaga makhluk rimba yang kuat. " +
            "Ikuti jalan batu kuno itu, tapi hati-hati dengan serigala belantara. Portal kembali ada di belakangmu.");

        // Satwa & musuh elite
        AnimalAssetSetup.SpawnWolf(C + new Vector3(-30f, 0f, 20f),  Quaternion.Euler(0f, 60f, 0f),   root, "Serigala Belantara 1");
        AnimalAssetSetup.SpawnWolf(C + new Vector3( 32f, 0f, 10f),  Quaternion.Euler(0f, -100f, 0f), root, "Serigala Belantara 2");
        AnimalAssetSetup.SpawnWolf(C + new Vector3( 10f, 0f, 90f),  Quaternion.Euler(0f, 180f, 0f),  root, "Serigala Belantara 3");
        SpawnEnemyMarker(root, "Elite_JungleGuardian_1", templeCenter + new Vector3(-12f, 0f, -22f));
        SpawnEnemyMarker(root, "Elite_JungleGuardian_2", templeCenter + new Vector3( 12f, 0f, -22f));
        SpawnEnemyMarker(root, "Elite_JungleGuardian_3", templeCenter + new Vector3(  0f, 0f, -32f));

        BuildJungleBoundary(root, C, 125f);
    }

    static Vector3 JungleRandomOffPath(Vector3 C, Vector3 jPortalPos, Vector3 templeCenter, float range)
    {
        for (int tries = 0; tries < 20; tries++)
        {
            float x = C.x + Random.Range(-range, range);
            float z = C.z + Random.Range(-range, range);
            if (Mathf.Abs(x - C.x) < 4f && z > jPortalPos.z - 6f && z < templeCenter.z - 10f) continue;
            if (Vector2.Distance(new Vector2(x, z), new Vector2(jPortalPos.x, jPortalPos.z)) < 10f) continue;
            if (Vector2.Distance(new Vector2(x, z), new Vector2(templeCenter.x, templeCenter.z)) < 22f) continue;
            return new Vector3(x, 0f, z);
        }
        return new Vector3(C.x + range * 0.5f, 0f, C.z + range * 0.5f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  KUIL RIMBA BERTINGKAT (3 tingkat + tangga di tiap tingkat)
    // ─────────────────────────────────────────────────────────────────────
    static void BuildJungleTemple(Transform parent, Vector3 c, Material stone, Material dark, Material rune,
        Material vine, Material slavicMat)
    {
        var root = new GameObject("Jungle_Temple");
        root.transform.SetParent(parent);
        root.transform.position = c;
        var t = root.transform;

        float[] half = { 14f, 9.5f, 5f };
        const float tierH = 1.5f;
        const float run = 0.30f;
        int steps = HouseDimensions.StairSteps(tierH);
        float rise = tierH / steps;

        for (int i = 0; i < 3; i++)
            JungleBox(t, $"Tier_{i}", new Vector3(0f, tierH * (i + 1) * 0.5f, 0f),
                new Vector3(half[i] * 2f, tierH * (i + 1), half[i] * 2f), (i % 2 == 0) ? stone : dark);

        // Tangga di sisi selatan tiap tingkat (lebar 4.5 m)
        for (int i = 0; i < 3; i++)
        {
            float y0 = tierH * i;
            float startZ = -half[i] - steps * run;
            for (int k = 0; k < steps; k++)
            {
                float hgt = rise * (k + 1);
                JungleBox(t, $"Stair_{i}_{k}", new Vector3(0f, y0 + hgt * 0.5f, startZ + run * (k + 0.5f)),
                    new Vector3(4.5f, hgt, run), stone);
            }
        }

        // Kuil puncak: 4 pilar tinggi 4.5 m + atap
        float topY = tierH * 3f;
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                JungleCyl(t, "ShrinePillar", new Vector3(sx * 3.2f, topY + 2.25f, sz * 3.2f), 0.45f, 4.5f, stone, true);
                JungleBox(t, "PillarRune", new Vector3(sx * 3.2f, topY + 2.4f, sz * 3.2f - Mathf.Sign(sz) * 0.46f),
                    new Vector3(0.12f, 1.4f, 0.04f), rune, default, false);
            }
        JungleBox(t, "ShrineRoof", new Vector3(0f, topY + 4.75f, 0f), new Vector3(8.4f, 0.5f, 8.4f), dark);
        JungleBox(t, "Altar", new Vector3(0f, topY + 0.5f, 0f), new Vector3(2.2f, 1.0f, 2.2f), stone);
        JungleBox(t, "AltarRune", new Vector3(0f, topY + 1.02f, 0f), new Vector3(1.6f, 0.04f, 1.6f), rune, default, false);

        // Patung penjaga di 4 sudut tingkat bawah
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                JungleBox(t, "GuardianStatue", new Vector3(sx * 12.5f, tierH + 1.5f, sz * 12.5f), new Vector3(1.2f, 3f, 1.2f), dark);
                JungleBox(t, "GuardianEyes", new Vector3(sx * 12.5f, tierH + 2.4f, sz * 12.5f - 0.62f * Mathf.Sign(sz)),
                    new Vector3(0.7f, 0.12f, 0.04f), rune, default, false);
            }

        // Sulur di tepi atap
        for (int i = 0; i < 12; i++)
        {
            float len = Random.Range(2f, 4f);
            float ang = i / 12f * Mathf.PI * 2f;
            JungleCyl(t, "TempleVine", new Vector3(Mathf.Cos(ang) * 4.1f, topY + 4.5f - len * 0.5f, Mathf.Sin(ang) * 4.1f), 0.04f, len, vine);
        }

        // Cahaya kuil
        var lg = new GameObject("TempleLight");
        lg.transform.SetParent(t, false);
        lg.transform.localPosition = new Vector3(0f, topY + 3f, 0f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.3f, 1f, 0.6f);
        l.intensity = 2.5f;
        l.range = 28f;
        l.shadows = LightShadows.None;
    }

    // Dinding batas tak terlihat di sekeliling jungle
    static void BuildJungleBoundary(Transform parent, Vector3 c, float half)
    {
        var rootGo = new GameObject("Jungle_Boundary");
        rootGo.transform.SetParent(parent);
        const float th = 4f, h = 40f;
        var defs = new (string n, Vector3 pos, Vector3 size)[]
        {
            ("N", c + new Vector3(0f, h * 0.5f,  half + th * 0.5f), new Vector3(half * 2f + th * 2f, h, th)),
            ("S", c + new Vector3(0f, h * 0.5f, -half - th * 0.5f), new Vector3(half * 2f + th * 2f, h, th)),
            ("E", c + new Vector3( half + th * 0.5f, h * 0.5f, 0f), new Vector3(th, h, half * 2f)),
            ("W", c + new Vector3(-half - th * 0.5f, h * 0.5f, 0f), new Vector3(th, h, half * 2f)),
        };
        foreach (var d in defs)
        {
            var go = new GameObject("Barrier_" + d.n);
            go.transform.SetParent(rootGo.transform);
            go.transform.position = d.pos;
            go.AddComponent<BoxCollider>().size = d.size;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPER: MATERIAL, TEKSTUR, PRIMITIF
    // ─────────────────────────────────────────────────────────────────────
    static void JungleEnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) JungleEnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static Material JungleMat(string name, Color color, float smoothness = 0.1f, Color? emission = null, float metallic = 0f)
    {
        JungleEnsureFolder(JungleArtFolder + "/Materials");
        string path = $"{JungleArtFolder}/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D GetOrCreatePortalTexture()
    {
        JungleEnsureFolder(JungleArtFolder);
        string path = JungleArtFolder + "/T_PortalEnergy.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float s1 = Mathf.Sin(Mathf.PI * 2f * (2f * u + 3f * v));
                float s2 = Mathf.Sin(Mathf.PI * 2f * (3f * u - 2f * v + 0.3f));
                float s3 = Mathf.Sin(Mathf.PI * 2f * (5f * v + u));
                float f = Mathf.Clamp01(0.5f + 0.30f * s1 + 0.25f * s2 + 0.15f * s3);
                Color col = Color.Lerp(new Color(0.02f, 0.30f, 0.18f), new Color(0.55f, 1f, 0.65f), f);
                col.a = Mathf.Lerp(0.45f, 0.95f, f);
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp)
        {
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material GetOrCreatePortalEnergyMat(string name, Texture2D tex, Color tint, Vector2 tiling)
    {
        JungleEnsureFolder(JungleArtFolder + "/Materials");
        string path = $"{JungleArtFolder}/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); mat.SetTextureScale("_BaseMap", tiling); }
        mat.SetColor("_BaseColor", tint);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_Cull"))    mat.SetFloat("_Cull", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetShaderPassEnabled("ShadowCaster", false);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject JungleBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat,
        Vector3 euler = default, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject JungleCyl(Transform parent, string name, Vector3 localPos, float radius, float height,
        Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject JungleSphere(Transform parent, string name, Vector3 localPos, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SPAWNER POHON & SEMAK RIMBA (Model JungleTree & JungleBush)
    // ─────────────────────────────────────────────────────────────────────
    static void SpawnProceduralJungleTree(Transform parent, string name, Vector3 pos, float height, float baseRadius, int leaves, Color leafColor, int seed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(Random.Range(-1.5f, 1.5f), Random.value * 360f, Random.Range(-1.5f, 1.5f));

        var t = go.AddComponent<JungleTree>();
        t.height = height;
        t.baseRadius = baseRadius;
        t.topRadius = baseRadius * 0.32f;
        t.buttressCount = Random.Range(4, 8);
        t.buttressFlare = Random.Range(1.1f, 1.7f);
        t.buttressHeight = height * 0.12f;
        t.canopyRadius = 10f * (height / 38f);
        t.leafClusters = leaves;
        t.canopyStart = 0.58f;
        t.clusterSize = new Vector2(4.5f, 8f) * (height / 38f);
        t.leafTint = leafColor;
        t.Build(seed);

        if (go.GetComponent<JungleTreeSway>() == null)
            go.AddComponent<JungleTreeSway>();
    }

    static void SpawnProceduralJungleBush(Transform parent, string name, Vector3 pos, int seed)
    {
        Random.State old = Random.state;
        Random.InitState(seed);

        var parts = new List<CombineInstance>();
        Mesh lumpy = JungleTree.LumpySphere();
        if (lumpy == null) { Random.state = old; return; }

        int n = Random.Range(3, 6);
        for (int i = 0; i < n; i++)
        {
            Vector3 p = new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(0.3f, 0.9f), Random.Range(-1.1f, 1.1f));
            float s = Random.Range(1.0f, 1.9f);
            parts.Add(new CombineInstance
            {
                mesh = lumpy,
                transform = Matrix4x4.TRS(p, Quaternion.Euler(0, Random.value * 360f, 0), new Vector3(s, s * 0.6f, s))
            });
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.AddComponent<MeshFilter>().sharedMesh = JungleTree.Combine(parts);
        var mr = go.AddComponent<MeshRenderer>();
        Color c = Color.Lerp(new Color(0.10f, 0.40f, 0.12f), new Color(0.25f, 0.55f, 0.18f), Random.value);
        mr.sharedMaterial = JungleTree.MakeMaterial(c, 0.1f);

        Random.state = old;
    }
}
#endif
