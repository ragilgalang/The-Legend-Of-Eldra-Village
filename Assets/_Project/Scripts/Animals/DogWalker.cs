using System.Collections.Generic;
using UnityEngine;

// Anjing prosedural dengan siklus jalan. Tempel ke GameObject kosong, lalu Play.
// Koordinat data dari skrip Blender (Z atas, menghadap +Y) diubah ke Unity (Y atas, menghadap +Z).
public class DogWalker : MonoBehaviour
{
    [Header("Gerak")]
    public float cycleSeconds = 2f;      // durasi satu siklus langkah
    public float animSpeed = 1f;         // pengali kecepatan animasi
    public bool moveForward = false;     // true = anjing benar-benar maju
    public float walkSpeed = 0.5f;       // m/detik jika moveForward aktif

    [Header("Warna")]
    public Color fur = new Color(0.34f, 0.19f, 0.08f);
    public Color dark = new Color(0.12f, 0.06f, 0.03f);
    public Color light = new Color(0.62f, 0.45f, 0.27f);
    public Color nose = new Color(0.01f, 0.01f, 0.01f);
    public Color eye = new Color(0.02f, 0.01f, 0.005f);

    [Header("Lainnya")]
    public bool createGround = true;

    const float TAU = Mathf.PI * 2f;
    readonly Dictionary<string, Transform> pivot = new Dictionary<string, Transform>();
    readonly Dictionary<string, Vector3> headPos = new Dictionary<string, Vector3>();   // posisi kepala tulang (koordinat Blender)
    readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
    Vector3 rootRest;
    Shader shader;

    static Vector3 V(float x, float y, float z) { return new Vector3(x, z, y); }   // Blender -> Unity
    static Vector3 V(Vector3 b) { return new Vector3(b.x, b.z, b.y); }

    void Start()
    {
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        BuildBones();
        BuildBody();
        if (createGround) BuildGround();
        rootRest = pivot["root"].localPosition;
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

    // centre dan radii memakai koordinat Blender (x, y, z)
    void Part(string bone, Vector3 center, Vector3 radii, Color col, Quaternion rot)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.name = bone + "_part";
        go.transform.SetParent(pivot[bone], false);
        go.transform.localPosition = V(center - headPos[bone]);
        go.transform.localRotation = rot;
        go.transform.localScale = 2f * V(radii);        // bola Unity berdiameter 1
        go.GetComponent<Renderer>().sharedMaterial = Mat(col);
    }
    void Part(string bone, Vector3 center, Vector3 radii, Color col) { Part(bone, center, radii, col, Quaternion.identity); }
    static Vector3 B(float x, float y, float z) { return new Vector3(x, y, z); }

    void BuildBody()
    {
        // badan
        Part("spine", B(0, 0.24f, 0.62f), B(0.21f, 0.3f, 0.23f), fur);
        Part("spine", B(0, -0.04f, 0.58f), B(0.18f, 0.3f, 0.18f), fur);
        Part("spine", B(0, -0.32f, 0.62f), B(0.2f, 0.26f, 0.21f), fur);
        Part("neck", B(0, 0.5f, 0.78f), B(0.11f, 0.17f, 0.16f), light, Quaternion.Euler(-40, 0, 0));
        // kepala
        Part("head", B(0, 0.72f, 0.88f), B(0.12f, 0.14f, 0.12f), fur);
        Part("head", B(0, 0.88f, 0.83f), B(0.065f, 0.12f, 0.06f), light);
        Part("head", B(0, 0.995f, 0.835f), B(0.03f, 0.02f, 0.022f), nose);
        foreach (int s in new[] { 1, -1 })
        {
            Part("head", B(s * 0.075f, 0.82f, 0.915f), B(0.016f, 0.014f, 0.016f), eye);
            Part("head", B(s * 0.105f, 0.68f, 0.97f), B(0.025f, 0.065f, 0.11f), dark, Quaternion.Euler(0, 0, -s * 14));
        }
        // ekor
        Part("tail1", B(0, -0.6f, 0.75f), B(0.045f, 0.09f, 0.05f), fur, Quaternion.Euler(35, 0, 0));
        Part("tail2", B(0, -0.78f, 0.81f), B(0.04f, 0.09f, 0.04f), dark, Quaternion.Euler(-10, 0, 0));
        // kaki
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

    // ------------------------------------------------------------ animasi
    // Blender: rotasi X positif membawa kaki ke depan. Di Unity tandanya dibalik (-sudut).
    static Quaternion RotX(float deg) { return Quaternion.Euler(-deg, 0, 0); }

    void Update()
    {
        if (pivot.Count == 0) return;
        float t = Mathf.Repeat(Time.time * animSpeed / cycleSeconds, 1f);

        // urutan jejak kaki: belakang kiri, depan kiri, belakang kanan, depan kanan
        Leg("BL", t, 0.00f, 22f, 45f);
        Leg("FL", t, 0.25f, 26f, 55f);
        Leg("BR", t, 0.50f, 22f, 45f);
        Leg("FR", t, 0.75f, 26f, 55f);

        pivot["root"].localPosition = rootRest + Vector3.up * 0.012f * Mathf.Cos(TAU * 2f * t);
        pivot["spine"].localRotation = Quaternion.Euler(0, 2.5f * Mathf.Sin(TAU * t), 0);
        pivot["neck"].localRotation = RotX(3f * Mathf.Sin(TAU * 2f * t + 0.6f));
        pivot["head"].localRotation = RotX(-3f * Mathf.Sin(TAU * 2f * t + 0.6f));
        pivot["tail1"].localRotation = Quaternion.Euler(0, 22f * Mathf.Sin(TAU * 2f * t), 0);
        pivot["tail2"].localRotation = Quaternion.Euler(0, 18f * Mathf.Sin(TAU * 2f * t - 0.8f), 0);

        if (moveForward) transform.position += transform.forward * walkSpeed * animSpeed * Time.deltaTime;
    }

    void Leg(string leg, float t, float phase, float swingDeg, float flexDeg)
    {
        float p = TAU * (t - phase);
        pivot[leg + "_up"].localRotation = RotX(swingDeg * Mathf.Sin(p));
        float lift = Mathf.Max(0f, Mathf.Cos(p));
        pivot[leg + "_lo"].localRotation = RotX(-flexDeg * lift * lift);
    }
}
