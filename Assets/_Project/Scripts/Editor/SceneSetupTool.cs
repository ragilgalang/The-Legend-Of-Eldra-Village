#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SceneSetupTool
{
    [MenuItem("Eldara/Setup Prototype Scene")]
    public static void SetupScene()
    {
        // 1. Setup Ground
        GameObject ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 meter area
        }

        // 2. Setup Directional Light (Sun)
        Light sunLight = null;
        var dirLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in dirLights)
        {
            if (l.type == LightType.Directional) { sunLight = l; break; }
        }
        if (!sunLight)
        {
            var lightGo = new GameObject("Directional Light");
            sunLight = lightGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 3. Setup Player
        GameObject player = GameObject.FindWithTag("Player");
        if (!player)
        {
            player = GameObject.Find("Player");
        }
        if (!player)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.05f, 0);

            // Indikator arah hadap (mata/kacamata)
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "FaceIndicator";
            visor.transform.SetParent(player.transform);
            visor.transform.localPosition = new Vector3(0, 0.5f, 0.35f);
            visor.transform.localScale = new Vector3(0.5f, 0.2f, 0.3f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
        }

        // Komponen Player
        var cc = player.GetComponent<CharacterController>();
        if (!cc)
        {
            // Hapus collider capsule bawaan primitive agar tidak bentrok dengan CharacterController
            var capCollider = player.GetComponent<CapsuleCollider>();
            if (capCollider) Object.DestroyImmediate(capCollider);
            cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0, 0);
            cc.height = 2f;
            cc.radius = 0.5f;
        }

        var playerCtrl = player.GetComponent<PlayerController>() ?? player.AddComponent<PlayerController>();
        var playerHealth = player.GetComponent<Health>() ?? player.AddComponent<Health>();
        playerHealth.max = 100f;
        var playerCombat = player.GetComponent<PlayerCombat>() ?? player.AddComponent<PlayerCombat>();
        var playerInteractor = player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();
        var hitFlash = player.GetComponent<HitFlash>() ?? player.AddComponent<HitFlash>();

        // 4. Setup Camera & CameraRig
        Camera cam = Camera.main;
        if (!cam)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        var camRig = cam.GetComponent<CameraRig>() ?? cam.gameObject.AddComponent<CameraRig>();
        var soCam = new SerializedObject(camRig);
        soCam.FindProperty("target").objectReferenceValue = player.transform;
        
        var renderersProp = soCam.FindProperty("playerRenderers");
        renderersProp.arraySize = 1;
        renderersProp.GetArrayElementAtIndex(0).objectReferenceValue = player.GetComponent<Renderer>();
        soCam.ApplyModifiedProperties();

        // 5. Setup Core Managers
        GameObject managers = GameObject.Find("--- CORE MANAGERS ---");
        if (!managers)
        {
            managers = new GameObject("--- CORE MANAGERS ---");
        }

        // DayNightCycle
        var dnc = managers.GetComponent<DayNightCycle>() ?? managers.AddComponent<DayNightCycle>();
        var soDnc = new SerializedObject(dnc);
        soDnc.FindProperty("sun").objectReferenceValue = sunLight;
        
        // Buat kurva intensitas dan gradien default
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.05f),
            new Keyframe(0.25f, 0.2f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(0.75f, 0.3f),
            new Keyframe(1f, 0.05f)
        );
        soDnc.FindProperty("sunIntensity").animationCurveValue = curve;

        var colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.1f, 0.15f, 0.35f), 0f),      // Midnight
                new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f),        // Dawn
                new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.5f),        // Noon
                new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0.75f),       // Dusk
                new GradientColorKey(new Color(0.1f, 0.15f, 0.35f), 1f)        // Midnight
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        soDnc.FindProperty("sunColor").gradientValue = colorGrad;
        soDnc.ApplyModifiedProperties();

        // Inventory, QuestManager, DialogueManager, MusicManager, GameManager, SaveSystem
        if (!managers.GetComponent<Inventory>()) managers.AddComponent<Inventory>();
        if (!managers.GetComponent<QuestManager>()) managers.AddComponent<QuestManager>();
        if (!managers.GetComponent<DialogueManager>()) managers.AddComponent<DialogueManager>();
        if (!managers.GetComponent<MusicManager>()) managers.AddComponent<MusicManager>();
        
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

        // 6. Setup Sample Interactable Items
        GameObject itemsRoot = GameObject.Find("--- WORLD OBJECTS ---");
        if (!itemsRoot) itemsRoot = new GameObject("--- WORLD OBJECTS ---");

        // Contoh: Iron Ore
        if (!GameObject.Find("Iron_Ore_Node"))
        {
            var ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ore.name = "Iron_Ore_Node";
            ore.transform.SetParent(itemsRoot.transform);
            ore.transform.position = new Vector3(4f, 0.5f, 4f);
            ore.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            var pickup = ore.AddComponent<ResourcePickup>();
            pickup.itemId = "iron_ore";
            pickup.amount = 1;
            pickup.prompt = "Tekan E untuk menambang Bijih Besi";
        }

        // Contoh: Altar Segel
        if (!GameObject.Find("Seal_Altar"))
        {
            var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "Seal_Altar";
            altar.transform.SetParent(itemsRoot.transform);
            altar.transform.position = new Vector3(0f, 0.25f, 12f);
            altar.transform.localScale = new Vector3(3f, 0.5f, 3f);
            altar.AddComponent<SealAltar>();
        }

        // 7. Setup Canvas & UI
        GameObject canvasGo = GameObject.Find("HUD Canvas");
        if (!canvasGo)
        {
            canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Jam / Clock UI
        var clockGo = GameObject.Find("ClockText");
        if (!clockGo)
        {
            clockGo = new GameObject("ClockText");
            clockGo.transform.SetParent(canvasGo.transform, false);
            var clockText = clockGo.AddComponent<TextMeshProUGUI>();
            clockText.text = "Hari 1  07:00 (Siang)";
            clockText.fontSize = 24;
            clockText.alignment = TextAlignmentOptions.Top;
            clockText.color = Color.white;
            var rect = clockText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(500, 50);

            var clockComp = clockGo.AddComponent<ClockUI>();
            var soClock = new SerializedObject(clockComp);
            soClock.FindProperty("timeText").objectReferenceValue = clockText;
            soClock.ApplyModifiedProperties();
        }

        // Tas / Inventory HUD
        var invGo = GameObject.Find("InventoryText");
        if (!invGo)
        {
            invGo = new GameObject("InventoryText");
            invGo.transform.SetParent(canvasGo.transform, false);
            var invText = invGo.AddComponent<TextMeshProUGUI>();
            invText.text = "Tas:\n(Kosong)";
            invText.fontSize = 18;
            invText.alignment = TextAlignmentOptions.TopLeft;
            invText.color = Color.yellow;
            var rect = invText.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(30, 0);
            rect.sizeDelta = new Vector2(250, 300);

            var invComp = invGo.AddComponent<InventoryHUD>();
            var soInv = new SerializedObject(invComp);
            soInv.FindProperty("text").objectReferenceValue = invText;
            soInv.ApplyModifiedProperties();
        }

        // Prompt Interaksi (Tekan E)
        var promptGo = GameObject.Find("InteractionPrompt");
        if (!promptGo)
        {
            promptGo = new GameObject("InteractionPrompt");
            promptGo.transform.SetParent(canvasGo.transform, false);
            var promptText = promptGo.AddComponent<TextMeshProUGUI>();
            promptText.text = "Tekan E";
            promptText.fontSize = 22;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
            var rect = promptText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.4f);
            rect.anchorMax = new Vector2(0.5f, 0.4f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 50);
            promptGo.SetActive(false);

            var soPlayerInt = new SerializedObject(playerInteractor);
            soPlayerInt.FindProperty("promptText").objectReferenceValue = promptText;
            soPlayerInt.ApplyModifiedProperties();
        }

        // Tandai scene telah diedit
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog(
            "Berhasil!",
            "Prototype Scene 'Legenda Desa Eldara' berhasil disetup!\n\n" +
            "Objek yang dibuat:\n" +
            "• Ground (Tanah luas)\n" +
            "• Player (WASD jalan, Shift lari, V ganti kamera, Klik Kiri serang)\n" +
            "• CameraRig (Kamera 1st/3rd Person halus)\n" +
            "• Core Managers (DayNight, Inventory, GameManager)\n" +
            "• HUD UI (Waktu siang/malam, Tas Inventory, Prompt E)\n" +
            "• Objek contoh (Batu Bijih Besi & Altar Segel)\n\n" +
            "Silakan klik tombol ▶️ Play di Unity untuk mencoba!",
            "OK"
        );
    }
}
#endif

