using System.Text;
using TMPro;
using UnityEngine;

public class QuestHUD : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    void Start()
    {
        if (QuestManager.Instance)
        {
            QuestManager.Instance.OnProgress += Refresh;
            QuestManager.Instance.OnQuestCompleted += _ => Refresh();
        }
        Refresh();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance) QuestManager.Instance.OnProgress -= Refresh;
    }

    void Refresh()
    {
        if (!text) return;
        if (!QuestManager.Instance || QuestManager.Instance.Current == null)
        {
            text.text = "";
            return;
        }

        var q = QuestManager.Instance.Current;
        var sb = new StringBuilder(q.title + "\n");
        foreach (var o in q.objectives)
            sb.AppendLine($"- {o.targetId}: {o.current}/{o.requiredAmount}");
        text.text = sb.ToString();
    }
}

