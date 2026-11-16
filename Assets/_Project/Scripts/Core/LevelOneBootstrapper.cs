using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class LevelOneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsurePlayableLevel()
    {
        var questManager = Object.FindAnyObjectByType<QuestManager>();
        var existingPlayer = Object.FindAnyObjectByType<PlayerController>();
        if (questManager != null && existingPlayer != null)
        {
            EnsureStoryQuest(questManager);
            return;
        }

        BuildGround();
        var player = EnsurePlayer();
        EnsureCamera(player);
        EnsureManagers(player);
        EnsureQuestHud();
        EnsurePickupZone();
    }

    static void EnsureStoryQuest(QuestManager questManager)
    {
        var field = questManager.GetType().GetField("firstQuest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || field.GetValue(questManager) != null)
            return;

        var meetElder = CreateQuest(
            "prologue_meet_elder",
            "Peringatan di Balai Desa",
            "Desa Eldara terancam serangan goblin saat malam. Temui Kepala Desa untuk mengetahui apa yang harus dilakukan.",
            new QuestObjective { type = QuestObjective.Type.Talk, targetId = "kepala_desa" });
        var gatherSupplies = CreateQuest(
            "prologue_gather_supplies",
            "Persiapan Menjelang Senja",
            "Pandai besi membutuhkan tiga balok kayu dan satu bijih besi untuk memperkuat pertahanan desa.",
            new QuestObjective { type = QuestObjective.Type.Collect, targetId = "wood", requiredAmount = 3 },
            new QuestObjective { type = QuestObjective.Type.Collect, targetId = "iron_ore", requiredAmount = 1 });
        var meetSmith = CreateQuest(
            "prologue_meet_smith",
            "Bawa Kabar ke Pandai Besi",
            "Bicaralah dengan Pandai Besi setelah mengumpulkan bahan untuk pertahanan.",
            new QuestObjective { type = QuestObjective.Type.Talk, targetId = "pandai_besi" });
        var seekShaman = CreateQuest(
            "prologue_seek_shaman",
            "Rahasia di Balik Serangan",
            "Kepala Desa mencurigai serangan ini berkaitan dengan kutukan lama. Temui Dukun untuk mencari tahu.",
            new QuestObjective { type = QuestObjective.Type.Talk, targetId = "dukun" });
        var reachAltar = CreateQuest(
            "level1_summon_boss",
            "Bangunkan Segel Kuno",
            "Dukun mengatakan Raja Goblin bersembunyi di balik kutukan. Pergi ke altar batu di utara desa dan aktifkan segelnya.",
            new QuestObjective { type = QuestObjective.Type.Reach, targetId = "activate_altar" });
        var defeatBoss = CreateQuest(
            "level1_defeat_boss",
            "Raja Goblin Terbangun",
            "Raja Goblin telah terpanggil. Kalahkan dia di lingkaran altar sebelum ia menghancurkan desa.",
            new QuestObjective { type = QuestObjective.Type.Kill, targetId = "raja_goblin" });
        var returnToAltar = CreateQuest(
            "level1_return_altar",
            "Segel Kembali Kegelapan",
            "Raja Goblin menjatuhkan tiga pecahan segel. Bawa semuanya ke altar dan tekan E untuk menutup segel.",
            new QuestObjective { type = QuestObjective.Type.Reach, targetId = "seal_altar" });

        meetElder.nextQuest = gatherSupplies;
        gatherSupplies.nextQuest = meetSmith;
        meetSmith.nextQuest = seekShaman;
        seekShaman.nextQuest = reachAltar;
        reachAltar.nextQuest = defeatBoss;
        defeatBoss.nextQuest = returnToAltar;
        field.SetValue(questManager, meetElder);
    }

    static QuestData CreateQuest(string id, string title, string description, params QuestObjective[] objectives)
    {
        var quest = ScriptableObject.CreateInstance<QuestData>();
        quest.name = id;
        quest.questId = id;
        quest.title = title;
        quest.description = description;
        quest.objectives = objectives;
        return quest;
    }

    static void BuildGround()
    {
        var ground = GameObject.Find("Ground");
        if (!ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
        }

        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(12f, 1f, 12f);

        var renderer = ground.GetComponent<Renderer>();
        if (renderer && renderer.sharedMaterial == null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.38f, 0.68f, 0.32f, 1f);
            renderer.sharedMaterial = mat;
        }
    }

    static GameObject EnsurePlayer()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (!playerGo)
        {
            playerGo = new GameObject("Player");
            playerGo.tag = "Player";
        }

        var cc = playerGo.GetComponent<CharacterController>();
        if (!cc)
        {
            cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
        }

        if (!playerGo.GetComponent<PlayerController>()) playerGo.AddComponent<PlayerController>();
        if (!playerGo.GetComponent<Health>()) playerGo.AddComponent<Health>();
        if (!playerGo.GetComponent<PlayerInteractor>()) playerGo.AddComponent<PlayerInteractor>();

        playerGo.transform.position = new Vector3(0f, 0.25f, 0f);

        var body = playerGo.transform.Find("Body");
        if (!body)
        {
            var bodyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyGo.name = "Body";
            bodyGo.transform.SetParent(playerGo.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var c = bodyGo.GetComponent<Collider>();
            if (c) Object.Destroy(c);
        }

        return playerGo;
    }

    static void EnsureCamera(GameObject player)
    {
        var camObj = Camera.main ? Camera.main.gameObject : new GameObject("Main Camera");
        camObj.name = "Main Camera";
        camObj.tag = "MainCamera";

        if (!camObj.GetComponent<Camera>()) camObj.AddComponent<Camera>();
        if (!camObj.GetComponent<AudioListener>()) camObj.AddComponent<AudioListener>();

        var rig = camObj.GetComponent<CameraRig>();
        if (!rig) rig = camObj.AddComponent<CameraRig>();

        SetField(rig, "target", player.transform);
        SetField(rig, "playerRenderers", player.GetComponentsInChildren<Renderer>());
        camObj.transform.position = new Vector3(0f, 2.5f, -8f);
        camObj.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
    }

    static void EnsureManagers(GameObject player)
    {
        var managerRoot = new GameObject("RuntimeManagers");
        var inventoryObj = new GameObject("Inventory");
        inventoryObj.transform.SetParent(managerRoot.transform, false);
        if (!inventoryObj.GetComponent<Inventory>()) inventoryObj.AddComponent<Inventory>();

        var gameManagerObj = new GameObject("GameManager");
        gameManagerObj.transform.SetParent(managerRoot.transform, false);
        var gameManager = gameManagerObj.GetComponent<GameManager>();
        if (!gameManager) gameManager = gameManagerObj.AddComponent<GameManager>();
        SetField(gameManager, "player", player.GetComponent<PlayerController>());
        SetField(gameManager, "playerHealth", player.GetComponent<Health>());

        var questObj = new GameObject("QuestManager");
        questObj.transform.SetParent(managerRoot.transform, false);
        var questManager = questObj.GetComponent<QuestManager>();
        if (!questManager) questManager = questObj.AddComponent<QuestManager>();

        var quest = ScriptableObject.CreateInstance<QuestData>();
        quest.name = "Q_Level1_Wood";
        quest.questId = "wood_quest";
        quest.title = "Bersiaplah Menjelang Malam";
        quest.description = "Kumpulkan kayu untuk menyiapkan desa sebelum malam tiba.";
        quest.objectives = new[]
        {
            new QuestObjective { type = QuestObjective.Type.Collect, targetId = "wood_log", requiredAmount = 3 }
        };

        SetField(questManager, "firstQuest", quest);

        var lightObj = GameObject.Find("Directional Light");
        if (!lightObj)
        {
            lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        var dncObj = GameObject.Find("DayNightCycle");
        if (!dncObj)
        {
            dncObj = new GameObject("DayNightCycle");
            dncObj.transform.SetParent(managerRoot.transform, false);
        }

        var dnc = dncObj.GetComponent<DayNightCycle>();
        if (!dnc) dnc = dncObj.AddComponent<DayNightCycle>();
        SetField(dnc, "sun", lightObj.GetComponent<Light>());
        SetField(dnc, "followTarget", player.transform);

        var musicObj = GameObject.Find("MusicManager");
        if (!musicObj)
        {
            musicObj = new GameObject("MusicManager");
            musicObj.transform.SetParent(managerRoot.transform, false);
            musicObj.AddComponent<MusicManager>();
        }
    }

    static void EnsureQuestHud()
    {
        var canvasObj = GameObject.Find("LevelOneCanvas");
        if (!canvasObj)
        {
            canvasObj = new GameObject("LevelOneCanvas");
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var textObj = canvasObj.transform.Find("QuestText")?.gameObject;
        if (!textObj)
        {
            textObj = new GameObject("QuestText");
            textObj.transform.SetParent(canvasObj.transform, false);
        }

        var text = textObj.GetComponent<TextMeshProUGUI>();
        if (!text) text = textObj.AddComponent<TextMeshProUGUI>();

        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.8f);
        rect.anchorMax = new Vector2(0.35f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;

        var hud = textObj.GetComponent<QuestHUD>();
        if (!hud) hud = textObj.AddComponent<QuestHUD>();
        SetField(hud, "text", text);
    }

    static void EnsurePickupZone()
    {
        Vector3[] positions =
        {
            new Vector3(3f, 0.5f, 3f),
            new Vector3(-3f, 0.5f, 4f),
            new Vector3(5f, 0.5f, -2f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            var obj = GameObject.Find("WoodPickup_" + i);
            if (!obj)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = "WoodPickup_" + i;
                obj.transform.position = positions[i];
                obj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                var renderer = obj.GetComponent<Renderer>();
                if (renderer)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = new Color(0.59f, 0.39f, 0.16f, 1f);
                }

                var pickup = obj.AddComponent<ResourcePickup>();
                pickup.itemId = "wood_log";
                pickup.amount = 1;
            }
        }
    }

    static void SetField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        var prop = target.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite)
            prop.SetValue(target, value);
    }
}
