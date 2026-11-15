using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Membangun karakter "Ki Wira" (warga desa bergaya tradisional-legenda) secara prosedural
/// dari bentuk dasar dan mesh lathe. Cocok sebagai karakter prototipe / karakter utama.
/// Pasang di GameObject Player lalu klik kanan komponen > "Bangun Ki Wira",
/// atau dipanggil otomatis oleh SceneSetupTool.
/// </summary>
public class KiWiraBuilder : MonoBehaviour
{
    public const string NamaModel = "KiWira_Model";
    const float ZS = 0.72f; // pipihkan badan di sumbu depan-belakang

    // Profil badan tunik (radius, tinggi) dalam meter
    static readonly Vector2[] Profil =
    {
        new Vector2(0.29f, 0.70f), new Vector2(0.275f, 0.84f), new Vector2(0.23f, 1.0f),
        new Vector2(0.215f, 1.15f), new Vector2(0.235f, 1.33f), new Vector2(0.215f, 1.46f),
        new Vector2(0.09f, 1.53f)
    };

    Material mKulit, mRambut, mTunik, mCelana, mKrem, mKulitSamak, mEmas, mSelempang, mIkat, mList, mMata;
    Transform model;

    [ContextMenu("Bangun Ki Wira")]
    public void Build()
    {
        Hapus();
        var go = new GameObject(NamaModel);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        model = go.transform;

        BuatMaterial();
        BuatTubuh();
        BuatKaki();
        BuatLengan();
        BuatKepala();

        // Refresh referensi animasi jika ada KiWiraIdle
        var idle = GetComponent<KiWiraIdle>();
        if (idle != null) idle.Cari();
    }

    [ContextMenu("Hapus Model")]
    public void Hapus()
    {
        var lama = transform.Find(NamaModel);
        if (lama != null) SafeDestroy(lama.gameObject);
    }

    public static GameObject BuildOn(GameObject root)
    {
        var builder = root.GetComponent<KiWiraBuilder>();
        if (!builder) builder = root.AddComponent<KiWiraBuilder>();
        builder.Build();
        return builder.transform.Find(NamaModel)?.gameObject;
    }

    // ---------------------------------------------------------------- material

    void BuatMaterial()
    {
        mKulit = Mat(Hex("#A9764D"), 0.35f);
        mRambut = Mat(Hex("#1D1612"), 0.4f);
        mTunik = Mat(Hex("#34476E"), 0.1f, Kain(Hex("#34476E"), 1), 3, 3);
        mCelana = Mat(Hex("#4A3727"), 0.08f, Kain(Hex("#4A3727"), 2), 2, 2);
        mKrem = Mat(Hex("#CDBF9C"), 0.1f, Kain(Hex("#CDBF9C"), 3), 2, 2);
        mKulitSamak = Mat(Hex("#5A3A22"), 0.4f);
        mEmas = Mat(Hex("#C9962B"), 0.6f);
        mSelempang = Mat(Hex("#6E2626"), 0.1f, Batik(Hex("#6E2626"), Hex("#D6A53A"), 4), 5, 1);
        mIkat = Mat(Hex("#2C3D63"), 0.1f, Batik(Hex("#2C3D63"), Hex("#D6A53A"), 5), 4, 1);
        mList = Mat(Hex("#2C3D63"), 0.1f, Batik(Hex("#2C3D63"), Hex("#D6A53A"), 6), 9, 1);
        mMata = Mat(Hex("#15100D"), 0.7f);
    }

    static Color Hex(string h)
    {
        Color c;
        return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.magenta;
    }

    static Material Mat(Color c, float halus, Texture2D tex = null, float rx = 1f, float ry = 1f)
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
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", new Vector2(rx, ry));
            }
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", halus);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", halus);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    // Tekstur kain berbintik halus
    static Texture2D Kain(Color dasar, int seed)
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
        FinishTex(t);
        return t;
    }

    // Tekstur batik motif kawung sederhana
    static Texture2D Batik(Color bg, Color fg, int seed)
    {
        var t = new Texture2D(256, 128, TextureFormat.RGBA32, true);
        var rnd = new System.Random(seed);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float lx = (x % 64) - 32f, ly = (y % 64) - 32f;
                bool tinta = y < 5 || y >= 123 || (lx * lx + ly * ly) < 10f;
                for (int k = 0; k < 4 && !tinta; k++)
                {
                    float rx = lx, ry = ly;
                    if (k == 1) { rx = ly; ry = -lx; }
                    else if (k == 2) { rx = -lx; ry = -ly; }
                    else if (k == 3) { rx = -ly; ry = lx; }
                    float a = rx / 9f, b = (ry + 15f) / 16f;
                    float e = a * a + b * b;
                    if (Mathf.Abs(e - 1f) < 0.28f) tinta = true;
                }
                Color c = tinta ? fg : bg;
                float d = ((float)rnd.NextDouble() - 0.5f) * 0.07f;
                t.SetPixel(x, y, new Color(Mathf.Clamp01(c.r + d), Mathf.Clamp01(c.g + d), Mathf.Clamp01(c.b + d), 1f));
            }
        }
        FinishTex(t);
        return t;
    }

    static void FinishTex(Texture2D t)
    {
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        t.anisoLevel = 4;
        t.Apply(true);
    }

    // ---------------------------------------------------------------- tubuh

    void BuatTubuh()
    {
        var torso = new GameObject("Torso").transform;
        torso.SetParent(model, false);

        // tunik (mesh lathe, dua sisi)
        var tunik = MeshObj("Tunik", torso, Lathe(Haluskan(Profil, 6), 52, ZS), mTunik);

        // list batik di tepi bawah, selempang pinggang, kerah
        Band("ListTunik", torso, mList, 0.293f, 0.293f, 0.055f, 0.725f, ZS);
        Band("Selempang", torso, mSelempang, 0.238f, 0.238f, 0.10f, 1.0f, ZS);
        float rk = R(1.5f) + 0.004f;
        Band("Kerah", torso, mEmas, rk, rk, 0.03f, 1.5f, ZS);

        // simpul dan ujung selempang
        Ball("SimpulSelempang", torso, mSelempang, new Vector3(0.13f, 1.0f, 0.152f), 0.034f, new Vector3(1f, 1f, 0.8f));
        Kotak("UjungSelempang1", torso, mSelempang, new Vector3(0.115f, 0.89f, 0.157f), new Vector3(0.05f, 0.2f, 0.012f), 0f, 0f, 7f);
        Kotak("UjungSelempang2", torso, mSelempang, new Vector3(0.15f, 0.9f, 0.15f), new Vector3(0.045f, 0.16f, 0.012f), 0f, 0f, -10f);

        // tali kulit menyilang mengikuti permukaan badan
        var titik = new List<Vector3>();
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f, px = 0.12f - 0.32f * t, py = 1.43f - 0.44f * t;
            float pz = ZS * Mathf.Sqrt(Mathf.Max(R(py) * R(py) - px * px, 0f)) + 0.012f;
            titik.Add(new Vector3(px, py, pz));
        }
        for (int i = 0; i < titik.Count - 1; i++)
            Seg("TaliKulit" + i, torso, mKulitSamak, titik[i], titik[i + 1], 0.017f, 0.017f);

        // kantong di pinggul kiri
        Kotak("Kantong", torso, mKulitSamak, new Vector3(-0.265f, 0.86f, 0.02f), new Vector3(0.06f, 0.13f, 0.11f), 0f, 0f, 0f);
        Kotak("TutupKantong", torso, mKulitSamak, new Vector3(-0.265f, 0.9f, 0.02f), new Vector3(0.066f, 0.05f, 0.116f), 0f, 0f, 0f);
        Ball("KancingKantong", torso, mEmas, new Vector3(-0.298f, 0.9f, 0.02f), 0.012f, Vector3.one);

        // leher
        Seg("Leher", torso, mKulit, new Vector3(0f, 1.48f, 0f), new Vector3(0f, 1.6f, 0.005f), 0.05f, 0.05f);
    }

    void BuatKaki()
    {
        foreach (int s in new[] { -1, 1 })
        {
            string sisi = s < 0 ? "Kiri" : "Kanan";
            var pivot = Pivot("Kaki_" + sisi, new Vector3(s * 0.095f, 0.8f, 0f));
            Vector3 pinggul = new Vector3(s * 0.095f, 0.8f, 0f);
            Vector3 lutut = new Vector3(s * 0.10f, 0.46f, 0.03f);
            Vector3 mata = new Vector3(s * 0.10f, 0.11f, 0f);

            var bagian = new List<GameObject>
            {
                Seg("PahaCelana", model, mCelana, pinggul, lutut, 0.088f, 0.066f),
                Seg("BetisCelana", model, mCelana, lutut, mata, 0.066f, 0.05f),
                Ball("Lutut", model, mCelana, lutut, 0.067f, Vector3.one),
                Seg("BelatBetis", model, mKrem, Vector3.Lerp(lutut, mata, 0.12f), Vector3.Lerp(lutut, mata, 0.92f), 0.072f, 0.058f),
                Ball("Sepatu", model, mKulitSamak, new Vector3(s * 0.10f, 0.055f, 0.045f), 0.062f, new Vector3(1f, 0.75f, 1.9f))
            };
            Para(pivot, bagian);
        }
    }

    void BuatLengan()
    {
        foreach (int s in new[] { -1, 1 })
        {
            string sisi = s < 0 ? "Kiri" : "Kanan";
            Vector3 bahu = new Vector3(s * 0.25f, 1.4f, 0f);
            Vector3 siku = new Vector3(s * 0.32f, 1.13f, 0f);
            Vector3 pergelangan = s > 0 ? new Vector3(0.37f, 0.86f, 0.09f) : new Vector3(-0.38f, 0.86f, 0.09f);
            var pivot = Pivot("Lengan_" + sisi, bahu);

            var bagian = new List<GameObject>
            {
                Ball("Bahu", model, mTunik, bahu, 0.063f, Vector3.one),
                Seg("LenganAtas", model, mTunik, bahu, siku, 0.062f, 0.052f),
                Ball("Siku", model, mTunik, siku, 0.052f, Vector3.one),
                Seg("LenganBawah", model, mKulit, siku, pergelangan, 0.047f, 0.037f),
                Seg("Gelang", model, mKrem, Vector3.Lerp(siku, pergelangan, 0.45f), Vector3.Lerp(siku, pergelangan, 0.93f), 0.053f, 0.046f),
                Ball("Tangan", model, mKulit, pergelangan + new Vector3(0f, -0.02f, 0f), 0.043f, new Vector3(1f, 1.1f, 1f))
            };
            Para(pivot, bagian);
        }
    }

    void BuatKepala()
    {
        Vector3 pusat = new Vector3(0f, 1.69f, 0.005f);
        var pivot = Pivot("Kepala", new Vector3(0f, 1.6f, 0.005f));
        var bagian = new List<GameObject>();

        bagian.Add(Ball("Tengkorak", model, mKulit, pusat, 0.105f, new Vector3(0.95f, 1.13f, 1.0f)));
        foreach (int s in new[] { -1, 1 })
        {
            bagian.Add(Ball("Telinga", model, mKulit, pusat + new Vector3(s * 0.10f, -0.005f, 0f), 0.02f, new Vector3(0.6f, 1f, 0.9f)));
            bagian.Add(Ball("Mata", model, mMata, pusat + new Vector3(s * 0.039f, 0.006f, 0.097f), 0.0105f, new Vector3(1f, 1.1f, 0.6f)));
            bagian.Add(Kotak("Alis", model, mRambut, pusat + new Vector3(s * 0.041f, 0.03f, 0.097f), new Vector3(0.04f, 0.008f, 0.01f), 0f, 0f, s * 7f));
        }
        bagian.Add(Ball("Hidung", model, mKulit, pusat + new Vector3(0f, -0.022f, 0.108f), 0.018f, new Vector3(1f, 1.15f, 1f)));
        bagian.Add(Kotak("Mulut", model, mMata, pusat + new Vector3(0f, -0.058f, 0.098f), new Vector3(0.038f, 0.006f, 0.008f), 0f, 0f, 0f));

        // rambut pendek: setengah bola + belakang kepala
        var topiRambut = MeshObj("Rambut", model, Lathe(SetengahBola(0.111f, 1.1f), 32, 1.02f), mRambut);
        topiRambut.transform.position = pusat + new Vector3(0f, 0.012f, -0.004f);
        bagian.Add(topiRambut);
        bagian.Add(Ball("RambutBelakang", model, mRambut, pusat + new Vector3(0f, -0.03f, -0.035f), 0.097f, new Vector3(1f, 0.95f, 0.9f)));

        // ikat kepala batik + simpul di sisi kanan
        var ikat = Band("IkatKepala", model, mIkat, 0.113f, 0.117f, 0.05f, pusat.y + 0.043f, 1f);
        ikat.transform.position += new Vector3(0f, 0f, pusat.z);
        bagian.Add(ikat);
        bagian.Add(Ball("SimpulIkat", model, mIkat, pusat + new Vector3(0.108f, 0.05f, 0.055f), 0.03f, new Vector3(1f, 1f, 0.8f)));
        bagian.Add(Kotak("UjungIkat1", model, mIkat, pusat + new Vector3(0.118f, 0.0f, 0.05f), new Vector3(0.03f, 0.08f, 0.008f), 0f, 0f, 11f));
        bagian.Add(Kotak("UjungIkat2", model, mIkat, pusat + new Vector3(0.13f, 0.01f, 0.045f), new Vector3(0.026f, 0.06f, 0.008f), 0f, 0f, -14f));

        Para(pivot, bagian);
    }

    // ---------------------------------------------------------------- helper bentuk

    static float R(float y)
    {
        for (int i = 0; i < Profil.Length - 1; i++)
        {
            Vector2 a = Profil[i], b = Profil[i + 1];
            if (y >= a.y && y <= b.y) return a.x + (b.x - a.x) * ((y - a.y) / (b.y - a.y));
        }
        return Profil[Profil.Length - 1].x;
    }

    Transform Pivot(string nama, Vector3 pos)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(model, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    static void Para(Transform pivot, List<GameObject> bagian)
    {
        foreach (var g in bagian) g.transform.SetParent(pivot, true);
    }

    static GameObject Prim(PrimitiveType tipe, string nama, Transform induk, Material m)
    {
        var g = GameObject.CreatePrimitive(tipe);
        g.name = nama;
        var col = g.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);
        g.GetComponent<Renderer>().sharedMaterial = m;
        g.transform.SetParent(induk, false);
        return g;
    }

    static GameObject Ball(string nama, Transform induk, Material m, Vector3 pos, float r, Vector3 kali)
    {
        var g = Prim(PrimitiveType.Sphere, nama, induk, m);
        g.transform.localPosition = pos;
        g.transform.localScale = new Vector3(2f * r * kali.x, 2f * r * kali.y, 2f * r * kali.z);
        return g;
    }

    static GameObject Kotak(string nama, Transform induk, Material m, Vector3 pos, Vector3 ukuran, float rx, float ry, float rz)
    {
        var g = Prim(PrimitiveType.Cube, nama, induk, m);
        g.transform.localPosition = pos;
        g.transform.localScale = ukuran;
        g.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        return g;
    }

    // Silinder dari titik a ke b (radius rata-rata, karena silinder primitif tidak bisa meruncing)
    static GameObject Seg(string nama, Transform induk, Material m, Vector3 a, Vector3 b, float r1, float r2)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        float r = (r1 + r2) * 0.5f;
        var g = Prim(PrimitiveType.Cylinder, nama, induk, m);
        g.transform.localPosition = (a + b) * 0.5f;
        g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        g.transform.localScale = new Vector3(2f * r, len * 0.5f, 2f * r);
        return g;
    }

    static GameObject Band(string nama, Transform induk, Material m, float rBawah, float rAtas, float tinggi, float y, float zs)
    {
        var prof = new List<Vector2> { new Vector2(rBawah, -tinggi * 0.5f), new Vector2(rAtas, tinggi * 0.5f) };
        var g = MeshObj(nama, induk, Lathe(prof, 52, zs), m);
        g.transform.localPosition = new Vector3(0f, y, 0f);
        return g;
    }

    static GameObject MeshObj(string nama, Transform induk, Mesh mesh, Material m)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(induk, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    static List<Vector2> SetengahBola(float r, float tinggiKali)
    {
        var p = new List<Vector2>();
        for (int n = 0; n <= 8; n++)
        {
            float f = n / 8f * Mathf.PI * 0.5f;
            p.Add(new Vector2(r * Mathf.Cos(f), r * Mathf.Sin(f) * tinggiKali));
        }
        return p;
    }

    static List<Vector2> Haluskan(Vector2[] p, int per)
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

    // Mesh putar (lathe) dua sisi: sisi luar dan dalam sama-sama terlihat dengan normal benar.
    static Mesh Lathe(List<Vector2> prof, int segs, float zs)
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

    static void SafeDestroy(Object o)
    {
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
