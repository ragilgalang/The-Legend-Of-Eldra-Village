using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] float range = 15f, damage = 12f, interval = 1.5f;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] Transform head;

    float next;

    void Update()
    {
        if (Time.time < next) return;

        Health best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(transform.position, range, enemyMask))
        {
            var h = c.GetComponentInParent<Health>();
            if (!h || h.IsDead) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { best = h; bestDist = d; }
        }
        if (!best) return;

        next = Time.time + interval;
        if (head) head.LookAt(best.transform.position);
        best.Damage(damage);
    }
}

