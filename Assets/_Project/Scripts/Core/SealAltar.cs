using UnityEngine;

public class SealAltar : Interactable
{
    [SerializeField] string[] shardIds = { "shard_mine", "shard_lake", "shard_ruins" };
    [SerializeField] GameObject sealEffect;
    [SerializeField] float endingDelay = 4f;

    bool sealed_;

    void Awake() => prompt = "Tekan E untuk berinteraksi dengan altar";

    public override void Interact(PlayerController player)
    {
        if (sealed_) return;

        var gameManager = GameManager.Instance;
        if (!gameManager)
        {
            Debug.LogError("GameManager tidak ditemukan; altar tidak dapat memulai encounter.");
            return;
        }

        var questManager = QuestManager.Instance;
        if (!gameManager.BossDefeated)
        {
            if (gameManager.BossEncounterActive)
            {
                ShowHint("altar_boss_active", "Raja Goblin telah bangkit. Kalahkan dia untuk memperoleh tiga pecahan segel.");
                return;
            }

            if (!questManager || questManager.Current == null ||
                questManager.Current.questId != "level1_summon_boss")
            {
                ShowHint("altar_not_ready", "Dukun belum menyuruhmu mengaktifkan altar. Selesaikan persiapan desa dan temui Dukun terlebih dahulu.");
                return;
            }

            if (!gameManager.StartBossEncounter())
            {
                ShowHint("altar_encounter_error", "Altar tidak dapat membangunkan musuh.");
                return;
            }

            questManager.Report(QuestObjective.Type.Reach, "activate_altar");
            return;
        }

        if (!questManager || questManager.Current == null ||
            questManager.Current.questId != "level1_return_altar")
        {
            ShowHint("altar_return_not_ready", "Segel sudah siap ditutup setelah tugas mengalahkan Raja Goblin tercatat.");
            return;
        }

        var inventory = Inventory.Instance;
        if (!inventory)
        {
            Debug.LogError("Inventory tidak ditemukan; pecahan segel tidak dapat diperiksa.");
            return;
        }

        foreach (var id in shardIds)
        {
            if (inventory.Has(id)) continue;
            ShowHint("altar_need_shards", "Kumpulkan tiga pecahan segel dari Raja Goblin sebelum menutup altar.");
            return;
        }

        foreach (var id in shardIds)
            inventory.Remove(id);

        sealed_ = true;
        if (sealEffect) sealEffect.SetActive(true);
        Invoke(nameof(Finish), endingDelay);
    }

    void Finish()
    {
        if (QuestManager.Instance)
            QuestManager.Instance.Report(QuestObjective.Type.Reach, "seal_altar");
        else
            Debug.LogError("QuestManager tidak ditemukan; objective menyegel altar tidak dapat diperbarui.");

        if (GameManager.Instance)
            GameManager.Instance.TriggerEnding();
        else
            Debug.LogError("GameManager tidak ditemukan; ending Level 1 tidak dapat ditampilkan.");
    }

    static void ShowHint(string id, string message)
    {
        if (HintManager.Instance)
            HintManager.Instance.Show(id, message);
        else
            Debug.LogWarning(message);
    }
}
