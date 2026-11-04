#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click scene setup tool untuk Legenda Desa Eldara.
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

        // Pastikan tag "NPC" terdaftar di TagManager
        EnsureTagExists("NPC");

        SetupEnvironment();
        SetupPlayer(out GameObject player, out PlayerController playerCtrl,
                    out Health playerHealth, out PlayerInteractor playerInteractor);
        SetupCamera(player);
        SetupManagers(playerCtrl, playerHealth);
        SetupVillage();
        SetupNPCs();
        SetupEnemySpawners();
        SetupTowers();
        SetupWorldObjects();
        SetupHUD(playerInteractor, playerHealth);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog(
            "✅ Scene Siap!",
            "Legenda Desa Eldara berhasil disetup!\n\n" +
            "Yang dibuat:\n" +
            "🌏 Terrain + Ground\n" +
            "🧑 Player (WASD, Shift lari, E interaksi, V ganti kamera)\n" +
            "🏘️ 5 Rumah desa + pagar\n" +
            "🧑‍🤝‍🧑 4 NPC (Kepala Desa, Pandai Besi, Petani, Dukun)\n" +
            "👹 4 Titik spawn musuh (aktif malam hari)\n" +
            "🏰 6 Slot tower + 3 SpikeTrap\n" +
            "⚙️ Semua Manager & HUD\n\n" +
            "Tekan ▶ Play untuk mencoba!\n" +
            "(Bake NavMesh di Window > AI > Navigation jika NPC tidak jalan)",
            "Siap!"
        );
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. ENVIRONMENT
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnvironment()
    {
        // Ground plane (fallback, tampak di bawah terrain)
        GameObject ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
        }

        // Directional Light (Sun)
        Light sunLight = null;
        var go_sun = GameObject.Find("Directional Light");
        if (go_sun) sunLight = go_sun.GetComponent<Light>();
        if (!sunLight)
        {
            // Cari light manapun yang directional
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

        // DayNightCycle manager (di sini karena butuh sunLight referensi langsung)
        GameObject managers = GetOrCreateManagerRoot();
        var dnc = managers.GetComponent<DayNightCycle>() ?? managers.AddComponent<DayNightCycle>();
        var soDnc = new SerializedObject(dnc);
        soDnc.FindProperty("sun").objectReferenceValue = sunLight;

        var curve = new AnimationCurve(
            new Keyframe(0f, 0.05f), new Keyframe(0.25f, 0.2f),
            new Keyframe(0.5f, 1.2f), new Keyframe(0.75f, 0.3f),
            new Keyframe(1f, 0.05f));
        soDnc.FindProperty("sunIntensity").animationCurveValue = curve;

        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.1f, 0.15f, 0.35f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f),
                new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.5f),
                new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0.75f),
                new GradientColorKey(new Color(0.1f, 0.15f, 0.35f), 1f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        soDnc.FindProperty("sunColor").gradientValue = grad;
        soDnc.ApplyModifiedProperties();
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
            player.transform.position = new Vector3(0, 1.05f, 0);

            // Visor wajah (arah hadap)
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "FaceIndicator";
            visor.transform.SetParent(player.transform);
            visor.transform.localPosition = new Vector3(0, 0.4f, 0.38f);
            visor.transform.localScale = new Vector3(0.5f, 0.15f, 0.25f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            ApplyColor(visor, Color.black);
        }
        // Hapus CapsuleCollider bawaan, ganti CharacterController
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
    //  3. KAMERA
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
    //  4. MANAGER
    // ─────────────────────────────────────────────────────────────────────
    static void SetupManagers(PlayerController playerCtrl, Health playerHealth)
    {
        var managers = GetOrCreateManagerRoot();

        if (!managers.GetComponent<Inventory>())       managers.AddComponent<Inventory>();
        if (!managers.GetComponent<QuestManager>())    managers.AddComponent<QuestManager>();
        if (!managers.GetComponent<DialogueManager>()) managers.AddComponent<DialogueManager>();
        if (!managers.GetComponent<MusicManager>())    managers.AddComponent<MusicManager>();
        if (!managers.GetComponent<NightLight>())      managers.AddComponent<NightLight>();

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
    //  5. DESA (5 RUMAH + PAGAR)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupVillage()
    {
        GameObject village = GameObject.Find("Village");
        if (village) return; // sudah ada

        village = new GameObject("Village");

        // Posisi 5 rumah (berlabel nama NPC yang tinggal di sana)
        var houseData = new (Vector3 pos, string name, Color col)[]
        {
            (new Vector3(-8f, 0f, 8f),  "House_KepalaDesа",  new Color(0.6f, 0.4f, 0.2f)),
            (new Vector3( 8f, 0f, 8f),  "House_PandaiBesi",  new Color(0.4f, 0.4f, 0.4f)),
            (new Vector3(-8f, 0f,-8f),  "House_Petani",      new Color(0.5f, 0.7f, 0.3f)),
            (new Vector3( 8f, 0f,-8f),  "House_Dukun",       new Color(0.7f, 0.5f, 0.8f)),
            (new Vector3( 0f, 0f,10f),  "House_Penjaga",     new Color(0.7f, 0.6f, 0.4f)),
        };

        foreach (var (pos, hName, col) in houseData)
        {
            if (GameObject.Find(hName)) continue;

            // Badan rumah
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = hName;
            body.transform.SetParent(village.transform);
            body.transform.position = pos + new Vector3(0, 1.5f, 0);
            body.transform.localScale = new Vector3(4f, 3f, 4f);
            ApplyColor(body, col);

            // Atap (prisma dari cube miring)
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = hName + "_Roof";
            roof.transform.SetParent(village.transform);
            roof.transform.position = pos + new Vector3(0, 3.5f, 0);
            roof.transform.localScale = new Vector3(4.5f, 1.5f, 4.5f);
            roof.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            ApplyColor(roof, new Color(0.5f, 0.2f, 0.1f));
            Object.DestroyImmediate(roof.GetComponent<Collider>());
        }

        // Pagar keliling desa (empat sisi, cube panjang tipis)
        BuildFence(village.transform, new Vector3( 0f, 0.75f, 15f), new Vector3(30f, 1.5f, 0.4f), "Fence_N");
        BuildFence(village.transform, new Vector3( 0f, 0.75f,-15f), new Vector3(30f, 1.5f, 0.4f), "Fence_S");
        BuildFence(village.transform, new Vector3( 15f,0.75f,  0f), new Vector3(0.4f,1.5f, 30f), "Fence_E");
        BuildFence(village.transform, new Vector3(-15f,0.75f,  0f), new Vector3(0.4f,1.5f, 30f), "Fence_W");

        // Pohon dekoratif
        PlaceTree(village.transform, new Vector3( 5f, 0f, 2f));
        PlaceTree(village.transform, new Vector3(-5f, 0f, 2f));
        PlaceTree(village.transform, new Vector3( 5f, 0f,-5f));
        PlaceTree(village.transform, new Vector3(-5f, 0f,-5f));
        PlaceTree(village.transform, new Vector3( 0f, 0f,-12f));
    }

    static void BuildFence(Transform parent, Vector3 pos, Vector3 scale, string fName)
    {
        if (GameObject.Find(fName)) return;
        var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fence.name = fName;
        fence.transform.SetParent(parent);
        fence.transform.position = pos;
        fence.transform.localScale = scale;
        ApplyColor(fence, new Color(0.4f, 0.25f, 0.1f));
    }

    static void PlaceTree(Transform parent, Vector3 pos)
    {
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Tree_Trunk";
        trunk.transform.SetParent(parent);
        trunk.transform.position = pos + new Vector3(0, 1f, 0);
        trunk.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
        ApplyColor(trunk, new Color(0.35f, 0.2f, 0.05f));
        Object.DestroyImmediate(trunk.GetComponent<Collider>());

        var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Tree_Leaves";
        leaves.transform.SetParent(parent);
        leaves.transform.position = pos + new Vector3(0, 2.8f, 0);
        leaves.transform.localScale = new Vector3(1.5f, 1.8f, 1.5f);
        ApplyColor(leaves, new Color(0.1f, 0.55f, 0.15f));
        Object.DestroyImmediate(leaves.GetComponent<Collider>());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  6. NPC
    // ─────────────────────────────────────────────────────────────────────
    static void SetupNPCs()
    {
        GameObject npcRoot = GameObject.Find("--- NPCs ---");
        if (!npcRoot) npcRoot = new GameObject("--- NPCs ---");

        var npcData = new (string npcName, Vector3 pos, Color col, string greeting)[]
        {
            ("NPC_KepalaDesа",  new Vector3(-8f, 1.05f, 5f),
             new Color(1f, 0.8f, 0.4f),
             "Selamat datang, petualang! Desa kami dalam bahaya. Malam ini para goblin akan menyerang!"),

            ("NPC_PandaiBesi",  new Vector3( 8f, 1.05f, 5f),
             new Color(0.5f, 0.5f, 0.5f),
             "Kalau kamu punya besi, aku bisa bikin senjata atau bangunan pelindung."),

            ("NPC_Petani",      new Vector3(-8f, 1.05f,-5f),
             new Color(0.4f, 0.7f, 0.3f),
             "Tolong, lahan pertanianku rusak! Butuh kayu untuk memperbaiki pagar."),

            ("NPC_Dukun",       new Vector3( 8f, 1.05f,-5f),
             new Color(0.6f, 0.3f, 0.9f),
             "Altar segel itu... kunci untuk menghentikan kutukan malam. Jangan sentuh sebelum saatnya."),
        };

        foreach (var (npcName, pos, col, greeting) in npcData)
        {
            if (GameObject.Find(npcName)) continue;

            var npcGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcGo.name = npcName;
            npcGo.tag = "NPC";
            npcGo.transform.SetParent(npcRoot.transform);
            npcGo.transform.position = pos;
            ApplyColor(npcGo, col);

            // Label nama di atas kepala (sphere kecil sebagai penanda)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npcGo.transform);
            head.transform.localPosition = new Vector3(0, 0.6f, 0);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            ApplyColor(head, col * 0.7f);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Tambah komponen NPC
            npcGo.AddComponent<NpcBrain>();
            var npcDlg = npcGo.AddComponent<NpcDialogue>();

            // Buat DialogueData sederhana (ScriptableObject) langsung di Assets
            CreateSimpleDialogue(npcName, greeting, npcDlg);
        }
    }

    // Helper buat DialogueData asset sederhana (1 baris dialogue)
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
            // DialogueData.lines adalah DialogueData.Line[] (bukan string[])
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

    // ─────────────────────────────────────────────────────────────────────
    //  7. ENEMY SPAWNER
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnemySpawners()
    {
        if (GameObject.Find("--- ENEMY SPAWNERS ---")) return;

        var spawnerRoot = new GameObject("--- ENEMY SPAWNERS ---");

        // 4 titik spawn di luar pagar (4 penjuru)
        var spawnPositions = new Vector3[]
        {
            new Vector3( 0f, 0f, 18f),   // Utara
            new Vector3( 0f, 0f,-18f),   // Selatan
            new Vector3( 18f,0f, 0f),    // Timur
            new Vector3(-18f,0f, 0f),    // Barat
        };

        var spawnerGo = new GameObject("NightSpawner");
        spawnerGo.transform.SetParent(spawnerRoot.transform);
        spawnerGo.transform.position = Vector3.zero;

        var nightSpawner = spawnerGo.AddComponent<NightSpawner>();

        // Buat marker visual untuk tiap titik spawn
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"SpawnPoint_{i + 1}";
            marker.transform.SetParent(spawnerRoot.transform);
            marker.transform.position = spawnPositions[i];
            marker.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);
            ApplyColor(marker, new Color(0.8f, 0.1f, 0.1f));
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        // Boss Spawn Point (di dekat altar)
        var bossMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bossMarker.name = "BossSpawnPoint";
        bossMarker.transform.SetParent(spawnerRoot.transform);
        bossMarker.transform.position = new Vector3(0f, 0f, 20f);
        bossMarker.transform.localScale = new Vector3(3f, 0.5f, 3f);
        ApplyColor(bossMarker, new Color(0.8f, 0f, 0.8f));
        Object.DestroyImmediate(bossMarker.GetComponent<Collider>());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8. TOWER SLOTS & SPIKITRAP
    // ─────────────────────────────────────────────────────────────────────
    static void SetupTowers()
    {
        if (GameObject.Find("--- TOWERS ---")) return;

        var towerRoot = new GameObject("--- TOWERS ---");

        // 6 slot tower di sepanjang pagar (posisi strategis)
        var towerSlots = new Vector3[]
        {
            new Vector3(-10f, 0f, 14f),  // Sudut NW
            new Vector3( 10f, 0f, 14f),  // Sudut NE
            new Vector3(-14f, 0f,  0f),  // Barat tengah
            new Vector3( 14f, 0f,  0f),  // Timur tengah
            new Vector3(-10f, 0f,-14f),  // Sudut SW
            new Vector3( 10f, 0f,-14f),  // Sudut SE
        };

        for (int i = 0; i < towerSlots.Length; i++)
        {
            // Fondasi / platform tower
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = $"TowerSlot_{i + 1}";
            platform.transform.SetParent(towerRoot.transform);
            platform.transform.position = towerSlots[i] + new Vector3(0, 0.1f, 0);
            platform.transform.localScale = new Vector3(2f, 0.2f, 2f);
            ApplyColor(platform, new Color(0.6f, 0.6f, 0.5f));
            Object.DestroyImmediate(platform.GetComponent<Collider>());

            // Tower bangunan di atas platform
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = $"Tower_{i + 1}";
            tower.transform.SetParent(towerRoot.transform);
            tower.transform.position = towerSlots[i] + new Vector3(0, 1.5f, 0);
            tower.transform.localScale = new Vector3(1.5f, 3f, 1.5f);
            ApplyColor(tower, new Color(0.5f, 0.4f, 0.3f));

            tower.AddComponent<Tower>();
        }

        // 3 SpikeTrap di jalur masuk desa
        var trapPositions = new Vector3[]
        {
            new Vector3( 0f, 0.05f, 13f),   // Pintu utara
            new Vector3(-5f, 0.05f,-13f),   // Pintu selatan kiri
            new Vector3( 5f, 0.05f,-13f),   // Pintu selatan kanan
        };

        for (int i = 0; i < trapPositions.Length; i++)
        {
            var trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trap.name = $"SpikeTrap_{i + 1}";
            trap.transform.SetParent(towerRoot.transform);
            trap.transform.position = trapPositions[i];
            trap.transform.localScale = new Vector3(2f, 0.1f, 2f);
            ApplyColor(trap, new Color(0.3f, 0.3f, 0.5f));
            trap.AddComponent<SpikeTrap>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9. WORLD OBJECTS (Resource + Altar)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupWorldObjects()
    {
        GameObject itemsRoot = GameObject.Find("--- WORLD OBJECTS ---");
        if (!itemsRoot) itemsRoot = new GameObject("--- WORLD OBJECTS ---");

        // Resource nodes
        PlaceResource(itemsRoot.transform, "Iron_Ore_1", "iron_ore", "Tekan E untuk menambang Bijih Besi",
                      new Vector3( 4f, 0.5f, 4f), new Color(0.4f, 0.4f, 0.5f));
        PlaceResource(itemsRoot.transform, "Iron_Ore_2", "iron_ore", "Tekan E untuk menambang Bijih Besi",
                      new Vector3(-4f, 0.5f, 4f), new Color(0.4f, 0.4f, 0.5f));
        PlaceResource(itemsRoot.transform, "Wood_Log_1", "wood",     "Tekan E untuk menebang Kayu",
                      new Vector3( 4f, 0.5f,-4f), new Color(0.5f, 0.3f, 0.1f));
        PlaceResource(itemsRoot.transform, "Wood_Log_2", "wood",     "Tekan E untuk menebang Kayu",
                      new Vector3(-4f, 0.5f,-4f), new Color(0.5f, 0.3f, 0.1f));
        PlaceResource(itemsRoot.transform, "Stone_Rock",  "stone",   "Tekan E untuk memecah Batu",
                      new Vector3( 0f, 0.5f, 6f), new Color(0.55f, 0.55f, 0.55f));

        // Altar Segel (di tengah utara desa, di dalam pagar)
        if (!GameObject.Find("Seal_Altar"))
        {
            var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "Seal_Altar";
            altar.transform.SetParent(itemsRoot.transform);
            altar.transform.position = new Vector3(0f, 0.25f, 12f);
            altar.transform.localScale = new Vector3(3f, 0.5f, 3f);
            ApplyColor(altar, new Color(0.6f, 0.5f, 1f));
            altar.AddComponent<SealAltar>();

            // Pilar-pilar altar
            for (int p = 0; p < 4; p++)
            {
                float angle = p * 90f * Mathf.Deg2Rad;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"AltarPillar_{p}";
                pillar.transform.SetParent(itemsRoot.transform);
                pillar.transform.position = new Vector3(
                    Mathf.Cos(angle) * 2f, 1f, 12f + Mathf.Sin(angle) * 2f);
                pillar.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
                ApplyColor(pillar, new Color(0.7f, 0.6f, 1f));
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }
        }
    }

    static void PlaceResource(Transform parent, string goName, string itemId,
                              string prompt, Vector3 pos, Color col)
    {
        if (GameObject.Find(goName)) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = goName;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        ApplyColor(go, col);

        var pickup = go.AddComponent<ResourcePickup>();
        pickup.itemId = itemId;
        pickup.amount = 1;
        pickup.prompt = prompt;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  10. HUD
    // ─────────────────────────────────────────────────────────────────────
    static void SetupHUD(PlayerInteractor playerInteractor, Health playerHealth)
    {
        // Canvas
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

        // EventSystem
        if (!Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ── Clock (atas tengah) ──
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

        // ── Inventory (kiri tengah) ──
        MakeHUDText(canvasGo, "InventoryText", "🎒 Tas:\n(Kosong)",
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

        // ── Quest HUD (kanan atas) ──
        MakeHUDText(canvasGo, "QuestText", "📜 Quest:\n-",
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

        // ── Health Bar (bawah tengah) ──
        MakeHUDText(canvasGo, "HealthText", "❤️ 100 / 100",
                    20, new Color(1f, 0.3f, 0.3f), TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0, 30), new Vector2(300, 40),
                    out var hpText);
        // HealthBar menggunakan Slider; buat simple text dulu (HealthBar akan auto-find player)
        if (!canvasGo.GetComponentInChildren<HealthBar>())
        {
            var hbComp = GameObject.Find("HealthText").AddComponent<HealthBar>();
            var so = new SerializedObject(hbComp);
            // Wire target jika ada field 'target'
            var targetProp = so.FindProperty("target");
            if (targetProp != null)
                targetProp.objectReferenceValue = playerHealth;
            so.ApplyModifiedProperties();
        }

        // ── Interaction Prompt (tengah layar) ──
        MakeHUDText(canvasGo, "InteractionPrompt", "[ E ] Tekan untuk interaksi",
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

        // ── Dialogue Panel (bawah layar) ──
        SetupDialoguePanel(canvasGo);
    }

    static void SetupDialoguePanel(GameObject canvasGo)
    {
        if (GameObject.Find("DialoguePanel")) return;

        // Panel background
        var panelGo = new GameObject("DialoguePanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var img = panelGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.8f);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0f);
        panelRect.anchorMax = new Vector2(0.9f, 0f);
        panelRect.pivot    = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0, 20);
        panelRect.sizeDelta = new Vector2(0, 180);

        // Speaker name
        var nameGo = new GameObject("SpeakerName");
        nameGo.transform.SetParent(panelGo.transform, false);
        var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "Kepala Desa";
        nameTxt.fontSize = 20;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = Color.yellow;
        var nameRect = nameTxt.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot     = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(-40, 30);

        // Dialogue text
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
        dlgRect.offsetMin = new Vector2(20, 10);
        dlgRect.offsetMax = new Vector2(-20, -40);

        panelGo.SetActive(false);

        // Wire ke DialogueManager
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
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────
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

    /// <summary>
    /// Buat TMP Text di Canvas; return referensi TextMeshProUGUI-nya.
    /// </summary>
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

    /// <summary>
    /// Daftarkan tag baru di TagManager jika belum ada.
    /// </summary>
    static void EnsureTagExists(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var tagsProp = tagManager.FindProperty("tags");

        // Cek apakah tag sudah ada
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return; // sudah ada

        // Tambahkan tag baru
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}
#endif
