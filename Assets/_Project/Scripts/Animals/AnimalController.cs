using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontroler AI Hewan untuk Desa Eldara (Anjing Milo, Kucing Luna/Oliver, Serigala).
/// Menghadirkan pergerakan halus, alami, dan realistis:
/// - Animasi skeletal mulus (Idle & Walking dengan peredaman gerak alami)
/// - Akselerasi & deselerasi halus (tidak mendadak berhenti/mulai)
/// - Rotasi berbelok halus (Smooth Rotation Damping tanpa patah-patah)
/// - Penyesuaian kemiringan tanah (Terrain Normal Slope Alignment)
/// - AI Wandering dengan micro-pause penasaran (sniffing, looking around)
/// - Mode Companion: Anjing mengikuti pemain dengan kecepatan adaptif
/// </summary>
public class AnimalController : Interactable
{
    public enum AnimalType { Dog, Cat, Wolf, Fish }

    [Header("Identitas Hewan")]
    public AnimalType animalType = AnimalType.Dog;
    public string animalName = "Milo";

    [Header("Parameter Gerak Realistis")]
    public float wanderRadius = 7.0f;
    public float moveSpeed = 1.9f;
    public float acceleration = 3.5f;
    public float turnSpeed = 220.0f;
    public float idleTimeMin = 2.5f;
    public float idleTimeMax = 6.0f;

    [Header("Interaksi & Companion")]
    [TextArea(2, 3)]
    public string petMessage = "Guk! Milo mengibaskan ekornya riang dan tampak sangat senang!";
    public bool canFollowPlayer = true;
    public float companionDuration = 40.0f;

    // Komponen
    private NavMeshAgent navAgent;
    private Animator animator;
    private Transform visualTransform;
    private Transform playerTransform;

    // AI State
    private enum State { Idle, Wandering, LookingAtPlayer, FollowingPlayer }
    private State currentState = State.Idle;

    private Vector3 homePosition;
    private Vector3 currentTarget;
    private float currentSpeed;
    private float stateTimer;
    private float companionTimer;
    private float idleSubTimer;
    private float idleHeadYaw;

    // Ground slope alignment
    private Vector3 groundNormal = Vector3.up;

    // Procedural secondary motion
    private float stepCycle;
    private float breatheCycle;

    void Awake()
    {
        homePosition = transform.position;

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent)
        {
            navAgent.updateRotation = false; // Kita kendalikan rotasi secara manual agar jauh lebih halus!
            navAgent.speed = moveSpeed;
            navAgent.acceleration = acceleration * 2f;
        }

        // Cache visual child transform & animator
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
            animator = visualTransform.GetComponent<Animator>();
        }
        if (!animator) animator = GetComponentInChildren<Animator>();

        prompt = $"Tekan E untuk mengelus {animalName}";
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p) playerTransform = p.transform;

        stateTimer = Random.Range(1.0f, 2.5f);
        SetAnimationMoving(false);
    }

    void Update()
    {
        breatheCycle += Time.deltaTime * 2.2f;

        float distToPlayer = playerTransform ? Vector3.Distance(transform.position, playerTransform.position) : 999f;

        // 1. Eksekusi state AI
        if (currentState == State.FollowingPlayer)
        {
            UpdateCompanionState(distToPlayer);
        }
        else
        {
            UpdateWanderState(distToPlayer);
        }

        // 2. Deteksi kemiringan tanah secara dinamis
        AlignWithGroundSlope();

        // 3. Sinkronisasi Animator
        UpdateAnimator();

        // 4. Procedural Secondary Animation (Napas halus & bobbing)
        ApplyProceduralSecondaryMotion();
    }

    private void UpdateWanderState(float distToPlayer)
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                // Perlambat kecepatan sampai 0 secara halus
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);

                // Micro-behavior saat diam: tengok kiri-kanan santai atau tatap pemain jika dekat
                idleSubTimer -= Time.deltaTime;
                if (idleSubTimer <= 0f)
                {
                    idleSubTimer = Random.Range(2.0f, 4.0f);
                    idleHeadYaw = Random.Range(-25f, 25f);
                }

                if (distToPlayer < 3.0f && (animalType == AnimalType.Dog || animalType == AnimalType.Cat))
                {
                    SmoothLookTowards(playerTransform.position, turnSpeed * 0.7f);
                }
                else
                {
                    // Goyangkan arah tatapan sedikit secara organik
                    Vector3 lookDir = transform.forward + transform.right * (idleHeadYaw * 0.015f);
                    SmoothLookTowards(transform.position + lookDir, turnSpeed * 0.4f);
                }

                if (stateTimer <= 0f)
                {
                    // Pilih tujuan baru dalam radius
                    Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
                    currentTarget = homePosition + new Vector3(randCircle.x, 0, randCircle.y);

                    if (Physics.Raycast(currentTarget + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 15f))
                    {
                        currentTarget = hit.point;
                    }

                    if (navAgent && navAgent.isOnNavMesh)
                    {
                        navAgent.SetDestination(currentTarget);
                    }

                    currentState = State.Wandering;
                    stateTimer = Random.Range(4.5f, 9.0f);
                }
                break;

            case State.Wandering:
                // Navigasi menuju target
                Vector3 moveDir = Vector3.zero;

                if (navAgent && navAgent.isOnNavMesh)
                {
                    if (navAgent.hasPath)
                    {
                        moveDir = navAgent.desiredVelocity.normalized;
                    }
                    else
                    {
                        moveDir = (currentTarget - transform.position).normalized;
                    }

                    // Akselerasi halus
                    float targetSpeed = (navAgent.remainingDistance > 0.8f) ? moveSpeed : (moveSpeed * 0.5f);
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
                    navAgent.speed = currentSpeed;

                    if (!navAgent.pathPending && navAgent.remainingDistance <= 0.45f)
                    {
                        ArriveAtIdle();
                        return;
                    }
                }
                else
                {
                    // Fallback navigasi tanpa NavMesh
                    Vector3 toTarget = currentTarget - transform.position;
                    toTarget.y = 0;
                    float dist = toTarget.magnitude;

                    if (dist > 0.4f)
                    {
                        moveDir = toTarget.normalized;
                        currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, acceleration * Time.deltaTime);
                        transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                    }
                    else
                    {
                        ArriveAtIdle();
                        return;
                    }
                }

                // Rotasi berbelok halus menghadap arah jalan
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    SmoothLookTowards(transform.position + moveDir, turnSpeed);
                    stepCycle += Time.deltaTime * (currentSpeed * 4.5f);
                }

                if (stateTimer <= 0f)
                {
                    ArriveAtIdle();
                }
                break;
        }
    }

    private void UpdateCompanionState(float distToPlayer)
    {
        companionTimer -= Time.deltaTime;
        if (companionTimer <= 0f || !playerTransform)
        {
            currentState = State.Idle;
            stateTimer = 2.0f;
            prompt = $"Tekan E untuk mengelus {animalName}";
            return;
        }

        // Target companion: berada di samping kanan-belakang pemain
        Vector3 companionSlot = playerTransform.position 
                              - playerTransform.forward * 1.8f 
                              + playerTransform.right * 1.2f;

        float distToSlot = Vector3.Distance(transform.position, companionSlot);

        if (distToSlot > 0.6f)
        {
            // Kecepatan adaptif mengikuti pemain (trot santai saat pemain jalan, lari saat pemain lari)
            float desiredSpeed = Mathf.Clamp(distToSlot * 1.25f, moveSpeed * 0.8f, moveSpeed * 1.8f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * 1.5f * Time.deltaTime);

            Vector3 moveDir = (companionSlot - transform.position).normalized;

            if (navAgent && navAgent.isOnNavMesh)
            {
                navAgent.speed = currentSpeed;
                navAgent.SetDestination(companionSlot);
                if (navAgent.desiredVelocity.sqrMagnitude > 0.01f)
                    moveDir = navAgent.desiredVelocity.normalized;
            }
            else
            {
                transform.position += transform.forward * (currentSpeed * Time.deltaTime);
            }

            SmoothLookTowards(transform.position + moveDir, turnSpeed * 1.2f);
            stepCycle += Time.deltaTime * (currentSpeed * 4.5f);
        }
        else
        {
            // Berhenti di samping pemain dan menatap pemain dengan senang
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * 2f * Time.deltaTime);
            if (navAgent && navAgent.isOnNavMesh) navAgent.ResetPath();
            SmoothLookTowards(playerTransform.position, turnSpeed * 0.9f);
        }
    }

    private void ArriveAtIdle()
    {
        currentState = State.Idle;
        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
        if (navAgent && navAgent.isOnNavMesh) navAgent.ResetPath();
    }

    private void SmoothLookTowards(Vector3 targetPos, float speed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, groundNormal);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
        }
    }

    private void AlignWithGroundSlope()
    {
        // Raycast ke bawah untuk mendeteksi kontur tanah & cobblestone
        if (Physics.Raycast(transform.position + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 2.5f))
        {
            groundNormal = Vector3.Slerp(groundNormal, hit.normal, Time.deltaTime * 6.0f);
            // Ketinggian kaki pas menyentuh tanah
            Vector3 pos = transform.position;
            pos.y = Mathf.MoveTowards(pos.y, hit.point.y, Time.deltaTime * 5.0f);
            transform.position = pos;
        }
        else
        {
            groundNormal = Vector3.Slerp(groundNormal, Vector3.up, Time.deltaTime * 3.0f);
        }
    }

    private void UpdateAnimator()
    {
        bool isMoving = currentSpeed > 0.15f;
        SetAnimationMoving(isMoving);

        if (animator != null)
        {
            // Skala kecepatan playback animasi agar langkah kaki sinkron dengan kecepatan jalan
            float animSpeedRatio = isMoving ? Mathf.Clamp(currentSpeed / moveSpeed, 0.7f, 1.4f) : 1.0f;
            animator.speed = animSpeedRatio;
        }
    }

    private void SetAnimationMoving(bool moving)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("isMoving", moving);
        }
    }

    private void ApplyProceduralSecondaryMotion()
    {
        if (!visualTransform) return;

        // 1. Napas lembut (subtle breathing cycle)
        float breathe = 1.0f + Mathf.Sin(breatheCycle) * 0.012f;
        visualTransform.localScale = new Vector3(
            visualTransform.localScale.x * breathe,
            visualTransform.localScale.y * (2f - breathe),
            visualTransform.localScale.z * breathe
        );

        // 2. Micro-roll saat melangkah agar tubuh tampak organik
        if (currentSpeed > 0.2f)
        {
            float roll = Mathf.Sin(stepCycle) * 1.2f;
            visualTransform.localRotation = Quaternion.Euler(0, 0, roll);
        }
        else
        {
            visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, Quaternion.identity, Time.deltaTime * 5f);
        }
    }

    public override void Interact(PlayerController player)
    {
        if (HintManager.Instance != null)
        {
            HintManager.Instance.Show($"pet_{animalName}", petMessage);
        }

        // Lompat kecil ceria
        transform.position += Vector3.up * 0.18f;

        if (animalType == AnimalType.Dog && canFollowPlayer)
        {
            currentState = State.FollowingPlayer;
            companionTimer = companionDuration;
            prompt = $"{animalName} sedang menemanimu berjalan-jalan!";
        }
        else if (animalType == AnimalType.Cat)
        {
            currentState = State.Idle;
            stateTimer = 4.5f;
            currentSpeed = 0f;
        }
    }
}
