#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Perbaikan desain peta desa Eldara (partial SceneSetupTool).
///
/// MASALAH YANG DIPERBAIKI
///  1. Pagar benteng menutup jalan: pagar barat/timur (x=±80) memotong jalan Barat/Timur di z=0, dan pagar
///     utara (z=52) memotong jalan utara di x=14. Sekarang ada gerbang berpilar + menara pengapit.
///  2. Jalan putus: ada celah tanpa jalan di x=±54..±66 (Barat/Timur) dan altar di utara tidak punya jalan.
///  3. Lampu jalan hanya ada di alun-alun & luar benteng; jalan utama di dalam desa gelap.
///  4. Rumah berdiri langsung di rumput; sekarang ada pelataran tanah di bawah tiap rumah.
///  5. Pita kosong selebar ±30 m antara rumah dan pagar: diisi distrik tani, kincir angin, kolam,
///     kandang ternak, lapangan latihan, dan rumpun pohon (desa "di tengah hutan").
///
/// CARA PASANG: tambahkan satu baris di akhir ApplyVillageDesignPass() (file Beautify):
///     ApplyMapDesignPass(root.transform);
/// </summary>
public static partial class SceneSetupTool
{
    static readonly Dictionary<string, Material> _mapMats = new Dictionary<string, Material>();

    // ─────────────────────────────────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────────────────────────────────
    static void ApplyMapDesignPass(Transform root)
    {
        _mapMats.Clear();
        var map = new GameObject("Map_Design_Pass");
        map.transform.SetParent(root, false);
        var t = map.transform;

        SafeMapStep("Gerbang & pagar", () => FixPerimeterGates(t));
        SafeMapStep("Jalan penghubung", () => LinkMissingRoads(t));
        SafeMapStep("Lampu jalan dalam desa", () => PlaceInnerAvenueLamps(t));
        SafeMapStep("Pelataran rumah", () => BuildHouseYardPads(t));
        SafeMapStep("Distrik pertanian", () => BuildFarmDistrict(t));
        SafeMapStep("Kincir angin", () => BuildWindmill(t, new Vector3(-58f, 0f, 10f)));
        SafeMapStep("Kolam desa", () => BuildVillagePond(t, new Vector3(-58f, 0f, -23f)));
        SafeMapStep("Kandang ternak", () => BuildLivestockPen(t, new Vector3(58f, 0f, 20f)));
        SafeMapStep("Lapangan latihan", () => BuildTrainingYard(t, new Vector3(62f, 0f, -26f)));
        SafeMapStep("Rumpun pohon desa", () => BuildVillageGroves(t));

        Debug.Log("[Eldara] Pass desain peta selesai.");
    }

    static void SafeMapStep(string label, System.Action step)
    {
        try { step(); }
        catch (System.Exception e) { Debug.LogWarning($"[Eldara] Langkah desain peta '{label}' dilewati: {e.Message}"); }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPER: material bersama & primitif
    // ─────────────────────────────────────────────────────────────────────
    static Material MapMat(string key, Color color, float smoothness = 0.08f, Color? emission = null)
    {
        if (_mapMats.TryGetValue(key, out var m) && m != null) return m;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader) { name = "M_Map_" + key, color = color };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission.Value);
        }
        _mapMats[key] = m;
        return m;
    }

    static GameObject MapPrimLocal(PrimitiveType type, string name, Transform parent, Vector3 localPos,
        Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        if (mat != null)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;
        return go;
    }

    // Overload penempatan posisi dunia (backward-compatible)
    static GameObject MapPrim(PrimitiveType type, string name, Transform parent, Vector3 pos,
        Vector3 scale, Material mat)
    {
        var go = MapPrimLocal(type, name, parent, Vector3.zero, scale, mat);
        go.transform.position = pos;
        return go;
    }

    static GameObject MapGroup(Transform parent, string name, Vector3 worldPos, float yaw = 0f)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.position = worldPos;
        if (Mathf.Abs(yaw) > 0.01f) g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return g;
    }

    static void PlaceMapLamp(Transform parent, Vector3 pos, Vector3 facing)
    {
        if (!IsAreaFree(pos, 0.8f)) return;
        float yRot = (facing.sqrMagnitude > 0.001f) ? Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg : 0f;
        AddTraditionalVillageLamp(parent, pos, yRot);
    }

    static GameObject CreateContinuousPavedRoad(Transform parent, Vector3 start, Vector3 end, float width, Material roadMat, float uvScale = 2.5f)
    {
        return CreateContinuousPavedRoad(parent, "Map_Road_Segment", start, end, width, roadMat, uvScale);
    }

    static Mesh CreateHollowRingMesh(string name, float r1, float r2, float height)
    {
        float outerR = Mathf.Max(r1, r2);
        float innerR = Mathf.Min(r1, r2);
        return CreateHollowRingMesh(name, outerR, innerR, topY: height, bottomY: 0f, segments: 32, addInnerWall: true);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. GERBANG BENTENG (membuka pagar di perpotongan jalan)
    // ─────────────────────────────────────────────────────────────────────
    static void FixPerimeterGates(Transform parent)
    {
        var pRoot = GameObject.Find("--- PERIMETER DEFENSES ---");
        if (pRoot == null)
        {
            Debug.LogWarning("[Eldara] '--- PERIMETER DEFENSES ---' tidak ditemukan, gerbang benteng dilewati.");
            return;
        }

        // Hapus modul pagar yang tepat menutup jalur jalan utama:
        //  - Barat: x ~ -80, |z| < 4
        //  - Timur: x ~ +80, |z| < 4
        //  - Utara: z ~ +52, |x - 14| < 4 (jalan ke Altar)
        var toRemove = new List<GameObject>();
        for (int i = 0; i < pRoot.transform.childCount; i++)
        {
            var child = pRoot.transform.GetChild(i).gameObject;
            var pos = child.transform.position;

            if (Mathf.Abs(pos.x + 80f) < 2.5f && Mathf.Abs(pos.z) < 4f) { toRemove.Add(child); continue; }
            if (Mathf.Abs(pos.x - 80f) < 2.5f && Mathf.Abs(pos.z) < 4f) { toRemove.Add(child); continue; }
            if (Mathf.Abs(pos.z - 52f) < 2.5f && Mathf.Abs(pos.x - 14f) < 4f) { toRemove.Add(child); continue; }
        }
        foreach (var go in toRemove) Object.DestroyImmediate(go);
        Debug.Log($"[Eldara] Membuka {toRemove.Count} modul pagar benteng untuk gerbang.");

        // Bangun gerbang berpilar + menara pengapit
        BuildGate(parent, "Gate_West", new Vector3(-80f, 0f, 0f), roadAlongX: true, inward: Vector3.right, pRoot.transform);
        BuildGate(parent, "Gate_East", new Vector3( 80f, 0f, 0f), roadAlongX: true, inward: Vector3.left,  pRoot.transform);
        BuildGate(parent, "Gate_North", new Vector3( 14f, 0f, 52f), roadAlongX: false, inward: Vector3.back, pRoot.transform);
    }

    static void BuildGate(Transform parent, string name, Vector3 center, bool roadAlongX, Vector3 inward,
        Transform fenceParent)
    {
        var gate = MapGroup(parent, name, center);
        var lateral = roadAlongX ? Vector3.forward : Vector3.right;
        float openingWidth = 7.0f; // lebar jalan 5.5 + clearance

        var stone = MapMat("GateStone", new Color(0.42f, 0.40f, 0.36f), 0.05f);
        var roofMat = MapMat("GateRoof", new Color(0.24f, 0.15f, 0.10f));

        // a) Dua pilar utama gerbang (kiri & kanan bukaan)
        foreach (int sign in new[] { -1, 1 })
        {
            var pPos = lateral * (sign * openingWidth * 0.5f);
            MapPrimLocal(PrimitiveType.Cube, "GatePillar", gate.transform, pPos + new Vector3(0, 2.5f, 0),
                new Vector3(1.2f, 5.0f, 1.2f), stone);
            MapPrimLocal(PrimitiveType.Cube, "PillarCap", gate.transform, pPos + new Vector3(0, 5.2f, 0),
                new Vector3(1.5f, 0.4f, 1.5f), stone);
        }

        // b) Balok palang atas (arch/lintel)
        Vector3 lintelSize = roadAlongX
            ? new Vector3(1.1f, 0.7f, openingWidth + 1.2f)
            : new Vector3(openingWidth + 1.2f, 0.7f, 1.1f);
        MapPrimLocal(PrimitiveType.Cube, "GateLintel", gate.transform, new Vector3(0, 4.6f, 0), lintelSize, stone);

        // Atap genteng kecil di atas palang
        Vector3 roofSize = roadAlongX
            ? new Vector3(1.8f, 0.35f, openingWidth + 1.6f)
            : new Vector3(openingWidth + 1.6f, 0.35f, 1.8f);
        MapPrimLocal(PrimitiveType.Cube, "GateRoofCap", gate.transform, new Vector3(0, 5.1f, 0), roofSize, roofMat);

        // Lentera di kedua pilar
        AddPorchLantern(gate.transform, lateral * (-openingWidth * 0.5f) + inward * 0.7f + new Vector3(0, 3.2f, 0),
            new Color(1f, 0.82f, 0.45f), 10f);
        AddPorchLantern(gate.transform, lateral * ( openingWidth * 0.5f) + inward * 0.7f + new Vector3(0, 3.2f, 0),
            new Color(1f, 0.82f, 0.45f), 10f);

        // Rekatkan ke pilar pagar yang ada di sampingnya: tutup celah dengan blok batu sambungan
        float towerOffset = openingWidth * 0.5f + 1.4f;
        foreach (int sign in new[] { -1, 1 })
        {
            var tPos = lateral * (sign * towerOffset);
            MapPrimLocal(PrimitiveType.Cube, "WallFlank", gate.transform, tPos + new Vector3(0, 2.0f, 0),
                roadAlongX ? new Vector3(0.9f, 4.0f, 1.6f) : new Vector3(1.6f, 4.0f, 0.9f), stone);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. JALAN PENGHUBUNG (sambungan putus & akses altar)
    // ─────────────────────────────────────────────────────────────────────
    static void LinkMissingRoads(Transform parent)
    {
        var roadMat = GetOrCreateVillageRoadMaterial();
        var g = new GameObject("Map_Link_Roads");
        g.transform.SetParent(parent, false);

        // Celah antara ujung Avenue (±54) dan awal jalan ekspansi (±66)
        CreateContinuousPavedRoad(g.transform,
            new Vector3(-66f, 0.05f, 0f), new Vector3(-54f, 0.05f, 0f), 5.5f, roadMat);
        CreateContinuousPavedRoad(g.transform,
            new Vector3( 54f, 0.05f, 0f), new Vector3( 66f, 0.05f, 0f), 5.5f, roadMat);

        // Jalan setapak dari ujung Avenue Utara (14, 0, 52) menuju Altar Segel (14, 0, 75)
        CreateContinuousPavedRoad(g.transform,
            new Vector3(14f, 0.05f, 52f), new Vector3(14f, 0.05f, 75f), 4.0f, roadMat);
        // Cabang belok ke altar di x=6.9f jika posisinya offset
        CreateContinuousPavedRoad(g.transform,
            new Vector3(14f, 0.05f, 75f), new Vector3(6.9f, 0.05f, 75f), 3.5f, roadMat);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3. LAMPU JALAN DALAM DESA
    // ─────────────────────────────────────────────────────────────────────
    static void PlaceInnerAvenueLamps(Transform parent)
    {
        var lamps = new GameObject("Map_Inner_Lamps");
        lamps.transform.SetParent(parent, false);
        var t = lamps.transform;

        // Jalan Utama Utara (x = 14, z = 14..50, langkah 12 m)
        for (float z = 18f; z <= 48f; z += 12f)
        {
            PlaceMapLamp(t, new Vector3(10.8f, 0f, z), Vector3.right);
            PlaceMapLamp(t, new Vector3(17.2f, 0f, z), Vector3.left);
        }

        // Jalan Utama Selatan (x = -14, z = -14..-50, langkah 12 m)
        for (float z = -18f; z >= -48f; z -= 12f)
        {
            PlaceMapLamp(t, new Vector3(-17.2f, 0f, z), Vector3.right);
            PlaceMapLamp(t, new Vector3(-10.8f, 0f, z), Vector3.left);
        }

        // Jalan Utama Barat (z = 0, x = -18..-50)
        for (float x = -18f; x >= -50f; x -= 12f)
        {
            PlaceMapLamp(t, new Vector3(x, 0f,  3.2f), Vector3.back);
            PlaceMapLamp(t, new Vector3(x, 0f, -3.2f), Vector3.forward);
        }

        // Jalan Utama Timur (z = 0, x = 18..50)
        for (float x = 18f; x <= 50f; x += 12f)
        {
            PlaceMapLamp(t, new Vector3(x, 0f,  3.2f), Vector3.back);
            PlaceMapLamp(t, new Vector3(x, 0f, -3.2f), Vector3.forward);
        }

        // Jalur Alun-Alun (Civic Promenade, z = ±14 dan x = ±14)
        foreach (float sx in new[] { -14f, 14f })
        foreach (float sz in new[] { -14f, 14f })
        {
            PlaceMapLamp(t, new Vector3(sx, 0f, sz), (new Vector3(-sx, 0f, -sz)).normalized);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  4. PELATARAN TANAH DI BAWAH RUMAH
    // ─────────────────────────────────────────────────────────────────────
    static void BuildHouseYardPads(Transform parent)
    {
        var hRoot = GameObject.Find("--- RESIDENTIAL DISTRICTS ---");
        if (hRoot == null) return;

        var pads = new GameObject("Map_House_Pads");
        pads.transform.SetParent(parent, false);

        var dirtMat = MapMat("YardDirt", new Color(0.38f, 0.32f, 0.22f), 0.02f);
        var gravelMat = MapMat("YardGravel", new Color(0.50f, 0.47f, 0.40f), 0.05f);

        // Cari setiap rumah di 4 distrik
        var renderers = hRoot.GetComponentsInChildren<MeshRenderer>();
        var houseBounds = new Dictionary<Transform, Bounds>();
        foreach (var r in renderers)
        {
            var top = r.transform;
            while (top.parent != null && top.parent != hRoot.transform
                   && !top.parent.name.StartsWith("District_"))
            {
                top = top.parent;
            }
            if (!houseBounds.TryGetValue(top, out var b)) houseBounds[top] = r.bounds;
            else { b.Encapsulate(r.bounds); houseBounds[top] = b; }
        }

        int count = 0;
        foreach (var kv in houseBounds)
        {
            var b = kv.Value;
            if (b.size.x < 3f || b.size.z < 3f) continue;

            // Pelataran tanah liat/kerikil tipis tepat di atas rumput (y = 0.02)
            Vector3 padPos = new Vector3(b.center.x, 0.02f, b.center.z);
            Vector3 padScale = new Vector3(b.size.x + 2.4f, 0.04f, b.size.z + 2.4f);
            var mat = (count % 3 == 0) ? gravelMat : dirtMat;
            MapPrimLocal(PrimitiveType.Cube, "YardPad_" + kv.Key.name, pads.transform, padPos, padScale, mat);
            count++;
        }
        Debug.Log($"[Eldara] Menambahkan pelataran untuk {count} rumah warga.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5a. DISTRIK PERTANIAN (barat laut): petak gandum, wortel, orang-orangan sawah
    // ─────────────────────────────────────────────────────────────────────
    static void BuildFarmDistrict(Transform parent)
    {
        var farm = MapGroup(parent, "Farm_District_West", Vector3.zero);

        var soilMat = MapMat("FarmSoil", new Color(0.24f, 0.17f, 0.10f), 0.02f);
        var wheatMat = MapMat("FarmWheat", new Color(0.82f, 0.68f, 0.26f), 0.05f);
        var cabbageMat = MapMat("FarmCabbage", new Color(0.30f, 0.55f, 0.22f), 0.10f);
        var fenceWood = MapMat("FarmFenceWood", new Color(0.32f, 0.22f, 0.12f));

        // 6 petak sawah/ladang teratur di x=-55..-73, z=20..45
        float[] xs = { -58.5f, -69.5f };
        float[] zs = { 22f, 32f, 42f };
        int idx = 0;
        foreach (float cx in xs)
        foreach (float cz in zs)
        {
            var plot = MapGroup(farm.transform, $"Plot_{idx++}", new Vector3(cx, 0f, cz));

            // Tanah gembur
            MapPrimLocal(PrimitiveType.Cube, "SoilBed", plot.transform, new Vector3(0, 0.08f, 0),
                new Vector3(8.5f, 0.16f, 7.5f), soilMat);

            // Baris tanaman (alur/bedengan)
            var cropMat = (idx % 2 == 0) ? wheatMat : cabbageMat;
            for (float rz = -2.8f; rz <= 2.8f; rz += 1.4f)
            {
                MapPrimLocal(PrimitiveType.Cube, "CropRow", plot.transform,
                    new Vector3(0, 0.35f, rz), new Vector3(7.8f, 0.45f, 0.7f), cropMat);
            }

            // Pagar pendek di sekeliling tiap petak
            MapPrimLocal(PrimitiveType.Cube, "Fence_N", plot.transform, new Vector3(0, 0.4f,  3.9f), new Vector3(8.8f, 0.6f, 0.12f), fenceWood);
            MapPrimLocal(PrimitiveType.Cube, "Fence_S", plot.transform, new Vector3(0, 0.4f, -3.9f), new Vector3(8.8f, 0.6f, 0.12f), fenceWood);
            MapPrimLocal(PrimitiveType.Cube, "Fence_W", plot.transform, new Vector3(-4.4f, 0.4f, 0), new Vector3(0.12f, 0.6f, 7.8f), fenceWood);
            MapPrimLocal(PrimitiveType.Cube, "Fence_E", plot.transform, new Vector3( 4.4f, 0.4f, 0), new Vector3(0.12f, 0.6f, 7.8f), fenceWood);
        }

        // Jalan setapak tanah penghubung ladang ke Avenue Barat & Utara
        var dirtMat = MapMat("YardDirt", new Color(0.38f, 0.32f, 0.22f), 0.02f);
        CreateContinuousPavedRoad(farm.transform,
            new Vector3(-50f, 0.04f, 22f), new Vector3(-72f, 0.04f, 22f), 2.2f, dirtMat);
        CreateContinuousPavedRoad(farm.transform,
            new Vector3(-64f, 0.04f, 15f), new Vector3(-64f, 0.04f, 46f), 2.2f, dirtMat);

        // 2 orang-orangan sawah di tengah ladang
        BuildScarecrow(farm.transform, new Vector3(-58.5f, 0f, 27f), 30f);
        BuildScarecrow(farm.transform, new Vector3(-69.5f, 0f, 37f), -45f);
    }

    static void BuildScarecrow(Transform parent, Vector3 pos, float yaw)
    {
        var sc = MapGroup(parent, "Scarecrow", pos, yaw);
        var wood = MapMat("ScWood", new Color(0.35f, 0.23f, 0.13f));
        var cloth = MapMat("ScCloth", new Color(0.68f, 0.22f, 0.18f));
        var straw = MapMat("ScStraw", new Color(0.85f, 0.74f, 0.38f));

        // Tiang salib kayu
        MapPrimLocal(PrimitiveType.Cylinder, "Post", sc.transform, new Vector3(0, 1.2f, 0), new Vector3(0.12f, 1.2f, 0.12f), wood);
        MapPrimLocal(PrimitiveType.Cylinder, "Arms", sc.transform, new Vector3(0, 1.7f, 0), new Vector3(0.1f, 0.9f, 0.1f), wood)
            .transform.localRotation = Quaternion.Euler(0, 0, 90f);

        // Baju rombeng & kepala jerami
        MapPrimLocal(PrimitiveType.Cube, "Body", sc.transform, new Vector3(0, 1.4f, 0), new Vector3(0.55f, 0.8f, 0.35f), cloth);
        MapPrimLocal(PrimitiveType.Sphere, "Head", sc.transform, new Vector3(0, 2.15f, 0), new Vector3(0.45f, 0.5f, 0.45f), straw);
        MapPrimLocal(PrimitiveType.Cylinder, "HatBrim", sc.transform, new Vector3(0, 2.45f, 0), new Vector3(0.75f, 0.06f, 0.75f), straw);
        MapPrimLocal(PrimitiveType.Cylinder, "HatCrown", sc.transform, new Vector3(0, 2.58f, 0), new Vector3(0.42f, 0.22f, 0.42f), straw);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5b. KINCIR ANGIN ELDARA (barat, x = -58, z = 10): lambang kemakmuran desa
    // ─────────────────────────────────────────────────────────────────────
    static void BuildWindmill(Transform parent, Vector3 basePos)
    {
        var wm = MapGroup(parent, "Windmill_Eldara", basePos);
        var stone = MapMat("WindmillStone", new Color(0.72f, 0.68f, 0.60f), 0.05f);
        var wood = MapMat("WindmillWood", new Color(0.35f, 0.23f, 0.13f));
        var roofMat = MapMat("WindmillRoof", new Color(0.28f, 0.17f, 0.10f));
        var sail = MapMat("WindmillSail", new Color(0.92f, 0.90f, 0.82f), 0.10f);

        // Badan menara (bertingkat makin mengecil ke atas)
        MapPrimLocal(PrimitiveType.Cylinder, "Base_Tier1", wm.transform, new Vector3(0, 2.0f, 0), new Vector3(5.0f, 2.0f, 5.0f), stone);
        MapPrimLocal(PrimitiveType.Cylinder, "Base_Tier2", wm.transform, new Vector3(0, 5.5f, 0), new Vector3(4.2f, 2.0f, 4.2f), stone);
        MapPrimLocal(PrimitiveType.Cylinder, "Base_Tier3", wm.transform, new Vector3(0, 8.5f, 0), new Vector3(3.5f, 1.5f, 3.5f), wood);

        // Atap kerucut
        var roof = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        roof.name = "RoofDome";
        roof.transform.SetParent(wm.transform, false);
        roof.transform.localPosition = new Vector3(0, 10.5f, 0);
        roof.transform.localScale = new Vector3(4.0f, 2.2f, 4.0f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMat;

        // Pintu masuk di lantai bawah
        MapPrimLocal(PrimitiveType.Cube, "Door", wm.transform, new Vector3(0, 1.1f, -2.55f), new Vector3(1.2f, 2.0f, 0.2f), wood);

        // Poros baling-baling kincir
        var hub = new GameObject("Windmill_Hub");
        hub.transform.SetParent(wm.transform, false);
        hub.transform.localPosition = new Vector3(0, 9.2f, -2.1f);

        // 4 bilah baling-baling berputar
        for (int k = 0; k < 4; k++)
        {
            var arm = new GameObject($"Blade_{k}");
            arm.transform.SetParent(hub.transform, false);
            arm.transform.localRotation = Quaternion.Euler(k * 90f, 0, 0);
            MapPrimLocal(PrimitiveType.Cube, "Spar", arm.transform, new Vector3(0, 2.8f, 0), new Vector3(0.12f, 5.2f, 0.14f), wood);
            MapPrimLocal(PrimitiveType.Cube, "Sail", arm.transform, new Vector3(0.0f, 3.4f, 0.62f), new Vector3(0.04f, 3.8f, 1.1f), sail);
        }
        var spin = hub.AddComponent<VillageSpinner>();
        spin.axis = Vector3.right;
        spin.degreesPerSecond = 14f;

        AddPorchLantern(wm.transform, new Vector3(0, 2.7f, -2.8f), new Color(1f, 0.72f, 0.35f), 8f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5c. KOLAM DESA (barat daya, x = -58, z = -23): tempat cuci & minum hewan
    // ─────────────────────────────────────────────────────────────────────
    static void BuildVillagePond(Transform parent, Vector3 c)
    {
        var pond = MapGroup(parent, "Village_Pond", Vector3.zero);

        // Dasar kolam (disc tipis di tanah)
        var bed = CreateMeshObject("Pond_Bed", pond.transform, CreateCircularDiscMesh("Pond_Bed_Mesh", 7.4f, 0.03f),
            MapMat("PondBed", new Color(0.12f, 0.16f, 0.14f), 0.02f), new Color(0.12f, 0.16f, 0.14f));
        bed.transform.position = c;

        var rim = CreateMeshObject("Pond_Rim", pond.transform,
            CreateHollowRingMesh("Pond_Rim_Mesh", outerR: 7.8f, innerR: 6.8f, topY: 0.18f, bottomY: 0f),
            MapMat("PondRim", new Color(0.38f, 0.35f, 0.28f), 0.05f), new Color(0.38f, 0.35f, 0.28f));
        rim.transform.position = c;

        // Air realistis
        var water = CreateMeshObject("Pond_Water", pond.transform,
            CreateTessellatedWaterMesh("Pond_Water_Mesh", radius: 6.8f, yLevel: 0f, rings: 4, segments: 24),
            GetOrCreateRealisticWaterMaterial(), new Color(0.18f, 0.38f, 0.42f, 0.75f));
        water.transform.position = c + new Vector3(0f, 0.10f, 0f);

        // Dermaga kayu kecil untuk menimba air
        var wood = MapMat("DockWood", new Color(0.38f, 0.25f, 0.14f));
        var dock = MapGroup(pond.transform, "Pond_Dock", c + new Vector3(0f, 0.18f, 5.4f));
        MapPrimLocal(PrimitiveType.Cube, "Planks", dock.transform, Vector3.zero, new Vector3(2.0f, 0.14f, 3.6f), wood);
        MapPrimLocal(PrimitiveType.Cylinder, "Post_L", dock.transform, new Vector3(-0.9f, -0.4f, -1.5f), new Vector3(0.14f, 0.6f, 0.14f), wood);
        MapPrimLocal(PrimitiveType.Cylinder, "Post_R", dock.transform, new Vector3( 0.9f, -0.4f, -1.5f), new Vector3(0.14f, 0.6f, 0.14f), wood);

        // Lentera di dermaga
        AddPorchLantern(dock.transform, new Vector3(0.9f, 0.9f, -1.5f), new Color(1f, 0.80f, 0.42f), 7f);

        // Beberapa batu di tepi kolam
        var stone = MapMat("PondStone", new Color(0.48f, 0.46f, 0.42f));
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.PI * 2f / 8f) + 0.2f;
            var bPos = c + new Vector3(Mathf.Cos(ang) * 7.1f, 0.18f, Mathf.Sin(ang) * 7.1f);
            var rock = MapPrim(PrimitiveType.Sphere, $"RimRock_{i}", pond.transform, bPos,
                new Vector3(0.9f, 0.5f, 0.7f), stone);
            rock.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0, 360f), 0f);
        }

        // Jalur setapak dari Avenue Barat (z=0) ke kolam (z=-23)
        var dirtMat = MapMat("YardDirt", new Color(0.38f, 0.32f, 0.22f), 0.02f);
        CreateContinuousPavedRoad(pond.transform,
            new Vector3(-58f, 0.04f, 0f), new Vector3(-58f, 0.04f, -16f), 2.4f, dirtMat);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5d. KANDANG TERNAK (timur laut): pagar, jerami, palung, domba
    // ─────────────────────────────────────────────────────────────────────
    static void PlacePenFence(Transform parent, Vector3 pos, float yRot, Material fallback)
    {
        var f = InstantiateArtModel("EA03_Village_Fence_01a", pos, Quaternion.Euler(0, yRot, 0), Vector3.one, parent);
        if (f == null)
            MapPrim(PrimitiveType.Cube, "FenceFallback", parent, pos + new Vector3(0, 0.6f, 0),
                new Vector3(2.5f, 1.1f, 0.15f), fallback);
    }

    static void BuildLivestockPen(Transform parent, Vector3 c)
    {
        var pen = MapGroup(parent, "Livestock_Pen", c);
        var hayMat = MapMat("HayMat", new Color(0.85f, 0.75f, 0.35f), 0.02f);
        var wood = MapMat("PenWood", new Color(0.34f, 0.22f, 0.12f));
        var mud = MapMat("PenMud", new Color(0.30f, 0.24f, 0.16f), 0.01f);

        // Lantai tanah becek kandang (16 x 14 m)
        MapPrimLocal(PrimitiveType.Cube, "MudFloor", pen.transform, new Vector3(0, 0.03f, 0),
            new Vector3(16f, 0.06f, 14f), mud);

        // Pagar keliling kandang dengan bukaan di barat (x = -8)
        var fenceFall = wood;
        for (float x = -7f; x <= 7f; x += 2.8f)
        {
            PlacePenFence(pen.transform, c + new Vector3(x, 0,  7f), 0f,   fenceFall);
            PlacePenFence(pen.transform, c + new Vector3(x, 0, -7f), 180f, fenceFall);
        }
        for (float z = -5.6f; z <= 5.6f; z += 2.8f)
        {
            PlacePenFence(pen.transform, c + new Vector3( 8f, 0, z), 90f,  fenceFall);
            if (Mathf.Abs(z) > 2.0f) // bukaan pintu gerbang kandang di barat
                PlacePenFence(pen.transform, c + new Vector3(-8f, 0, z), -90f, fenceFall);
        }

        // Tumpukan jerami (hay bales)
        MapPrimLocal(PrimitiveType.Cylinder, "HayBale_1", pen.transform, new Vector3(4.5f, 0.6f, 4.0f), new Vector3(1.4f, 1.0f, 1.4f), hayMat);
        MapPrimLocal(PrimitiveType.Cylinder, "HayBale_2", pen.transform, new Vector3(5.8f, 0.6f, 3.8f), new Vector3(1.3f, 0.9f, 1.3f), hayMat);
        MapPrimLocal(PrimitiveType.Cylinder, "HayBale_3", pen.transform, new Vector3(5.1f, 1.4f, 3.9f), new Vector3(1.3f, 0.9f, 1.3f), hayMat);

        // Palung minum kayu
        MapPrimLocal(PrimitiveType.Cube, "Trough", pen.transform, new Vector3(-3f, 0.4f, 4.5f), new Vector3(3.2f, 0.7f, 1.0f), wood);
        MapPrimLocal(PrimitiveType.Cube, "Water", pen.transform, new Vector3(-3f, 0.6f, 4.5f), new Vector3(2.9f, 0.2f, 0.8f),
            MapMat("TroughWater", new Color(0.2f, 0.35f, 0.45f), 0.8f));

        // Hewan ternak dari paket model (jika ada)
        for (int i = 0; i < 3; i++)
        {
            var p = c + new Vector3(-2f + i * 3f, 0f, -1.5f + (i % 2) * 2.5f);
            var animal = InstantiateArtModel("EA03_Animal_Sheep_01a", p, Quaternion.Euler(0, Random.Range(0, 360f), 0),
                Vector3.one * 1.1f, pen.transform);
            if (animal == null) // fallback sederhana
            {
                var sheep = MapPrim(PrimitiveType.Sphere, $"Sheep_{i}", pen.transform, p + new Vector3(0, 0.5f, 0),
                    new Vector3(1.1f, 0.9f, 1.5f), MapMat("SheepWool", new Color(0.92f, 0.90f, 0.85f), 0.05f));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5e. LAPANGAN LATIHAN PENJAGA (tenggara): boneka latihan, sasaran panah, jerami
    // ─────────────────────────────────────────────────────────────────────
    static void BuildTrainingYard(Transform parent, Vector3 c)
    {
        var yard = MapGroup(parent, "Training_Yard", Vector3.zero);
        var sand = MapMat("YardSand", new Color(0.55f, 0.50f, 0.38f), 0.02f);
        var wood = MapMat("TrainWood", new Color(0.35f, 0.22f, 0.12f));
        var straw = MapMat("TargetStraw", new Color(0.85f, 0.75f, 0.35f), 0.05f);

        // Pelataran pasir/tanah padat untuk bertarung
        MapPrim(PrimitiveType.Cube, "SandArena", yard.transform, c + new Vector3(0, 0.03f, 0),
            new Vector3(18f, 0.06f, 14f), sand);

        // 3 boneka latihan pedang (training dummies)
        for (int i = 0; i < 3; i++)
        {
            var dPos = c + new Vector3(-5f + i * 4.5f, 0f, 3.0f);
            var dummy = MapGroup(yard.transform, $"Dummy_{i}", dPos);
            MapPrimLocal(PrimitiveType.Cylinder, "Pole", dummy.transform, new Vector3(0, 1.0f, 0), new Vector3(0.16f, 1.0f, 0.16f), wood);
            MapPrimLocal(PrimitiveType.Cube, "CrossArm", dummy.transform, new Vector3(0, 1.4f, 0), new Vector3(1.3f, 0.14f, 0.14f), wood);
            MapPrimLocal(PrimitiveType.Sphere, "Torso", dummy.transform, new Vector3(0, 1.25f, 0), new Vector3(0.6f, 0.9f, 0.6f), straw);
            MapPrimLocal(PrimitiveType.Sphere, "Head", dummy.transform, new Vector3(0, 1.9f, 0), new Vector3(0.4f, 0.4f, 0.4f), straw);
        }

        // 2 sasaran panah jerami (archery targets) di sisi selatan
        for (int i = 0; i < 2; i++)
        {
            var tPos = c + new Vector3(-3f + i * 6f, 0f, -4.5f);
            var target = MapGroup(yard.transform, $"Target_{i}", tPos, yaw: 180f);
            // Kaki penyangga A-frame
            MapPrimLocal(PrimitiveType.Cube, "LegL", target.transform, new Vector3(-0.4f, 0.8f, -0.2f), new Vector3(0.08f, 1.6f, 0.08f), wood)
                .transform.localRotation = Quaternion.Euler(12f, 0, 8f);
            MapPrimLocal(PrimitiveType.Cube, "LegR", target.transform, new Vector3( 0.4f, 0.8f, -0.2f), new Vector3(0.08f, 1.6f, 0.08f), wood)
                .transform.localRotation = Quaternion.Euler(12f, 0, -8f);
            MapPrimLocal(PrimitiveType.Cube, "LegBack", target.transform, new Vector3(0, 0.8f, 0.35f), new Vector3(0.08f, 1.6f, 0.08f), wood)
                .transform.localRotation = Quaternion.Euler(-20f, 0, 0);

            // Lingkaran sasaran jerami bundar
            var disc = MapPrimLocal(PrimitiveType.Cylinder, "StrawFace", target.transform, new Vector3(0, 1.35f, 0),
                new Vector3(1.2f, 0.12f, 1.2f), straw);
            disc.transform.localRotation = Quaternion.Euler(80f, 0, 0);

            // Cincin sasaran merah di tengah
            var bullseye = MapPrimLocal(PrimitiveType.Cylinder, "Bullseye", target.transform, new Vector3(0, 1.35f, -0.06f),
                new Vector3(0.45f, 0.14f, 0.45f), MapMat("BullseyeRed", new Color(0.78f, 0.18f, 0.14f)));
            bullseye.transform.localRotation = Quaternion.Euler(80f, 0, 0);
        }

        // Rak senjata & tumpukan perisai kayu
        MapPrim(PrimitiveType.Cube, "WeaponRack", yard.transform, c + new Vector3(7.5f, 0.8f, 0f),
            new Vector3(0.5f, 1.6f, 3.2f), wood);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5f. RUMPUN POHON DESA: mengisi pita kosong selebar ±30 m di dalam pagar
    // ─────────────────────────────────────────────────────────────────────
    static bool InMapFeatureZone(float x, float z)
    {
        // Hindari zona fitur peta baru:
        if (x < -48f && z > 14f) return true;               // distrik pertanian
        if (Vector2.Distance(new Vector2(x, z), new Vector2(-58f, 10f)) < 8f) return true;  // kincir angin
        if (Vector2.Distance(new Vector2(x, z), new Vector2(-58f, -23f)) < 11f) return true; // kolam
        if (Vector2.Distance(new Vector2(x, z), new Vector2(58f, 20f)) < 12f) return true;  // kandang ternak
        if (Vector2.Distance(new Vector2(x, z), new Vector2(62f, -26f)) < 13f) return true; // lapangan latihan
        return false;
    }

    static void BuildVillageGroves(Transform parent)
    {
        var grove = MapGroup(parent, "Village_Groves", Vector3.zero);

        var bark = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var birchBark = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: true);
        var summer = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var spring = AssetMaterialSetup.SetupProceduralFoliageMaterial("SPRING");

        const float cell = 6.5f;
        float seed = Random.Range(0f, 1000f);
        int count = 0;

        for (float gx = -78f; gx < 78f; gx += cell)
        for (float gz = -52f; gz < 50f; gz += cell)
        {
            float x = gx + Random.Range(0.2f, 0.8f) * cell;
            float z = gz + Random.Range(0.2f, 0.8f) * cell;

            float ax = Mathf.Abs(x);
            if (ax < 50f || ax > 76f) continue;               // hanya pita samping dalam pagar
            if (IsReservedSpot(x, z)) continue;
            if (DistanceToRoads(x, z) < 5f) continue;
            if (InMapFeatureZone(x, z)) continue;

            float n = Mathf.PerlinNoise(seed + x * 0.05f, seed + z * 0.05f);
            if (n < 0.28f) continue;                           // sisakan sedikit padang terbuka

            var pos = new Vector3(x, 0f, z);
            if (!IsAreaFree(pos, 2.2f)) continue;

            float scale = Random.Range(1.0f, 1.45f);
            float r = Random.value;
            if (r < 0.35f)      RealisticTreeBuilder.BuildPineTree(pos, scale, grove.transform, bark, summer);
            else if (r < 0.75f) RealisticTreeBuilder.BuildOakTree(pos, scale, grove.transform, bark, summer);
            else                RealisticTreeBuilder.BuildBirchTree(pos, scale, grove.transform, birchBark, spring);
            count++;
        }

        Debug.Log($"[Eldara] Rumpun pohon desa: {count} pohon.");
    }
}
#endif
