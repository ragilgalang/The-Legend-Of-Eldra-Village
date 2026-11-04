using System;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public QuestData Current { get; private set; }
    public event Action<QuestData> OnQuestStarted, OnQuestCompleted;
    public event Action OnProgress;

    [SerializeField] QuestData firstQuest;   // opsional: quest otomatis di awal

    void Awake() => Instance = this;
    void Start() { if (firstQuest) StartQuest(firstQuest); }

    public void StartQuest(QuestData q)
    {
        if (!q) return;
        foreach (var o in q.objectives) o.current = 0;
        Current = q;
        OnQuestStarted?.Invoke(q);
        OnProgress?.Invoke();
    }

    public void Report(QuestObjective.Type type, string id, int amount = 1)
    {
        if (Current == null) return;
        bool changed = false;
        foreach (var o in Current.objectives)
        {
            if (o.type == type && o.targetId == id && o.current < o.requiredAmount)
            {
                o.current = Mathf.Min(o.current + amount, o.requiredAmount);
                changed = true;
            }
        }
        if (!changed) return;

        OnProgress?.Invoke();
        if (AllDone()) Complete();
    }

    bool AllDone()
    {
        foreach (var o in Current.objectives)
            if (o.current < o.requiredAmount) return false;
        return true;
    }

    void Complete()
    {
        var done = Current;
        Current = null;
        OnQuestCompleted?.Invoke(done);
        if (done.nextQuest) StartQuest(done.nextQuest);
    }

    public void Refresh() => OnProgress?.Invoke();
}

