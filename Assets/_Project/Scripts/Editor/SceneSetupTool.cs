#if UNITY_EDITOR
using System.Collections.Generic;
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
public static partial class SceneSetupTool
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

        // Seed tetap: menjalankan ulang setup menghasilkan tata letak yang sama (tidak "mengocok" desa tiap kali).
        Random.InitState(20260611);

        // Pastikan scene yang aktif adalah SampleScene, bukan scene baru kosong (Untitled)
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
        {
            if (System.IO.File.Exists(sampleScenePath))
            {
                EditorSceneManager.OpenScene(sampleScenePath);
            }
        }

        // 1. BERSIHKAN SEMUA OBJEK LAMA dari sesi build sebelumnya
        //    (termasuk bangunan dari ProceduralHouseBuilder lama yang namanya berbeda)
        string[] namaLama = new string[]
        {
            "Village", "--- NPCs ---", "--- TOWERS ---", "--- WILDERNESS ---",
            "--- GRASS FIELDS ---", "--- 3D WILDFLOWERS ---", "--- WORLD OBJECTS ---",
            "--- FAUNA & ANIMALS ---", "--- EXPANSION ZONES ---", "--- MEGA EXPANSION ---",
            "--- ZONE TRIGGERS ---", "--- ENEMY SPAWNERS ---", "--- VILLAGE DETAILS ---", "Village_Yard_Fences",
            "--- CHUNK RENDER DISTANCE ---",
            "Managers", "HUD", "CameraRig", "Ground",
            "EXPANSION_ZONES", "MegaExpansion", "ZoneTriggers", "NightSpawner",
            "Zone_North_Mountain", "Zone_South_Swamp", "Zone_West_Fishing", "Zone_East_Cliffs",
            "Zone_FarNorth_SnowPeak", "Zone_FarSouth_FairyForest",
            "Zone_FarWest_SeaTemple", "Zone_FarEast_FireCanyon",
            "ZoneConnectors", "ZoneConnectors_Mega",
        };
        foreach (string nm in namaLama)
        {
            GameObject lama = GameObject.Find(nm);
            while (lama != null)
            {
                Object.DestroyImmediate(lama);
                lama = GameObject.Find(nm);
            }
        }

        // 2. Tag & Material pre-flight
        EnsureTagExists("NPC");
        var slavicMat = AssetMaterialSetup.SetupSlavicMaterial();
        AssetMaterialSetup.SetupMedievalMaterials();

        // 3. Setup Terrain & Atmosfer
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

        // 9d. MEGA EKSPANSI PETA — 4 Zona Lanjutan (total peta 1000x1000m):
        //     Puncak Salju Utara, Hutan Peri Selatan, Kuil Laut Barat, Ngarai Api Timur
        SetupMegaExpansion(slavicMat, grassTuftMat);

        // 9e. Zone Trigger & Batas Wilayah (BoxCollider music/weather per zona)
        SetupZoneTriggers();

        // 9f. Perbaikan Desain Akhir (Hutan alami berumpun, sumur desa, lampu jalan luar, kabut dinamis)
        ApplyVillageDesignPass();

        // 10. Titik Invasi Monster Malam (4 Arah Hutan Liar)
        SetupEnemySpawners();

        // 11. Antarmuka Pemain (Full HUD)
        SetupHUD(playerInteractor, playerHealth);

        // 12. Sistem Jarak Render Chunk Global ala Minecraft (Optimasi 60 FPS Seluruh Peta)
        ChunkRenderDistance.ApplyToEntireWorldMenu();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = player;

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "🏰 Desa Eldara Skala Penuh Berhasil Dibangun!",
                "Selamat datang di Desa Eldara versi ekspansif!\n\n" +
                "Fitur yang telah dibangun:\n" +
                "☀️ Siklus siang-malam: cahaya matahari bergerak terbit di timur, tenggelam di barat\n" +
                "🌫️ Warna kabut ikut berubah mengikuti waktu\n" +
                "☁️ 22+ Awan 3D melayang dinamis ditiup angin & berubah warna fajar-siang-senja\n" +
                "🏡 11 Gaya Rumah Kayu Realistis: Rumah Gadang, Joglo, Minka Jepang, Log Cabin, Chalet Alpen, A-Frame, Rumah Panggung, Barn, Treehouse, Pondok Danau, & Villa Modern\n" +
                "👥 5 Karakter NPC 3D Stylized (Porting Blender): Kepala Desa (Merchant), Petani (Farmer), Pandai Besi & Penjaga Benteng (Guard), Dukun (Wizard), & Gadis Desa (Woman) — Lengkap dengan rute patroli mandiri & animasi berjalan prosedural!\n" +
                "🎪 Alun-Alun Pasar: Gerobak pedagang, peti, tong kayu, bangku, & rambu desa\n" +
                "🌾 Kawasan Pertanian: Lumbung gandum, kandang ternak, & pagar kebun\n" +
                "⚒️ Kawasan Pandai Besi: Bengkel tempa, tumpukan balok kayu, & area tambang bijih\n" +
                "🔮 Kompleks Altar Mistis: Formasi 12 pilar batu kuno di utara desa\n" +
                "🛡️ 8 Menara Pengawas Benteng + Pagar Kayu Melingkar Skala 80 Meter\n" +
                "🌲 Hutan Rimba Keliling: 90+ Pohon cemara & ek bervariasi mengitari desa\n" +
                "🐾 Ekosistem Fauna: Ayam Jago Eldara (11 klip animasi prosedural!), Kucing Luna & Oliver, Anjing Milo, Serigala, & Elang!\n" +
                "🏮 Lentera Malam: Lampu lentera hangat di tiap rumah & menara\n\n" +
                "🗺️ EKSPANSI PETA GELOMBANG 1 (500×500 meter):\n" +
                "⛰️ Zona UTARA — Desa Pegunungan, Kuil Batu Kuno, Jembatan Kabut, 2 NPC Baru\n" +
                "🌿 Zona SELATAN — Rawa Misterius, Perkemahan Goblin, Kolam Racun, NPC Penyihir Rawa\n" +
                "⛵ Zona BARAT — Desa Nelayan, Dermaga Sungai Perak, Perahu Rakit, Kapten Nelayan\n" +
                "💎 Zona TIMUR — Tebing Tambang Kristal, Gua Gelap, Formasi Kristal, 2 NPC Penambang\n" +
                "🛤️ 4 Jalan Cobblestone Penghubung Antar-Zona\n" +
                "🐺 6 Serigala Tambahan di Tiap Zona Baru\n\n" +
                "🗺️ MEGA EKSPANSI PETA GELOMBANG 2 (1000×1000 meter):\n" +
                "🏔️ Zona PUNCAK SALJU UTARA — Kuil Es Kuno, Penjaga Salju, Kristal Beku, Jembatan Batu\n" +
                "🌸 Zona HUTAN PERI SELATAN — Pohon Bercahaya, Lingkaran Peri, Reruntuhan Mistis, NPC Peri\n" +
                "🌊 Zona KUIL LAUT BARAT — Kuil Tenggelam, Kolam Besar, Patung Dewa Laut, NPC Penjaga\n" +
                "🌋 Zona NGARAI API TIMUR — Kawah Vulkanik, Lava, Obsidian Tower, NPC Penjaga Api\n" +
                "🛤️ 4 Koridor Jalan Tambahan ke Zona Mega\n" +
                "👾 Musuh Elite di tiap Zona Mega\n" +
                "🎵 Sistem Musik Zona Otomatis (cross-fade saat masuk zona)\n" +
                "🌦️ Sistem Cuaca Dinamis (hujan, salju, badai, kabut, cerah)\n" +
                "🗺️ 9 Zone Trigger Musik & Cuaca (BoxCollider batas wilayah)\n\n" +
                "Tekan ▶ Play untuk mulai menjelajahi dunia barumu!",
                "Bagus Sekali, Mainkan!"
            );
        }
    }

    [MenuItem("Eldara/Hapus Semua Pagar Liar & Penghalang")]
    public static void RemovePerimeterFences()
    {
        int count = 0;
        var yardFences = GameObject.Find("Village_Yard_Fences");
        if (yardFences)
        {
            count += yardFences.transform.childCount;
            Object.DestroyImmediate(yardFences);
        }

        var towerRoot = GameObject.Find("--- TOWERS ---");
        if (towerRoot)
        {
            var toRemove = new List<GameObject>();
            for (int i = 0; i < towerRoot.transform.childCount; i++)
            {
                var child = towerRoot.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("EA03_Village_Fence_01a"))
                {
                    // Pertahankan dua sayap gerbang masuk selatan (z ≈ -48)
                    if (Mathf.Abs(child.transform.position.z - (-48f)) < 1f) continue;
                    toRemove.Add(child);
                }
            }
            count += toRemove.Count;
            foreach (var go in toRemove) Object.DestroyImmediate(go);
        }

        // Hapus rumah nelayan jika masih ada di scene
        string[] fishHouses = { "FishHouse_1", "FishHouse_2", "FishHouse_3", "FishHouse_Barn" };
        foreach (var fh in fishHouses)
        {
            var fhGo = GameObject.Find(fh);
            if (fhGo) { count++; Object.DestroyImmediate(fhGo); }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Eldara] {count} modul pagar pekarangan & benteng liar berhasil dihapus.");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Pagar Dibersihkan!", $"{count} modul pagar yang menghalangi pekarangan dan jalan berhasil dibersihkan dari scene.", "OK");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. ENVIRONMENT & ATMOSPHERE
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnvironment()
    {
        // Ground plane luas (Plane 10 m x skala 100 = 1000 x 1000 meter)
        GameObject ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
        }
        ground.transform.localScale = new Vector3(100f, 1f, 100f);

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
            // Gunakan Clean_Grass_Meadow.png (1024x1024 seamless tanpa gambar bunga ceper 2D) agar tanah rumput hijau murni
            var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/SlavicWorld/Textures/Clean_Grass_Meadow.png")
                           ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/SlavicWorld/Textures/Flower_Grass.png");
            if (grassTex)
            {
                grassMat.SetTexture("_BaseMap", grassTex);
                grassMat.SetTextureScale("_BaseMap", new Vector2(70f, 70f));
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
                grassMat.SetTextureScale("_BumpMap", new Vector2(65f, 65f));
                grassMat.EnableKeyword("_NORMALMAP");
                grassMat.SetFloat("_BumpScale", 0.75f);
            }
            grassMat.SetColor("_BaseColor", new Color(0.78f, 0.92f, 0.65f, 1f));
            grassMat.SetFloat("_Smoothness", 0.04f);
            EditorUtility.SetDirty(grassMat);
            ground.GetComponent<Renderer>().sharedMaterial = grassMat;
        }

        // Kabut Atmosferik Realistis (Distance Fog - Desa jernih, kabut lembut di horizon)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 75f;
        RenderSettings.fogEndDistance = 380f;
        RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.85f);

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
        var dnc = managers.GetComponent<DayNightCycle>();
        if (!dnc) dnc = managers.AddComponent<DayNightCycle>();
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

        // Terapkan posisi matahari awal (jam startHour) & bersihkan bola matahari/bulan lama
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

        var cc = player.GetComponent<CharacterController>();
        if (!cc) cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;

        // Reset posisi dan rotasi player secara mutlak di pelataran alun-alun depan Balai Desa (menghadap Balai Desa)
        bool ccActive = cc.enabled;
        cc.enabled = false;
        player.transform.position = new Vector3(0f, 1.05f, 10.5f);
        player.transform.rotation = Quaternion.Euler(0, 180f, 0);
        cc.enabled = ccActive;

        playerCtrl = player.GetComponent<PlayerController>();
        if (!playerCtrl) playerCtrl = player.AddComponent<PlayerController>();

        playerHealth = player.GetComponent<Health>();
        if (!playerHealth) playerHealth = player.AddComponent<Health>();
        playerHealth.max = 100f;

        if (!player.GetComponent<PlayerCombat>()) player.AddComponent<PlayerCombat>();

        playerInteractor = player.GetComponent<PlayerInteractor>();
        if (!playerInteractor) playerInteractor = player.AddComponent<PlayerInteractor>();

        if (!player.GetComponent<HitFlash>()) player.AddComponent<HitFlash>();
        if (!player.GetComponent<BuildMode>()) player.AddComponent<BuildMode>();
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

        var rig = cam.GetComponent<CameraRig>();
        if (!rig) rig = cam.gameObject.AddComponent<CameraRig>();
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
        if (!managers.GetComponent<CloudManager>())    managers.AddComponent<CloudManager>();
        if (!managers.GetComponent<WeatherSystem>())   managers.AddComponent<WeatherSystem>();
        if (!managers.GetComponent<ZoneMusicManager>()) managers.AddComponent<ZoneMusicManager>();

        var gm = managers.GetComponent<GameManager>();
        if (!gm) gm = managers.AddComponent<GameManager>();
        var soGm = new SerializedObject(gm);
        soGm.FindProperty("player").objectReferenceValue = playerCtrl;
        soGm.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        soGm.ApplyModifiedProperties();

        var saveSys = managers.GetComponent<SaveSystem>();
        if (!saveSys) saveSys = managers.AddComponent<SaveSystem>();
        var soSave = new SerializedObject(saveSys);
        soSave.FindProperty("player").objectReferenceValue = playerCtrl;
        soSave.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        soSave.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5. BANGUN DESA SKALA PENUH (KAWASAN PEMUKIMAN, PASAR, PERTANIAN, PENGRAJIN)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupExpansiveVillage(Material slavicMat)
    {
        GameObject village = GameObject.Find("Village");
        if (village) Object.DestroyImmediate(village);

        village = new GameObject("Village");

        // ═════════════════════════════════════════════════════════════════
        //  A. SISTEM JALAN KOTA MEDIEVAL (COBBLESTONE BOULEVARD & CONNECTORS)
        // ═════════════════════════════════════════════════════════════════

        // Material jalan batu paving medieval (PBR Cobblestone)
        var roadMat = GetOrCreateVillageRoadMaterial();

        // 1. Alun-Alun Paving Pusat (Central Plaza 18 meter)
        CreateCircularPlaza(village.transform, "Central_Plaza_Stone", Vector3.zero, 9.0f, roadMat, 2.5f);

        var roadsParent = new GameObject("Village_Road_Network");
        roadsParent.transform.SetParent(village.transform);

        // 2. Jalur Utama Utara (North Avenue, X = 0, Z = 8.8 s/d 32 menuju Balai Desa, lebar 4.2m)
        CreateContinuousPavedRoad(roadsParent.transform, "Road_North_Avenue",
            new Vector3(0, 0, 8.8f), new Vector3(0, 0, 32f), 4.2f, roadMat, 2.5f);

        // Pelataran Promenade Balai Desa & Pusat Adat (North Civic Plaza, Z = 32, X = -28 s/d +28, lebar 5.5m)
        CreateContinuousPavedRoad(roadsParent.transform, "Plaza_North_Civic",
            new Vector3(-28f, 0, 32f), new Vector3(28f, 0, 32f), 5.5f, roadMat, 2.5f);

        // Jalan Setapak Pintu Rumah Gadang & Pendopo Joglo ke Pelataran Civic
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_Civic_Gadang",
            new Vector3(-22f, 0, 32f), new Vector3(-22f, 0, 35f), 3.0f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_Civic_Joglo",
            new Vector3(22f, 0, 32f), new Vector3(22f, 0, 35f), 3.0f, roadMat, 2.5f);

        // 3. Jalur Utama Selatan (South Avenue, X = 0, Z = -8.8 s/d -50 menuju Gerbang Benteng, lebar 4.2m)
        CreateContinuousPavedRoad(roadsParent.transform, "Road_South_Avenue",
            new Vector3(0, 0, -8.8f), new Vector3(0, 0, -50f), 4.2f, roadMat, 2.5f);

        // Jalan Setapak Penginapan A-Frame & Minka Dukun ke South Avenue
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_South_AFrame",
            new Vector3(-2.1f, 0, -30f), new Vector3(-14f, 0, -30f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_South_Minka",
            new Vector3(2.1f, 0, -30f), new Vector3(14f, 0, -30f), 2.8f, roadMat, 2.5f);

        // 4. Jalur Utama Barat (West Avenue, Z = 0, X = -8.8 s/d -54 menuju Pertanian & Danau, lebar 4.2m)
        CreateContinuousPavedRoad(roadsParent.transform, "Road_West_Avenue",
            new Vector3(-8.8f, 0, 0), new Vector3(-54f, 0, 0), 4.2f, roadMat, 2.5f);

        // Jalan Setapak Rumah-Rumah Distrik Barat ke West Avenue
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_West_FarmNorth",
            new Vector3(-22f, 0, 2.1f), new Vector3(-22f, 0, 11f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_West_BarnNorth",
            new Vector3(-40f, 0, 2.1f), new Vector3(-40f, 0, 11f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_West_ChaletSouth",
            new Vector3(-22f, 0, -2.1f), new Vector3(-22f, 0, -11f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_West_LakeSouth",
            new Vector3(-40f, 0, -2.1f), new Vector3(-40f, 0, -11f), 2.8f, roadMat, 2.5f);

        // 5. Jalur Utama Timur (East Avenue, Z = 0, X = 8.8 s/d 54 menuju Pandai Besi & Pengrajin, lebar 4.2m)
        CreateContinuousPavedRoad(roadsParent.transform, "Road_East_Avenue",
            new Vector3(8.8f, 0, 0), new Vector3(54f, 0, 0), 4.2f, roadMat, 2.5f);

        // Jalan Setapak Rumah-Rumah Distrik Timur ke East Avenue
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_East_SmithNorth",
            new Vector3(21f, 0, 2.1f), new Vector3(21f, 0, 12.4f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_East_StorageNorth",
            new Vector3(32.2f, 0, 2.1f), new Vector3(32.2f, 0, 12.9f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_East_CabinSouth",
            new Vector3(22f, 0, -2.1f), new Vector3(22f, 0, -11f), 2.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(roadsParent.transform, "Walkway_East_ModernSouth",
            new Vector3(41.4f, 0, -2.1f), new Vector3(41.4f, 0, -7f), 2.8f, roadMat, 2.5f);

        // ═════════════════════════════════════════════════════════════════
        //  B. ALUN-ALUN PUSAT: SUMUR DESA KUNO & PASAR TERBUKA
        // ═════════════════════════════════════════════════════════════════

        // 1. Sumur Desa Kuno di Tengah Alun-Alun (Center Well Landmark)
        BuildVillageWell(village.transform, new Vector3(0f, 0f, 0f));

        // 2. Area Pasar Terbuka (Ditempatkan di sisi timur alun-alun di persimpangan jalan)
        InstantiateArtModel("Prop_Wagon", new Vector3(12.5f, 0f, 3.5f), Quaternion.Euler(0, -15f, 0), Vector3.one, village.transform);
        InstantiateArtModel("Prop_Wagon", new Vector3(12.5f, 0f, -3.5f), Quaternion.Euler(0, 20f, 0), Vector3.one, village.transform);
        InstantiateArtModel("EA03_Prop_Container_Crate_01a", new Vector3(14.5f, 0f, 2f), Quaternion.Euler(0, 15f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_02a", new Vector3(15f, 0f, 3.2f), Quaternion.Euler(0, 80f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_03a", new Vector3(14.8f, 0.75f, 2.5f), Quaternion.Euler(0, 35f, 0), Vector3.one * 0.9f, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(16f, 0f, -2f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(16.7f, 0f, -2.6f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Vegetable_Basket_02", new Vector3(11.5f, 0f, 2.5f), Quaternion.identity, Vector3.one, village.transform, slavicMat);

        // 3. Bangku Santai Warga di Sudut Barat Alun-Alun
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-10.5f, 0f, 2.5f), Quaternion.Euler(0, 90f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-10.5f, 0f, -2.5f), Quaternion.Euler(0, 90f, 0), Vector3.one, village.transform, slavicMat);

        // 4. Tiang Lentera Sudut Alun-Alun (4 Lampu Gantung Kayu Tradisional di 4 Sudut Plaza)
        AddTraditionalVillageLamp(village.transform, new Vector3(-8.5f, 0,  8.5f),  135f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 8.5f, 0,  8.5f), -135f);
        AddTraditionalVillageLamp(village.transform, new Vector3(-8.5f, 0, -8.5f),   45f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 8.5f, 0, -8.5f),  -45f);

        // ═════════════════════════════════════════════════════════════════
        //  C. TATA LETAK BANGUNAN DESA (4 DISTRIK SELAYAKNYA DESA NYATA)
        // ═════════════════════════════════════════════════════════════════

        // ── DISTRIK 1: UTARA — BALAI DESA & PUSAT ADAT PEMERINTAHAN ─────────
        // Menghadap ke Selatan ke arah Promenade Civic & Alun-Alun

        // 1. Balai Desa Eldara (Megah di ujung North Avenue, menghadap Selatan lurus ke Alun-Alun)
        var hall = InstantiateArtModel("EA03_Town_Building_Administrative _01a",
            new Vector3(0f, 0f, 38f), Quaternion.Euler(0, 180f, 0),
            new Vector3(1.15f, 1.15f, 1.15f), village.transform, slavicMat);
        if (hall) { hall.name = "Bld_TownHall"; AddPorchLantern(hall.transform, new Vector3(0, 3.2f, -4f), range: 12f); }

        // 2. Rumah Gadang (Rumah Kepala Desa Adat - Sayap Barat Laut, menghadap Selatan ke promenade)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Gadang,
            new Vector3(-22f, 0f, 36f), Quaternion.Euler(0, 180f, 0),
            village.transform, "House_KepalaDesa_Gadang");

        // 3. Balai Pertemuan Pendopo Joglo (Sayap Timur Laut, menghadap Selatan ke promenade)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Joglo,
            new Vector3(22f, 0f, 36f), Quaternion.Euler(0, 180f, 0),
            village.transform, "Pavilion_Joglo");

        // ── DISTRIK 2: BARAT — KAWASAN PERTANIAN & DANAU ────────────────────
        // Menghadap ke Jalan Barat (West Avenue Z = 0)

        // 4. Rumah Petani Panggung (Sisi Utara Jalan Barat, menghadap Selatan ke jalan)
        var farmHouse = ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
            new Vector3(-22f, 0f, 15f), Quaternion.Euler(0, 180f, 0),
            village.transform, "House_Petani_Panggung");
        if (farmHouse)
        {
            InstantiateArtModel("EA03_Prop_Container_Bale_01a",   new Vector3(-18f, 0f, 10f), Quaternion.identity,          Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Container_Bale_01a",   new Vector3(-18f, 0.9f, 10.2f), Quaternion.Euler(0,40f,0), Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Vegetable_Basket_02",  new Vector3(-17f, 0f, 11.5f), Quaternion.identity,         Vector3.one, village.transform, slavicMat);
        }

        // 5. Lumbung Gandum Besar / Barn (Sisi Utara Jalan Barat, sebelah barat rumah petani, menghadap Selatan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Barn,
            new Vector3(-40f, 0f, 15f), Quaternion.Euler(0, 180f, 0),
            village.transform, "Barn_Gandum");

        // 6. Rumah Warga Lembah / Chalet (Sisi Selatan Jalan Barat, menghadap Utara ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Chalet,
            new Vector3(-22f, 0f, -15f), Quaternion.Euler(0, 0f, 0),
            village.transform, "House_WargaBarat_Chalet");

        // 7. Pondok Tepi Danau (Sisi Selatan Jalan Barat, sebelah barat Chalet, menghadap Utara ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Danau,
            new Vector3(-40f, 0f, -15f), Quaternion.Euler(0, 0f, 0),
            village.transform, "House_PondokDanau");

        // ── DISTRIK 3: TIMUR — KAWASAN PANDAI BESI & PENGRAJIN ──────────────
        // Menghadap ke Jalan Timur (East Avenue Z = 0)

        // 8. Bengkel Pandai Besi (Sisi Utara Jalan Timur, menghadap Selatan ke jalan)
        var smith = BuildModularHouse(village.transform, "House_PandaiBesi",
            new Vector3(22f, 0f, 15f), 0f, "wood", false, false);
        if (smith)
        {
            InstantiateArtModel("EA03_Prop_Tool_Stump_02a",          new Vector3(18f, 0f, 10f),     Quaternion.identity,     Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Tool_Pick_01a",           new Vector3(18.3f,0.4f,10.2f), Quaternion.Euler(45,0,0),Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Forester_CutOutTrees_03", new Vector3(28f, 0f, 10f),     Quaternion.identity,     Vector3.one, village.transform, slavicMat);
        }

        // 9. Gudang Logistik Pandai Besi (Sisi Utara Jalan Timur, sebelah timur bengkel, menghadap Selatan)
        BuildModularHouse(village.transform, "Storage_PandaiBesi",
            new Vector3(33f, 0f, 15f), 0f, "wood", false, false, small: true);

        // 10. Rumah Pengrajin / Log Cabin (Sisi Selatan Jalan Timur, menghadap Utara ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
            new Vector3(22f, 0f, -15f), Quaternion.Euler(0, 0f, 0),
            village.transform, "House_WargaTimur_Cabin");

        // 11. Villa Kayu Pengrajin Modern (Sisi Selatan Jalan Timur, sebelah timur Cabin, menghadap Utara)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Modern,
            new Vector3(40f, 0f, -15f), Quaternion.Euler(0, 0f, 0),
            village.transform, "Villa_Modern");

        // ── DISTRIK 4: SELATAN — KAWASAN PEMUKIMAN & GERBANG BENTENG ────────
        // Mengapit Jalan Selatan (South Avenue X = 0)

        // 12. Pondok Singgah Pengelana / A-Frame (Sisi Barat Jalan Selatan, menghadap Timur ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.AFrame,
            new Vector3(-16f, 0f, -30f), Quaternion.Euler(0, 90f, 0),
            village.transform, "House_AFrame_Singgah");

        // 13. Pondok Dukun Mistis Minka (Sisi Timur Jalan Selatan, menghadap Barat ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Minka,
            new Vector3(16f, 0f, -30f), Quaternion.Euler(0, -90f, 0),
            village.transform, "House_Dukun_Minka");

        // 14. Pos Penjaga Gerbang Barat (Sisi Barat Gerbang Selatan, menghadap Utara)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
            new Vector3(-14f, 0f, -46f), Quaternion.Euler(0, 0f, 0),
            village.transform, "House_Gerbang_Kiri");

        // 15. Pos Penjaga Gerbang Timur (Sisi Timur Gerbang Selatan, menghadap Utara)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
            new Vector3(14f, 0f, -46f), Quaternion.Euler(0, 0f, 0),
            village.transform, "House_Gerbang_Kanan");

        // ── DISTRIK LUAR — POS RIMBA ──────────────────────────────────────
        // 16. Treehouse Pengawas (Pojok Timur Laut di dahan rimba, menghadap Barat Daya ke desa)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Pohon,
            new Vector3(74f, 0f, 40f), Quaternion.Euler(0, -135f, 0),
            village.transform, "Treehouse_Pengawas");


    }

    // ─────────────────────────────────────────────────────────────────────
    //  LANDMARK & STREET FURNITURE
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Membuat mesh cincin / silinder berlubang (Hollow Ring / Shaft Tube) untuk sumur atau plinth.
    /// Dibuat double-sided sehingga memiliki ketebalan nyata dan terlihat solid dari segala sudut tanpa tembus pandang.
    /// </summary>
    static Mesh CreateHollowRingMesh(string name, float outerR, float innerR, float topY, float bottomY, int segments = 32, bool addInnerWall = true)
    {
        var mesh = new Mesh { name = name };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        void AddQuad(int v0, int v1, int v2, int v3)
        {
            // Sisi Depan (Clockwise)
            triangles.Add(v0);
            triangles.Add(v1);
            triangles.Add(v2);

            triangles.Add(v0);
            triangles.Add(v2);
            triangles.Add(v3);

            // Sisi Belakang (Double-sided - memastikan tidak pernah tembus pandang dari sudut manapun)
            triangles.Add(v0);
            triangles.Add(v2);
            triangles.Add(v1);

            triangles.Add(v0);
            triangles.Add(v3);
            triangles.Add(v2);
        }

        // 1. Dinding Luar (Outer Wall)
        int outerStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float u = (float)i / segments * 4f;

            // v0: bawah luar
            vertices.Add(new Vector3(cos * outerR, bottomY, sin * outerR));
            normals.Add(new Vector3(cos, 0f, sin));
            uvs.Add(new Vector2(u, 0f));

            // v1: atas luar
            vertices.Add(new Vector3(cos * outerR, topY, sin * outerR));
            normals.Add(new Vector3(cos, 0f, sin));
            uvs.Add(new Vector2(u, 1f));
        }

        for (int i = 0; i < segments; i++)
        {
            int b = outerStart + i * 2;
            AddQuad(b, b + 1, b + 3, b + 2);
        }

        // 2. Bibir Atas (Top Rim: antara outerR dan innerR di topY)
        int rimStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Atas luar
            vertices.Add(new Vector3(cos * outerR, topY, sin * outerR));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f));

            // Atas dalam
            vertices.Add(new Vector3(cos * innerR, topY, sin * innerR));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + cos * 0.35f, 0.5f + sin * 0.35f));
        }

        for (int i = 0; i < segments; i++)
        {
            int b = rimStart + i * 2;
            AddQuad(b, b + 1, b + 3, b + 2);
        }

        // 3. Dinding Dalam (Inner Shaft Wall)
        if (addInnerWall)
        {
            int innerStart = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float u = (float)i / segments * 3f;

                // Atas dalam
                vertices.Add(new Vector3(cos * innerR, topY, sin * innerR));
                normals.Add(new Vector3(-cos, 0f, -sin));
                uvs.Add(new Vector2(u, 1f));

                // Bawah dalam
                vertices.Add(new Vector3(cos * innerR, bottomY, sin * innerR));
                normals.Add(new Vector3(-cos, 0f, -sin));
                uvs.Add(new Vector2(u, 0f));
            }

            for (int i = 0; i < segments; i++)
            {
                int b = innerStart + i * 2;
                AddQuad(b, b + 1, b + 3, b + 2);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Membuat mesh cakram lingkaran horizontal (Circular Disc Mesh) double-sided untuk permukaan air atau lantai sumur
    /// </summary>
    static Mesh CreateCircularDiscMesh(string name, float radius, float yLevel, int segments = 32)
    {
        var mesh = new Mesh { name = name };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        vertices.Add(new Vector3(0f, yLevel, 0f));
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices.Add(new Vector3(cos * radius, yLevel, sin * radius));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f));
        }

        for (int i = 0; i < segments; i++)
        {
            // Sisi Atas
            triangles.Add(0);
            triangles.Add(1 + i);
            triangles.Add(1 + i + 1);

            // Sisi Bawah (Double-sided)
            triangles.Add(0);
            triangles.Add(1 + i + 1);
            triangles.Add(1 + i);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Helper membuat GameObject berbasis custom mesh lengkap dengan Renderer & Collider
    /// </summary>
    static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material mat, Color fallbackColor, bool addMeshCollider = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;
        else ApplyColor(go, fallbackColor);

        if (addMeshCollider)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }
        return go;
    }

    /// <summary>
    /// Helper membuat ember kayu pedesaan tradisional dengan klem besi dan rongga dalam
    /// </summary>
    static GameObject MakeWoodenBucket(Transform parent, Vector3 localPos, Quaternion localRot, Material woodMat, Material metalMat, Material darkMat)
    {
        var bucketGo = new GameObject("Well_Bucket");
        bucketGo.transform.SetParent(parent, false);
        bucketGo.transform.localPosition = localPos;
        bucketGo.transform.localRotation = localRot;

        // Badan ember kayu silinder
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "BucketBody";
        body.transform.SetParent(bucketGo.transform, false);
        body.transform.localPosition = new Vector3(0, 0.10f, 0);
        body.transform.localScale = new Vector3(0.28f, 0.10f, 0.28f);
        if (woodMat) body.GetComponent<Renderer>().sharedMaterial = woodMat;
        else ApplyColor(body, new Color(0.35f, 0.22f, 0.12f));
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Rongga dalam ember gelap
        var cavity = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cavity.name = "BucketCavity";
        cavity.transform.SetParent(bucketGo.transform, false);
        cavity.transform.localPosition = new Vector3(0, 0.15f, 0);
        cavity.transform.localScale = new Vector3(0.23f, 0.05f, 0.23f);
        if (darkMat) cavity.GetComponent<Renderer>().sharedMaterial = darkMat;
        else ApplyColor(cavity, new Color(0.12f, 0.12f, 0.12f));
        Object.DestroyImmediate(cavity.GetComponent<Collider>());

        // Klem besi pengikat ember
        var hoop1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hoop1.name = "HoopLower";
        hoop1.transform.SetParent(bucketGo.transform, false);
        hoop1.transform.localPosition = new Vector3(0, 0.05f, 0);
        hoop1.transform.localScale = new Vector3(0.29f, 0.015f, 0.29f);
        if (metalMat) hoop1.GetComponent<Renderer>().sharedMaterial = metalMat;
        else ApplyColor(hoop1, new Color(0.2f, 0.2f, 0.2f));
        Object.DestroyImmediate(hoop1.GetComponent<Collider>());

        var hoop2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hoop2.name = "HoopUpper";
        hoop2.transform.SetParent(bucketGo.transform, false);
        hoop2.transform.localPosition = new Vector3(0, 0.16f, 0);
        hoop2.transform.localScale = new Vector3(0.29f, 0.015f, 0.29f);
        if (metalMat) hoop2.GetComponent<Renderer>().sharedMaterial = metalMat;
        else ApplyColor(hoop2, new Color(0.2f, 0.2f, 0.2f));
        Object.DestroyImmediate(hoop2.GetComponent<Collider>());

        // Gagang lengkung ember
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "BucketHandle";
        handle.transform.SetParent(bucketGo.transform, false);
        handle.transform.localPosition = new Vector3(0, 0.26f, 0);
        handle.transform.localScale = new Vector3(0.02f, 0.14f, 0.26f);
        if (metalMat) handle.GetComponent<Renderer>().sharedMaterial = metalMat;
        else ApplyColor(handle, new Color(0.2f, 0.2f, 0.2f));
        Object.DestroyImmediate(handle.GetComponent<Collider>());

        return bucketGo;
    }

    /// <summary>
    /// Membuat mesh permukaan air berteselasi cincin konsentris halus (Tessellated Water Disc Mesh)
    /// </summary>
    static Mesh CreateTessellatedWaterMesh(string name, float radius, float yLevel, int rings = 4, int segments = 36)
    {
        var mesh = new Mesh { name = name };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        // Titik Pusat (Center vertex)
        vertices.Add(new Vector3(0f, yLevel, 0f));
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        // Cincin konsentris titik (Concentric rings)
        for (int r = 1; r <= rings; r++)
        {
            float ringR = radius * ((float)r / rings);
            for (int s = 0; s < segments; s++)
            {
                float angle = (float)s / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(cos * ringR, yLevel, sin * ringR));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f + cos * 0.5f * ((float)r / rings), 0.5f + sin * 0.5f * ((float)r / rings)));
            }
        }

        // Segitiga untuk lingkaran pusat
        for (int s = 0; s < segments; s++)
        {
            int next = (s + 1) % segments;
            // Sisi Atas
            triangles.Add(0);
            triangles.Add(1 + s);
            triangles.Add(1 + next);
            // Sisi Bawah (Double-sided)
            triangles.Add(0);
            triangles.Add(1 + next);
            triangles.Add(1 + s);
        }

        // Segitiga di antara cincin-cincin konsentris
        for (int r = 1; r < rings; r++)
        {
            int innerStart = 1 + (r - 1) * segments;
            int outerStart = 1 + r * segments;

            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;

                int i0 = innerStart + s;
                int i1 = innerStart + next;
                int o0 = outerStart + s;
                int o1 = outerStart + next;

                // Sisi Atas
                triangles.Add(i0);
                triangles.Add(o0);
                triangles.Add(i1);

                triangles.Add(i1);
                triangles.Add(o0);
                triangles.Add(o1);

                // Sisi Bawah (Double-sided)
                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(o0);

                triangles.Add(i1);
                triangles.Add(o1);
                triangles.Add(o0);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Mendapatkan atau membuat material air sumur desa fotorealistik (Realistic PBR Water Material)
    /// </summary>
    static Material GetOrCreateRealisticWaterMaterial()
    {
        string matPath = "Assets/_Project/Art/MedievalVillage/Materials/M_Village_Well_Water.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        var shader = Shader.Find("Eldara/RealisticWater");
        if (!mat)
        {
            if (shader) mat = new Material(shader);
            else mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (shader && mat.shader != shader)
        {
            mat.shader = shader;
            EditorUtility.SetDirty(mat);
        }

        // Konfigurasi parameter air jernih dan pantulan realistis
        mat.SetColor("_DeepColor", new Color(0.03f, 0.18f, 0.32f, 0.90f));     // Biru safir kedalaman
        mat.SetColor("_ShallowColor", new Color(0.16f, 0.62f, 0.78f, 0.65f));  // Toska jernih berkilau
        mat.SetColor("_FresnelColor", new Color(0.85f, 0.95f, 1.0f, 1.0f));    // Pantulan langit cerah
        mat.SetFloat("_Smoothness", 0.96f);
        mat.SetFloat("_WaveSpeed", 1.35f);
        mat.SetFloat("_WaveScale", 7.5f);
        mat.SetFloat("_WaveStrength", 0.42f);
        mat.SetFloat("_RippleFrequency", 24.0f);
        mat.SetFloat("_RippleSpeed", 2.6f);
        mat.SetFloat("_RippleStrength", 0.38f);
        return mat;
    }

    /// <summary>
    /// Membangun Sumur Batu Kuno Megah Berongga (Hollow Stone Well) di pusat Alun-Alun desa
    /// Dilengkapi rongga lubang sumur dalam dengan air jernih berkilau, dinding batu tekstur abad pertengahan,
    /// pilar kayu bertiang siku, penggulung tali kerekan, ember kayu bertali, dan atap genteng sirap.
    /// </summary>
    static void BuildVillageWell(Transform parent, Vector3 pos)
    {
        var wellRoot = new GameObject("Village_Stone_Well");
        wellRoot.transform.SetParent(parent);
        wellRoot.transform.position = pos;

        // Muat material tematik berkualitas tinggi
        var brickMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_UnevenBrick.mat");
        var rockMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_RockTrim.mat");
        var woodMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_WoodTrim.mat");
        var roofMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_RoundTiles.mat");
        var waterMat = GetOrCreateRealisticWaterMaterial();
        var darkMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Dark.mat");
        var metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Metal.mat");

        // 1. Pelataran Tangga Melingkar Berlubang Dasar Sumur (Hollow Foundation Plinth)
        var stepMesh = CreateHollowRingMesh("Well_StepBase_Mesh", outerR: 1.65f, innerR: 0.95f, topY: 0.10f, bottomY: 0f, segments: 32, addInnerWall: false);
        CreateMeshObject("Well_StepBase", wellRoot.transform, stepMesh, rockMat, new Color(0.62f, 0.60f, 0.55f));

        // 2. Dinding Cincin Batu Sumur Berongga (Main Hollow Stone Curb Shaft)
        // Memiliki rongga lubang dalam berdiameter 1.76m dengan dinding batu tebal 37cm yang solid dan double-sided!
        var curbMesh = CreateHollowRingMesh("Well_StoneCurb_Mesh", outerR: 1.25f, innerR: 0.88f, topY: 0.95f, bottomY: 0f, segments: 32, addInnerWall: true);
        CreateMeshObject("Well_StoneCurb", wellRoot.transform, curbMesh, brickMat, new Color(0.48f, 0.46f, 0.44f), addMeshCollider: true);

        // 3. Bibir Batu Pahat Atas Sumur Berlubang (Top Chiseled Stone Rim)
        var rimMesh = CreateHollowRingMesh("Well_StoneRim_Mesh", outerR: 1.30f, innerR: 0.85f, topY: 0.99f, bottomY: 0.93f, segments: 32, addInnerWall: true);
        CreateMeshObject("Well_StoneRim", wellRoot.transform, rimMesh, rockMat, new Color(0.55f, 0.53f, 0.50f));

        // 4. Dasar Gelap & Permukaan Air Fotorealistik di Dalam Rongga Sumur (Shaft Floor & Water)
        var floorMesh = CreateCircularDiscMesh("Well_Floor_Mesh", radius: 0.88f, yLevel: 0.05f, segments: 32);
        CreateMeshObject("Well_ShaftFloor", wellRoot.transform, floorMesh, rockMat, new Color(0.18f, 0.18f, 0.16f));

        // Kerikil & koin kuno di dasar sumur di bawah air jernih
        var floorProps = new GameObject("Well_Floor_Pebbles");
        floorProps.transform.SetParent(wellRoot.transform, false);
        Vector3[] pebbleOffsets = {
            new Vector3( 0.22f, 0.06f,  0.18f),
            new Vector3(-0.30f, 0.06f,  0.15f),
            new Vector3( 0.15f, 0.06f, -0.28f),
            new Vector3(-0.18f, 0.06f, -0.22f),
            new Vector3( 0.35f, 0.06f, -0.05f),
        };
        for (int p = 0; p < pebbleOffsets.Length; p++)
        {
            var pebble = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pebble.name = $"Pebble_{p}";
            pebble.transform.SetParent(floorProps.transform, false);
            pebble.transform.localPosition = pebbleOffsets[p];
            pebble.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            if (p % 2 == 0) ApplyColor(pebble, new Color(0.85f, 0.72f, 0.25f)); // Koin kuno keemasan
            else if (darkMat) pebble.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.DestroyImmediate(pebble.GetComponent<Collider>());
        }

        // Permukaan Air Bergelombang Realistis (Tessellated Dynamic Water Surface)
        // Terletak di ketinggian Y = 0.35m (64 cm di bawah bibir sumur), dengan animasi riak gelombang alami
        var waterMesh = CreateTessellatedWaterMesh("Well_Water_Mesh", radius: 0.875f, yLevel: 0.35f, rings: 4, segments: 36);
        CreateMeshObject("Well_Water", wellRoot.transform, waterMesh, waterMat, new Color(0.12f, 0.45f, 0.68f));

        // 8 Box Colliders membentuk cincin pelindung solid di sekeliling dinding sumur
        var collidersRoot = new GameObject("Well_Colliders");
        collidersRoot.transform.SetParent(wellRoot.transform, false);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            var colGo = new GameObject($"Col_Segment_{i}");
            colGo.transform.SetParent(collidersRoot.transform, false);
            colGo.transform.localPosition = new Vector3(cos * 1.06f, 0.50f, sin * 1.06f);
            colGo.transform.localRotation = Quaternion.Euler(0, -i * 45f, 0);
            var bc = colGo.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.90f, 1.0f, 0.38f);
        }

        // 5. Dua Pilar Kayu Solid (Hand-Hewn Timber Posts)
        GameObject MakeTimber(string name, Vector3 localPos, Vector3 localScale, Quaternion? localRot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(wellRoot.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (localRot.HasValue) go.transform.localRotation = localRot.Value;
            if (woodMat) go.GetComponent<Renderer>().sharedMaterial = woodMat;
            else ApplyColor(go, new Color(0.30f, 0.18f, 0.10f));
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        MakeTimber("Well_Pole_L", new Vector3(-1.05f, 1.725f, 0f), new Vector3(0.18f, 1.65f, 0.18f));
        MakeTimber("Well_Pole_R", new Vector3( 1.05f, 1.725f, 0f), new Vector3(0.18f, 1.65f, 0.18f));

        // 6. Palang Kayu Penopang Atas (Main Timber Crossbeam)
        MakeTimber("Well_Crossbeam", new Vector3(0f, 2.45f, 0f), new Vector3(2.50f, 0.18f, 0.18f));

        // Penyangga siku kayu 45 derajat (Diagonal Braces)
        MakeTimber("Well_Brace_L", new Vector3(-0.78f, 2.25f, 0f), new Vector3(0.09f, 0.42f, 0.09f), Quaternion.Euler(0, 0, -45f));
        MakeTimber("Well_Brace_R", new Vector3( 0.78f, 2.25f, 0f), new Vector3(0.09f, 0.42f, 0.09f), Quaternion.Euler(0, 0,  45f));

        // 7. Penggulung Tali Sumur (Wooden Pulley Spindle & Crank)
        var spindle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spindle.name = "Well_Spindle";
        spindle.transform.SetParent(wellRoot.transform, false);
        spindle.transform.localPosition = new Vector3(0f, 2.22f, 0f);
        spindle.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        spindle.transform.localScale = new Vector3(0.15f, 0.60f, 0.15f);
        if (woodMat) spindle.GetComponent<Renderer>().sharedMaterial = woodMat;
        else ApplyColor(spindle, new Color(0.68f, 0.58f, 0.42f));
        Object.DestroyImmediate(spindle.GetComponent<Collider>());

        // Gulungan tali di tengah poros
        var coil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coil.name = "Well_RopeCoil";
        coil.transform.SetParent(wellRoot.transform, false);
        coil.transform.localPosition = new Vector3(0f, 2.22f, 0f);
        coil.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        coil.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
        ApplyColor(coil, new Color(0.68f, 0.58f, 0.42f));
        Object.DestroyImmediate(coil.GetComponent<Collider>());

        // Engkol pemutar kerekan (Crank Arm & Handle)
        var crankArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crankArm.name = "Well_CrankArm";
        crankArm.transform.SetParent(wellRoot.transform, false);
        crankArm.transform.localPosition = new Vector3(1.15f, 2.12f, 0f);
        crankArm.transform.localScale = new Vector3(0.03f, 0.24f, 0.03f);
        if (metalMat) crankArm.GetComponent<Renderer>().sharedMaterial = metalMat;
        else ApplyColor(crankArm, new Color(0.2f, 0.2f, 0.2f));
        Object.DestroyImmediate(crankArm.GetComponent<Collider>());

        var crankGrip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crankGrip.name = "Well_CrankGrip";
        crankGrip.transform.SetParent(wellRoot.transform, false);
        crankGrip.transform.localPosition = new Vector3(1.15f, 2.00f, 0.08f);
        crankGrip.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        crankGrip.transform.localScale = new Vector3(0.04f, 0.08f, 0.04f);
        if (woodMat) crankGrip.GetComponent<Renderer>().sharedMaterial = woodMat;
        else ApplyColor(crankGrip, new Color(0.35f, 0.22f, 0.12f));
        Object.DestroyImmediate(crankGrip.GetComponent<Collider>());

        // Tali yang Menjulur Lurus ke Bawah Masuk ke Dalam Lubang Sumur
        var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rope.name = "Well_Rope";
        rope.transform.SetParent(wellRoot.transform, false);
        rope.transform.localPosition = new Vector3(0f, 1.40f, 0f);
        rope.transform.localScale = new Vector3(0.035f, 0.80f, 0.035f);
        ApplyColor(rope, new Color(0.68f, 0.58f, 0.42f));
        Object.DestroyImmediate(rope.GetComponent<Collider>());

        // 8. Ember Timba Kayu
        // Ember 1: Menggantung di ujung tali tepat di dalam lubang sumur di atas air
        MakeWoodenBucket(wellRoot.transform, new Vector3(0f, 0.48f, 0f), Quaternion.identity, woodMat, metalMat, darkMat);

        // Ember 2: Berdiri di atas bibir batu sumur untuk suasana pedesaan yang hidup
        MakeWoodenBucket(wellRoot.transform, new Vector3(0.82f, 0.99f, 0.32f), Quaternion.Euler(0, 35f, 0), woodMat, metalMat, darkMat);

        // 9. Atap Segitiga Genteng Sirap (Gable Shingle Roof)
        var roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofL.name = "Well_Roof_L";
        roofL.transform.SetParent(wellRoot.transform, false);
        roofL.transform.localPosition = new Vector3(-0.55f, 2.85f, 0f);
        roofL.transform.localScale = new Vector3(1.35f, 0.08f, 2.20f);
        roofL.transform.localRotation = Quaternion.Euler(0, 0, 32f);
        if (roofMat) roofL.GetComponent<Renderer>().sharedMaterial = roofMat;
        else ApplyColor(roofL, new Color(0.26f, 0.15f, 0.08f));
        Object.DestroyImmediate(roofL.GetComponent<Collider>());

        var roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofR.name = "Well_Roof_R";
        roofR.transform.SetParent(wellRoot.transform, false);
        roofR.transform.localPosition = new Vector3(0.55f, 2.85f, 0f);
        roofR.transform.localScale = new Vector3(1.35f, 0.08f, 2.20f);
        roofR.transform.localRotation = Quaternion.Euler(0, 0, -32f);
        if (roofMat) roofR.GetComponent<Renderer>().sharedMaterial = roofMat;
        else ApplyColor(roofR, new Color(0.26f, 0.15f, 0.08f));
        Object.DestroyImmediate(roofR.GetComponent<Collider>());

        // Nok Puncak Atap (Roof Ridge Cap Beam)
        MakeTimber("Well_Roof_Ridge", new Vector3(0f, 3.22f, 0f), new Vector3(0.12f, 0.12f, 2.25f));

        // 10. Lentera Hangat Sumur (Amber Lantern)
        AddPorchLantern(wellRoot.transform, new Vector3(0f, 2.05f, 0f), new Color(1f, 0.78f, 0.42f), 10f);
    }

    /// <summary>
    /// Membangun tiang lentera gantung kayu tradisional pedesaan (Rustic Timber Hanging Lantern Post)
    /// Dilengkapi pondasi batu kasar, tiang kayu gelondongan berklem besi tempa,
    /// lengan gantung kayu bersiku 45 derajat, rantai besi, dan sangkar lentera berpendar hangat.
    /// </summary>
    static void AddTraditionalVillageLamp(Transform parent, Vector3 pos, float yRotation = 0f)
    {
        var lampGo = new GameObject("TraditionalVillageLamp");
        lampGo.transform.SetParent(parent);
        lampGo.transform.position = pos;
        lampGo.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Muat material tematik berkualitas
        var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Wood.mat");
        var darkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Dark.mat");
        var metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Metal.mat");
        var lampMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Lamp.mat");
        var stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Village_Cobblestone_Road.mat");

        // Helper lokal untuk membuat elemen tiang tanpa collider bawaan
        GameObject MakePart(string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat, Color fallbackColor)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(lampGo.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (mat != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
            else
            {
                ApplyColor(go, fallbackColor);
            }
            return go;
        }

        // 1. Pondasi Batu Kasar (Stone Footing / Pedestal) di atas tanah
        MakePart("StoneBase", PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), new Vector3(0.46f, 0.18f, 0.46f), stoneMat, new Color(0.48f, 0.46f, 0.42f));
        MakePart("BaseRing", PrimitiveType.Cylinder, new Vector3(0, 0.36f, 0), new Vector3(0.24f, 0.04f, 0.24f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        // 2. Tiang Kayu Gelondongan Utama (Weathered Timber Post)
        MakePart("TimberPost", PrimitiveType.Cylinder, new Vector3(0, 1.45f, 0), new Vector3(0.18f, 1.10f, 0.18f), woodMat, new Color(0.26f, 0.16f, 0.09f));

        // 3. Klem Besi Tempa Pengikat Kayu (Upper Forged Iron Band)
        MakePart("IronCollarUpper", PrimitiveType.Cylinder, new Vector3(0, 2.30f, 0), new Vector3(0.21f, 0.04f, 0.21f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        // 4. Mahkota Puncak Tiang (Post Top Finial)
        MakePart("PostCap", PrimitiveType.Cube, new Vector3(0, 2.58f, 0), new Vector3(0.22f, 0.08f, 0.22f), darkMat, new Color(0.20f, 0.12f, 0.07f));

        // 5. Lengan Gantung Kayu Horisontal (Gallows Beam Arm) menjulur ke depan (+Z)
        MakePart("GallowsArm", PrimitiveType.Cube, new Vector3(0, 2.50f, 0.42f), new Vector3(0.12f, 0.12f, 0.84f), woodMat, new Color(0.26f, 0.16f, 0.09f));
        MakePart("ArmEndCap", PrimitiveType.Cube, new Vector3(0, 2.50f, 0.85f), new Vector3(0.14f, 0.14f, 0.04f), darkMat, new Color(0.20f, 0.12f, 0.07f));

        // 6. Penyangga Siku Kayu 45 Derajat (Diagonal Support Strut)
        var strut = MakePart("DiagonalStrut", PrimitiveType.Cube, new Vector3(0, 2.26f, 0.24f), new Vector3(0.09f, 0.09f, 0.42f), woodMat, new Color(0.26f, 0.16f, 0.09f));
        strut.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

        // 7. Pengait & Rantai Besi Tempa (Forged Iron Hook & Suspension Chain)
        MakePart("IronHook", PrimitiveType.Cube, new Vector3(0, 2.41f, 0.72f), new Vector3(0.04f, 0.08f, 0.04f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("ChainLink1", PrimitiveType.Cylinder, new Vector3(0, 2.33f, 0.72f), new Vector3(0.03f, 0.05f, 0.03f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("ChainLink2", PrimitiveType.Cylinder, new Vector3(0, 2.24f, 0.72f), new Vector3(0.03f, 0.05f, 0.03f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        // 8. Sangkar Lentera Tradisional Gantung (Hanging Traditional Village Lantern)
        float lz = 0.72f;
        // Ring gantung atas lentera
        MakePart("LanternRing", PrimitiveType.Cylinder, new Vector3(0, 2.19f, lz), new Vector3(0.08f, 0.02f, 0.08f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        // Atap limas pelindung cuaca (Pyramid Canopy Roof)
        MakePart("RoofTop", PrimitiveType.Cube, new Vector3(0, 2.16f, lz), new Vector3(0.24f, 0.04f, 0.24f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("RoofEaves", PrimitiveType.Cube, new Vector3(0, 2.12f, lz), new Vector3(0.36f, 0.03f, 0.36f), metalMat, new Color(0.22f, 0.22f, 0.22f));

        // Alas lentera (Base Tray)
        MakePart("LanternBase", PrimitiveType.Cube, new Vector3(0, 1.83f, lz), new Vector3(0.30f, 0.04f, 0.30f), metalMat, new Color(0.22f, 0.22f, 0.22f));
        MakePart("BaseFinial", PrimitiveType.Cylinder, new Vector3(0, 1.79f, lz), new Vector3(0.06f, 0.04f, 0.06f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        // 4 Pilar Sudut Rangka Besi (4 Corner Struts)
        float[] cOffsets = { -0.12f, 0.12f };
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                MakePart($"Strut_{i}_{j}", PrimitiveType.Cube, new Vector3(cOffsets[i], 1.975f, lz + cOffsets[j]), new Vector3(0.022f, 0.27f, 0.022f), metalMat, new Color(0.18f, 0.18f, 0.18f));
            }
        }

        // Inti Kaca Berpendar Lentera (Emissive Amber Glass Core)
        var glassCore = MakePart("GlassCore", PrimitiveType.Cylinder, new Vector3(0, 1.975f, lz), new Vector3(0.18f, 0.13f, 0.18f), lampMat, new Color(1f, 0.85f, 0.45f));
        var glassRend = glassCore.GetComponent<Renderer>();

        // 9. Lampu Titik Amber Hangat (Warm Amber Point Light)
        var lightGo = new GameObject("LanternPointLight");
        lightGo.transform.SetParent(lampGo.transform);
        lightGo.transform.localPosition = new Vector3(0, 1.975f, lz);

        var pLight = lightGo.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.74f, 0.38f); // Cahaya lilin/minyak hangat
        pLight.intensity = 2.2f;
        pLight.range = 12f;
        pLight.shadows = LightShadows.Soft;

        // 10. NightLight System (Otomatis redup di siang hari, berpendar & berkerlip alami saat senja/malam)
        var nl = lampGo.AddComponent<NightLight>();
        nl.Configure(pLight, glassRend, new Color(1f, 0.74f, 0.38f), 2.2f, true);

        // 11. Capsule Collider pada tiang agar pemain dapat menabrak tiang secara realistis
        var col = lampGo.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1.35f, 0);
        col.height = 2.7f;
        col.radius = 0.25f;
    }

    /// <summary>
    /// Overload retro-kompatibel untuk tiang lentera jalan
    /// </summary>
    static void AddStreetLamp(Transform parent, Vector3 pos)
    {
        AddTraditionalVillageLamp(parent, pos, 0f);
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

        float s = 1f;

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
        var leaf = InstantiateArtModelLocal("Door_1_Flat", new Vector3(-1.45f, 0, -1.95f), Quaternion.Euler(0, -35f, 0), Vector3.one, modelContainer.transform, matWood);
        if (leaf) foreach (var c in leaf.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

        // Segmen Kanan (X = 1): Jendela Lebar
        InstantiateArtModelLocal(winWallFbx, new Vector3(1f, 0, -2f), Quaternion.identity, Vector3.one, modelContainer.transform, matWall);
        // Daun Jendela Kayu Terbuka (Window Shutters)
        var shutters = InstantiateArtModelLocal("WindowShutters_Wide_Flat_Open", new Vector3(1f, 0, -2f), Quaternion.identity, Vector3.one, modelContainer.transform, matWood);
        if (shutters) foreach (var c in shutters.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

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

        // ── 12. PERABOT & INTERIOR TRADISIONAL ──
        var interiorRoot = new GameObject("Interior_Furnishings");
        interiorRoot.transform.SetParent(modelContainer.transform, false);
        interiorRoot.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Pintu modular menghadap -Z lokal
        int flags = small
            ? (ProceduralHouseBuilder.FChest | ProceduralHouseBuilder.FMat)
            : ProceduralHouseBuilder.FAll;
        ProceduralHouseBuilder.FurnishInterior(interiorRoot, 0f, 0f, 1.8f, 1.8f, 0.02f, 1.0f, flags, 2.8f);

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

        bool isWalkSurfaceOrFoliage = fbxName.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     fbxName.IndexOf("Cobble", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     fbxName.IndexOf("Grass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     fbxName.IndexOf("Flower", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (isWalkSurfaceOrFoliage)
        {
            foreach (var col in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);
        }
        else
        {
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
                if (!mf.GetComponent<Collider>())
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
        }

        return instance;
    }

    // Helper: Dapatkan material MedievalVillage berdasarkan nama
    static Material GetMedievalMat(string matName)
    {
        string path = $"Assets/_Project/Art/MedievalVillage/Materials/{matName}.mat";
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    // Helper: Buat / dapatkan material jalan batu paving desa (Cobblestone)
    static Material GetOrCreateVillageRoadMaterial()
    {
        string roadMatPath = "Assets/_Project/Art/MedievalVillage/Materials/M_Village_Cobblestone_Road.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(roadMatPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "M_Village_Cobblestone_Road";
            AssetDatabase.CreateAsset(mat, roadMatPath);
        }

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/MedievalVillage/Textures/T_UnevenBrick_BaseColor.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/MedievalVillage/Textures/T_UnevenBrick_Normal.png");

        if (albedo) mat.SetTexture("_BaseMap", albedo);
        if (normal)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 0.9f);
        }
        mat.SetColor("_BaseColor", new Color(0.92f, 0.90f, 0.86f, 1f));
        mat.SetFloat("_Smoothness", 0.15f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Helper: Buat jalan batu terusan (continuous paved road) tanpa celah rumput
    static GameObject CreateContinuousPavedRoad(
        Transform parent,
        string name,
        Vector3 start,
        Vector3 end,
        float width,
        Material roadMat,
        float uvScale = 2.5f)
    {
        var roadGO = new GameObject(name);
        roadGO.transform.SetParent(parent);
        roadGO.transform.position = Vector3.zero;
        roadGO.transform.rotation = Quaternion.identity;

        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length < 0.05f) return roadGO;

        Vector3 forward = delta.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float topY = 0.025f; // Permukaan jalan sejajar alun-alun
        float botY = -0.05f; // Masuk ke dalam tanah agar tepi tidak mengambang

        Vector3 halfW = right * (width * 0.5f);
        Vector3 p0 = start - halfW;
        Vector3 p1 = start + halfW;
        Vector3 p2 = end - halfW;
        Vector3 p3 = end + halfW;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        // 1. TOP FACE (Permukaan jalan yang diinjak)
        int idx = verts.Count;
        verts.Add(new Vector3(p0.x, topY, p0.z));
        verts.Add(new Vector3(p1.x, topY, p1.z));
        verts.Add(new Vector3(p2.x, topY, p2.z));
        verts.Add(new Vector3(p3.x, topY, p3.z));

        // World-aligned UV planar mapping agar sambungan dan percabangan 100% mulus tanpa celah atau stretch
        uvs.Add(new Vector2(p0.x / uvScale, p0.z / uvScale));
        uvs.Add(new Vector2(p1.x / uvScale, p1.z / uvScale));
        uvs.Add(new Vector2(p2.x / uvScale, p2.z / uvScale));
        uvs.Add(new Vector2(p3.x / uvScale, p3.z / uvScale));

        norms.Add(Vector3.up);
        norms.Add(Vector3.up);
        norms.Add(Vector3.up);
        norms.Add(Vector3.up);

        tris.Add(idx + 0);
        tris.Add(idx + 2);
        tris.Add(idx + 1);

        tris.Add(idx + 1);
        tris.Add(idx + 2);
        tris.Add(idx + 3);

        // 2. LEFT SIDE SKIRT (Tepi kiri jalan)
        idx = verts.Count;
        verts.Add(new Vector3(p0.x, topY, p0.z));
        verts.Add(new Vector3(p2.x, topY, p2.z));
        verts.Add(new Vector3(p0.x, botY, p0.z));
        verts.Add(new Vector3(p2.x, botY, p2.z));

        uvs.Add(new Vector2(p0.x / uvScale, p0.z / uvScale));
        uvs.Add(new Vector2(p2.x / uvScale, p2.z / uvScale));
        uvs.Add(new Vector2(p0.x / uvScale, p0.z / uvScale));
        uvs.Add(new Vector2(p2.x / uvScale, p2.z / uvScale));

        norms.Add(-right);
        norms.Add(-right);
        norms.Add(-right);
        norms.Add(-right);

        tris.Add(idx + 0);
        tris.Add(idx + 2);
        tris.Add(idx + 1);

        tris.Add(idx + 1);
        tris.Add(idx + 2);
        tris.Add(idx + 3);

        // 3. RIGHT SIDE SKIRT (Tepi kanan jalan)
        idx = verts.Count;
        verts.Add(new Vector3(p1.x, topY, p1.z));
        verts.Add(new Vector3(p3.x, topY, p3.z));
        verts.Add(new Vector3(p1.x, botY, p1.z));
        verts.Add(new Vector3(p3.x, botY, p3.z));

        uvs.Add(new Vector2(p1.x / uvScale, p1.z / uvScale));
        uvs.Add(new Vector2(p3.x / uvScale, p3.z / uvScale));
        uvs.Add(new Vector2(p1.x / uvScale, p1.z / uvScale));
        uvs.Add(new Vector2(p3.x / uvScale, p3.z / uvScale));

        norms.Add(right);
        norms.Add(right);
        norms.Add(right);
        norms.Add(right);

        tris.Add(idx + 0);
        tris.Add(idx + 1);
        tris.Add(idx + 2);

        tris.Add(idx + 2);
        tris.Add(idx + 1);
        tris.Add(idx + 3);

        // 4. START CAP (Ujung awal jalan)
        idx = verts.Count;
        verts.Add(new Vector3(p0.x, topY, p0.z));
        verts.Add(new Vector3(p1.x, topY, p1.z));
        verts.Add(new Vector3(p0.x, botY, p0.z));
        verts.Add(new Vector3(p1.x, botY, p1.z));

        uvs.Add(new Vector2(p0.x / uvScale, p0.z / uvScale));
        uvs.Add(new Vector2(p1.x / uvScale, p1.z / uvScale));
        uvs.Add(new Vector2(p0.x / uvScale, p0.z / uvScale));
        uvs.Add(new Vector2(p1.x / uvScale, p1.z / uvScale));

        norms.Add(-forward);
        norms.Add(-forward);
        norms.Add(-forward);
        norms.Add(-forward);

        tris.Add(idx + 0);
        tris.Add(idx + 1);
        tris.Add(idx + 2);

        tris.Add(idx + 2);
        tris.Add(idx + 1);
        tris.Add(idx + 3);

        // 5. END CAP (Ujung akhir jalan)
        idx = verts.Count;
        verts.Add(new Vector3(p2.x, topY, p2.z));
        verts.Add(new Vector3(p3.x, topY, p3.z));
        verts.Add(new Vector3(p2.x, botY, p2.z));
        verts.Add(new Vector3(p3.x, botY, p3.z));

        uvs.Add(new Vector2(p2.x / uvScale, p2.z / uvScale));
        uvs.Add(new Vector2(p3.x / uvScale, p3.z / uvScale));
        uvs.Add(new Vector2(p2.x / uvScale, p2.z / uvScale));
        uvs.Add(new Vector2(p3.x / uvScale, p3.z / uvScale));

        norms.Add(forward);
        norms.Add(forward);
        norms.Add(forward);
        norms.Add(forward);

        tris.Add(idx + 0);
        tris.Add(idx + 2);
        tris.Add(idx + 1);

        tris.Add(idx + 1);
        tris.Add(idx + 2);
        tris.Add(idx + 3);

        var mesh = new Mesh();
        mesh.name = $"Mesh_{name}";
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        var mf = roadGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = roadGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = roadMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        return roadGO;
    }

    // Helper: Buat pelataran alun-alun melingkar batu paving (Central Plaza)
    static GameObject CreateCircularPlaza(
        Transform parent,
        string name,
        Vector3 center,
        float radius,
        Material roadMat,
        float uvScale = 2.5f)
    {
        var plazaGO = new GameObject(name);
        plazaGO.transform.SetParent(parent);
        plazaGO.transform.position = Vector3.zero;
        plazaGO.transform.rotation = Quaternion.identity;

        float topY = 0.025f;
        float botY = -0.05f;
        int segments = 48;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        // Center vertex
        int centerIdx = verts.Count;
        verts.Add(new Vector3(center.x, topY, center.z));
        uvs.Add(new Vector2(center.x / uvScale, center.z / uvScale));
        norms.Add(Vector3.up);

        // Perimeter top vertices: indices 1 to segments + 1
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) * Mathf.PI * 2f / segments;
            float px = center.x + Mathf.Cos(angle) * radius;
            float pz = center.z + Mathf.Sin(angle) * radius;

            verts.Add(new Vector3(px, topY, pz));
            uvs.Add(new Vector2(px / uvScale, pz / uvScale));
            norms.Add(Vector3.up);
        }

        // Top disc triangles (fan)
        for (int i = 1; i <= segments; i++)
        {
            tris.Add(centerIdx);
            tris.Add(i + 1);
            tris.Add(i);
        }

        // Perimeter skirt (drop into ground)
        int skirtStartIdx = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) * Mathf.PI * 2f / segments;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float px = center.x + cos * radius;
            float pz = center.z + sin * radius;

            // Top
            verts.Add(new Vector3(px, topY, pz));
            uvs.Add(new Vector2(px / uvScale, pz / uvScale));
            norms.Add(new Vector3(cos, 0f, sin));

            // Bot
            verts.Add(new Vector3(px, botY, pz));
            uvs.Add(new Vector2(px / uvScale, pz / uvScale));
            norms.Add(new Vector3(cos, 0f, sin));
        }

        for (int i = 0; i < segments; i++)
        {
            int t0 = skirtStartIdx + i * 2;
            int b0 = t0 + 1;
            int t1 = skirtStartIdx + (i + 1) * 2;
            int b1 = t1 + 1;

            tris.Add(t0);
            tris.Add(t1);
            tris.Add(b0);

            tris.Add(b0);
            tris.Add(t1);
            tris.Add(b1);
        }

        var mesh = new Mesh();
        mesh.name = $"Mesh_{name}";
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        var mf = plazaGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = plazaGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = roadMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        return plazaGO;
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
            // Di teras Balai Desa (0, 0.9, 6), menyusuri tangga Balai Desa dan Alun-Alun
            ("NPC_KepalaDesa",
             ProceduralNpcBuilder.NpcStyle.Merchant,
             new Vector3(0f, 0.9f, 6.0f), 180f,
             "Selamat datang, petualang! Desa Eldara dalam bahaya besar. Saat matahari terbenam, pasukan monster akan menyerbu dari rimba kegelapan hutan. Persiapkan persenjataan dan dirikan menara panah!",
             new Vector3[] {
                 new Vector3(0f, 0.9f,  6.0f), // Teras Balai Desa
                 new Vector3(0f, 0.14f, 10.0f), // Pelataran Alun-Alun
                 new Vector3(0f, 0.14f, 18.0f), // Jalan Utama Utara
                 new Vector3(0f, 0.14f, 10.0f)  // Kembali ke Alun-Alun
             }),

            // 2. Petani Ladang (Farmer: Caping jerami lebar, rompi kerja, kumis melengkung, & garpu rumput)
            // Di persimpangan jalan lingkar barat laut dekat petak sawah
            ("NPC_Petani",
             ProceduralNpcBuilder.NpcStyle.Farmer,
             new Vector3(-20f, 0.14f, 20f), 180f,
             "Aduh gawat, kawanan serigala buas kemarin malam merusak pagar gandumku! Tolong tebang balok kayu di tepi hutan rimba untuk memperbaiki lumbung kami.",
             new Vector3[] {
                 new Vector3(-20f, 0.14f, 20f), // Persimpangan Jalan Barat-Lingkar
                 new Vector3(-42f, 0.14f, 20f), // Jalur Sawah Barat
                 new Vector3(-42f, 0.14f, 42f), // Ujung Sawah Utara
                 new Vector3(-20f, 0.14f, 20f)  // Kembali ke Jalan Lingkar
             }),

            // 3. Pandai Besi & Ahli Senjata (Guard/Smith: Zirah baja tempa, sabuk kulit, & pedang baja)
            // Di persimpangan jalan timur & jalan lingkar
            ("NPC_PandaiBesi",
             ProceduralNpcBuilder.NpcStyle.Guard,
             new Vector3(20f, 0.14f, 0f), 180f,
             "Salam, ksatria! Aku butuh bijih besi murni dan batu keras dari area tambang timur. Bawakan padaku, dan aku akan merakit menara panah yang kokoh!",
             new Vector3[] {
                 new Vector3(20f, 0.14f,  0f), // Jalan Lingkar Timur
                 new Vector3(42f, 0.14f,  0f), // Jalan Samping Timur
                 new Vector3(20f, 0.14f, 20f), // Jalan Lingkar Timur Laut
                 new Vector3(20f, 0.14f,  0f)  // Kembali ke Jalan Utama Timur
             }),

            // 4. Dukun Mistis Pelindung Segel (Wizard: Topi kerucut sihir, janggut putih, & tongkat kristal)
            // Di jalan lingkar selatan
            ("NPC_Dukun",
             ProceduralNpcBuilder.NpcStyle.Wizard,
             new Vector3(0f, 0.14f, -20f), 0f,
             "Altar Segel Kuno di utara berdenyut dengan energi gelap... Jika kamu berani mengaktifkannya saat malam tiba, Raja Monster akan terpanggil. Pastikan menara bentengmu sudah siap!",
             new Vector3[] {
                 new Vector3( 0f, 0.14f, -20f), // Jalan Lingkar Selatan
                 new Vector3(20f, 0.14f, -20f), // Sudut Tenggara Lingkar
                 new Vector3( 0f, 0.14f, -12f), // Pintu Masuk Alun-Alun Selatan
                 new Vector3( 0f, 0.14f, -20f)  // Kembali ke Selatan
             }),

            // 5. Gadis Desa Ramah (Woman: Gaun zamrud, kepang rambut panjang, celemek putih)
            // Di Alun-Alun tengah (-6, 0, 2), mengitari sumur desa dan pasar
            ("NPC_GadisDesa",
             ProceduralNpcBuilder.NpcStyle.Woman,
             new Vector3(0f, 0f, 4.5f), 180f,
             "Halo petualang! Udara di desa Eldara hari ini begitu sejuk dan damai. Berhati-hatilah saat senja menjelang ya, kabut hutan sering membawa bahaya tak terduga!",
             new Vector3[] {
                 new Vector3( 0f, 0f,  4.5f), // Sisi Utara Sumur Kuno
                 new Vector3( 4.5f, 0f, 0f),  // Sisi Timur Sumur Kuno
                 new Vector3( 0f, 0f, -4.5f), // Sisi Selatan Sumur Kuno
                 new Vector3(-4.5f, 0f, 0f)   // Sisi Barat Sumur Kuno
             }),

            // 6. Kapten Penjaga Benteng (Guard: Helm baja, jubah perang, pedang ksatria)
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

            // 4 Penjuru Sudut Benteng Utama (Sudut Pagar X=±62, Z=-54 s/d 50)
            (new Vector3(-62f, 0f,  50f), "Tower_NorthWest"),
            (new Vector3( 62f, 0f,  50f), "Tower_NorthEast"),
            (new Vector3(-62f, 0f, -54f), "Tower_SouthWest"),
            (new Vector3( 62f, 0f, -54f), "Tower_SouthEast"),

            // Sisi Sayap Tengah
            (new Vector3(-62f, 0f,   7.5f), "Tower_WestFlank"),
            (new Vector3( 62f, 0f,   7.5f), "Tower_EastFlank"),
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

        // Spike Traps di Jalur Masuk Gerbang Utama
        var trapPositions = new Vector3[]
        {
            new Vector3( 0f, 0.05f, -50f),
            new Vector3(-2.5f, 0.05f, -48f),
            new Vector3( 2.5f, 0.05f, -48f),
            new Vector3( 0f, 0.05f, -46f),
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

        // Barikade Pagar Penghalang di Jalur Masuk Gerbang Utama
        InstantiateArtModel("EA03_Village_Fence_01a", new Vector3(-2.8f, 0f, -48f), Quaternion.Euler(0, 20f, 0), Vector3.one, towerRoot.transform, slavicMat);
        InstantiateArtModel("EA03_Village_Fence_01a", new Vector3( 2.8f, 0f, -48f), Quaternion.Euler(0, -20f, 0), Vector3.one, towerRoot.transform, slavicMat);
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

        // 1. Pohon-pohon peneduh & taman alami di dalam desa
        // - Alun-Alun Pusat, jalan raya utama, dan jalan setapak steril dari rintangan pohon
        // - Ditempatkan di 4 taman sudut plaza dan pekarangan belakang tiap rumah sebagai peneduh alami
        var innerTrees = new Vector3[]
        {
            // 4 Pohon Peneduh Sudut Alun-Alun Pusat (Taman Terbuka)
            new Vector3(-11f, 0f,  11f), new Vector3( 11f, 0f,  11f),
            new Vector3(-11f, 0f, -11f), new Vector3( 11f, 0f, -11f),

            // Kompleks Pemerintahan & Adat Utara
            new Vector3(-36f, 0f, 36f), // Taman samping Rumah Gadang
            new Vector3( 36f, 0f, 36f), // Taman samping Pendopo Joglo
            new Vector3(  0f, 0f, 46f), // Hutan lindung belakang Balai Desa

            // Kawasan Pertanian & Danau Barat
            new Vector3(-22f, 0f,  25f), // Kebun belakang Rumah Petani
            new Vector3(-40f, 0f,  25f), // Ladang belakang Barn Gandum
            new Vector3(-22f, 0f, -25f), // Pekarangan belakang Chalet
            new Vector3(-40f, 0f, -25f), // Taman tepian Danau

            // Kawasan Pandai Besi & Pengrajin Timur
            new Vector3( 22f, 0f,  25f), // Halaman belakang Bengkel Tempa
            new Vector3( 38f, 0f,  25f), // Area belakang Gudang Logistik
            new Vector3( 22f, 0f, -25f), // Kebun belakang Log Cabin
            new Vector3( 38f, 0f, -25f), // Taman belakang Villa Modern

            // Kawasan Gerbang Selatan & Penginapan
            new Vector3(-18f, 0f, -38f), // Samping Penginapan A-Frame
            new Vector3( 18f, 0f, -38f), // Taman Bambu/Bonsai Minka Dukun
            new Vector3(-18f, 0f, -54f), // Pohon rindang dekat Gerbang Kiri
            new Vector3( 18f, 0f, -54f)  // Pohon rindang dekat Gerbang Kanan
        };

        int innerIndex = 0;
        foreach (var pos in innerTrees)
        {
            innerIndex++;
            float scale = Random.Range(1.1f, 1.35f);
            GameObject treeObj;

            // Variasi pohon peneduh alun-alun: Pohon Ek Rimbun & Pohon Birch
            if (innerIndex % 2 == 0)
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
                if (sway)
                {
                    sway.enableFallingLeaves = true;
                }

                // Semak liar di pangkal pohon
                InstantiateArtModel("EA03_Nature_TrunkPlant_01a", pos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * 0.9f, natureRoot.transform, slavicMat);

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

        // 2. Hutan Rimba Luar (Di Luar Perimeter Desa)
        // Menggunakan BuildNaturalForest dari Beautify: berumpun alami, ada padang terbuka, spesies mengelompok, bebas jalan & tambang
        BuildNaturalForest(natureRoot.transform, barkMat, birchBarkMat, summerLeafMat, springLeafMat, autumnLeafMat, slavicMat, bushModels, rockModels);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8b. HAMPARAN PADANG RUMPUT 3D REALISTIS & BUNGA LIAR TIMBUL
    // ─────────────────────────────────────────────────────────────────────
    static void SetupRealisticGrassFields(Material grassMat)
    {
        GameObject grassRoot = GameObject.Find("--- GRASS FIELDS ---");
        if (grassRoot) Object.DestroyImmediate(grassRoot);

        grassRoot = new GameObject("--- GRASS FIELDS ---");

        var flowerRoot = new GameObject("--- 3D WILDFLOWERS ---");
        flowerRoot.transform.SetParent(grassRoot.transform);

        if (!grassMat) return;

        // 1. Tepi Boulevard Jalan Utama (Bordir Rumput Rimbun & Bunga Liar 3D Timbul Sepanjang Jalan)
        // a. Jalan Utara (Menuju Balai Desa, Z = 9.5 s/d 38)
        for (float z = 10f; z <= 38f; z += 3.5f)
        {
            RealisticGrassBuilder.BuildGrassClump(new Vector3(-2.8f, 0, z), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3( 2.8f, 0, z), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(-2.9f, 0, z), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.RedPoppy);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3( 2.9f, 0, z), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.WhiteDaisy);
        }

        // b. Jalan Selatan (Menuju Gerbang Benteng, Z = -10 s/d -46)
        for (float z = -10f; z >= -46f; z -= 3.5f)
        {
            RealisticGrassBuilder.BuildGrassClump(new Vector3(-2.8f, 0, z), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3( 2.8f, 0, z), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(-2.9f, 0, z), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.GoldenDandelion);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3( 2.9f, 0, z), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.MysticPurple);
        }

        // c. Jalan Timur (Menuju Kawasan Pengrajin, X = 10 s/d 64)
        for (float x = 10f; x <= 64f; x += 3.5f)
        {
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0,  2.8f), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0, -2.8f), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0,  2.9f), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.SkyBluebell);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0, -2.9f), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.GoldenDandelion);
        }

        // d. Jalan Barat (Menuju Pertanian & Kincir Angin, X = -10 s/d -64)
        for (float x = -10f; x >= -64f; x -= 3.5f)
        {
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0,  2.8f), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0, -2.8f), 8, 1.5f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0,  2.9f), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.WhiteDaisy);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0, -2.9f), 4, 1.2f, flowerRoot.transform, RealisticFlowerBuilder.FlowerColor.RedPoppy);
        }

        // 2. Taman Bunga Tematik di 4 Sudut Plaza Alun-Alun Pusat
        var innerTrees = new Vector3[]
        {
            new Vector3(-11f, 0f,  11f), new Vector3( 11f, 0f,  11f),
            new Vector3(-11f, 0f, -11f), new Vector3( 11f, 0f, -11f),
        };
        foreach (var tp in innerTrees)
        {
            RealisticGrassBuilder.BuildGrassClump(tp, 12, 3.2f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(tp, 6, 2.8f, flowerRoot.transform);
        }

        // 3. Sepanjang Garis Perimeter Pagar Kayu Benteng (Perimeter 160x106m)
        for (float x = -57f; x <= 57f; x += 8f)
        {
            if (Mathf.Abs(x) < 6f) continue;
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0,  50f), 8, 3.5f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0, -52f), 8, 3.5f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0,  50f), 4, 3.0f, flowerRoot.transform);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0, -52f), 4, 3.0f, flowerRoot.transform);
        }
        for (float z = -50f; z <= 48f; z += 8f)
        {
            if (Mathf.Abs(z) < 6f) continue;
            RealisticGrassBuilder.BuildGrassClump(new Vector3( 60f, 0, z), 8, 3.5f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3(-60f, 0, z), 8, 3.5f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3( 60f, 0, z), 4, 3.0f, flowerRoot.transform);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(-60f, 0, z), 4, 3.0f, flowerRoot.transform);
        }

        // 4. Hamparan Padang Rumput Liar Luar Benteng Desa (R = 94m s/d 145m)
        int meadowCount = 100;
        for (int i = 0; i < meadowCount; i++)
        {
            float ang = (i / (float)meadowCount) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(94f, 145f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            // Lewati jika memotong 4 koridor jalan utama (|X| < 6 atau |Z| < 6)
            if (Mathf.Abs(pos.x) < 6f || Mathf.Abs(pos.z) < 6f) continue;
            RealisticGrassBuilder.BuildGrassClump(pos, 12, 4.5f, grassRoot.transform, grassMat);
            if (i % 2 == 0)
                RealisticFlowerBuilder.BuildFlowerClump(pos, 5, 3.5f, flowerRoot.transform);
        }
        // Cincin luar (60-105m) — semakin lebat di dekat hutan
        int meadowOuter = 110;
        for (int i = 0; i < meadowOuter; i++)
        {
            float ang = (i / (float)meadowOuter) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(60f, 105f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            RealisticGrassBuilder.BuildGrassClump(pos, 14, 5.5f, grassRoot.transform, grassMat);
            if (i % 2 == 0)
                RealisticFlowerBuilder.BuildFlowerClump(pos, 6, 4.0f, flowerRoot.transform);
        }

        // 6. Hamparan Rumput Rimbun & Taburan Bunga 3D Timbul di Seluruh Lapangan Desa
        for (int i = 0; i < 160; i++)
        {
            float x = Random.Range(-32f, 32f);
            float z = Random.Range(-32f, 32f);

            // Filter agar tidak spawn di atas alun-alun atau badan jalan
            Vector3 pos = new Vector3(x, 0f, z);
            if (RealisticGrassBuilder.IsPositionOnStoneOrRoad(pos)) continue;

            RealisticGrassBuilder.BuildGrassClump(pos, 8, 2.5f, grassRoot.transform, grassMat);

            if (Random.value < 0.65f)
            {
                RealisticFlowerBuilder.BuildFlowerClump(pos, 4, 2.0f, flowerRoot.transform);
            }
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

        // ── A. AREA TAMBANG BIJIH BESI — Di luar benteng, dekat gerbang timur (X=78+)
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_1", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(78f, 0f, 14f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_2", "EA03_Environment_Rock_CatHead_01b", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(84f, 0f, 20f), Quaternion.Euler(0, 45f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_3", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(90f, 0f, 14f), Quaternion.Euler(0, 110f, 0), new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.4f, 0.4f, 0.5f));

        // ── B. AREA BALOK KAYU — Di luar benteng, dekat gerbang barat (X=-78-)
        PlaceArtResource(itemsRoot.transform, "Wood_Log_1", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-78f, 0f, 18f), Quaternion.identity, Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_2", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-84f, 0f, 24f), Quaternion.Euler(0, 80f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_3", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-90f, 0f, 18f), Quaternion.Euler(0, 30f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));

        // ── C. BATU ALAM TAMBANG (STONE BOULDERS) — Di luar perimeter desa
        PlaceArtResource(itemsRoot.transform, "Stone_Rock_1", "EA03_Environment_Rock_Head_01a", "stone",
            "Tekan E untuk memecah Batu", new Vector3(56f, 0f, 12f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f), slavicMat, new Color(0.55f, 0.55f, 0.55f));
        PlaceArtResource(itemsRoot.transform, "Stone_Rock_2", "EA03_Environment_Rock_Head_01b", "stone",
            "Tekan E untuk memecah Batu", new Vector3(-56f, 0f, -92f), Quaternion.Euler(0, 50f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.55f, 0.55f, 0.55f));

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
        foreach (var fish in Object.FindObjectsByType<FishController>(FindObjectsSortMode.None))
        {
            if (fish) Object.DestroyImmediate(fish.gameObject);
        }

        GameObject faunaRoot = GameObject.Find("--- FAUNA & ANIMALS ---");
        if (faunaRoot) Object.DestroyImmediate(faunaRoot);

        faunaRoot = new GameObject("--- FAUNA & ANIMALS ---");

        // 0. Anjing Setia Desa (Milo - Model & Walk Cycle dari dog_walk.py)
        // Ditempatkan di Alun-Alun Pusat di depan pemain & dekat Sumur Desa agar langsung terlihat jelas
        ProceduralDogBuilder.BuildDog(faunaRoot.transform, new Vector3(2.2f, 0f, -3.8f), Quaternion.Euler(0, -135f, 0), "Milo");

        // 0b. Kucing Desa — Luna & Oliver
        // Model: "Rigged and animated Cat" oleh Blend Swap #86110, CC-BY 3.0
        // Luna — Kucing abu-abu setia di dekat sumur alun-alun
        AnimalAssetSetup.SpawnCat(new Vector3(-3.5f, 0f, 2f),
            Quaternion.Euler(0, 45f, 0), faunaRoot.transform,
            "Luna", "Meoow~ Rubuh aku dong, petualang!", altColor: false);
        // Oliver — Kucing oranye liar yang sering tidur di dekat tiang lampu alun-alun
        AnimalAssetSetup.SpawnCat(new Vector3(9f, 0f, 9f),
            Quaternion.Euler(0, -120f, 0), faunaRoot.transform,
            "Oliver", "Purrr... kamu menggangguku tidur!", altColor: true);

        // 0c. Ayam Jago Desa (Eldara Rooster prosedural 11 klip animasi - port skrip Blender v3)
        // Spawn ayam jago di Alun-Alun, kawasan pertanian, pandai besi, dan gerbang benteng
        void SpawnVillageRooster(Vector3 pos, float yRot, string name, Eldara.RoosterClip clip)
        {
            var roosterGo = new GameObject(name);
            roosterGo.transform.SetParent(faunaRoot.transform);
            roosterGo.transform.position = pos;
            roosterGo.transform.rotation = Quaternion.Euler(0, yRot, 0);
            var rooster = roosterGo.AddComponent<Eldara.EldaraRooster>();
            var so = new SerializedObject(rooster);
            var p = so.FindProperty("startClip");
            if (p != null) p.enumValueIndex = (int)clip;
            so.ApplyModifiedProperties();
        }

        // Jago Alun-Alun (di dekat pasar & sumur alun-alun)
        SpawnVillageRooster(new Vector3(-4f, 0f, -3f), 65f, "Rooster_Jago_AlunAlun", Eldara.RoosterClip.Idle);
        // Jago Pertanian Barat (di pekarangan lumbung & rumah petani)
        SpawnVillageRooster(new Vector3(-20f, 0f, 8f), -45f, "Rooster_Jago_Pertanian", Eldara.RoosterClip.Peck);
        // Jago Pengrajin Timur (di halaman bengkel pandai besi)
        SpawnVillageRooster(new Vector3(20f, 0f, 8f), 135f, "Rooster_Jago_Pengrajin", Eldara.RoosterClip.Walk);
        // Jago Gerbang Selatan (di pelataran gerbang benteng selatan)
        SpawnVillageRooster(new Vector3(4f, 0f, -44f), -120f, "Rooster_Jago_Gerbang", Eldara.RoosterClip.Crow);

        // 1. Kawanan Serigala Rimba (Di hutan lebat luar pagar desa)
        // Alpha Wolf: Hutan Utara dekat Altar Kuno
        AnimalAssetSetup.SpawnWolf(new Vector3(2f, 0f, 72f), Quaternion.Euler(0, 180f, 0), faunaRoot.transform, "Serigala Rimba Alpha");
        // Beta Wolf 1: Hutan Barat
        AnimalAssetSetup.SpawnWolf(new Vector3(-78f, 0f, 20f), Quaternion.Euler(0, 90f, 0), faunaRoot.transform, "Serigala Rimba Barat");
        // Beta Wolf 2: Hutan Timur
        AnimalAssetSetup.SpawnWolf(new Vector3(78f, 0f, 20f), Quaternion.Euler(0, -90f, 0), faunaRoot.transform, "Serigala Rimba Timur");

        // 5. Aquila - Elang Perkasa Penjaga Langit (Melayang tinggi melingkar di angkasa desa)
        AnimalAssetSetup.SpawnEagle(new Vector3(0f, 28f, 0f), faunaRoot.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9c. EKSPANSI PETA GELOMBANG 1 — 4 ZONA BARU (Total dunia 500x500m)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Membangun 4 zona dunia (Gelombang 1) di luar hutan desa:
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
        var roadMat = GetOrCreateVillageRoadMaterial();
        var expandRoads = new GameObject("Expansion_Road_Network");
        expandRoads.transform.SetParent(expandRoot.transform);

        // Jalan Utama Utara ke Desa Pegunungan (Bypass elegan sayap timur Balai Desa & Altar Mistis, lebar 3.8m)
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_Bypass1",
            new Vector3(4.2f, 0, 34f), new Vector3(14f, 0, 44f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_Foothills",
            new Vector3(14f, 0, 44f), new Vector3(14f, 0, 88f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_ValleyCurve",
            new Vector3(14f, 0, 88f), new Vector3(0f, 0, 105f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_Highway",
            new Vector3(0f, 0, 105f), new Vector3(0f, 0, 160f), 4.0f, roadMat, 2.5f);

        // Jalan Utama Selatan ke Gerbang Benteng Kuno (Z=-48 ke Z=-160, lebar 4m)
        CreateContinuousPavedRoad(expandRoads.transform, "Road_South_Expansion",
            new Vector3(0, 0, -48f), new Vector3(0, 0, -160f), 4.0f, roadMat, 2.5f);

        // Jalan Utama Barat ke Lembah Pertanian Luas & Kincir Angin (X=-66 ke X=-160, lebar 4m)
        CreateContinuousPavedRoad(expandRoads.transform, "Road_West_Expansion",
            new Vector3(-54f, 0, 0), new Vector3(-160f, 0, 0), 4.0f, roadMat, 2.5f);

        // Jalan Utama Timur ke Hutan Belantara & Makam Kuno (X=66 ke X=160, lebar 4m)
        CreateContinuousPavedRoad(expandRoads.transform, "Road_East_Expansion",
            new Vector3(54f, 0, 0), new Vector3(160f, 0, 0), 4.0f, roadMat, 2.5f);

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
    //  9d. MEGA EKSPANSI PETA — 4 ZONA LANJUTAN (Total dunia 1000x1000m)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 4 Zona Mega di luar 4 Zona Ekspansi pertama (Z/X 220-480):
    /// - UTARA JAUH   (Z=280-480): Puncak Salju — Kuil Es Kuno, Penjaga Salju, Kristal Beku
    /// - SELATAN JAUH (Z=-280 s/d -480): Hutan Peri — Pohon Bercahaya, Lingkaran Peri, Reruntuhan
    /// - BARAT JAUH   (X=-280 s/d -480): Kuil Laut — Kolam Besar, Patung Dewa, NPC Penjaga
    /// - TIMUR JAUH   (X=280-480): Ngarai Api — Kawah Vulkanik, Obsidian Tower, NPC Api
    /// </summary>
    static void SetupMegaExpansion(Material slavicMat, Material grassMat)
    {
        GameObject megaRoot = GameObject.Find("--- MEGA EXPANSION ---");
        if (megaRoot) Object.DestroyImmediate(megaRoot);
        megaRoot = new GameObject("--- MEGA EXPANSION ---");

        var barkMat       = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var birchBarkMat  = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: true);
        var summerLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var autumnLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("AUTUMN");
        var roadMat       = GetOrCreateVillageRoadMaterial();

        // ══════════════════════════════════════════════════════════════════
        // KORIDOR JALAN MEGA — Perpanjang 4 jalan utama ke luar zona 1
        // ══════════════════════════════════════════════════════════════════
        var megaRoads = new GameObject("MegaRoad_Network");
        megaRoads.transform.SetParent(megaRoot.transform);

        // Koridor Utara (Z 220→480)
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_North_A",
            new Vector3(0f, 0f, 220f), new Vector3(0f, 0f, 350f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_North_B",
            new Vector3(0f, 0f, 350f), new Vector3(0f, 0f, 480f), 3.8f, roadMat, 2.5f);

        // Koridor Selatan (Z -220→-480)
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_South_A",
            new Vector3(0f, 0f, -220f), new Vector3(0f, 0f, -350f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_South_B",
            new Vector3(0f, 0f, -350f), new Vector3(0f, 0f, -480f), 3.8f, roadMat, 2.5f);

        // Koridor Barat (X -220→-480)
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_West_A",
            new Vector3(-220f, 0f, 0f), new Vector3(-350f, 0f, 0f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_West_B",
            new Vector3(-350f, 0f, 0f), new Vector3(-480f, 0f, 0f), 3.8f, roadMat, 2.5f);

        // Koridor Timur (X 220→480)
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_East_A",
            new Vector3(220f, 0f, 0f), new Vector3(350f, 0f, 0f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_East_B",
            new Vector3(350f, 0f, 0f), new Vector3(480f, 0f, 0f), 3.8f, roadMat, 2.5f);

        // ══════════════════════════════════════════════════════════════════
        // ZONA MEGA-UTARA — PUNCAK SALJU (Z=280 - 480)
        // ══════════════════════════════════════════════════════════════════
        {
            var snowRoot = new GameObject("Zone_FarNorth_SnowPeak");
            snowRoot.transform.SetParent(megaRoot.transform);
            var snowCenter = new Vector3(0f, 0f, 380f);

            // Material salju & es
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var snowMat = new Material(shader);
            snowMat.SetColor("_BaseColor", new Color(0.92f, 0.96f, 1f));
            snowMat.SetFloat("_Smoothness", 0.15f);
            var iceMat = new Material(shader);
            iceMat.SetColor("_BaseColor", new Color(0.65f, 0.88f, 1f, 0.85f));
            iceMat.SetFloat("_Smoothness", 0.9f);
            iceMat.SetFloat("_Metallic", 0.1f);
            var stoneMat = new Material(shader);
            stoneMat.SetColor("_BaseColor", new Color(0.5f, 0.52f, 0.56f));
            stoneMat.SetFloat("_Smoothness", 0.08f);

            // Tanah salju
            var snowGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            snowGround.name = "SnowGround";
            snowGround.transform.SetParent(snowRoot.transform);
            snowGround.transform.position = snowCenter;
            snowGround.transform.localScale = new Vector3(20f, 1f, 20f);
            snowGround.GetComponent<Renderer>().sharedMaterial = snowMat;
            Object.DestroyImmediate(snowGround.GetComponent<Collider>());

            // Pegunungan salju (5 gunung berbeda ukuran)
            var mountains = new (Vector3 offset, Vector3 scale)[]
            {
                (new Vector3(  0f, 0f,  40f), new Vector3(50f, 90f, 50f)),
                (new Vector3( 60f, 0f,  10f), new Vector3(35f, 65f, 35f)),
                (new Vector3(-55f, 0f,  20f), new Vector3(40f, 70f, 40f)),
                (new Vector3( 30f, 0f, -30f), new Vector3(28f, 50f, 28f)),
                (new Vector3(-25f, 0f, -40f), new Vector3(32f, 55f, 32f)),
            };
            foreach (var (off, sc) in mountains)
            {
                var mt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mt.name = "SnowMountain";
                mt.transform.SetParent(snowRoot.transform);
                mt.transform.position = snowCenter + off;
                mt.transform.localScale = sc;
                mt.GetComponent<Renderer>().sharedMaterial = snowMat;
                Object.DestroyImmediate(mt.GetComponent<Collider>());
            }

            // Kuil Es Kuno (5 pilar es + atap)
            var templeCenter = snowCenter + new Vector3(0f, 0f, 10f);
            var templeRoot = new GameObject("IceTemple");
            templeRoot.transform.SetParent(snowRoot.transform);
            templeRoot.transform.position = templeCenter;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"IceTemple_Pillar_{i}";
                pillar.transform.SetParent(templeRoot.transform);
                pillar.transform.localPosition = new Vector3(Mathf.Cos(angle) * 7f, 5f, Mathf.Sin(angle) * 7f);
                pillar.transform.localScale = new Vector3(1.2f, 5f, 1.2f);
                pillar.GetComponent<Renderer>().sharedMaterial = iceMat;
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }
            // Altar es tengah
            var altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            altar.name = "IceTemple_Altar";
            altar.transform.SetParent(templeRoot.transform);
            altar.transform.localPosition = new Vector3(0f, 1f, 0f);
            altar.transform.localScale = new Vector3(4f, 2f, 4f);
            altar.GetComponent<Renderer>().sharedMaterial = iceMat;
            Object.DestroyImmediate(altar.GetComponent<Collider>());
            // Langkah-langkah kuil
            for (int s = 0; s < 3; s++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"IceTemple_Step_{s}";
                step.transform.SetParent(templeRoot.transform);
                float sz = 10f - s * 2f;
                step.transform.localPosition = new Vector3(0f, s * 0.5f - 0.75f, 0f);
                step.transform.localScale = new Vector3(sz, 0.5f, sz);
                step.GetComponent<Renderer>().sharedMaterial = stoneMat;
                Object.DestroyImmediate(step.GetComponent<Collider>());
            }

            // Formasi kristal beku (8 kristal runcing)
            for (int k = 0; k < 8; k++)
            {
                float ang = k * 45f * Mathf.Deg2Rad;
                float r   = Random.Range(15f, 30f);
                float h   = Random.Range(4f, 10f);
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crystal.name = $"FrozenCrystal_{k}";
                crystal.transform.SetParent(snowRoot.transform);
                crystal.transform.position = snowCenter + new Vector3(Mathf.Cos(ang) * r, h * 0.5f, Mathf.Sin(ang) * r);
                crystal.transform.localScale = new Vector3(1f, h, 1f);
                crystal.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f));
                crystal.GetComponent<Renderer>().sharedMaterial = iceMat;
                Object.DestroyImmediate(crystal.GetComponent<Collider>());
            }

            // Jembatan batu menuju kuil (dari Z=280 ke Z=320)
            for (int b = 0; b < 5; b++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"SnowBridge_Plank_{b}";
                plank.transform.SetParent(snowRoot.transform);
                plank.transform.position = new Vector3(0f, 0.2f, 290f + b * 7f);
                plank.transform.localScale = new Vector3(5f, 0.4f, 6f);
                plank.GetComponent<Renderer>().sharedMaterial = stoneMat;
                Object.DestroyImmediate(plank.GetComponent<Collider>());
            }

            // Pohon pinus bersalju (20 pohon)
            for (int t = 0; t < 20; t++)
            {
                float tx = Random.Range(-80f, 80f);
                float tz = Random.Range(-60f, 60f);
                RealisticTreeBuilder.BuildPineTree(
                    snowCenter + new Vector3(tx, 0f, tz),
                    Random.Range(5f, 10f), snowRoot.transform, barkMat, summerLeafMat);
            }

            // Cahaya biru redup (aura dingin)
            var coldLight = new GameObject("SnowPeak_AmbientLight");
            coldLight.transform.SetParent(snowRoot.transform);
            coldLight.transform.position = snowCenter + new Vector3(0f, 25f, 0f);
            var cl = coldLight.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(0.6f, 0.8f, 1f);
            cl.intensity = 1.2f;
            cl.range = 180f;

            // NPC Penjaga Salju
            SpawnNPC(snowRoot.transform, "NPC_PenjagaSalju",
                snowCenter + new Vector3(-8f, 0f, 5f), Quaternion.Euler(0, 160f, 0),
                new Color(0.72f, 0.78f, 0.85f),
                "Hati-hati, pengembara! Kuil Es ini menyimpan bahaya purba...");

            // Spawn musuh elite zona salju
            SpawnEnemyMarker(snowRoot.transform, "Elite_FrostGuardian_1", snowCenter + new Vector3( 12f, 0f, 15f));
            SpawnEnemyMarker(snowRoot.transform, "Elite_FrostGuardian_2", snowCenter + new Vector3(-12f, 0f, 15f));
            SpawnEnemyMarker(snowRoot.transform, "Elite_FrostGuardian_3", snowCenter + new Vector3(  0f, 0f, 30f));
        }

        // ══════════════════════════════════════════════════════════════════
        // ZONA MEGA-SELATAN — HUTAN PERI (Z=-280 s/d -480)
        // ══════════════════════════════════════════════════════════════════
        {
            var fairyRoot = new GameObject("Zone_FarSouth_FairyForest");
            fairyRoot.transform.SetParent(megaRoot.transform);
            var fairyCenter = new Vector3(0f, 0f, -380f);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var fairyMossMat = new Material(shader);
            fairyMossMat.SetColor("_BaseColor", new Color(0.1f, 0.55f, 0.2f));
            fairyMossMat.SetFloat("_Smoothness", 0.3f);
            var glowMat = new Material(shader);
            glowMat.SetColor("_BaseColor", new Color(0.9f, 1f, 0.5f));
            glowMat.SetColor("_EmissionColor", new Color(0.3f, 0.7f, 0.1f) * 2.5f);
            glowMat.EnableKeyword("_EMISSION");
            var ruinStoneMat = new Material(shader);
            ruinStoneMat.SetColor("_BaseColor", new Color(0.35f, 0.38f, 0.3f));
            ruinStoneMat.SetFloat("_Smoothness", 0.05f);

            // Tanah berlumut hijau tua
            var fairyGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fairyGround.name = "FairyGround";
            fairyGround.transform.SetParent(fairyRoot.transform);
            fairyGround.transform.position = fairyCenter;
            fairyGround.transform.localScale = new Vector3(20f, 1f, 20f);
            fairyGround.GetComponent<Renderer>().sharedMaterial = fairyMossMat;
            Object.DestroyImmediate(fairyGround.GetComponent<Collider>());

            // Pohon bercahaya besar (15 pohon raksasa)
            for (int t = 0; t < 15; t++)
            {
                float tx = Random.Range(-85f, 85f);
                float tz = Random.Range(-70f, 70f);
                var treePos = fairyCenter + new Vector3(tx, 0f, tz);
                RealisticTreeBuilder.BuildOakTree(treePos,
                    Random.Range(9f, 18f), fairyRoot.transform, barkMat, summerLeafMat);
                // Tambah cahaya peri di setiap pohon
                var glowGo = new GameObject($"FairyGlow_{t}");
                glowGo.transform.SetParent(fairyRoot.transform);
                glowGo.transform.position = treePos + new Vector3(0f, 4f, 0f);
                var gl = glowGo.AddComponent<Light>();
                gl.type = LightType.Point;
                gl.color = new Color(
                    Random.Range(0.3f, 1f),
                    Random.Range(0.6f, 1f),
                    Random.Range(0.2f, 0.8f));
                gl.intensity = 0.8f;
                gl.range = 12f;
            }

            // Lingkaran Peri (Fairy Ring) — 12 jamur bercahaya melingkar
            for (int m = 0; m < 12; m++)
            {
                float ang = m * 30f * Mathf.Deg2Rad;
                var mushroom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mushroom.name = $"FairyMushroom_{m}";
                mushroom.transform.SetParent(fairyRoot.transform);
                mushroom.transform.position = fairyCenter + new Vector3(Mathf.Cos(ang) * 8f, 0.4f, Mathf.Sin(ang) * 8f);
                mushroom.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
                mushroom.GetComponent<Renderer>().sharedMaterial = glowMat;
                Object.DestroyImmediate(mushroom.GetComponent<Collider>());
                // Tutup jamur
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = $"FairyMushroomCap_{m}";
                cap.transform.SetParent(fairyRoot.transform);
                cap.transform.position = fairyCenter + new Vector3(Mathf.Cos(ang) * 8f, 1.1f, Mathf.Sin(ang) * 8f);
                cap.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
                var capMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                capMat.SetColor("_BaseColor", new Color(Random.Range(0.6f, 1f), 0.1f, Random.Range(0.1f, 0.4f)));
                capMat.SetColor("_EmissionColor", capMat.GetColor("_BaseColor") * 1.5f);
                capMat.EnableKeyword("_EMISSION");
                cap.GetComponent<Renderer>().sharedMaterial = capMat;
                Object.DestroyImmediate(cap.GetComponent<Collider>());
            }
            // Altar Lingkaran Peri
            var fairyAltar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fairyAltar.name = "FairyAltar";
            fairyAltar.transform.SetParent(fairyRoot.transform);
            fairyAltar.transform.position = fairyCenter + new Vector3(0f, 0.5f, 0f);
            fairyAltar.transform.localScale = new Vector3(2.5f, 0.5f, 2.5f);
            fairyAltar.GetComponent<Renderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(fairyAltar.GetComponent<Collider>());

            // Reruntuhan batu kuno (4 tembok runtuh)
            var ruinOffsets = new Vector3[]
            {
                new Vector3( 20f, 0f,  18f), new Vector3(-22f, 0f,  15f),
                new Vector3( 18f, 0f, -20f), new Vector3(-18f, 0f, -18f),
            };
            foreach (var ro in ruinOffsets)
            {
                for (int rb = 0; rb < 3; rb++)
                {
                    var rBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rBlock.name = "FairyRuin_Block";
                    rBlock.transform.SetParent(fairyRoot.transform);
                    rBlock.transform.position = fairyCenter + ro + new Vector3(rb * 1.4f, Random.Range(0.5f, 1.5f), 0f);
                    rBlock.transform.localScale = new Vector3(1.2f, Random.Range(1f, 2.5f), 1.2f);
                    rBlock.transform.rotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), Random.Range(-10f, 10f));
                    rBlock.GetComponent<Renderer>().sharedMaterial = ruinStoneMat;
                    Object.DestroyImmediate(rBlock.GetComponent<Collider>());
                }
            }

            // Cahaya hijau keemasan (atmosfer peri)
            var fairyLight = new GameObject("FairyForest_AmbientLight");
            fairyLight.transform.SetParent(fairyRoot.transform);
            fairyLight.transform.position = fairyCenter + new Vector3(0f, 30f, 0f);
            var fl = fairyLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.color = new Color(0.4f, 1f, 0.5f);
            fl.intensity = 1.5f;
            fl.range = 200f;

            // NPC Peri Hutan
            SpawnNPC(fairyRoot.transform, "NPC_PeriHutan",
                fairyCenter + new Vector3(5f, 0f, -5f), Quaternion.Euler(0, 30f, 0),
                new Color(0.7f, 1f, 0.7f),
                "Selamat datang di Hutan Peri, pengembara! Jangan ganggu Lingkaran Peri...");

            // Spawn musuh penjaga hutan peri
            SpawnEnemyMarker(fairyRoot.transform, "Elite_FairyGuardian_1", fairyCenter + new Vector3(-15f, 0f, -10f));
            SpawnEnemyMarker(fairyRoot.transform, "Elite_FairyGuardian_2", fairyCenter + new Vector3( 15f, 0f, -10f));
        }

        // ══════════════════════════════════════════════════════════════════
        // ZONA MEGA-BARAT — KUIL LAUT (X=-280 s/d -480)
        // ══════════════════════════════════════════════════════════════════
        {
            var seaRoot = new GameObject("Zone_FarWest_SeaTemple");
            seaRoot.transform.SetParent(megaRoot.transform);
            var seaCenter = new Vector3(-380f, 0f, 0f);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var waterMat = new Material(shader);
            waterMat.SetColor("_BaseColor", new Color(0.05f, 0.4f, 0.7f, 0.75f));
            waterMat.SetFloat("_Smoothness", 0.95f);
            waterMat.SetFloat("_Metallic", 0.1f);
            if (waterMat.HasProperty("_Surface")) waterMat.SetFloat("_Surface", 1f); // transparent
            waterMat.renderQueue = 3000;
            var coralMat = new Material(shader);
            coralMat.SetColor("_BaseColor", new Color(1f, 0.4f, 0.3f));
            coralMat.SetFloat("_Smoothness", 0.3f);
            var tempStoneMat = new Material(shader);
            tempStoneMat.SetColor("_BaseColor", new Color(0.4f, 0.5f, 0.55f));
            tempStoneMat.SetFloat("_Smoothness", 0.5f);
            var godMat = new Material(shader);
            godMat.SetColor("_BaseColor", new Color(0.8f, 0.65f, 0.2f));
            godMat.SetFloat("_Metallic", 0.6f);
            godMat.SetFloat("_Smoothness", 0.7f);

            // Kolam besar kuil laut
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "SeaTemple_Pool";
            pool.transform.SetParent(seaRoot.transform);
            pool.transform.position = seaCenter + new Vector3(0f, -0.4f, 0f);
            pool.transform.localScale = new Vector3(80f, 0.4f, 80f);
            pool.GetComponent<Renderer>().sharedMaterial = waterMat;
            Object.DestroyImmediate(pool.GetComponent<Collider>());

            // Kuil di tengah kolam (platform bertingkat)
            for (int lvl = 0; lvl < 4; lvl++)
            {
                float sz = 22f - lvl * 4.5f;
                var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name = $"SeaTemple_Platform_{lvl}";
                platform.transform.SetParent(seaRoot.transform);
                platform.transform.position = seaCenter + new Vector3(0f, lvl * 2.5f - 0.3f, 0f);
                platform.transform.localScale = new Vector3(sz, 1.2f, sz);
                platform.GetComponent<Renderer>().sharedMaterial = tempStoneMat;
                Object.DestroyImmediate(platform.GetComponent<Collider>());
            }

            // Patung Dewa Laut (3 patung di platform atas)
            var godPositions = new Vector3[]
            {
                new Vector3(0f, 11f, 0f),
                new Vector3(5f, 10.5f, 0f),
                new Vector3(-5f, 10.5f, 0f),
            };
            foreach (var gp in godPositions)
            {
                var godBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                godBody.name = "SeaGod_Statue";
                godBody.transform.SetParent(seaRoot.transform);
                godBody.transform.position = seaCenter + gp;
                godBody.transform.localScale = new Vector3(1.5f, 2.5f, 1.5f);
                godBody.GetComponent<Renderer>().sharedMaterial = godMat;
                Object.DestroyImmediate(godBody.GetComponent<Collider>());
            }

            // 8 Pilar kuil mengelilingi platform
            for (int p = 0; p < 8; p++)
            {
                float ang = p * 45f * Mathf.Deg2Rad;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"SeaTemple_Pillar_{p}";
                pillar.transform.SetParent(seaRoot.transform);
                pillar.transform.position = seaCenter + new Vector3(Mathf.Cos(ang) * 11f, 5.5f, Mathf.Sin(ang) * 11f);
                pillar.transform.localScale = new Vector3(1.5f, 5.5f, 1.5f);
                pillar.GetComponent<Renderer>().sharedMaterial = tempStoneMat;
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }

            // Karang dan formasi bawah laut mengelilingi kolam (karang terumbu)
            for (int c = 0; c < 15; c++)
            {
                float ang = c * 24f * Mathf.Deg2Rad;
                float r   = Random.Range(42f, 55f);
                var coral = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coral.name = $"CoralReef_{c}";
                coral.transform.SetParent(seaRoot.transform);
                coral.transform.position = seaCenter + new Vector3(Mathf.Cos(ang) * r, Random.Range(-0.5f, 1.5f), Mathf.Sin(ang) * r);
                coral.transform.localScale = new Vector3(Random.Range(1.5f, 4f), Random.Range(1f, 3f), Random.Range(1.5f, 4f));
                coral.GetComponent<Renderer>().sharedMaterial = coralMat;
                Object.DestroyImmediate(coral.GetComponent<Collider>());
            }

            // Jembatan kayu ke kuil (dari X=-220 ke kolam)
            for (int b = 0; b < 10; b++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"SeaBridge_Plank_{b}";
                plank.transform.SetParent(seaRoot.transform);
                plank.transform.position = seaCenter + new Vector3(45f - b * 5f, 0.15f, 0f);
                plank.transform.localScale = new Vector3(4f, 0.3f, 5f);
                plank.GetComponent<Renderer>().sharedMaterial = tempStoneMat;
                Object.DestroyImmediate(plank.GetComponent<Collider>());
            }

            // Cahaya biru laut dari bawah
            var seaLight = new GameObject("SeaTemple_Light");
            seaLight.transform.SetParent(seaRoot.transform);
            seaLight.transform.position = seaCenter + new Vector3(0f, 5f, 0f);
            var sL = seaLight.AddComponent<Light>();
            sL.type = LightType.Point;
            sL.color = new Color(0.1f, 0.5f, 1f);
            sL.intensity = 2.5f;
            sL.range = 100f;

            // NPC Penjaga Kuil Laut
            SpawnNPC(seaRoot.transform, "NPC_PenjagaKuilLaut",
                seaCenter + new Vector3(32f, 0f, 5f), Quaternion.Euler(0, -90f, 0),
                new Color(0.4f, 0.6f, 0.8f),
                "Kuil Dewa Laut ini sudah berdiri ribuan tahun. Hormati tempat ini, wahai pengembara.");

            AnimalAssetSetup.SpawnWolf(seaCenter + new Vector3(38f, 0f, -8f),
                Quaternion.Euler(0, 90f, 0), seaRoot.transform, "Penjaga Kuil Barat");

            // Spawn musuh elite kuil laut
            SpawnEnemyMarker(seaRoot.transform, "Elite_SeaGuardian_1", seaCenter + new Vector3(-8f, 10f, -8f));
            SpawnEnemyMarker(seaRoot.transform, "Elite_SeaGuardian_2", seaCenter + new Vector3( 8f, 10f,  8f));
        }

        // ══════════════════════════════════════════════════════════════════
        // ZONA MEGA-TIMUR — NGARAI API (X=280-480)
        // ══════════════════════════════════════════════════════════════════
        {
            var fireRoot = new GameObject("Zone_FarEast_FireCanyon");
            fireRoot.transform.SetParent(megaRoot.transform);
            var fireCenter = new Vector3(380f, 0f, 0f);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var lavaMat = new Material(shader);
            lavaMat.SetColor("_BaseColor", new Color(1f, 0.25f, 0f));
            lavaMat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0f) * 3f);
            lavaMat.EnableKeyword("_EMISSION");
            lavaMat.SetFloat("_Smoothness", 0.6f);
            var obsidianMat = new Material(shader);
            obsidianMat.SetColor("_BaseColor", new Color(0.08f, 0.05f, 0.1f));
            obsidianMat.SetFloat("_Metallic", 0.4f);
            obsidianMat.SetFloat("_Smoothness", 0.75f);
            var ashMat = new Material(shader);
            ashMat.SetColor("_BaseColor", new Color(0.25f, 0.22f, 0.2f));
            ashMat.SetFloat("_Smoothness", 0.02f);
            var emberMat = new Material(shader);
            emberMat.SetColor("_BaseColor", new Color(1f, 0.5f, 0f));
            emberMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 4f);
            emberMat.EnableKeyword("_EMISSION");

            // Tanah abu vulkanik
            var ashGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ashGround.name = "AshGround";
            ashGround.transform.SetParent(fireRoot.transform);
            ashGround.transform.position = fireCenter;
            ashGround.transform.localScale = new Vector3(20f, 1f, 20f);
            ashGround.GetComponent<Renderer>().sharedMaterial = ashMat;
            Object.DestroyImmediate(ashGround.GetComponent<Collider>());

            // Kawah lava utama (di tengah)
            var crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name = "VolcanoCrater";
            crater.transform.SetParent(fireRoot.transform);
            crater.transform.position = fireCenter + new Vector3(0f, -1f, 10f);
            crater.transform.localScale = new Vector3(35f, 1f, 35f);
            crater.GetComponent<Renderer>().sharedMaterial = lavaMat;
            Object.DestroyImmediate(crater.GetComponent<Collider>());

            // Dinding kawah (12 batu besar mengelilingi)
            for (int r = 0; r < 12; r++)
            {
                float ang = r * 30f * Mathf.Deg2Rad;
                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = $"CraterWall_{r}";
                rock.transform.SetParent(fireRoot.transform);
                rock.transform.position = fireCenter + new Vector3(10f + Mathf.Cos(ang) * 20f, Random.Range(1f, 6f), 10f + Mathf.Sin(ang) * 20f);
                rock.transform.localScale = new Vector3(Random.Range(4f, 9f), Random.Range(3f, 7f), Random.Range(4f, 9f));
                rock.GetComponent<Renderer>().sharedMaterial = obsidianMat;
                Object.DestroyImmediate(rock.GetComponent<Collider>());
            }

            // Obsidian Tower (menara gelap 3 lantai)
            var towerCenter = fireCenter + new Vector3(-18f, 0f, -10f);
            for (int fl = 0; fl < 3; fl++)
            {
                float sz = 7f - fl * 1.5f;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"ObsidianTower_Floor_{fl}";
                floor.transform.SetParent(fireRoot.transform);
                floor.transform.position = towerCenter + new Vector3(0f, fl * 5f + 2.5f, 0f);
                floor.transform.localScale = new Vector3(sz, 5f, sz);
                floor.GetComponent<Renderer>().sharedMaterial = obsidianMat;
                Object.DestroyImmediate(floor.GetComponent<Collider>());
            }
            // Puncak Obsidian Tower (kristal api)
            var towerTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            towerTop.name = "ObsidianTower_Top";
            towerTop.transform.SetParent(fireRoot.transform);
            towerTop.transform.position = towerCenter + new Vector3(0f, 18f, 0f);
            towerTop.transform.localScale = new Vector3(4f, 4f, 4f);
            towerTop.GetComponent<Renderer>().sharedMaterial = lavaMat;
            Object.DestroyImmediate(towerTop.GetComponent<Collider>());

            // Kolom api/lahar (5 pilar lava)
            var lavaPillarPos = new Vector3[]
            {
                new Vector3(10f, 0f, -15f), new Vector3(-8f, 0f, 25f),
                new Vector3(22f, 0f,  18f), new Vector3(5f,  0f, 32f),
                new Vector3(-20f,0f, -20f),
            };
            foreach (var lp in lavaPillarPos)
            {
                float lh = Random.Range(3f, 8f);
                var lavaPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lavaPillar.name = "LavaPillar";
                lavaPillar.transform.SetParent(fireRoot.transform);
                lavaPillar.transform.position = fireCenter + lp + new Vector3(0f, lh * 0.5f, 0f);
                lavaPillar.transform.localScale = new Vector3(2f, lh, 2f);
                lavaPillar.GetComponent<Renderer>().sharedMaterial = lavaMat;
                Object.DestroyImmediate(lavaPillar.GetComponent<Collider>());
            }

            // Formasi batu obsidian runcing (10 batu)
            for (int ob = 0; ob < 10; ob++)
            {
                float tx = Random.Range(-70f, 70f);
                float tz = Random.Range(-60f, 60f);
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spike.name = $"ObsidianSpike_{ob}";
                spike.transform.SetParent(fireRoot.transform);
                float sh = Random.Range(5f, 14f);
                spike.transform.position = fireCenter + new Vector3(tx, sh * 0.5f, tz);
                spike.transform.localScale = new Vector3(Random.Range(1f, 2.5f), sh, Random.Range(1f, 2.5f));
                spike.transform.rotation = Quaternion.Euler(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
                spike.GetComponent<Renderer>().sharedMaterial = obsidianMat;
                Object.DestroyImmediate(spike.GetComponent<Collider>());
            }

            // Cahaya merah lava (atmosfer api)
            var fireLight1 = new GameObject("FireCanyon_Light1");
            fireLight1.transform.SetParent(fireRoot.transform);
            fireLight1.transform.position = fireCenter + new Vector3(0f, 8f, 10f);
            var fL1 = fireLight1.AddComponent<Light>();
            fL1.type = LightType.Point;
            fL1.color = new Color(1f, 0.3f, 0f);
            fL1.intensity = 3f;
            fL1.range = 120f;

            var fireLight2 = new GameObject("FireCanyon_Light2");
            fireLight2.transform.SetParent(fireRoot.transform);
            fireLight2.transform.position = towerCenter + new Vector3(0f, 20f, 0f);
            var fL2 = fireLight2.AddComponent<Light>();
            fL2.type = LightType.Point;
            fL2.color = new Color(1f, 0.15f, 0f);
            fL2.intensity = 2f;
            fL2.range = 80f;

            // NPC Penjaga Api
            SpawnNPC(fireRoot.transform, "NPC_PenjagaApi",
                fireCenter + new Vector3(-25f, 0f, -5f), Quaternion.Euler(0, 90f, 0),
                new Color(0.9f, 0.4f, 0.2f),
                "Ngarai Api hanya untuk yang terkuat! Balikkan langkahmu, pengembara lemah!");

            AnimalAssetSetup.SpawnWolf(fireCenter + new Vector3(-28f, 0f, 8f),
                Quaternion.Euler(0, 90f, 0), fireRoot.transform, "Serigala Api");

            // Spawn musuh elite Ngarai Api
            SpawnEnemyMarker(fireRoot.transform, "Elite_FireDemon_1", fireCenter + new Vector3( 5f, 0f, 10f));
            SpawnEnemyMarker(fireRoot.transform, "Elite_FireDemon_2", fireCenter + new Vector3(-5f, 0f, 10f));
            SpawnEnemyMarker(fireRoot.transform, "Elite_FireBoss",    fireCenter + new Vector3( 0f, 0f, 30f));
        }

        // ══════════════════════════════════════════════════════════════════
        // POHON PEMBATAS MEGA — Hutan tebal di perbatasan 1000m
        // ══════════════════════════════════════════════════════════════════
        int treeCount = 0;
        for (float tx = -480f; tx <= 480f; tx += Random.Range(10f, 18f))
        {
            // Perbatasan Utara
            RealisticTreeBuilder.BuildPineTree(
                new Vector3(tx, 0f, 490f + Random.Range(-5f, 5f)),
                Random.Range(7f, 14f), megaRoot.transform, barkMat, summerLeafMat);
            // Perbatasan Selatan
            RealisticTreeBuilder.BuildAutumnOakTree(
                new Vector3(tx, 0f, -490f + Random.Range(-5f, 5f)),
                Random.Range(7f, 14f), megaRoot.transform, barkMat, autumnLeafMat);
            if (++treeCount > 200) break;
        }
        treeCount = 0;
        for (float tz = -480f; tz <= 480f; tz += Random.Range(10f, 18f))
        {
            // Perbatasan Barat
            RealisticTreeBuilder.BuildBirchTree(
                new Vector3(-490f + Random.Range(-5f, 5f), 0f, tz),
                Random.Range(7f, 14f), megaRoot.transform, birchBarkMat, summerLeafMat);
            // Perbatasan Timur
            RealisticTreeBuilder.BuildAutumnOakTree(
                new Vector3(490f + Random.Range(-5f, 5f), 0f, tz),
                Random.Range(7f, 14f), megaRoot.transform, barkMat, autumnLeafMat);
            if (++treeCount > 200) break;
        }
    }

    // Helper: Spawn NPC sederhana dengan dialog satu baris
    static void SpawnNPC(Transform parent, string npcName, Vector3 worldPos,
                         Quaternion rotation, Color skinTint, string dialogText)
    {
        // Pilih gaya NPC berdasarkan nama (heuristik sederhana)
        ProceduralNpcBuilder.NpcStyle style = ProceduralNpcBuilder.NpcStyle.Guard;
        if (npcName.Contains("Penyihir") || npcName.Contains("Peri"))
            style = ProceduralNpcBuilder.NpcStyle.Wizard;
        else if (npcName.Contains("Kapten") || npcName.Contains("Nelayan"))
            style = ProceduralNpcBuilder.NpcStyle.Farmer;
        else if (npcName.Contains("Penam") || npcName.Contains("Tua"))
            style = ProceduralNpcBuilder.NpcStyle.Farmer;
        else if (npcName.Contains("Api") || npcName.Contains("Penjaga"))
            style = ProceduralNpcBuilder.NpcStyle.Guard;

        var npcGo = ProceduralNpcBuilder.BuildNpc(style, worldPos, rotation, parent, npcName);
        if (!npcGo) return;

        var dlg = npcGo.GetComponent<NpcDialogue>() ?? npcGo.AddComponent<NpcDialogue>();
        CreateSimpleDialogue(npcName, dialogText, dlg);
    }

    // Helper: Spawn titik spawn musuh (marker merah)
    static void SpawnEnemyMarker(Transform parent, string markerName, Vector3 worldPos)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = markerName;
        marker.transform.SetParent(parent);
        marker.transform.position = worldPos;
        marker.transform.localScale = new Vector3(2f, 0.4f, 2f);
        ApplyColor(marker, new Color(0.85f, 0.05f, 0.05f));
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9e. ZONE TRIGGER — Musik & Cuaca Otomatis Per Zona
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Membuat BoxCollider trigger batas tiap zona (8 zona + desa pusat).
    /// Saat pemain menyeberang batas, ZoneMusicTrigger mengganti musik dan cuaca.
    /// </summary>
    static void SetupZoneTriggers()
    {
        var triggerRoot = GameObject.Find("--- ZONE TRIGGERS ---");
        if (triggerRoot) Object.DestroyImmediate(triggerRoot);
        triggerRoot = new GameObject("--- ZONE TRIGGERS ---");

        // (zona, center, size, zoneType)
        var zoneDefs = new (string name, Vector3 center, Vector3 size, ZoneMusicTrigger.ZoneType type)[]
        {
            // Desa Pusat — village music
            ("ZoneTrigger_Village",
                new Vector3(0f,    5f,   0f),
                new Vector3(120f,  20f,  120f),
                ZoneMusicTrigger.ZoneType.Village),

            // Gelombang 1 — Ekspansi Pertama
            ("ZoneTrigger_North_Mountain",
                new Vector3(0f,    5f,  190f),
                new Vector3(120f,  20f,  70f),
                ZoneMusicTrigger.ZoneType.Mountain),
            ("ZoneTrigger_South_Swamp",
                new Vector3(0f,    5f, -190f),
                new Vector3(120f,  20f,  70f),
                ZoneMusicTrigger.ZoneType.Swamp),
            ("ZoneTrigger_West_Fishing",
                new Vector3(-190f, 5f,   0f),
                new Vector3(70f,   20f, 120f),
                ZoneMusicTrigger.ZoneType.Fishing),
            ("ZoneTrigger_East_Mines",
                new Vector3( 190f, 5f,   0f),
                new Vector3(70f,   20f, 120f),
                ZoneMusicTrigger.ZoneType.Mines),

            // Gelombang 2 — Mega Ekspansi
            ("ZoneTrigger_FarNorth_SnowPeak",
                new Vector3(0f,    5f,  380f),
                new Vector3(200f,  20f, 200f),
                ZoneMusicTrigger.ZoneType.SnowPeak),
            ("ZoneTrigger_FarSouth_FairyForest",
                new Vector3(0f,    5f, -380f),
                new Vector3(200f,  20f, 200f),
                ZoneMusicTrigger.ZoneType.FairyForest),
            ("ZoneTrigger_FarWest_SeaTemple",
                new Vector3(-380f, 5f,   0f),
                new Vector3(200f,  20f, 200f),
                ZoneMusicTrigger.ZoneType.SeaTemple),
            ("ZoneTrigger_FarEast_FireCanyon",
                new Vector3( 380f, 5f,   0f),
                new Vector3(200f,  20f, 200f),
                ZoneMusicTrigger.ZoneType.FireCanyon),
        };

        foreach (var (name, center, size, zoneType) in zoneDefs)
        {
            var go = new GameObject(name);
            go.transform.SetParent(triggerRoot.transform);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider>();
            col.size      = size;
            col.isTrigger = true;
            var trigger = go.AddComponent<ZoneMusicTrigger>();
            // Set field zone via SerializedObject agar bisa di-set di Edit Mode
            var so = new UnityEditor.SerializedObject(trigger);
            so.FindProperty("zone").enumValueIndex = (int)zoneType;
            so.ApplyModifiedProperties();
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
            (new Vector3( 76f,0f,   8f), "SpawnPoint_East_Mines"),
            (new Vector3(-76f,0f,   8f), "SpawnPoint_West_Forest"),
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

            bool isWalkSurfaceOrFoliage = fbxName.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         fbxName.IndexOf("Cobble", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         fbxName.IndexOf("Grass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         fbxName.IndexOf("Flower", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (isWalkSurfaceOrFoliage)
            {
                foreach (var col in instance.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);
            }
            else
            {
                foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!mf.GetComponent<Collider>())
                    {
                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
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
        var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0) return;

        var tagManager = new SerializedObject(tagAssets[0]);
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
        if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            AssetDatabase.CreateFolder("Assets", "_Project");
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


    // ═════════════════════════════════════════════════════════════════════
    //  PASS DESAIN AKHIR (dari SceneSetupTool.Beautify.cs)
    // ═════════════════════════════════════════════════════════════════════
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
        const float inner = 66f;     // di luar pagar benteng (menara di 62m)
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
        if (Vector2.Distance(p, new Vector2( 84f, 9f)) < 13f) return true;            // Tambang bijih besi
        if (Vector2.Distance(p, new Vector2(-84f, 9f)) < 13f) return true;            // Area balok kayu
        if (Vector2.Distance(p, new Vector2( 74f, 40f)) < 16f) return true;           // Treehouse Pengawas
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
