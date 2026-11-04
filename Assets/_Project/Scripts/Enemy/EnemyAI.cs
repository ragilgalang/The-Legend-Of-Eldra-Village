using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] string enemyId = "goblin";
    [SerializeField] string[] targetTags = { "Player", "Defend" };
    [SerializeField] float attackRange = 2.2f, attackDamage = 8f, attackCooldown = 1.2f;
    [SerializeField] Animator anim;

    NavMeshAgent agent;
    Health health;
    Transform target;
    Vector3 homePoint;
    float nextAttack, nextRetarget;
    bool retreating, dead;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        health.OnDied += OnDied;
    }

    void Start()
    {
        homePoint = transform.position;                 // titik spawn di hutan
        agent.stoppingDistance = attackRange - 0.4f;
        DayNightCycle.Instance.OnPhaseChanged += OnPhase;
    }

    void OnDestroy()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged -= OnPhase;
    }

    void OnPhase(TimePhase p)
    {
        if (dead || (p != TimePhase.Dawn && p != TimePhase.Day)) return;
        retreating = true;
        agent.isStopped = false;
        agent.SetDestination(homePoint);
        Destroy(gameObject, 15f);                       // mundur ke hutan lalu hilang
    }

    void OnDied(Health h)
    {
        dead = true;
        agent.enabled = false;
        if (anim) anim.SetTrigger("Die");
        QuestManager.Instance?.Report(QuestObjective.Type.Kill, enemyId);
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        if (dead) return;
        if (anim && agent.enabled) anim.SetFloat("Speed", agent.velocity.magnitude);
        if (retreating) return;

        if (Time.time >= nextRetarget)
        {
            nextRetarget = Time.time + 0.5f;
            target = FindTarget();
        }
        if (!target) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange)
        {
            agent.isStopped = true;
            Vector3 look = target.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(look);

            if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + attackCooldown;
                if (anim) anim.SetTrigger("Attack");
                var h = target.GetComponentInParent<Health>();
                if (h) h.Damage(attackDamage);
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }

    Transform FindTarget()
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var tag in targetTags)
            foreach (var go in GameObject.FindGameObjectsWithTag(tag))
            {
                float d = (go.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { best = go.transform; bestDist = d; }
            }
        return best;
    }
}

