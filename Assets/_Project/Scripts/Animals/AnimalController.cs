using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontroler AI Hewan untuk Desa Eldara (Anjing Milo, Kucing Luna/Oliver, Serigala Rimba).
/// Menghadirkan pergerakan halus, alami, dan 100% menapak kokoh di tanah (Anti-Melayang/Terbang):
/// - Menapak tanah mutlak (Ground Lock: Y = 0 permanen di Update & LateUpdate)
/// - Skala tubuh stabil (Anti-Ballooning: ukuran mesh diikat ke visualBaseScale asli)
/// - Animasi skeletal mulus (Idle & Walking dengan peredaman gerak alami)
/// - Akselerasi & deselerasi halus (belok di tempat jika sudut tajam sebelum melangkah)
/// - Rotasi horizontal murni (Yaw-only turning 360°, tanpa pitch ke angkasa)
/// - AI Wandering dengan micro-pause penasaran (menoleh santai ke sekeliling)
/// - Mode Companion: Anjing mengikuti pemain dengan kecepatan adaptif di darat
/// </summary>
public class AnimalController : Interactable
{
    public enum AnimalType { Dog, Cat, Wolf, Fish }

    [Header("Identitas Hewan")]
    public AnimalType animalType = AnimalType.Dog;
    public string animalName = "Milo";

    [Header("Parameter Gerak Realistis")]
    public float wanderRadius = 6.0f;
    public float moveSpeed = 1.8f;
    public float acceleration = 4.0f;
    public float turnSpeed = 220.0f;
    public float idleTimeMin = 2.5f;
    public float idleTimeMax = 5.5f;

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
    private enum State { Idle, Wandering, FollowingPlayer }
    private State currentState = State.Idle;

    private Vector3 homePosition;
    private Vector3 currentTarget;
    private float currentSpeed;
    private float stateTimer;
    private float companionTimer;
    private float idleSubTimer;
    private float idleHeadYaw;

    // Base Scale & Secondary Motion
    private Vector3 visualBaseScale = Vector3.one;
    private float stepCycle;
    private float breatheCycle;
    private float petBounceTimer;

    void Awake()
    {
        homePosition = transform.position;
        homePosition.y = 0f; // Kunci ketinggian asal di tanah datar Desa Eldara (Y = 0)

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent)
        {
            navAgent.updateRotation = false; // Kita kendalikan rotasi secara manual agar mulus
            navAgent.updateUpAxis = false;
            navAgent.speed = moveSpeed;
            navAgent.acceleration = acceleration * 2f;
            // Jika NavMesh belum dibake, nonaktifkan navAgent agar tidak memunculkan log error
            if (!navAgent.isOnNavMesh)
            {
                navAgent.enabled = false;
            }
        }

        // Cache visual child transform, base scale, & animator
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
            visualBaseScale = visualTransform.localScale;
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

        // Pastikan visualBaseScale tersimpan akurat
        if (visualTransform != null && visualBaseScale == Vector3.one && visualTransform.localScale != Vector3.one)
        {
            visualBaseScale = visualTransform.localScale;
        }

        // Kunci tanah awal
        SnapToGround();
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

        // 2. Kunci tanah mutlak (Ground Lock: mencegah hewan melayang ke udara)
        SnapToGround();

        // 3. Sinkronisasi Animator
        UpdateAnimator();

        // 4. Procedural Secondary Animation (Napas & micro-roll pada visual lokal)
        ApplyProceduralSecondaryMotion();
    }

    void LateUpdate()
    {
        // Penguncian ganda di LateUpdate setelah seluruh animasi & physics selesai dihitung
        SnapToGround();
    }

    private void UpdateWanderState(float distToPlayer)
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                // Perlambat kecepatan hingga berhenti secara halus
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);

                // Micro-behavior saat diam: tengok santai kiri-kanan atau tatap pemain jika dekat
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
                    Vector3 lookDir = transform.forward + transform.right * (idleHeadYaw * 0.015f);
                    SmoothLookTowards(transform.position + lookDir, turnSpeed * 0.4f);
                }

                if (stateTimer <= 0f)
                {
                    // Pilih tujuan baru di tanah (Y = 0)
                    Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
                    currentTarget = new Vector3(homePosition.x + randCircle.x, 0f, homePosition.z + randCircle.y);

                    if (navAgent && navAgent.enabled && navAgent.isOnNavMesh)
                    {
                        navAgent.SetDestination(currentTarget);
                    }

                    currentState = State.Wandering;
                    stateTimer = Random.Range(4.5f, 8.5f);
                }
                break;

            case State.Wandering:
                Vector3 moveDir = Vector3.zero;

                if (navAgent && navAgent.enabled && navAgent.isOnNavMesh)
                {
                    if (navAgent.hasPath)
                    {
                        moveDir = navAgent.desiredVelocity;
                        moveDir.y = 0f;
                        moveDir.Normalize();
                    }
                    else
                    {
                        moveDir = currentTarget - transform.position;
                        moveDir.y = 0f;
                        moveDir.Normalize();
                    }

                    float targetSpeed = (navAgent.remainingDistance > 0.8f) ? moveSpeed : (moveSpeed * 0.45f);
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
                    // Navigasi darat langsung tanpa NavMesh (Datar murni, Y = 0)
                    Vector3 toTarget = currentTarget - transform.position;
                    toTarget.y = 0f;
                    float dist = toTarget.magnitude;

                    if (dist > 0.45f)
                    {
                        moveDir = toTarget.normalized;

                        // Perilaku belok realistis: jika sudut belokan tajam (> 60°), perlambat gerak maju
                        Vector3 forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                        float angleToTarget = Vector3.Angle(forwardFlat, moveDir);
                        float speedFactor = Mathf.Clamp01((180f - angleToTarget) / 90f);

                        float desiredSpeed = moveSpeed * speedFactor;
                        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

                        transform.position += forwardFlat * (currentSpeed * Time.deltaTime);
                    }
                    else
                    {
                        ArriveAtIdle();
                        return;
                    }
                }

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

        // Posisi target companion: di samping-belakang pemain di permukaan tanah (Y = 0)
        Vector3 companionSlot = playerTransform.position 
                              - playerTransform.forward * 1.5f 
                              + playerTransform.right * 1.0f;
        companionSlot.y = 0f;

        float distToSlot = Vector3.Distance(transform.position, companionSlot);

        if (distToSlot > 0.6f)
        {
            Vector3 moveDir = companionSlot - transform.position;
            moveDir.y = 0f;
            moveDir.Normalize();

            Vector3 forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            float angleToSlot = Vector3.Angle(forwardFlat, moveDir);
            float speedFactor = Mathf.Clamp01((180f - angleToSlot) / 90f);

            float desiredSpeed = Mathf.Clamp(distToSlot * 1.25f, moveSpeed * 0.8f, moveSpeed * 1.8f) * speedFactor;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * 1.5f * Time.deltaTime);

            if (navAgent && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.speed = currentSpeed;
                navAgent.SetDestination(companionSlot);
                if (navAgent.desiredVelocity.sqrMagnitude > 0.01f)
                {
                    moveDir = navAgent.desiredVelocity;
                    moveDir.y = 0f;
                    moveDir.Normalize();
                }
            }
            else
            {
                transform.position += forwardFlat * (currentSpeed * Time.deltaTime);
            }

            SmoothLookTowards(transform.position + moveDir, turnSpeed * 1.3f);
            stepCycle += Time.deltaTime * (currentSpeed * 4.5f);
        }
        else
        {
            // Berhenti santai di samping pemain
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * 2f * Time.deltaTime);
            if (navAgent && navAgent.enabled && navAgent.isOnNavMesh) navAgent.ResetPath();
            SmoothLookTowards(playerTransform.position, turnSpeed * 0.9f);
        }
    }

    private void ArriveAtIdle()
    {
        currentState = State.Idle;
        currentSpeed = 0f;
        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
        if (navAgent && navAgent.enabled && navAgent.isOnNavMesh) navAgent.ResetPath();
    }

    private void SmoothLookTowards(Vector3 targetPos, float speed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f; // Rotasi horizontal murni (Yaw-only), TIDAK PERNAH PITCH KE LANGIT
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Mengunci ketinggian hewan agar 100% menapak tanah datar Desa Eldara (Y = 0) dan tidak pernah melayang ke langit.
    /// </summary>
    private void SnapToGround()
    {
        Vector3 pos = transform.position;
        pos.y = 0f; // Desa Eldara memiliki permukaan tanah datar di Y = 0
        transform.position = pos;
    }

    private void UpdateAnimator()
    {
        bool isMoving = currentSpeed > 0.12f;
        SetAnimationMoving(isMoving);

        if (animator != null)
        {
            float animSpeedRatio = isMoving ? Mathf.Clamp(currentSpeed / moveSpeed, 0.75f, 1.35f) : 1.0f;
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

        // 1. Napas lembut (berbasis visualBaseScale yang tersimpan, BUKAN compounding multiplication)
        float breathe = 1.0f + Mathf.Sin(breatheCycle) * 0.012f;
        visualTransform.localScale = new Vector3(
            visualBaseScale.x * breathe,
            visualBaseScale.y * (2f - breathe),
            visualBaseScale.z * breathe
        );

        // 2. Efek lompat kecil ceria saat dielus (pada visual lokal, BUKAN root transform)
        float bounceY = 0f;
        if (petBounceTimer > 0f)
        {
            petBounceTimer -= Time.deltaTime;
            bounceY = Mathf.Sin((0.35f - petBounceTimer) / 0.35f * Mathf.PI) * 0.15f;
        }

        // 3. Micro-roll saat melangkah agar tubuh tampak organik
        float roll = (currentSpeed > 0.2f) ? Mathf.Sin(stepCycle) * 1.1f : 0f;
        visualTransform.localPosition = new Vector3(0, bounceY, 0);
        visualTransform.localRotation = Quaternion.Euler(0, 0, roll);
    }

    public override void Interact(PlayerController player)
    {
        if (HintManager.Instance != null)
        {
            HintManager.Instance.Show($"pet_{animalName}", petMessage);
        }

        // Efek lompat ceria pada visual child (tidak mengubah ketinggian root)
        petBounceTimer = 0.35f;

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
