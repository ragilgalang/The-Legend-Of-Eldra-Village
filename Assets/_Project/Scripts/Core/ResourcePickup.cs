using UnityEngine;

public class ResourcePickup : Interactable
{
    public string itemId = "iron_ore";
    public int amount = 1;

    public override void Interact(PlayerController player)
    {
        QuestManager.Instance.Report(QuestObjective.Type.Collect, itemId, amount);
        Destroy(gameObject);
    }
}

