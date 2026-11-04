using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [SerializeField] float damage = 25f;
    [SerializeField] int uses = 5;

    void OnTriggerEnter(Collider other)
    {
        var h = other.GetComponentInParent<Health>();
        if (!h || !other.GetComponentInParent<EnemyAI>()) return;

        h.Damage(damage);
        if (--uses <= 0) Destroy(gameObject);
    }
}

