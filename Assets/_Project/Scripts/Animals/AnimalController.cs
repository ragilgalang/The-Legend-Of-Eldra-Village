using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontroler AI Hewan untuk Desa Eldara (Anjing Milo, Kucing Luna/Oliver, Serigala Rimba).
/// Menghadirkan pergerakan yang sangat halus (smooth), alami, dan 100% anti-terbang:
/// - Anti-Melayang Mutlak (Double Ground Lock Y clamped [0, 0.35m] di Update & LateUpdate)
/// - Anti-Tenggelam (Menapak sempurna di atas batu cobblestone tanpa tenggelam ke dalam batu)
/// - Menghindar Rintangan Cerdas (Raycast Whiskers: tidak menabrak gerobak, peti, atau pagar)
/// - Belokan Halus Alami (Slerp Ease-in/out dengan modulasi kecepatan saat berbelok)
/// - Target Terbuka (Validasi titik wander agar tidak memilih titik di dalam bangunan/objek)
/// - Animasi Tersinkronisasi (Kecepatan playback kaki selaras dengan laju gerak tubuh)
/// - Mode Sahabat (Anjing Milo menemani pemain berjalan dengan akselerasi adaptif)
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
    public float acceleration = 3.2f;
    public float turnSmoothness = 5.0f;
    public float idleTimeMin = 3.0f;
    public float idleTimeMax = 6.5f;

    [Header("Interaksi & Companion")]
    [TextArea(2, 3)]
    public string petMessage = "Guk! Milo mengibaskan ekornya riang dan kini menemanimu berjalan-jalan!";
    public bool canFollowPlayer = true;
    public float companionDuration = 45.0f;

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

    // Secondary & Procedural
    private Vector3 visualBaseScale = Vector3.one;
    private float stepCycle;
    private float breatheCycle;
    private float petBounceTimer;

    // Ground & Obstacle
    private float currentGroundY = 0f;

    void Awake()
    {
        homePosition = transform.position;
        homePosition.y = 0f;

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent)
        {
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;
            navAgent.speed = moveSpeed;
            navAgent.acceleration = acceleration * 2f;
            // Jika NavMesh belum dibake, nonaktifkan navAgent agar tidak ada interferensi
            if (!navAgent.isOnNavMesh)
            {
                navAgent.enabled = false;
            }
        }

        // Cache visual child transform & scale dasar
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

        stateTimer = Random.Range(1.5f, 3.0f);
        SetAnimationMoving(false);

        if (visualTransform != null && visualBaseScale == Vector3.one && visualTransform.localScale != Vector3.one)
        {
            visualBaseScale = visualTransform.localScale;
        }

        // Pijakkan kaki awal di tanah
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

        // 2. Kunci tanah adaptif (Ground & Cobblestone Lock: Y clamped [0, 0.35m])
        SnapToGround();

        // 3. Sinkronisasi Animator
        UpdateAnimator();

        // 4. Procedural Secondary Animation (Napas & micro-roll visual)
        ApplyProceduralSecondaryMotion();
    }

    void LateUpdate()
    {
        // Penguncian ganda di LateUpdate memastikan hewan 100% mustahil melayang ke angkasa
        SnapToGround();

        Vector3 p = transform.position;
        p.y = Mathf.Clamp(currentGroundY, 0f, 0.25f);
        transform.position = p;

        if (visualTransform)
        {
            Vector3 vPos = visualTransform.localPosition;
            vPos.y = Mathf.Clamp(vPos.y, 0f, 0.12f);
            visualTransform.localPosition = vPos;
        }
    }

    private void UpdateWanderState(float distToPlayer)
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                // Deselerasi lembut hingga berhenti sempurna
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * 1.5f * Time.deltaTime);

                // Perilaku saat diam: santai menoleh atau tatap pemain jika dekat
                idleSubTimer -= Time.deltaTime;
                if (idleSubTimer <= 0f)
                {
                    idleSubTimer = Random.Range(2.0f, 4.5f);
                    idleHeadYaw = Random.Range(-22f, 22f);
                }

                if (distToPlayer < 3.2f && (animalType == AnimalType.Dog || animalType == AnimalType.Cat))
                {
                    SmoothLookTowards(playerTransform.position, turnSmoothness * 0.8f);
                }
                else
                {
                    Vector3 idleLookDir = transform.forward + transform.right * (idleHeadYaw * 0.015f);
                    SmoothLookTowards(transform.position + idleLookDir, turnSmoothness * 0.4f);
                }

                if (stateTimer <= 0f)
                {
                    // Pilih tujuan baru di area terbuka (menghindari bangunan & gerobak)
                    currentTarget = PickValidWanderTarget();

                    if (navAgent && navAgent.enabled && navAgent.isOnNavMesh)
                    {
                        navAgent.SetDestination(currentTarget);
                    }

                    currentState = State.Wandering;
                    stateTimer = Random.Range(5.0f, 9.0f);
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

                    if (!navAgent.pathPending && navAgent.remainingDistance <= 0.5f)
                    {
                        ArriveAtIdle();
                        return;
                    }
                }
                else
                {
                    // Navigasi darat langsung dengan Whisker Obstacle Avoidance
                    Vector3 toTarget = currentTarget - transform.position;
                    toTarget.y = 0f;
                    float dist = toTarget.magnitude;

                    if (dist <= 0.5f)
                    {
                        ArriveAtIdle();
                        return;
                    }

                    moveDir = toTarget.normalized;

                    // Sensor Whisker: Deteksi rintangan di depan (Gerobak, Kotak, Pagar, Rumah)
                    moveDir = AvoidObstacles(moveDir);

                    // Belokan halus: Perlambat langkah jika sudut belok tajam
                    Vector3 forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                    float angleToTarget = Vector3.Angle(forwardFlat, moveDir);
                    float turnEase = Mathf.Clamp01((180f - angleToTarget) / 100f);

                    float desiredSpeed = moveSpeed * turnEase;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

                    transform.position += forwardFlat * (currentSpeed * Time.deltaTime);
                }

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    SmoothLookTowards(transform.position + moveDir, turnSmoothness);
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
            stateTimer = 2.5f;
            prompt = $"Tekan E untuk mengelus {animalName}";
            return;
        }

        // Posisi target companion: 1.5m di belakang-samping pemain
        Vector3 companionSlot = playerTransform.position 
                              - playerTransform.forward * 1.5f 
                              + playerTransform.right * 1.0f;
        companionSlot.y = 0f;

        float distToSlot = Vector3.Distance(transform.position, companionSlot);

        if (distToSlot > 0.65f)
        {
            Vector3 moveDir = companionSlot - transform.position;
            moveDir.y = 0f;
            moveDir.Normalize();

            // Hindari rintangan saat mengikuti pemain
            moveDir = AvoidObstacles(moveDir);

            Vector3 forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            float angleToSlot = Vector3.Angle(forwardFlat, moveDir);
            float turnEase = Mathf.Clamp01((180f - angleToSlot) / 90f);

            // Kecepatan adaptif: lari jika pemain jauh, santai jika dekat
            float desiredSpeed = Mathf.Clamp(distToSlot * 1.3f, moveSpeed * 0.8f, moveSpeed * 1.9f) * turnEase;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * 1.6f * Time.deltaTime);

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

            SmoothLookTowards(transform.position + moveDir, turnSmoothness * 1.25f);
            stepCycle += Time.deltaTime * (currentSpeed * 4.5f);
        }
        else
        {
            // Berhenti santai di samping pemain
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * 2.2f * Time.deltaTime);
            if (navAgent && navAgent.enabled && navAgent.isOnNavMesh) navAgent.ResetPath();
            SmoothLookTowards(playerTransform.position, turnSmoothness * 0.9f);
        }
    }

    /// <summary>
    /// Sensor Whisker Raycast: Mengarahkan hewan membelok menjauhi gerobak, pagar, atau tembok.
    /// </summary>
    private Vector3 AvoidObstacles(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.35f;
        float checkDistance = 1.15f;

        // Cek rintangan langsung di depan
        if (Physics.SphereCast(origin, 0.22f, transform.forward, out RaycastHit hitFront, checkDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hitFront.transform.root != transform && hitFront.point.y > 0.35f)
            {
                // Rintangan tinggi terdeteksi (gerobak/rumah/pagar)!
                Vector3 leftWhisker = Quaternion.Euler(0, -45f, 0) * transform.forward;
                Vector3 rightWhisker = Quaternion.Euler(0, 45f, 0) * transform.forward;

                bool leftHit = Physics.Raycast(origin, leftWhisker, checkDistance * 1.2f, ~0, QueryTriggerInteraction.Ignore);
                bool rightHit = Physics.Raycast(origin, rightWhisker, checkDistance * 1.2f, ~0, QueryTriggerInteraction.Ignore);

                if (!leftHit) return leftWhisker.normalized;
                if (!rightHit) return rightWhisker.normalized;

                // Jika kedua sisi buntu, berhenti santai dan pilih tujuan baru
                ArriveAtIdle();
            }
        }

        return desiredDir;
    }

    /// <summary>
    /// Memilih titik tujuan wander yang berada di area terbuka (bebas rintangan).
    /// </summary>
    private Vector3 PickValidWanderTarget()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = new Vector3(homePosition.x + circle.x, 0f, homePosition.z + circle.y);

            // Periksa apakah titik ini tertabrak bangunan atau prop
            if (!Physics.CheckSphere(candidate + Vector3.up * 0.5f, 0.45f, ~0, QueryTriggerInteraction.Ignore))
            {
                return candidate;
            }
        }

        // Fallback ke posisi asal jika semua titik padat rintangan
        return homePosition;
    }

    private void ArriveAtIdle()
    {
        currentState = State.Idle;
        currentSpeed = 0f;
        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
        if (navAgent && navAgent.enabled && navAgent.isOnNavMesh) navAgent.ResetPath();
    }

    private void SmoothLookTowards(Vector3 targetPos, float smoothness)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f; // Rotasi horizontal murni (Yaw-only), TIDAK PERNAH PITCH KE LANGIT
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothness);
        }
    }

    /// <summary>
    /// Menyesuaikan ketinggian kaki dengan tanah atau undakan batu (Cobblestone).
    /// DENGAN BATASAN KETAT (CLAMP [0, 0.25m]) AGAR 100% MUTLAK MUSTAHIL MELAYANG KE LANGIT!
    /// </summary>
    private void SnapToGround()
    {
        float targetY = 0f;

        // Raycast ke bawah dari ketinggian 0.6m untuk mendeteksi tanah atau permukaan batu
        Vector3 origin = transform.position + Vector3.up * 0.6f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 1.4f, ~0, QueryTriggerInteraction.Ignore);

        float highestGround = -999f;
        foreach (var h in hits)
        {
            if (h.transform.root == transform) continue;
            if (h.collider.isTrigger) continue;
            if (h.transform.CompareTag("Player") || h.transform.CompareTag("NPC")) continue;

            // Hanya anggap sebagai lantai jika ketinggian tanah absolut wajar (antara -0.15m s/d 0.25m)
            // Mengabaikan dinding, atap, gerobak, pagar, peti, atau objek tinggi lain
            if (h.point.y <= 0.25f && h.point.y >= -0.15f)
            {
                if (h.point.y > highestGround)
                {
                    highestGround = h.point.y;
                }
            }
        }

        if (highestGround > -900f)
        {
            targetY = highestGround;
        }

        // KUNCI MATEMATIS ANTI-MELAYANG MUTLAK:
        // Permukaan tanah Desa Eldara di Y = 0 dan undakan batu tertinggi di Y = 0.20m.
        // Dengan clamp ketat [0f, 0.25f], hewan 100% MUSTAHIL melayang ke langit dalam kondisi apapun!
        targetY = Mathf.Clamp(targetY, 0f, 0.25f);

        currentGroundY = Mathf.MoveTowards(currentGroundY, targetY, Time.deltaTime * 8.0f);

        Vector3 pos = transform.position;
        pos.y = currentGroundY;
        transform.position = pos;
    }

    private void UpdateAnimator()
    {
        bool isMoving = currentSpeed > 0.10f;
        SetAnimationMoving(isMoving);

        if (animator != null)
        {
            // Sinkronisasi kecepatan playback: animasi kaki sesuai laju gerak tubuh
            // Pastikan applyRootMotion=false agar animasi TIDAK menggerakkan posisi karakter
            if (animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
            }

            // Skala kecepatan animasi agar langkah kaki sinkron dengan kecepatan berjalan
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

        // 1. Napas lembut (berbasis visualBaseScale yang tersimpan statis, BUKAN compounding)
        float breathe = 1.0f + Mathf.Sin(breatheCycle) * 0.012f;
        visualTransform.localScale = new Vector3(
            visualBaseScale.x * breathe,
            visualBaseScale.y * (2f - breathe),
            visualBaseScale.z * breathe
        );

        // 2. Efek lompat ceria saat dielus
        float bounceY = 0f;
        if (petBounceTimer > 0f)
        {
            petBounceTimer -= Time.deltaTime;
            bounceY = Mathf.Sin((0.35f - petBounceTimer) / 0.35f * Mathf.PI) * 0.15f;
        }

        // 3. Micro-roll saat melangkah agar gerak tubuh organik
        float roll = (currentSpeed > 0.15f) ? Mathf.Sin(stepCycle) * 1.0f : 0f;
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
            stateTimer = 5.0f;
            currentSpeed = 0f;
        }
    }
}
