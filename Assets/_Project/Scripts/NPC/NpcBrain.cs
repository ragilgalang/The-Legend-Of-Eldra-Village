using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcBrain : MonoBehaviour
{
    [SerializeField] Transform workSpot, homeDoor;
    [SerializeField] GameObject visual;      // anak berisi model + collider interaksi
    [SerializeField] Animator anim;

    NavMeshAgent agent;
    bool goingHome;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void Start()
    {
        if (DayNightCycle.Instance)
        {
            DayNightCycle.Instance.OnPhaseChanged += HandlePhase;
            HandlePhase(DayNightCycle.Instance.Phase);
        }
    }

    void OnDestroy()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged -= HandlePhase;
    }

    void HandlePhase(TimePhase phase)
    {
        if (!agent) return;

        switch (phase)
        {
            case TimePhase.Day:
            case TimePhase.Dawn:
                SetHidden(false);
                if (agent.isOnNavMesh)
                {
                    if (homeDoor) agent.Warp(homeDoor.position);
                    if (workSpot) agent.SetDestination(workSpot.position);
                }
                goingHome = false;
                break;
            case TimePhase.Dusk:
                if (agent.enabled && agent.isOnNavMesh && homeDoor)
                {
                    agent.SetDestination(homeDoor.position);
                    goingHome = true;
                }
                break;
            case TimePhase.Night:
                SetHidden(true);
                break;
        }
    }

    void Update()
    {
        if (!agent || !agent.enabled || !agent.isOnNavMesh) return;

        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
        if (anim)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
            anim.SetBool("Working", arrived && !goingHome);
        }
        if (goingHome && arrived) { SetHidden(true); goingHome = false; }
    }

    void SetHidden(bool hidden)
    {
        if (visual) visual.SetActive(!hidden);
        if (agent && agent.isOnNavMesh) agent.enabled = !hidden;
    }
}
