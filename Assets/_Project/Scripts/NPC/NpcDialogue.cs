using UnityEngine;

public class NpcDialogue : Interactable
{
    public string npcId = "mira";
    public DialogueData dialogue;
    public QuestData questOnFinish;   // opsional: quest yang dimulai setelah bicara

    void Reset() => prompt = "Tekan E untuk berbicara";

    public override void Interact(PlayerController player)
    {
        var resolvedNpcId = ResolveNpcId();
        if (!dialogue || !DialogueManager.Instance)
        {
            var problem = !dialogue
                ? $"Dialog untuk NPC '{gameObject.name}' belum diassign."
                : "DialogueManager tidak ditemukan.";
            Debug.LogError($"{problem} Interaksi diteruskan agar quest tidak macet.", this);
            if (HintManager.Instance)
                HintManager.Instance.Show("missing_dialogue_" + resolvedNpcId, problem);
            CompleteInteraction(resolvedNpcId);
            return;
        }

        DialogueManager.Instance.Play(dialogue, () => CompleteInteraction(resolvedNpcId));
    }

    void CompleteInteraction(string resolvedNpcId)
    {
        if (!QuestManager.Instance)
        {
            Debug.LogError("QuestManager tidak ditemukan; objective bicara NPC tidak dapat diperbarui.", this);
            return;
        }

        QuestManager.Instance.Report(QuestObjective.Type.Talk, resolvedNpcId);
        if (questOnFinish) QuestManager.Instance.StartQuest(questOnFinish);
    }

    string ResolveNpcId()
    {
        if (npcId != "mira") return npcId;

        switch (gameObject.name)
        {
            case "NPC_KepalaDesa": return "kepala_desa";
            case "NPC_PandaiBesi": return "pandai_besi";
            case "NPC_Petani": return "petani";
            case "NPC_Dukun": return "dukun";
            default: return npcId;
        }
    }
}

