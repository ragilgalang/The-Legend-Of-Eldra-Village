using UnityEngine;

[CreateAssetMenu(menuName = "Eldara/Quest")]
public class QuestData : ScriptableObject
{
    public string questId, title, description;
    public QuestObjective[] objectives;   // Collect, Kill, Reach, Talk
    public QuestData nextQuest;
    public string startDialogueId;
}

[System.Serializable]
public class QuestObjective
{
    public enum Type { Collect, Kill, Reach, Talk, Survive }
    public Type type;
    public string targetId;
    public int requiredAmount = 1;
    [HideInInspector] public int current;
}

