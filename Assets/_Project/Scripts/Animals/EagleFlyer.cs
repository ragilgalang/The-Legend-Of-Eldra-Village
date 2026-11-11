using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elang botak prosedural realistis: badan berlekuk, bulu berlapis (primer, sekunder, coverts, ekor mengipas),
/// kaki bercakar, mata tajam elang, kepakan sayap dinamis, melayang anggun (thermal soaring), dan terbang melingkar.
///
/// Dikonversi langsung dari script Blender eagle_fly.py.
/// Tempel ke GameObject kosong lalu Play, atau gunakan lewat SceneSetupTool/AnimalAssetSetup.
/// </summary>
public class EagleFlyer : MonoBehaviour
{
    [Header("Lintasan Terbang")]
    public Vector3 flightCenter = new Vector3(0f, 26f, 0f);
    public float orbitRadius = 38.0f;
    public float orbitHeight = 26.0f;
    public float orbitDuration = 14.0f;      // detik per satu putaran melingkar penuh
    public float bankAngle = 18.0f;           // kemiringan badan ke dalam saat berbelok (derajat)
    public bool updatePosition = true;

    [Header("Kepakan & Melayang (Soaring)")]
    public float flapDuration = 1.25f;        // frame FLAP=30 pada 24fps -> 1.25 detik per kepakan
    public float glideCycleDuration = 5.0f;   // frame 120 pada 24fps -> 5 detik per siklus kepak & melayang
    public bool continuousFlapping = false;   // jika true, mengepak terus menerus
    public float animSpeed = 1.0f;

    [Header("Ukuran & Skala")]
    public float eagleScale = 1.4f;           // pengali skala agar elang tampak megah dari tanah desa

    const float TAU = Mathf.PI * 2f;
    readonly Dictionary<string, Transform> pivots = new Dictionary<string, Transform>();
    readonly Dictionary<string, Vector3> pivotWorldBlender = new Dictionary<string, Vector3>();
    readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    Shader shader;
    Vector3 bodyRestLocalPos;
    bool isBuilt = false;

    // Blender (X kanan, Y depan, Z atas) -> Unity (X kanan, Y atas, Z depan)
    static Vector3 V(float x, float y, float z) { return new Vector3(x, z, y); }
    static Vector3 V(Vector3 b) { return new Vector3(b.x, b.z, b.y); }

    // ------------------------------------------------------------------ guard NaN
    static float Safe(float v, float fallback = 0f)
    {
        return float.IsNaN(v) || float.IsInfinity(v) ? fallback : v;
    }
    static Vector3 SafeVec(Vector3 v)
    {
        return new Vector3(Safe(v.x), Safe(v.y), Safe(v.z));
    }

    void Awake()
    {
        EnsureBuilt();
    }

    void Start()
    {
        EnsureBuilt();
    }

    public void EnsureBuilt()
    {
        if (isBuilt && pivots.ContainsKey("shR") && pivots["shR"] != null) return;
        BuildEagle();
    }

    [ContextMenu("Rebuild Eagle")]
    public void BuildEagle()
    {
        pivots.Clear();
        pivotWorldBlender.Clear();
        mats.Clear();

        // Hapus model lama agar selalu bersih
        var toDestroy = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            toDestroy.Add(child);
        }
        foreach (var c in toDestroy)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        InitMaterials();
        BuildRigHierarchy();
        BuildPlumageAndAnatomy();

        transform.localScale = Vector3.one * eagleScale;
        isBuilt = true;
    }

    // ------------------------------------------------------------ Tekstur & Material
    void InitMaterials()
    {
        // Coba shader URP Lit, lalu HDRP, lalu Standard
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Color Hex(string hx)
        {
            ColorUtility.TryParseHtmlString("#" + hx, out Color c);
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        }

        Material MakeMat(string name, Color col, float smoothness = 0f)
        {
            var m = new Material(shader) { name = name };

            // Warna dasar
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color"))     m.SetColor("_Color", col);
            m.color = col;

            // Matte total: smoothness=0, metallic=0
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0f);

            // Matikan kilau spekular & pantulan lingkungan
            if (m.HasProperty("_SpecularHighlights"))     m.SetFloat("_SpecularHighlights", 0f);
            if (m.HasProperty("_GlossyReflections"))      m.SetFloat("_GlossyReflections", 0f);
            if (m.HasProperty("_EnvironmentReflections")) m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");

            // ZERO emisi — tidak ada cahaya yang dipancarkan sama sekali
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            m.DisableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            // Single-sided (Back culling)
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 2f);

            mats[name] = m;
            return m;
        }

        MakeMat("body",   Hex("2e1c0e"), 0f);   // Bulu badan cokelat tua pekat — matte
        MakeMat("pants",  Hex("3d2613"), 0f);   // Bulu paha cokelat tanah
        MakeMat("white",  Hex("b0aea6"), 0f);   // Kepala putih gading abu (lebih gelap, anti-bloom)
        MakeMat("yellow", Hex("9a6e0a"), 0.1f); // Paruh & cakar kuning keemasan redup
        MakeMat("black",  Hex("101010"), 0f);   // Ujung cakar hitam
        MakeMat("eye",    Hex("7a5810"), 0.3f); // Mata elang amber gelap
        MakeMat("lead",   Hex("331f0f"), 0f);   // Tepi depan sayap
        MakeMat("sec",    Hex("22140a"), 0f);   // Bulu sekunder gelap
        MakeMat("prim",   Hex("140a04"), 0f);   // Bulu primer hitam kecokelatan
        MakeMat("gc",     Hex("2a190c"), 0f);   // Coverts besar
        MakeMat("mc",     Hex("331e0e"), 0f);   // Coverts tengah
        MakeMat("lc",     Hex("3d2512"), 0f);   // Coverts kecil
        MakeMat("tail",   Hex("b8b6ae"), 0f);   // Ekor putih gading redup (anti-bloom)
    }

    // ------------------------------------------------------------ Rig Hierarchy (Pivots)
    void Pivot(string name, string parent, Vector3 blenderPos)
    {
        var go = new GameObject(name);
        go.hideFlags = HideFlags.DontSave;
        Vector3 parentBlenderPos = Vector3.zero;
        if (string.IsNullOrEmpty(parent))
        {
            go.transform.SetParent(transform, false);
        }
        else
        {
            go.transform.SetParent(pivots[parent], false);
            parentBlenderPos = pivotWorldBlender[parent];
        }

        go.transform.localPosition = V(blenderPos - parentBlenderPos);
        go.transform.localRotation = Quaternion.identity;
        pivots[name] = go.transform;
        pivotWorldBlender[name] = blenderPos;
    }

    void BuildRigHierarchy()
    {
        pivots["path"] = transform;
        pivotWorldBlender["path"] = Vector3.zero;

        Pivot("bank", "path", Vector3.zero);
        Pivot("body", "bank", Vector3.zero);
        Pivot("neck", "body", new Vector3(0f, 0.37f, 0.10f));
        Pivot("head", "neck", new Vector3(0f, 0.50f, 0.16f));
        Pivot("tail", "body", new Vector3(0f, -0.34f, 0f));

        // Sayap Kanan (+X)
        Pivot("shR", "body", new Vector3(0.12f, 0.08f, 0.06f));
        Pivot("elR", "shR",  new Vector3(0.50f, 0.06f, 0.07f));
        Pivot("wrR", "elR",  new Vector3(0.85f, 0.07f, 0.06f));

        // Sayap Kiri (-X)
        Pivot("shL", "body", new Vector3(-0.12f, 0.08f, 0.06f));
        Pivot("elL", "shL",  new Vector3(-0.50f, 0.06f, 0.07f));
        Pivot("wrL", "elL",  new Vector3(-0.85f, 0.07f, 0.06f));

        bodyRestLocalPos = pivots["body"].localPosition;
    }

    // ------------------------------------------------------------ Pembangun Mesh Prosedural
    GameObject AddPart(string parent, Mesh mesh, Material mat,
                       Vector3 blenderPos = default, Quaternion localRot = default)
    {
        if (mesh == null) return null;

        var go = new GameObject(mesh.name);
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(pivots[parent], false);
        Vector3 pp = pivotWorldBlender[parent];
        go.transform.localPosition = V(blenderPos - pp);
        go.transform.localRotation = localRot == default ? Quaternion.identity : localRot;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.receiveShadows = true;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return go;
    }

    Mesh BuildLoftMesh(string name, Vector4[] secs, int seg = 16)
    {
        var mesh = new Mesh { name = name };
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        for (int i = 0; i < secs.Length; i++)
        {
            float fy = secs[i].x;
            float rx = secs[i].y;
            float rz = secs[i].z;
            float cz = secs[i].w;

            for (int j = 0; j < seg; j++)
            {
                float a = TAU * j / seg;
                verts.Add(V(Safe(Mathf.Cos(a) * rx), Safe(fy), Safe(cz + Mathf.Sin(a) * rz)));
            }
        }

        for (int i = 0; i < secs.Length - 1; i++)
        {
            for (int j = 0; j < seg; j++)
            {
                int a0 = i * seg + j;
                int a1 = i * seg + (j + 1) % seg;
                int b0 = (i + 1) * seg + j;
                int b1 = (i + 1) * seg + (j + 1) % seg;

                tris.Add(a0); tris.Add(b1); tris.Add(b0);
                tris.Add(a0); tris.Add(a1); tris.Add(b1);
            }
        }

        // Tutup ujung belakang & depan
        int capRear = verts.Count;
        verts.Add(V(0f, secs[0].x, secs[0].w));
        for (int j = 0; j < seg; j++) { tris.Add(capRear); tris.Add((j + 1) % seg); tris.Add(j); }

        int capFront = verts.Count;
        verts.Add(V(0f, secs[secs.Length - 1].x, secs[secs.Length - 1].w));
        int lastRow = (secs.Length - 1) * seg;
        for (int j = 0; j < seg; j++) { tris.Add(capFront); tris.Add(lastRow + j); tris.Add(lastRow + (j + 1) % seg); }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return ForceSafeBounds(mesh);
    }

    Mesh BuildTubeMesh(string name, Vector3[] pts, float[] radii, int seg = 10)
    {
        var mesh  = new Mesh { name = name };
        var verts = new List<Vector3>();
        var tris  = new List<int>();
        int n = pts.Length;

        for (int i = 0; i < n; i++)
        {
            Vector3 p     = pts[i];
            Vector3 nextP = pts[Mathf.Min(i + 1, n - 1)];
            Vector3 prevP = pts[Mathf.Max(i - 1, 0)];
            Vector3 t = (nextP - prevP).normalized;
            if (t.sqrMagnitude < 1e-4f) t = Vector3.forward;

            Vector3 refVec = Mathf.Abs(t.z) > 0.9f ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
            Vector3 nn = Vector3.Cross(t, refVec).normalized;
            if (nn.sqrMagnitude < 1e-4f) nn = Vector3.up;
            Vector3 bn = Vector3.Cross(t, nn).normalized;
            if (bn.sqrMagnitude < 1e-4f) bn = Vector3.right;

            float r = radii[i];
            for (int j = 0; j < seg; j++)
            {
                float a = TAU * j / seg;
                Vector3 ringPt = SafeVec(p + nn * Mathf.Cos(a) * r + bn * Mathf.Sin(a) * r);
                verts.Add(V(ringPt));
            }
        }

        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < seg; j++)
            {
                int a0 = i * seg + j;
                int a1 = i * seg + (j + 1) % seg;
                int b0 = (i + 1) * seg + j;
                int b1 = (i + 1) * seg + (j + 1) % seg;
                tris.Add(a0); tris.Add(b1); tris.Add(a1);
                tris.Add(a0); tris.Add(b0); tris.Add(b1);
            }
        }

        // Cap ujung pangkal dan ujung akhir
        int capStart = verts.Count;
        verts.Add(V(SafeVec(pts[0])));
        for (int j = 0; j < seg; j++) { tris.Add(capStart); tris.Add((j + 1) % seg); tris.Add(j); }

        int capEnd   = verts.Count;
        verts.Add(V(SafeVec(pts[n - 1])));
        int lastRing = (n - 1) * seg;
        for (int j = 0; j < seg; j++) { tris.Add(capEnd); tris.Add(lastRing + j); tris.Add(lastRing + (j + 1) % seg); }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return ForceSafeBounds(mesh);
    }

    /// <summary>
    /// Mesh bulu single-surface yang aman dari NaN.
    /// Profil lebar: sin(pi * u^0.75)^0.6 — identik dengan eagle_fly.py.
    /// Dua permukaan (atas/bawah) dengan offset 1.5mm agar normal valid & tidak cancel.
    /// </summary>
    Mesh BuildFeatherMesh(float L, float W, float droop)
    {
        var mesh  = new Mesh { name = "feather" };
        int nu = 6;   // segmen memanjang
        int nv = 3;   // segmen menyamping

        var verts   = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var tris    = new List<int>();
        var normals = new List<Vector3>();

        // Hitung lebar bulu di posisi u (0=pangkal, 1=ujung)
        float FeatherWidth(float u)
        {
            u = Mathf.Clamp01(u);
            if (u < 1e-5f || u > 1f - 1e-5f) return 0f;
            float inner    = Mathf.PI * Mathf.Pow(u, 0.75f);
            float sinInner = Mathf.Sin(inner);
            if (sinInner <= 0f) return 0f;
            float w = W * Mathf.Pow(sinInner, 0.6f);
            return Safe(w, 0f);
        }

        // ---- Simpul Atas (Dorsal) ----
        for (int i = 0; i <= nu; i++)
        {
            float u  = (float)i / nu;
            float fw = FeatherWidth(u);
            for (int j = 0; j <= nv; j++)
            {
                float v  = -1f + 2f * j / nv;
                float bx = Safe(v * fw);
                float by = Safe(u * L);
                float bz = Safe(-droop * L * u * u + 0.0025f * (1f - v * v));
                verts.Add(V(bx, by, bz));
                uvs.Add(new Vector2((float)j / nv, u));
                normals.Add(Vector3.up);  // normal awal, akan di-recalculate
            }
        }
        int topCount = verts.Count;

        // Segitiga Atas (CCW → normal ke atas)
        for (int i = 0; i < nu; i++)
        {
            for (int j = 0; j < nv; j++)
            {
                int a  = i * (nv + 1) + j;
                int a1 = a + 1;
                int b0 = a + (nv + 1);
                int b1 = a + (nv + 1) + 1;
                tris.Add(a);  tris.Add(b1); tris.Add(a1);
                tris.Add(a);  tris.Add(b0); tris.Add(b1);
            }
        }

        // ---- Simpul Bawah (Ventral) — offset -1.5mm di bz ----
        const float kOffset = 0.0015f;
        for (int i = 0; i <= nu; i++)
        {
            float u  = (float)i / nu;
            float fw = FeatherWidth(u);
            for (int j = 0; j <= nv; j++)
            {
                float v  = -1f + 2f * j / nv;
                float bx = Safe(v * fw);
                float by = Safe(u * L);
                float bz = Safe(-droop * L * u * u + 0.0025f * (1f - v * v) - kOffset);
                verts.Add(V(bx, by, bz));
                uvs.Add(new Vector2((float)j / nv, u));
                normals.Add(Vector3.down);  // normal awal
            }
        }

        // Segitiga Bawah (CW → normal ke bawah)
        for (int i = 0; i < nu; i++)
        {
            for (int j = 0; j < nv; j++)
            {
                int a  = topCount + i * (nv + 1) + j;
                int a1 = a + 1;
                int b0 = a + (nv + 1);
                int b1 = a + (nv + 1) + 1;
                tris.Add(a);  tris.Add(a1); tris.Add(b1);
                tris.Add(a);  tris.Add(b1); tris.Add(b0);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);

        // Recalculate normals lalu koreksi yang NaN/zero
        mesh.RecalculateNormals();
        var recalcNormals = mesh.normals;
        bool anyFixed = false;
        for (int i = 0; i < recalcNormals.Length; i++)
        {
            var n = recalcNormals[i];
            if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) || n.sqrMagnitude < 1e-6f)
            {
                recalcNormals[i] = i < topCount ? Vector3.up : Vector3.down;
                anyFixed = true;
            }
        }
        if (anyFixed) mesh.normals = recalcNormals;

        mesh.RecalculateBounds();
        return ForceSafeBounds(mesh, L, W);
    }

    // Pastikan bounds tidak NaN — paksa nilai masuk akal jika perlu
    static Mesh ForceSafeBounds(Mesh mesh, float L = 0.3f, float W = 0.05f)
    {
        if (mesh == null) return null;
        var b = mesh.bounds;
        bool bad = float.IsNaN(b.size.x) || float.IsNaN(b.size.y) || float.IsNaN(b.size.z)
                || float.IsInfinity(b.size.x) || float.IsInfinity(b.size.y) || float.IsInfinity(b.size.z)
                || (b.size.x == 0f && b.size.y == 0f && b.size.z == 0f && mesh.vertexCount > 1);
        if (bad)
        {
            mesh.bounds = new Bounds(new Vector3(0f, L * 0.5f, 0f), new Vector3(W * 2f + 0.01f, L + 0.01f, 0.01f));
        }
        return mesh;
    }

    void Fea(string parent, Vector3 root, Vector2 d, float L, float W, Material mat, float droop = 0.1f)
    {
        float len = Mathf.Sqrt(d.x * d.x + d.y * d.y);
        if (len < 1e-5f) len = 1f;

        Mesh fMesh = BuildFeatherMesh(L, W, droop);
        if (fMesh == null) return;

        Vector3 dir = new Vector3(d.x / len, 0f, d.y / len);
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        AddPart(parent, fMesh, mat, root, rot);
    }

    void Ell(string parent, Vector3 center, Vector3 radii, Material mat, float rx = 0f, float rz = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.hideFlags = HideFlags.DontSave;

        var col = go.GetComponent<Collider>();
        if (col)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        go.name = "ell";
        go.transform.SetParent(pivots[parent], false);
        Vector3 pp = pivotWorldBlender[parent];
        go.transform.localPosition = V(center - pp);
        go.transform.localRotation = Quaternion.Euler(-rx, 0f, -rz);
        go.transform.localScale    = 2f * V(radii);

        var mr = go.GetComponent<Renderer>();
        if (mr)
        {
            mr.sharedMaterial    = mat;
            mr.receiveShadows    = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    // ------------------------------------------------------------ Konstruksi Anatomi Elang
    void BuildPlumageAndAnatomy()
    {
        // 1. Badan, leher, kepala
        AddPart("body", BuildLoftMesh("body_loft", new[] {
            new Vector4(-0.40f, 0.002f, 0.002f, 0.02f),
            new Vector4(-0.34f, 0.06f,  0.055f, 0.02f),
            new Vector4(-0.26f, 0.11f,  0.095f, 0.02f),
            new Vector4(-0.12f, 0.15f,  0.13f,  0.012f),
            new Vector4( 0.04f, 0.17f,  0.15f,  0.005f),
            new Vector4( 0.18f, 0.17f,  0.155f, 0.012f),
            new Vector4( 0.29f, 0.15f,  0.15f,  0.035f),
            new Vector4( 0.37f, 0.11f,  0.12f,  0.07f),
            new Vector4( 0.42f, 0.002f, 0.002f, 0.09f)
        }), mats["body"]);

        AddPart("neck", BuildLoftMesh("neck_loft", new[] {
            new Vector4(0.26f, 0.10f,  0.10f,  0.04f),
            new Vector4(0.33f, 0.12f,  0.12f,  0.06f),
            new Vector4(0.40f, 0.108f, 0.113f, 0.10f),
            new Vector4(0.47f, 0.096f, 0.10f,  0.135f),
            new Vector4(0.52f, 0.093f, 0.097f, 0.152f)
        }), mats["white"]);

        AddPart("head", BuildLoftMesh("head_loft", new[] {
            new Vector4(0.46f,  0.095f, 0.10f,  0.14f),
            new Vector4(0.50f,  0.092f, 0.098f, 0.15f),
            new Vector4(0.545f, 0.09f,  0.096f, 0.165f),
            new Vector4(0.585f, 0.08f,  0.088f, 0.17f),
            new Vector4(0.62f,  0.06f,  0.066f, 0.163f),
            new Vector4(0.645f, 0.04f,  0.046f, 0.155f),
            new Vector4(0.66f,  0.002f, 0.002f, 0.15f)
        }), mats["white"]);

        // Paruh
        AddPart("head", BuildLoftMesh("beak_upper", new[] {
            new Vector4(0.60f,  0.042f, 0.05f,  0.15f),
            new Vector4(0.65f,  0.04f,  0.052f, 0.145f),
            new Vector4(0.69f,  0.033f, 0.048f, 0.137f),
            new Vector4(0.725f, 0.022f, 0.038f, 0.122f),
            new Vector4(0.745f, 0.012f, 0.026f, 0.105f),
            new Vector4(0.755f, 0.006f, 0.018f, 0.088f),
            new Vector4(0.76f,  0.002f, 0.004f, 0.078f)
        }), mats["yellow"]);

        AddPart("head", BuildLoftMesh("beak_lower", new[] {
            new Vector4(0.62f, 0.03f,  0.02f,  0.118f),
            new Vector4(0.66f, 0.028f, 0.018f, 0.115f),
            new Vector4(0.70f, 0.02f,  0.014f, 0.113f),
            new Vector4(0.73f, 0.002f, 0.002f, 0.11f)
        }), mats["yellow"]);

        // Mata & Alis
        foreach (int s in new[] { 1, -1 })
        {
            Ell("head", new Vector3(s * 0.06f,   0.575f, 0.21f), new Vector3(0.03f,  0.04f,  0.012f), mats["white"]);
            Ell("head", new Vector3(s * 0.068f,  0.583f, 0.19f), new Vector3(0.016f, 0.016f, 0.016f), mats["eye"]);
            Ell("head", new Vector3(s * 0.0745f, 0.595f, 0.19f), new Vector3(0.008f, 0.008f, 0.008f), mats["black"]);
        }

        // 2. Kaki
        float K = 0.85f;
        foreach (int s in new[] { 1, -1 })
        {
            float x = s * 0.095f;
            AddPart("body", BuildTubeMesh("pants_leg", new[] {
                new Vector3(s * 0.09f, 0.03f,  -0.07f),
                new Vector3(x,         0.0f,   -0.13f),
                new Vector3(x,        -0.05f,  -0.17f)
            }, new[] { 0.06f, 0.05f, 0.032f }, 12), mats["pants"]);

            AddPart("body", BuildTubeMesh("tarsus", new[] {
                new Vector3(x, -0.05f,  -0.165f),
                new Vector3(x, -0.085f, -0.185f),
                new Vector3(x, -0.115f, -0.20f)
            }, new[] { 0.022f, 0.02f, 0.017f }, 10), mats["yellow"]);

            Vector3 A = new Vector3(x, -0.115f, -0.20f);
            Ell("body", A, new Vector3(0.02f, 0.02f, 0.02f), mats["yellow"]);

            Vector3 At(Vector3 o) => new Vector3(A.x + o.x * K, A.y + o.y * K, A.z + o.z * K);

            foreach (float psi in new[] { -0.35f, 0.0f, 0.35f })
            {
                float lx = Mathf.Sin(psi);
                var toePts = new[] {
                    At(Vector3.zero),
                    At(new Vector3(lx * 0.035f, 0.04f,  -0.01f)),
                    At(new Vector3(lx * 0.06f,  0.07f,  -0.035f)),
                    At(new Vector3(lx * 0.075f, 0.082f, -0.072f))
                };
                AddPart("body", BuildTubeMesh("toe", toePts,
                    new[] { 0.014f, 0.012f, 0.0105f, 0.009f }, 8), mats["yellow"]);

                var clawPts = new[] {
                    At(new Vector3(lx * 0.075f, 0.082f, -0.072f)),
                    At(new Vector3(lx * 0.082f, 0.09f,  -0.10f)),
                    At(new Vector3(lx * 0.082f, 0.088f, -0.125f))
                };
                AddPart("body", BuildTubeMesh("claw", clawPts,
                    new[] { 0.009f, 0.006f, 0.001f }, 8), mats["black"]);
            }

            var hindToe = new[] {
                At(Vector3.zero),
                At(new Vector3(0f, -0.03f,  -0.01f)),
                At(new Vector3(0f, -0.05f,  -0.035f)),
                At(new Vector3(0f, -0.055f, -0.065f))
            };
            AddPart("body", BuildTubeMesh("hind_toe", hindToe,
                new[] { 0.014f, 0.012f, 0.0105f, 0.009f }, 8), mats["yellow"]);

            var hindClaw = new[] {
                At(new Vector3(0f, -0.055f, -0.065f)),
                At(new Vector3(0f, -0.05f,  -0.095f)),
                At(new Vector3(0f, -0.035f, -0.12f))
            };
            AddPart("body", BuildTubeMesh("hind_claw", hindClaw,
                new[] { 0.011f, 0.007f, 0.001f }, 8), mats["black"]);
        }

        // 3. Ekor (12 helai)
        for (int i = 0; i < 12; i++)
        {
            float b = (-26f + 4.73f * i) * Mathf.Deg2Rad;
            Vector3 tailRoot = new Vector3(0f, -0.30f, 0.0015f * (6f - Mathf.Abs(i - 5.5f)));
            Vector2 tailDir  = new Vector2(Mathf.Sin(b), -Mathf.Cos(b));
            Fea("tail", tailRoot, tailDir, 0.42f, 0.05f, mats["tail"], 0.04f);
        }

        // 4. Sayap
        BuildWingPlumage(1);
        BuildWingPlumage(-1);
    }

    void BuildWingPlumage(int s)
    {
        string nm = s > 0 ? "R" : "L";
        string sh = "sh" + nm, el = "el" + nm, wr = "wr" + nm;

        AddPart(sh, BuildTubeMesh("sh_lead", new[] {
            new Vector3(s * 0.12f, 0.08f,  0.06f),
            new Vector3(s * 0.31f, 0.085f, 0.065f),
            new Vector3(s * 0.50f, 0.08f,  0.065f)
        }, new[] { 0.05f, 0.045f, 0.04f }, 10), mats["lead"]);

        AddPart(el, BuildTubeMesh("el_lead", new[] {
            new Vector3(s * 0.50f, 0.08f,  0.065f),
            new Vector3(s * 0.68f, 0.09f,  0.062f),
            new Vector3(s * 0.85f, 0.09f,  0.06f)
        }, new[] { 0.04f, 0.034f, 0.028f }, 10), mats["lead"]);

        AddPart(wr, BuildTubeMesh("wr_lead", new[] {
            new Vector3(s * 0.85f, 0.09f,  0.06f),
            new Vector3(s * 0.92f, 0.095f, 0.058f),
            new Vector3(s * 0.98f, 0.09f,  0.055f)
        }, new[] { 0.028f, 0.02f, 0.011f }, 8), mats["lead"]);

        // Lengan atas (5 baris)
        for (int i = 0; i < 5; i++)
        {
            float x = 0.16f + 0.075f * i;
            float z = 0.0008f * i;
            Vector2 d = new Vector2(s * 0.04f, -1f);
            Fea(sh, new Vector3(s * x,           0.0f,  0.050f + z), d, 0.30f, 0.055f, mats["sec"]);
            Fea(sh, new Vector3(s * x,           0.06f, 0.0555f + z), d, 0.14f, 0.050f, mats["gc"]);
            Fea(sh, new Vector3(s * (x + 0.01f), 0.085f, 0.061f + z), d, 0.10f, 0.040f, mats["mc"]);
            Fea(sh, new Vector3(s * (x + 0.02f), 0.095f, 0.0665f + z), d, 0.07f, 0.035f, mats["lc"]);
        }

        // Lengan bawah (7 baris)
        for (int i = 0; i < 7; i++)
        {
            float x = 0.52f + 0.05f * i;
            float z = 0.0008f * i;
            Vector2 d = new Vector2(s * (0.05f + 0.05f * i), -1f);
            Fea(el, new Vector3(s * x,           0.0f,  0.050f + z), d, 0.32f + 0.012f * i, 0.045f, mats["sec"]);
            Fea(el, new Vector3(s * x,           0.07f, 0.0555f + z), d, 0.15f, 0.045f, mats["gc"]);
            Fea(el, new Vector3(s * (x + 0.01f), 0.088f, 0.061f + z), d, 0.10f, 0.040f, mats["mc"]);
            Fea(el, new Vector3(s * (x + 0.02f), 0.098f, 0.0665f + z), d, 0.07f, 0.035f, mats["lc"]);
        }

        // Tangan: 10 bulu primer
        for (int i = 0; i < 10; i++)
        {
            float a   = (-62f + 8f * i) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(s * Mathf.Cos(a), Mathf.Sin(a));
            float ln  = (0.30f + 0.17f * Mathf.Sin(Mathf.PI * (i + 0.8f) / 10.4f)) * 0.92f;
            Vector3 root = new Vector3(s * (0.86f + 0.012f * i), 0.05f - 0.004f * i, 0.048f + 0.0008f * i);
            Fea(wr, root, d, ln, 0.032f, mats["prim"]);
            Fea(wr, new Vector3(root.x, root.y + 0.03f, 0.054f + 0.0008f * i), d, 0.16f, 0.030f, mats["gc"]);
        }

        // Coverts tangan
        for (int i = 0; i < 5; i++)
        {
            float a   = (-40f + 12f * i) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(s * Mathf.Cos(a), Mathf.Sin(a));
            Fea(wr, new Vector3(s * (0.84f + 0.03f * i), 0.098f, 0.067f + 0.0008f * i), d, 0.08f, 0.03f, mats["lc"]);
        }
    }

    // ------------------------------------------------------------ Kinematika Terbang & Kepakan
    float SmoothStepCurve(float x, float a, float b)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    void Update()
    {
        if (!isBuilt || !pivots.ContainsKey("shR") || pivots["shR"] == null)
        {
            EnsureBuilt();
            return;
        }

        float time     = Time.time;
        float animTime = time * animSpeed;

        float th = TAU * animTime / Mathf.Max(0.1f, orbitDuration);
        float ph = TAU * animTime / Mathf.Max(0.05f, flapDuration);

        float u = Mathf.Repeat(animTime, Mathf.Max(0.1f, glideCycleDuration)) / Mathf.Max(0.1f, glideCycleDuration);
        float e = continuousFlapping ? 1f
                : SmoothStepCurve(u, 0.02f, 0.10f) * (1f - SmoothStepCurve(u, 0.60f, 0.72f));

        // 1. Posisi & Orientasi Lingkaran Terbang
        if (updatePosition)
        {
            Vector3 orbitPos = new Vector3(
                flightCenter.x + orbitRadius * Mathf.Cos(th),
                orbitHeight    + 0.25f * Mathf.Sin(2f * th),
                flightCenter.z + orbitRadius * Mathf.Sin(th)
            );
            transform.position = orbitPos;
            transform.rotation = Quaternion.Euler(0f, -th * Mathf.Rad2Deg, 0f);
        }

        // 2. Bank Angle
        pivots["bank"].localRotation = Quaternion.Euler(0f, 0f, bankAngle);

        // 3. Kepakan Sayap
        float flapDeg  = 6f + e * (2f + 38f * Mathf.Cos(ph));
        float wristDeg = 6f + e * 24f * Mathf.Sin(ph - 0.35f);
        float elbowDeg = e * 10f * Mathf.Sin(ph - 0.2f);
        float foldDeg  = 3f + e * 22f * Mathf.Max(0f, -Mathf.Sin(ph));

        foreach (var side in new[] { new { s = 1, nm = "R" }, new { s = -1, nm = "L" } })
        {
            int    s  = side.s;
            string nm = side.nm;
            pivots["sh" + nm].localRotation = Quaternion.Euler(0f, s * 3f,              s * flapDeg);
            pivots["el" + nm].localRotation = Quaternion.Euler(0f, s * foldDeg,         s * elbowDeg);
            pivots["wr" + nm].localRotation = Quaternion.Euler(0f, s * 0.6f * foldDeg,  s * wristDeg);
        }

        // 4. Gerakan Tubuh
        pivots["body"].localPosition = bodyRestLocalPos + new Vector3(0f, 0.05f * e * Mathf.Sin(ph), 0f);
        pivots["body"].localRotation = Quaternion.Euler(-e * 3f * Mathf.Sin(ph + 0.4f), 0f, 0f);
        pivots["neck"].localRotation = Quaternion.Euler(4f, 0f, 0f);
        pivots["head"].localRotation = Quaternion.Euler(
             e * 3f * Mathf.Sin(ph + 0.4f),
            -(12f + 6f * Mathf.Sin(3f * th)),
             0f);
        pivots["tail"].localRotation = Quaternion.Euler(-(4f + e * 6f * Mathf.Sin(ph + 0.9f)), 0f, 0f);
    }
}
