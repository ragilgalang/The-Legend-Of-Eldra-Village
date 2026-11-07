using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontroler AI Hewan untuk Desa Eldara (Anjing Milo, Kucing Luna/Oliver, Serigala Rimba).
///
/// Perbaikan utama (jalan menyamping / seperti kepiting):
/// 1. Gerak SELALU maju searah badan (transform.forward). Badan berputar dulu menuju arah tujuan,
///    tubuh tidak pernah "strafing" ke samping. NavMeshAgent hanya dipakai untuk menentukan arah (updatePosition = false).
/// 2. Orientasi model visual dideteksi otomatis (sumbu terpanjang tubuh) dan DIKALIKAN dengan rotasi asli FBX,
///    bukan menimpanya. Bisa di-flip manual lewat "flipFacing" atau dimatikan dan diatur manual lewat "visualRotationOffset".
/// 3. Belok memakai kecepatan sudut (derajat/detik) dengan ease-out, kecepatan jalan turun saat belokan tajam.
/// 4. Kecepatan animasi kaki disinkronkan dengan kecepatan nyata (clipWalkSpeed) agar kaki tidak meluncur.
/// 5. Mode Sahabat memakai hysteresis (mulai jalan jika jauh, berhenti jika dekat) sehingga tidak bergetar.
/// 6. Tetap anti-melayang (Y di-clamp 0 .. 0.25 m) dan menghindari rintangan dengan sensor whisker.
/// </summary>
public class AnimalController : Interactable
{
    public enum AnimalType { Dog, Cat, Wolf, Fish }

    [Header("Identitas Hewan")]
    public AnimalType animalType = AnimalType.Dog;
    public string animalName = "Milo";

    [Header("Parameter Gerak Realistis")]
    public float wanderRadius = 6.0f;
    [Tooltip("Kecepatan jalan maksimum (m/s).")]
    public float moveSpeed = 1.8f;
    public float acceleration = 3.2f;
    [Tooltip("Kecepatan putar maksimum badan (derajat/detik).")]
    public float turnSpeed = 140f;
    [Tooltip("Kompatibilitas legacy smoothness (RealisticDogBuilder / AnimalAssetSetup).")]
    public float turnSmoothness = 5.0f;
    public float idleTimeMin = 3.0f;
    public float idleTimeMax = 6.5f;

    [Header("Sinkronisasi Animasi Kaki")]
    [Tooltip("Kecepatan gerak (m/s) yang cocok dengan clip jalan pada animator.speed = 1. Naikkan jika kaki terlihat meluncur/terlalu lambat, turunkan jika kaki terlalu cepat.")]
    public float clipWalkSpeed = 1.8f;

    [Header("Interaksi & Companion")]
    [TextArea(2, 3)]
    public string petMessage = "Guk! Milo mengibaskan ekornya riang dan kini menemanimu berjalan-jalan!";
    public bool canFollowPlayer = true;
    public float companionDuration = 45.0f;

    [Header("Orientasi Model Visual")]
    [Tooltip("Deteksi otomatis arah hadap model (sumbu tubuh terpanjang). Matikan jika hasilnya salah dan atur manual di bawah.")]
    public bool autoDetectFacing = true;
    [Tooltip("Centang jika hewan berjalan mundur (kepala di belakang arah jalan).")]
    public bool flipFacing = false;
    [Tooltip("Dipakai jika autoDetectFacing dimatikan. Model menghadap +X -> Y = -90, menghadap -X -> Y = 90, menghadap +Z -> 0, menghadap -Z -> 180.")]
    public Vector3 visualRotationOffset = new Vector3(0f, -90f, 0f);

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
    private bool companionMoving;
    private float repathTimer;
    private bool animMoving;

    // Visual
    private Vector3 visualBaseScale = Vector3.one;
    private Vector3 visualBasePosition = Vector3.zero;
    private Quaternion visualBaseRotation = Quaternion.identity;
    private float stepCycle;
    private float breatheCycle;
    private float petBounceTimer;

    // Ground
    private float currentGroundY = 0f;

    private bool AgentReady
    {
        get { return navAgent != null && navAgent.enabled && navAgent.isOnNavMesh; }
    }

    void Awake()
    {
        homePosition = transform.position;
        homePosition.y = 0f;

        if (turnSmoothness > 0f && turnSpeed == 140f)
        {
            turnSpeed = turnSmoothness * 28f;
        }

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent)
        {
            // Script yang menggerakkan badan (selalu maju searah hadap). Agent hanya "navigator".
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;
            navAgent.updatePosition = false;
            navAgent.speed = moveSpeed;
            navAgent.acceleration = acceleration * 4f;
            if (!navAgent.isOnNavMesh) navAgent.enabled = false;
        }

        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
            visualBaseScale = visualTransform.localScale;
            visualBasePosition = visualTransform.localPosition;

            if (animalType == AnimalType.Dog || animalType == AnimalType.Cat || animalType == AnimalType.Wolf)
            {
                ApplyVisualFacing();
            }

            visualBaseRotation = visualTransform.localRotation;
            animator = visualTransform.GetComponent<Animator>();
        }

        if (!animator) animator = GetComponentInChildren<Animator>(true);

        if (animator)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        prompt = $"Tekan E untuk mengelus {animalName}";
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p) playerTransform = p.transform;

        stateTimer = Random.Range(1.5f, 3.0f);
        SetAnimationMoving(false);
        SnapToGround();
    }

    // ------------------------------------------------------------------
    // ORIENTASI MODEL
    // ------------------------------------------------------------------

    /// <summary>
    /// Memutar model visual agar "hidung" menghadap +Z root. Rotasi asli FBX dipertahankan
    /// (dikalikan), tidak ditimpa, sehingga FBX dengan rotasi impor bawaan tidak rusak.
    /// </summary>
    private void ApplyVisualFacing()
    {
        float yaw = visualRotationOffset.y;
        Quaternion extra = Quaternion.Euler(visualRotationOffset);

        if (autoDetectFacing && TryDetectBodyAxis(out bool alongX))
        {
            // Tubuh memanjang di sumbu X -> perlu -90 agar menghadap +Z. Sudah di Z -> 0.
            yaw = alongX ? -90f : 0f;
            if (flipFacing) yaw += 180f;
            extra = Quaternion.Euler(0f, yaw, 0f);
        }
        else if (flipFacing)
        {
            extra = Quaternion.Euler(0f, yaw + 180f, 0f) * Quaternion.Euler(visualRotationOffset.x, 0f, visualRotationOffset.z);
        }

        // Pre-multiply: yaw terjadi pada sumbu atas root, lalu rotasi asli FBX tetap berlaku.
        visualTransform.localRotation = extra * visualTransform.localRotation;
    }

    /// <summary>
    /// Mengukur panjang tubuh pada sumbu X dan Z dunia (root diputar sementara ke identitas).
    /// Badan hewan berkaki empat selalu lebih panjang daripada lebar.
    /// </summary>
    private bool TryDetectBodyAxis(out bool alongX)
    {
        alongX = false;
        Renderer[] rends = visualTransform.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return false;

        Quaternion savedRot = transform.rotation;
        transform.rotation = Quaternion.identity;

        Bounds b = new Bounds();
        bool has = false;
        foreach (var r in rends)
        {
            if (r is ParticleSystemRenderer) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }

        transform.rotation = savedRot;

        if (!has || (b.size.x < 0.01f && b.size.z < 0.01f)) return false;
        if (Mathf.Abs(b.size.x - b.size.z) < 0.02f) return false; // ambigu -> pakai offset manual

        alongX = b.size.x > b.size.z;
        return true;
    }

    // ------------------------------------------------------------------
    // LOOP UTAMA
    // ------------------------------------------------------------------

    void Update()
    {
        breatheCycle += Time.deltaTime * 2.2f;

        float distToPlayer = playerTransform ? Vector3.Distance(transform.position, playerTransform.position) : 999f;

        if (currentState == State.FollowingPlayer) UpdateCompanionState(distToPlayer);
        else UpdateWanderState(distToPlayer);

        SnapToGround();
        UpdateAnimator();
        ApplyProceduralSecondaryMotion();
    }

    void LateUpdate()
    {
        Vector3 p = transform.position;
        p.y = Mathf.Clamp(currentGroundY, 0f, 0.25f);
        transform.position = p;
    }

    // ------------------------------------------------------------------
    // LOCOMOTION INTI: selalu maju searah badan, putar dulu baru melaju
    // ------------------------------------------------------------------

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    /// <summary>
    /// Menggerakkan hewan menuju arah yang diinginkan secara natural:
    /// badan berputar dengan ease-out, kecepatan turun saat belokan tajam,
    /// dan perpindahan SELALU sepanjang transform.forward (tidak pernah menyamping).
    /// </summary>
    private void Locomote(Vector3 desiredDir, float targetSpeed, float turnMul = 1f)
    {
        desiredDir = Flat(desiredDir);
        if (desiredDir.sqrMagnitude < 0.0001f)
        {
            Decelerate(1.5f);
            return;
        }
        desiredDir.Normalize();

        Vector3 fwd = Flat(transform.forward).normalized;
        float angle = Vector3.Angle(fwd, desiredDir);

        // Putar badan: makin kecil sudut, makin halus (ease-out), dibatasi turnSpeed
        float maxStep = turnSpeed * turnMul * Time.deltaTime;
        float easedStep = Mathf.Max(angle * 4f * Time.deltaTime, 25f * Time.deltaTime);
        float step = Mathf.Min(maxStep, easedStep);
        Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, step);

        // Selaraskan kecepatan dengan sudut: lurus = penuh, belok tajam = pelan tapi kaki tetap melangkah
        float alignment = Mathf.InverseLerp(100f, 20f, angle);
        float desiredSpeed = targetSpeed * Mathf.Lerp(0.2f, 1f, alignment);
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        MoveForward(currentSpeed);
        stepCycle += Time.deltaTime * (currentSpeed * 4.5f);
    }

    private void Decelerate(float rateMultiplier)
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * rateMultiplier * Time.deltaTime);
        if (currentSpeed > 0.001f) MoveForward(currentSpeed);
    }

    private void MoveForward(float speed)
    {
        Vector3 fwd = Flat(transform.forward).normalized;
        transform.position += fwd * (speed * Time.deltaTime);

        // Sinkronkan "otak" NavMeshAgent dengan posisi badan sebenarnya
        if (AgentReady) navAgent.nextPosition = transform.position;
    }

    /// <summary>
    /// Memutar badan di tempat dengan halus (hanya yaw).
    /// </summary>
    private void TurnTowards(Vector3 targetPos, float speedMul)
    {
        Vector3 dir = Flat(targetPos - transform.position);
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * speedMul * Time.deltaTime);
    }

    // ------------------------------------------------------------------
    // STATE: IDLE & WANDER
    // ------------------------------------------------------------------

    private void UpdateWanderState(float distToPlayer)
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                Decelerate(1.5f);

                // Menoleh ke pemain jika dekat (anjing & kucing), selain itu diam tenang
                if (distToPlayer < 3.2f && playerTransform &&
                    (animalType == AnimalType.Dog || animalType == AnimalType.Cat))
                {
                    TurnTowards(playerTransform.position, 0.35f);
                }

                if (stateTimer <= 0f)
                {
                    currentTarget = PickValidWanderTarget();
                    if (AgentReady) navAgent.SetDestination(currentTarget);

                    currentState = State.Wandering;
                    stateTimer = Random.Range(6.0f, 10.0f);
                }
                break;

            case State.Wandering:
            {
                Vector3 toTarget = Flat(currentTarget - transform.position);
                float remaining = toTarget.magnitude;

                if (remaining <= 0.5f || stateTimer <= 0f)
                {
                    ArriveAtIdle();
                    return;
                }

                Vector3 dir = toTarget;

                if (AgentReady)
                {
                    if (navAgent.pathPending)
                    {
                        Decelerate(1.0f);
                        return;
                    }
                    if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                    {
                        ArriveAtIdle();
                        return;
                    }
                    if (navAgent.hasPath)
                    {
                        Vector3 steer = Flat(navAgent.steeringTarget - transform.position);
                        if (steer.sqrMagnitude > 0.01f) dir = steer;
                    }
                }

                dir = AvoidObstacles(dir, out bool blocked);
                if (blocked)
                {
                    ArriveAtIdle();
                    return;
                }

                // Melambat halus saat mendekati tujuan
                float targetSpeed = moveSpeed * Mathf.Clamp(remaining / 1.2f, 0.4f, 1f);
                Locomote(dir, targetSpeed);
                break;
            }
        }
    }

    // ------------------------------------------------------------------
    // STATE: COMPANION
    // ------------------------------------------------------------------

    private void UpdateCompanionState(float distToPlayer)
    {
        companionTimer -= Time.deltaTime;
        if (companionTimer <= 0f || !playerTransform)
        {
            currentState = State.Idle;
            stateTimer = 2.5f;
            companionMoving = false;
            prompt = $"Tekan E untuk mengelus {animalName}";
            if (AgentReady) navAgent.ResetPath();
            return;
        }

        // Titik pendamping: di belakang-samping pemain
        Vector3 companionSlot = playerTransform.position
                              - playerTransform.forward * 1.5f
                              + playerTransform.right * 1.0f;
        companionSlot.y = 0f;

        float distToSlot = Vector3.Distance(Flat(transform.position), companionSlot);

        // Hysteresis: mulai jalan jika > 1.0 m, berhenti jika < 0.5 m (mencegah bergetar)
        if (!companionMoving && distToSlot > 1.0f) companionMoving = true;
        else if (companionMoving && distToSlot < 0.5f) companionMoving = false;

        if (companionMoving)
        {
            Vector3 dir = companionSlot - transform.position;

            if (AgentReady)
            {
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f)
                {
                    repathTimer = 0.25f;
                    navAgent.SetDestination(companionSlot);
                }

                if (!navAgent.pathPending && navAgent.hasPath)
                {
                    Vector3 steer = Flat(navAgent.steeringTarget - transform.position);
                    if (steer.sqrMagnitude > 0.01f) dir = steer;
                }
            }

            dir = AvoidObstacles(dir, out bool blocked);

            // Cepat jika pemain jauh, santai jika dekat
            float desired = Mathf.Clamp(distToSlot * 1.1f, moveSpeed * 0.7f, moveSpeed * 1.9f);
            Locomote(dir, blocked ? 0f : desired, 1.3f);
        }
        else
        {
            Decelerate(2.0f);
            if (AgentReady && navAgent.hasPath) navAgent.ResetPath();
            if (playerTransform) TurnTowards(playerTransform.position, 0.5f);
        }
    }

    // ------------------------------------------------------------------
    // RINTANGAN & TARGET
    // ------------------------------------------------------------------

    /// <summary>
    /// Sensor whisker: jika ada rintangan tinggi di depan, belok ke sisi (kiri/kanan 45 derajat) yang kosong.
    /// blocked = true jika kedua sisi buntu.
    /// </summary>
    private Vector3 AvoidObstacles(Vector3 desiredDir, out bool blocked)
    {
        blocked = false;

        Vector3 fwd = Flat(transform.forward).normalized;
        Vector3 origin = transform.position + Vector3.up * 0.35f;
        float checkDistance = 1.15f;

        if (Physics.SphereCast(origin, 0.22f, fwd, out RaycastHit hitFront, checkDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            bool isSelf = hitFront.transform.IsChildOf(transform);
            bool isLivingThing = hitFront.transform.CompareTag("Player") || hitFront.transform.CompareTag("NPC");
            bool isTall = hitFront.collider.bounds.max.y > 0.4f;

            if (!isSelf && !isLivingThing && isTall)
            {
                Vector3 leftWhisker = Quaternion.Euler(0, -45f, 0) * fwd;
                Vector3 rightWhisker = Quaternion.Euler(0, 45f, 0) * fwd;

                bool leftHit = Physics.Raycast(origin, leftWhisker, checkDistance * 1.2f, ~0, QueryTriggerInteraction.Ignore);
                bool rightHit = Physics.Raycast(origin, rightWhisker, checkDistance * 1.2f, ~0, QueryTriggerInteraction.Ignore);

                if (!leftHit) return leftWhisker;
                if (!rightHit) return rightWhisker;

                blocked = true;
            }
        }

        return desiredDir;
    }

    private Vector3 PickValidWanderTarget()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = new Vector3(homePosition.x + circle.x, 0f, homePosition.z + circle.y);

            // Hindari titik yang terlalu dekat (jalan pendek terlihat canggung)
            if (Vector3.Distance(Flat(transform.position), candidate) < 1.5f) continue;

            if (!Physics.CheckSphere(candidate + Vector3.up * 0.5f, 0.45f, ~0, QueryTriggerInteraction.Ignore))
            {
                return candidate;
            }
        }
        return homePosition;
    }

    private void ArriveAtIdle()
    {
        currentState = State.Idle;
        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
        if (AgentReady) navAgent.ResetPath();
        // currentSpeed tidak di-nol-kan paksa: Idle akan mengerem halus
    }

    // ------------------------------------------------------------------
    // GROUND
    // ------------------------------------------------------------------

    private void SnapToGround()
    {
        float targetY = 0f;

        Vector3 origin = transform.position + Vector3.up * 0.6f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 1.4f, ~0, QueryTriggerInteraction.Ignore);

        float highestGround = -999f;
        foreach (var h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;
            if (h.collider.isTrigger) continue;
            if (h.transform.CompareTag("Player") || h.transform.CompareTag("NPC")) continue;

            if (h.point.y <= 0.25f && h.point.y >= -0.15f)
            {
                if (h.point.y > highestGround) highestGround = h.point.y;
            }
        }

        if (highestGround > -900f) targetY = highestGround;

        targetY = Mathf.Clamp(targetY, 0f, 0.25f);
        currentGroundY = Mathf.MoveTowards(currentGroundY, targetY, Time.deltaTime * 8.0f);

        Vector3 pos = transform.position;
        pos.y = currentGroundY;
        transform.position = pos;
    }

    // ------------------------------------------------------------------
    // ANIMASI
    // ------------------------------------------------------------------

    private void UpdateAnimator()
    {
        // Hysteresis agar animasi tidak berkedip antara Idle <-> Walk
        if (!animMoving && currentSpeed > 0.08f) animMoving = true;
        else if (animMoving && currentSpeed < 0.03f) animMoving = false;

        SetAnimationMoving(animMoving);

        if (animator != null)
        {
            if (animator.applyRootMotion) animator.applyRootMotion = false;
            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Kecepatan playback = kecepatan nyata / kecepatan clip -> kaki tidak meluncur
            float ratio = animMoving
                ? Mathf.Clamp(currentSpeed / Mathf.Max(clipWalkSpeed, 0.01f), 0.5f, 1.8f)
                : 1.0f;
            animator.speed = Mathf.Lerp(animator.speed, ratio, Time.deltaTime * 8f);
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

        bool isRiggedFbx = visualTransform.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;

        float bounceY = 0f;
        if (petBounceTimer > 0f)
        {
            petBounceTimer -= Time.deltaTime;
            float amp = isRiggedFbx ? 0.06f : 0.15f;
            bounceY = Mathf.Sin((0.35f - petBounceTimer) / 0.35f * Mathf.PI) * amp;
        }

        visualTransform.localPosition = visualBasePosition + new Vector3(0f, bounceY, 0f);

        if (isRiggedFbx)
        {
            // Rotasi bone dikelola Animator; cukup napas halus (skala uniform)
            float breathe = 1.0f + Mathf.Sin(breatheCycle) * 0.008f;
            visualTransform.localScale = visualBaseScale * breathe;
        }
        else
        {
            float breathe = 1.0f + Mathf.Sin(breatheCycle) * 0.012f;
            visualTransform.localScale = new Vector3(
                visualBaseScale.x * breathe,
                visualBaseScale.y * (2f - breathe),
                visualBaseScale.z * breathe);

            float roll = (currentSpeed > 0.15f) ? Mathf.Sin(stepCycle) * 1.0f : 0f;
            visualTransform.localRotation = visualBaseRotation * Quaternion.Euler(0, 0, roll);
        }
    }

    // ------------------------------------------------------------------
    // INTERAKSI
    // ------------------------------------------------------------------

    public override void Interact(PlayerController player)
    {
        if (HintManager.Instance != null)
        {
            HintManager.Instance.Show($"pet_{animalName}", petMessage);
        }

        petBounceTimer = 0.35f;

        if (animalType == AnimalType.Dog && canFollowPlayer)
        {
            currentState = State.FollowingPlayer;
            companionTimer = companionDuration;
            companionMoving = false;
            prompt = $"{animalName} sedang menemanimu berjalan-jalan!";
        }
        else if (animalType == AnimalType.Cat)
        {
            currentState = State.Idle;
            stateTimer = 5.0f;
        }
    }
}
