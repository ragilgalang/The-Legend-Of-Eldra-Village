using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontroler AI Hewan untuk Desa Eldara (Anjing, Kucing, Serigala).
/// Mendukung:
/// - AI Patroli/Wandering organik di sekitar area asal
/// - Animasi gerak (Animator / Animation clip jika tersedia, dengan procedural secondary motion)
/// - Menengok ke arah pemain saat berada di dekatnya
/// - Interaksi pemain [E] Elus Hewan dengan respon animasi ceria & pesan di layar
/// - Mode Companion: Anjing akan mengikuti pemain setelah dielus!
/// </summary>
public class AnimalController : Interactable
{
    public enum AnimalType { Dog, Cat, Wolf, Fish }

    [Header("Identitas Hewan")]
    public AnimalType animalType = AnimalType.Dog;
    public string animalName = "Milo";

    [Header("Parameter Gerak")]
    public float wanderRadius = 6.0f;
    public float moveSpeed = 1.8f;
    public float idleTimeMin = 2.5f;
    public float idleTimeMax = 5.5f;

    [Header("Interaksi")]
    [TextArea(2, 3)]
    public string petMessage = "Guk! Milo mengibaskan ekornya riang dan tampak sangat senang!";
    public bool canFollowPlayer = true;
    public float companionDuration = 35.0f;

    // Komponen
    private NavMeshAgent navAgent;
    private Animator animator;
    private Animation legacyAnim;
    private Transform playerTransform;

    // AI State
    private enum State { Idle, Wandering, LookingAtPlayer, FollowingPlayer }
    private State currentState = State.Idle;

    private Vector3 homePosition;
    private Vector3 currentTarget;
    private float stateTimer;
    private float companionTimer;
    private Quaternion baseLocalRot;
    private Vector3 baseLocalScale;

    // Procedural animation
    private float stepCycle;
    private float breatheCycle;

    void Awake()
    {
        homePosition = transform.position;
        baseLocalRot = transform.localRotation;
        baseLocalScale = transform.localScale;

        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        legacyAnim = GetComponentInChildren<Animation>();

        // Set prompt interaksi default
        prompt = $"Tekan E untuk mengelus {animalName}";
    }

    void Start()
    {
        // Cari player
        var p = GameObject.FindWithTag("Player");
        if (p) playerTransform = p.transform;

        stateTimer = Random.Range(1.0f, 3.0f);
        TryPlayAnimation("idle");
    }

    void Update()
    {
        breatheCycle += Time.deltaTime * 2.2f;

        // Cek jarak ke player
        float distToPlayer = playerTransform ? Vector3.Distance(transform.position, playerTransform.position) : 999f;

        // Update Companion Mode jika sedang aktif
        if (currentState == State.FollowingPlayer)
        {
            UpdateCompanionState(distToPlayer);
        }
        else
        {
            UpdateWanderState(distToPlayer);
        }

        // Terapkan procedural motion lembut (napas & goyangan tubuh saat berjalan)
        ApplyProceduralMotion();
    }

    private void UpdateWanderState(float distToPlayer)
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                // Jika pemain sangat dekat (kurang dari 2.5m) dan hewan jinak, tengok ke arah pemain
                if (distToPlayer < 2.5f && (animalType == AnimalType.Dog || animalType == AnimalType.Cat))
                {
                    LookTowards(playerTransform.position, 3.0f);
                }

                if (stateTimer <= 0f)
                {
                    // Pilih tujuan baru
                    Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
                    currentTarget = homePosition + new Vector3(randCircle.x, 0, randCircle.y);

                    // Clamp ke tanah
                    if (Physics.Raycast(currentTarget + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                    {
                        currentTarget = hit.point;
                    }

                    if (navAgent && navAgent.isOnNavMesh)
                    {
                        navAgent.speed = moveSpeed;
                        navAgent.SetDestination(currentTarget);
                    }

                    currentState = State.Wandering;
                    stateTimer = Random.Range(4.0f, 8.0f);
                    TryPlayAnimation("walk");
                }
                break;

            case State.Wandering:
                // Gerak manual jika NavMesh belum di-bake
                if (!navAgent || !navAgent.isOnNavMesh)
                {
                    Vector3 dir = (currentTarget - transform.position);
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.2f)
                    {
                        LookTowards(currentTarget, 5.0f);
                        transform.position += transform.forward * (moveSpeed * Time.deltaTime);
                        stepCycle += Time.deltaTime * 8.0f;
                    }
                    else
                    {
                        // Sampai di tujuan
                        currentState = State.Idle;
                        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                        TryPlayAnimation("idle");
                    }
                }
                else
                {
                    stepCycle += Time.deltaTime * (navAgent.velocity.magnitude * 4.0f);
                    if (!navAgent.pathPending && navAgent.remainingDistance <= 0.4f)
                    {
                        currentState = State.Idle;
                        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                        TryPlayAnimation("idle");
                    }
                }

                if (stateTimer <= 0f)
                {
                    currentState = State.Idle;
                    stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                    TryPlayAnimation("idle");
                }
                break;
        }
    }

    private void UpdateCompanionState(float distToPlayer)
    {
        companionTimer -= Time.deltaTime;
        if (companionTimer <= 0f || !playerTransform)
        {
            // Companion mode selesai, kembali pulang
            currentState = State.Idle;
            stateTimer = 2.0f;
            TryPlayAnimation("idle");
            return;
        }

        // Ikuti pemain dengan jarak aman 2.2 - 3.2 meter
        if (distToPlayer > 3.0f)
        {
            Vector3 targetPos = playerTransform.position - playerTransform.forward * 2.0f;
            if (navAgent && navAgent.isOnNavMesh)
            {
                navAgent.speed = moveSpeed * 1.45f;
                navAgent.SetDestination(targetPos);
            }
            else
            {
                LookTowards(playerTransform.position, 6.0f);
                transform.position += transform.forward * (moveSpeed * 1.4f * Time.deltaTime);
            }
            stepCycle += Time.deltaTime * 10.0f;
            TryPlayAnimation("walk");
        }
        else
        {
            // Berhenti di samping pemain dan menatap pemain dengan senang
            if (navAgent && navAgent.isOnNavMesh) navAgent.ResetPath();
            LookTowards(playerTransform.position, 4.0f);
            TryPlayAnimation("idle");
        }
    }

    private void LookTowards(Vector3 targetPos, float speed)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * speed);
        }
    }

    private void ApplyProceduralMotion()
    {
        // 1. Napas lembut (subtle breathing scale)
        float breathe = 1.0f + Mathf.Sin(breatheCycle) * 0.015f;
        transform.localScale = new Vector3(baseLocalScale.x * breathe, baseLocalScale.y * (2f - breathe), baseLocalScale.z * breathe);

        // 2. Sedikit goyangan saat berjalan (walking bob & roll)
        if (currentState == State.Wandering || currentState == State.FollowingPlayer)
        {
            float roll = Mathf.Sin(stepCycle) * 2.2f;
            float pitch = Mathf.Cos(stepCycle * 2f) * 1.2f;
            transform.localRotation *= Quaternion.Euler(pitch * 0.3f, 0, roll * 0.3f);
        }
    }

    public override void Interact(PlayerController player)
    {
        // Tampilkan feedback teks interaksi
        if (HintManager.Instance != null)
        {
            HintManager.Instance.Show($"pet_{animalName}", petMessage);
        }

        // Efek lompat kecil ceria
        transform.position += Vector3.up * 0.25f;

        if (animalType == AnimalType.Dog && canFollowPlayer)
        {
            currentState = State.FollowingPlayer;
            companionTimer = companionDuration;
            prompt = $"{animalName} sedang menemanimu!";
        }
        else if (animalType == AnimalType.Cat)
        {
            // Kucing mengeong riang
            currentState = State.Idle;
            stateTimer = 4.0f;
        }
    }

    private void TryPlayAnimation(string clipKeyword)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("isMoving", clipKeyword == "walk");
        }
        else if (legacyAnim != null)
        {
            foreach (AnimationState state in legacyAnim)
            {
                if (state.name.ToLower().Contains(clipKeyword))
                {
                    legacyAnim.CrossFade(state.name, 0.25f);
                    return;
                }
            }
        }
    }
}
