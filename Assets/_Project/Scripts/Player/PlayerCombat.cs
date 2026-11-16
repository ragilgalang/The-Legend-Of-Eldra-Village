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
        foreach (var c in Physics.OverlapSphere(transform.position + transform.forward * reach, radius, enemyMask))
        {
            var h = c.GetComponentInParent<Health>();
            if (h && hit.Add(h)) h.Damage(damage);
        }
    }
}

