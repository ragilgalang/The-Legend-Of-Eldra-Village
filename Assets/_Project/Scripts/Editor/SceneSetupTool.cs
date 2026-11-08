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
            if (!Application.isBatchMode)
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

        // 9c. EKSPANSI PETA — 4 Zona Baru: Pegunungan Utara, Rawa Selatan,
        //     Desa Nelayan Barat, Tebing Tambang Timur (total peta 500x500m)
        SetupExpandedWorld(slavicMat, grassTuftMat);

        // 10. Titik Invasi Monster Malam (4 Arah Hutan Liar)
        SetupEnemySpawners();

        // 11. Antarmuka Pemain (Full HUD)
        SetupHUD(playerInteractor, playerHealth);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = player;

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "🏰 Desa Eldara Skala Penuh Berhasil Dibangun!",
                "Selamat datang di Desa Eldara versi ekspansif!\n\n" +
                "Fitur yang telah dibangun:\n" +
                "☀️ Matahari 3D menyala melengkung di langit (terbit di timur, tenggelam di barat)\n" +
                "🌙 Bulan perak terbit di sisi seberang saat malam tiba\n" +
                "☁️ 22+ Awan 3D melayang dinamis ditiup angin & berubah warna fajar-siang-senja\n" +
                "🏡 11 Gaya Rumah Kayu Realistis: Rumah Gadang, Joglo, Minka Jepang, Log Cabin, Chalet Alpen, A-Frame, Rumah Panggung, Barn, Treehouse, Pondok Danau, & Villa Modern\n" +
                "👥 5 Karakter NPC 3D Stylized (Porting Blender): Kepala Desa (Merchant), Petani (Farmer), Pandai Besi & Penjaga Benteng (Guard), Dukun (Wizard), & Gadis Desa (Woman) — Lengkap dengan rute patroli mandiri & animasi berjalan prosedural!\n" +
                "🎪 Alun-Alun Pasar: Gerobak pedagang, peti, tong kayu, bangku, & rambu desa\n" +
                "🌾 Kawasan Pertanian: Lumbung gandum, kandang ternak, & pagar kebun\n" +
                "⚒️ Kawasan Pandai Besi: Bengkel tempa, tumpukan balok kayu, & area tambang bijih\n" +
                "🔮 Kompleks Altar Mistis: Formasi 12 pilar batu kuno di utara desa\n" +
                "🛡️ 8 Menara Pengawas Benteng + Pagar Kayu Melingkar Skala 80 Meter\n" +
                "🌲 Hutan Rimba Keliling: 90+ Pohon cemara & ek bervariasi mengitari desa\n" +
                "🐾 Ekosistem Fauna: Anjing Penjaga (Milo), Kucing Desa (Luna & Oliver), Serigala Rimba, & Elang Perkasa di Langit!\n" +
                "🏮 Lentera Malam: Lampu lentera hangat di tiap rumah & menara\n\n" +
                "🗺️ EKSPANSI PETA BARU (500×500 meter):\n" +
                "⛰️ Zona UTARA — Desa Pegunungan, Kuil Batu Kuno, Jembatan Kabut, 2 NPC Baru\n" +
                "🌿 Zona SELATAN — Rawa Misterius, Perkemahan Goblin, Kolam Racun, NPC Penyihir Rawa\n" +
                "⛵ Zona BARAT — Desa Nelayan, Dermaga Sungai Perak, Perahu Rakit, Kapten Nelayan\n" +
                "💎 Zona TIMUR — Tebing Tambang Kristal, Gua Gelap, Formasi Kristal, 2 NPC Penambang\n" +
                "🛤️ 4 Jalan Cobblestone Penghubung Antar-Zona\n" +
                "🐺 6 Serigala Tambahan di Tiap Zona Baru\n\n" +
                "Tekan ▶ Play untuk mulai menjelajahi dunia barumu!",
                "Bagus Sekali, Mainkan!"
            );
        }
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
        ground.transform.localScale = new Vector3(50f, 1f, 50f);

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

        // ── A. JALAN SETAPAK UTAMA (COBBLESTONE ROADS YANG LUAS & LAPANG) ──
        // Jalur Utama Utara - Selatan (Z = -46 s/d 44) menuju Balai Desa & Gerbang Selatan
        for (float z = -46f; z <= 44f; z += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(0, 0.02f, z), Quaternion.identity, new Vector3(1.4f, 1f, 1.4f), village.transform, slavicMat);

        // Jalur Utama Barat - Timur (X = -65 s/d 65) menghubungkan seluruh distrik desa
        for (float x = -65f; x <= 65f; x += 4.5f)
        {
            if (Mathf.Abs(x) < 2.5f) continue;
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(x, 0.02f, 0), Quaternion.Euler(0, 90f, 0), new Vector3(1.4f, 1f, 1.4f), village.transform, slavicMat);
        }

        // Jalur Cabang Barat Laut menuju Rumah Gadang & Joglo
        for (float d = 4f; d <= 32f; d += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(-d * 0.9f, 0.02f, d * 0.7f), Quaternion.Euler(0, -50f, 0), new Vector3(1.2f, 1f, 1.2f), village.transform, slavicMat);

        // Jalur Cabang Barat Daya menuju Pondok Danau & Chalet
        for (float d = 4f; d <= 32f; d += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(-d * 0.7f, 0.02f, -d * 0.9f), Quaternion.Euler(0, 40f, 0), new Vector3(1.2f, 1f, 1.2f), village.transform, slavicMat);

        // Jalur Cabang Tenggara menuju Minka & Pohon
        for (float d = 4f; d <= 32f; d += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(d * 0.9f, 0.02f, -d * 0.7f), Quaternion.Euler(0, 130f, 0), new Vector3(1.2f, 1f, 1.2f), village.transform, slavicMat);

        // Jalur Cabang Timur Laut menuju Cabin & Villa Modern
        for (float d = 4f; d <= 32f; d += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(d * 0.7f, 0.02f, d * 0.9f), Quaternion.Euler(0, -140f, 0), new Vector3(1.2f, 1f, 1.2f), village.transform, slavicMat);

        // ── B. ALUN-ALUN & PASAR DESA PUSAT (TERBUKA & LEGA) ──
        InstantiateArtModel("Prop_Wagon", new Vector3(6f, 0f, 4f), Quaternion.Euler(0, 35f, 0), Vector3.one, village.transform);
        InstantiateArtModel("Prop_Wagon", new Vector3(-6f, 0f, -5f), Quaternion.Euler(0, 160f, 0), Vector3.one, village.transform);
        InstantiateArtModel("EA03_Prop_Container_Crate_01a", new Vector3(8.5f, 0f, 5f), Quaternion.Euler(0, 15f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_02a", new Vector3(9.2f, 0f, 5.2f), Quaternion.Euler(0, 75f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_03a", new Vector3(8.8f, 0.8f, 5.1f), Quaternion.Euler(0, 30f, 0), Vector3.one * 0.9f, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(7.5f, 0f, 2f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(8.2f, 0f, 2.3f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Tabble_01a", new Vector3(-5f, 0f, 5f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-5f, 0f, 6.2f), Quaternion.Euler(0, 180f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-5f, 0f, 3.8f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Sign_Chapel_01", new Vector3(3f, 0f, -3f), Quaternion.Euler(0, -45f, 0), Vector3.one, village.transform, slavicMat);

        // ── C. BANGUNAN DESA UTAMA — 11 GAYA RUMAH KAYU (TATA LETAK LAPANG & BERJAUHAN) ──

        // 1. Balai Desa Eldara (Town Hall administratif di ujung utara jalan raya)
        var hall = InstantiateArtModel("EA03_Town_Building_Administrative _01a", new Vector3(0f, 0f, 44f), Quaternion.Euler(0, 180f, 0), new Vector3(1.1f, 1.1f, 1.1f), village.transform, slavicMat);
        if (hall) { hall.name = "Bld_TownHall"; AddPorchLantern(hall.transform, new Vector3(0, 3f, -4f)); }

        // 2. Rumah Kepala Desa / Tetua Adat — GADANG (Barat Laut, X=-42, Z=32, berjarak 52m dari alun-alun)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Gadang,
            new Vector3(-42f, 0f, 32f), Quaternion.Euler(0, 135f, 0), village.transform, "House_KepalaDesa_Gadang");

        // 3. Balai Pendopo Adat — JOGLO (Ujung Barat Laut, X=-68, Z=18f, berjarak 30m dari Rumah Gadang)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Joglo,
            new Vector3(-68f, 0f, 18f), Quaternion.Euler(0, 45f, 0), village.transform, "Pavilion_Joglo");

        // 4. Rumah Petani — PANGGUNG (Kawasan Pertanian Barat, X=-38, Z=-10, berjarak 40m dari Joglo)
        var farmHouse = ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
            new Vector3(-38f, 0f, -10f), Quaternion.Euler(0, 75f, 0), village.transform, "House_Petani_Panggung");
        if (farmHouse)
        {
            InstantiateArtModel("EA03_Prop_Container_Bale_01a", new Vector3(-34f, 0f, -8f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Container_Bale_01a", new Vector3(-34f, 0.9f, -7.8f), Quaternion.Euler(0, 45, 0), Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Vegetable_Basket_02", new Vector3(-35f, 0f, -11f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        }

        // 5. Lumbung Gandum Besar — BARN (Ladang Barat Jauh, X=-66, Z=-24, berjarak 31m dari Rumah Panggung)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Barn,
            new Vector3(-66f, 0f, -24f), Quaternion.Euler(0, 60f, 0), village.transform, "Barn_Gandum");

        // 6. Pondok Tepi Danau — DANAU (Tepi Danau Barat Daya, X=-54, Z=-44, berjarak 27m dari Barn)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Danau,
            new Vector3(-54f, 0f, -44f), Quaternion.Euler(0, 45f, 0), village.transform, "House_PondokDanau");

        // 7. Rumah Panggung Nusantara — CHALET (Kawasan Selatan, X=-24, Z=-42)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Chalet,
            new Vector3(-24f, 0f, -42f), Quaternion.Euler(0, 20f, 0), village.transform, "House_WargaSelatan_Panggung");

        // 8. Pondok Singgah Pengelana — AFRAME (Kawasan Selatan Timur, X=24, Z=-42, berjarak 48m dari Chalet)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.AFrame,
            new Vector3(24f, 0f, -42f), Quaternion.Euler(0, -20f, 0), village.transform, "House_AFrame_Singgah");

        // 9. Pondok Dukun Mistis — MINKA (Lembah Zen Tenggara, X=54, Z=-36, berjarak 31m dari A-Frame)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Minka,
            new Vector3(54f, 0f, -36f), Quaternion.Euler(0, -45f, 0), village.transform, "House_Dukun_Minka");

        // 10. Gardu Pengawas Rimba — POHON (Kanopi Hutan Timur, X=68, Z=-12, berjarak 28m dari Minka)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Pohon,
            new Vector3(68f, 0f, -12f), Quaternion.Euler(0, -75f, 0), village.transform, "Treehouse_Pengawas");

        // 11. Bengkel Pandai Besi (Kawasan Pengrajin Timur, X=36, Z=-2, berjarak 33m dari Rumah Pohon)
        var smith = BuildModularHouse(village.transform, "House_PandaiBesi",
            new Vector3(36f, 0f, -2f), -90f, "wood", false, false);
        if (smith)
        {
            InstantiateArtModel("EA03_Prop_Tool_Stump_02a", new Vector3(32f, 0f, -2f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Tool_Pick_01a", new Vector3(32.3f, 0.4f, -1.8f), Quaternion.Euler(45, 0, 0), Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Forester_CutOutTrees_03", new Vector3(34f, 0f, 3f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        }

        // 12. Gudang Logistik Pandai Besi (X=42, Z=10, komplek bengkel timur)
        BuildModularHouse(village.transform, "Storage_PandaiBesi",
            new Vector3(42f, 0f, 10f), -90f, "wood", false, false, small: true);

        // 13. Rumah Warga Pemburu — CABIN (Hutan Utara Timur, X=38, Z=32, berjarak 34m dari Pandai Besi)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
            new Vector3(38f, 0f, 32f), Quaternion.Euler(0, -135f, 0), village.transform, "House_WargaUtara_Cabin");

        // 14. Villa Kayu Modern — MODERN (Paviliun Kaca Timur Jauh, X=66, Z=26, berjarak 29m dari Cabin)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Modern,
            new Vector3(66f, 0f, 26f), Quaternion.Euler(0, -110f, 0), village.transform, "Villa_Modern");
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
    //  6. NPC POPULATION (STYLIZED 3D CHIBI GAME-ART DENGAN PERGERAKAN MANDIRI)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupNPCs()
    {
        GameObject npcRoot = GameObject.Find("--- NPCs ---");
        if (npcRoot) Object.DestroyImmediate(npcRoot);
        npcRoot = new GameObject("--- NPCs ---");

        var npcConfigs = new (
            string npcName,
            ProceduralNpcBuilder.NpcStyle style,
            Vector3 startPos,
            float yRot,
            string greeting,
            Vector3[] route
        )[]
        {
            // 1. Kepala Desa / Saudagar Terkemuka (Merchant: Baret beludru, jubah merah emas, ransel ekspedisi)
            ("NPC_KepalaDesa",
             ProceduralNpcBuilder.NpcStyle.Merchant,
             new Vector3(-38f, 0f, 26f), 135f,
             "Selamat datang, petualang! Desa Eldara dalam bahaya besar. Saat matahari terbenam, pasukan monster akan menyerbu dari rimba kegelapan hutan. Persiapkan persenjataan dan dirikan menara panah!",
             new Vector3[] {
                 new Vector3(-38f, 0f, 26f), // Halaman Rumah Gadang
                 new Vector3(-12f, 0f, 16f), // Jalur Cabang Barat Laut
                 new Vector3(  0f, 0f, 22f), // Halaman Balai Desa
                 new Vector3(  6f, 0f,  4f), // Alun-Alun Pasar Timur
                 new Vector3( -5f, 0f,  5f)  // Bangku Alun-Alun
             }),

            // 2. Petani Ladang (Farmer: Caping jerami lebar, rompi kerja, kumis melengkung, & garpu rumput)
            ("NPC_Petani",
             ProceduralNpcBuilder.NpcStyle.Farmer,
             new Vector3(-34f, 0f, -8f), 75f,
             "Aduh gawat, kawanan serigala buas kemarin malam merusak pagar gandumku! Tolong tebang balok kayu di tepi hutan rimba untuk memperbaiki lumbung kami.",
             new Vector3[] {
                 new Vector3(-34f, 0f, -8f), // Depan Rumah Panggung
                 new Vector3(-48f, 0f, -16f), // Ladang Gandum
                 new Vector3(-62f, 0f, -22f), // Depan Lumbung Barn Gandum
                 new Vector3(-36f, 0f, -4f)  // Pinggir Jalan Setapak Barat
             }),

            // 3. Pandai Besi & Ahli Senjata (Guard/Smith: Zirah baja tempa, sabuk kulit, & pedang baja)
            ("NPC_PandaiBesi",
             ProceduralNpcBuilder.NpcStyle.Guard,
             new Vector3(32f, 0f, -2f), -90f,
             "Salam, ksatria! Aku butuh bijih besi murni dan batu keras dari area tambang timur. Bawakan padaku, dan aku akan merakit menara panah yang kokoh!",
             new Vector3[] {
                 new Vector3(32f, 0f, -2f), // Depan Paron Tempa Bengkel
                 new Vector3(40f, 0f,  8f), // Gudang Logistik Besi
                 new Vector3(30f, 0f,  6f), // Tumpukan Balok Kayu & Bijih
                 new Vector3(34f, 0f, -6f)  // Pinggir Jalan Setapak Timur
             }),

            // 4. Dukun Mistis Pelindung Segel (Wizard: Topi kerucut sihir melengkung, janggut putih panjang, & tongkat kristal cyan berpendar)
            ("NPC_Dukun",
             ProceduralNpcBuilder.NpcStyle.Wizard,
             new Vector3(50f, 0f, -32f), -45f,
             "Altar Segel Kuno di utara berdenyut dengan energi gelap... Jika kamu berani mengaktifkannya saat malam tiba, Raja Monster akan terpanggil. Pastikan menara bentengmu sudah siap!",
             new Vector3[] {
                 new Vector3(50f, 0f, -32f), // Teras Minka Jepang
                 new Vector3(40f, 0f, -24f), // Lembah Zen Meditasi
                 new Vector3(25f, 0f, -20f), // Jalur Cabang Tenggara
                 new Vector3(52f, 0f, -38f)  // Lentera Mistis Minka
             }),

            // 5. Gadis Desa Ramah (Woman: Gaun zamrud, kepang rambut panjang di dada, celemek putih, & tas selempang)
            ("NPC_GadisDesa",
             ProceduralNpcBuilder.NpcStyle.Woman,
             new Vector3(-20f, 0f, -38f), 20f,
             "Halo petualang! Udara di desa Eldara hari ini begitu sejuk dan damai. Berhati-hatilah saat senja menjelang ya, kabut hutan sering membawa bahaya tak terduga!",
             new Vector3[] {
                 new Vector3(-20f, 0f, -38f), // Halaman Depan Chalet
                 new Vector3(-40f, 0f, -42f), // Tepi Kolam Danau
                 new Vector3( -2f, 0f, -26f), // Jalan Setapak Selatan
                 new Vector3( 18f, 0f, -36f)  // Halaman Depan A-Frame
             }),

            // 6. Kapten Penjaga Benteng (Guard: Helm baja bermahkota bulu merah, jubah perang, & pedang ksatria)
            ("NPC_PenjagaBenteng",
             ProceduralNpcBuilder.NpcStyle.Guard,
             new Vector3(-4f, 0f, -48f), 0f,
             "Berhenti! Aku kapten regu pengawal benteng Eldara. Kami bertugas mengawasi gerbang selatan ini dari serbuan kawanan monster malam hari!",
             new Vector3[] {
                 new Vector3(-4f, 0f, -48f), // Pos Menara Barat Gerbang
                 new Vector3( 4f, 0f, -48f), // Pos Menara Timur Gerbang
                 new Vector3( 0f, 0f, -42f), // Tengah Jalan Masuk Selatan
                 new Vector3( 0f, 0f, -52f)  // Pemeriksaan Pintu Benteng Luar
             })
        };

        foreach (var (npcName, style, startPos, yRot, greeting, route) in npcConfigs)
        {
            var npcGo = ProceduralNpcBuilder.BuildNpc(
                style,
                startPos,
                Quaternion.Euler(0, yRot, 0),
                npcRoot.transform,
                npcName
            );

            // Dialogue
            var npcDlg = npcGo.GetComponent<NpcDialogue>() ?? npcGo.AddComponent<NpcDialogue>();
            CreateSimpleDialogue(npcName, greeting, npcDlg);

            // Mover & Route
            var mover = npcGo.GetComponent<NpcMover>();
            if (mover)
            {
                mover.SetWaypoints(new System.Collections.Generic.List<Vector3>(route));
                mover.moveSpeed = 1.35f;
                mover.waitTimeAtWaypoint = Random.Range(3.5f, 5.5f);
            }
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

        // 8 Titik Menara Pengawas Kayu Benteng (Watchtowers Lapang Keliling Desa)
        var towerSlots = new (Vector3 pos, string name)[]
        {
            // Gerbang Masuk Selatan (Twin Watchtowers)
            (new Vector3(-6f, 0f, -54f), "Tower_Gate_West"),
            (new Vector3( 6f, 0f, -54f), "Tower_Gate_East"),

            // 4 Penjuru Sudut Benteng Utama (Sudut Pagar X=±80, Z=-54 s/d 52)
            (new Vector3(-80f, 0f,  52f), "Tower_NorthWest"),
            (new Vector3( 80f, 0f,  52f), "Tower_NorthEast"),
            (new Vector3(-80f, 0f, -54f), "Tower_SouthWest"),
            (new Vector3( 80f, 0f, -54f), "Tower_SouthEast"),

            // Sisi Sayap Tengah
            (new Vector3(-80f, 0f,   0f), "Tower_WestFlank"),
            (new Vector3( 80f, 0f,   0f), "Tower_EastFlank"),
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

        // Pagar Kayu Benteng Keliling (Perimeter Palisade Skala Besar 160x106m)
        string fenceFbx = "EA03_Village_Fence_01a";
        // Sisi Selatan (Z = -54, dengan celah gerbang utama di tengah X = -4.5 s/d 4.5)
        for (float x = -80f; x <= 80f; x += 3.2f)
        {
            if (Mathf.Abs(x) < 5f) continue;
            InstantiateArtModel(fenceFbx, new Vector3(x, 0, -54f), Quaternion.Euler(0, 180f, 0), Vector3.one, towerRoot.transform, slavicMat);
        }
        // Sisi Utara (Z = 52, dengan celah menuju Altar Kuno)
        for (float x = -80f; x <= 80f; x += 3.2f)
        {
            if (Mathf.Abs(x) < 4f) continue; // Celah menuju Altar
            InstantiateArtModel(fenceFbx, new Vector3(x, 0, 52f), Quaternion.identity, Vector3.one, towerRoot.transform, slavicMat);
        }
        // Sisi Timur (X = 80)
        for (float z = -54f; z <= 52f; z += 3.2f)
        {
            InstantiateArtModel(fenceFbx, new Vector3(80f, 0, z), Quaternion.Euler(0, 90f, 0), Vector3.one, towerRoot.transform, slavicMat);
        }
        // Sisi Barat (X = -80)
        for (float z = -54f; z <= 52f; z += 3.2f)
        {
            InstantiateArtModel(fenceFbx, new Vector3(-80f, 0, z), Quaternion.Euler(0, -90f, 0), Vector3.one, towerRoot.transform, slavicMat);
        }

        // Spike Traps di Jalur Masuk Gerbang Utama
        var trapPositions = new Vector3[]
        {
            new Vector3( 0f, 0.05f, -50f),
            new Vector3(-2.5f, 0.05f, -55f),
            new Vector3( 2.5f, 0.05f, -55f),
            new Vector3( 0f, 0.05f, -60f),
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

        RealisticTreeBuilder.ClearCache();

        var barkMat       = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var birchBarkMat  = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: true);
        var summerLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var springLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("SPRING");
        var autumnLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("AUTUMN");

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
            // Baris asli — pojok & tengah desa
            new Vector3( 10f, 0f, 12f), new Vector3(-12f, 0f, 10f),
            new Vector3( 12f, 0f, -8f), new Vector3(-10f, 0f, -8f),
            new Vector3(-4f, 0f, 12f),  new Vector3( 4f, 0f, 12f),
            new Vector3(-14f, 0f, -2f), new Vector3( 14f, 0f, -2f),
            new Vector3( 0f, 0f, -14f), new Vector3( 24f, 0f, 22f),
            new Vector3(-26f, 0f, 20f), new Vector3(-14f, 0f, -12f),
            // Tambahan: pinggir jalan utara-selatan
            new Vector3( 6f, 0f, 20f),  new Vector3(-6f, 0f, 20f),
            new Vector3( 6f, 0f,-20f),  new Vector3(-6f, 0f,-20f),
            // Tambahan: sisi timur-barat
            new Vector3( 22f, 0f,  4f), new Vector3(-22f, 0f,  4f),
            new Vector3( 22f, 0f,-14f), new Vector3(-22f, 0f,-14f),
            // Tambahan: sudut-sudut taman desa
            new Vector3( 18f, 0f, 14f), new Vector3(-18f, 0f, 14f),
            new Vector3( 20f, 0f,-20f), new Vector3(-20f, 0f,-20f),
            // Tambahan: pohon hias di alun-alun & jalur masuk
            new Vector3(  0f, 0f, 26f), new Vector3(  0f, 0f,-26f),
            new Vector3( 28f, 0f,  0f), new Vector3(-28f, 0f,  0f),
            new Vector3(-10f, 0f, 30f), new Vector3( 10f, 0f, 30f),
        };

        int innerIndex = 0;
        foreach (var pos in innerTrees)
        {
            innerIndex++;
            float scale = Random.Range(1.0f, 1.3f);
            GameObject treeObj;

            // Variasi pohon desa: Pohon Ek Rimbun, Pohon Birch Indah, dan Pohon Ek Musim Gugur
            if (innerIndex % 4 == 0)
            {
                treeObj = RealisticTreeBuilder.BuildAutumnOakTree(pos, scale, natureRoot.transform, barkMat, autumnLeafMat);
            }
            else if (innerIndex % 3 == 0)
            {
                treeObj = RealisticTreeBuilder.BuildBirchTree(pos, scale, natureRoot.transform, birchBarkMat, springLeafMat);
            }
            else
            {
                treeObj = RealisticTreeBuilder.BuildOakTree(pos, scale, natureRoot.transform, barkMat, summerLeafMat);
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

        // 2. Hutan Rimba Luar (Di Luar Garis Pagar Benteng Keliling Desa)
        // Dikelompokkan di 4 zona alam eksterior: Rimba Utara (Altar), Lembah Selatan, Hutan Barat, & Tambang Timur
        var outerTreeZones = new System.Collections.Generic.List<Vector3>();

        // Zona Rimba Utara (Z = 56 s/d 220, X = -220 s/d 220, sisakan celah jalan altar |X| < 6)
        for (int i = 0; i < 120; i++)
        {
            float x = Random.Range(-220f, 220f);
            if (Mathf.Abs(x) < 7f) continue;
            float z = Random.Range(56f, 220f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }

        // Zona Rimba Selatan (Z = -220 s/d -58, X = -220 s/d 220, sisakan celah gerbang selatan |X| < 7)
        for (int i = 0; i < 110; i++)
        {
            float x = Random.Range(-220f, 220f);
            if (Mathf.Abs(x) < 7f) continue;
            float z = Random.Range(-220f, -58f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }

        // Zona Hutan Barat (X = -220 s/d -56, Z = -100 s/d 100) — lebih tebal
        for (int i = 0; i < 90; i++)
        {
            float x = Random.Range(-220f, -56f);
            float z = Random.Range(-100f, 100f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }

        // Zona Hutan Timur & Tebing Tambang (X = 56 s/d 220, Z = -100 s/d 100) — lebih tebal
        for (int i = 0; i < 90; i++)
        {
            float x = Random.Range(56f, 220f);
            float z = Random.Range(-100f, 100f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }

        // Zona Peralihan 4 sudut pojok (lebih lebat)
        for (int i = 0; i < 40; i++)
        {
            float x = Random.Range(-220f, -60f);
            float z = Random.Range(56f, 220f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }
        for (int i = 0; i < 40; i++)
        {
            float x = Random.Range(60f, 220f);
            float z = Random.Range(56f, 220f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }
        for (int i = 0; i < 40; i++)
        {
            float x = Random.Range(-220f, -60f);
            float z = Random.Range(-220f, -56f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }
        for (int i = 0; i < 40; i++)
        {
            float x = Random.Range(60f, 220f);
            float z = Random.Range(-220f, -56f);
            outerTreeZones.Add(new Vector3(x, 0f, z));
        }

        int treeIdx = 0;
        foreach (var pos in outerTreeZones)
        {
            treeIdx++;
            float scale = Random.Range(0.95f, 1.45f);

            // Kombinasi Pohon Pinus Pegunungan, Pohon Ek Lebat, & Ek Keemasan
            if (treeIdx % 3 == 0)
            {
                RealisticTreeBuilder.BuildPineTree(pos, scale, natureRoot.transform, barkMat, summerLeafMat);
            }
            else if (treeIdx % 5 == 0)
            {
                RealisticTreeBuilder.BuildAutumnOakTree(pos, scale, natureRoot.transform, barkMat, autumnLeafMat);
            }
            else
            {
                RealisticTreeBuilder.BuildOakTree(pos, scale, natureRoot.transform, barkMat, summerLeafMat);
            }

            // Semak belukar di lantai hutan luar
            if (treeIdx % 2 == 0)
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

            // Bongkahan batu tebing alami di hutan luar
            if (treeIdx % 3 == 0)
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
            // Tambahan: sisi kanan-kiri jalan utama
            new Vector3( 4.5f, 0f, 18f), new Vector3(-4.5f, 0f, 18f),
            new Vector3( 4.5f, 0f,-18f), new Vector3(-4.5f, 0f,-18f),
            new Vector3( 15f,  0f,  4f), new Vector3( 20f,  0f,  4f),
            new Vector3(-15f,  0f,  4f), new Vector3(-20f,  0f,  4f),
        };
        foreach (var pt in pathwayPoints)
        {
            RealisticGrassBuilder.BuildGrassClump(pt, 8, 2.0f, grassRoot.transform, grassMat);
        }

        // 2. Pekarangan & Halaman Sekitar Bangunan Desa — lebih rapat
        Vector3[] yardCenters = {
            new Vector3(-16f, 0f,  16f), new Vector3(-20f, 0f,  12f), // Halaman Rumah Warga 1
            new Vector3( 16f, 0f,  18f), new Vector3( 20f, 0f,  12f), // Halaman Rumah Warga 2
            new Vector3(-16f, 0f,  -4f), new Vector3(-22f, 0f,  -8f), // Halaman Pandai Besi
            new Vector3( 18f, 0f,  -4f), new Vector3( 22f, 0f,  -8f), // Halaman Rumah Pedagang
            new Vector3(  0f, 0f, -16f), new Vector3(  4f, 0f, -22f), // Halaman Selatan Dekat Gerbang
            new Vector3(-12f, 0f,  24f), new Vector3( -8f, 0f,  28f), // Halaman Utara Balai Desa
            new Vector3( 14f, 0f,  25f), new Vector3(  8f, 0f,  28f), // Halaman Kebun Desa
            new Vector3( 26f, 0f, -16f), new Vector3(-26f, 0f, -16f), // Sudut luar tenggara & barat daya
            new Vector3( 26f, 0f,  26f), new Vector3(-26f, 0f,  26f), // Sudut luar timur laut & barat laut
        };
        foreach (var yc in yardCenters)
        {
            RealisticGrassBuilder.BuildGrassClump(yc, 20, 6.0f, grassRoot.transform, grassMat);
        }

        // 3. Pangkal Pohon-Pohon Desa & Semak Alami — sinkron dengan innerTrees di WILDERNESS
        var innerTrees = new Vector3[]
        {
            new Vector3( 10f, 0f, 12f), new Vector3(-12f, 0f, 10f),
            new Vector3( 12f, 0f, -8f), new Vector3(-10f, 0f, -8f),
            new Vector3(-4f, 0f, 12f),  new Vector3( 4f, 0f, 12f),
            new Vector3(-14f, 0f, -2f), new Vector3( 14f, 0f, -2f),
            new Vector3( 0f, 0f, -14f), new Vector3( 24f, 0f, 22f),
            new Vector3(-26f, 0f, 20f), new Vector3(-14f, 0f, -12f),
            // Tambahan sesuai pohon baru yang ditanam
            new Vector3( 6f, 0f, 20f),  new Vector3(-6f, 0f, 20f),
            new Vector3( 6f, 0f,-20f),  new Vector3(-6f, 0f,-20f),
            new Vector3( 22f, 0f,  4f), new Vector3(-22f, 0f,  4f),
            new Vector3( 22f, 0f,-14f), new Vector3(-22f, 0f,-14f),
            new Vector3( 18f, 0f, 14f), new Vector3(-18f, 0f, 14f),
            new Vector3( 20f, 0f,-20f), new Vector3(-20f, 0f,-20f),
            new Vector3(  0f, 0f, 26f), new Vector3(  0f, 0f,-26f),
            new Vector3( 28f, 0f,  0f), new Vector3(-28f, 0f,  0f),
        };
        foreach (var tp in innerTrees)
        {
            RealisticGrassBuilder.BuildGrassClump(tp, 12, 3.5f, grassRoot.transform, grassMat);
        }

        // 4. Sepanjang Garis Perimeter Pagar Kayu & Menara Pertahanan (Radius ~32-36m)
        int fenceClumps = 56; // jauh lebih rapat dari sebelumnya (36 → 56)
        for (int i = 0; i < fenceClumps; i++)
        {
            float ang = (i / (float)fenceClumps) * 360f * Mathf.Deg2Rad;
            float deg = ang * Mathf.Rad2Deg;
            if (deg > 255f && deg < 285f) continue; // Celah gerbang masuk selatan

            float r = Random.Range(29f, 36f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            RealisticGrassBuilder.BuildGrassClump(pos, 8, 3.0f, grassRoot.transform, grassMat);
        }

        // 5. Hamparan Padang Rumput Liar Luar Desa — lebah rapat, cincin ganda
        // Cincin dalam (36-60m)
        int meadowInner = 72;
        for (int i = 0; i < meadowInner; i++)
        {
            float ang = (i / (float)meadowInner) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(36f, 60f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            RealisticGrassBuilder.BuildGrassClump(pos, 10, 4.5f, grassRoot.transform, grassMat);
        }
        // Cincin luar (60-100m) — semakin lebat di dekat hutan
        int meadowOuter = 90;
        for (int i = 0; i < meadowOuter; i++)
        {
            float ang = (i / (float)meadowOuter) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(60f, 105f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            RealisticGrassBuilder.BuildGrassClump(pos, 12, 5.5f, grassRoot.transform, grassMat);
        }

        // 6. Tambalan Rumput Acak di Dalam Desa (mengisi area kosong antar bangunan)
        for (int i = 0; i < 50; i++)
        {
            float x = Random.Range(-28f, 28f);
            float z = Random.Range(-28f, 28f);
            // Jangan taruh di tengah alun-alun (radius 5m)
            if (Mathf.Sqrt(x * x + z * z) < 5f) continue;
            Vector3 pos = new Vector3(x, 0f, z);
            RealisticGrassBuilder.BuildGrassClump(pos, 6, 2.0f, grassRoot.transform, grassMat);
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
            "Tekan E untuk memecah Batu", new Vector3(48f, 0f, -6f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f), slavicMat, new Color(0.55f, 0.55f, 0.55f));
        PlaceArtResource(itemsRoot.transform, "Stone_Rock_2", "EA03_Environment_Rock_Head_01b", "stone",
            "Tekan E untuk memecah Batu", new Vector3(-28f, 0f, -35f), Quaternion.Euler(0, 50f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.55f, 0.55f, 0.55f));

        // ── D. KOMPLEKS ALTAR SEGEL KUNO (ANCIENT STONE CIRCLE DI UTARA DESA) ──
        Vector3 altarCenter = new Vector3(0f, 0f, 75f);

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
        // Bersihkan objek ikan darat & fauna lama jika ada
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var fishToDestroy = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var t in allTransforms)
        {
            if (!t) continue;
            string n = t.name.ToLower();
            if (n.Contains("fish") || n.Contains("piranha") || t.GetComponent<FishController>())
            {
                Transform curr = t;
                while (curr.parent != null && curr.parent.name != "--- FAUNA & ANIMALS ---" && 
                      (curr.parent.name.ToLower().Contains("fish") || curr.parent.name.ToLower().Contains("piranha")))
                {
                    curr = curr.parent;
                }
                fishToDestroy.Add(curr.gameObject);
            }
        }
        foreach (var go in fishToDestroy)
        {
            if (go) Object.DestroyImmediate(go);
        }

        GameObject faunaRoot = GameObject.Find("--- FAUNA & ANIMALS ---");
        if (faunaRoot) Object.DestroyImmediate(faunaRoot);

        faunaRoot = new GameObject("--- FAUNA & ANIMALS ---");

        // 1. Milo - Anjing Kintamani Asli Penjaga Desa (Di dekat Balai Desa & Alun-Alun)
        AnimalAssetSetup.SpawnDog(new Vector3(4.5f, 0f, 6.5f), Quaternion.Euler(0, 190f, 0), faunaRoot.transform,
            RealisticDogBuilder.DogBreed.Kintamani, "Milo",
            "Guk! Milo si Anjing Kintamani mengibaskan ekor melengkungnya riang dan menemanimu berjalan-jalan!");

        // 1b. Bobi - Anjing Golden Retriever Ramah (Di pelataran kebun gandum & rumah panggung petani)
        AnimalAssetSetup.SpawnDog(new Vector3(-34f, 0f, -6f), Quaternion.Euler(0, 75f, 0), faunaRoot.transform,
            RealisticDogBuilder.DogBreed.GoldenRetriever, "Bobi",
            "Guk guk! Bobi si Golden Retriever menyapa ramah dengan telinga turun dan bulu emas berkilau!");

        // 1c. Bruno - Anjing German Shepherd Penjaga Benteng (Di pos gerbang selatan luar)
        AnimalAssetSetup.SpawnDog(new Vector3(-5f, 0f, -50f), Quaternion.Euler(0, 20f, 0), faunaRoot.transform,
            RealisticDogBuilder.DogBreed.GermanShepherd, "Bruno",
            "Guk! Bruno si German Shepherd berdiri tegap dan waspada menjaga gerbang utama desa!");

        // 1d. Kiko - Anjing Dachshund Lincah (Di teras Rumah Panggung Selatan)
        AnimalAssetSetup.SpawnDog(new Vector3(-20f, 0f, -46f), Quaternion.Euler(0, -110f, 0), faunaRoot.transform,
            RealisticDogBuilder.DogBreed.Dachshund, "Kiko",
            "Guk! Kiko si Dachshund bertubuh panjang berlari-lari kecil menyambutmu!");

        // 2. Luna - Kucing Abu-Abu Desa (Di teras Log Cabin Warga Pemburu)
        AnimalAssetSetup.SpawnCat(new Vector3(36f, 0f, 32f), Quaternion.Euler(0, 45f, 0), faunaRoot.transform,
            "Luna", "Meow~ Luna mendengkur manja dan mengusapkan badannya ke kakimu.", false);

        // 3. Oliver - Kucing Oranye Pasar (Di dekat gerobak & peti kayu alun-alun)
        AnimalAssetSetup.SpawnCat(new Vector3(8f, 0f, 2f), Quaternion.Euler(0, -60f, 0), faunaRoot.transform,
            "Oliver", "Meow! Oliver si kucing oranye melompat manja dan menatapmu ramah.", true);

        // 4. Kawanan Serigala Rimba (Di hutan lebat luar pagar desa)
        // Alpha Wolf: Hutan Utara dekat Altar Kuno
        AnimalAssetSetup.SpawnWolf(new Vector3(2f, 0f, 72f), Quaternion.Euler(0, 180f, 0), faunaRoot.transform, "Serigala Rimba Alpha");
        // Beta Wolf 1: Hutan Barat
        AnimalAssetSetup.SpawnWolf(new Vector3(-95f, 0f, 20f), Quaternion.Euler(0, 90f, 0), faunaRoot.transform, "Serigala Rimba Barat");
        // Beta Wolf 2: Hutan Timur
        AnimalAssetSetup.SpawnWolf(new Vector3(95f, 0f, 20f), Quaternion.Euler(0, -90f, 0), faunaRoot.transform, "Serigala Rimba Timur");

        // 5. Aquila - Elang Perkasa Penjaga Langit (Melayang tinggi melingkar di angkasa desa)
        AnimalAssetSetup.SpawnEagle(new Vector3(0f, 28f, 0f), faunaRoot.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9c. EKSPANSI PETA — 4 ZONA BARU (Total dunia 500x500m)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Membangun 4 zona dunia baru di luar hutan desa:
    /// - UTARA  (Z=160-220): Desa Pegunungan, Kuil Batu Kuno, Jembatan Kabut
    /// - SELATAN (Z=-160 s/d -220): Rawa Misterius, Perkemahan Goblin Bayangan
    /// - BARAT  (X=-155 s/d -220): Desa Nelayan Tepi Sungai, Dermaga, Perahu Rakit
    /// - TIMUR  (X=155-220): Tebing Tambang Kristal, Gua Gelap, Pos Penjaga
    /// Juga menambah jalan antar-zona, NPC, resource point, spawn musuh,
    /// serigala liar, dan rumput tambahan di masing-masing zona.
    /// </summary>
    static void SetupExpandedWorld(Material slavicMat, Material grassMat)
    {
        GameObject expandRoot = GameObject.Find("--- EXPANSION ZONES ---");
        if (expandRoot) Object.DestroyImmediate(expandRoot);
        expandRoot = new GameObject("--- EXPANSION ZONES ---");

        var barkMat       = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var birchBarkMat  = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: true);
        var summerLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var autumnLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("AUTUMN");

        // ── JALAN PENGHUBUNG KE 4 ZONA BARU ──
        // Jalan Utara (Z=56 ke Z=160)
        for (float z = 56f; z <= 160f; z += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(0, 0.02f, z),
                Quaternion.identity, new Vector3(1.2f, 1f, 1.2f), expandRoot.transform, slavicMat);
        // Jalan Selatan (Z=-56 ke Z=-160)
        for (float z = -56f; z >= -160f; z -= 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(0, 0.02f, z),
                Quaternion.identity, new Vector3(1.2f, 1f, 1.2f), expandRoot.transform, slavicMat);
        // Jalan Barat (X=-80 ke X=-160, Z=0)
        for (float x = -80f; x >= -160f; x -= 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(x, 0.02f, 0),
                Quaternion.Euler(0, 90f, 0), new Vector3(1.2f, 1f, 1.2f), expandRoot.transform, slavicMat);
        // Jalan Timur (X=80 ke X=160, Z=0)
        for (float x = 80f; x <= 160f; x += 4.5f)
            InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(x, 0.02f, 0),
                Quaternion.Euler(0, 90f, 0), new Vector3(1.2f, 1f, 1.2f), expandRoot.transform, slavicMat);

        // ════════════════════════════════════════════════════════════════
        // ZONA UTARA — DESA PEGUNUNGAN ELDARA ATAS (Z=160-220)
        // ════════════════════════════════════════════════════════════════
        {
            var northRoot = new GameObject("Zone_North_MountainVillage");
            northRoot.transform.SetParent(expandRoot.transform);

            // Gerbang Pegunungan — Dua pilar batu tinggi
            var pillarL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillarL.name = "GatePillar_NorthLeft";
            pillarL.transform.SetParent(northRoot.transform);
            pillarL.transform.position = new Vector3(-4f, 3f, 158f);
            pillarL.transform.localScale = new Vector3(1.2f, 6f, 1.2f);
            ApplyColor(pillarL, new Color(0.5f, 0.47f, 0.44f));
            var pillarR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillarR.name = "GatePillar_NorthRight";
            pillarR.transform.SetParent(northRoot.transform);
            pillarR.transform.position = new Vector3(4f, 3f, 158f);
            pillarR.transform.localScale = new Vector3(1.2f, 6f, 1.2f);
            ApplyColor(pillarR, new Color(0.5f, 0.47f, 0.44f));

            // Kuil Batu Kuno di puncak (Z=200)
            var temple = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temple.name = "TempleBase_North";
            temple.transform.SetParent(northRoot.transform);
            temple.transform.position = new Vector3(0f, 0.4f, 200f);
            temple.transform.localScale = new Vector3(16f, 0.8f, 12f);
            ApplyColor(temple, new Color(0.58f, 0.52f, 0.46f));

            // 6 pilar kuil (barisan depan & belakang)
            for (int p = 0; p < 6; p++)
            {
                float px = Mathf.Lerp(-6f, 6f, p / 5f);
                var kp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                kp.name = $"TemplePillar_N_{p}_front";
                kp.transform.SetParent(northRoot.transform);
                kp.transform.position = new Vector3(px, 2.5f, 194f);
                kp.transform.localScale = new Vector3(0.9f, 5f, 0.9f);
                ApplyColor(kp, new Color(0.62f, 0.56f, 0.5f));
                Object.DestroyImmediate(kp.GetComponent<Collider>());
                var kp2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                kp2.name = $"TemplePillar_N_{p}_back";
                kp2.transform.SetParent(northRoot.transform);
                kp2.transform.position = new Vector3(px, 2.5f, 206f);
                kp2.transform.localScale = new Vector3(0.9f, 5f, 0.9f);
                ApplyColor(kp2, new Color(0.62f, 0.56f, 0.5f));
                Object.DestroyImmediate(kp2.GetComponent<Collider>());
            }

            // Atap kuil & cahaya mistis
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "TempleRoof_North";
            roof.transform.SetParent(northRoot.transform);
            roof.transform.position = new Vector3(0f, 5.5f, 200f);
            roof.transform.localScale = new Vector3(17f, 0.6f, 13f);
            ApplyColor(roof, new Color(0.35f, 0.28f, 0.22f));
            AddPorchLantern(northRoot.transform, new Vector3(0, 7f, 200f), new Color(0.5f, 0.8f, 1f), 20f);

            // 4 Rumah Pegunungan
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
                new Vector3(-18f, 0f, 165f), Quaternion.Euler(0, 90f, 0), northRoot.transform, "MtnHouse_West_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Chalet,
                new Vector3( 18f, 0f, 165f), Quaternion.Euler(0, -90f, 0), northRoot.transform, "MtnHouse_East_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.AFrame,
                new Vector3(-22f, 0f, 182f), Quaternion.Euler(0, 70f, 0), northRoot.transform, "MtnHouse_West_2");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
                new Vector3( 22f, 0f, 182f), Quaternion.Euler(0, -70f, 0), northRoot.transform, "MtnHouse_East_2");

            // Jembatan Kabut
            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "Bridge_MistNorth";
            bridge.transform.SetParent(northRoot.transform);
            bridge.transform.position = new Vector3(0f, 0.15f, 152f);
            bridge.transform.localScale = new Vector3(4f, 0.3f, 10f);
            ApplyColor(bridge, new Color(0.4f, 0.32f, 0.22f));

            // NPC Penjaga Kuil
            var npcKuil = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Wizard,
                new Vector3(-4f, 0f, 198f), Quaternion.Euler(0, 180f, 0),
                northRoot.transform, "NPC_PenjagaKuil");
            var dlgKuil = npcKuil.GetComponent<NpcDialogue>() ?? npcKuil.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PenjagaKuil",
                "Selamat datang di Kuil Batu Kuno Eldara Atas, pengembara. " +
                "Tempat ini menyimpan jimat pelindung desa dari zaman para leluhur. " +
                "Jangan ganggu ketenangan kuil ini, atau laknat pegunungan akan menimpamu!",
                dlgKuil);

            // NPC Pedagang Gunung
            var npcPdg = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Merchant,
                new Vector3(16f, 0f, 168f), Quaternion.Euler(0, -120f, 0),
                northRoot.transform, "NPC_PedagangGunungNorth");
            var dlgPdg = npcPdg.GetComponent<NpcDialogue>() ?? npcPdg.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PedagangGunungNorth",
                "Hei, traveler! Aku datang dari puncak gunung membawa kristal biru langka. " +
                "Kalau kamu bisa bawakan aku kayu keras dari hutan barat, " +
                "aku akan tukar dengan batu kristal yang bisa memperkuat menara pertahananmu!",
                dlgPdg);

            // Resource: Kristal Biru
            PlaceArtResource(northRoot.transform, "CrystalBlue_1",
                "EA03_Environment_Rock_Head_01c", "crystal",
                "Tekan E untuk mengambil Kristal Biru",
                new Vector3(-8f, 0f, 195f), Quaternion.Euler(0, 30f, 0),
                new Vector3(1.2f, 1.4f, 1.2f), slavicMat, new Color(0.3f, 0.5f, 0.9f));
            PlaceArtResource(northRoot.transform, "CrystalBlue_2",
                "EA03_Environment_Rock_Head_01b", "crystal",
                "Tekan E untuk mengambil Kristal Biru",
                new Vector3(10f, 0f, 192f), Quaternion.Euler(0, 80f, 0),
                new Vector3(1.0f, 1.3f, 1.0f), slavicMat, new Color(0.3f, 0.5f, 0.9f));

            // Pohon & rumput pegunungan
            for (int i = 0; i < 35; i++)
            {
                float tx = Random.Range(-40f, 40f);
                float tz = Random.Range(160f, 215f);
                if (Mathf.Abs(tx) < 10f && tz > 192f && tz < 210f) continue;
                float ts = Random.Range(0.9f, 1.4f);
                if (i % 3 == 0)
                    RealisticTreeBuilder.BuildPineTree(new Vector3(tx, 0, tz), ts, northRoot.transform, barkMat, summerLeafMat);
                else
                    RealisticTreeBuilder.BuildOakTree(new Vector3(tx, 0, tz), ts, northRoot.transform, barkMat, summerLeafMat);
            }
            if (grassMat)
                for (int i = 0; i < 30; i++)
                {
                    float gx = Random.Range(-35f, 35f);
                    float gz = Random.Range(158f, 215f);
                    RealisticGrassBuilder.BuildGrassClump(new Vector3(gx, 0, gz), 10, 4f, northRoot.transform, grassMat);
                }

            // Serigala gunung
            AnimalAssetSetup.SpawnWolf(new Vector3(-15f, 0f, 175f), Quaternion.Euler(0, 45f, 0), northRoot.transform, "Serigala Gunung Barat");
            AnimalAssetSetup.SpawnWolf(new Vector3( 20f, 0f, 188f), Quaternion.Euler(0, -130f, 0), northRoot.transform, "Serigala Gunung Timur");
        }

        // ════════════════════════════════════════════════════════════════
        // ZONA SELATAN — RAWA MISTERIUS & PERKEMAHAN GOBLIN (Z=-155 s/d -220)
        // ════════════════════════════════════════════════════════════════
        {
            var southRoot = new GameObject("Zone_South_SwampCamp");
            southRoot.transform.SetParent(expandRoot.transform);

            // 3 Kolam Rawa gelap
            Vector3[] ponds = {
                new Vector3(-12f, 0f, -175f),
                new Vector3(  8f, 0f, -185f),
                new Vector3( -5f, 0f, -200f)
            };
            foreach (var pond in ponds)
            {
                var pondObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pondObj.name = "SwampPond";
                pondObj.transform.SetParent(southRoot.transform);
                pondObj.transform.position = pond + new Vector3(0, 0.03f, 0);
                pondObj.transform.localScale = new Vector3(Random.Range(6f, 10f), 0.1f, Random.Range(5f, 8f));
                ApplyColor(pondObj, new Color(0.15f, 0.22f, 0.14f));
                Object.DestroyImmediate(pondObj.GetComponent<Collider>());
                AddPorchLantern(southRoot.transform, pond + new Vector3(0, 0.5f, 0), new Color(0.3f, 0.7f, 0.2f), 8f);
            }

            // Api Unggun Perkemahan Goblin
            Vector3 campCenter = new Vector3(5f, 0f, -185f);
            var campfire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            campfire.name = "Campfire_Goblin";
            campfire.transform.SetParent(southRoot.transform);
            campfire.transform.position = campCenter + new Vector3(0, 0.3f, 0);
            campfire.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            ApplyColor(campfire, new Color(0.6f, 0.3f, 0.1f));
            AddPorchLantern(southRoot.transform, campCenter + new Vector3(0, 1f, 0), new Color(1f, 0.4f, 0.1f), 14f);

            // 4 Tenda goblin
            Vector3[] tentPos = {
                campCenter + new Vector3(-5f, 0f, -4f),
                campCenter + new Vector3( 6f, 0f, -3f),
                campCenter + new Vector3(-4f, 0f,  5f),
                campCenter + new Vector3( 5f, 0f,  5f),
            };
            for (int t = 0; t < tentPos.Length; t++)
            {
                var tent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tent.name = $"GoblinTent_{t + 1}";
                tent.transform.SetParent(southRoot.transform);
                tent.transform.position = tentPos[t] + new Vector3(0, 1.1f, 0);
                tent.transform.localScale = new Vector3(2.8f, 2.2f, 3f);
                tent.transform.rotation = Quaternion.Euler(0, t * 85f, 0);
                ApplyColor(tent, new Color(0.3f, 0.26f, 0.18f));
            }

            // Spawn musuh perkemahan
            var spawnS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawnS.name = "SpawnPoint_South_SwampCamp";
            spawnS.transform.SetParent(southRoot.transform);
            spawnS.transform.position = campCenter;
            spawnS.transform.localScale = new Vector3(2f, 0.3f, 2f);
            ApplyColor(spawnS, new Color(0.85f, 0.1f, 0.1f));
            Object.DestroyImmediate(spawnS.GetComponent<Collider>());

            // NPC Penyihir Rawa
            var npcRawa = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Wizard,
                new Vector3(-18f, 0f, -172f), Quaternion.Euler(0, 30f, 0),
                southRoot.transform, "NPC_PenyihirRawa");
            var dlgRawa = npcRawa.GetComponent<NpcDialogue>() ?? npcRawa.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PenyihirRawa",
                "Hssst... pengembara dungu! Kamu berani masuk ke Rawa Bayangan?! " +
                "Kawanan goblin telah merampas jimat perlindunganku. " +
                "Kalahkan Raja Perkemahan mereka dan bawa kembali jimatku, " +
                "atau rawa ini akan menelanmu perlahan-lahan...",
                dlgRawa);

            // Resource: Jamur Langka
            PlaceArtResource(southRoot.transform, "SwampShroom_1",
                "EA03_Nature_Bush_03b", "rare_herb",
                "Tekan E untuk memetik Jamur Langka",
                new Vector3(-8f, 0f, -178f), Quaternion.identity,
                Vector3.one * 0.7f, slavicMat, new Color(0.6f, 0.9f, 0.3f));

            // Pohon & rumput rawa
            for (int i = 0; i < 28; i++)
            {
                float tx = Random.Range(-40f, 40f);
                float tz = Random.Range(-215f, -158f);
                if (Vector3.Distance(new Vector3(tx, 0, tz), campCenter) < 12f) continue;
                RealisticTreeBuilder.BuildAutumnOakTree(new Vector3(tx, 0, tz),
                    Random.Range(0.85f, 1.3f), southRoot.transform, barkMat, autumnLeafMat);
            }
            if (grassMat)
                for (int i = 0; i < 25; i++)
                {
                    float gx = Random.Range(-38f, 38f);
                    float gz = Random.Range(-215f, -158f);
                    RealisticGrassBuilder.BuildGrassClump(new Vector3(gx, 0, gz), 8, 3.5f, southRoot.transform, grassMat);
                }
        }

        // ════════════════════════════════════════════════════════════════
        // ZONA BARAT — DESA NELAYAN TEPI SUNGAI (X=-155 s/d -220)
        // ════════════════════════════════════════════════════════════════
        {
            var westRoot = new GameObject("Zone_West_FishingVillage");
            westRoot.transform.SetParent(expandRoot.transform);

            // Sungai (strip biru di X≈-148)
            var river = GameObject.CreatePrimitive(PrimitiveType.Cube);
            river.name = "River_West";
            river.transform.SetParent(westRoot.transform);
            river.transform.position = new Vector3(-148f, 0.05f, 0f);
            river.transform.localScale = new Vector3(5f, 0.1f, 120f);
            ApplyColor(river, new Color(0.2f, 0.42f, 0.72f));
            Object.DestroyImmediate(river.GetComponent<Collider>());
            AddPorchLantern(westRoot.transform, new Vector3(-148f, 0.3f, 0f), new Color(0.4f, 0.7f, 1f), 25f);

            // Dermaga
            for (int p = 0; p < 6; p++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Dock_Plank_{p}";
                plank.transform.SetParent(westRoot.transform);
                plank.transform.position = new Vector3(-148f + p * 1.1f - 2f, 0.18f, 10f);
                plank.transform.localScale = new Vector3(1.0f, 0.15f, 2.5f);
                ApplyColor(plank, new Color(0.4f, 0.3f, 0.18f));
            }

            // 2 Perahu rakit
            var boat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boat.name = "FishingBoat_1";
            boat.transform.SetParent(westRoot.transform);
            boat.transform.position = new Vector3(-150f, 0.1f, 15f);
            boat.transform.localScale = new Vector3(2.5f, 0.6f, 5f);
            boat.transform.rotation = Quaternion.Euler(0, 15f, 0);
            ApplyColor(boat, new Color(0.45f, 0.3f, 0.18f));
            var boat2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boat2.name = "FishingBoat_2";
            boat2.transform.SetParent(westRoot.transform);
            boat2.transform.position = new Vector3(-150.5f, 0.1f, -8f);
            boat2.transform.localScale = new Vector3(2.2f, 0.55f, 4.5f);
            boat2.transform.rotation = Quaternion.Euler(0, -20f, 0);
            ApplyColor(boat2, new Color(0.4f, 0.28f, 0.16f));

            // 4 Rumah Nelayan
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
                new Vector3(-165f, 0f, 12f), Quaternion.Euler(0, 90f, 0), westRoot.transform, "FishHouse_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Danau,
                new Vector3(-165f, 0f, -10f), Quaternion.Euler(0, 90f, 0), westRoot.transform, "FishHouse_2");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
                new Vector3(-180f, 0f, 20f), Quaternion.Euler(0, 70f, 0), westRoot.transform, "FishHouse_3");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Barn,
                new Vector3(-180f, 0f, -18f), Quaternion.Euler(0, 100f, 0), westRoot.transform, "FishHouse_Barn");

            // NPC Kapten Nelayan
            var npcNelayan = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Farmer,
                new Vector3(-162f, 0f, 5f), Quaternion.Euler(0, 90f, 0),
                westRoot.transform, "NPC_KaptenNelayan");
            var dlgNelayan = npcNelayan.GetComponent<NpcDialogue>() ?? npcNelayan.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_KaptenNelayan",
                "Ahoy, pelancong! Selamat datang di Dermaga Sungai Perak. " +
                "Aku Kapten Bagas, pemimpin para nelayan di sini. " +
                "Akhir-akhir ini ikan-ikan aneh bermunculan dari kedalaman sungai... " +
                "Mungkin ada sesuatu yang mengganggu aliran air dari hutan timur!",
                dlgNelayan);

            // NPC Pedagang Ikan
            var npcIkan = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Woman,
                new Vector3(-160f, 0f, -8f), Quaternion.Euler(0, 60f, 0),
                westRoot.transform, "NPC_PedagangIkan");
            var dlgIkan = npcIkan.GetComponent<NpcDialogue>() ?? npcIkan.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PedagangIkan",
                "Hei, kamu mau beli ikan segar? Kami punya ikan mas, ikan patin, dan belut sungai! " +
                "Tapi hati-hati ya, kemarin ada buaya raksasa terlihat di tikungan sungai sebelah utara...",
                dlgIkan);

            // Resource: Ikan Segar
            PlaceArtResource(westRoot.transform, "FishCatch_1",
                "EA03_Prop_Container_Crate_02a", "fish",
                "Tekan E untuk mengambil Ikan Segar",
                new Vector3(-144f, 0f, 14f), Quaternion.identity,
                Vector3.one, slavicMat, new Color(0.3f, 0.6f, 0.8f));

            // Anjing dermaga
            AnimalAssetSetup.SpawnDog(new Vector3(-158f, 0f, 2f), Quaternion.Euler(0, 90f, 0),
                westRoot.transform, RealisticDogBuilder.DogBreed.GoldenRetriever, "Bagas_Jr",
                "Guk! Bagas Jr menjaga dermaga dengan semangat!");

            // Pohon & rumput zona barat
            for (int i = 0; i < 30; i++)
            {
                float tx = Random.Range(-215f, -155f);
                float tz = Random.Range(-45f, 45f);
                float ts = Random.Range(0.9f, 1.3f);
                if (i % 3 == 0)
                    RealisticTreeBuilder.BuildBirchTree(new Vector3(tx, 0, tz), ts, westRoot.transform, birchBarkMat, summerLeafMat);
                else
                    RealisticTreeBuilder.BuildOakTree(new Vector3(tx, 0, tz), ts, westRoot.transform, barkMat, summerLeafMat);
            }
            if (grassMat)
                for (int i = 0; i < 28; i++)
                {
                    float gx = Random.Range(-210f, -155f);
                    float gz = Random.Range(-40f, 40f);
                    RealisticGrassBuilder.BuildGrassClump(new Vector3(gx, 0, gz), 10, 4.5f, westRoot.transform, grassMat);
                }
        }

        // ════════════════════════════════════════════════════════════════
        // ZONA TIMUR — TEBING TAMBANG KRISTAL & GUA GELAP (X=155-220)
        // ════════════════════════════════════════════════════════════════
        {
            var eastRoot = new GameObject("Zone_East_CrystalMines");
            eastRoot.transform.SetParent(expandRoot.transform);

            // Dinding Tebing Batu
            var cliffBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffBase.name = "CliffBase_East";
            cliffBase.transform.SetParent(eastRoot.transform);
            cliffBase.transform.position = new Vector3(175f, 4f, 0f);
            cliffBase.transform.localScale = new Vector3(10f, 8f, 60f);
            ApplyColor(cliffBase, new Color(0.5f, 0.46f, 0.42f));

            // Mulut Gua Gelap
            var cavemouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cavemouth.name = "CaveMouth_East";
            cavemouth.transform.SetParent(eastRoot.transform);
            cavemouth.transform.position = new Vector3(171f, 1.5f, 8f);
            cavemouth.transform.localScale = new Vector3(1f, 3.5f, 5.5f);
            ApplyColor(cavemouth, new Color(0.05f, 0.05f, 0.08f));
            Object.DestroyImmediate(cavemouth.GetComponent<Collider>());
            AddPorchLantern(eastRoot.transform, new Vector3(172f, 2f, 8f), new Color(0.4f, 0.2f, 1f), 15f);

            // 6 Kristal raksasa berdiri
            Vector3[] crystalPos = {
                new Vector3(162f, 0f, -5f), new Vector3(165f, 0f,  3f),
                new Vector3(168f, 0f, -10f), new Vector3(160f, 0f, 10f),
                new Vector3(170f, 0f, 16f), new Vector3(163f, 0f, -16f),
            };
            for (int c = 0; c < crystalPos.Length; c++)
            {
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crystal.name = $"CrystalFormation_{c + 1}";
                crystal.transform.SetParent(eastRoot.transform);
                float ch = Random.Range(1.2f, 2.8f);
                crystal.transform.position = crystalPos[c] + new Vector3(0, ch, 0);
                crystal.transform.localScale = new Vector3(0.5f, ch, 0.5f);
                crystal.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0, 360f), Random.Range(-8f, 8f));
                ApplyColor(crystal, new Color(0.5f + Random.Range(0f, 0.2f), 0.2f, 0.9f));
                Object.DestroyImmediate(crystal.GetComponent<Collider>());
                AddPorchLantern(crystal.transform, new Vector3(0, ch, 0), new Color(0.5f, 0.3f, 1f), 6f);
            }

            // Pos Penjaga Tambang
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
                new Vector3(162f, 0f, -25f), Quaternion.Euler(0, -90f, 0), eastRoot.transform, "MineGuardPost_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Minka,
                new Vector3(162f, 0f,  25f), Quaternion.Euler(0, -90f, 0), eastRoot.transform, "MineGuardPost_2");

            // Resource: Bijih Kristal Ungu
            PlaceArtResource(eastRoot.transform, "CrystalOre_1",
                "EA03_Environment_Rock_CatHead_01a", "crystal_ore",
                "Tekan E untuk menambang Bijih Kristal",
                new Vector3(165f, 0f, 3f), Quaternion.Euler(0, 45f, 0),
                new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.6f, 0.3f, 0.9f));
            PlaceArtResource(eastRoot.transform, "CrystalOre_2",
                "EA03_Environment_Rock_Head_01a", "crystal_ore",
                "Tekan E untuk menambang Bijih Kristal",
                new Vector3(163f, 0f, -8f), Quaternion.Euler(0, 120f, 0),
                new Vector3(1.1f, 1.1f, 1.1f), slavicMat, new Color(0.6f, 0.3f, 0.9f));
            PlaceArtResource(eastRoot.transform, "CrystalOre_3",
                "EA03_Environment_Rock_CatHead_01b", "crystal_ore",
                "Tekan E untuk menambang Bijih Kristal",
                new Vector3(169f, 0f, 14f), Quaternion.Euler(0, 70f, 0),
                new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.6f, 0.3f, 0.9f));

            // NPC Mandor Tambang
            var npcMandor = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Guard,
                new Vector3(160f, 0f, -20f), Quaternion.Euler(0, -120f, 0),
                eastRoot.transform, "NPC_MandorTambang");
            var dlgMandor = npcMandor.GetComponent<NpcDialogue>() ?? npcMandor.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_MandorTambang",
                "Hei, berhenti! Area Tambang Kristal Eldara adalah kawasan terbatas. " +
                "Hanya penambang berlisensi yang boleh masuk ke dalam gua. " +
                "Kalau kamu bawa perlengkapan yang cukup, aku bisa izinkan kamu masuk... " +
                "tapi awas, ada makhluk bawah tanah yang menjaga kristal-kristal itu!",
                dlgMandor);

            // NPC Penambang Tua
            var npcTua = ProceduralNpcBuilder.BuildNpc(
                ProceduralNpcBuilder.NpcStyle.Farmer,
                new Vector3(158f, 0f, 18f), Quaternion.Euler(0, -150f, 0),
                eastRoot.transform, "NPC_PenambangTua");
            var dlgTua = npcTua.GetComponent<NpcDialogue>() ?? npcTua.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PenambangTua",
                "Sudah 30 tahun aku menambang di sini, nak. Kristal ungu ini punya kekuatan luar biasa. " +
                "Kalau kamu bisa menyerahkan 5 bijih kristal ke Pandai Besi Desa, " +
                "dia bisa buat senjata terkuat yang pernah ada di Eldara!",
                dlgTua);

            // Pohon & rumput zona timur
            for (int i = 0; i < 20; i++)
            {
                float tx = Random.Range(155f, 210f);
                float tz = Random.Range(-45f, 45f);
                if (tx > 170f && Mathf.Abs(tz) < 25f) continue;
                RealisticTreeBuilder.BuildPineTree(new Vector3(tx, 0, tz),
                    Random.Range(0.8f, 1.2f), eastRoot.transform, barkMat, summerLeafMat);
            }
            if (grassMat)
                for (int i = 0; i < 20; i++)
                {
                    float gx = Random.Range(158f, 205f);
                    float gz = Random.Range(-38f, 38f);
                    if (gx > 168f && Mathf.Abs(gz) < 20f) continue;
                    RealisticGrassBuilder.BuildGrassClump(new Vector3(gx, 0, gz), 7, 3f, eastRoot.transform, grassMat);
                }

            // Serigala tambang
            AnimalAssetSetup.SpawnWolf(new Vector3(158f, 0f, -38f), Quaternion.Euler(0, -90f, 0), eastRoot.transform, "Serigala Tambang Utara");
            AnimalAssetSetup.SpawnWolf(new Vector3(160f, 0f,  35f), Quaternion.Euler(0, -130f, 0), eastRoot.transform, "Serigala Tambang Selatan");
        }

        // Spawn musuh di jalur masuk 4 zona baru
        Vector3[] extraSpawns = {
            new Vector3(  0f, 0f, 165f),
            new Vector3(  0f, 0f,-168f),
            new Vector3(-162f, 0f,  0f),
            new Vector3( 162f, 0f,  0f),
        };
        foreach (var sp in extraSpawns)
        {
            var spMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spMarker.name = "SpawnPoint_Expansion";
            spMarker.transform.SetParent(expandRoot.transform);
            spMarker.transform.position = sp;
            spMarker.transform.localScale = new Vector3(2f, 0.3f, 2f);
            ApplyColor(spMarker, new Color(0.85f, 0.1f, 0.1f));
            Object.DestroyImmediate(spMarker.GetComponent<Collider>());
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  10. TITIK SPAWN MUSUH (DEEP IN THE WOODS)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnemySpawners()
    {
        var spawnerRoot = GameObject.Find("--- ENEMY SPAWNERS ---");
        if (spawnerRoot) Object.DestroyImmediate(spawnerRoot);
        spawnerRoot = new GameObject("--- ENEMY SPAWNERS ---");

        // 4 Titik Invasi di 4 penjuru luar benteng (di dalam rimba hutan lebat)
        var spawnPositions = new (Vector3 pos, string sName)[]
        {
            (new Vector3( 0f, 0f, 85f),  "SpawnPoint_North_Woods"),
            (new Vector3( 0f, 0f,-80f),  "SpawnPoint_South_Road"),
            (new Vector3( 92f,0f,   0f), "SpawnPoint_East_Mines"),
            (new Vector3(-92f,0f,   0f), "SpawnPoint_West_Forest"),
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
        bossMarker.transform.position = new Vector3(0f, 0f, 80f);
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
