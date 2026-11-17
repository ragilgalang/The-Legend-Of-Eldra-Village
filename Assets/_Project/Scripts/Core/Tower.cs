using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] public float range = 26f;
    [SerializeField] public float damage = 25f;
    [SerializeField] public float interval = 1.35f;
    [SerializeField] public LayerMask enemyMask;
    [SerializeField] public Transform head;

    // Property aliases untuk kompatibilitas WatchtowerBuilder & sistem tempur
    public float attackRange { get => range; set => range = value; }
    public float fireInterval { get => interval; set => interval = value; }
    public float arrowDamage { get => damage; set => damage = value; }
    public float arrowSpeed = 34f;
    public Transform firePoint;
    public Transform turretHead { get => head; set => head = value; }

    float next;

    void Awake()
    {
        if (enemyMask == 0)
        {
            enemyMask = LayerMask.GetMask("Enemy");
            if (enemyMask == 0) enemyMask = ~0;
        }
    }

    void Update()
    {
        if (Time.time < next) return;

        Health best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(transform.position, range, enemyMask))
        {
            // Hanya serang musuh: jangan pernah menembak pemain, NPC, atau hewan
            if (!c.GetComponentInParent<EnemyAI>()) continue;
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

