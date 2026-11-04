using UnityEngine;

public class NpcDialogue : Interactable
{
    public string npcId = "mira";
    public DialogueData dialogue;
    public QuestData questOnFinish;   // opsional: quest yang dimulai setelah bicara

    void Reset() => prompt = "Tekan E untuk berbicara";

    public override void Interact(PlayerController player)
    {
        DialogueManager.Instance.Play(dialogue, () =>
        {
            QuestManager.Instance.Report(QuestObjective.Type.Talk, npcId);
            if (questOnFinish) QuestManager.Instance.StartQuest(questOnFinish);
        });
    }
}

