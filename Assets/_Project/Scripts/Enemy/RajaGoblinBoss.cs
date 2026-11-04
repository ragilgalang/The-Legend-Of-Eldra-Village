using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class RajaGoblinBoss : MonoBehaviour
{
    [Header("Serangan")]
    [SerializeField] float meleeRange = 3f, meleeDamage = 20f, meleeCooldown = 1.6f;

    [Header("Panggil Anak Buah")]
    [SerializeField] GameObject minionPrefab;
    [SerializeField] int minionsPerSummon = 3;
    [SerializeField] float summonCooldown = 12f;

    [SerializeField] Animator anim;

    NavMeshAgent agent;
    Health hp;
    Transform player;
    int phase = 1;
    float nextAttack, nextSummon;
    bool dead;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hp = GetComponent<Health>();
    }

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        agent.stoppingDistance = meleeRange - 0.5f;
        hp.OnChanged += OnHealth;
        hp.OnDied += OnDied;
    }

    void OnHealth(float cur, float max)
    {
        float r = cur / max;
        int p = r < 0.33f ? 3 : r < 0.66f ? 2 : 1;
        if (p == phase) return;
        phase = p;
        if (anim) anim.SetTrigger("Roar");

        if (phase == 2) Summon();
        if (phase == 3)
        {
            agent.speed *= 1.5f;
            meleeCooldown *= 0.6f;
        }
    }

    void Update()
    {
        if (dead || !player) return;
        if (anim) anim.SetFloat("Speed", agent.velocity.magnitude);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > meleeRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            Vector3 look = player.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);

            if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + meleeCooldown;
                if (anim) anim.SetTrigger("Attack");
                player.GetComponentInParent<Health>()?.Damage(meleeDamage);
            }
        }

        if (phase >= 2 && Time.time >= nextSummon) Summon();
    }

    void Summon()
    {
        nextSummon = Time.time + summonCooldown;
        if (!minionPrefab) return;

        for (int i = 0; i < minionsPerSummon; i++)
        {
            Vector3 p = transform.position + Random.insideUnitSphere * 4f;
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                Instantiate(minionPrefab, hit.position, Quaternion.identity);
        }
    }

    void OnDied(Health h)
    {
        dead = true;
        agent.enabled = false;
        if (anim) anim.SetTrigger("Die");
        GameManager.Instance.BossDefeated = true;
        QuestManager.Instance?.Report(QuestObjective.Type.Kill, "raja_goblin");
        Destroy(gameObject, 6f);
    }
}

