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

        // 1. Pastikan tag "NPC" terdaftar di TagManager
        EnsureTagExists("NPC");

        // 2. Setup / Load Materials
        var slavicMat = AssetMaterialSetup.SetupSlavicMaterial();
        AssetMaterialSetup.SetupMedievalMaterials();

        // 3. Bangun Scene
        SetupEnvironment();
        SetupPlayer(out GameObject player, out PlayerController playerCtrl,
                    out Health playerHealth, out PlayerInteractor playerInteractor);
        SetupCamera(player);
        SetupManagers(playerCtrl, playerHealth);
        SetupVillage(slavicMat);
        SetupNPCs();
        SetupEnemySpawners();
        SetupTowers(slavicMat);
        SetupWorldObjects(slavicMat);
        SetupHUD(playerInteractor, playerHealth);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog(
            "✅ Desa Eldara Berhasil Dibangun!",
            "Legenda Desa Eldara telah diperbarui dengan Aset 3D Nyata!\n\n" +
            "Yang baru di scene:\n" +
            "🏡 Rumah desa 3D (Balai Desa, Rumah Pandai Besi, Petani, Dukun, Pos Penjaga)\n" +
            "🌲 Pohon-pohon 3D realistis di sekeliling desa\n" +
            "🪵 Pagar kayu 3D mengelilingi perimeter desa\n" +
            "🏰 Menara pengawas kayu 3D di 6 titik pertahanan\n" +
            "⛏️ Bongkahan bijih besi, tumpukan balok kayu, dan batu alam 3D\n" +
            "🛤️ Jalan setapak batu (cobblestone) + gerobak & peti prop\n" +
            "🌿 Tanah rumput bertekstur + kabut atmosferik (fog)\n\n" +
            "Tekan ▶ Play untuk menjelajahi desa barumu!",
            "Ayo Mainkan!"
        );
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. ENVIRONMENT
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnvironment()
    {
        // Ground plane luas (150 x 150 meter)
        GameObject ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
        }
        ground.transform.localScale = new Vector3(15f, 1f, 15f);

        // Pasang Texture Rumput di Ground
        string grassMatPath = "Assets/_Project/Art/SlavicWorld/Materials/M_Ground_Grass.mat";
        var grassMat = AssetDatabase.LoadAssetAtPath<Material>(grassMatPath);
        if (!grassMat)
        {
            var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/SlavicWorld/Textures/Grass_Texture.png");
            if (grassTex)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                grassMat = new Material(shader);
                grassMat.SetTexture("_BaseMap", grassTex);
                grassMat.SetTextureScale("_BaseMap", new Vector2(30f, 30f));
                grassMat.SetFloat("_Smoothness", 0.05f);
                AssetDatabase.CreateAsset(grassMat, grassMatPath);
            }
        }
        if (grassMat)
        {
            ground.GetComponent<Renderer>().sharedMaterial = grassMat;
        }

        // Efek Kabut Atmosferik (Distance Fog)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.007f;
        RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.85f);

        // Directional Light (Sun)
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

        // DayNightCycle manager
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
            ApplyColor(player, new Color(0.2f, 0.55f, 0.85f)); // Biru petualang
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
    //  5. DESA (RUMAH 3D + PAGAR KAYU + POHON + PROPS)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupVillage(Material slavicMat)
    {
        GameObject village = GameObject.Find("Village");
        if (village) Object.DestroyImmediate(village);

        village = new GameObject("Village");

        // 1. Rumah-rumah 3D Nyata
        var houseData = new (Vector3 pos, Quaternion rot, string name, string fbx, Color fallbackCol)[]
        {
            (new Vector3(-10f, 0f, 10f), Quaternion.Euler(0, 135f, 0), "House_KepalaDesа", "EA03_Village_OutBuilding_PentHouse_01a", new Color(0.6f, 0.4f, 0.2f)),
            (new Vector3( 10f, 0f, 10f), Quaternion.Euler(0, -135f, 0), "House_PandaiBesi", "EA03_Village_OutBuilding_Shed_01a",     new Color(0.4f, 0.4f, 0.4f)),
            (new Vector3(-10f, 0f,-10f), Quaternion.Euler(0, 45f, 0),   "House_Petani",     "EA03_Village_AnimalBreeding_Hovel_01a", new Color(0.5f, 0.7f, 0.3f)),
            (new Vector3( 10f, 0f,-10f), Quaternion.Euler(0, -45f, 0),  "House_Dukun",      "EA03_Village_OutBuilding_Shed_03b",     new Color(0.7f, 0.5f, 0.8f)),
            (new Vector3(  0f, 0f, 14f), Quaternion.Euler(0, 180f, 0),  "House_Penjaga",    "EA03_Village_OutBuilding_Convenience_01a", new Color(0.7f, 0.6f, 0.4f)),
        };

        foreach (var (pos, rot, hName, fbx, fallbackCol) in houseData)
        {
            var houseObj = InstantiateArtModel(fbx, pos, rot, Vector3.one, village.transform, slavicMat);
            if (!houseObj)
            {
                // Fallback primitive
                houseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                houseObj.name = hName;
                houseObj.transform.SetParent(village.transform);
                houseObj.transform.position = pos + new Vector3(0, 1.5f, 0);
                houseObj.transform.localScale = new Vector3(4f, 3f, 4f);
                ApplyColor(houseObj, fallbackCol);

                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = hName + "_Roof";
                roof.transform.SetParent(village.transform);
                roof.transform.position = pos + new Vector3(0, 3.5f, 0);
                roof.transform.localScale = new Vector3(4.5f, 1.5f, 4.5f);
                roof.transform.localRotation = Quaternion.Euler(0, 45f, 0);
                ApplyColor(roof, new Color(0.5f, 0.2f, 0.1f));
                Object.DestroyImmediate(roof.GetComponent<Collider>());
            }
            else
            {
                houseObj.name = hName;
            }
        }

        // 2. Pagar Keliling Desa (3D Wooden Fences)
        BuildVillageFences(village.transform, slavicMat);

        // 3. Pohon-pohon 3D Nyata
        PlaceVillageTrees(village.transform, slavicMat);

        // 4. Props Dekoratif Desa (Jalan setapak batu, gerobak, peti)
        PlaceVillageProps(village.transform, slavicMat);
    }

    static void BuildVillageFences(Transform parent, Material slavicMat)
    {
        string fenceFbx = "EA03_Village_Fence_01a";
        bool hasModel = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/SlavicWorld/Models/{fenceFbx}.fbx") != null;

        if (hasModel)
        {
            // Sisi Utara (Z = 16) dengan celah pintu gerbang tengah
            for (float x = -15f; x <= 15f; x += 3.2f)
            {
                if (Mathf.Abs(x) < 2.5f) continue;
                InstantiateArtModel(fenceFbx, new Vector3(x, 0, 16f), Quaternion.identity, Vector3.one, parent, slavicMat);
            }
            // Sisi Selatan (Z = -16) dengan celah gerbang tengah
            for (float x = -15f; x <= 15f; x += 3.2f)
            {
                if (Mathf.Abs(x) < 2.5f) continue;
                InstantiateArtModel(fenceFbx, new Vector3(x, 0, -16f), Quaternion.Euler(0, 180f, 0), Vector3.one, parent, slavicMat);
            }
            // Sisi Timur (X = 16)
            for (float z = -15f; z <= 15f; z += 3.2f)
            {
                InstantiateArtModel(fenceFbx, new Vector3(16f, 0, z), Quaternion.Euler(0, 90f, 0), Vector3.one, parent, slavicMat);
            }
            // Sisi Barat (X = -16)
            for (float z = -15f; z <= 15f; z += 3.2f)
            {
                InstantiateArtModel(fenceFbx, new Vector3(-16f, 0, z), Quaternion.Euler(0, -90f, 0), Vector3.one, parent, slavicMat);
            }
        }
        else
        {
            // Fallback primitive
            BuildFence(parent, new Vector3( 0f, 0.75f, 16f), new Vector3(32f, 1.5f, 0.4f), "Fence_N");
            BuildFence(parent, new Vector3( 0f, 0.75f,-16f), new Vector3(32f, 1.5f, 0.4f), "Fence_S");
            BuildFence(parent, new Vector3( 16f,0.75f,  0f), new Vector3(0.4f,1.5f, 32f), "Fence_E");
            BuildFence(parent, new Vector3(-16f,0.75f,  0f), new Vector3(0.4f,1.5f, 32f), "Fence_W");
        }
    }

    static void PlaceVillageTrees(Transform parent, Material slavicMat)
    {
        var treeData = new (Vector3 pos, string fbx)[]
        {
            (new Vector3( 6f, 0f,  3f), "EA03_Nature_Tree_01b"),
            (new Vector3(-6f, 0f,  3f), "EA03_Nature_Tree_02b"),
            (new Vector3( 6f, 0f, -4f), "EA03_Nature_Tree_03b"),
            (new Vector3(-6f, 0f, -4f), "EA03_Nature_Tree_05a"),
            (new Vector3( 0f, 0f,-11f), "EA03_Nature_Tree_06b"),
            (new Vector3(-13f, 0f,  3f), "EA03_Nature_Tree_01b"),
            (new Vector3( 13f, 0f,  3f), "EA03_Nature_Tree_02b"),
            (new Vector3(-4f, 0f, 13f), "EA03_Nature_Tree_03b"),
            (new Vector3( 4f, 0f, 13f), "EA03_Nature_Tree_05a"),
            (new Vector3(-13f, 0f,-13f), "EA03_Nature_Tree_06b"),
            (new Vector3( 13f, 0f,-13f), "EA03_Nature_Tree_01b"),
        };

        foreach (var (pos, fbx) in treeData)
        {
            var tree = InstantiateArtModel(fbx, pos, Quaternion.Euler(0, Random.Range(0, 360f), 0), Vector3.one, parent, slavicMat);
            if (!tree)
            {
                PlaceTreeFallback(parent, pos);
            }
        }
    }

    static void PlaceVillageProps(Transform parent, Material slavicMat)
    {
        // Jalan setapak batu (Cobblestone path)
        InstantiateArtModel("EA03_Environment_Road_Cobble_01a", new Vector3(0, 0.02f, 0), Quaternion.identity, new Vector3(1.5f, 1f, 1.5f), parent, slavicMat);

        // Gerobak & Peti Medieval
        InstantiateArtModel("Prop_Wagon", new Vector3(4f, 0f, 2f), Quaternion.Euler(0, 35f, 0), Vector3.one, parent);
        InstantiateArtModel("Prop_Crate", new Vector3(5.5f, 0f, 2.8f), Quaternion.Euler(0, 15f, 0), Vector3.one, parent);
        InstantiateArtModel("Prop_Crate", new Vector3(5.5f, 0.8f, 2.8f), Quaternion.Euler(0, 45f, 0), Vector3.one * 0.8f, parent);
    }

    static void BuildFence(Transform parent, Vector3 pos, Vector3 scale, string fName)
    {
        var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fence.name = fName;
        fence.transform.SetParent(parent);
        fence.transform.position = pos;
        fence.transform.localScale = scale;
        ApplyColor(fence, new Color(0.4f, 0.25f, 0.1f));
    }

    static void PlaceTreeFallback(Transform parent, Vector3 pos)
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
        if (npcRoot) Object.DestroyImmediate(npcRoot);
        npcRoot = new GameObject("--- NPCs ---");

        var npcData = new (string npcName, Vector3 pos, Color col, string greeting)[]
        {
            ("NPC_KepalaDesа",  new Vector3(-8f, 1.05f, 6f),
             new Color(1f, 0.8f, 0.3f), // Jubah emas
             "Selamat datang, petualang! Desa Eldara dalam bahaya. Saat malam tiba, para goblin akan menyerang desa kita!"),

            ("NPC_PandaiBesi",  new Vector3( 8f, 1.05f, 6f),
             new Color(0.6f, 0.3f, 0.2f), // Pakaian tempa kulit
             "Butuh senjata atau menara pertahanan? Bawakan aku bijih besi dan batu, aku siap membangunnya!"),

            ("NPC_Petani",      new Vector3(-8f, 1.05f,-6f),
             new Color(0.3f, 0.65f, 0.25f), // Pakaian hijau petani
             "Lahan gandumku hancur diserang monster. Kumpulkan balok kayu untuk membantu memperbaiki lumbung!"),

            ("NPC_Dukun",       new Vector3( 8f, 1.05f,-6f),
             new Color(0.55f, 0.25f, 0.85f), // Jubah ungu misterius
             "Altar Segel kuno di utara menyimpan rahasia kutukan malam ini. Jangan sentuh altar itu sebelum kamu siap menghadapi Raja Goblin!"),
        };

        foreach (var (npcName, pos, col, greeting) in npcData)
        {
            var npcGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcGo.name = npcName;
            npcGo.tag = "NPC";
            npcGo.transform.SetParent(npcRoot.transform);
            npcGo.transform.position = pos;
            ApplyColor(npcGo, col);

            // Label topi / kepala
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npcGo.transform);
            head.transform.localPosition = new Vector3(0, 0.6f, 0);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            ApplyColor(head, col * 0.7f);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            npcGo.AddComponent<NpcBrain>();
            var npcDlg = npcGo.AddComponent<NpcDialogue>();
            CreateSimpleDialogue(npcName, greeting, npcDlg);
        }
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

    // ─────────────────────────────────────────────────────────────────────
    //  7. ENEMY SPAWNER
    // ─────────────────────────────────────────────────────────────────────
    static void SetupEnemySpawners()
    {
        if (GameObject.Find("--- ENEMY SPAWNERS ---")) return;

        var spawnerRoot = new GameObject("--- ENEMY SPAWNERS ---");

        var spawnPositions = new Vector3[]
        {
            new Vector3( 0f, 0f, 22f),   // Utara
            new Vector3( 0f, 0f,-22f),   // Selatan
            new Vector3( 22f,0f, 0f),    // Timur
            new Vector3(-22f,0f, 0f),    // Barat
        };

        var spawnerGo = new GameObject("NightSpawner");
        spawnerGo.transform.SetParent(spawnerRoot.transform);
        spawnerGo.transform.position = Vector3.zero;
        spawnerGo.AddComponent<NightSpawner>();

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

        var bossMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bossMarker.name = "BossSpawnPoint";
        bossMarker.transform.SetParent(spawnerRoot.transform);
        bossMarker.transform.position = new Vector3(0f, 0f, 25f);
        bossMarker.transform.localScale = new Vector3(3f, 0.5f, 3f);
        ApplyColor(bossMarker, new Color(0.8f, 0f, 0.8f));
        Object.DestroyImmediate(bossMarker.GetComponent<Collider>());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8. TOWER SLOTS 3D & SPIKETRAP
    // ─────────────────────────────────────────────────────────────────────
    static void SetupTowers(Material slavicMat)
    {
        GameObject towerRoot = GameObject.Find("--- TOWERS ---");
        if (towerRoot) Object.DestroyImmediate(towerRoot);

        towerRoot = new GameObject("--- TOWERS ---");

        // 6 Titik Menara Pengawas di penjuru pagar desa
        var towerSlots = new Vector3[]
        {
            new Vector3(-13f, 0f, 13f),  // NW
            new Vector3( 13f, 0f, 13f),  // NE
            new Vector3(-15f, 0f,  0f),  // W
            new Vector3( 15f, 0f,  0f),  // E
            new Vector3(-13f, 0f,-13f),  // SW
            new Vector3( 13f, 0f,-13f),  // SE
        };

        for (int i = 0; i < towerSlots.Length; i++)
        {
            string slotName = $"TowerSlot_{i + 1}";
            var towerObj = InstantiateArtModel("EA03_Village_Tover_01a", towerSlots[i], Quaternion.identity, Vector3.one, towerRoot.transform, slavicMat);
            if (!towerObj)
            {
                // Fallback primitive
                towerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                towerObj.transform.position = towerSlots[i] + new Vector3(0, 1.5f, 0);
                towerObj.transform.localScale = new Vector3(1.5f, 3f, 1.5f);
                ApplyColor(towerObj, new Color(0.5f, 0.4f, 0.3f));
            }
            towerObj.name = slotName;
            if (!towerObj.GetComponent<Tower>()) towerObj.AddComponent<Tower>();
        }

        // SpikeTraps di pintu masuk
        var trapPositions = new Vector3[]
        {
            new Vector3( 0f, 0.05f, 15f),
            new Vector3(-4f, 0.05f,-15f),
            new Vector3( 4f, 0.05f,-15f),
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
    //  9. WORLD OBJECTS 3D (Resource + Altar)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupWorldObjects(Material slavicMat)
    {
        GameObject itemsRoot = GameObject.Find("--- WORLD OBJECTS ---");
        if (itemsRoot) Object.DestroyImmediate(itemsRoot);

        itemsRoot = new GameObject("--- WORLD OBJECTS ---");

        // 1. Bijih Besi 3D (Rock CatHead)
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_1", "EA03_Environment_Rock_CatHead_01a", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(5f, 0f, 5f), Quaternion.identity, new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.4f, 0.4f, 0.5f));
        PlaceArtResource(itemsRoot.transform, "Iron_Ore_2", "EA03_Environment_Rock_CatHead_01b", "iron_ore",
            "Tekan E untuk menambang Bijih Besi", new Vector3(-5f, 0f, 5f), Quaternion.Euler(0, 70f, 0), new Vector3(1.2f, 1.2f, 1.2f), slavicMat, new Color(0.4f, 0.4f, 0.5f));

        // 2. Balok Kayu 3D (CutOut Wood Logs)
        PlaceArtResource(itemsRoot.transform, "Wood_Log_1", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Kayu", new Vector3(5f, 0f, -5f), Quaternion.identity, Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));
        PlaceArtResource(itemsRoot.transform, "Wood_Log_2", "EA03_Prop_Forester_CutOutTrees_03", "wood",
            "Tekan E untuk mengambil Kayu", new Vector3(-5f, 0f, -5f), Quaternion.Euler(0, 90f, 0), Vector3.one, slavicMat, new Color(0.5f, 0.3f, 0.1f));

        // 3. Batu Alam 3D (Rock Head)
        PlaceArtResource(itemsRoot.transform, "Stone_Rock", "EA03_Environment_Rock_Head_01a", "stone",
            "Tekan E untuk memecah Batu", new Vector3(0f, 0f, 7f), Quaternion.identity, Vector3.one, slavicMat, new Color(0.55f, 0.55f, 0.55f));

        // 4. Altar Segel di Utara Desa
        var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        altar.name = "Seal_Altar";
        altar.transform.SetParent(itemsRoot.transform);
        altar.transform.position = new Vector3(0f, 0.25f, 12f);
        altar.transform.localScale = new Vector3(3f, 0.5f, 3f);
        ApplyColor(altar, new Color(0.6f, 0.5f, 1f));
        altar.AddComponent<SealAltar>();

        // Pilar-pilar Altar
        for (int p = 0; p < 4; p++)
        {
            float angle = p * 90f * Mathf.Deg2Rad;
            Vector3 pillarPos = new Vector3(Mathf.Cos(angle) * 2f, 0f, 12f + Mathf.Sin(angle) * 2f);
            var pillar = InstantiateArtModel("EA03_Environment_Rock_Head_01c", pillarPos, Quaternion.identity, new Vector3(0.7f, 2f, 0.7f), itemsRoot.transform, slavicMat);
            if (!pillar)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fallback.name = $"AltarPillar_{p}";
                fallback.transform.SetParent(itemsRoot.transform);
                fallback.transform.position = pillarPos + new Vector3(0, 1f, 0);
                fallback.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
                ApplyColor(fallback, new Color(0.7f, 0.6f, 1f));
                Object.DestroyImmediate(fallback.GetComponent<Collider>());
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
    //  10. HUD
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

        // Clock (atas tengah)
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

        // Inventory (kiri tengah)
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

        // Quest HUD (kanan atas)
        MakeHUDText(canvasGo, "QuestText", "📜 Quest:\n- Kumpulkan kayu & bijih besi\n- Bicara dengan Kepala Desa",
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

        // Health Bar (bawah tengah)
        MakeHUDText(canvasGo, "HealthText", "❤️ 100 / 100",
                    20, new Color(1f, 0.3f, 0.3f), TextAlignmentOptions.Center,
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

        // Interaction Prompt (tengah layar)
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
    //  HELPERS (3D MODEL INSTANTIATOR & TAGS)
    // ─────────────────────────────────────────────────────────────────────
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

            // Pasang MeshCollider agar bisa dipijak & ditabrak
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
}
#endif
