using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Anjing prosedural yang jalan-jalan sendiri (Wander), atau bisa mengikuti target / dikontrol keyboard.
// Tempel ke GameObject kosong lalu Play. Kaki bergerak sesuai kecepatan, jadi tidak meluncur.
public class DogWalker : MonoBehaviour
{
    public enum ControlMode { Wander, FollowTarget, Keyboard }

    [Header("Mode")]
    public ControlMode mode = ControlMode.Wander;
    public Transform target;               // hanya dipakai di mode FollowTarget

    [Header("Jalan-jalan (Wander)")]
    public float wanderRadius = 8f;        // jarak maksimal dari titik awal
    public float minPause = 1.5f;          // lama berhenti (detik)
    public float maxPause = 5f;
    public float arriveDistance = 0.5f;
    [Range(0f, 1f)] public float sniffChance = 0.45f;
    public bool avoidObstacles = true;
    public LayerMask obstacleMask = ~0;
    public float lookAhead = 1.2f;         // jarak deteksi halangan di depan (ukuran lokal)

    [Header("Gerak")]
    public float walkSpeed = 1.2f;         // m/detik
    public float runSpeed = 3.0f;          // dipakai mode FollowTarget
    public float turnSpeed = 120f;         // derajat/detik
    public float acceleration = 4f;
    public float followDistance = 2.5f;
    public float backDistance = 1.2f;

    [Header("Animasi")]
    public float strideLength = 0.55f;     // kecilkan jika kaki terlihat meluncur ke depan
    public bool snapToGround = true;
    public LayerMask groundMask = ~0;
    public bool createGround = false;

    [Header("Warna")]
    public Color fur = new Color(0.34f, 0.19f, 0.08f);
    public Color dark = new Color(0.12f, 0.06f, 0.03f);
    public Color light = new Color(0.62f, 0.45f, 0.27f);
    public Color nose = new Color(0.01f, 0.01f, 0.01f);
    public Color eye = new Color(0.02f, 0.01f, 0.005f);

    const float TAU = Mathf.PI * 2f;
    readonly Dictionary<string, Transform> pivot = new Dictionary<string, Transform>();
    readonly Dictionary<string, Vector3> headPos = new Dictionary<string, Vector3>();
    readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
    readonly RaycastHit[] hits = new RaycastHit[12];
    Vector3 rootRest;
    Shader shader;
    float phase, speed, amp, idleTime;

    // keadaan jalan-jalan
    Vector3 home, waypoint, stuckPos;
    bool hasWaypoint, sniffing, avoiding;
    float pauseTimer, walkTimer, cruise, lookTimer, headYaw, headYawGoal, headPitch, headPitchGoal;
    float avoidDir, avoidTimer, clearTimer, stuckTimer;

    static Vector3 V(float x, float y, float z) { return new Vector3(x, z, y); }   // Blender -> Unity
    static Vector3 V(Vector3 b) { return new Vector3(b.x, b.z, b.y); }
    static Vector3 B(float x, float y, float z) { return new Vector3(x, y, z); }
    float Scale { get { return Mathf.Max(0.05f, Mathf.Abs(transform.lossyScale.y)); } }

    void Start()
    {
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        BuildBones();
        BuildBody();
        if (createGround) BuildGround();
        rootRest = pivot["root"].localPosition;

        home = transform.position;
        stuckPos = home;
        cruise = walkSpeed;
        pauseTimer = Random.Range(0.5f, 2f);   // mulai dengan berhenti sebentar

        if (mode == ControlMode.FollowTarget && target == null)
        {
            try
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            catch (UnityException) { }
            if (target == null) mode = ControlMode.Wander;
        }
    }

    // ------------------------------------------------------------ tulang
    void Bone(string name, string parent, float x, float y, float z)
    {
        var go = new GameObject(name);
        Vector3 head = new Vector3(x, y, z);
        Vector3 parentHead = parent == null ? Vector3.zero : headPos[parent];
        go.transform.SetParent(parent == null ? transform : pivot[parent], false);
        go.transform.localPosition = V(head - parentHead);
        headPos[name] = head;
        pivot[name] = go.transform;
    }

    void BuildBones()
    {
        Bone("root", null, 0, 0, 0.62f);
        Bone("spine", "root", 0, -0.52f, 0.64f);
        Bone("neck", "spine", 0, 0.32f, 0.66f);
        Bone("head", "neck", 0, 0.6f, 0.86f);
        Bone("tail1", "spine", 0, -0.52f, 0.68f);
        Bone("tail2", "tail1", 0, -0.7f, 0.82f);
        foreach (var s in new[] { new KeyValuePair<string, float>("L", 0.12f), new KeyValuePair<string, float>("R", -0.12f) })
        {
            Bone("F" + s.Key + "_up", "spine", s.Value, 0.38f, 0.6f);
            Bone("F" + s.Key + "_lo", "F" + s.Key + "_up", s.Value, 0.38f, 0.32f);
            Bone("B" + s.Key + "_up", "spine", s.Value, -0.4f, 0.6f);
            Bone("B" + s.Key + "_lo", "B" + s.Key + "_up", s.Value, -0.4f, 0.32f);
        }
    }

    // ------------------------------------------------------------ bentuk tubuh
    Material Mat(Color c)
    {
        Material m;
        if (!mats.TryGetValue(c, out m))
        {
            m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            mats[c] = m;
        }
        return m;
    }

    void Part(string bone, Vector3 center, Vector3 radii, Color col, Quaternion rot)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.name = bone + "_part";
        go.transform.SetParent(pivot[bone], false);
        go.transform.localPosition = V(center - headPos[bone]);
        go.transform.localRotation = rot;
        go.transform.localScale = 2f * V(radii);
        go.GetComponent<Renderer>().sharedMaterial = Mat(col);
    }
    void Part(string bone, Vector3 center, Vector3 radii, Color col) { Part(bone, center, radii, col, Quaternion.identity); }

    void BuildBody()
    {
        Part("spine", B(0, 0.24f, 0.62f), B(0.21f, 0.3f, 0.23f), fur);
        Part("spine", B(0, -0.04f, 0.58f), B(0.18f, 0.3f, 0.18f), fur);
        Part("spine", B(0, -0.32f, 0.62f), B(0.2f, 0.26f, 0.21f), fur);
        Part("neck", B(0, 0.5f, 0.78f), B(0.11f, 0.17f, 0.16f), light, Quaternion.Euler(-40, 0, 0));
        Part("head", B(0, 0.72f, 0.88f), B(0.12f, 0.14f, 0.12f), fur);
        Part("head", B(0, 0.88f, 0.83f), B(0.065f, 0.12f, 0.06f), light);
        Part("head", B(0, 0.995f, 0.835f), B(0.03f, 0.02f, 0.022f), nose);
        foreach (int s in new[] { 1, -1 })
        {
            Part("head", B(s * 0.075f, 0.82f, 0.915f), B(0.016f, 0.014f, 0.016f), eye);
            Part("head", B(s * 0.105f, 0.68f, 0.97f), B(0.025f, 0.065f, 0.11f), dark, Quaternion.Euler(0, 0, -s * 14));
        }
        Part("tail1", B(0, -0.6f, 0.75f), B(0.045f, 0.09f, 0.05f), fur, Quaternion.Euler(35, 0, 0));
        Part("tail2", B(0, -0.78f, 0.81f), B(0.04f, 0.09f, 0.04f), dark, Quaternion.Euler(-10, 0, 0));
        foreach (var sd in new[] { new KeyValuePair<string, float>("L", 0.12f), new KeyValuePair<string, float>("R", -0.12f) })
        {
            string L = sd.Key; float x = sd.Value;
            Part("F" + L + "_up", B(x * 1.1f, 0.38f, 0.45f), B(0.065f, 0.075f, 0.17f), fur);
            Part("F" + L + "_lo", B(x * 1.05f, 0.38f, 0.19f), B(0.04f, 0.045f, 0.15f), light);
            Part("F" + L + "_lo", B(x * 1.05f, 0.405f, 0.035f), B(0.052f, 0.085f, 0.037f), light);
            Part("B" + L + "_up", B(x * 1.15f, -0.4f, 0.46f), B(0.08f, 0.11f, 0.19f), fur);
            Part("B" + L + "_lo", B(x * 1.05f, -0.4f, 0.19f), B(0.038f, 0.045f, 0.15f), light);
            Part("B" + L + "_lo", B(x * 1.05f, -0.375f, 0.035f), B(0.05f, 0.085f, 0.037f), light);
        }
    }

    void BuildGround()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "Ground";
        g.transform.localScale = new Vector3(3, 1, 3);
        g.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.16f, 0.19f, 0.14f));
    }

    // ------------------------------------------------------------ deteksi halangan
    // Benda dianggap halangan jika berdiri tegak dan cukup tinggi (bukan lantai atau tonjolan batu kecil).
    bool IsObstacleHit(RaycastHit h)
    {
        float sc = Scale;
        if (h.distance <= 0f)    // bola sudah menempel di collider: pakai tinggi collider
            return h.collider.bounds.max.y > transform.position.y + 0.3f * sc;
        if (h.normal.y > 0.6f) return false;
        if (h.point.y < transform.position.y + 0.18f * sc) return false;
        return true;
    }

    bool PathBlocked(Vector3 origin, Vector3 dir, float dist, float radius)
    {
        int n = Physics.SphereCastNonAlloc(origin, radius, dir, hits, dist, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (IsObstacleHit(hits[i])) return true;
        return false;
    }

    Vector3 SensorOrigin()
    {
        float sc = Scale;
        return transform.position + transform.forward * 0.5f * sc + Vector3.up * 0.45f * sc;
    }

    // angle: 0 = lurus ke depan, negatif = kiri, positif = kanan
    bool Blocked(float angle = 0f)
    {
        if (!avoidObstacles) return false;
        float sc = Scale;
        Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
        return PathBlocked(SensorOrigin(), dir, lookAhead * sc, 0.25f * sc);
    }

    // ------------------------------------------------------------ keputusan gerak
    void ReadKeyboard(out float v, out float h)
    {
        v = 0f; h = 0f;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
#else
        v = Input.GetAxisRaw("Vertical");
        h = Input.GetAxisRaw("Horizontal");
#endif
    }

    void Decide(float dt, out float wantSpeed, out float turn)
    {
        wantSpeed = 0f; turn = 0f;
        switch (mode)
        {
            case ControlMode.Keyboard:
            {
                float v, h;
                ReadKeyboard(out v, out h);
                wantSpeed = v * walkSpeed;
                turn = h;
                break;
            }
            case ControlMode.FollowTarget:
                DecideFollow(out wantSpeed, out turn);
                break;
            default:
                DecideWander(dt, out wantSpeed, out turn);
                break;
        }
    }

    void DecideFollow(out float wantSpeed, out float turn)
    {
        wantSpeed = 0f; turn = 0f;
        if (target == null) return;
        Vector3 to = target.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;
        float angle = Vector3.SignedAngle(transform.forward, to, Vector3.up);
        turn = Mathf.Clamp(angle / 30f, -1f, 1f);
        if (dist > followDistance)
        {
            float facing = Mathf.Clamp01(1f - Mathf.Abs(angle) / 90f);
            float far = Mathf.Clamp01((dist - followDistance) / 3f);
            wantSpeed = Mathf.Lerp(walkSpeed, runSpeed, far) * facing;
        }
        else if (dist < backDistance)
        {
            wantSpeed = -walkSpeed * 0.6f;
        }
    }

    // Pilih tujuan baru. ahead = true: dekat dan searah hadap (dipakai setelah menghindar).
    void PickWaypoint(bool ahead)
    {
        float sc = Scale;
        Vector3 origin = transform.position + Vector3.up * 0.45f * sc;
        bool found = false;
        Vector3 best = transform.position;

        for (int i = 0; i < 10 && !found; i++)
        {
            Vector3 dir = ahead
                ? Quaternion.AngleAxis(Random.Range(-35f, 35f), Vector3.up) * transform.forward
                : Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * Vector3.forward;
            float dist = ahead ? Random.Range(2f, 4f) * sc : Random.Range(wanderRadius * 0.3f, wanderRadius);

            Vector3 p = transform.position + dir * dist;
            Vector3 off = p - home; off.y = 0f;
            if (off.magnitude > wanderRadius) p = home + off.normalized * wanderRadius;

            Vector3 d = p - transform.position; d.y = 0f;
            float len = d.magnitude;
            if (len < 1f * sc) continue;

            if (avoidObstacles)
            {
                if (PathBlocked(origin, d / len, len, 0.25f * sc)) continue;
                if (Physics.CheckSphere(p + Vector3.up * 0.5f * sc, 0.3f * sc, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
            }
            best = p;
            found = true;
        }

        if (!found)
        {
            StartPause(0.5f, 1.2f);     // belum ada jalur bebas: diam sebentar lalu coba lagi
            return;
        }

        waypoint = best;
        hasWaypoint = true;
        walkTimer = 0f;
        stuckTimer = 0f;
        stuckPos = transform.position;
        cruise = Random.Range(0.7f, 1.15f) * walkSpeed;
    }

    void StartPause(float min, float max)
    {
        hasWaypoint = false;
        pauseTimer = Random.Range(min, max);
        sniffing = Random.value < sniffChance;
        lookTimer = 0f;
    }

    // Mulai berputar di tempat ke sisi yang lebih lega.
    void BeginAvoid()
    {
        avoiding = true;
        avoidTimer = 0f;
        clearTimer = 0f;
        hasWaypoint = false;
        bool leftBlocked = Blocked(-50f);
        bool rightBlocked = Blocked(50f);
        if (leftBlocked && !rightBlocked) avoidDir = 1f;            // kiri tertutup: belok kanan
        else if (rightBlocked && !leftBlocked) avoidDir = -1f;      // kanan tertutup: belok kiri
        else avoidDir = Random.value < 0.5f ? -1f : 1f;
    }

    void DecideWander(float dt, out float wantSpeed, out float turn)
    {
        wantSpeed = 0f; turn = 0f;

        // 1) sedang menghindar: berputar di tempat sampai depan kosong
        if (avoiding)
        {
            headYawGoal = 0f; headPitchGoal = 0f;
            avoidTimer += dt;
            turn = avoidDir;
            clearTimer = Blocked() ? 0f : clearTimer + dt;
            if (clearTimer > 0.25f || avoidTimer > 4f)
            {
                avoiding = false;
                PickWaypoint(true);
            }
            return;
        }

        // 2) berhenti: menoleh dan mengendus
        if (pauseTimer > 0f)
        {
            pauseTimer -= dt;
            lookTimer -= dt;
            if (lookTimer <= 0f)
            {
                lookTimer = Random.Range(1f, 2.2f);
                headYawGoal = Random.Range(-35f, 35f);
            }
            headPitchGoal = sniffing ? 22f + 3f * Mathf.Sin(Time.time * 12f) : 0f;
            return;
        }

        // 3) berjalan ke tujuan
        headYawGoal = 0f; headPitchGoal = 0f;
        if (!hasWaypoint)
        {
            PickWaypoint(false);
            if (!hasWaypoint) return;
        }

        walkTimer += dt;
        Vector3 to = waypoint - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        if (dist < arriveDistance * Scale || walkTimer > 20f)
        {
            StartPause(minPause, maxPause);
            return;
        }

        if (Blocked())
        {
            BeginAvoid();
            return;
        }

        float angle = Vector3.SignedAngle(transform.forward, to, Vector3.up);
        turn = Mathf.Clamp(angle / 30f, -1f, 1f);
        float facing = Mathf.Clamp01(1f - Mathf.Abs(angle) / 70f);   // berbelok dulu baru melangkah
        wantSpeed = cruise * facing;

        // deteksi macet: sudah menghadap tujuan tapi hampir tidak berpindah
        if (Mathf.Abs(angle) > 30f)
        {
            stuckTimer = 0f;
            stuckPos = transform.position;
        }
        else
        {
            stuckTimer += dt;
            if (stuckTimer > 1.5f)
            {
                if ((transform.position - stuckPos).magnitude < 0.2f * Scale)
                {
                    BeginAvoid();
                    wantSpeed = 0f; turn = avoidDir;
                }
                stuckTimer = 0f;
                stuckPos = transform.position;
            }
        }
    }

    // ------------------------------------------------------------ gerak + animasi
    void Update()
    {
        if (pivot.Count == 0) return;
        float dt = Time.deltaTime;

        float wantSpeed, turn;
        Decide(dt, out wantSpeed, out turn);

        float rate = Mathf.Abs(wantSpeed) < Mathf.Abs(speed) ? acceleration * 2.5f : acceleration;   // mengerem lebih cepat
        speed = Mathf.MoveTowards(speed, wantSpeed, rate * dt);
        transform.Rotate(0f, turn * turnSpeed * dt, 0f);
        transform.position += transform.forward * speed * dt;

        if (snapToGround)
        {
            // hanya permukaan setinggi langkah kaki, jadi atap atau benda tinggi diabaikan
            float sc = Scale;
            float step = 0.6f * sc;
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * step, Vector3.down, out hit, step + 2f * sc,
                                groundMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.5f)
            {
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, hit.point.y, 1f - Mathf.Exp(-14f * dt));
                transform.position = p;
            }
        }

        // berputar di tempat: kaki ikut melangkah
        bool turningInPlace = Mathf.Abs(speed) < 0.15f && Mathf.Abs(turn) > 0.35f;
        phase += speed * dt / (strideLength * Scale);
        if (turningInPlace) phase += dt / 0.9f;
        float targetAmp = turningInPlace ? 0.6f : Mathf.Clamp01(Mathf.Abs(speed) / 0.35f);
        amp = Mathf.MoveTowards(amp, targetAmp, 6f * dt);
        idleTime += dt;

        headYaw = Mathf.Lerp(headYaw, headYawGoal, 3f * dt);
        headPitch = Mathf.Lerp(headPitch, headPitchGoal, 4f * dt);

        Animate(Mathf.Repeat(phase, 1f));
    }

    static Quaternion RotX(float deg) { return Quaternion.Euler(-deg, 0, 0); }

    void Animate(float t)
    {
        Leg("BL", t, 0.00f, 22f, 45f);
        Leg("FL", t, 0.25f, 26f, 55f);
        Leg("BR", t, 0.50f, 22f, 45f);
        Leg("FR", t, 0.75f, 26f, 55f);

        float bob = 0.012f * Mathf.Cos(TAU * 2f * t) * amp;
        pivot["root"].localPosition = rootRest + Vector3.up * bob;
        pivot["spine"].localRotation = Quaternion.Euler(0, 2.5f * Mathf.Sin(TAU * t) * amp, 0);
        pivot["neck"].localRotation = RotX(3f * Mathf.Sin(TAU * 2f * t + 0.6f) * amp);

        // kepala: goyangan jalan + menoleh dan mengendus saat berhenti
        pivot["head"].localRotation = RotX(-3f * Mathf.Sin(TAU * 2f * t + 0.6f) * amp)
                                      * RotX(-headPitch) * Quaternion.Euler(0, headYaw, 0);

        float wagRate = Mathf.Lerp(3f, 9f, amp);
        float wag = Mathf.Sin(idleTime * wagRate);
        pivot["tail1"].localRotation = Quaternion.Euler(0, Mathf.Lerp(12f, 22f, amp) * wag, 0);
        pivot["tail2"].localRotation = Quaternion.Euler(0, Mathf.Lerp(10f, 18f, amp) * Mathf.Sin(idleTime * wagRate - 0.8f), 0);
    }

    void Leg(string leg, float t, float ph, float swingDeg, float flexDeg)
    {
        float p = TAU * (t - ph);
        pivot[leg + "_up"].localRotation = RotX(swingDeg * Mathf.Sin(p) * amp);
        float lift = Mathf.Max(0f, Mathf.Cos(p));
        pivot[leg + "_lo"].localRotation = RotX(-flexDeg * lift * lift * amp);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, wanderRadius);
        if (!Application.isPlaying) return;
        if (hasWaypoint) Gizmos.DrawSphere(waypoint + Vector3.up * 0.1f, 0.12f);

        // bola deteksi halangan: merah = terhalang, biru muda = aman
        float sc = Scale;
        Gizmos.color = Blocked() ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(SensorOrigin() + transform.forward * lookAhead * sc, 0.25f * sc);
    }
}
