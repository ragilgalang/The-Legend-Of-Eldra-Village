using System.Collections.Generic;
using UnityEngine;

/// <summary>Fungsi bantu untuk merakit karakter prosedural (bentuk dasar, mesh lathe, tekstur kain).</summary>
public static class KiWiraUtil
{
    public static Color Hex(string h)
    {
        Color c;
        return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.magenta;
    }

    public static Material Mat(Color c, float halus, Texture2D tex = null, float rx = 1f, float ry = 1f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        Color warna = tex != null ? Color.white : c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", warna);
        if (m.HasProperty("_Color")) m.SetColor("_Color", warna);
        if (tex != null)
        {
            m.mainTexture = tex;
            m.mainTextureScale = new Vector2(rx, ry);
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", halus);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", halus);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    public static Texture2D Kain(Color dasar, int seed)
    {
        var t = new Texture2D(128, 128, TextureFormat.RGBA32, true);
        var rnd = new System.Random(seed);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float d = ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                if (y % 4 == 0) d -= 0.03f;
                t.SetPixel(x, y, new Color(Mathf.Clamp01(dasar.r + d), Mathf.Clamp01(dasar.g + d), Mathf.Clamp01(dasar.b + d), 1f));
            }
        }
        Selesai(t);
        return t;
    }

    public static Texture2D Batik(Color bg, Color fg, int seed)
    {
        var t = new Texture2D(256, 128, TextureFormat.RGBA32, true);
        var rnd = new System.Random(seed);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float lx = (x % 64) - 32f, ly = (y % 64) - 32f;
                bool tinta = (lx * lx + ly * ly) < 9f;
                for (int k = 0; k < 4 && !tinta; k++)
                {
                    float rx = lx, ry = ly;
                    if (k == 1) { rx = ly; ry = -lx; }
                    else if (k == 2) { rx = -lx; ry = -ly; }
                    else if (k == 3) { rx = -ly; ry = lx; }
                    float a = rx / 9f, b = (ry + 15f) / 16f;
                    if (Mathf.Abs(a * a + b * b - 1f) < 0.26f) tinta = true;
                }
                Color c = tinta ? fg : bg;
                float d = ((float)rnd.NextDouble() - 0.5f) * 0.07f;
                t.SetPixel(x, y, new Color(Mathf.Clamp01(c.r + d), Mathf.Clamp01(c.g + d), Mathf.Clamp01(c.b + d), 1f));
            }
        }
        Selesai(t);
        return t;
    }

    static void Selesai(Texture2D t)
    {
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        t.anisoLevel = 4;
        t.Apply(true);
    }

    // ------------------------------------------------------------ bentuk dasar

    public static GameObject Prim(PrimitiveType tipe, string nama, Transform induk, Material m)
    {
        var g = GameObject.CreatePrimitive(tipe);
        g.name = nama;
        var col = g.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);
        g.GetComponent<Renderer>().sharedMaterial = m;
        g.transform.SetParent(induk, false);
        return g;
    }

    public static GameObject Ball(string nama, Transform induk, Material m, Vector3 pos, float r, Vector3 kali)
    {
        var g = Prim(PrimitiveType.Sphere, nama, induk, m);
        g.transform.localPosition = pos;
        g.transform.localScale = new Vector3(2f * r * kali.x, 2f * r * kali.y, 2f * r * kali.z);
        return g;
    }

    public static GameObject Ball(string nama, Transform induk, Material m, Vector3 pos, float r)
    {
        return Ball(nama, induk, m, pos, r, Vector3.one);
    }

    public static GameObject Kotak(string nama, Transform induk, Material m, Vector3 pos, Vector3 ukuran, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var g = Prim(PrimitiveType.Cube, nama, induk, m);
        g.transform.localPosition = pos;
        g.transform.localScale = ukuran;
        g.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        return g;
    }

    public static GameObject Seg(string nama, Transform induk, Material m, Vector3 a, Vector3 b, float r1, float r2)
    {
        Vector3 d = b - a;
        float r = (r1 + r2) * 0.5f;
        var g = Prim(PrimitiveType.Cylinder, nama, induk, m);
        g.transform.localPosition = (a + b) * 0.5f;
        g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        g.transform.localScale = new Vector3(2f * r, d.magnitude * 0.5f, 2f * r);
        return g;
    }

    public static GameObject Band(string nama, Transform induk, Material m, float rBawah, float rAtas, float tinggi, float y, float zs)
    {
        var prof = new List<Vector2> { new Vector2(rBawah, -tinggi * 0.5f), new Vector2(rAtas, tinggi * 0.5f) };
        var g = MeshObj(nama, induk, Lathe(prof, 48, zs), m);
        g.transform.localPosition = new Vector3(0f, y, 0f);
        return g;
    }

    public static GameObject MeshObj(string nama, Transform induk, Mesh mesh, Material m)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(induk, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    public static Transform Pivot(Transform induk, string nama, Vector3 pos)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(induk, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    public static void Para(Transform pivot, List<GameObject> bagian)
    {
        foreach (var g in bagian) g.transform.SetParent(pivot, true);
    }

    // ------------------------------------------------------------ profil dan lathe

    // Tutup setengah bola dari kutub atas sampai sudut thetaMax (radian).
    public static List<Vector2> Tudung(float r, float thetaMax, float tinggiKali)
    {
        var p = new List<Vector2>();
        for (int n = 0; n <= 10; n++)
        {
            float f = thetaMax * n / 10f;
            p.Add(new Vector2(r * Mathf.Sin(f), r * Mathf.Cos(f) * tinggiKali));
        }
        return p;
    }

    public static List<Vector2> Haluskan(Vector2[] p, int per)
    {
        var hasil = new List<Vector2>();
        for (int i = 0; i < p.Length - 1; i++)
        {
            Vector2 p0 = p[Mathf.Max(i - 1, 0)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(i + 2, p.Length - 1)];
            for (int s = 0; s < per; s++) hasil.Add(CatmullRom(p0, p1, p2, p3, (float)s / per));
        }
        hasil.Add(p[p.Length - 1]);
        return hasil;
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // Mesh putar dua sisi (luar dan dalam) agar kain tipis terlihat dari kedua arah.
    public static Mesh Lathe(List<Vector2> prof, int segs, float zs)
    {
        int P = prof.Count, S = segs, vc = P * (S + 1);
        var v = new Vector3[vc * 2];
        var uv = new Vector2[vc * 2];
        var tri = new int[(P - 1) * S * 12];

        for (int j = 0; j < P; j++)
        {
            for (int i = 0; i <= S; i++)
            {
                float a = 2f * Mathf.PI * i / S;
                var pos = new Vector3(prof[j].x * Mathf.Cos(a), prof[j].y, prof[j].x * Mathf.Sin(a) * zs);
                int idx = j * (S + 1) + i;
                v[idx] = pos; v[idx + vc] = pos;
                uv[idx] = uv[idx + vc] = new Vector2((float)i / S, P > 1 ? (float)j / (P - 1) : 0f);
            }
        }

        int k = 0;
        for (int j = 0; j < P - 1; j++)
        {
            for (int i = 0; i < S; i++)
            {
                int a = j * (S + 1) + i, b = a + 1, c = a + (S + 1), d = c + 1;
                tri[k++] = a; tri[k++] = c; tri[k++] = b;
                tri[k++] = b; tri[k++] = c; tri[k++] = d;
                tri[k++] = a + vc; tri[k++] = b + vc; tri[k++] = c + vc;
                tri[k++] = b + vc; tri[k++] = d + vc; tri[k++] = c + vc;
            }
        }

        var mesh = new Mesh { name = "KiWira_Lathe" };
        mesh.vertices = v;
        mesh.uv = uv;
        mesh.triangles = tri;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static void SafeDestroy(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }
}
