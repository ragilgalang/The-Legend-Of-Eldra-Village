using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pohon hutan rimba prosedural: batang besar + akar banir, cabang, dan tajuk daun yang lebat.
/// Tidak butuh model/texture eksternal. Pasang di GameObject kosong lalu panggil Build().
/// </summary>
public class JungleTree : MonoBehaviour
{
    [Header("Batang")]
    public float height = 38f;
    public float baseRadius = 1.8f;      // batang besar
    public float topRadius = 0.6f;
    [Range(6, 32)] public int radialSegments = 16;
    [Range(6, 40)] public int rings = 22;
    public int buttressCount = 6;        // jumlah akar banir
    public float buttressFlare = 1.3f;   // seberapa lebar akar di dasar
    public float buttressHeight = 4.5f;
    public float bend = 1.2f;            // lengkung batang

    [Header("Tajuk & Daun")]
    public int branchCount = 8;
    public int leafClusters = 28;
    public float canopyRadius = 10f;
    [Range(0.3f, 0.9f)] public float canopyStart = 0.62f; // mulai di % tinggi batang
    public Vector2 clusterSize = new Vector2(4.5f, 8f);

    [Header("Material (opsional, otomatis dibuat bila kosong)")]
    public Material barkMaterial;
    public Material leafMaterial;
    public Color leafTint = new Color(0.12f, 0.45f, 0.13f);

    static Mesh _lumpy;
    static Material _bark, _leaf;

    // ---------------------------------------------------------------- BUILD
    public void Build(int seed)
    {
        Random.State old = Random.state;
        Random.InitState(seed);

        // bersihkan anak lama
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // --- Batang
        Mesh trunk = BuildTube(height, baseRadius, topRadius, radialSegments, rings,
                               buttressCount, buttressFlare, buttressHeight, bend, seed * 0.37f);

        var barkParts = new List<CombineInstance>();
        barkParts.Add(new CombineInstance { mesh = trunk, transform = Matrix4x4.identity });

        // --- Cabang + tajuk
        Mesh branch = BuildTube(1f, 1f, 0.35f, 7, 4, 0, 0f, 1f, 0.15f, seed);
        var leafParts = new List<CombineInstance>();
        Mesh lumpy = LumpySphere();

        float yStart = height * canopyStart;
        for (int i = 0; i < branchCount; i++)
        {
            float y = Random.Range(yStart * 0.85f, height * 0.95f);
            float ang = Random.value * Mathf.PI * 2f;
            float len = Random.Range(canopyRadius * 0.5f, canopyRadius * 0.95f) * Mathf.Lerp(1f, 0.5f, y / height);
            Vector3 dir = new Vector3(Mathf.Cos(ang), Random.Range(0.25f, 0.7f), Mathf.Sin(ang)).normalized;
            float r = Mathf.Lerp(0.55f, 0.25f, y / height);
            Vector3 pos = TrunkCenter(y / height, seed * 0.37f, bend, height);
            pos.y = y;

            barkParts.Add(new CombineInstance
            {
                mesh = branch,
                transform = Matrix4x4.TRS(pos, Quaternion.FromToRotation(Vector3.up, dir), new Vector3(r, len, r))
            });

            // gumpalan daun di ujung cabang
            Vector3 tip = pos + dir * len;
            for (int k = 0; k < 3; k++)
                AddLeaf(leafParts, lumpy, tip + Random.insideUnitSphere * 2.2f);
        }

        // gumpalan daun tambahan agar tajuk padat (lapis bawah → atas)
        for (int i = 0; i < leafClusters; i++)
        {
            float t = Random.Range(canopyStart, 1.02f);
            float spread = canopyRadius * Mathf.Sin(Mathf.Clamp01((t - canopyStart) / (1.05f - canopyStart)) * Mathf.PI * 0.85f + 0.35f);
            Vector2 c = Random.insideUnitCircle * spread;
            Vector3 p = TrunkCenter(Mathf.Min(t, 1f), seed * 0.37f, bend, height);
            p += new Vector3(c.x, 0f, c.y);
            p.y = t * height;
            AddLeaf(leafParts, lumpy, p);
        }

        // --- Objek batang
        var trunkGO = new GameObject("Trunk");
        trunkGO.transform.SetParent(transform, false);
        trunkGO.AddComponent<MeshFilter>().sharedMesh = Combine(barkParts);
        trunkGO.AddComponent<MeshRenderer>().sharedMaterial = barkMaterial != null ? barkMaterial : DefaultBark();
        var col = trunkGO.AddComponent<CapsuleCollider>();
        col.radius = baseRadius * 0.9f;
        col.height = height * 0.6f;
        col.center = new Vector3(0, height * 0.3f, 0);

        // --- Objek daun
        var leafGO = new GameObject("Canopy");
        leafGO.transform.SetParent(transform, false);
        leafGO.AddComponent<MeshFilter>().sharedMesh = Combine(leafParts);
        var lr = leafGO.AddComponent<MeshRenderer>();
        lr.sharedMaterial = leafMaterial != null ? leafMaterial : DefaultLeaf();
        var mpb = new MaterialPropertyBlock();
        Color tint = Color.Lerp(leafTint, leafTint * Random.Range(0.7f, 1.25f), 0.8f);
        tint.a = 1f;
        mpb.SetColor("_BaseColor", tint); // URP
        mpb.SetColor("_Color", tint);     // Built-in
        lr.SetPropertyBlock(mpb);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        Random.state = old;
    }

    void AddLeaf(List<CombineInstance> list, Mesh lumpy, Vector3 pos)
    {
        if (lumpy == null) return;
        float s = Random.Range(clusterSize.x, clusterSize.y);
        Quaternion rot = Quaternion.Euler(Random.Range(-15f, 15f), Random.value * 360f, Random.Range(-15f, 15f));
        list.Add(new CombineInstance
        {
            mesh = lumpy,
            transform = Matrix4x4.TRS(pos, rot, new Vector3(s * 1.15f, s * 0.65f, s * 1.15f)) // agak pipih seperti tajuk
        });
    }

    // ------------------------------------------------------------- MESH UTILS
    static Vector3 TrunkCenter(float t, float seed, float bend, float h)
    {
        return new Vector3(Mathf.Sin(t * 3f + seed) * bend * t, t * h, Mathf.Cos(t * 2.3f + seed) * bend * t);
    }

    /// <summary>Tabung meruncing dengan akar banir di dasar dan lengkung ringan.</summary>
    public static Mesh BuildTube(float h, float baseR, float topR, int radial, int rings,
                                 int lobes, float flare, float flareH, float bend, float seed)
    {
        int vc = (rings + 1) * (radial + 1);
        var verts = new Vector3[vc];
        var uvs = new Vector2[vc];
        var tris = new int[rings * radial * 6];

        for (int i = 0; i <= rings; i++)
        {
            float t = i / (float)rings;
            float y = t * h;
            float r = Mathf.Lerp(baseR, topR, Mathf.Pow(t, 0.8f));
            float fl = flare * Mathf.Exp(-y / Mathf.Max(0.01f, flareH));
            Vector3 center = TrunkCenter(t, seed, bend, h);

            for (int j = 0; j <= radial; j++)
            {
                float a = j / (float)radial * Mathf.PI * 2f;
                float lobe = lobes > 0 ? Mathf.Pow(Mathf.Abs(Mathf.Cos(a * lobes * 0.5f)), 3f) : 0f;
                float noise = (Mathf.PerlinNoise(a * 2f + seed, y * 0.4f) - 0.5f) * 0.12f * r; // tekstur kulit kasar
                float rr = r * (1f + fl * lobe) + noise;
                int idx = i * (radial + 1) + j;
                verts[idx] = new Vector3(center.x + Mathf.Cos(a) * rr, y, center.z + Mathf.Sin(a) * rr);
                uvs[idx] = new Vector2(j / (float)radial * 3f, y / 4f);
            }
        }

        int ti = 0;
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < radial; j++)
            {
                int a = i * (radial + 1) + j;
                int b = a + radial + 1;
                tris[ti++] = a; tris[ti++] = a + 1; tris[ti++] = b;
                tris[ti++] = a + 1; tris[ti++] = b + 1; tris[ti++] = b;
            }

        var m = new Mesh { name = "TubeMesh" };
        m.vertices = verts; m.uv = uvs; m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Bola dengan permukaan bergelombang untuk gumpalan daun.</summary>
    public static Mesh LumpySphere()
    {
        if (_lumpy != null) return _lumpy;
        Mesh src = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        if (src == null)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mf = temp.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                src = mf.sharedMesh;
            if (Application.isPlaying) Destroy(temp);
            else DestroyImmediate(temp);
        }

        if (src == null) return null;

        var verts = src.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            float n = Mathf.PerlinNoise(v.x * 3.1f + 10f, v.y * 3.1f + v.z * 2.3f + 10f);
            float n2 = Mathf.PerlinNoise(v.z * 6f + 3f, v.x * 6f + 7f);
            verts[i] = v * (0.75f + n * 0.5f + n2 * 0.18f);
        }
        _lumpy = new Mesh { name = "LumpySphere" };
        _lumpy.hideFlags = HideFlags.DontSave;
        _lumpy.vertices = verts;
        _lumpy.triangles = src.triangles;
        _lumpy.uv = src.uv;
        _lumpy.RecalculateNormals();
        _lumpy.RecalculateBounds();
        return _lumpy;
    }

    public static Mesh Combine(List<CombineInstance> parts)
    {
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.CombineMeshes(parts.ToArray(), true, true);
        m.RecalculateBounds();
        return m;
    }

    // --------------------------------------------------------------- MATERIAL
    public static Material MakeMaterial(Color c, float smoothness = 0.1f)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Sprites/Default");
        var m = new Material(s);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        return m;
    }

    static Material DefaultBark() { return _bark != null ? _bark : (_bark = MakeMaterial(new Color(0.28f, 0.19f, 0.12f), 0.05f)); }
    static Material DefaultLeaf() { return _leaf != null ? _leaf : (_leaf = MakeMaterial(Color.white, 0.15f)); }
}
