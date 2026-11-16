#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Eldara;
using Eldara.EditorTools;

/// <summary>
/// Tool pembangun dunia (World Builder) untuk Legenda Desa Eldara — Desain Desa RPG Organik & Estetik.
/// Fitur Tata Letak Baru:
/// - Alun-Alun Utama (Central Town Square) dengan Sumur Batu Kuno, Taman Bundar, & Pasar Terbuka
/// - Jalan Lingkar Dalam (Inner Ring Promenade) + 4 Poros Utama + Gang Setapak Melengkung ke Tiap Rumah
/// - Sungai Kecil Melintasi Distrik Barat (Riverbend) lengkap dengan Jembatan Kayu Lengkung & Pondok Danau di tepi air
/// - Klaster Permukiman Natural (Rumah berorientasi organik menghadap jalan/plaza dengan pekarangan, kebun, & lampu)
/// - Benteng Palisade & Menara Pengawas dengan Gerbang Selatan Megah
/// Menu: Eldara > Setup Prototype Scene
/// </summary>
public static partial class SceneSetupTool
{
    [MenuItem("Eldara/Setup Prototype Scene")]
    public static void SetupScene()
    {
        if (Application.isPlaying)
        {
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Stop!", "Hentikan Play Mode dulu sebelum setup scene.", "OK");
            return;
        }

        Random.InitState(20260611);

        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
        {
            if (System.IO.File.Exists(sampleScenePath))
            {
                EditorSceneManager.OpenScene(sampleScenePath);
            }
        }

        string[] namaLama = new string[]
        {
            "Village", "--- NPCs ---", "--- TOWERS ---", "--- WILDERNESS ---",
            "--- GRASS FIELDS ---", "--- 3D WILDFLOWERS ---", "--- WORLD OBJECTS ---",
            "--- FAUNA & ANIMALS ---", "--- EXPANSION ZONES ---", "--- MEGA EXPANSION ---",
            "--- ZONE TRIGGERS ---", "--- ENEMY SPAWNERS ---", "--- VILLAGE DETAILS ---", "Village_Yard_Fences",
            "--- CHUNK RENDER DISTANCE ---",
            "--- JUNGLE BIOME ---", "--- JUNGLE PORTAL (VILLAGE) ---",
            "SpawnPoints", "SpawnManager",
            "Managers", "HUD", "CameraRig", "Ground",
            "EXPANSION_ZONES", "MegaExpansion", "ZoneTriggers", "NightSpawner",
            "Zone_North_Mountain", "Zone_South_Swamp", "Zone_West_Fishing", "Zone_East_Cliffs",
            "Zone_FarNorth_SnowPeak", "Zone_FarSouth_FairyForest",
            "Zone_FarWest_SeaTemple", "Zone_FarEast_FireCanyon",
            "ZoneConnectors", "ZoneConnectors_Mega",
        };
        var activeRoots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in activeRoots)
        {
            if (root == null) continue;
            foreach (string nm in namaLama)
            {
                if (root.name == nm)
                {
                    Object.DestroyImmediate(root);
                    break;
                }
            }
        }

        EnsureTagExists("NPC");
        var slavicMat = AssetMaterialSetup.SetupSlavicMaterial();
        AssetMaterialSetup.SetupMedievalMaterials();

        SetupEnvironment();

        SetupPlayer(out GameObject player, out PlayerController playerCtrl,
                    out Health playerHealth, out PlayerInteractor playerInteractor);
        SetupCamera(player);

        SetupManagers(playerCtrl, playerHealth);

        // Bangun Desa dengan Desain Organik & Tata Letak Baru
        SetupExpansiveVillage(slavicMat);

        SetupSpawnPoints(playerCtrl);
        SetupNPCs();
        SetupFortifications(slavicMat);
        SetupSurroundingWilderness(slavicMat);

        var grassTuftMat = AssetMaterialSetup.SetupGrassMaterial();
        SetupRealisticGrassFields(grassTuftMat);

        SetupWorldObjects(slavicMat);
        SetupFaunaAndAnimals();
        SetupExpandedWorld(slavicMat, grassTuftMat);
        SetupMegaExpansion(slavicMat, grassTuftMat);
        SetupJungleBiome(slavicMat, grassTuftMat);
        SetupZoneTriggers();
        ApplyVillageDesignPass();
        SetupEnemySpawners();
        SetupHUD(playerInteractor, playerHealth);
        ChunkRenderDistance.ApplyToEntireWorldMenu();

        // Pastikan MainMenu dan SampleScene terdaftar di Build Settings & Play Mode selalu mulai dari MainMenu
        MainMenuSceneTool.EnsurePlayModeStartsAtMainMenu();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = player;

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Desain Desa Eldara Baru Selesai!",
                "Desa Eldara telah dibangun ulang dengan tata letak baru yang jauh lebih alami dan hidup:\n\n" +
                "1. Alun-Alun Utama (Central Square) + Jalan Lingkar Dalam (Inner Ring Road)\n" +
                "2. Sungai Kecil Melintasi Distrik Barat + Jembatan Kayu Lengkung + Pondok Danau di tepi sungai\n" +
                "3. Klaster 16 Bangunan Adat & Kayu yang berorientasi organik ke jalan & taman\n" +
                "4. Pekarangan berpagar rendah, kebun sayur, lentera jalan, & rambu petunjuk arah\n" +
                "5. Benteng Palisade Keliling dengan Gerbang Selatan Megah",
                "Jelajahi Desa!"
            );
        }
    }

    public static void RemovePerimeterFences()
    {
        int count = 0;
        var yardFences = GameObject.Find("Village_Yard_Fences");
        if (yardFences) { count += yardFences.transform.childCount; Object.DestroyImmediate(yardFences); }

        var towerRoot = GameObject.Find("--- TOWERS ---");
        if (towerRoot)
        {
            var toRemove = new List<GameObject>();
            for (int i = 0; i < towerRoot.transform.childCount; i++)
            {
                var child = towerRoot.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("EA03_Village_Fence_01a") || child.name == "Palisade_Wall")
                {
                    if (Mathf.Abs(child.transform.position.z - (-56f)) < 1f) continue;
                    toRemove.Add(child);
                }
            }
            count += toRemove.Count;
            foreach (var go in toRemove) Object.DestroyImmediate(go);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Eldara] {count} modul pagar dihapus.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. ENVIRONMENT & ATMOSPHERE
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnvironment()
    {
        GameObject ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
        }
        ground.transform.localScale = new Vector3(100f, 1f, 100f);

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
            grassMat.SetColor("_BaseColor", new Color(0.76f, 0.91f, 0.62f, 1f));
            grassMat.SetFloat("_Smoothness", 0.04f);
            EditorUtility.SetDirty(grassMat);
            ground.GetComponent<Renderer>().sharedMaterial = grassMat;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 75f;
        RenderSettings.fogEndDistance = 380f;
        RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.85f);

        Light sunLight = null;
        var go_sun = GameObject.Find("Directional Light");
        if (go_sun) sunLight = go_sun.GetComponent<Light>();
        if (!sunLight)
        {
            foreach (var l in Object.FindObjectsOfType<Light>(true))
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

        dnc.EnsureCelestialVisuals();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. PLAYER & CAMERA & MANAGERS
    // ─────────────────────────────────────────────────────────────────────
    static void SetupPlayer(out GameObject player, out PlayerController playerCtrl,
                            out Health playerHealth, out PlayerInteractor playerInteractor)
    {
        player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (!player)
        {
            player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 0.1f, -5f);
        }
        else
        {
            var mf = player.GetComponent<MeshFilter>();
            if (mf) Object.DestroyImmediate(mf);
            var mr = player.GetComponent<MeshRenderer>();
            if (mr) Object.DestroyImmediate(mr);
            var visor = player.transform.Find("FaceIndicator");
            if (visor) Object.DestroyImmediate(visor.gameObject);
        }

        var oldCol = player.GetComponent<CapsuleCollider>();
        if (oldCol) Object.DestroyImmediate(oldCol);

        var cc = player.GetComponent<CharacterController>();
        if (!cc) cc = player.AddComponent<CharacterController>();
        cc.height = 1.85f;
        cc.radius = 0.38f;
        cc.center = new Vector3(0f, 0.925f, 0f);
        cc.stepOffset  = 0.45f;
        cc.slopeLimit  = 55f;

        KiWiraBuilder.BuildOn(player);

        var idle = player.GetComponent<KiWiraIdle>();
        if (!idle) idle = player.AddComponent<KiWiraIdle>();
        idle.napas = true;
        idle.putarOtomatis = false;
        idle.jalanDiTempat = false;

        bool ccActive = cc.enabled;
        cc.enabled = false;
        player.transform.position = new Vector3(0f, 0.1f, -8.5f);
        player.transform.rotation = Quaternion.Euler(0, 0f, 0);
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

        var renderers = player.GetComponentsInChildren<Renderer>(true);
        var rProp = so.FindProperty("playerRenderers");
        rProp.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
        {
            rProp.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }
        so.ApplyModifiedProperties();
    }

    static void SetupManagers(PlayerController playerCtrl, Health playerHealth)
    {
        var managers = GetOrCreateManagerRoot();

        if (!managers.GetComponent<Inventory>())        managers.AddComponent<Inventory>();
        if (!managers.GetComponent<QuestManager>())     managers.AddComponent<QuestManager>();
        if (!managers.GetComponent<DialogueManager>())  managers.AddComponent<DialogueManager>();
        if (!managers.GetComponent<MusicManager>())     managers.AddComponent<MusicManager>();
        if (!managers.GetComponent<CloudManager>())     managers.AddComponent<CloudManager>();
        if (!managers.GetComponent<WeatherSystem>())    managers.AddComponent<WeatherSystem>();
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

        var spawnMgr = managers.GetComponent<SpawnManager>();
        if (!spawnMgr) spawnMgr = managers.AddComponent<SpawnManager>();
        spawnMgr.pemain = playerCtrl != null ? playerCtrl.transform : null;
    }

    static void SetupSpawnPoints(PlayerController playerCtrl)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && (root.name == "SpawnPoints" || root.name == "SpawnUtama" || root.name == "SpawnAwal" || root.name == "SpawnRespawn"))
            {
                Object.DestroyImmediate(root);
            }
        }

        SpawnSetupTool.Buat(40f, 180f, true);

        var manager = Object.FindObjectOfType<SpawnManager>(true);
        if (manager != null && manager.spawnAwal != null && playerCtrl != null)
        {
            var cc = playerCtrl.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerCtrl.transform.SetPositionAndRotation(manager.spawnAwal.PosisiSpawn, manager.spawnAwal.RotasiSpawn);
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5. DESAIN DESA ELDARA BARU (TATA LETAK RUMAH & JALAN ORGANIK)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupExpansiveVillage(Material slavicMat)
    {
        GameObject village = GameObject.Find("Village");
        if (village) Object.DestroyImmediate(village);

        village = new GameObject("Village");

        var roadMat  = GetOrCreateVillageRoadMaterial();
        var curbMat  = GetMedievalMat("M_Medieval_RockTrim") ?? roadMat;
        var woodMat  = GetMedievalMat("M_Medieval_WoodTrim") ?? roadMat;
        var waterMat = GetOrCreateRealisticWaterMaterial();

        var roadsParent = new GameObject("Village_Road_Network");
        roadsParent.transform.SetParent(village.transform);

        // ═════════════════════════════════════════════════════════════════
        //  A. ALUN-ALUN PUSAT (CENTRAL TOWN SQUARE) & JALAN LINGKAR DALAM
        // ═════════════════════════════════════════════════════════════════

        // 1. Plaza Bundar Utama (Radius 10.5m) dengan Cincin Batu Pinggir
        CreateCircularPlaza(village.transform, "Central_Plaza_Stone", Vector3.zero, 10.5f, roadMat, 2.5f);
        var plazaRingMesh = CreateHollowRingMesh("Central_Plaza_Curb", 10.85f, 10.45f, 0.055f, -0.04f, 56, false);
        CreateMeshObject("Central_Plaza_Curb", village.transform, plazaRingMesh, curbMat, new Color(0.45f, 0.44f, 0.42f));

        // 2. Jalan Lingkar Segi Delapan (Inner Ring Promenade, radius ~23m)
        //    Menghubungkan seluruh distrik desa secara sirkular tanpa jalan buntu!
        const float ringR = 23.0f;
        int ringSides = 8;
        Vector3[] ringPts = new Vector3[ringSides];
        for (int i = 0; i < ringSides; i++)
        {
            float ang = (i * 45f) * Mathf.Deg2Rad;
            ringPts[i] = new Vector3(Mathf.Cos(ang) * ringR, 0f, Mathf.Sin(ang) * ringR);
        }
        for (int i = 0; i < ringSides; i++)
        {
            Vector3 a = ringPts[i];
            Vector3 b = ringPts[(i + 1) % ringSides];
            // Perpanjang sedikit di setiap sudut agar sambungan mulus
            Vector3 dir = (b - a).normalized;
            CreateContinuousPavedRoad(roadsParent.transform, $"Inner_Ring_Road_{i}",
                a - dir * 1.4f, b + dir * 1.4f, 3.6f, roadMat, 2.5f, true, curbMat);
        }

        // 3. Empat Poros Jalan Utama (Cardinal Avenues) dari Alun-Alun ke Gerbang Benteng
        // Poros Utara: Alun-Alun -> Pelataran Balai Desa (Z = 36)
        CreateContinuousPavedRoad(roadsParent.transform, "Avenue_North",
            new Vector3(0f, 0f, 10.2f), new Vector3(0f, 0f, 36f), 4.4f, roadMat, 2.5f, true, curbMat);

        // Pelataran Balai Desa (Civic Forecourt: X = -18..+18, Z = 34..39)
        CreateContinuousPavedRoad(roadsParent.transform, "Civic_Forecourt",
            new Vector3(-18f, 0f, 36f), new Vector3(18f, 0f, 36f), 5.0f, roadMat, 2.5f, true, curbMat);

        // Poros Selatan: Alun-Alun -> Gerbang Benteng Selatan (Z = -56)
        CreateContinuousPavedRoad(roadsParent.transform, "Avenue_South",
            new Vector3(0f, 0f, -10.2f), new Vector3(0f, 0f, -56f), 4.4f, roadMat, 2.5f, true, curbMat);

        // Poros Timur: Alun-Alun -> Gerbang Timur (X = 62)
        CreateContinuousPavedRoad(roadsParent.transform, "Avenue_East",
            new Vector3(10.2f, 0f, 0f), new Vector3(62f, 0f, 0f), 4.2f, roadMat, 2.5f, true, curbMat);

        // Poros Barat: Alun-Alun -> Jembatan Sungai -> Gerbang Barat (X = -62)
        // Berhenti di tepi timur jembatan (X = -32.5) dan lanjut dari tepi barat jembatan (X = -41.5)
        CreateContinuousPavedRoad(roadsParent.transform, "Avenue_West_Inner",
            new Vector3(-10.2f, 0f, 0f), new Vector3(-32.8f, 0f, 0f), 4.2f, roadMat, 2.5f, true, curbMat);
        CreateContinuousPavedRoad(roadsParent.transform, "Avenue_West_Outer",
            new Vector3(-41.2f, 0f, 0f), new Vector3(-62f, 0f, 0f), 4.2f, roadMat, 2.5f, true, curbMat);

        // 4. Jalan Setapak Penghubung ke Tiap Klaster Rumah
        // Ke Rumah Gadang (Barat Laut) & Pendopo Joglo (Timur Laut)
        CreateContinuousPavedRoad(roadsParent.transform, "Path_To_Gadang",
            new Vector3(-16.2f, 0f, 16.2f), new Vector3(-24f, 0f, 25f), 2.8f, roadMat, 2.5f, false);
        CreateContinuousPavedRoad(roadsParent.transform, "Path_To_Joglo",
            new Vector3(16.2f, 0f, 16.2f), new Vector3(24f, 0f, 25f), 2.8f, roadMat, 2.5f, false);

        // Ke Bengkel Pandai Besi & Gudang Logistik (Timur)
        CreateContinuousPavedRoad(roadsParent.transform, "Path_East_North",
            new Vector3(36f, 0f, 2.0f), new Vector3(36f, 0f, 14f), 2.8f, roadMat, 2.5f, false);
        CreateContinuousPavedRoad(roadsParent.transform, "Path_East_South",
            new Vector3(36f, 0f, -2.0f), new Vector3(36f, 0f, -14f), 2.8f, roadMat, 2.5f, false);

        // Ke Pondok Dukun Minka & A-Frame (Selatan)
        CreateContinuousPavedRoad(roadsParent.transform, "Path_South_West",
            new Vector3(-2.0f, 0f, -34f), new Vector3(-13f, 0f, -34f), 2.8f, roadMat, 2.5f, false);
        CreateContinuousPavedRoad(roadsParent.transform, "Path_South_East",
            new Vector3(2.0f, 0f, -34f), new Vector3(13f, 0f, -34f), 2.8f, roadMat, 2.5f, false);

        // Ke Area Pertanian Barat Laut (Utara Jalan Barat)
        CreateContinuousPavedRoad(roadsParent.transform, "Path_West_Farm",
            new Vector3(-28f, 0f, 2.0f), new Vector3(-28f, 0f, 13f), 2.8f, roadMat, 2.5f, false);

        // ═════════════════════════════════════════════════════════════════
        //  B. SUNGAI KECIL DESA (ELDARA BROOK) & JEMBATAN KAYU LENGKUNG
        // ═════════════════════════════════════════════════════════════════
        BuildVillageBrookAndBridge(village.transform, waterMat, woodMat, curbMat);

        // ═════════════════════════════════════════════════════════════════
        //  C. ALUN-ALUN PUSAT: SUMUR BATU, PASAR TERBUKA, & RAMBU ARAH
        // ═════════════════════════════════════════════════════════════════
        BuildVillageWell(village.transform, new Vector3(0f, 0f, 0f));

        // Rambu Petunjuk Arah di Sudut Alun-Alun
        InstantiateArtModel("EA03_Prop_Signpost_01a", new Vector3(-3.6f, 0f, 9.2f), Quaternion.Euler(0, 35f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Signpost_01a", new Vector3( 3.6f, 0f,-9.2f), Quaternion.Euler(0, 215f, 0), Vector3.one, village.transform, slavicMat);

        // Area Pasar Terbuka di Kuadran Tenggara Alun-Alun (X = 12..17, Z = -4..-13)
        InstantiateArtModel("Prop_Wagon", new Vector3(13.5f, 0f, -6.5f), Quaternion.Euler(0, -35f, 0), Vector3.one, village.transform);
        InstantiateArtModel("Prop_Wagon", new Vector3(11.0f, 0f, -11.5f), Quaternion.Euler(0, 45f, 0), Vector3.one, village.transform);
        InstantiateArtModel("EA03_Prop_Container_Crate_01a", new Vector3(15.2f, 0f, -8.5f), Quaternion.Euler(0, 15f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_02a", new Vector3(15.8f, 0f, -9.6f), Quaternion.Euler(0, 80f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Crate_03a", new Vector3(15.4f, 0.75f, -9.0f), Quaternion.Euler(0, 35f, 0), Vector3.one * 0.9f, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(13.2f, 0f, -12.5f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Container_Barrel_01d", new Vector3(14.0f, 0f, -12.8f), Quaternion.identity, Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Vegetable_Basket_02", new Vector3(12.2f, 0f, -8.0f), Quaternion.identity, Vector3.one, village.transform, slavicMat);

        // Taman Duduk Warga di Kuadran Barat Daya Alun-Alun
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-9.5f, 0f,  4.2f), Quaternion.Euler(0, 115f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3(-9.5f, 0f, -4.2f), Quaternion.Euler(0,  65f, 0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Town_Bench_01a", new Vector3( 9.5f, 0f,  4.2f), Quaternion.Euler(0, -115f, 0), Vector3.one, village.transform, slavicMat);

        // Lentera Alun-Alun (4 Sudut) & Lentera Jalan Lingkar + Poros Utama
        AddTraditionalVillageLamp(village.transform, new Vector3(-9.8f, 0,  9.8f),  135f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 9.8f, 0,  9.8f), -135f);
        AddTraditionalVillageLamp(village.transform, new Vector3(-9.8f, 0, -9.8f),   45f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 9.8f, 0, -9.8f),  -45f);

        // Lentera sepanjang Poros Utama & Jalan Lingkar
        AddTraditionalVillageLamp(village.transform, new Vector3(-3.0f, 0,  28f),  90f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 3.0f, 0,  28f), -90f);
        AddTraditionalVillageLamp(village.transform, new Vector3(-3.0f, 0, -28f),  90f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 3.0f, 0, -44f), -90f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 28f,  0,  2.8f), 180f);
        AddTraditionalVillageLamp(village.transform, new Vector3( 46f,  0, -2.8f),   0f);
        AddTraditionalVillageLamp(village.transform, new Vector3(-28f,  0,  2.8f), 180f);
        AddTraditionalVillageLamp(village.transform, new Vector3(-46f,  0, -2.8f),   0f);

        // ═════════════════════════════════════════════════════════════════
        //  D. TATA LETAK 16 BANGUNAN DESA (BERORIENTASI ORGANIK KE JALAN)
        // ═════════════════════════════════════════════════════════════════

        // ── 1. KAWASAN PEMERINTAHAN & ADAT (UTARA) ────────────────────────
        // Balai Desa Agung di puncak poros utara (menghadap Selatan lurus ke Sumur & Alun-Alun)
        var hall = ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.BalaiDesa,
            new Vector3(0f, 0f, 42f), Quaternion.Euler(0, 180f, 0), village.transform, "Bld_TownHall");
        if (hall) AddPorchLantern(hall.transform, new Vector3(0, 3.8f, -6.0f), range: 14f);

        // Rumah Gadang Kepala Desa (Barat Laut Jalan Lingkar, serong 135° menghadap pusat desa)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Gadang,
            new Vector3(-26f, 0f, 27f), Quaternion.Euler(0, 135f, 0), village.transform, "House_KepalaDesa_Gadang");
        CreateHouseYard(village.transform, "Yard_Gadang", new Vector3(-26f, 0f, 27f), 13f, 9f, 135f, roadMat);

        // Pendopo Pertemuan Joglo (Timur Laut Jalan Lingkar, serong -135° menghadap pusat desa)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Joglo,
            new Vector3(26f, 0f, 27f), Quaternion.Euler(0, -135f, 0), village.transform, "Pavilion_Joglo");
        CreateHouseYard(village.transform, "Yard_Joglo", new Vector3(26f, 0f, 27f), 13f, 12f, -135f, roadMat);

        // ── 2. KAWASAN PERTANIAN & TEPI SUNGAI (BARAT) ───────────────────
        // Rumah Petani Panggung (Utara Jalan Barat, menghadap Selatan ke jalan)
        var farmHouse = ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
            new Vector3(-26f, 0f, 16f), Quaternion.Euler(0, 180f, 0), village.transform, "House_Petani_Panggung");
        CreateHouseYard(village.transform, "Yard_Petani", new Vector3(-26f, 0f, 16.5f), 11.5f, 10.5f, 180f, roadMat);
        if (farmHouse)
        {
            InstantiateArtModel("EA03_Prop_Container_Bale_01a",   new Vector3(-21f, 0f, 9.5f), Quaternion.identity,          Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Container_Bale_01a",   new Vector3(-21f, 0.9f, 9.7f), Quaternion.Euler(0,40f,0), Vector3.one, village.transform, slavicMat);
            InstantiateArtModel("EA03_Prop_Vegetable_Basket_02",  new Vector3(-20f, 0f, 11.0f), Quaternion.identity,         Vector3.one, village.transform, slavicMat);
        }

        // Lumbung Padi / Barn Besar (Sebelah barat sungai di sisi utara, menghadap Selatan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Barn,
            new Vector3(-49f, 0f, 15f), Quaternion.Euler(0, 180f, 0), village.transform, "Barn_Gandum");
        CreateHouseYard(village.transform, "Yard_Barn", new Vector3(-49f, 0f, 15.5f), 13f, 11.5f, 180f, roadMat);

        // Chalet Warga Lembah (Barat Daya Jalan Lingkar, serong 45° menghadap pusat)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Chalet,
            new Vector3(-24f, 0f, -21f), Quaternion.Euler(0, 45f, 0), village.transform, "House_WargaBarat_Chalet");
        CreateHouseYard(village.transform, "Yard_ChaletBarat", new Vector3(-24f, 0f, -21f), 11f, 9.5f, 45f, roadMat);

        // Pondok Tepi Sungai / Danau (Menghadap Timur ke aliran sungai desa di X = -37)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Danau,
            new Vector3(-47f, 0f, -15f), Quaternion.Euler(0, 90f, 0), village.transform, "House_PondokDanau");

        // ── 3. KAWASAN PANDAI BESI & PENGRAJIN (TIMUR) ───────────────────
        // Bengkel Pandai Besi (Utara Jalan Timur, menghadap Selatan ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
            new Vector3(36f, 0f, 17f), Quaternion.Euler(0, 180f, 0), village.transform, "House_PandaiBesi");
        CreateHouseYard(village.transform, "Yard_PandaiBesi", new Vector3(36f, 0f, 16.5f), 11.5f, 10.5f, 180f, roadMat);
        InstantiateArtModel("EA03_Prop_Tool_Stump_02a",          new Vector3(31.5f, 0f, 9.5f),     Quaternion.identity,      Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Tool_Pick_01a",           new Vector3(31.8f,0.4f,9.7f), Quaternion.Euler(45,0,0), Vector3.one, village.transform, slavicMat);
        InstantiateArtModel("EA03_Prop_Forester_CutOutTrees_03", new Vector3(42f, 0f, 9.5f),     Quaternion.identity,      Vector3.one, village.transform, slavicMat);

        // Gudang Logistik Timur (Sebelah timur bengkel, menghadap Selatan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Chalet,
            new Vector3(49f, 0f, 17f), Quaternion.Euler(0, 180f, 0), village.transform, "Storage_PandaiBesi");
        CreateHouseYard(village.transform, "Yard_StorageTimur", new Vector3(49f, 0f, 16.5f), 11f, 10f, 180f, roadMat);

        // Rumah Pengrajin Kayu (Selatan Jalan Timur, menghadap Utara ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
            new Vector3(36f, 0f, -16f), Quaternion.Euler(0, 0f, 0), village.transform, "House_WargaTimur_Cabin");
        CreateHouseYard(village.transform, "Yard_CabinTimur", new Vector3(36f, 0f, -16.5f), 11.5f, 10f, 0f, roadMat);

        // Villa Kayu Modern Saudagar (Sebelah timur pengrajin, menghadap Utara ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Modern,
            new Vector3(49f, 0f, -16f), Quaternion.Euler(0, 0f, 0), village.transform, "Villa_Modern");

        // ── 4. KAWASAN PEMUKIMAN & GERBANG SELATAN ───────────────────────
        // Penginapan A-Frame Pengelana (Barat Poros Selatan, menghadap Timur ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.AFrame,
            new Vector3(-16f, 0f, -34f), Quaternion.Euler(0, 90f, 0), village.transform, "House_AFrame_Singgah");
        CreateHouseYard(village.transform, "Yard_AFrame", new Vector3(-17f, 0f, -34f), 11f, 12f, 90f, roadMat);

        // Pondok Dukun Mistis Minka (Timur Poros Selatan, menghadap Barat ke jalan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Minka,
            new Vector3(16f, 0f, -34f), Quaternion.Euler(0, -90f, 0), village.transform, "House_Dukun_Minka");

        // Pos Penjaga Gerbang Kiri & Kanan (Mengapit gerbang selatan)
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
            new Vector3(-14f, 0f, -47f), Quaternion.Euler(0, 0f, 0), village.transform, "House_Gerbang_Kiri");
        CreateHouseYard(village.transform, "Yard_GerbangKiri", new Vector3(-14f, 0f, -47f), 11f, 9.5f, 0f, roadMat);

        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Panggung,
            new Vector3(14f, 0f, -47f), Quaternion.Euler(0, 0f, 0), village.transform, "House_Gerbang_Kanan");
        CreateHouseYard(village.transform, "Yard_GerbangKanan", new Vector3(14f, 0f, -47f), 11.5f, 10f, 0f, roadMat);

        // ── 5. MENARA POHON PENGAWAS RIMBA ───────────────────────────────
        ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Pohon,
            new Vector3(74f, 0f, 40f), Quaternion.Euler(0, -135f, 0), village.transform, "Treehouse_Pengawas");
    }

    /// <summary>
    /// Membangun aliran Sungai Desa (Eldara Brook) di sisi barat (X = -37) dengan tepian batu
    /// serta Jembatan Kayu Lengkung (Arched Timber Bridge) yang mulus dilalui pemain.
    /// </summary>
    static void BuildVillageBrookAndBridge(Transform parent, Material waterMat, Material woodMat, Material stoneMat)
    {
        var brookRoot = new GameObject("Village_Brook_And_Bridge");
        brookRoot.transform.SetParent(parent, false);

        float brookX = -37.0f;
        float brookWidth = 5.4f;
        float brookLen = 104f; // Z = -54 sampai +50

        // 1. Dasar sungai gelap & permukaan air bergelombang
        var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bed.name = "Brook_Bed";
        bed.transform.SetParent(brookRoot.transform, false);
        bed.transform.position = new Vector3(brookX, -0.04f, -2f);
        bed.transform.localScale = new Vector3(brookWidth + 0.8f, 0.05f, brookLen);
        if (stoneMat) bed.GetComponent<Renderer>().sharedMaterial = stoneMat;
        Object.DestroyImmediate(bed.GetComponent<Collider>());

        var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "Brook_Water";
        water.transform.SetParent(brookRoot.transform, false);
        water.transform.position = new Vector3(brookX, 0.015f, -2f);
        water.transform.localScale = new Vector3(brookWidth, 0.03f, brookLen);
        if (waterMat) water.GetComponent<Renderer>().sharedMaterial = waterMat;
        Object.DestroyImmediate(water.GetComponent<Collider>());

        // 2. Tanggul Batu Tepi Sungai (Kiri & Kanan, menyisakan celah jembatan di Z = -2.4..+2.4)
        foreach (float side in new[] { -1f, 1f })
        {
            float bankX = brookX + side * (brookWidth * 0.5f + 0.25f);
            // Segmen Utara (Z = 2.5 .. 50)
            var bankN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bankN.name = "Brook_StoneBank_N";
            bankN.transform.SetParent(brookRoot.transform, false);
            bankN.transform.position = new Vector3(bankX, 0.04f, 26.25f);
            bankN.transform.localScale = new Vector3(0.45f, 0.14f, 47.5f);
            if (stoneMat) bankN.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(bankN.GetComponent<Collider>());

            // Segmen Selatan (Z = -54 .. -2.5)
            var bankS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bankS.name = "Brook_StoneBank_S";
            bankS.transform.SetParent(brookRoot.transform, false);
            bankS.transform.position = new Vector3(bankX, 0.04f, -28.25f);
            bankS.transform.localScale = new Vector3(0.45f, 0.14f, 51.5f);
            if (stoneMat) bankS.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(bankS.GetComponent<Collider>());
        }

        // 3. Jembatan Kayu Lengkung (X = -41.4 .. -32.6, lebar Z = 4.4m)
        var bridgeGo = new GameObject("Village_Wooden_Bridge");
        bridgeGo.transform.SetParent(brookRoot.transform, false);
        bridgeGo.transform.position = new Vector3(brookX, 0f, 0f);

        float bridgeSpan = 8.8f;
        float bridgeW = 4.4f;

        // Ramp naik timur, dek tengah datar, ramp turun barat (kemiringan landai ~7 derajat, mulus dinaiki)
        var deckMid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deckMid.name = "Bridge_Deck_Mid";
        deckMid.transform.SetParent(bridgeGo.transform, false);
        deckMid.transform.position = new Vector3(brookX, 0.34f, 0f);
        deckMid.transform.localScale = new Vector3(3.6f, 0.16f, bridgeW);
        if (woodMat) deckMid.GetComponent<Renderer>().sharedMaterial = woodMat;

        var rampE = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampE.name = "Bridge_Ramp_East";
        rampE.transform.SetParent(bridgeGo.transform, false);
        rampE.transform.position = new Vector3(brookX + 3.0f, 0.18f, 0f);
        rampE.transform.rotation = Quaternion.Euler(0f, 0f, 7.2f);
        rampE.transform.localScale = new Vector3(2.8f, 0.16f, bridgeW);
        if (woodMat) rampE.GetComponent<Renderer>().sharedMaterial = woodMat;

        var rampW = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampW.name = "Bridge_Ramp_West";
        rampW.transform.SetParent(bridgeGo.transform, false);
        rampW.transform.position = new Vector3(brookX - 3.0f, 0.18f, 0f);
        rampW.transform.rotation = Quaternion.Euler(0f, 0f, -7.2f);
        rampW.transform.localScale = new Vector3(2.8f, 0.16f, bridgeW);
        if (woodMat) rampW.GetComponent<Renderer>().sharedMaterial = woodMat;

        // Pagar Jembatan & Tiang Lentera Sudut Jembatan
        foreach (float sz in new[] { -1f, 1f })
        {
            float pz = sz * (bridgeW * 0.5f - 0.12f);
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Bridge_Handrail";
            rail.transform.SetParent(bridgeGo.transform, false);
            rail.transform.position = new Vector3(brookX, 1.15f, pz);
            rail.transform.localScale = new Vector3(bridgeSpan - 0.4f, 0.10f, 0.10f);
            if (woodMat) rail.GetComponent<Renderer>().sharedMaterial = woodMat;

            for (float px = -bridgeSpan * 0.45f; px <= bridgeSpan * 0.45f + 0.01f; px += 2.0f)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Bridge_Post";
                post.transform.SetParent(bridgeGo.transform, false);
                post.transform.position = new Vector3(brookX + px, 0.65f, pz);
                post.transform.localScale = new Vector3(0.16f, 1.15f, 0.16f);
                if (woodMat) post.GetComponent<Renderer>().sharedMaterial = woodMat;
            }
        }

        AddPorchLantern(bridgeGo.transform, new Vector3( 3.8f, 1.45f,  2.05f), new Color(1f, 0.78f, 0.38f), 10f);
        AddPorchLantern(bridgeGo.transform, new Vector3(-3.8f, 1.45f, -2.05f), new Color(1f, 0.78f, 0.38f), 10f);
    }

    static void CreateHouseYard(Transform parent, string name, Vector3 center, float width, float depth, float yawDeg, Material borderMat)
    {
        var yardGo = new GameObject(name);
        yardGo.transform.SetParent(parent, false);
        yardGo.transform.position = center;
        yardGo.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Yard_GroundPad";
        pad.transform.SetParent(yardGo.transform, false);
        pad.transform.localPosition = new Vector3(0f, 0.008f, 0f);
        pad.transform.localScale = new Vector3(width, 0.02f, depth);
        if (borderMat) pad.GetComponent<Renderer>().sharedMaterial = borderMat;
        Object.DestroyImmediate(pad.GetComponent<Collider>());

        var woodMat = GetMedievalMat("M_Medieval_WoodTrim");
        float postH = 0.85f;
        float hw = width * 0.5f;
        float hd = depth * 0.5f;

        void MakeRail(Vector3 pos, Vector3 scale)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Yard_Fence";
            r.transform.SetParent(yardGo.transform, false);
            r.transform.localPosition = pos;
            r.transform.localScale = scale;
            if (woodMat) r.GetComponent<Renderer>().sharedMaterial = woodMat;
        }

        MakeRail(new Vector3(-hw, postH * 0.65f, 0f), new Vector3(0.08f, 0.08f, depth));
        MakeRail(new Vector3( hw, postH * 0.65f, 0f), new Vector3(0.08f, 0.08f, depth));
        MakeRail(new Vector3(0f, postH * 0.65f, -hd), new Vector3(width, 0.08f, 0.08f));

        for (float z = -hd; z <= hd + 0.01f; z += 2.5f)
        {
            MakeRail(new Vector3(-hw, postH * 0.5f, z), new Vector3(0.14f, postH, 0.14f));
            MakeRail(new Vector3( hw, postH * 0.5f, z), new Vector3(0.14f, postH, 0.14f));
        }
        for (float x = -hw + 2.5f; x < hw; x += 2.5f)
        {
            MakeRail(new Vector3(x, postH * 0.5f, -hd), new Vector3(0.14f, postH, 0.14f));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  LANDMARK & STREET FURNITURE
    // ─────────────────────────────────────────────────────────────────────
    static Mesh CreateHollowRingMesh(string name, float outerR, float innerR, float topY, float bottomY, int segments = 32, bool addInnerWall = true)
    {
        var mesh = new Mesh { name = name };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        void AddQuad(int v0, int v1, int v2, int v3)
        {
            triangles.Add(v0); triangles.Add(v1); triangles.Add(v2);
            triangles.Add(v0); triangles.Add(v2); triangles.Add(v3);
            triangles.Add(v0); triangles.Add(v2); triangles.Add(v1);
            triangles.Add(v0); triangles.Add(v3); triangles.Add(v2);
        }

        int outerStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float u = (float)i / segments * 4f;

            vertices.Add(new Vector3(cos * outerR, bottomY, sin * outerR));
            normals.Add(new Vector3(cos, 0f, sin));
            uvs.Add(new Vector2(u, 0f));

            vertices.Add(new Vector3(cos * outerR, topY, sin * outerR));
            normals.Add(new Vector3(cos, 0f, sin));
            uvs.Add(new Vector2(u, 1f));
        }

        for (int i = 0; i < segments; i++)
        {
            int b = outerStart + i * 2;
            AddQuad(b, b + 1, b + 3, b + 2);
        }

        int rimStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices.Add(new Vector3(cos * outerR, topY, sin * outerR));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f));

            vertices.Add(new Vector3(cos * innerR, topY, sin * innerR));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + cos * 0.35f, 0.5f + sin * 0.35f));
        }

        for (int i = 0; i < segments; i++)
        {
            int b = rimStart + i * 2;
            AddQuad(b, b + 1, b + 3, b + 2);
        }

        if (addInnerWall)
        {
            int innerStart = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float u = (float)i / segments * 3f;

                vertices.Add(new Vector3(cos * innerR, topY, sin * innerR));
                normals.Add(new Vector3(-cos, 0f, -sin));
                uvs.Add(new Vector2(u, 1f));

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
            triangles.Add(0); triangles.Add(1 + i); triangles.Add(1 + i + 1);
            triangles.Add(0); triangles.Add(1 + i + 1); triangles.Add(1 + i);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

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

    static GameObject MakeWoodenBucket(Transform parent, Vector3 localPos, Quaternion localRot, Material woodMat, Material metalMat, Material darkMat)
    {
        var bucketGo = new GameObject("Well_Bucket");
        bucketGo.transform.SetParent(parent, false);
        bucketGo.transform.localPosition = localPos;
        bucketGo.transform.localRotation = localRot;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "BucketBody";
        body.transform.SetParent(bucketGo.transform, false);
        body.transform.localPosition = new Vector3(0, 0.10f, 0);
        body.transform.localScale = new Vector3(0.28f, 0.10f, 0.28f);
        if (woodMat) body.GetComponent<Renderer>().sharedMaterial = woodMat;
        else ApplyColor(body, new Color(0.35f, 0.22f, 0.12f));
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var cavity = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cavity.name = "BucketCavity";
        cavity.transform.SetParent(bucketGo.transform, false);
        cavity.transform.localPosition = new Vector3(0, 0.15f, 0);
        cavity.transform.localScale = new Vector3(0.23f, 0.05f, 0.23f);
        if (darkMat) cavity.GetComponent<Renderer>().sharedMaterial = darkMat;
        else ApplyColor(cavity, new Color(0.12f, 0.12f, 0.12f));
        Object.DestroyImmediate(cavity.GetComponent<Collider>());

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

    static Mesh CreateTessellatedWaterMesh(string name, float radius, float yLevel, int rings = 4, int segments = 36)
    {
        var mesh = new Mesh { name = name };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        vertices.Add(new Vector3(0f, yLevel, 0f));
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

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

        for (int s = 0; s < segments; s++)
        {
            int next = (s + 1) % segments;
            triangles.Add(0); triangles.Add(1 + s); triangles.Add(1 + next);
            triangles.Add(0); triangles.Add(1 + next); triangles.Add(1 + s);
        }

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

                triangles.Add(i0); triangles.Add(o0); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(o0); triangles.Add(o1);
                triangles.Add(i0); triangles.Add(i1); triangles.Add(o0);
                triangles.Add(i1); triangles.Add(o1); triangles.Add(o0);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

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

        mat.SetColor("_DeepColor", new Color(0.03f, 0.18f, 0.32f, 0.90f));
        mat.SetColor("_ShallowColor", new Color(0.16f, 0.62f, 0.78f, 0.65f));
        mat.SetColor("_FresnelColor", new Color(0.85f, 0.95f, 1.0f, 1.0f));
        mat.SetFloat("_Smoothness", 0.96f);
        mat.SetFloat("_WaveSpeed", 1.35f);
        mat.SetFloat("_WaveScale", 7.5f);
        mat.SetFloat("_WaveStrength", 0.42f);
        mat.SetFloat("_RippleFrequency", 24.0f);
        mat.SetFloat("_RippleSpeed", 2.6f);
        mat.SetFloat("_RippleStrength", 0.38f);
        return mat;
    }

    static void BuildVillageWell(Transform parent, Vector3 pos)
    {
        var wellRoot = new GameObject("Village_Stone_Well");
        wellRoot.transform.SetParent(parent);
        wellRoot.transform.position = pos;

        var brickMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_UnevenBrick.mat");
        var rockMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_RockTrim.mat");
        var woodMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_WoodTrim.mat");
        var roofMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Medieval_RoundTiles.mat");
        var waterMat = GetOrCreateRealisticWaterMaterial();
        var darkMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Dark.mat");
        var metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Metal.mat");

        var stepMesh = CreateHollowRingMesh("Well_StepBase_Mesh", outerR: 1.65f, innerR: 0.95f, topY: 0.10f, bottomY: 0f, segments: 32, addInnerWall: false);
        CreateMeshObject("Well_StepBase", wellRoot.transform, stepMesh, rockMat, new Color(0.62f, 0.60f, 0.55f));

        var curbMesh = CreateHollowRingMesh("Well_StoneCurb_Mesh", outerR: 1.25f, innerR: 0.88f, topY: 0.95f, bottomY: 0f, segments: 32, addInnerWall: true);
        CreateMeshObject("Well_StoneCurb", wellRoot.transform, curbMesh, brickMat, new Color(0.48f, 0.46f, 0.44f), addMeshCollider: true);

        var rimMesh = CreateHollowRingMesh("Well_StoneRim_Mesh", outerR: 1.30f, innerR: 0.85f, topY: 0.99f, bottomY: 0.93f, segments: 32, addInnerWall: true);
        CreateMeshObject("Well_StoneRim", wellRoot.transform, rimMesh, rockMat, new Color(0.55f, 0.53f, 0.50f));

        var floorMesh = CreateCircularDiscMesh("Well_Floor_Mesh", radius: 0.88f, yLevel: 0.05f, segments: 32);
        CreateMeshObject("Well_ShaftFloor", wellRoot.transform, floorMesh, rockMat, new Color(0.18f, 0.18f, 0.16f));

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
            if (p % 2 == 0) ApplyColor(pebble, new Color(0.85f, 0.72f, 0.25f));
            else if (darkMat) pebble.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.DestroyImmediate(pebble.GetComponent<Collider>());
        }

        var waterMesh = CreateTessellatedWaterMesh("Well_Water_Mesh", radius: 0.875f, yLevel: 0.35f, rings: 4, segments: 36);
        CreateMeshObject("Well_Water", wellRoot.transform, waterMesh, waterMat, new Color(0.12f, 0.45f, 0.68f));

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
        MakeTimber("Well_Crossbeam", new Vector3(0f, 2.45f, 0f), new Vector3(2.50f, 0.18f, 0.18f));
        MakeTimber("Well_Brace_L", new Vector3(-0.78f, 2.25f, 0f), new Vector3(0.09f, 0.42f, 0.09f), Quaternion.Euler(0, 0, -45f));
        MakeTimber("Well_Brace_R", new Vector3( 0.78f, 2.25f, 0f), new Vector3(0.09f, 0.42f, 0.09f), Quaternion.Euler(0, 0,  45f));

        var spindle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spindle.name = "Well_Spindle";
        spindle.transform.SetParent(wellRoot.transform, false);
        spindle.transform.localPosition = new Vector3(0f, 2.22f, 0f);
        spindle.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        spindle.transform.localScale = new Vector3(0.15f, 0.60f, 0.15f);
        if (woodMat) spindle.GetComponent<Renderer>().sharedMaterial = woodMat;
        else ApplyColor(spindle, new Color(0.68f, 0.58f, 0.42f));
        Object.DestroyImmediate(spindle.GetComponent<Collider>());

        var coil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coil.name = "Well_RopeCoil";
        coil.transform.SetParent(wellRoot.transform, false);
        coil.transform.localPosition = new Vector3(0f, 2.22f, 0f);
        coil.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        coil.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
        ApplyColor(coil, new Color(0.68f, 0.58f, 0.42f));
        Object.DestroyImmediate(coil.GetComponent<Collider>());

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

        var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rope.name = "Well_Rope";
        rope.transform.SetParent(wellRoot.transform, false);
        rope.transform.localPosition = new Vector3(0f, 1.40f, 0f);
        rope.transform.localScale = new Vector3(0.035f, 0.80f, 0.035f);
        ApplyColor(rope, new Color(0.68f, 0.58f, 0.42f));
        Object.DestroyImmediate(rope.GetComponent<Collider>());

        MakeWoodenBucket(wellRoot.transform, new Vector3(0f, 0.48f, 0f), Quaternion.identity, woodMat, metalMat, darkMat);
        MakeWoodenBucket(wellRoot.transform, new Vector3(0.82f, 0.99f, 0.32f), Quaternion.Euler(0, 35f, 0), woodMat, metalMat, darkMat);

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

        MakeTimber("Well_Roof_Ridge", new Vector3(0f, 3.22f, 0f), new Vector3(0.12f, 0.12f, 2.25f));
        AddPorchLantern(wellRoot.transform, new Vector3(0f, 2.05f, 0f), new Color(1f, 0.78f, 0.42f), 10f);
    }

    static void AddTraditionalVillageLamp(Transform parent, Vector3 pos, float yRotation = 0f)
    {
        var lampGo = new GameObject("TraditionalVillageLamp");
        lampGo.transform.SetParent(parent);
        lampGo.transform.position = pos;
        lampGo.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Wood.mat");
        var darkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Dark.mat");
        var metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Metal.mat");
        var lampMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HouseMaterials/M_House_Lamp.mat");
        var stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MedievalVillage/Materials/M_Village_Cobblestone_Road.mat");

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
            else ApplyColor(go, fallbackColor);
            return go;
        }

        MakePart("StoneBase", PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), new Vector3(0.46f, 0.18f, 0.46f), stoneMat, new Color(0.48f, 0.46f, 0.42f));
        MakePart("BaseRing", PrimitiveType.Cylinder, new Vector3(0, 0.36f, 0), new Vector3(0.24f, 0.04f, 0.24f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("TimberPost", PrimitiveType.Cylinder, new Vector3(0, 1.45f, 0), new Vector3(0.18f, 1.10f, 0.18f), woodMat, new Color(0.26f, 0.16f, 0.09f));
        MakePart("IronCollarUpper", PrimitiveType.Cylinder, new Vector3(0, 2.30f, 0), new Vector3(0.21f, 0.04f, 0.21f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("PostCap", PrimitiveType.Cube, new Vector3(0, 2.58f, 0), new Vector3(0.22f, 0.08f, 0.22f), darkMat, new Color(0.20f, 0.12f, 0.07f));
        MakePart("GallowsArm", PrimitiveType.Cube, new Vector3(0, 2.50f, 0.42f), new Vector3(0.12f, 0.12f, 0.84f), woodMat, new Color(0.26f, 0.16f, 0.09f));
        MakePart("ArmEndCap", PrimitiveType.Cube, new Vector3(0, 2.50f, 0.85f), new Vector3(0.14f, 0.14f, 0.04f), darkMat, new Color(0.20f, 0.12f, 0.07f));

        var strut = MakePart("DiagonalStrut", PrimitiveType.Cube, new Vector3(0, 2.26f, 0.24f), new Vector3(0.09f, 0.09f, 0.42f), woodMat, new Color(0.26f, 0.16f, 0.09f));
        strut.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

        MakePart("IronHook", PrimitiveType.Cube, new Vector3(0, 2.41f, 0.72f), new Vector3(0.04f, 0.08f, 0.04f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("ChainLink1", PrimitiveType.Cylinder, new Vector3(0, 2.33f, 0.72f), new Vector3(0.03f, 0.05f, 0.03f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("ChainLink2", PrimitiveType.Cylinder, new Vector3(0, 2.24f, 0.72f), new Vector3(0.03f, 0.05f, 0.03f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        float lz = 0.72f;
        MakePart("LanternRing", PrimitiveType.Cylinder, new Vector3(0, 2.19f, lz), new Vector3(0.08f, 0.02f, 0.08f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("RoofTop", PrimitiveType.Cube, new Vector3(0, 2.16f, lz), new Vector3(0.24f, 0.04f, 0.24f), metalMat, new Color(0.18f, 0.18f, 0.18f));
        MakePart("RoofEaves", PrimitiveType.Cube, new Vector3(0, 2.12f, lz), new Vector3(0.36f, 0.03f, 0.36f), metalMat, new Color(0.22f, 0.22f, 0.22f));
        MakePart("LanternBase", PrimitiveType.Cube, new Vector3(0, 1.83f, lz), new Vector3(0.30f, 0.04f, 0.30f), metalMat, new Color(0.22f, 0.22f, 0.22f));
        MakePart("BaseFinial", PrimitiveType.Cylinder, new Vector3(0, 1.79f, lz), new Vector3(0.06f, 0.04f, 0.06f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        float[] cOffsets = { -0.12f, 0.12f };
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                MakePart($"Strut_{i}_{j}", PrimitiveType.Cube, new Vector3(cOffsets[i], 1.975f, lz + cOffsets[j]), new Vector3(0.022f, 0.27f, 0.022f), metalMat, new Color(0.18f, 0.18f, 0.18f));

        var glassCore = MakePart("GlassCore", PrimitiveType.Cylinder, new Vector3(0, 1.975f, lz), new Vector3(0.18f, 0.13f, 0.18f), lampMat, new Color(1f, 0.85f, 0.45f));
        var glassRend = glassCore.GetComponent<Renderer>();

        var lightGo = new GameObject("LanternPointLight");
        lightGo.transform.SetParent(lampGo.transform);
        lightGo.transform.localPosition = new Vector3(0, 1.975f, lz);

        var pLight = lightGo.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.74f, 0.38f);
        pLight.intensity = 2.0f;
        pLight.range = 11f;
        pLight.shadows = LightShadows.None;

        var nl = lampGo.AddComponent<NightLight>();
        nl.Configure(pLight, glassRend, new Color(1f, 0.74f, 0.38f), 2.0f, true);

        var col = lampGo.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1.35f, 0);
        col.height = 2.7f;
        col.radius = 0.25f;
    }

    static void AddStreetLamp(Transform parent, Vector3 pos)
    {
        AddTraditionalVillageLamp(parent, pos, 0f);
    }

    static Material GetMedievalMat(string matName)
    {
        string path = $"Assets/_Project/Art/MedievalVillage/Materials/{matName}.mat";
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

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

    static GameObject CreateContinuousPavedRoad(
        Transform parent,
        string name,
        Vector3 start,
        Vector3 end,
        float width,
        Material roadMat,
        float uvScale = 2.5f,
        bool addCurbs = false,
        Material curbMat = null)
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

        float topY = 0.025f;
        float botY = -0.05f;

        Vector3 halfW = right * (width * 0.5f);
        Vector3 p0 = start - halfW;
        Vector3 p1 = start + halfW;
        Vector3 p2 = end - halfW;
        Vector3 p3 = end + halfW;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        int idx = verts.Count;
        verts.Add(new Vector3(p0.x, topY, p0.z));
        verts.Add(new Vector3(p1.x, topY, p1.z));
        verts.Add(new Vector3(p2.x, topY, p2.z));
        verts.Add(new Vector3(p3.x, topY, p3.z));

        uvs.Add(new Vector2(p0.x / uvScale, p0.z / uvScale));
        uvs.Add(new Vector2(p1.x / uvScale, p1.z / uvScale));
        uvs.Add(new Vector2(p2.x / uvScale, p2.z / uvScale));
        uvs.Add(new Vector2(p3.x / uvScale, p3.z / uvScale));

        norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up);
        tris.Add(idx + 0); tris.Add(idx + 2); tris.Add(idx + 1);
        tris.Add(idx + 1); tris.Add(idx + 2); tris.Add(idx + 3);

        void AddQuadVerts(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            uvs.Add(new Vector2(a.x / uvScale, a.z / uvScale));
            uvs.Add(new Vector2(b.x / uvScale, b.z / uvScale));
            uvs.Add(new Vector2(c.x / uvScale, c.z / uvScale));
            uvs.Add(new Vector2(d.x / uvScale, d.z / uvScale));
            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
            tris.Add(s + 0); tris.Add(s + 2); tris.Add(s + 1);
            tris.Add(s + 1); tris.Add(s + 2); tris.Add(s + 3);
        }

        AddQuadVerts(new Vector3(p0.x, topY, p0.z), new Vector3(p2.x, topY, p2.z), new Vector3(p0.x, botY, p0.z), new Vector3(p2.x, botY, p2.z), -right);
        AddQuadVerts(new Vector3(p3.x, topY, p3.z), new Vector3(p1.x, topY, p1.z), new Vector3(p3.x, botY, p3.z), new Vector3(p1.x, botY, p1.z), right);
        AddQuadVerts(new Vector3(p1.x, topY, p1.z), new Vector3(p0.x, topY, p0.z), new Vector3(p1.x, botY, p1.z), new Vector3(p0.x, botY, p0.z), -forward);
        AddQuadVerts(new Vector3(p2.x, topY, p2.z), new Vector3(p3.x, topY, p3.z), new Vector3(p2.x, botY, p2.z), new Vector3(p3.x, botY, p3.z), forward);

        var mesh = new Mesh { name = $"Mesh_{name}" };
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

        if (addCurbs)
        {
            Material cMat = curbMat ? curbMat : roadMat;
            Quaternion q = Quaternion.LookRotation(forward);
            Vector3 mid = (start + end) * 0.5f;
            float curbOffset = width * 0.5f + 0.14f;

            foreach (float side in new[] { -1f, 1f })
            {
                var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = side < 0 ? "Curb_Left" : "Curb_Right";
                curb.transform.SetParent(roadGO.transform, false);
                curb.transform.position = mid + right * (side * curbOffset) + new Vector3(0f, 0.02f, 0f);
                curb.transform.rotation = q;
                curb.transform.localScale = new Vector3(0.28f, 0.07f, length);
                curb.GetComponent<Renderer>().sharedMaterial = cMat;
                Object.DestroyImmediate(curb.GetComponent<Collider>());
            }
        }

        return roadGO;
    }

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
        int segments = 56;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        int centerIdx = verts.Count;
        verts.Add(new Vector3(center.x, topY, center.z));
        uvs.Add(new Vector2(center.x / uvScale, center.z / uvScale));
        norms.Add(Vector3.up);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) * Mathf.PI * 2f / segments;
            float px = center.x + Mathf.Cos(angle) * radius;
            float pz = center.z + Mathf.Sin(angle) * radius;

            verts.Add(new Vector3(px, topY, pz));
            uvs.Add(new Vector2(px / uvScale, pz / uvScale));
            norms.Add(Vector3.up);
        }

        for (int i = 1; i <= segments; i++)
        {
            tris.Add(centerIdx);
            tris.Add(i + 1);
            tris.Add(i);
        }

        int skirtStartIdx = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) * Mathf.PI * 2f / segments;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float px = center.x + cos * radius;
            float pz = center.z + sin * radius;

            verts.Add(new Vector3(px, topY, pz));
            uvs.Add(new Vector2(px / uvScale, pz / uvScale));
            norms.Add(new Vector3(cos, 0f, sin));

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

            tris.Add(t0); tris.Add(t1); tris.Add(b0);
            tris.Add(b0); tris.Add(t1); tris.Add(b1);
        }

        var mesh = new Mesh { name = $"Mesh_{name}" };
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
    //  6. NPC POPULATION (MENGIKUTI RUTE JALAN LINGKAR & POROS DESA)
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
            ("NPC_KepalaDesa",
             ProceduralNpcBuilder.NpcStyle.Merchant,
             new Vector3(0f, 0.9f, 9.0f), 180f,
             "Selamat datang, petualang! Desa Eldara dalam bahaya besar. Saat matahari terbenam, pasukan monster akan menyerbu dari rimba kegelapan hutan. Persiapkan persenjataan dan dirikan menara panah!",
             new Vector3[] {
                 new Vector3(0f, 0.9f,  9.0f),
                 new Vector3(0f, 0.14f, 16.0f),
                 new Vector3(0f, 0.14f, 32.0f),
                 new Vector3(0f, 0.14f, 16.0f)
             }),

            ("NPC_Petani",
             ProceduralNpcBuilder.NpcStyle.Farmer,
             new Vector3(-16.2f, 0.14f, 16.2f), 90f,
             "Aduh gawat, kawanan serigala buas kemarin malam merusak pagar gandumku! Tolong tebang balok kayu di tepi hutan rimba untuk memperbaiki lumbung kami.",
             new Vector3[] {
                 new Vector3(-16.2f, 0.14f, 16.2f),
                 new Vector3(-23.0f, 0.14f,  0.0f),
                 new Vector3(-28.0f, 0.14f,  0.0f),
                 new Vector3(-23.0f, 0.14f,  0.0f)
             }),

            ("NPC_PandaiBesi",
             ProceduralNpcBuilder.NpcStyle.Guard,
             new Vector3(23.0f, 0.14f, 0.0f), 180f,
             "Salam, ksatria! Aku butuh bijih besi murni dan batu keras dari area tambang timur. Bawakan padaku, dan aku akan merakit menara panah yang kokoh!",
             new Vector3[] {
                 new Vector3(23.0f, 0.14f,  0.0f),
                 new Vector3(36.0f, 0.14f,  0.0f),
                 new Vector3(36.0f, 0.14f,  9.0f),
                 new Vector3(36.0f, 0.14f,  0.0f)
             }),

            ("NPC_Dukun",
             ProceduralNpcBuilder.NpcStyle.Wizard,
             new Vector3(0f, 0.14f, -23.0f), 0f,
             "Altar Segel Kuno di utara berdenyut dengan energi gelap... Jika kamu berani mengaktifkannya saat malam tiba, Raja Monster akan terpanggil. Pastikan menara bentengmu sudah siap!",
             new Vector3[] {
                 new Vector3(  0.0f, 0.14f, -23.0f),
                 new Vector3( 16.2f, 0.14f, -16.2f),
                 new Vector3(  0.0f, 0.14f, -34.0f),
                 new Vector3(-16.2f, 0.14f, -16.2f)
             }),

            ("NPC_GadisDesa",
             ProceduralNpcBuilder.NpcStyle.Woman,
             new Vector3(0f, 0.05f, 5.5f), 180f,
             "Halo petualang! Udara di desa Eldara hari ini begitu sejuk dan damai. Berhati-hatilah saat senja menjelang ya, kabut hutan sering membawa bahaya tak terduga!",
             new Vector3[] {
                 new Vector3( 0.0f, 0.05f,  5.5f),
                 new Vector3( 5.5f, 0.05f,  0.0f),
                 new Vector3( 0.0f, 0.05f, -5.5f),
                 new Vector3(-5.5f, 0.05f,  0.0f)
             }),

            ("NPC_PenjagaBenteng",
             ProceduralNpcBuilder.NpcStyle.Guard,
             new Vector3(-3.5f, 0.05f, -50f), 0f,
             "Berhenti! Aku kapten regu pengawal benteng Eldara. Kami bertugas mengawasi gerbang selatan ini dari serbuan kawanan monster malam hari!",
             new Vector3[] {
                 new Vector3(-3.5f, 0.05f, -50f),
                 new Vector3( 3.5f, 0.05f, -50f),
                 new Vector3( 0.0f, 0.05f, -44f),
                 new Vector3( 0.0f, 0.05f, -54f)
             })
        };

        foreach (var (npcName, style, startPos, yRot, greeting, route) in npcConfigs)
        {
            var npcGo = ProceduralNpcBuilder.BuildNpc(style, startPos, Quaternion.Euler(0, yRot, 0), npcRoot.transform, npcName);
            var npcDlg = npcGo.GetComponent<NpcDialogue>() ?? npcGo.AddComponent<NpcDialogue>();
            CreateSimpleDialogue(npcName, greeting, npcDlg);

            var mover = npcGo.GetComponent<NpcMover>();
            if (mover)
            {
                mover.SetWaypoints(new List<Vector3>(route));
                mover.moveSpeed = 1.35f;
                mover.waitTimeAtWaypoint = Random.Range(3.5f, 5.5f);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  7. BENTENG & PERTAHANAN (8 MENARA + PALISADE KELILING + GERBANG)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupFortifications(Material slavicMat)
    {
        GameObject towerRoot = GameObject.Find("--- TOWERS ---");
        if (towerRoot) Object.DestroyImmediate(towerRoot);

        towerRoot = new GameObject("--- TOWERS ---");

        var towerSlots = new (Vector3 pos, string name)[]
        {
            (new Vector3(-6.8f, 0f, -56f), "Tower_Gate_West"),
            (new Vector3( 6.8f, 0f, -56f), "Tower_Gate_East"),
            (new Vector3(-62f,  0f,  52f), "Tower_NorthWest"),
            (new Vector3( 62f,  0f,  52f), "Tower_NorthEast"),
            (new Vector3(-62f,  0f, -56f), "Tower_SouthWest"),
            (new Vector3( 62f,  0f, -56f), "Tower_SouthEast"),
            (new Vector3(-62f,  0f,   7.5f), "Tower_WestFlank"),
            (new Vector3( 62f,  0f,   7.5f), "Tower_EastFlank"),
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

            AddStairRamp(towerObj.transform, new Vector3(0f, 1.75f, -2.0f), new Vector3(1.2f, 0.12f, 3.0f), new Vector3(-55f, 0f, 0f));
            AddStairRamp(towerObj.transform, new Vector3(0f, 3.4f, -1.0f), new Vector3(1.2f, 0.12f, 0.8f), new Vector3(-10f, 0f, 0f));
            AddPorchLantern(towerObj.transform, new Vector3(0, 4.5f, 0), new Color(1f, 0.65f, 0.2f), 12f);
        }

        // Gerbang Masuk Selatan Megah
        var gateArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateArch.name = "SouthGate_Crossbeam";
        gateArch.transform.SetParent(towerRoot.transform);
        gateArch.transform.position = new Vector3(0f, 5.2f, -56f);
        gateArch.transform.localScale = new Vector3(14.5f, 0.55f, 0.7f);
        ApplyColor(gateArch, new Color(0.28f, 0.17f, 0.09f));

        AddPorchLantern(towerRoot.transform, new Vector3(-3.2f, 4.7f, -56f), new Color(1f, 0.75f, 0.35f), 14f);
        AddPorchLantern(towerRoot.transform, new Vector3( 3.2f, 4.7f, -56f), new Color(1f, 0.75f, 0.35f), 14f);

        void BuildPalisadeLine(Vector3 start, Vector3 end)
        {
            Vector3 diff = end - start;
            float dist = diff.magnitude;
            int count = Mathf.FloorToInt(dist / 2.2f);
            Quaternion rot = Quaternion.LookRotation(diff.normalized) * Quaternion.Euler(0, 90f, 0);
            for (int i = 1; i < count; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, i / (float)count);
                var wall = InstantiateArtModel("EA03_Village_Fence_01a", p, rot, new Vector3(1.1f, 1.4f, 1.1f), towerRoot.transform, slavicMat);
                if (!wall)
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Palisade_Wall";
                    wall.transform.SetParent(towerRoot.transform);
                    wall.transform.position = p + Vector3.up * 1.4f;
                    wall.transform.rotation = rot;
                    wall.transform.localScale = new Vector3(2.2f, 2.8f, 0.35f);
                    ApplyColor(wall, new Color(0.32f, 0.20f, 0.11f));
                }
            }
        }

        // Pagar keliling perimeter kayu ditiadakan agar alam desa terbuka plong dan jalan bebas hambatan
        // (BuildPalisadeLine ditiadakan sesuai permintaan penghapusan pagar)

        var trapPositions = new Vector3[]
        {
            new Vector3(-4.5f, 0.05f, -64f),
            new Vector3( 4.5f, 0.05f, -64f),
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
    //  8. HUTAN RIMBA & TAMAN PENEDUH DALAM DESA
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

                // ── POHON PENEDUH & RIMBUN DI DALAM DESA (TAMAN, PEKARANGAN, & TEPI SUNGAI) ──
        var innerTrees = new (Vector3 pos, int type)[]
        {
            // 1. Empat Sudut Alun-Alun Pusat
            (new Vector3(-11.5f, 0f,  11.5f), 0), (new Vector3( 11.5f, 0f,  11.5f), 1),
            (new Vector3(-11.5f, 0f, -11.5f), 1), (new Vector3( 11.5f, 0f, -11.5f), 2),

            // 2. Bantaran Sungai Eldara Barat (X = -31 s/d -45, peneduh tepi air yang asri)
            (new Vector3(-32f, 0f,  12f), 1), (new Vector3(-32f, 0f,  24f), 0), (new Vector3(-32f, 0f,  36f), 2),
            (new Vector3(-32f, 0f, -12f), 0), (new Vector3(-32f, 0f, -24f), 1), (new Vector3(-32f, 0f, -36f), 2),
            (new Vector3(-42f, 0f,   8f), 2), (new Vector3(-42f, 0f,  26f), 1), (new Vector3(-42f, 0f,  38f), 0),
            (new Vector3(-42f, 0f,  -8f), 1), (new Vector3(-42f, 0f, -26f), 0), (new Vector3(-42f, 0f, -38f), 2),

            // 3. Kawasan Balai Desa & Hutan Lindung Utara (Belakang & Samping Balairung Agung)
            (new Vector3(  0f, 0f,  51f), 3), (new Vector3(-12f, 0f,  49f), 0), (new Vector3( 12f, 0f,  49f), 2),
            (new Vector3(-24f, 0f,  46f), 3), (new Vector3( 24f, 0f,  46f), 3),
            (new Vector3(-16f, 0f,  36f), 0), (new Vector3( 16f, 0f,  36f), 1),
            (new Vector3(-36f, 0f,  32f), 2), (new Vector3( 36f, 0f,  32f), 0),

            // 4. Kawasan Pandai Besi & Pengrajin Timur
            (new Vector3( 18f, 0f,  22f), 1), (new Vector3( 34f, 0f,  24f), 0), (new Vector3( 52f, 0f,  26f), 2),
            (new Vector3( 18f, 0f, -22f), 0), (new Vector3( 34f, 0f, -24f), 2), (new Vector3( 52f, 0f, -26f), 1),
            (new Vector3( 54f, 0f,   8f), 0), (new Vector3( 54f, 0f,  -8f), 2),

            // 5. Kawasan Pemukiman & Gerbang Selatan
            (new Vector3(-24f, 0f, -32f), 2), (new Vector3(-26f, 0f, -44f), 0), (new Vector3(-36f, 0f, -48f), 1),
            (new Vector3( 24f, 0f, -32f), 1), (new Vector3( 26f, 0f, -44f), 2), (new Vector3( 36f, 0f, -48f), 0),
            (new Vector3(-12f, 0f, -52f), 0), (new Vector3( 12f, 0f, -52f), 1),
            (new Vector3(-48f, 0f, -48f), 3), (new Vector3( 48f, 0f, -48f), 3)
        };

        foreach (var (pos, tType) in innerTrees)
        {
            float scale = Random.Range(1.10f, 1.45f);
            GameObject treeObj = null;
            switch (tType)
            {
                case 0: treeObj = RealisticTreeBuilder.BuildOakTree(pos, scale, natureRoot.transform, barkMat, summerLeafMat); break;
                case 1: treeObj = RealisticTreeBuilder.BuildBirchTree(pos, scale, natureRoot.transform, birchBarkMat, springLeafMat); break;
                case 2: treeObj = RealisticTreeBuilder.BuildAutumnOakTree(pos, scale, natureRoot.transform, barkMat, autumnLeafMat); break;
                case 3: treeObj = RealisticTreeBuilder.BuildPineTree(pos, scale, natureRoot.transform, barkMat, summerLeafMat); break;
                default: treeObj = RealisticTreeBuilder.BuildOakTree(pos, scale, natureRoot.transform, barkMat, summerLeafMat); break;
            }

            if (treeObj)
            {
                var sway = treeObj.GetComponent<TreeWindSway>();
                if (sway) sway.enableFallingLeaves = true;

                InstantiateArtModel("EA03_Nature_TrunkPlant_01a", pos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * 1.0f, natureRoot.transform, slavicMat);

                int bushCount = Random.Range(1, 3);
                for (int b = 0; b < bushCount; b++)
                {
                    string bFbx = bushModels[Random.Range(0, bushModels.Length)];
                    Vector3 bushPos = pos + new Vector3(Random.Range(-1.8f, 1.8f), 0, Random.Range(-1.8f, 1.8f));
                    var bush = InstantiateArtModel(bFbx, bushPos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one * Random.Range(0.85f, 1.25f), natureRoot.transform, slavicMat);
                    if (bush)
                    {
                        var bSway = bush.AddComponent<TreeWindSway>();
                        bSway.swayIntensity = 1.0f;
                        bSway.swaySpeed = 1.8f;
                    }
                }
            }
        }

        BuildNaturalForest(natureRoot.transform, barkMat, birchBarkMat, summerLeafMat, springLeafMat, autumnLeafMat, slavicMat, bushModels, rockModels);
        BuildMapBoundaryWalls(natureRoot.transform, half: 160f, height: 60f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8b. HAMPARAN PADANG RUMPUT 3D & BUNGA LIAR (STERIL DARI JALAN & SUNGAI)
    // ─────────────────────────────────────────────────────────────────────
        static void SetupRealisticGrassFields(Material grassMat)
    {
        GameObject grassRoot = GameObject.Find("--- GRASS FIELDS ---");
        if (grassRoot) Object.DestroyImmediate(grassRoot);

        grassRoot = new GameObject("--- GRASS FIELDS ---");

        var flowerRoot = new GameObject("--- 3D WILDFLOWERS ---");
        flowerRoot.transform.SetParent(grassRoot.transform);

        if (!grassMat) return;

        var flowerColors = new RealisticFlowerBuilder.FlowerColor[]
        {
            RealisticFlowerBuilder.FlowerColor.RedPoppy,
            RealisticFlowerBuilder.FlowerColor.GoldenDandelion,
            RealisticFlowerBuilder.FlowerColor.SkyBluebell,
            RealisticFlowerBuilder.FlowerColor.MysticPurple,
            RealisticFlowerBuilder.FlowerColor.WhiteDaisy
        };

        // 1. BORDIR RUMPUT TEBAL & BUNGA DI KEDUA TEPI SUNGAI DESA (X = -37)
        for (float z = -52f; z <= 48f; z += 2.2f)
        {
            if (Mathf.Abs(z) < 4.5f) continue; // celah jembatan
            // Tepi timur sungai
            RealisticGrassBuilder.BuildGrassClump(new Vector3(-33.0f, 0, z), 14, 1.8f, grassRoot.transform, grassMat);
            var colE = flowerColors[Mathf.Abs(Mathf.RoundToInt(z * 3)) % flowerColors.Length];
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(-33.0f, 0, z), 6, 1.4f, flowerRoot.transform, colE);

            // Tepi barat sungai
            RealisticGrassBuilder.BuildGrassClump(new Vector3(-41.0f, 0, z), 14, 1.8f, grassRoot.transform, grassMat);
            var colW = flowerColors[Mathf.Abs(Mathf.RoundToInt(z * 5)) % flowerColors.Length];
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(-41.0f, 0, z), 6, 1.4f, flowerRoot.transform, colW);
        }

        // 2. TAMAN BUNGA & RUMPUT DI SEKITAR ALUN-ALUN PUSAT (R = 10.5m, membingkai plaza melingkar)
        const int ringPoints = 20;
        for (int i = 0; i < ringPoints; i++)
        {
            float ang = (i / (float)ringPoints) * 360f * Mathf.Deg2Rad;
            float r = 10.8f + Random.Range(-0.4f, 0.6f);
            Vector3 pRing = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            if (Mathf.Abs(pRing.x) < 3.0f || Mathf.Abs(pRing.z) < 3.0f) continue;

            RealisticGrassBuilder.BuildGrassClump(pRing, 16, 2.2f, grassRoot.transform, grassMat);
            var col = flowerColors[i % flowerColors.Length];
            RealisticFlowerBuilder.BuildFlowerClump(pRing, 8, 1.8f, flowerRoot.transform, col);
        }

        // Empat sudut Alun-Alun di bawah pohon peneduh
        var innerPlazaCorners = new Vector3[]
        {
            new Vector3(-11.5f, 0f,  11.5f), new Vector3( 11.5f, 0f,  11.5f),
            new Vector3(-11.5f, 0f, -11.5f), new Vector3( 11.5f, 0f, -11.5f),
        };
        foreach (var tp in innerPlazaCorners)
        {
            RealisticGrassBuilder.BuildGrassClump(tp, 24, 3.6f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(tp, 12, 3.0f, flowerRoot.transform);
        }

        // 3. TAMAN BUNGA & PEKARANGAN RUMAH WARGA
        var yardGardens = new (Vector3 pos, RealisticFlowerBuilder.FlowerColor col)[]
        {
            // Balai Desa (Kiri & Kanan Tangga Megah)
            (new Vector3(-6.5f, 0f, 38f), RealisticFlowerBuilder.FlowerColor.RedPoppy),
            (new Vector3( 6.5f, 0f, 38f), RealisticFlowerBuilder.FlowerColor.GoldenDandelion),
            (new Vector3(-10f,  0f, 40f), RealisticFlowerBuilder.FlowerColor.MysticPurple),
            (new Vector3( 10f,  0f, 40f), RealisticFlowerBuilder.FlowerColor.WhiteDaisy),

            // Rumah Gadang & Joglo
            (new Vector3(-20f, 0f, 26f), RealisticFlowerBuilder.FlowerColor.WhiteDaisy),
            (new Vector3(-26f, 0f, 32f), RealisticFlowerBuilder.FlowerColor.RedPoppy),
            (new Vector3( 20f, 0f, 26f), RealisticFlowerBuilder.FlowerColor.GoldenDandelion),
            (new Vector3( 26f, 0f, 32f), RealisticFlowerBuilder.FlowerColor.MysticPurple),

            // Petani & Lumbung
            (new Vector3(-20f, 0f, 16f), RealisticFlowerBuilder.FlowerColor.GoldenDandelion),
            (new Vector3(-48f, 0f, 22f), RealisticFlowerBuilder.FlowerColor.WhiteDaisy),

            // Pondok Tepi Danau
            (new Vector3(-48f, 0f, -18f), RealisticFlowerBuilder.FlowerColor.SkyBluebell),

            // Pandai Besi & Storage
            (new Vector3( 20f, 0f, 16f), RealisticFlowerBuilder.FlowerColor.RedPoppy),
            (new Vector3( 42f, 0f, 22f), RealisticFlowerBuilder.FlowerColor.GoldenDandelion),

            // Cabin Pengrajin & Villa Modern
            (new Vector3( 20f, 0f, -16f), RealisticFlowerBuilder.FlowerColor.MysticPurple),
            (new Vector3( 42f, 0f, -16f), RealisticFlowerBuilder.FlowerColor.SkyBluebell),

            // Penginapan A-Frame & Minka Dukun
            (new Vector3(-18f, 0f, -28f), RealisticFlowerBuilder.FlowerColor.WhiteDaisy),
            (new Vector3( 18f, 0f, -28f), RealisticFlowerBuilder.FlowerColor.MysticPurple),

            // Gerbang Selatan Kiri & Kanan
            (new Vector3(-18f, 0f, -44f), RealisticFlowerBuilder.FlowerColor.RedPoppy),
            (new Vector3( 18f, 0f, -44f), RealisticFlowerBuilder.FlowerColor.GoldenDandelion),
        };

        foreach (var yg in yardGardens)
        {
            RealisticGrassBuilder.BuildGrassClump(yg.pos, 18, 2.8f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(yg.pos, 10, 2.2f, flowerRoot.transform, yg.col);
        }

        // 4. SEPANJANG DINDING PERIMETER DESA (Border Bunga & Rumput Tinggi)
        for (float x = -58f; x <= 58f; x += 5.5f)
        {
            if (Mathf.Abs(x) < 7f || Mathf.Abs(x - (-37f)) < 5f) continue;
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0,  51f), 14, 3.2f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3(x, 0, -54f), 14, 3.2f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0,  51f), 7, 2.6f, flowerRoot.transform);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(x, 0, -54f), 7, 2.6f, flowerRoot.transform);
        }
        for (float z = -52f; z <= 48f; z += 5.5f)
        {
            if (Mathf.Abs(z) < 6f) continue;
            RealisticGrassBuilder.BuildGrassClump(new Vector3( 60f, 0, z), 14, 3.2f, grassRoot.transform, grassMat);
            RealisticGrassBuilder.BuildGrassClump(new Vector3(-60f, 0, z), 14, 3.2f, grassRoot.transform, grassMat);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3( 60f, 0, z), 7, 2.6f, flowerRoot.transform);
            RealisticFlowerBuilder.BuildFlowerClump(new Vector3(-60f, 0, z), 7, 2.6f, flowerRoot.transform);
        }

        // 5. HAMPARAN KARPET PADANG RUMPUT 3D & BUNGA LIAR LEBAT DI SELURUH DESA (650 Percobaan)
        for (int i = 0; i < 650; i++)
        {
            float x = Random.Range(-58f, 58f);
            float z = Random.Range(-53f, 49f);
            Vector3 pos = new Vector3(x, 0f, z);
            float distCenter = new Vector2(x, z).magnitude;

            // Bebas dari Alun-Alun Tengah (R < 9.8)
            if (distCenter < 9.8f) continue;
            // Bebas dari Jalan Lingkar Utama
            if (distCenter > 20.0f && distCenter < 24.8f) continue;
            // Bebas dari 4 Poros Utama Jalan
            if (Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f) continue;
            // Bebas dari Aliran Air Sungai Desa (X = -37)
            if (Mathf.Abs(x - (-37f)) < 3.8f) continue;
            // Bebas dari Tapak Bangunan
            if (RealisticGrassBuilder.IsPositionOnStoneOrRoad(pos)) continue;

            // Rumpun Rumput Lebat (14-18 helai)
            int bladeCount = Random.Range(14, 19);
            float spread = Random.Range(2.8f, 3.8f);
            RealisticGrassBuilder.BuildGrassClump(pos, bladeCount, spread, grassRoot.transform, grassMat);

            // Bunga Liar Berwarna-warni Timbul (Peluang 85%)
            if (Random.value < 0.85f)
            {
                var randomColor = flowerColors[Random.Range(0, flowerColors.Length)];
                int flowerCount = Random.Range(7, 12);
                RealisticFlowerBuilder.BuildFlowerClump(pos, flowerCount, spread * 0.75f, flowerRoot.transform, randomColor);
            }
        }

        // 6. PADANG RUMPUT & BUNGA LIAR DI LUAR DESA
        int meadowCount = 200;
        for (int i = 0; i < meadowCount; i++)
        {
            float ang = (i / (float)meadowCount) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(68f, 150f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            if (Mathf.Abs(pos.x) < 5f || Mathf.Abs(pos.z) < 5f) continue;
            RealisticGrassBuilder.BuildGrassClump(pos, 16, 4.8f, grassRoot.transform, grassMat);
            if (i % 2 == 0)
            {
                var fCol = flowerColors[i % flowerColors.Length];
                RealisticFlowerBuilder.BuildFlowerClump(pos, 8, 3.5f, flowerRoot.transform, fCol);
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

        PlaceArtResource(itemsRoot.transform, "Iron_Ore_1", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(78f, 0f, 14f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_2", "EA03_Environment_Rock_CatHead_01b", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(84f, 0f, 20f), Quaternion.Euler(0, 45f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_3", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(90f, 0f, 14f), Quaternion.Euler(0, 110f, 0), new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.4f, 0.4f, 0.5f));

        PlaceArtResource(itemsRoot.transform, "Wood_Log_1", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-78f, 0f, 18f), Quaternion.identity, Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_2", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-84f, 0f, 24f), Quaternion.Euler(0, 80f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_3", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Balok Kayu", new Vector3(-90f, 0f, 18f), Quaternion.Euler(0, 30f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));

        PlaceArtResource(itemsRoot.transform, "Stone_Rock_1", "EA03_Environment_Rock_Head_01a", "stone",
            "Tekan E untuk memecah Batu", new Vector3(72f, 0f, -14f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f), slavicMat, new Color(0.55f, 0.55f, 0.55f));
        PlaceArtResource(itemsRoot.transform, "Stone_Rock_2", "EA03_Environment_Rock_Head_01b", "stone",
            "Tekan E untuk memecah Batu", new Vector3(-56f, 0f, -92f), Quaternion.Euler(0, 50f, 0), new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.55f, 0.55f, 0.55f));

        Vector3 altarCenter = new Vector3(0f, 0f, 75f);
        var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        altar.name = "Seal_Altar";
        altar.transform.SetParent(itemsRoot.transform);
        altar.transform.position = altarCenter + new Vector3(0, 0.3f, 0);
        altar.transform.localScale = new Vector3(4f, 0.6f, 4f);
        ApplyColor(altar, new Color(0.45f, 0.35f, 0.85f));
        altar.AddComponent<SealAltar>();

        AddPorchLantern(altar.transform, new Vector3(0, 1.5f, 0), new Color(0.7f, 0.2f, 1f), 15f);

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
        foreach (var fish in Object.FindObjectsOfType<FishController>())
        {
            if (fish) Object.DestroyImmediate(fish.gameObject);
        }

        GameObject faunaRoot = GameObject.Find("--- FAUNA & ANIMALS ---");
        if (faunaRoot) Object.DestroyImmediate(faunaRoot);

        faunaRoot = new GameObject("--- FAUNA & ANIMALS ---");

        ProceduralDogBuilder.BuildDog(faunaRoot.transform, new Vector3(2.4f, 0f, -3.5f), Quaternion.Euler(0, -135f, 0), "Milo");

        AnimalAssetSetup.SpawnCat(new Vector3(-3.8f, 0f, 2.2f),
            Quaternion.Euler(0, 45f, 0), faunaRoot.transform,
            "Luna", "Meoow~ Rubuh aku dong, petualang!", altColor: false);
        AnimalAssetSetup.SpawnCat(new Vector3(9.5f, 0f, 9.5f),
            Quaternion.Euler(0, -120f, 0), faunaRoot.transform,
            "Oliver", "Purrr... kamu menggangguku tidur!", altColor: true);

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

        SpawnVillageRooster(new Vector3(-4.2f, 0f, -3.2f), 65f, "Rooster_Jago_AlunAlun", Eldara.RoosterClip.Idle);
        SpawnVillageRooster(new Vector3(-23f, 0f, 7f), -45f, "Rooster_Jago_Pertanian", Eldara.RoosterClip.Peck);
        SpawnVillageRooster(new Vector3(31f, 0f, 7f), 135f, "Rooster_Jago_Pengrajin", Eldara.RoosterClip.Walk);
        SpawnVillageRooster(new Vector3(4.2f, 0f, -45f), -120f, "Rooster_Jago_Gerbang", Eldara.RoosterClip.Crow);

        AnimalAssetSetup.SpawnWolf(new Vector3(2f, 0f, 72f), Quaternion.Euler(0, 180f, 0), faunaRoot.transform, "Serigala Rimba Alpha");
        AnimalAssetSetup.SpawnWolf(new Vector3(-78f, 0f, 20f), Quaternion.Euler(0, 90f, 0), faunaRoot.transform, "Serigala Rimba Barat");
        AnimalAssetSetup.SpawnWolf(new Vector3(78f, 0f, 20f), Quaternion.Euler(0, -90f, 0), faunaRoot.transform, "Serigala Rimba Timur");

        AnimalAssetSetup.SpawnEagle(new Vector3(0f, 28f, 0f), faunaRoot.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9c. EKSPANSI PETA GELOMBANG 1 — 4 ZONA BARU (500x500m)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupExpandedWorld(Material slavicMat, Material grassMat)
    {
        GameObject expandRoot = GameObject.Find("--- EXPANSION ZONES ---");
        if (expandRoot) Object.DestroyImmediate(expandRoot);
        expandRoot = new GameObject("--- EXPANSION ZONES ---");

        var barkMat       = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var birchBarkMat  = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: true);
        var summerLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var autumnLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("AUTUMN");

        var roadMat = GetOrCreateVillageRoadMaterial();
        var expandRoads = new GameObject("Expansion_Road_Network");
        expandRoads.transform.SetParent(expandRoot.transform);

        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_Bypass1",
            new Vector3(4.2f, 0, 36f), new Vector3(14f, 0, 46f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_Foothills",
            new Vector3(14f, 0, 46f), new Vector3(14f, 0, 88f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_ValleyCurve",
            new Vector3(14f, 0, 88f), new Vector3(0f, 0, 105f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_North_Expansion_Highway",
            new Vector3(0f, 0, 105f), new Vector3(0f, 0, 160f), 4.0f, roadMat, 2.5f);

        CreateContinuousPavedRoad(expandRoads.transform, "Road_South_Expansion",
            new Vector3(0, 0, -56f), new Vector3(0, 0, -160f), 4.0f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_West_Expansion",
            new Vector3(-62f, 0, 0), new Vector3(-160f, 0, 0), 4.0f, roadMat, 2.5f);
        CreateContinuousPavedRoad(expandRoads.transform, "Road_East_Expansion",
            new Vector3(62f, 0, 0), new Vector3(160f, 0, 0), 4.0f, roadMat, 2.5f);

        // ZONA UTARA
        {
            var northRoot = new GameObject("Zone_North_MountainVillage");
            northRoot.transform.SetParent(expandRoot.transform);

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

            var temple = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temple.name = "TempleBase_North";
            temple.transform.SetParent(northRoot.transform);
            temple.transform.position = new Vector3(0f, 0.4f, 200f);
            temple.transform.localScale = new Vector3(16f, 0.8f, 12f);
            ApplyColor(temple, new Color(0.58f, 0.52f, 0.46f));

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

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "TempleRoof_North";
            roof.transform.SetParent(northRoot.transform);
            roof.transform.position = new Vector3(0f, 5.5f, 200f);
            roof.transform.localScale = new Vector3(17f, 0.6f, 13f);
            ApplyColor(roof, new Color(0.35f, 0.28f, 0.22f));
            AddPorchLantern(northRoot.transform, new Vector3(0, 7f, 200f), new Color(0.5f, 0.8f, 1f), 20f);

            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
                new Vector3(-18f, 0f, 165f), Quaternion.Euler(0, 90f, 0), northRoot.transform, "MtnHouse_West_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Chalet,
                new Vector3( 18f, 0f, 165f), Quaternion.Euler(0, -90f, 0), northRoot.transform, "MtnHouse_East_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.AFrame,
                new Vector3(-22f, 0f, 182f), Quaternion.Euler(0, 70f, 0), northRoot.transform, "MtnHouse_West_2");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
                new Vector3( 22f, 0f, 182f), Quaternion.Euler(0, -70f, 0), northRoot.transform, "MtnHouse_East_2");

            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "Bridge_MistNorth";
            bridge.transform.SetParent(northRoot.transform);
            bridge.transform.position = new Vector3(0f, 0.15f, 152f);
            bridge.transform.localScale = new Vector3(4f, 0.3f, 10f);
            ApplyColor(bridge, new Color(0.4f, 0.32f, 0.22f));

            var npcKuil = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Wizard,
                new Vector3(-4f, 0f, 198f), Quaternion.Euler(0, 180f, 0), northRoot.transform, "NPC_PenjagaKuil");
            var dlgKuil = npcKuil.GetComponent<NpcDialogue>() ?? npcKuil.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PenjagaKuil",
                "Selamat datang di Kuil Batu Kuno Eldara Atas, pengembara. Tempat ini menyimpan jimat pelindung desa dari zaman para leluhur.",
                dlgKuil);

            var npcPdg = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Merchant,
                new Vector3(16f, 0f, 168f), Quaternion.Euler(0, -120f, 0), northRoot.transform, "NPC_PedagangGunungNorth");
            var dlgPdg = npcPdg.GetComponent<NpcDialogue>() ?? npcPdg.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PedagangGunungNorth",
                "Hei, traveler! Aku datang dari puncak gunung membawa kristal biru langka.",
                dlgPdg);

            PlaceArtResource(northRoot.transform, "CrystalBlue_1", "EA03_Environment_Rock_Head_01c", "crystal",
                "Tekan E untuk mengambil Kristal Biru", new Vector3(-8f, 0f, 195f), Quaternion.Euler(0, 30f, 0),
                new Vector3(1.2f, 1.4f, 1.2f), slavicMat, new Color(0.3f, 0.5f, 0.9f));
            PlaceArtResource(northRoot.transform, "CrystalBlue_2", "EA03_Environment_Rock_Head_01b", "crystal",
                "Tekan E untuk mengambil Kristal Biru", new Vector3(10f, 0f, 192f), Quaternion.Euler(0, 80f, 0),
                new Vector3(1.0f, 1.3f, 1.0f), slavicMat, new Color(0.3f, 0.5f, 0.9f));

            for (int i = 0; i < 35; i++)
            {
                float tx = Random.Range(-40f, 40f);
                float tz = Random.Range(160f, 215f);
                if (Mathf.Abs(tx) < 10f && tz > 192f && tz < 210f) continue;
                float ts = Random.Range(0.9f, 1.4f);
                if (i % 3 == 0) RealisticTreeBuilder.BuildPineTree(new Vector3(tx, 0, tz), ts, northRoot.transform, barkMat, summerLeafMat);
                else RealisticTreeBuilder.BuildOakTree(new Vector3(tx, 0, tz), ts, northRoot.transform, barkMat, summerLeafMat);
            }
            if (grassMat)
                for (int i = 0; i < 30; i++)
                {
                    float gx = Random.Range(-35f, 35f);
                    float gz = Random.Range(158f, 215f);
                    RealisticGrassBuilder.BuildGrassClump(new Vector3(gx, 0, gz), 10, 4f, northRoot.transform, grassMat);
                }

            AnimalAssetSetup.SpawnWolf(new Vector3(-15f, 0f, 175f), Quaternion.Euler(0, 45f, 0), northRoot.transform, "Serigala Gunung Barat");
            AnimalAssetSetup.SpawnWolf(new Vector3( 20f, 0f, 188f), Quaternion.Euler(0, -130f, 0), northRoot.transform, "Serigala Gunung Timur");
        }

        // ZONA SELATAN
        {
            var southRoot = new GameObject("Zone_South_SwampCamp");
            southRoot.transform.SetParent(expandRoot.transform);

            Vector3[] ponds = { new Vector3(-12f, 0f, -175f), new Vector3(8f, 0f, -185f), new Vector3(-5f, 0f, -200f) };
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

            Vector3 campCenter = new Vector3(5f, 0f, -185f);
            var campfire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            campfire.name = "Campfire_Goblin";
            campfire.transform.SetParent(southRoot.transform);
            campfire.transform.position = campCenter + new Vector3(0, 0.3f, 0);
            campfire.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            ApplyColor(campfire, new Color(0.6f, 0.3f, 0.1f));
            AddPorchLantern(southRoot.transform, campCenter + new Vector3(0, 1f, 0), new Color(1f, 0.4f, 0.1f), 14f);

            Vector3[] tentPos = {
                campCenter + new Vector3(-5f, 0f, -4f), campCenter + new Vector3( 6f, 0f, -3f),
                campCenter + new Vector3(-4f, 0f,  5f), campCenter + new Vector3( 5f, 0f,  5f),
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

            var spawnS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawnS.name = "SpawnPoint_South_SwampCamp";
            spawnS.transform.SetParent(southRoot.transform);
            spawnS.transform.position = campCenter;
            spawnS.transform.localScale = new Vector3(2f, 0.3f, 2f);
            ApplyColor(spawnS, new Color(0.85f, 0.1f, 0.1f));
            Object.DestroyImmediate(spawnS.GetComponent<Collider>());

            var npcRawa = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Wizard,
                new Vector3(-18f, 0f, -172f), Quaternion.Euler(0, 30f, 0), southRoot.transform, "NPC_PenyihirRawa");
            var dlgRawa = npcRawa.GetComponent<NpcDialogue>() ?? npcRawa.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PenyihirRawa",
                "Hssst... pengembara dungu! Kamu berani masuk ke Rawa Bayangan?! Kawanan goblin telah merampas jimat perlindunganku.",
                dlgRawa);

            PlaceArtResource(southRoot.transform, "SwampShroom_1", "EA03_Nature_Bush_03b", "rare_herb",
                "Tekan E untuk memetik Jamur Langka", new Vector3(-8f, 0f, -178f), Quaternion.identity,
                Vector3.one * 0.7f, slavicMat, new Color(0.6f, 0.9f, 0.3f));

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

        // ZONA BARAT
        {
            var westRoot = new GameObject("Zone_West_FishingVillage");
            westRoot.transform.SetParent(expandRoot.transform);

            var river = GameObject.CreatePrimitive(PrimitiveType.Cube);
            river.name = "River_West";
            river.transform.SetParent(westRoot.transform);
            river.transform.position = new Vector3(-148f, 0.05f, 0f);
            river.transform.localScale = new Vector3(5f, 0.1f, 120f);
            ApplyColor(river, new Color(0.2f, 0.42f, 0.72f));
            Object.DestroyImmediate(river.GetComponent<Collider>());
            AddPorchLantern(westRoot.transform, new Vector3(-148f, 0.3f, 0f), new Color(0.4f, 0.7f, 1f), 25f);

            for (int p = 0; p < 6; p++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Dock_Plank_{p}";
                plank.transform.SetParent(westRoot.transform);
                plank.transform.position = new Vector3(-148f + p * 1.1f - 2f, 0.18f, 10f);
                plank.transform.localScale = new Vector3(1.0f, 0.15f, 2.5f);
                ApplyColor(plank, new Color(0.4f, 0.3f, 0.18f));
            }

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

            var npcNelayan = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Farmer,
                new Vector3(-162f, 0f, 5f), Quaternion.Euler(0, 90f, 0), westRoot.transform, "NPC_KaptenNelayan");
            var dlgNelayan = npcNelayan.GetComponent<NpcDialogue>() ?? npcNelayan.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_KaptenNelayan",
                "Ahoy, pelancong! Selamat datang di Dermaga Sungai Perak.",
                dlgNelayan);

            var npcIkan = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Woman,
                new Vector3(-160f, 0f, -8f), Quaternion.Euler(0, 60f, 0), westRoot.transform, "NPC_PedagangIkan");
            var dlgIkan = npcIkan.GetComponent<NpcDialogue>() ?? npcIkan.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PedagangIkan",
                "Hei, kamu mau beli ikan segar? Kami punya ikan mas, ikan patin, dan belut sungai!",
                dlgIkan);

            PlaceArtResource(westRoot.transform, "FishCatch_1", "EA03_Prop_Container_Crate_02a", "fish",
                "Tekan E untuk mengambil Ikan Segar", new Vector3(-144f, 0f, 14f), Quaternion.identity,
                Vector3.one, slavicMat, new Color(0.3f, 0.6f, 0.8f));

            for (int i = 0; i < 30; i++)
            {
                float tx = Random.Range(-215f, -155f);
                float tz = Random.Range(-45f, 45f);
                float ts = Random.Range(0.9f, 1.3f);
                if (i % 3 == 0) RealisticTreeBuilder.BuildBirchTree(new Vector3(tx, 0, tz), ts, westRoot.transform, birchBarkMat, summerLeafMat);
                else RealisticTreeBuilder.BuildOakTree(new Vector3(tx, 0, tz), ts, westRoot.transform, barkMat, summerLeafMat);
            }
            if (grassMat)
                for (int i = 0; i < 28; i++)
                {
                    float gx = Random.Range(-210f, -155f);
                    float gz = Random.Range(-40f, 40f);
                    RealisticGrassBuilder.BuildGrassClump(new Vector3(gx, 0, gz), 10, 4.5f, westRoot.transform, grassMat);
                }
        }

        // ZONA TIMUR
        {
            var eastRoot = new GameObject("Zone_East_CrystalMines");
            eastRoot.transform.SetParent(expandRoot.transform);

            var cliffBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffBase.name = "CliffBase_East";
            cliffBase.transform.SetParent(eastRoot.transform);
            cliffBase.transform.position = new Vector3(175f, 4f, 0f);
            cliffBase.transform.localScale = new Vector3(10f, 8f, 60f);
            ApplyColor(cliffBase, new Color(0.5f, 0.46f, 0.42f));

            var cavemouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cavemouth.name = "CaveMouth_East";
            cavemouth.transform.SetParent(eastRoot.transform);
            cavemouth.transform.position = new Vector3(171f, 1.5f, 8f);
            cavemouth.transform.localScale = new Vector3(1f, 3.5f, 5.5f);
            ApplyColor(cavemouth, new Color(0.05f, 0.05f, 0.08f));
            Object.DestroyImmediate(cavemouth.GetComponent<Collider>());
            AddPorchLantern(eastRoot.transform, new Vector3(172f, 2f, 8f), new Color(0.4f, 0.2f, 1f), 15f);

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

            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Cabin,
                new Vector3(162f, 0f, -25f), Quaternion.Euler(0, -90f, 0), eastRoot.transform, "MineGuardPost_1");
            ProceduralHouseBuilder.BuildHouse(ProceduralHouseBuilder.HouseStyle.Minka,
                new Vector3(162f, 0f,  25f), Quaternion.Euler(0, -90f, 0), eastRoot.transform, "MineGuardPost_2");

            PlaceArtResource(eastRoot.transform, "CrystalOre_1", "EA03_Environment_Rock_CatHead_01a", "crystal_ore",
                "Tekan E untuk menambang Bijih Kristal", new Vector3(165f, 0f, 3f), Quaternion.Euler(0, 45f, 0),
                new Vector3(1.3f, 1.3f, 1.3f), slavicMat, new Color(0.6f, 0.3f, 0.9f));
            PlaceArtResource(eastRoot.transform, "CrystalOre_2", "EA03_Environment_Rock_Head_01a", "crystal_ore",
                "Tekan E untuk menambang Bijih Kristal", new Vector3(163f, 0f, -8f), Quaternion.Euler(0, 120f, 0),
                new Vector3(1.1f, 1.1f, 1.1f), slavicMat, new Color(0.6f, 0.3f, 0.9f));
            PlaceArtResource(eastRoot.transform, "CrystalOre_3", "EA03_Environment_Rock_CatHead_01b", "crystal_ore",
                "Tekan E untuk menambang Bijih Kristal", new Vector3(169f, 0f, 14f), Quaternion.Euler(0, 70f, 0),
                new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.6f, 0.3f, 0.9f));

            var npcMandor = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Guard,
                new Vector3(160f, 0f, -20f), Quaternion.Euler(0, -120f, 0), eastRoot.transform, "NPC_MandorTambang");
            var dlgMandor = npcMandor.GetComponent<NpcDialogue>() ?? npcMandor.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_MandorTambang",
                "Hei, berhenti! Area Tambang Kristal Eldara adalah kawasan terbatas.",
                dlgMandor);

            var npcTua = ProceduralNpcBuilder.BuildNpc(ProceduralNpcBuilder.NpcStyle.Farmer,
                new Vector3(158f, 0f, 18f), Quaternion.Euler(0, -150f, 0), eastRoot.transform, "NPC_PenambangTua");
            var dlgTua = npcTua.GetComponent<NpcDialogue>() ?? npcTua.AddComponent<NpcDialogue>();
            CreateSimpleDialogue("NPC_PenambangTua",
                "Sudah 30 tahun aku menambang di sini, nak. Kristal ungu ini punya kekuatan luar biasa.",
                dlgTua);

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

            AnimalAssetSetup.SpawnWolf(new Vector3(158f, 0f, -38f), Quaternion.Euler(0, -90f, 0), eastRoot.transform, "Serigala Tambang Utara");
            AnimalAssetSetup.SpawnWolf(new Vector3(160f, 0f,  35f), Quaternion.Euler(0, -130f, 0), eastRoot.transform, "Serigala Tambang Selatan");
        }

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
    //  9d. MEGA EKSPANSI PETA — 4 ZONA LANJUTAN (1000x1000m)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupMegaExpansion(Material slavicMat, Material grassMat)
    {
        GameObject megaRoot = GameObject.Find("--- MEGA EXPANSION ---");
        if (megaRoot) Object.DestroyImmediate(megaRoot);
        megaRoot = new GameObject("--- MEGA EXPANSION ---");

        var barkMat       = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: false);
        var summerLeafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial("SUMMER");
        var roadMat       = GetOrCreateVillageRoadMaterial();

        var megaRoads = new GameObject("MegaRoad_Network");
        megaRoads.transform.SetParent(megaRoot.transform);

        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_North_A", new Vector3(0f, 0f, 220f), new Vector3(0f, 0f, 350f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_North_B", new Vector3(0f, 0f, 350f), new Vector3(0f, 0f, 480f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_South_A", new Vector3(0f, 0f, -220f), new Vector3(0f, 0f, -350f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_South_B", new Vector3(0f, 0f, -350f), new Vector3(0f, 0f, -480f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_West_A",  new Vector3(-220f, 0f, 0f), new Vector3(-350f, 0f, 0f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_West_B",  new Vector3(-350f, 0f, 0f), new Vector3(-480f, 0f, 0f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_East_A",  new Vector3(220f, 0f, 0f), new Vector3(350f, 0f, 0f), 3.8f, roadMat, 2.5f);
        CreateContinuousPavedRoad(megaRoads.transform, "MegaRoad_East_B",  new Vector3(350f, 0f, 0f), new Vector3(480f, 0f, 0f), 3.8f, roadMat, 2.5f);

        // ZONA MEGA-UTARA — PUNCAK SALJU
        {
            var snowRoot = new GameObject("Zone_FarNorth_SnowPeak");
            snowRoot.transform.SetParent(megaRoot.transform);
            var snowCenter = new Vector3(0f, 0f, 380f);

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

            var snowGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            snowGround.name = "SnowGround";
            snowGround.transform.SetParent(snowRoot.transform);
            snowGround.transform.position = snowCenter;
            snowGround.transform.localScale = new Vector3(20f, 1f, 20f);
            snowGround.GetComponent<Renderer>().sharedMaterial = snowMat;
            Object.DestroyImmediate(snowGround.GetComponent<Collider>());

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

            var altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            altar.name = "IceTemple_Altar";
            altar.transform.SetParent(templeRoot.transform);
            altar.transform.localPosition = new Vector3(0f, 1f, 0f);
            altar.transform.localScale = new Vector3(4f, 2f, 4f);
            altar.GetComponent<Renderer>().sharedMaterial = iceMat;
            Object.DestroyImmediate(altar.GetComponent<Collider>());

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

            for (int t = 0; t < 20; t++)
            {
                float tx = Random.Range(-80f, 80f);
                float tz = Random.Range(-60f, 60f);
                RealisticTreeBuilder.BuildPineTree(snowCenter + new Vector3(tx, 0f, tz),
                    Random.Range(5f, 10f), snowRoot.transform, barkMat, summerLeafMat);
            }

            var coldLight = new GameObject("SnowPeak_AmbientLight");
            coldLight.transform.SetParent(snowRoot.transform);
            coldLight.transform.position = snowCenter + new Vector3(0f, 25f, 0f);
            var cl = coldLight.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(0.6f, 0.8f, 1f);
            cl.intensity = 1.2f;
            cl.range = 180f;

            SpawnNPC(snowRoot.transform, "NPC_PenjagaSalju",
                snowCenter + new Vector3(-8f, 0f, 5f), Quaternion.Euler(0, 160f, 0),
                new Color(0.72f, 0.78f, 0.85f),
                "Hati-hati, pengembara! Kuil Es ini menyimpan bahaya purba...");

            SpawnEnemyMarker(snowRoot.transform, "Elite_FrostGuardian_1", snowCenter + new Vector3( 12f, 0f, 15f));
            SpawnEnemyMarker(snowRoot.transform, "Elite_FrostGuardian_2", snowCenter + new Vector3(-12f, 0f, 15f));
            SpawnEnemyMarker(snowRoot.transform, "Elite_FrostGuardian_3", snowCenter + new Vector3(  0f, 0f, 30f));
        }

        // ZONA MEGA-SELATAN — HUTAN PERI
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

            var fairyGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fairyGround.name = "FairyGround";
            fairyGround.transform.SetParent(fairyRoot.transform);
            fairyGround.transform.position = fairyCenter;
            fairyGround.transform.localScale = new Vector3(20f, 1f, 20f);
            fairyGround.GetComponent<Renderer>().sharedMaterial = fairyMossMat;
            Object.DestroyImmediate(fairyGround.GetComponent<Collider>());

            for (int t = 0; t < 15; t++)
            {
                float tx = Random.Range(-85f, 85f);
                float tz = Random.Range(-70f, 70f);
                var treePos = fairyCenter + new Vector3(tx, 0f, tz);
                RealisticTreeBuilder.BuildOakTree(treePos, Random.Range(9f, 18f), fairyRoot.transform, barkMat, summerLeafMat);
                var glowGo = new GameObject($"FairyGlow_{t}");
                glowGo.transform.SetParent(fairyRoot.transform);
                glowGo.transform.position = treePos + new Vector3(0f, 4f, 0f);
                var gl = glowGo.AddComponent<Light>();
                gl.type = LightType.Point;
                gl.color = new Color(Random.Range(0.3f, 1f), Random.Range(0.6f, 1f), Random.Range(0.2f, 0.8f));
                gl.intensity = 0.8f;
                gl.range = 12f;
            }

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

                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = $"FairyMushroomCap_{m}";
                cap.transform.SetParent(fairyRoot.transform);
                cap.transform.position = fairyCenter + new Vector3(Mathf.Cos(ang) * 8f, 1.1f, Mathf.Sin(ang) * 8f);
                cap.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
                var capMat = new Material(shader);
                capMat.SetColor("_BaseColor", new Color(Random.Range(0.6f, 1f), 0.1f, Random.Range(0.1f, 0.4f)));
                capMat.SetColor("_EmissionColor", capMat.GetColor("_BaseColor") * 1.5f);
                capMat.EnableKeyword("_EMISSION");
                cap.GetComponent<Renderer>().sharedMaterial = capMat;
                Object.DestroyImmediate(cap.GetComponent<Collider>());
            }

            var fairyAltar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fairyAltar.name = "FairyAltar";
            fairyAltar.transform.SetParent(fairyRoot.transform);
            fairyAltar.transform.position = fairyCenter + new Vector3(0f, 0.5f, 0f);
            fairyAltar.transform.localScale = new Vector3(2.5f, 0.5f, 2.5f);
            fairyAltar.GetComponent<Renderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(fairyAltar.GetComponent<Collider>());

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

            var fairyLight = new GameObject("FairyForest_AmbientLight");
            fairyLight.transform.SetParent(fairyRoot.transform);
            fairyLight.transform.position = fairyCenter + new Vector3(0f, 30f, 0f);
            var fl = fairyLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.color = new Color(0.4f, 1f, 0.5f);
            fl.intensity = 1.5f;
            fl.range = 200f;

            SpawnNPC(fairyRoot.transform, "NPC_PeriHutan",
                fairyCenter + new Vector3(5f, 0f, -5f), Quaternion.Euler(0, 30f, 0),
                new Color(0.7f, 1f, 0.7f),
                "Selamat datang di Hutan Peri, pengembara! Jangan ganggu Lingkaran Peri...");

            SpawnEnemyMarker(fairyRoot.transform, "Elite_FairyGuardian_1", fairyCenter + new Vector3(-15f, 0f, -10f));
            SpawnEnemyMarker(fairyRoot.transform, "Elite_FairyGuardian_2", fairyCenter + new Vector3( 15f, 0f, -10f));
        }

        // ZONA MEGA-BARAT — KUIL LAUT
        {
            var seaRoot = new GameObject("Zone_FarWest_SeaTemple");
            seaRoot.transform.SetParent(megaRoot.transform);
            var seaCenter = new Vector3(-380f, 0f, 0f);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var waterMat = new Material(shader);
            waterMat.SetColor("_BaseColor", new Color(0.05f, 0.4f, 0.7f, 0.75f));
            waterMat.SetFloat("_Smoothness", 0.95f);
            waterMat.SetFloat("_Metallic", 0.1f);
            if (waterMat.HasProperty("_Surface")) waterMat.SetFloat("_Surface", 1f);
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

            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "SeaTemple_Pool";
            pool.transform.SetParent(seaRoot.transform);
            pool.transform.position = seaCenter + new Vector3(0f, -0.4f, 0f);
            pool.transform.localScale = new Vector3(80f, 0.4f, 80f);
            pool.GetComponent<Renderer>().sharedMaterial = waterMat;
            Object.DestroyImmediate(pool.GetComponent<Collider>());

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

            var godPositions = new Vector3[] { new Vector3(0f, 11f, 0f), new Vector3(5f, 10.5f, 0f), new Vector3(-5f, 10.5f, 0f) };
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

            var seaLight = new GameObject("SeaTemple_Light");
            seaLight.transform.SetParent(seaRoot.transform);
            seaLight.transform.position = seaCenter + new Vector3(0f, 5f, 0f);
            var sL = seaLight.AddComponent<Light>();
            sL.type = LightType.Point;
            sL.color = new Color(0.1f, 0.5f, 1f);
            sL.intensity = 2.5f;
            sL.range = 100f;

            SpawnNPC(seaRoot.transform, "NPC_PenjagaKuilLaut",
                seaCenter + new Vector3(32f, 0f, 5f), Quaternion.Euler(0, -90f, 0),
                new Color(0.4f, 0.6f, 0.8f),
                "Kuil Dewa Laut ini sudah berdiri ribuan tahun. Hormati tempat ini, wahai pengembara.");

            AnimalAssetSetup.SpawnWolf(seaCenter + new Vector3(38f, 0f, -8f),
                Quaternion.Euler(0, 90f, 0), seaRoot.transform, "Penjaga Kuil Barat");

            SpawnEnemyMarker(seaRoot.transform, "Elite_SeaGuardian_1", seaCenter + new Vector3(-8f, 10f, -8f));
            SpawnEnemyMarker(seaRoot.transform, "Elite_SeaGuardian_2", seaCenter + new Vector3( 8f, 10f,  8f));
        }

        // ZONA MEGA-TIMUR — NGARAI API
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

            var ashGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ashGround.name = "AshGround";
            ashGround.transform.SetParent(fireRoot.transform);
            ashGround.transform.position = fireCenter;
            ashGround.transform.localScale = new Vector3(20f, 1f, 20f);
            ashGround.GetComponent<Renderer>().sharedMaterial = ashMat;
            Object.DestroyImmediate(ashGround.GetComponent<Collider>());

            var crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name = "VolcanoCrater";
            crater.transform.SetParent(fireRoot.transform);
            crater.transform.position = fireCenter + new Vector3(0f, -1f, 10f);
            crater.transform.localScale = new Vector3(35f, 1f, 35f);
            crater.GetComponent<Renderer>().sharedMaterial = lavaMat;
            Object.DestroyImmediate(crater.GetComponent<Collider>());

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

            var towerTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            towerTop.name = "ObsidianTower_Top";
            towerTop.transform.SetParent(fireRoot.transform);
            towerTop.transform.position = towerCenter + new Vector3(0f, 18f, 0f);
            towerTop.transform.localScale = new Vector3(4f, 4f, 4f);
            towerTop.GetComponent<Renderer>().sharedMaterial = lavaMat;
            Object.DestroyImmediate(towerTop.GetComponent<Collider>());

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

            SpawnNPC(fireRoot.transform, "NPC_PenjagaApi",
                fireCenter + new Vector3(-25f, 0f, -5f), Quaternion.Euler(0, 90f, 0),
                new Color(0.9f, 0.4f, 0.2f),
                "Ngarai Api hanya untuk yang terkuat! Balikkan langkahmu, pengembara lemah!");

            AnimalAssetSetup.SpawnWolf(fireCenter + new Vector3(-28f, 0f, 8f),
                Quaternion.Euler(0, 90f, 0), fireRoot.transform, "Serigala Api");

            SpawnEnemyMarker(fireRoot.transform, "Elite_FireDemon_1", fireCenter + new Vector3( 5f, 0f, 10f));
            SpawnEnemyMarker(fireRoot.transform, "Elite_FireDemon_2", fireCenter + new Vector3(-5f, 0f, 10f));
            SpawnEnemyMarker(fireRoot.transform, "Elite_FireBoss",    fireCenter + new Vector3( 0f, 0f, 30f));
        }

        BuildMapBoundaryWalls(megaRoot.transform);
    }

    static void SpawnNPC(Transform parent, string npcName, Vector3 worldPos,
                         Quaternion rotation, Color skinTint, string dialogText)
    {
        ProceduralNpcBuilder.NpcStyle style = ProceduralNpcBuilder.NpcStyle.Guard;
        if (npcName.Contains("Penyihir") || npcName.Contains("Peri"))
            style = ProceduralNpcBuilder.NpcStyle.Wizard;
        else if (npcName.Contains("Kapten") || npcName.Contains("Nelayan") || npcName.Contains("Penam") || npcName.Contains("Tua"))
            style = ProceduralNpcBuilder.NpcStyle.Farmer;

        var npcGo = ProceduralNpcBuilder.BuildNpc(style, worldPos, rotation, parent, npcName);
        if (!npcGo) return;

        var dlg = npcGo.GetComponent<NpcDialogue>() ?? npcGo.AddComponent<NpcDialogue>();
        CreateSimpleDialogue(npcName, dialogText, dlg);
    }

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
    //  9e. ZONE TRIGGER & ENEMY SPAWNERS & HUD & DESIGN PASS
    // ─────────────────────────────────────────────────────────────────────
    static void SetupZoneTriggers()
    {
        var triggerRoot = GameObject.Find("--- ZONE TRIGGERS ---");
        if (triggerRoot) Object.DestroyImmediate(triggerRoot);
        triggerRoot = new GameObject("--- ZONE TRIGGERS ---");

        var zoneDefs = new (string name, Vector3 center, Vector3 size, ZoneMusicTrigger.ZoneType type)[]
        {
            ("ZoneTrigger_Village",            new Vector3(0f,    5f,   0f),  new Vector3(120f, 20f, 120f), ZoneMusicTrigger.ZoneType.Village),
            ("ZoneTrigger_North_Mountain",     new Vector3(0f,    5f,  190f), new Vector3(120f, 20f,  70f), ZoneMusicTrigger.ZoneType.Mountain),
            ("ZoneTrigger_South_Swamp",        new Vector3(0f,    5f, -190f), new Vector3(120f, 20f,  70f), ZoneMusicTrigger.ZoneType.Swamp),
            ("ZoneTrigger_West_Fishing",       new Vector3(-190f, 5f,   0f),  new Vector3(70f,  20f, 120f), ZoneMusicTrigger.ZoneType.Fishing),
            ("ZoneTrigger_East_Mines",         new Vector3( 190f, 5f,   0f),  new Vector3(70f,  20f, 120f), ZoneMusicTrigger.ZoneType.Mines),
            ("ZoneTrigger_FarNorth_SnowPeak",  new Vector3(0f,    5f,  380f), new Vector3(200f, 20f, 200f), ZoneMusicTrigger.ZoneType.SnowPeak),
            ("ZoneTrigger_FarSouth_FairyForest",new Vector3(0f,   5f, -380f), new Vector3(200f, 20f, 200f), ZoneMusicTrigger.ZoneType.FairyForest),
            ("ZoneTrigger_FarWest_SeaTemple",  new Vector3(-380f, 5f,   0f),  new Vector3(200f, 20f, 200f), ZoneMusicTrigger.ZoneType.SeaTemple),
            ("ZoneTrigger_FarEast_FireCanyon", new Vector3( 380f, 5f,   0f),  new Vector3(200f, 20f, 200f), ZoneMusicTrigger.ZoneType.FireCanyon),
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
            var so = new UnityEditor.SerializedObject(trigger);
            so.FindProperty("zone").enumValueIndex = (int)zoneType;
            so.ApplyModifiedProperties();
        }
    }

    static void SetupEnemySpawners()
    {
        var spawnerRoot = GameObject.Find("--- ENEMY SPAWNERS ---");
        if (spawnerRoot) Object.DestroyImmediate(spawnerRoot);
        spawnerRoot = new GameObject("--- ENEMY SPAWNERS ---");

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

        var bossMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bossMarker.name = "BossSpawnPoint";
        bossMarker.transform.SetParent(spawnerRoot.transform);
        bossMarker.transform.position = new Vector3(0f, 0f, 80f);
        bossMarker.transform.localScale = new Vector3(4f, 0.5f, 4f);
        ApplyColor(bossMarker, new Color(0.85f, 0f, 0.85f));
        Object.DestroyImmediate(bossMarker.GetComponent<Collider>());
    }

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

        MakeHUDText(canvasGo, "ClockText", "Hari 1  07:00  (Siang)",
                    24, Color.white, TextAlignmentOptions.Top,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -20), new Vector2(500, 50), out var clockText);
        if (!canvasGo.GetComponentInChildren<ClockUI>())
        {
            var clockComp = GameObject.Find("ClockText").AddComponent<ClockUI>();
            var so = new SerializedObject(clockComp);
            so.FindProperty("timeText").objectReferenceValue = clockText;
            so.ApplyModifiedProperties();
        }

        MakeHUDText(canvasGo, "InventoryText", "Tas:\n(Kosong)",
                    16, Color.yellow, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(20, 0), new Vector2(240, 300), out var invText);
        if (!canvasGo.GetComponentInChildren<InventoryHUD>())
        {
            var invComp = GameObject.Find("InventoryText").AddComponent<InventoryHUD>();
            var so = new SerializedObject(invComp);
            so.FindProperty("text").objectReferenceValue = invText;
            so.ApplyModifiedProperties();
        }

        MakeHUDText(canvasGo, "QuestText", "Quest:\n- Kumpulkan kayu & bijih besi\n- Temui Kepala Desa",
                    16, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.TopRight,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-20, -20), new Vector2(300, 200), out var questText);
        if (!canvasGo.GetComponentInChildren<QuestHUD>())
        {
            var questComp = GameObject.Find("QuestText").AddComponent<QuestHUD>();
            var so = new SerializedObject(questComp);
            so.FindProperty("text").objectReferenceValue = questText;
            so.ApplyModifiedProperties();
        }

        MakeHUDText(canvasGo, "HealthText", "HP 100 / 100",
                    20, new Color(1f, 0.35f, 0.35f), TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0, 30), new Vector2(300, 40), out var hpText);
        if (!canvasGo.GetComponentInChildren<HealthBar>())
        {
            var hbComp = GameObject.Find("HealthText").AddComponent<HealthBar>();
            var so = new SerializedObject(hbComp);
            var targetProp = so.FindProperty("target");
            if (targetProp != null) targetProp.objectReferenceValue = playerHealth;
            so.ApplyModifiedProperties();
        }

        MakeHUDText(canvasGo, "InteractionPrompt", "[ E ] Interaksi",
                    22, Color.white, TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, 0), new Vector2(400, 50), out var promptText);
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

    static void AddPorchLantern(Transform parent, Vector3 localOffset, Color? lightColor = null, float range = 8f)
    {
        var lanternGo = new GameObject("LanternLight");
        lanternGo.transform.SetParent(parent);
        lanternGo.transform.localPosition = localOffset;

        var l = lanternGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor ?? new Color(1f, 0.72f, 0.35f);
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
                foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = customMat;

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
                bool hasAnyLod0 = false;
                foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.gameObject.name.IndexOf("LOD0", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    { hasAnyLod0 = true; break; }

                foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    string n = mf.gameObject.name;
                    if (hasAnyLod0 &&
                        (n.IndexOf("LOD1", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("LOD2", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("LOD3", System.StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;

                    if (!mf.GetComponent<Collider>() && mf.sharedMesh != null)
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

    static void AddStairRamp(Transform parent, Vector3 localPos, Vector3 size, Vector3 eulerAngles)
    {
        var rampGo = new GameObject("_StairRamp");
        rampGo.transform.SetParent(parent, false);
        rampGo.transform.localPosition = localPos;
        rampGo.transform.localEulerAngles = eulerAngles;

        var box = rampGo.AddComponent<BoxCollider>();
        box.size = size;
        box.center = Vector3.zero;
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
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
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

    static readonly Vector2[][] BendRoadSegments =
    {
        new[] { new Vector2( 4.2f, 36f), new Vector2(14f,  46f) },
        new[] { new Vector2(14f,  46f), new Vector2(14f,  88f) },
        new[] { new Vector2(14f,  88f), new Vector2( 0f, 105f) },
    };

    static void ApplyVillageDesignPass()
    {
        var old = GameObject.Find("--- VILLAGE DETAILS ---");
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject("--- VILLAGE DETAILS ---");

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0042f;

        PlaceVillageWell(root.transform);
        PlaceRoadLamps(root.transform);
    }

    static void BuildNaturalForest(
        Transform root,
        Material bark, Material birchBark,
        Material summerLeaf, Material springLeaf, Material autumnLeaf,
        Material slavicMat, string[] bushModels, string[] rockModels)
    {
        const float cell = 7.5f;
        const float inner = 72f;
        const float outer = 160f;

        float seedA = Random.Range(0f, 1000f);
        float seedB = Random.Range(0f, 1000f);
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

            float big  = Mathf.PerlinNoise(seedA + x * 0.018f, seedA + z * 0.018f);
            float fine = Mathf.PerlinNoise(seedA + x * 0.09f,  seedA + z * 0.09f);
            float density = big * 0.75f + fine * 0.25f;

            float edgeFade = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(inner, inner + 18f, edge));
            float roadFade = Mathf.Lerp(0.20f, 1f, Mathf.InverseLerp(6f, 20f, roadDist));
            float chance = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 0.62f, density)) * edgeFade * roadFade;

            var pos = new Vector3(x, 0f, z);

            if (Random.value <= chance)
            {
                float scale = Mathf.Lerp(0.95f, 1.5f, density) * Random.Range(0.9f, 1.15f);
                float species = Mathf.PerlinNoise(seedB + x * 0.011f, seedB + z * 0.011f);

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

    static bool IsReservedSpot(float x, float z)
    {
        if (Mathf.Abs(x + 148f) < 6f && Mathf.Abs(z) < 64f) return true;
        var p = new Vector2(x, z);
        if (Vector2.Distance(p, new Vector2( 84f, 9f)) < 13f) return true;
        if (Vector2.Distance(p, new Vector2(-84f, 9f)) < 13f) return true;
        if (Vector2.Distance(p, new Vector2( 74f, 40f)) < 16f) return true;

        var spawnGo = GameObject.Find("SpawnUtama");
        if (spawnGo != null)
        {
            if (Vector2.Distance(p, new Vector2(spawnGo.transform.position.x, spawnGo.transform.position.z)) < 15f) return true;
        }
        else
        {
            if (Mathf.Abs(x) < 15f && z >= -105f && z <= -70f) return true;
        }

        return false;
    }

    static float DistanceToRoads(float x, float z)
    {
        var p = new Vector2(x, z);
        float best = Mathf.Min(Mathf.Abs(x), Mathf.Abs(z));
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

    static void PlaceVillageWell(Transform parent)
    {
        if (GameObject.Find("Village_Stone_Well") != null) return;

        for (float r = 14f; r <= 18f; r += 2f)
        {
            for (int a = 0; a < 8; a++)
            {
                float ang = (a * 45f + 22.5f) * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                if (!IsAreaFree(pos, 2.2f)) continue;
                BuildVillageWell(parent, pos);
                return;
            }
        }
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
                float sign = (i % 2 == 0) ? 1f : -1f;
                var pos = dir * d + side * sign * 3.6f;
                if (!IsAreaFree(pos, 0.6f)) continue;

                Vector3 toRoad = -side * sign;
                float yaw = Mathf.Atan2(toRoad.x, toRoad.z) * Mathf.Rad2Deg;
                AddTraditionalVillageLamp(parent, pos, yaw);

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
