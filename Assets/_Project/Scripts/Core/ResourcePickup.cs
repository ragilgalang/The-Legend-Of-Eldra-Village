using UnityEngine;

public class ResourcePickup : Interactable
{
    public string itemId = "iron_ore";
    public int amount = 1;

    public override void Interact(PlayerController player)
    {
        Inventory.Instance.Add(itemId, amount);   // baris baru
        QuestManager.Instance.Report(QuestObjective.Type.Collect, itemId, amount);
        Destroy(gameObject);
    }
}

