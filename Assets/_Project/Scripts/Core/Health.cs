using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float max = 100f;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public event Action<float, float> OnChanged; // current, max
    public event Action<Health> OnDied;

    void Awake() => Current = max;

    public void Damage(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0f, Current - amount);
        OnChanged?.Invoke(Current, max);
        if (IsDead) OnDied?.Invoke(this);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(max, Current + amount);
        OnChanged?.Invoke(Current, max);
    }
}

