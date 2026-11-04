using System.Text;
using TMPro;
using UnityEngine;

public class InventoryHUD : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    void Start()
    {
        if (Inventory.Instance) Inventory.Instance.OnChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (Inventory.Instance) Inventory.Instance.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (!text || !Inventory.Instance) return;
        var sb = new StringBuilder("Tas\n");
        foreach (var kv in Inventory.Instance.Snapshot())
            sb.AppendLine($"{kv.Key}: {kv.Value}");
        text.text = sb.ToString();
    }
}

