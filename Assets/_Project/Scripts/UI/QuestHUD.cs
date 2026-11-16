using System.Text;
using TMPro;
using UnityEngine;

public class QuestHUD : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    string completionMessage;

    void Start()
    {
        if (QuestManager.Instance)
        {
            QuestManager.Instance.OnProgress += Refresh;
            QuestManager.Instance.OnQuestStarted += OnQuestStarted;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        }
        Refresh();
    }

    void OnDestroy()
    {
        if (!QuestManager.Instance) return;
        QuestManager.Instance.OnProgress -= Refresh;
        QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
        QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
    }

    void OnQuestStarted(QuestData quest)
    {
        completionMessage = null;
        Refresh();
    }

    void OnQuestCompleted(QuestData quest)
    {
        completionMessage = quest.questId == "level1_return_altar"
            ? "LEVEL 1 SELESAI: Segel Eldara telah tertutup."
            : $"Selesai: {quest.title}";
        Refresh();
    }

    void Refresh()
    {
        if (!text) return;
        if (!QuestManager.Instance || QuestManager.Instance.Current == null)
        {
            text.text = completionMessage ?? "";
            return;
        }

        var q = QuestManager.Instance.Current;
        var sb = new StringBuilder(q.title + "\n");
        if (!string.IsNullOrWhiteSpace(q.description))
            sb.AppendLine(q.description);
        foreach (var o in q.objectives)
            sb.AppendLine($"- {ObjectiveLabel(o)}: {o.current}/{o.requiredAmount}");
        text.text = sb.ToString();
    }

    static string ObjectiveLabel(QuestObjective objective)
    {
        if (objective.type == QuestObjective.Type.Kill)
        {
            if (objective.targetId == "raja_goblin") return "Kalahkan Raja Goblin";
        }
        else if (objective.type == QuestObjective.Type.Reach)
        {
            switch (objective.targetId)
            {
                case "activate_altar": return "Aktifkan altar segel";
                case "seal_altar": return "Tutup segel di altar";
            }
        }
        if (objective.type == QuestObjective.Type.Talk)
        {
            switch (objective.targetId)
            {
                case "kepala_desa": return "Temui Kepala Desa";
                case "pandai_besi": return "Temui Pandai Besi";
                case "petani": return "Temui Petani";
                case "dukun": return "Temui Dukun";
            }
        }
        else if (objective.type == QuestObjective.Type.Collect)
        {
            switch (objective.targetId)
            {
                case "wood": return "Kumpulkan balok kayu";
                case "iron_ore": return "Kumpulkan bijih besi";
                case "stone": return "Kumpulkan batu";
            }
        }

        return objective.targetId;
    }
}

