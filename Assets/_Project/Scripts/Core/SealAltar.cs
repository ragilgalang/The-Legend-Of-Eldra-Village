using UnityEngine;

public class SealAltar : Interactable
{
    [SerializeField] string[] shardIds = { "shard_mine", "shard_lake", "shard_ruins" };
    [SerializeField] GameObject sealEffect;
    [SerializeField] DialogueData needBossDialogue, needShardsDialogue;
    [SerializeField] float endingDelay = 4f;

    bool sealed_;

    void Reset() => prompt = "Tekan E untuk memasang pecahan";

    public override void Interact(PlayerController player)
    {
        if (sealed_) return;

        if (!GameManager.Instance.BossDefeated)
        {
            DialogueManager.Instance.Play(needBossDialogue);
            return;
        }
        foreach (var id in shardIds)
            if (!Inventory.Instance.Has(id))
            {
                DialogueManager.Instance.Play(needShardsDialogue);
                return;
            }

        foreach (var id in shardIds) Inventory.Instance.Remove(id);
        sealed_ = true;
        if (sealEffect) sealEffect.SetActive(true);
        Invoke(nameof(Finish), endingDelay);
    }

    void Finish() => GameManager.Instance.TriggerEnding();
}

