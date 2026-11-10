using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Anjing prosedural yang berjalan maju/mundur seperti hewan sungguhan.
// Tempel ke GameObject kosong lalu Play. Kaki bergerak sesuai kecepatan, jadi tidak meluncur.
public class DogWalker : MonoBehaviour
{
    public enum ControlMode { FollowTarget, Keyboard }

    [Header("Kontrol")]
    public ControlMode mode = ControlMode.FollowTarget;
    public Transform target;               // yang diikuti (mis. pemain)
    public float followDistance = 2.5f;    // lebih jauh dari ini: anjing maju
    public float backDistance = 1.2f;      // lebih dekat dari ini: anjing mundur
    public float walkSpeed = 1.2f;         // m/detik saat berjalan
    public float runSpeed = 3.0f;          // m/detik saat target jauh
    public float turnSpeed = 120f;         // derajat/detik
    public float acceleration = 4f;        // percepatan/perlambatan

    [Header("Animasi")]
    public float strideLength = 0.55f;     // jarak (ukuran lokal) per satu siklus langkah. Kecilkan jika kaki terlihat meluncur ke depan
    public bool snapToGround = true;       // menempel ke tanah dengan raycast
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
    Vector3 rootRest;
    Shader shader;
    float phase;          // fase siklus langkah (bisa maju atau mundur)
    float speed;          // kecepatan saat ini (negatif = mundur)
    float amp;            // 0 = diam, 1 = berjalan penuh
    float idleTime;

    static Vector3 V(float x, float y, float z) { return new Vector3(x, z, y); }   // Blender -> Unity
    static Vector3 V(Vector3 b) { return new Vector3(b.x, b.z, b.y); }
    static Vector3 B(float x, float y, float z) { return new Vector3(x, y, z); }

    void Start()
    {
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        BuildBones();
        BuildBody();
        if (createGround) BuildGround();
        rootRest = pivot["root"].localPosition;

        if (mode == ControlMode.FollowTarget && target == null)
        {
            try
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            catch (UnityException) { /* tag Player belum ada */ }
            if (target == null) mode = ControlMode.Keyboard;
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

    // ------------------------------------------------------------ kontrol
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

    // Menghasilkan kecepatan tujuan (negatif = mundur) dan arah belok (-1..1)
    void Decide(out float wantSpeed, out float turn)
    {
        wantSpeed = 0f; turn = 0f;
        if (mode == ControlMode.Keyboard || target == null)
        {
            float v, h;
            ReadKeyboard(out v, out h);
            wantSpeed = v * walkSpeed;
            turn = h;
            return;
        }

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        float angle = Vector3.SignedAngle(transform.forward, to, Vector3.up);
        turn = Mathf.Clamp(angle / 30f, -1f, 1f);

        if (dist > followDistance)
        {
            // jangan berlari lurus sebelum badan menghadap target
            float facing = Mathf.Clamp01(1f - Mathf.Abs(angle) / 90f);
            float far = Mathf.Clamp01((dist - followDistance) / 3f);
            wantSpeed = Mathf.Lerp(walkSpeed, runSpeed, far) * facing;
        }
        else if (dist < backDistance)
        {
            wantSpeed = -walkSpeed * 0.6f;      // mundur sambil tetap menghadap target
        }
    }

    // ------------------------------------------------------------ gerak + animasi
    void Update()
    {
        if (pivot.Count == 0) return;
        float dt = Time.deltaTime;

        float wantSpeed, turn;
        Decide(out wantSpeed, out turn);

        speed = Mathf.MoveTowards(speed, wantSpeed, acceleration * dt);
        transform.Rotate(0f, turn * turnSpeed * dt, 0f);
        transform.position += transform.forward * speed * dt;

        if (snapToGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 6f, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = transform.position;
                p.y = hit.point.y;
                transform.position = p;
            }
        }

        // fase langkah mengikuti jarak tempuh: maju = fase naik, mundur = fase turun
        float worldStride = Mathf.Max(0.05f, strideLength * Mathf.Abs(transform.lossyScale.y));
        phase += speed * dt / worldStride;

        float targetAmp = Mathf.Clamp01(Mathf.Abs(speed) / 0.35f);
        amp = Mathf.MoveTowards(amp, targetAmp, 6f * dt);
        idleTime += dt;

        Animate(Mathf.Repeat(phase, 1f));
    }

    static Quaternion RotX(float deg) { return Quaternion.Euler(-deg, 0, 0); }

    void Animate(float t)
    {
        // urutan jejak kaki: belakang kiri, depan kiri, belakang kanan, depan kanan
        Leg("BL", t, 0.00f, 22f, 45f);
        Leg("FL", t, 0.25f, 26f, 55f);
        Leg("BR", t, 0.50f, 22f, 45f);
        Leg("FR", t, 0.75f, 26f, 55f);

        float bob = 0.012f * Mathf.Cos(TAU * 2f * t) * amp;
        pivot["root"].localPosition = rootRest + Vector3.up * bob;
        pivot["spine"].localRotation = Quaternion.Euler(0, 2.5f * Mathf.Sin(TAU * t) * amp, 0);
        pivot["neck"].localRotation = RotX(3f * Mathf.Sin(TAU * 2f * t + 0.6f) * amp);
        pivot["head"].localRotation = RotX(-3f * Mathf.Sin(TAU * 2f * t + 0.6f) * amp);

        // ekor tetap mengibas pelan saat diam, lebih cepat saat berjalan
        float wag = Mathf.Sin(idleTime * Mathf.Lerp(3f, 9f, amp));
        pivot["tail1"].localRotation = Quaternion.Euler(0, Mathf.Lerp(12f, 22f, amp) * wag, 0);
        pivot["tail2"].localRotation = Quaternion.Euler(0, Mathf.Lerp(10f, 18f, amp) * Mathf.Sin(idleTime * Mathf.Lerp(3f, 9f, amp) - 0.8f), 0);
    }

    void Leg(string leg, float t, float ph, float swingDeg, float flexDeg)
    {
        float p = TAU * (t - ph);
        pivot[leg + "_up"].localRotation = RotX(swingDeg * Mathf.Sin(p) * amp);
        float lift = Mathf.Max(0f, Mathf.Cos(p));
        pivot[leg + "_lo"].localRotation = RotX(-flexDeg * lift * lift * amp);
    }
}
