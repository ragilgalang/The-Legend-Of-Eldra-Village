using UnityEngine;
using UnityEngine.AI;
using Eldara.AI.BehaviourTree;

/// <summary>
/// Pengendali Kecerdasan Buatan NPC berbasis Metode Behaviour Tree.
/// Mengatur pengambilan keputusan cerdas:
/// Prioritas 1: Evaluasi & Berlindung saat Malam Hari (Kutukan Monster)
/// Prioritas 2: Menyapa & Menghadap Pemain saat Pemain Mendekat
/// Prioritas 3: Menjalankan Rutin Kerja di Tempat Tugas (Workstation)
/// Prioritas 4: Berkeliling / Istirahat Santai di Desa
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NpcBehaviourTree : MonoBehaviour
{
    [Header("Lokasi Tugas & Rumah")]
    [SerializeField] Transform workSpot;
    [SerializeField] Transform homeDoor;
    [SerializeField] GameObject visualRoot;

    [Header("Pengaturan Interaksi Cerdas")]
    [SerializeField] float playerDetectRadius = 3.5f;
    [SerializeField] float turnSpeed = 6f;

    [Header("Status Behaviour Tree (Debug)")]
    [SerializeField] string currentActiveBranch = "Idle";

    private NavMeshAgent agent;
    private Animator anim;
    private Transform playerTransform;
    private BTNode rootNode;
    private bool isHidden;

    public void SetLocations(Transform work, Transform home, GameObject visual = null)
    {
        workSpot = work;
        homeDoor = home;
        if (visual) visualRoot = visual;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        if (!visualRoot)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r) visualRoot = r.gameObject;
        }
    }

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj) playerTransform = playerObj.transform;

        BuildBehaviourTree();
    }

    /// <summary>
    /// Membangun Arsitektur Hierarki Behaviour Tree untuk NPC.
    /// </summary>
    void BuildBehaviourTree()
    {
        // ── CABANG 1: EVAKUASI & PERLINDUNGAN MALAM HARI (PRIORITAS TERTINGGI) ──
        var nightSequence = new BTSequence(
            new BTCondition(CheckIsNightOrDusk),
            new BTAction(ActionMoveToHome),
            new BTAction(ActionHideInsideSafeHouse)
        );

        // ── CABANG 2: INTERAKSI & MENYAPA PEMAIN ──
        var playerSocialSequence = new BTSequence(
            new BTCondition(CheckIsPlayerNearby),
            new BTAction(ActionFaceAndGreetPlayer)
        );

        // ── CABANG 3: BEKERJA DI STASIUN TUGAS ──
        var workRoutineSequence = new BTSequence(
            new BTCondition(CheckHasWorkSpot),
            new BTAction(ActionMoveToWorkSpot),
            new BTAction(ActionPerformWork)
        );

        // ── CABANG 4: BERKELILING / IDLE SANTAI DI DESA ──
        var idleAction = new BTAction(ActionIdleAround);

        // ── ROOT NODE (SELECTOR): Mengevaluasi prioritas dari atas ke bawah ──
        rootNode = new BTSelector(
            nightSequence,
            playerSocialSequence,
            workRoutineSequence,
            idleAction
        );
    }

    void Update()
    {
        if (rootNode != null)
        {
            rootNode.Evaluate();
        }

        // Sinkronisasi Animasi dengan Kecepatan NavMesh
        if (anim && agent && agent.enabled)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  KONDISI LOGIKA (CONDITIONS)
    // ─────────────────────────────────────────────────────────────────────
    bool CheckIsNightOrDusk()
    {
        if (!DayNightCycle.Instance) return false;
        var p = DayNightCycle.Instance.Phase;
        return p == TimePhase.Night || p == TimePhase.Dusk;
    }

    bool CheckIsPlayerNearby()
    {
        if (!playerTransform || isHidden) return false;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        return dist <= playerDetectRadius;
    }

    bool CheckHasWorkSpot()
    {
        return workSpot != null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  AKSI NYATA (ACTIONS)
    // ─────────────────────────────────────────────────────────────────────
    NodeState ActionMoveToHome()
    {
        currentActiveBranch = "Evakuasi ke Rumah (Malam)";

        if (isHidden) return NodeState.SUCCESS;

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
        {
            SetNpcHidden(true);
            return NodeState.SUCCESS;
        }

        if (homeDoor)
        {
            float dist = Vector3.Distance(transform.position, homeDoor.position);
            if (dist > 1.2f)
            {
                agent.isStopped = false;
                agent.SetDestination(homeDoor.position);
                return NodeState.RUNNING;
            }
        }

        SetNpcHidden(true);
        return NodeState.SUCCESS;
    }

    NodeState ActionHideInsideSafeHouse()
    {
        currentActiveBranch = "Bersembunyi Aman di Dalam Rumah";
        SetNpcHidden(true);
        return NodeState.SUCCESS;
    }

    NodeState ActionFaceAndGreetPlayer()
    {
        currentActiveBranch = "Menyapa & Menghadap Pemain";
        SetNpcHidden(false);

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (playerTransform)
        {
            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        if (anim) anim.SetBool("Talking", true);

        return NodeState.SUCCESS;
    }

    NodeState ActionMoveToWorkSpot()
    {
        currentActiveBranch = "Berjalan Menuju Lokasi Kerja";
        SetNpcHidden(false);

        if (!workSpot) return NodeState.FAILURE;

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
        {
            return NodeState.SUCCESS;
        }

        float dist = Vector3.Distance(transform.position, workSpot.position);
        if (dist > 1.2f)
        {
            agent.isStopped = false;
            agent.SetDestination(workSpot.position);
            return NodeState.RUNNING;
        }

        return NodeState.SUCCESS;
    }

    NodeState ActionPerformWork()
    {
        currentActiveBranch = "Sedang Bekerja / Beraktivitas";
        SetNpcHidden(false);

        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (anim) anim.SetBool("Working", true);

        return NodeState.SUCCESS;
    }

    NodeState ActionIdleAround()
    {
        currentActiveBranch = "Istirahat / Berkeliling Santai";
        SetNpcHidden(false);

        if (anim)
        {
            anim.SetBool("Talking", false);
            anim.SetBool("Working", false);
        }

        return NodeState.SUCCESS;
    }

    void SetNpcHidden(bool hide)
    {
        if (isHidden == hide) return;
        isHidden = hide;

        if (visualRoot) visualRoot.SetActive(!hide);
        if (agent && agent.isOnNavMesh) agent.enabled = !hide;
    }
}

