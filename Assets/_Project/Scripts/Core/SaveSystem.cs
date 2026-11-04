using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class SaveData
{
    public int day;
    public float hour;
    public Vector3 playerPos;
    public float playerHealth;
    public List<string> itemIds = new();
    public List<int> itemCounts = new();
    public string questId;
    public List<int> questProgress = new();
    public bool bossDefeated;
}

public class SaveSystem : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Health playerHealth;
    [SerializeField] QuestData[] allQuests;   // seret semua QuestData ke sini

    string Path_ => Path.Combine(Application.persistentDataPath, "eldara_save.json");

    void Start()
    {
        if (PlayerPrefs.GetInt("LoadSave", 0) == 1)
        {
            PlayerPrefs.SetInt("LoadSave", 0);
            Load();
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f5Key.wasPressedThisFrame) Save();
        if (kb.f9Key.wasPressedThisFrame) Load();
    }

    public void Save()
    {
        var d = new SaveData
        {
            day = DayNightCycle.Instance.DayCount,
            hour = DayNightCycle.Instance.Hour,
            playerPos = player.transform.position,
            playerHealth = playerHealth.Current,
            bossDefeated = GameManager.Instance.BossDefeated
        };

        foreach (var kv in Inventory.Instance.Snapshot())
        {
            d.itemIds.Add(kv.Key);
            d.itemCounts.Add(kv.Value);
        }

        var q = QuestManager.Instance.Current;
        if (q != null)
        {
            d.questId = q.questId;
            foreach (var o in q.objectives) d.questProgress.Add(o.current);
        }

        File.WriteAllText(Path_, JsonUtility.ToJson(d, true));
        Debug.Log("Game tersimpan: " + Path_);
    }

    public void Load()
    {
        if (!File.Exists(Path_)) { Debug.Log("Belum ada save."); return; }
        var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path_));

        // Posisi: CharacterController harus dimatikan sementara
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = d.playerPos;
        cc.enabled = true;

        playerHealth.Heal(d.playerHealth - playerHealth.Current);
        DayNightCycle.Instance.SetTime(d.hour, d.day);
        GameManager.Instance.BossDefeated = d.bossDefeated;

        var inv = new Dictionary<string, int>();
        for (int i = 0; i < d.itemIds.Count; i++) inv[d.itemIds[i]] = d.itemCounts[i];
        Inventory.Instance.Load(inv);

        foreach (var q in allQuests)
        {
            if (q.questId != d.questId) continue;
            QuestManager.Instance.StartQuest(q);
            for (int i = 0; i < q.objectives.Length && i < d.questProgress.Count; i++)
                q.objectives[i].current = d.questProgress[i];
            QuestManager.Instance.Refresh(); // segarkan HUD
            break;
        }
    }
}

