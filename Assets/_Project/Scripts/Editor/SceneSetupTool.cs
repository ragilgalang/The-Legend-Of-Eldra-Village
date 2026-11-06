#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tool pembangun dunia (World Builder) untuk Legenda Desa Eldara.
/// Membangun desa RPG medieval skala penuh dengan 12+ bangunan, alun-alun desa,
/// kawasan pemukiman, pandai besi, pertanian, menara benteng, hutan lebat keliling, dan altar kuno.
/// Menu: Eldara > Setup Prototype Scene
/// </summary>
public static class SceneSetupTool
{
    // ─────────────────────────────────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Eldara/Setup Prototype Scene")]
    public static void SetupScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop!", "Hentikan Play Mode dulu sebelum setup scene.", "OK");
            return;
        }

        // 1. Tag & Material pre-flight
        EnsureTagExists("NPC");
        var slavicMat = AssetMaterialSetup.SetupSlavicMaterial();
        AssetMaterialSetup.SetupMedievalMaterials();

        // 2. Setup Terrain & Atmosfer
        SetupEnvironment();

        // 3. Setup Player & Camera
        SetupPlayer(out GameObject player, out PlayerController playerCtrl,
                    out Health playerHealth, out PlayerInteractor playerInteractor);
        SetupCamera(player);

        // 4. Setup Managers
        SetupManagers(playerCtrl, playerHealth);

        // 5. Bangun Desa Skala Penuh (Kawasan Pemukiman, Pasar, Pertanian, Pertambangan)
        SetupExpansiveVillage(slavicMat);

        // 6. Tempatkan Penduduk Desa (NPCs di lokasi aktivitas masing-masing)
        SetupNPCs();

        // 7. Benteng Pertahanan (8 Menara Pengawas + Barikade + Jebakan)
        SetupFortifications(slavicMat);

        // 8. Hutan Lebat & Tebing Alam Keliling (100+ Pohon & Gugusan Batu)
        SetupSurroundingWilderness(slavicMat);

        // 8b. Hamparan Padang Rumput 3D Realistis & Bunga Liar Dinamis
        var grassTuftMat = AssetMaterialSetup.SetupGrassMaterial();
        SetupRealisticGrassFields(grassTuftMat);

        // 9. Titik Sumber Daya Tambang & Altar Segel Kuno
        SetupWorldObjects(slavicMat);

        // 9b. Ekosistem Fauna & Hewan Desa Eldara (Anjing, Kucing, Serigala, Elang, Ikan)
        SetupFaunaAndAnimals();

        // 10. Titik Invasi Monster Malam (4 Arah Hutan Liar)
        SetupEnemySpawners();

        // 11. Antarmuka Pemain (Full HUD)
        SetupHUD(playerInteractor, playerHealth);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog(
            "🏰 Desa Eldara Skala Penuh Berhasil Dibangun!",
            "Selamat datang di Desa Eldara versi ekspansif!\n\n" +
            "Fitur yang telah dibangun:\n" +
            "☀️ Matahari 3D menyala melengkung di langit (terbit di timur, tenggelam di barat)\n" +
            "🌙 Bulan perak terbit di sisi seberang saat malam tiba\n" +
            "☁️ 22+ Awan 3D melayang dinamis ditiup angin & berubah warna fajar-siang-senja\n" +
            "🏛️ Balai Desa Megah + 6 Rumah Warga & Pondok Khusus\n" +
            "🎪 Alun-Alun Pasar: Gerobak pedagang, peti, tong kayu, bangku, & rambu desa\n" +
            "🌾 Kawasan Pertanian: Lumbung gandum, kandang ternak, & pagar kebun\n" +
            "⚒️ Kawasan Pandai Besi: Bengkel tempa, tumpukan balok kayu, & area tambang bijih\n" +
            "🔮 Kompleks Altar Mistis: Formasi 12 pilar batu kuno di utara desa\n" +
            "🛡️ 8 Menara Pengawas Benteng + Pagar Kayu Melingkar Skala 80 Meter\n" +
            "🌲 Hutan Rimba Keliling: 90+ Pohon cemara & ek bervariasi mengitari desa\n" +
            "🐾 Ekosistem Fauna: Anjing Penjaga (Milo), Kucing Desa (Luna & Oliver), Serigala Rimba, & Elang Perkasa di Langit!\n" +
            "🏮 Lentera Malam: Lampu lentera hangat di tiap rumah & menara\n\n" +
            "Tekan ▶ Play untuk mulai menjelajahi dunia barumu!",
            "Bagus Sekali, Mainkan!"
        );
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. ENVIRONMENT & ATMOSPHERE
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnvironment()
    {
        // Ground plane luas (250 x 250 meter)
        GameObject ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
        }
        ground.transform.localScale = new Vector3(25f, 1f, 25f);

        // Material Rumput Bertekstur PBR dengan Normal Map
        string grassMatPath = "Assets/_Project/Art/SlavicWorld/Materials/M_Ground_Grass.mat";
        var grassMat = AssetDatabase.LoadAssetAtPath<Material>(grassMatPath);
        if (!grassMat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            grassMat = new Material(shader);
            AssetDatabase.CreateAsset(grassMat, grassMatPath);
        }
        if (grassMat)
        {
            var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/SlavicWorld/Textures/Grass_Texture.png");
            if (grassTex)
            {
                grassMat.SetTexture("_BaseMap", grassTex);
                grassMat.SetTextureScale("_BaseMap", new Vector2(45f, 45f));
            }
            var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/SlavicWorld/Textures/Flower_Grass_NM.png");
            if (normTex)
            {
                var importer = AssetImporter.GetAtPath("Assets/_Project/Art/SlavicWorld/Textures/Flower_Grass_NM.png") as TextureImporter;
                if (importer && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
                grassMat.SetTexture("_BumpMap", normTex);
                grassMat.EnableKeyword("_NORMALMAP");
                grassMat.SetFloat("_BumpScale", 0.75f);
            }
            grassMat.SetColor("_BaseColor", new Color(0.78f, 0.92f, 0.65f, 1f));
            grassMat.SetFloat("_Smoothness", 0.04f);
            EditorUtility.SetDirty(grassMat);
            ground.GetComponent<Renderer>().sharedMaterial = grassMat;
        }

        // Kabut Atmosferik Realistis (Distance Fog)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.005f;
        RenderSettings.fogColor = new Color(0.7f, 0.76f, 0.82f);

        // Matahari (Directional Light)
        Light sunLight = null;
        var go_sun = GameObject.Find("Directional Light");
        if (go_sun) sunLight = go_sun.GetComponent<Light>();
        if (!sunLight)
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sunLight = l; break; }
        }
        if (!sunLight)
        {
            var go = new GameObject("Directional Light");
            sunLight = go.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
        sunLight.shadows = LightShadows.Soft;

        // DayNightCycle
        GameObject managers = GetOrCreateManagerRoot();
        var dnc = managers.GetComponent<DayNightCycle>() ?? managers.AddComponent<DayNightCycle>();
        var soDnc = new SerializedObject(dnc);
        soDnc.FindProperty("sun").objectReferenceValue = sunLight;

        var curve = new AnimationCurve(
            new Keyframe(0f, 0.05f), new Keyframe(0.25f, 0.25f),
            new Keyframe(0.5f, 1.25f), new Keyframe(0.75f, 0.35f),
            new Keyframe(1f, 0.05f));
        soDnc.FindProperty("sunIntensity").animationCurveValue = curve;

        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.12f, 0.16f, 0.35f), 0f),
                new GradientColorKey(new Color(1f, 0.65f, 0.35f), 0.25f),
                new GradientColorKey(new Color(1f, 0.98f, 0.92f), 0.5f),
                new GradientColorKey(new Color(1f, 0.48f, 0.22f), 0.75f),
                new GradientColorKey(new Color(0.12f, 0.16f, 0.35f), 1f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        soDnc.FindProperty("sunColor").gradientValue = grad;
        soDnc.ApplyModifiedProperties();

        // Pastikan visual bola Matahari dan Bulan terpasang di langit
        dnc.EnsureCelestialVisuals();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. PLAYER
    // ─────────────────────────────────────────────────────────────────────
    static void SetupPlayer(out GameObject player, out PlayerController playerCtrl,
                            out Health playerHealth, out PlayerInteractor playerInteractor)
    {
        player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (!player)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.05f, -5f);

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "FaceIndicator";
            visor.transform.SetParent(player.transform);
            visor.transform.localPosition = new Vector3(0, 0.4f, 0.38f);
            visor.transform.localScale = new Vector3(0.5f, 0.15f, 0.25f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            ApplyColor(visor, Color.black);
            ApplyColor(player, new Color(0.2f, 0.55f, 0.85f));
        }

        var oldCol = player.GetComponent<CapsuleCollider>();
        if (oldCol) Object.DestroyImmediate(oldCol);

        var cc = player.GetComponent<CharacterController>() ?? player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;

        playerCtrl       = player.GetComponent<PlayerController>()  ?? player.AddComponent<PlayerController>();
        playerHealth     = player.GetComponent<Health>()             ?? player.AddComponent<Health>();
        playerHealth.max = 100f;
        if (!player.GetComponent<PlayerCombat>())    player.AddComponent<PlayerCombat>();
        playerInteractor = player.GetComponent<PlayerInteractor>()   ?? player.AddComponent<PlayerInteractor>();
        if (!player.GetComponent<HitFlash>())        player.AddComponent<HitFlash>();
        if (!player.GetComponent<BuildMode>())       player.AddComponent<BuildMode>();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3. CAMERA
    // ─────────────────────────────────────────────────────────────────────
    static void SetupCamera(GameObject player)
    {
        Camera cam = Camera.main;
        if (!cam)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        var rig = cam.GetComponent<CameraRig>() ?? cam.gameObject.AddComponent<CameraRig>();
        var so = new SerializedObject(rig);
        so.FindProperty("target").objectReferenceValue = player.transform;
        var rProp = so.FindProperty("playerRenderers");
        rProp.arraySize = 1;
        rProp.GetArrayElementAtIndex(0).objectReferenceValue = player.GetComponent<Renderer>();
        so.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  4. MANAGERS
    // ─────────────────────────────────────────────────────────────────────
    static void SetupManagers(PlayerController playerCtrl, Health playerHealth)
    {
        var managers = GetOrCreateManagerRoot();

        if (!managers.GetComponent<Inventory>())       managers.AddComponent<Inventory>();
        if (!managers.GetComponent<QuestManager>())    managers.AddComponent<QuestManager>();
        if (!managers.GetComponent<DialogueManager>()) managers.AddComponent<DialogueManager>();
        if (!managers.GetComponent<MusicManager>())    managers.AddComponent<MusicManager>();
        if (!managers.GetComponent<NightLight>())      managers.AddComponent<NightLight>();
        if (!managers.GetComponent<CloudManager>())    managers.AddComponent<CloudManager>();

        var gm = managers.GetComponent<GameManager>() ?? managers.AddComponent<GameManager>();
        var soGm = new SerializedObject(gm);
        soGm.FindProperty("player").objectReferenceValue = playerCtrl;
        soGm.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        soGm.ApplyModifiedProperties();

        var saveSys = managers.GetComponent<SaveSystem>() ?? managers.AddComponent<SaveSystem>();
        var soSave = new SerializedObject(saveSys);
        soSave.FindProperty("player").objectReferenceValue = playerCtrl;
        soSave.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        soSave.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5. DESA SKALA PENUH (VILLAGE DISTRICTS & PROPS)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupExpansiveVillage(Material slavicMat)
    {
        GameObject village = GameObject.Find("Village");
        if (village) Object.DestroyImmediate(village);

        village = new GameObject("Village");

        // ── A. JALAN SETAPAK UTAMA (COBBLESTONE ROADS) ──
        for (float z = -20f; z <= 26f; z += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(0, 0.02f, z), Quaternion.identity, new Vector3(1.4f, 1f, 1.4f), village.transform, slavicMat);
        for (float x = -26f; x <= 26f; x += 4.5f)
        {
            if (Mathf.Abs(x) < 2f) continue;
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(x, 0.02f, 0), Quaternion.Euler(0, 90f, 0), new Vector3(1.4f, 1f, 1.4f), village.transform, slavicMat);
        }

        // ── B. ALUN-ALUN & PASAR DESA ──
        InstantiateArtModel("Prop_Wagon", new Vector3(5f, 0f, 3f), Quaternion.Euler(0, 35f, 0), Vector3.one, village.transform);
        InstantiateArtModel("Prop_Wagon", new Vector3(-5f, 0f, -4f), Quaternion.Euler(0, 160f, 0), Vector3.one, village.transform);
        InstantiateArtModel("EA03_Prop_Container_Crate_01a", new Vector3(7f, 0f, 4f), Quaternion.Euler(0, 15f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_02a", new Vector3(7.8f, 0f, 4.2f), Quaternion.Euler(0, 75f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_03a", new Vector3(7.3f, 0.8f, 4.1f), Quaternion.Euler(0, 30f, 0), Vector3.one * 0.9f, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(6f, 0f, 1.5f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(6.7f, 0f, 1.8f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Tabble_01a", new Vector3(-4f, 0f, 4f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-4f, 0f, 5.2f), Quaternion.Euler(0, 180f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-4f, 0f, 2.8f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Sign_Chapel_01", new Vector3(2f, 0f, -2f), Quaternion.Euler(0, -45f, 0), Vector3.one, village.transform, slavicMat);

        // ── C. BANGUNAN DESA UTAMA — MENGGUNAKAN MODULAR MEDIEVAL HOUSE ──

        // 1. Balai Desa Eldara (Town Hall besar, dari SlavicWorld karena sudah proper)
        var hall = InstantiateArtModel("EA03_Town_Building_Administrative _01a", new Vector3(0f, 0f, 26f), Quaternion.Euler(0, 180f, 0), new Vector3(1.1f, 1.1f, 1.1f), village.transform, slavicMat);
        if (hall) { hall.name = "Bld_TownHall"; AddPorchLantern(hall.transform, new Vector3(0, 3f, -4f)); }

        // 2. Rumah Kepala Desa — MODULAR: Rumah besar bata merah dengan 2 jendela lebar
        var houseLeader = BuildModularHouse(village.transform, "House_KepalaDesa",
            new Vector3(-18f, 0f, 18f), 135f, "brick", true, true);

        // 3. Rumah Warga Utara — MODULAR: Rumah plester sederhana
        var houseN1 = BuildModularHouse(village.transform, "House_WargaUtara",
            new Vector3(18f, 0f, 20f), -135f, "plaster", false, true);

        // 4. Bengkel Pandai Besi — MODULAR: Rumah kayu+plester, tanpa cerobong
        var smith = BuildModularHouse(village.transform, "House_PandaiBesi",
            new Vector3(24f, 0f, 6f), -90f, "wood", false, false);
        if (smith)
        {
            // Alat-alat pandai besi di depan bengkel
            InstantiateArtModel("EA03_Prop_Tool_Stump_02a", new Vector3(20f, 0f, 6f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Tool_Pick_01a", new Vector3(20.3f, 0.4f, 6.2f), Quaternion.Euler(45, 0, 0), Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Forester_CutOutTrees_03", new Vector3(22f, 0f, 11f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        }

        // 5. Gudang Peralatan (lebih kecil, kayu)
        var smithStorage = BuildModularHouse(village.transform, "Storage_PandaiBesi",
            new Vector3(26f, 0f, -6f), -90f, "wood", false, false, small: true);

        // 6. Rumah Petani — MODULAR: Plester, 1 jendela, cerobong
        var farmHouse = BuildModularHouse(village.transform, "House_Petani",
            new Vector3(-22f, 0f, 6f), 90f, "plaster", true, true);
        if (farmHouse)
        {
            InstantiateArtModel("EA03_Prop_Container_Bale_01a", new Vector3(-18f, 0f, 8f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Container_Bale_01a", new Vector3(-18f, 0.9f, 8.2f), Quaternion.Euler(0, 45, 0), Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Vegetable_Basket_02", new Vector3(-19f, 0f, 5f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        }

        // 7. Lumbung Gandum — MODULAR: Bata besar, tanpa jendela depan
        BuildModularHouse(village.transform, "Barn_Gandum",
            new Vector3(-26f, 0f, -8f), 60f, "brick", false, false);

        // 8. Pondok Dukun Mistis — MODULAR: Plester gelap, lentera ungu
        var shamanHut = BuildModularHouse(village.transform, "House_Dukun",
            new Vector3(18f, 0f, -14f), -45f, "plaster", true, true, small: true);
        if (shamanHut)
            AddPorchLantern(shamanHut.transform, new Vector3(-2f, 2.5f, 0), new Color(0.8f, 0.4f, 1f));

        // 9. Rumah Warga Selatan — MODULAR: Kayu, 1 jendela
        BuildModularHouse(village.transform, "House_WargaSelatan",
            new Vector3(-16f, 0f, -18f), 45f, "wood", false, true);

        // 10. Pos Penjaga Gerbang — MODULAR: Bata kecil
        BuildModularHouse(village.transform, "House_Penjaga",
            new Vector3(-9f, 0f, -22f), 0f, "brick", false, false, small: true);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MODULAR HOUSE BUILDER (MedievalVillage Kit)
    //  Merakit rumah 4x4 meter modular lengkap:
    //  - 8 panel dinding (pintu, jendela, kisi kayu Tudor, batu plester)
    //  - 4 pilar sudut batu/kayu
    //  - 4 ubin lantai kayu gelap
    //  - Atap genteng bulat 4x4 + 2 segitiga gable depan-belakang
    //  - Pintu kayu dengan engsel, daun jendela terbuka, cerobong asap batu
    //  - Tangga teras batu & lentera dinding berpendar hangat
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildModularHouse(
        Transform parent, string houseName,
        Vector3 centerPos, float yRotDeg,
        string style = "plaster",
        bool withChimney = false,
        bool withBalcony = false,
        bool small = false)
    {
        // Material per elemen arsitektur
        var matPlaster = GetMedievalMat("M_Medieval_Plaster");
        var matBrick   = GetMedievalMat("M_Medieval_UnevenBrick");
        var matWood    = GetMedievalMat("M_Medieval_WoodTrim");
        var matRoof    = GetMedievalMat("M_Medieval_RoundTiles");

        // Fallback jika belum ter-load
        if (!matPlaster) matPlaster = AssetMaterialSetup.SetupSlavicMaterial();
        if (!matBrick)   matBrick   = matPlaster;
        if (!matWood)    matWood    = matPlaster;
        if (!matRoof)    matRoof    = matPlaster;

        Material matWall   = (style == "brick") ? matBrick : (style == "wood" ? matWood : matPlaster);
        Material matCorner = (style == "wood")  ? matWood : matBrick;

        float s = small ? 0.8f : 1f;

        var houseRoot = new GameObject(houseName);
        houseRoot.transform.SetParent(parent);
        houseRoot.transform.position = centerPos;
        houseRoot.transform.rotation = Quaternion.Euler(0, yRotDeg, 0);

        // Sub-root agar seluruh prop modul terskala proporsional tanpa distorsi
        var modelContainer = new GameObject("Visual_Structure");
        modelContainer.transform.SetParent(houseRoot.transform, false);
        modelContainer.transform.localScale = Vector3.one * s;

        // Model dinding pintu & jendela sesuai style
        string doorWallFbx = (style == "brick") ? "Wall_UnevenBrick_Door_Flat" : "Wall_Plaster_Door_Flat";
        string winWallFbx  = (style == "brick") ? "Wall_UnevenBrick_Window_Wide_Flat" : "Wall_Plaster_Window_Wide_Flat";
        string solidWallFbx= (style == "brick") ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight";
        string timberWallFbx= "Wall_Plaster_WoodGrid"; // Kisi kayu Tudor untuk variasi estetik realistis

        // ── 1. DINDING DEPAN (Z = -2, menghadap Selatan lokal) ──
        // Segmen Kiri (X = -1): Pintu Masuk
        InstantiateArtModelLocal(doorWallFbx, new Vector3(-1f, 0, -2f), Quaternion.identity, Vector3.one, modelContainer.transform, matWall);
        // Daun Pintu Kayu (sedikit terbuka 30 derajat agar mengundang)
        InstantiateArtModelLocal("Door_1_Flat", new Vector3(-1.45f, 0, -1.95f), Quaternion.Euler(0, -35f, 0), Vector3.one, modelContainer.transform, matWood);

        // Segmen Kanan (X = 1): Jendela Lebar
        InstantiateArtModelLocal(winWallFbx, new Vector3(1f, 0, -2f), Quaternion.identity, Vector3.one, modelContainer.transform, matWall);
        // Daun Jendela Kayu Terbuka (Window Shutters)
        InstantiateArtModelLocal("WindowShutters_Wide_Flat_Open", new Vector3(1f, 0, -2f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);

        // ── 2. DINDING BELAKANG (Z = 2, menghadap Utara lokal) ──
        InstantiateArtModelLocal(solidWallFbx, new Vector3(-1f, 0, 2f), Quaternion.Euler(0, 180f, 0), Vector3.one, modelContainer.transform, matWall);
        InstantiateArtModelLocal(winWallFbx,   new Vector3( 1f, 0, 2f), Quaternion.Euler(0, 180f, 0), Vector3.one, modelContainer.transform, matWall);

        // ── 3. DINDING KIRI (X = -2, menghadap Barat lokal) ──
        InstantiateArtModelLocal(winWallFbx,    new Vector3(-2f, 0, -1f), Quaternion.Euler(0, 90f, 0), Vector3.one, modelContainer.transform, matWall);
        InstantiateArtModelLocal(timberWallFbx, new Vector3(-2f, 0,  1f), Quaternion.Euler(0, 90f, 0), Vector3.one, modelContainer.transform, matWall);

        // ── 4. DINDING KANAN (X = 2, menghadap Timur lokal) ──
        InstantiateArtModelLocal(timberWallFbx, new Vector3(2f, 0, -1f), Quaternion.Euler(0, -90f, 0), Vector3.one, modelContainer.transform, matWall);
        InstantiateArtModelLocal(solidWallFbx,  new Vector3(2f, 0,  1f), Quaternion.Euler(0, -90f, 0), Vector3.one, modelContainer.transform, matWall);

        // ── 5. PILAR SUDUT (4 Sudut Bangunan) ──
        string cornerFbx = (style == "wood") ? "Corner_Exterior_Wood" : "Corner_Exterior_Brick";
        InstantiateArtModelLocal(cornerFbx, new Vector3(-2f, 0, -2f), Quaternion.Euler(0, 0f, 0),   Vector3.one, modelContainer.transform, matCorner);
        InstantiateArtModelLocal(cornerFbx, new Vector3( 2f, 0, -2f), Quaternion.Euler(0, 90f, 0),  Vector3.one, modelContainer.transform, matCorner);
        InstantiateArtModelLocal(cornerFbx, new Vector3( 2f, 0,  2f), Quaternion.Euler(0, 180f, 0), Vector3.one, modelContainer.transform, matCorner);
        InstantiateArtModelLocal(cornerFbx, new Vector3(-2f, 0,  2f), Quaternion.Euler(0, 270f, 0), Vector3.one, modelContainer.transform, matCorner);

        // ── 6. LANTAI INTERIOR KAYU (4 Ubin Gelap) ──
        InstantiateArtModelLocal("Floor_WoodDark", new Vector3(-1f, 0.02f, -1f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);
        InstantiateArtModelLocal("Floor_WoodDark", new Vector3( 1f, 0.02f, -1f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);
        InstantiateArtModelLocal("Floor_WoodDark", new Vector3(-1f, 0.02f,  1f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);
        InstantiateArtModelLocal("Floor_WoodDark", new Vector3( 1f, 0.02f,  1f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);

        // ── 7. ATAP GENTENG BULAT MEDIEVAL 4x4 ──
        InstantiateArtModelLocal("Roof_RoundTiles_4x4", new Vector3(0, 3.1f, 0), Quaternion.identity, Vector3.one, modelContainer.transform, matRoof);
        // Segitiga Dinding Penutup Atap Depan & Belakang (Gable)
        InstantiateArtModelLocal("Roof_Front_Brick4", new Vector3(0, 3.1f, -2f), Quaternion.identity, Vector3.one, modelContainer.transform, matCorner);
        InstantiateArtModelLocal("Roof_Front_Brick4", new Vector3(0, 3.1f,  2f), Quaternion.Euler(0, 180f, 0), Vector3.one, modelContainer.transform, matCorner);

        // ── 8. CEROBONG ASAP BATU (Prop Chimney) ──
        if (withChimney)
        {
            InstantiateArtModelLocal("Prop_Chimney", new Vector3(1.1f, 4.2f, 0.6f), Quaternion.identity, Vector3.one, modelContainer.transform, matCorner);
        }

        // ── 9. TANGGA TERAS DEPAN PINTU ──
        InstantiateArtModelLocal("Stairs_Exterior_Straight", new Vector3(-1f, 0, -2.65f), Quaternion.identity, Vector3.one, modelContainer.transform, matCorner);

        // ── 10. BALKON DEPAN (Jika ada) ──
        if (withBalcony)
        {
            InstantiateArtModelLocal("Balcony_Simple_Straight", new Vector3(1f, 1.8f, -2.1f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);
        }

        // ── 11. LENTERA DINDING TERAS HANGAT ──
        AddPorchLantern(houseRoot.transform, new Vector3(-0.35f * s, 2.3f * s, -2.2f * s));

        return houseRoot;
    }

    // Helper: InstantiateArtModel dengan posisi/rotasi lokal (relatif ke parent)
    static GameObject InstantiateArtModelLocal(string fbxName, Vector3 localPos, Quaternion localRot, Vector3 scale, Transform parent, Material mat)
    {
        string path = $"Assets/_Project/Art/MedievalVillage/Models/{fbxName}.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab)
        {
            path = $"Assets/_Project/Art/SlavicWorld/Models/{fbxName}.fbx";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        if (!prefab) return null;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
        instance.transform.localScale    = scale;

        if (mat)
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;

        foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
            if (!mf.GetComponent<Collider>())
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }

        return instance;
    }

    // Helper: Dapatkan material MedievalVillage berdasarkan nama
    static Material GetMedievalMat(string matName)
    {
        string path = $"Assets/_Project/Art/MedievalVillage/Materials/{matName}.mat";
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  6. NPC POPULATION
    // ─────────────────────────────────────────────────────────────────────
    static void SetupNPCs()
    {
        GameObject npcRoot = GameObject.Find("--- NPCs ---");
        if (npcRoot) Object.DestroyImmediate(npcRoot);
        npcRoot = new GameObject("--- NPCs ---");

        var npcData = new (string npcName, Vector3 pos, Color col, string greeting)[]
        {
            ("NPC_KepalaDesa",  new Vector3(-14f, 1.05f, 16f),
             new Color(1f, 0.85f, 0.35f),
             "Selamat datang, petualang! Desa Eldara dalam bahaya besar. Saat matahari terbenam, pasukan goblin akan menyerbu dari kegelapan hutan. Persiapkan menara dan senjatamu!"),

            ("NPC_PandaiBesi",  new Vector3( 20f, 1.05f, 6f),
             new Color(0.7f, 0.35f, 0.2f),
             "Salam, ksatria! Aku butuh bijih besi dan batu keras dari area tambang timur. Bawakan padaku, dan aku akan merakit menara panah yang kokoh!"),

            ("NPC_Petani",      new Vector3(-17f, 1.05f, 5f),
             new Color(0.35f, 0.75f, 0.3f),
             "Aduh gawat, gerombolan serigala malam kemarin merusak pagar gandumku! Tolong tebang balok kayu di pinggir hutan untuk memperbaiki lumbung kami."),

            ("NPC_Dukun",       new Vector3( 15f, 1.05f,-12f),
             new Color(0.65f, 0.35f, 0.95f),
             "Altar Segel Kuno di utara berdenyut dengan energi gelap... Jika kamu berani mengaktifkannya saat malam, Raja Goblin akan terpanggil. Pastikan menara desamu sudah siap!"),
        };

        foreach (var (npcName, pos, col, greeting) in npcData)
        {
            var npcGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcGo.name = npcName;
            npcGo.tag = "NPC";
            npcGo.transform.SetParent(npcRoot.transform);
            npcGo.transform.position = pos;
            ApplyColor(npcGo, col);

            // Topi/Kepala penanda profesi
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npcGo.transform);
            head.transform.localPosition = new Vector3(0, 0.6f, 0);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            ApplyColor(head, col * 0.75f);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            npcGo.AddComponent<NpcBrain>();
            npcGo.AddComponent<NpcBehaviourTree>();
            var npcDlg = npcGo.AddComponent<NpcDialogue>();
            CreateSimpleDialogue(npcName, greeting, npcDlg);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  7. BENTENG & PERTAHANAN (8 MENARA + PAGAR 80M + JEBAKAN)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupFortifications(Material slavicMat)
    {
        GameObject towerRoot = GameObject.Find("--- TOWERS ---");
        if (towerRoot) Object.DestroyImmediate(towerRoot);

        towerRoot = new GameObject("--- TOWERS ---");

        // 8 Titik Menara Pengawas Kayu Benteng (Watchtowers)
        var towerSlots = new (Vector3 pos, string name)[]
        {
            // Gerbang Masuk Selatan (Twin Watchtowers)
            (new Vector3(-5f, 0f, -24f), "Tower_Gate_West"),
            (new Vector3( 5f, 0f, -24f), "Tower_Gate_East"),

            // Penjuru Benteng Utama
            (new Vector3(-32f, 0f,  16f), "Tower_NorthWest"),
            (new Vector3( 32f, 0f,  16f), "Tower_NorthEast"),
            (new Vector3(-35f, 0f, -14f), "Tower_SouthWest"),
            (new Vector3( 35f, 0f, -14f), "Tower_SouthEast"),

            // Sisi Sayap Luar
            (new Vector3(-36f, 0f,   2f), "Tower_WestFlank"),
            (new Vector3( 36f, 0f,   2f), "Tower_EastFlank"),
        };

        foreach (var (pos, tName) in towerSlots)
        {
            var towerObj = InstantiateArtModel("EA03_Village_Tover_01a", pos, Quaternion.identity, new Vector3(1.15f, 1.15f, 1.15f), towerRoot.transform, slavicMat);
            if (!towerObj)
            {
                towerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                towerObj.transform.position = pos + new Vector3(0, 2f, 0);
                towerObj.transform.localScale = new Vector3(2f, 4f, 2f);
                ApplyColor(towerObj, new Color(0.5f, 0.4f, 0.3f));
            }
            towerObj.name = tName;
            if (!towerObj.GetComponent<Tower>()) towerObj.AddComponent<Tower>();

            // Lentera Obor Menara Pengawas
            AddPorchLantern(towerObj.transform, new Vector3(0, 4.5f, 0), new Color(1f, 0.65f, 0.2f), 12f);
        }

        // Pagar Kayu Benteng Keliling (Perimeter Palisade ~70x60m)
        string fenceFbx = "EA03_Village_Fence_01a";
        // Sisi Selatan (dengan celah gerbang utama di tengah X = -3 s/d 3)
        for (float x = -34f; x <= 34f; x += 3.2f)
        {
            if (Mathf.Abs(x) < 4.5f) continue;
            InstantiateArtModel(fenceFbx, new Vector3(x, 0, -24f), Quaternion.Euler(0, 180f, 0), Vector3.one, towerRoot.transform, slavicMat);
        }
        // Sisi Utara (Z = 34)
        for (float x = -34f; x <= 34f; x += 3.2f)
        {
            if (Mathf.Abs(x) < 3.5f) continue; // Celah menuju Altar
            InstantiateArtModel(fenceFbx, new Vector3(x, 0, 34f), Quaternion.identity, Vector3.one, towerRoot.transform, slavicMat);
        }
        // Sisi Timur (X = 36)
        for (float z = -24f; z <= 34f; z += 3.2f)
        {
            InstantiateArtModel(fenceFbx, new Vector3(36f, 0, z), Quaternion.Euler(0, 90f, 0), Vector3.one, towerRoot.transform, slavicMat);
        }
        // Sisi Barat (X = -36)
        for (float z = -24f; z <= 34f; z += 3.2f)
        {
            InstantiateArtModel(fenceFbx, new Vector3(-36f, 0, z), Quaternion.Euler(0, -90f, 0), Vector3.one, towerRoot.transform, slavicMat);
        }

        // Spike Traps di Jalur Masuk Gerbang Utama
        var trapPositions = new Vector3[]
        {
            new Vector3( 0f, 0.05f, -21f),
            new Vector3(-2.5f, 0.05f, -26f),
            new Vector3( 2.5f, 0.05f, -26f),
            new Vector3( 0f, 0.05f, -30f),
        };

        for (int i = 0; i < trapPositions.Length; i++)
        {
            var trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trap.name = $"SpikeTrap_{i + 1}";
            trap.transform.SetParent(towerRoot.transform);
            trap.transform.position = trapPositions[i];
            trap.transform.localScale = new Vector3(2.5f, 0.12f, 2.5f);
            ApplyColor(trap, new Color(0.25f, 0.25f, 0.35f));
            trap.AddComponent<SpikeTrap>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8. HUTAN RIMBA & BATU ALAM KELILING (POHON REALISTIS SPEEDTREE STYLE)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupSurroundingWilderness(Material slavicMat)
    {
        GameObject natureRoot = GameObject.Find("--- WILDERNESS ---");
        if (natureRoot) Object.DestroyImmediate(natureRoot);

        natureRoot = new GameObject("--- WILDERNESS ---");

        var barkMat     = GetMedievalMat("M_Medieval_WoodTrim") ?? slavicMat;
        var oakLeafMat  = AssetMaterialSetup.SetupFoliageMaterial("M_Foliage_Oak", "T_RealisticLeaf_Oak");
        var pineLeafMat = AssetMaterialSetup.SetupFoliageMaterial("M_Foliage_Pine", "T_RealisticLeaf_Pine");

        string[] rockModels = {
            "EA03_Environment_Rock_CatHead_01a", "EA03_Environment_Rock_CatHead_01b",
            "EA03_Environment_Rock_Head_01a", "EA03_Environment_Rock_Head_01b", "EA03_Environment_Rock_Head_01c"
        };

        string[] bushModels = {
            "EA03_Nature_Bush_03b", "EA03_Nature_Bush_01c", "EA03_Nature_Bush_01b"
        };

        // 1. Pohon-pohon alami di dalam desa (taman, pinggir jalan, halaman rumah)
        var innerTrees = new Vector3[]
        {
            new Vector3( 10f, 0f, 12f), new Vector3(-12f, 0f, 10f),
            new Vector3( 12f, 0f, -8f), new Vector3(-10f, 0f, -8f),
            new Vector3(-4f, 0f, 12f),  new Vector3( 4f, 0f, 12f),
            new Vector3(-14f, 0f, -2f), new Vector3( 14f, 0f, -2f),
            new Vector3( 0f, 0f, -14f), new Vector3( 24f, 0f, 22f),
            new Vector3(-26f, 0f, 20f), new Vector3(-14f, 0f, -12f),
        };

        int innerIndex = 0;
        foreach (var pos in innerTrees)
        {
            innerIndex++;
            float scale = Random.Range(1.0f, 1.3f);
            GameObject treeObj;

            // Variasi pohon desa: Pohon Ek Rimbun dan Pohon Birch Indah
            if (innerIndex % 3 == 0)
            {
                treeObj = RealisticTreeBuilder.BuildBirchTree(pos, scale, natureRoot.transform, barkMat, oakLeafMat);
            }
            else
            {
                treeObj = RealisticTreeBuilder.BuildOakTree(pos, scale, natureRoot.transform, barkMat, oakLeafMat);
            }

            if (treeObj)
            {
                var sway = treeObj.GetComponent<TreeWindSway>();
                if (sway && innerIndex <= 4)
                {
                    sway.enableFallingLeaves = true;
                }

                // Semak liar & tanaman paku di pangkal pohon
                InstantiateArtModel("EA03_Nature_TrunkPlant_01a", pos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * 0.9f, natureRoot.transform, slavicMat);

                if (Random.value > 0.35f)
                {
                    string bFbx = bushModels[Random.Range(0, bushModels.Length)];
                    Vector3 bushPos = pos + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
                    var bush = InstantiateArtModel(bFbx, bushPos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * Random.Range(0.8f, 1.1f), natureRoot.transform, slavicMat);
                    if (bush)
                    {
                        var bSway = bush.AddComponent<TreeWindSway>();
                        bSway.swayIntensity = 1.0f;
                        bSway.swaySpeed = 1.8f;
                    }
                }
            }
        }

        // 2. Hutan Rimba Luar (Melindungi dan Mengitari Perimeter Benteng, Radius 42m - 90m)
        // Membentuk formasi cincin lebat dengan celah jalan masuk di Selatan
        int ringCount = 85;
        for (int i = 0; i < ringCount; i++)
        {
            float angle = (i / (float)ringCount) * 360f * Mathf.Deg2Rad;
            float deg = angle * Mathf.Rad2Deg;
            if (deg > 255f && deg < 285f) continue; // Celah gerbang selatan

            float radius = Random.Range(42f, 92f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            float scale = Random.Range(0.95f, 1.45f);

            // Kombinasi Pohon Pinus Pegunungan & Pohon Ek Lebat
            if (i % 2 == 0)
            {
                RealisticTreeBuilder.BuildPineTree(pos, scale, natureRoot.transform, barkMat, pineLeafMat);
            }
            else
            {
                RealisticTreeBuilder.BuildOakTree(pos, scale, natureRoot.transform, barkMat, oakLeafMat);
            }

            // Semak belukar di lantai hutan
            if (i % 2 == 0)
            {
                string bFbx = bushModels[Random.Range(0, bushModels.Length)];
                Vector3 bushPos = pos + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f));
                var bush = InstantiateArtModel(bFbx, bushPos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * Random.Range(0.8f, 1.3f), natureRoot.transform, slavicMat);
                if (bush)
                {
                    var bSway = bush.AddComponent<TreeWindSway>();
                    bSway.swayIntensity = 1.1f;
                    bSway.swaySpeed = 1.7f;
                }
            }

            // Bongkahan batu tebing alami
            if (i % 3 == 0)
            {
                string rFbx = rockModels[Random.Range(0, rockModels.Length)];
                Vector3 rockPos = pos + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                InstantiateArtModel(rFbx, rockPos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * Random.Range(1.2f, 2.5f), natureRoot.transform, slavicMat);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8b. HAMPARAN PADANG RUMPUT 3D REALISTIS & BUNGA LIAR
    // ─────────────────────────────────────────────────────────────────────
    static void SetupRealisticGrassFields(Material grassMat)
    {
        GameObject grassRoot = GameObject.Find("--- GRASS FIELDS ---");
        if (grassRoot) Object.DestroyImmediate(grassRoot);

        grassRoot = new GameObject("--- GRASS FIELDS ---");

        if (!grassMat) return;

        // 1. Tepi Jalan Setapak & Alun-Alun Desa (Bordir rumput alami di sela batu)
        Vector3[] pathwayPoints = {
            new Vector3( 2.2f, 0f,  4.5f), new Vector3(-2.2f, 0f,  4.5f),
            new Vector3( 2.2f, 0f,  9.0f), new Vector3(-2.2f, 0f,  9.0f),
            new Vector3( 2.5f, 0f, 15.0f), new Vector3(-2.5f, 0f, 15.0f),
            new Vector3( 5.5f, 0f,  2.0f), new Vector3( 9.5f, 0f,  2.0f),
            new Vector3(-5.5f, 0f,  2.0f), new Vector3(-9.5f, 0f,  2.0f),
            new Vector3( 2.2f, 0f, -6.0f), new Vector3(-2.2f, 0f, -6.0f),
            new Vector3( 2.2f, 0f,-12.0f), new Vector3(-2.2f, 0f,-12.0f),
            new Vector3( 0.0f, 0f,  0.5f), new Vector3( 0.0f, 0f, -0.5f),
        };
        foreach (var pt in pathwayPoints)
        {
            RealisticGrassBuilder.BuildGrassClump(pt, 7, 1.8f, grassRoot.transform, grassMat);
        }

        // 2. Pekarangan & Halaman Sekitar Bangunan Desa
        Vector3[] yardCenters = {
            new Vector3(-16f, 0f,  16f), // Halaman Rumah Warga 1
            new Vector3( 16f, 0f,  18f), // Halaman Rumah Warga 2
            new Vector3(-16f, 0f,  -4f), // Halaman Pandai Besi
            new Vector3( 18f, 0f,  -4f), // Halaman Rumah Pedagang
            new Vector3(  0f, 0f, -16f), // Halaman Selatan Dekat Gerbang
            new Vector3(-12f, 0f,  24f), // Halaman Utara Balai Desa
            new Vector3( 14f, 0f,  25f), // Halaman Kebun Desa
        };
        foreach (var yc in yardCenters)
        {
            RealisticGrassBuilder.BuildGrassClump(yc, 18, 5.5f, grassRoot.transform, grassMat);
        }

        // 3. Pangkal Pohon-Pohon Desa & Semak Alami
        var innerTrees = new Vector3[]
        {
            new Vector3( 10f, 0f, 12f), new Vector3(-12f, 0f, 10f),
            new Vector3( 12f, 0f, -8f), new Vector3(-10f, 0f, -8f),
            new Vector3(-4f, 0f, 12f),  new Vector3( 4f, 0f, 12f),
            new Vector3(-14f, 0f, -2f), new Vector3( 14f, 0f, -2f),
            new Vector3( 0f, 0f, -14f), new Vector3( 24f, 0f, 22f),
            new Vector3(-26f, 0f, 20f), new Vector3(-14f, 0f, -12f),
        };
        foreach (var tp in innerTrees)
        {
            RealisticGrassBuilder.BuildGrassClump(tp, 10, 3.0f, grassRoot.transform, grassMat);
        }

        // 4. Sepanjang Garis Perimeter Pagar Kayu & Menara Pertahanan (Radius ~32m)
        int fenceClumps = 36;
        for (int i = 0; i < fenceClumps; i++)
        {
            float ang = (i / (float)fenceClumps) * 360f * Mathf.Deg2Rad;
            float deg = ang * Mathf.Rad2Deg;
            if (deg > 255f && deg < 285f) continue; // Celah gerbang masuk selatan

            float r = Random.Range(30f, 34f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            RealisticGrassBuilder.BuildGrassClump(pos, 6, 2.5f, grassRoot.transform, grassMat);
        }

        // 5. Hamparan Padang Rumput Liar Luar Desa (Wild Meadows, Radius 36m - 75m)
        int meadowClumps = 48;
        for (int i = 0; i < meadowClumps; i++)
        {
            float ang = (i / (float)meadowClumps) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(36f, 75f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            RealisticGrassBuilder.BuildGrassClump(pos, 8, 4.0f, grassRoot.transform, grassMat);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9. WORLD OBJECTS & ALTAR COMPLEX
    // ─────────────────────────────────────────────────────────────────────
    static void SetupWorldObjects(Material slavicMat)
    {
        GameObject itemsRoot = GameObject.Find("--- WORLD OBJECTS ---");
        if (itemsRoot) Object.DestroyImmediate(itemsRoot);

        itemsRoot = new GameObject("--- WORLD OBJECTS ---");

        // ── A. AREA TAMBANG BIJIH BESI (EAST QUARRY) ──
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_1", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(30f, 0f, 10f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_2", "EA03_Environment_Rock_CatHead_01b", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(32f, 0f, 14f), Quaternion.Euler(0, 45f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_3", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(28f, 0f, -2f), Quaternion.Euler(0, 110f, 0), new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.4f, 0.4f, 0.5f));

        // ── B. AREA BALOK KAYU (WEST TIMBER YARD) ──
        PlaceArtResource(itemsRoot.transform, "Wood_Log_1", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-30f, 0f, 10f), Quaternion.identity, Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_2", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-32f, 0f, -4f), Quaternion.Euler(0, 80f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_3", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-18f, 0f, -14f), Quaternion.Euler(0, 30f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));

        // ── C. BATU ALAM TAMBANG (STONE BOULDERS) ──
        PlaceArtResource(itemsRoot.transform, "Stone_Rock_1", "EA03_Environment_Rock_Head_01a", "stone",
            "Tekan E untuk memecah Batu", new Vector3(28f, 0f, -10f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f), slavicMat, new Color(0.55f, 0.55f, 0.55f));
        PlaceArtResource(itemsRoot.transform, "Stone_Rock_2", "EA03_Environment_Rock_Head_01b", "stone",
            "Tekan E untuk memecah Batu", new Vector3(-14f, 0f, -22f), Quaternion.Euler(0, 50f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.55f, 0.55f, 0.55f));

        // ── D. KOMPLEKS ALTAR SEGEL KUNO (ANCIENT STONE CIRCLE DI UTARA DESA) ──
        Vector3 altarCenter = new Vector3(0f, 0f, 42f);

        // Altar Utama di Pusat Lingkaran Batu
        var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        altar.name = "Seal_Altar";
        altar.transform.SetParent(itemsRoot.transform);
        altar.transform.position = altarCenter + new Vector3(0, 0.3f, 0);
        altar.transform.localScale = new Vector3(4f, 0.6f, 4f);
        ApplyColor(altar, new Color(0.45f, 0.35f, 0.85f));
        altar.AddComponent<SealAltar>();

        // Efek Cahaya Mistis Altar
        AddPorchLantern(altar.transform, new Vector3(0, 1.5f, 0), new Color(0.7f, 0.2f, 1f), 15f);

        // 12 Pilar Batu Kuno Mengelilingi Altar (Stonehenge Style)
        for (int p = 0; p < 12; p++)
        {
            float angle = (p / 12f) * 360f * Mathf.Deg2Rad;
            Vector3 pillarPos = altarCenter + new Vector3(Mathf.Cos(angle) * 5.5f, 0f, Mathf.Sin(angle) * 5.5f);
            var pillar = InstantiateArtModel("EA03_Environment_Rock_Head_01c", pillarPos, Quaternion.Euler(0, Random.Range(0, 360f), 0), new Vector3(0.9f, 2.6f, 0.9f), itemsRoot.transform, slavicMat);
            if (!pillar)
            {
                pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"AltarPillar_{p}";
                pillar.transform.SetParent(itemsRoot.transform);
                pillar.transform.position = pillarPos + new Vector3(0, 1.3f, 0);
                pillar.transform.localScale = new Vector3(0.5f, 2.6f, 0.5f);
                ApplyColor(pillar, new Color(0.6f, 0.5f, 0.8f));
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }
        }
    }

    static void PlaceArtResource(Transform parent, string goName, string fbxName, string itemId,
                                 string prompt, Vector3 pos, Quaternion rot, Vector3 scale,
                                 Material slavicMat, Color fallbackCol)
    {
        var go = InstantiateArtModel(fbxName, pos, rot, scale, parent, slavicMat);
        if (!go)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.position = pos + new Vector3(0, 0.5f, 0);
            go.transform.localScale = scale;
            ApplyColor(go, fallbackCol);
        }
        go.name = goName;

        var pickup = go.GetComponent<ResourcePickup>() ?? go.AddComponent<ResourcePickup>();
        pickup.itemId = itemId;
        pickup.amount = 1;
        pickup.prompt = prompt;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9b. EKOSISTEM FAUNA & HEWAN DESA ELDARA
    // ─────────────────────────────────────────────────────────────────────
    static void SetupFaunaAndAnimals()
    {
        // Bersihkan objek ikan & fauna lama jika ada
        foreach (var strayFish in Object.FindObjectsByType<FishController>())
        {
            if (strayFish) Object.DestroyImmediate(strayFish.gameObject);
        }

        GameObject faunaRoot = GameObject.Find("--- FAUNA & ANIMALS ---");
        if (faunaRoot) Object.DestroyImmediate(faunaRoot);

        faunaRoot = new GameObject("--- FAUNA & ANIMALS ---");

        // 1. Milo - Anjing Penjaga Desa (Di dekat Balai Desa & Pandai Besi)
        AnimalAssetSetup.SpawnDog(new Vector3(4.5f, 0f, 6.5f), Quaternion.Euler(0, 190f, 0), faunaRoot.transform);

        // 2. Luna - Kucing Abu-Abu Desa (Di teras Rumah Warga 1)
        AnimalAssetSetup.SpawnCat(new Vector3(-13.5f, 0f, 14.5f), Quaternion.Euler(0, 45f, 0), faunaRoot.transform,
            "Luna", "Meow~ Luna mendengkur manja dan mengusapkan badannya ke kakimu.", false);

        // 3. Oliver - Kucing Oranye Pasar (Di dekat gerobak & peti kayu alun-alun)
        AnimalAssetSetup.SpawnCat(new Vector3(7.5f, 0f, -2.5f), Quaternion.Euler(0, -60f, 0), faunaRoot.transform,
            "Oliver", "Meow! Oliver si kucing oranye melompat manja dan menatapmu ramah.", true);

        // 4. Kawanan Serigala Rimba (Di hutan lebat luar desa)
        // Alpha Wolf: Hutan Utara dekat Altar Kuno
        AnimalAssetSetup.SpawnWolf(new Vector3(2f, 0f, 54f), Quaternion.Euler(0, 180f, 0), faunaRoot.transform, "Serigala Rimba Alpha");
        // Beta Wolf 1: Hutan Barat
        AnimalAssetSetup.SpawnWolf(new Vector3(-48f, 0f, 22f), Quaternion.Euler(0, 90f, 0), faunaRoot.transform, "Serigala Rimba Barat");
        // Beta Wolf 2: Hutan Timur
        AnimalAssetSetup.SpawnWolf(new Vector3(50f, 0f, 16f), Quaternion.Euler(0, -90f, 0), faunaRoot.transform, "Serigala Rimba Timur");

        // 5. Aquila - Elang Perkasa Penjaga Langit (Melayang tinggi melingkar di angkasa desa)
        AnimalAssetSetup.SpawnEagle(new Vector3(0f, 24f, 5f), faunaRoot.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  10. TITIK SPAWN MUSUH (DEEP IN THE WOODS)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnemySpawners()
    {
        var spawnerRoot = GameObject.Find("--- ENEMY SPAWNERS ---");
        if (spawnerRoot) Object.DestroyImmediate(spawnerRoot);
        spawnerRoot = new GameObject("--- ENEMY SPAWNERS ---");

        // 4 Titik Invasi di 4 penjuru luar benteng (di dalam hutan)
        var spawnPositions = new (Vector3 pos, string sName)[]
        {
            (new Vector3( 0f, 0f, 55f),  "SpawnPoint_North_Woods"),
            (new Vector3( 0f, 0f,-48f),  "SpawnPoint_South_Road"),
            (new Vector3( 52f,0f,   0f), "SpawnPoint_East_Mines"),
            (new Vector3(-52f,0f,   0f), "SpawnPoint_West_Forest"),
        };

        var spawnerGo = new GameObject("NightSpawner");
        spawnerGo.transform.SetParent(spawnerRoot.transform);
        spawnerGo.transform.position = Vector3.zero;
        spawnerGo.AddComponent<NightSpawner>();

        foreach (var (pos, sName) in spawnPositions)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = sName;
            marker.transform.SetParent(spawnerRoot.transform);
            marker.transform.position = pos;
            marker.transform.localScale = new Vector3(2f, 0.3f, 2f);
            ApplyColor(marker, new Color(0.85f, 0.1f, 0.1f));
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        // Titik Spawn Raja Goblin (Di utara dekat Altar)
        var bossMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bossMarker.name = "BossSpawnPoint";
        bossMarker.transform.SetParent(spawnerRoot.transform);
        bossMarker.transform.position = new Vector3(0f, 0f, 50f);
        bossMarker.transform.localScale = new Vector3(4f, 0.5f, 4f);
        ApplyColor(bossMarker, new Color(0.85f, 0f, 0.85f));
        Object.DestroyImmediate(bossMarker.GetComponent<Collider>());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  11. HUD LENGKAP
    // ─────────────────────────────────────────────────────────────────────
    static void SetupHUD(PlayerInteractor playerInteractor, Health playerHealth)
    {
        GameObject canvasGo = GameObject.Find("HUD Canvas");
        if (!canvasGo)
        {
            canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (!Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Jam Hari & Waktu (Atas Tengah)
        MakeHUDText(canvasGo, "ClockText", "Hari 1  07:00  (Siang)",
                    24, Color.white, TextAlignmentOptions.Top,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -20), new Vector2(500, 50),
                    out var clockText);
        var clockUI = canvasGo.GetComponentInChildren<ClockUI>();
        if (!clockUI)
        {
            var clockComp = GameObject.Find("ClockText").AddComponent<ClockUI>();
            var so = new SerializedObject(clockComp);
            so.FindProperty("timeText").objectReferenceValue = clockText;
            so.ApplyModifiedProperties();
        }

        // Tas / Inventory (Kiri Tengah)
        MakeHUDText(canvasGo, "InventoryText", "Tas:\n(Kosong)",
                    16, Color.yellow, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(20, 0), new Vector2(240, 300),
                    out var invText);
        if (!canvasGo.GetComponentInChildren<InventoryHUD>())
        {
            var invComp = GameObject.Find("InventoryText").AddComponent<InventoryHUD>();
            var so = new SerializedObject(invComp);
            so.FindProperty("text").objectReferenceValue = invText;
            so.ApplyModifiedProperties();
        }

        // Quest Tracker (Kanan Atas)
        MakeHUDText(canvasGo, "QuestText", "Quest:\n- Kumpulkan kayu & bijih besi\n- Temui Kepala Desa",
                    16, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.TopRight,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-20, -20), new Vector2(300, 200),
                    out var questText);
        if (!canvasGo.GetComponentInChildren<QuestHUD>())
        {
            var questComp = GameObject.Find("QuestText").AddComponent<QuestHUD>();
            var so = new SerializedObject(questComp);
            so.FindProperty("text").objectReferenceValue = questText;
            so.ApplyModifiedProperties();
        }

        // HP Bar (Bawah Tengah)
        MakeHUDText(canvasGo, "HealthText", "HP 100 / 100",
                    20, new Color(1f, 0.35f, 0.35f), TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0, 30), new Vector2(300, 40),
                    out var hpText);
        if (!canvasGo.GetComponentInChildren<HealthBar>())
        {
            var hbComp = GameObject.Find("HealthText").AddComponent<HealthBar>();
            var so = new SerializedObject(hbComp);
            var targetProp = so.FindProperty("target");
            if (targetProp != null) targetProp.objectReferenceValue = playerHealth;
            so.ApplyModifiedProperties();
        }

        // Prompt Interaksi [E]
        MakeHUDText(canvasGo, "InteractionPrompt", "[ E ] Interaksi",
                    22, Color.white, TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, 0), new Vector2(400, 50),
                    out var promptText);
        GameObject.Find("InteractionPrompt")?.SetActive(false);
        if (playerInteractor)
        {
            var so = new SerializedObject(playerInteractor);
            var pProp = so.FindProperty("promptText");
            if (pProp != null) { pProp.objectReferenceValue = promptText; so.ApplyModifiedProperties(); }
        }

        SetupDialoguePanel(canvasGo);
    }

    static void SetupDialoguePanel(GameObject canvasGo)
    {
        if (GameObject.Find("DialoguePanel")) return;

        var panelGo = new GameObject("DialoguePanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var img = panelGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0f);
        panelRect.anchorMax = new Vector2(0.85f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0, 25);
        panelRect.sizeDelta = new Vector2(0, 180);

        var nameGo = new GameObject("SpeakerName");
        nameGo.transform.SetParent(panelGo.transform, false);
        var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "Kepala Desa";
        nameTxt.fontSize = 22;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = new Color(1f, 0.85f, 0.3f);
        var nameRect = nameTxt.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot     = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(25, -15);
        nameRect.sizeDelta = new Vector2(-50, 35);

        var dlgTxtGo = new GameObject("DialogueText");
        dlgTxtGo.transform.SetParent(panelGo.transform, false);
        var dlgTxt = dlgTxtGo.AddComponent<TextMeshProUGUI>();
        dlgTxt.text = "...";
        dlgTxt.fontSize = 18;
        dlgTxt.color = Color.white;
        dlgTxt.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var dlgRect = dlgTxt.rectTransform;
        dlgRect.anchorMin = new Vector2(0f, 0f);
        dlgRect.anchorMax = new Vector2(1f, 1f);
        dlgRect.pivot     = new Vector2(0.5f, 0.5f);
        dlgRect.offsetMin = new Vector2(25, 15);
        dlgRect.offsetMax = new Vector2(-25, -45);

        panelGo.SetActive(false);

        var dm = Object.FindAnyObjectByType<DialogueManager>();
        if (dm)
        {
            var so = new SerializedObject(dm);
            var panelProp = so.FindProperty("panel");
            if (panelProp != null) panelProp.objectReferenceValue = panelGo;
            var lineProp = so.FindProperty("lineText");
            if (lineProp != null) lineProp.objectReferenceValue = dlgTxt;
            so.ApplyModifiedProperties();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPERS (LANTERNS, MODELS, TAGS)
    // ─────────────────────────────────────────────────────────────────────
    static void AddPorchLantern(Transform parent, Vector3 localOffset, Color? lightColor = null, float range = 8f)
    {
        var lanternGo = new GameObject("LanternLight");
        lanternGo.transform.SetParent(parent);
        lanternGo.transform.localPosition = localOffset;

        var l = lanternGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor ?? new Color(1f, 0.72f, 0.35f); // Hangat amber
        l.intensity = 2f;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    static GameObject InstantiateArtModel(string fbxName, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent, Material customMat = null)
    {
        string path = $"Assets/_Project/Art/SlavicWorld/Models/{fbxName}.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab)
        {
            path = $"Assets/_Project/Art/MedievalVillage/Models/{fbxName}.fbx";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        if (prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;

            if (customMat)
            {
                foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = customMat;
                }
            }

            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.GetComponent<Collider>())
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }

            return instance;
        }

        return null;
    }

    static GameObject GetOrCreateManagerRoot()
    {
        var go = GameObject.Find("--- CORE MANAGERS ---");
        if (!go) go = new GameObject("--- CORE MANAGERS ---");
        return go;
    }

    static void ApplyColor(GameObject go, Color col)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") 
                               ?? Shader.Find("Standard"));
        mat.color = col;
        r.sharedMaterial = mat;
    }

    static void MakeHUDText(
        GameObject canvas, string goName, string defaultText,
        float fontSize, Color color, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        out TextMeshProUGUI textComp)
    {
        var existing = GameObject.Find(goName);
        if (existing)
        {
            textComp = existing.GetComponent<TextMeshProUGUI>();
            if (textComp) textComp.text = defaultText;
            return;
        }

        var go = new GameObject(goName);
        go.transform.SetParent(canvas.transform, false);
        textComp = go.AddComponent<TextMeshProUGUI>();
        textComp.text = defaultText;
        textComp.fontSize = fontSize;
        textComp.color = color;
        textComp.alignment = align;
        textComp.textWrappingMode = TMPro.TextWrappingModes.Normal;

        var rect = textComp.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
    }

    static void EnsureTagExists(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return;

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    static void CreateSimpleDialogue(string npcName, string text, NpcDialogue npcDlg)
    {
        string folder = "Assets/_Project/Dialogues";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/_Project", "Dialogues");

        string assetPath = $"{folder}/{npcName}_Dialogue.asset";
        var dlgData = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);
        if (!dlgData)
        {
            dlgData = ScriptableObject.CreateInstance<DialogueData>();
            dlgData.lines = new DialogueData.Line[]
            {
                new DialogueData.Line { speaker = npcName.Replace("NPC_", ""), text = text }
            };
            AssetDatabase.CreateAsset(dlgData, assetPath);
        }

        var so = new SerializedObject(npcDlg);
        var prop = so.FindProperty("dialogue");
        if (prop != null)
        {
            prop.objectReferenceValue = dlgData;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
