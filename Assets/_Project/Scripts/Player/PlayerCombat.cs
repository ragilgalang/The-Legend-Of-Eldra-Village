using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] float damage = 20f, reach = 1.4f, radius = 1.3f, cooldown = 0.5f;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] Animator anim;

    PlayerController player;
    float next;

    void Awake() => player = GetComponent<PlayerController>();

    void Update()
    {
        var ms = Mouse.current;
        if (ms == null || (player != null && player.inputLocked)) return;
        if (!ms.leftButton.wasPressedThisFrame || Time.time < next) return;

        next = Time.time + cooldown;
        if (anim) anim.SetTrigger("Attack");

        var hit = new HashSet<Health>();
        var center = transform.position + transform.forward * reach;
        var colliders = enemyMask.value == 0
            ? Physics.OverlapSphere(center, radius)
            : Physics.OverlapSphere(center, radius, enemyMask);
        foreach (var c in colliders)
        {
            var h = c.GetComponentInParent<Health>();
            if (!h) continue;

            bool layerMatches = (enemyMask.value & (1 << c.gameObject.layer)) != 0;
            bool hasEnemyComponent = c.GetComponentInParent<EnemyAI>() ||
                                     c.GetComponentInParent<RajaGoblinBoss>() ||
                                     c.GetComponentInParent<LevelOneBossController>();
            if ((layerMatches || hasEnemyComponent) && hit.Add(h))
                h.Damage(damage);
        }
    }
}

