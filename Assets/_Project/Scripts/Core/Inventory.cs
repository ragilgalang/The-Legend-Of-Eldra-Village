using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }
    public event Action OnChanged;

    readonly Dictionary<string, int> items = new();

    void Awake() => Instance = this;

    public int Count(string id) => items.TryGetValue(id, out var n) ? n : 0;
    public bool Has(string id, int n = 1) => Count(id) >= n;

    public void Add(string id, int n = 1)
    {
        items[id] = Count(id) + n;
        OnChanged?.Invoke();
    }

    public bool Remove(string id, int n = 1)
    {
        if (!Has(id, n)) return false;
        items[id] -= n;
        if (items[id] <= 0) items.Remove(id);
        OnChanged?.Invoke();
        return true;
    }

    public Dictionary<string, int> Snapshot() => new(items);

    public void Load(Dictionary<string, int> data)
    {
        items.Clear();
        foreach (var kv in data) items[kv.Key] = kv.Value;
        OnChanged?.Invoke();
    }
}
