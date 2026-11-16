using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generator map desa prosedural untuk Unity (URP atau Built-in).
/// Balai desa di pusat (0,0), jalan salib + jalan lingkar + jalan samping,
/// rumah di kiri-kanan jalan (menghadap jalan), lampu jalan, dan pohon.
/// Cara pakai: GameObject kosong di posisi (0,0,0) -> Add Component "DesaGenerator" -> Play,
/// atau klik kanan komponen -> "Bangun Desa" (di Editor). Ubah seed untuk tata letak berbeda.
/// </summary>
public class DesaGenerator : MonoBehaviour
{
    public int seed = 7;
    public float panjangJalan = 84f;     // jarak jalan utama dari pusat
    public float jarakRumah = 10f;       // jarak antar rumah di sepanjang jalan
    public int jumlahPohon = 22;        // sedikit saja, supaya tidak terasa seperti hutan
    public int jumlahSawah = 14;         // petak sawah di lahan kosong antar jalan
    public bool malam = false;           // true = lampu jalan menyala
    public bool bangunSaatMulai = true;
    public bool aturKamera = false;      // false default di RPG agar tidak menimpa CameraRig utama
    public bool tambahCollider = true;   // true = dinding rumah & rintangan memiliki collider fisik

    struct Kotak { public float x0, z0, x1, z1; }
    struct Ruas { public float x0, z0, x1, z1, lebar; public bool horizontal; }

    readonly List<Kotak> jalan = new List<Kotak>();
    readonly List<Kotak> bangunan = new List<Kotak>();
    readonly List<Ruas> ruas = new List<Ruas>();
    readonly List<Vector3> posLampu = new List<Vector3>();
    System.Random rnd;
    Material[] mDinding, mAtap, mDaun, mPadi;
    Material mBund;
    Material mAspal, mBatu, mJalanBatu, mKayu, mKaca, mPintu, mTiang, mLampu, mHijau, mBatang, mMerah, mPutih;

    static Kotak K(float cx, float cz, float hx, float hz)
    { return new Kotak { x0 = cx - hx, x1 = cx + hx, z0 = cz - hz, z1 = cz + hz }; }
    static bool Tabrak(Kotak a, Kotak b, float m)
    { return a.x0 - m < b.x1 && a.x1 + m > b.x0 && a.z0 - m < b.z1 && a.z1 + m > b.z0; }
    bool Bebas(Kotak k, float mJalan, float mBangunan)
    {
        foreach (Kotak j in jalan) if (Tabrak(k, j, mJalan)) return false;
        foreach (Kotak b in bangunan) if (Tabrak(k, b, mBangunan)) return false;
        return true;
    }
    float R(float a, float b) { return a + (b - a) * (float)rnd.NextDouble(); }
    static void Hapus(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

    static Material Mat(Color c, float halus = 0.15f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh); m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", halus);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", halus);
        return m;
    }
    static Color C(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }

    void Awake() { if (bangunSaatMulai && transform.childCount == 0) Bangun(); }

    [ContextMenu("Bangun Desa")]
    public void Bangun()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Hapus(transform.GetChild(i).gameObject);
        jalan.Clear(); bangunan.Clear(); ruas.Clear(); posLampu.Clear();
        rnd = new System.Random(seed);

        mDinding = new[] { Mat(C(0xE8DCC0)), Mat(C(0xF2EFE6)), Mat(C(0xD9C7A0)), Mat(C(0xB8D4C8)), Mat(C(0xE6B8A2)), Mat(C(0xC9D6E8)) };
        mAtap = new[] { Mat(C(0xA34B2E)), Mat(C(0x8C3B25)), Mat(C(0x5E6670)), Mat(C(0x6B4A3A)) };
        mDaun = new[] { Mat(C(0x2F6B2A)), Mat(C(0x3F8A35)), Mat(C(0x265A28)) };
        mAspal = Mat(C(0x3A3A3E), 0.1f); mBatu = Mat(C(0xB9B4A8)); mJalanBatu = Mat(C(0x9C978B));
        mKayu = Mat(C(0x7A5230)); mKaca = Mat(C(0x7FB3D5), 0.8f); mPintu = Mat(C(0x5A3A22));
        mTiang = Mat(C(0x2B2B2B), 0.3f); mHijau = Mat(C(0x4C8C3A), 0.05f); mBatang = Mat(C(0x5A3F28));
        mMerah = Mat(C(0xCC2222)); mPutih = Mat(C(0xF5F5F5));
        mPadi = new[] { Mat(C(0x9BC24A), 0.05f), Mat(C(0xB5C95A), 0.05f), Mat(C(0x7FB040), 0.05f) };
        mBund = Mat(C(0x8A6B3F), 0.05f);
        mLampu = Mat(C(0xFFE9A8), 0.5f);
        mLampu.EnableKeyword("_EMISSION");
        mLampu.SetColor("_EmissionColor", malam ? C(0xFFD27A) * 3f : Color.black);

        // tanah rumput 240 x 240 m
        GameObject tanah = GameObject.CreatePrimitive(PrimitiveType.Plane);
        tanah.name = "Tanah"; tanah.transform.SetParent(transform, false);
        tanah.transform.localScale = new Vector3(24, 1, 24);
        tanah.GetComponent<Renderer>().sharedMaterial = mHijau;

        // alun-alun (plaza) di pusat
        bangunan.Add(K(0, 0, 13, 13));

        float L = panjangJalan, p = 13f, r = 20f, s = 42f;
        // jalan utama (salib)
        Jalan(p, 0, L, 0, 6); Jalan(-L, 0, -p, 0, 6); Jalan(0, p, 0, L, 6); Jalan(0, -L, 0, -p, 6);
        // jalan lingkar mengelilingi alun-alun
        Jalan(-r, r, r, r, 5); Jalan(-r, -r, r, -r, 5); Jalan(r, -r, r, r, 5); Jalan(-r, -r, -r, r, 5);
        // jalan samping
        Jalan(-L, s, L, s, 5); Jalan(-L, -s, L, -s, 5); Jalan(s, -L, s, L, 5); Jalan(-s, -L, -s, L, 5);

        Kubus(transform, "Alun-alun", new Vector3(0, 0.07f, 0), new Vector3(26, 0.14f, 26), mBatu);
        BuatBalaiDesa();

        // rumah di sepanjang setiap ruas jalan (jalan lingkar dulu supaya dekat pusat padat)
        int jumlahRumah = 0;
        foreach (Ruas rs in ruas) jumlahRumah += RumahDiRuas(rs);

        // lampu jalan
        foreach (Ruas rs in ruas) LampuDiRuas(rs);
        foreach (Vector2 q in new[] { new Vector2(11, 9), new Vector2(-11, 9), new Vector2(11, -9), new Vector2(-11, -9),
                                      new Vector2(11, 0), new Vector2(-11, 0), new Vector2(0, -10), new Vector2(0, 10.5f) })
            Lampu(new Vector3(q.x, 0.14f, q.y));

        BuatSawah();
        BuatPohon();
        AturSiangMalam();
        if (aturKamera && Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 95, -120);
            Camera.main.transform.LookAt(Vector3.zero);
            Camera.main.farClipPlane = 600;
        }
        Debug.Log("Desa dibangun: " + jumlahRumah + " rumah, " + posLampu.Count + " lampu.");
    }

    // ---------------------------------------------------------------- jalan
    void Jalan(float x0, float z0, float x1, float z1, float w)
    {
        Ruas rs = new Ruas { x0 = x0, z0 = z0, x1 = x1, z1 = z1, lebar = w, horizontal = Mathf.Approximately(z0, z1) };
        ruas.Add(rs);
        float e = w / 2f;   // perpanjang ujung agar sudut/perempatan tertutup
        float hx = rs.horizontal ? (x1 - x0) / 2f + e : e;
        float hz = rs.horizontal ? e : (z1 - z0) / 2f + e;
        float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
        jalan.Add(K(cx, cz, hx, hz));
        Kubus(transform, "Jalan", new Vector3(cx, 0.05f, cz), new Vector3(hx * 2, 0.1f, hz * 2), mAspal);
        // garis tengah putus-putus
        float len = rs.horizontal ? x1 - x0 : z1 - z0;
        for (float d = 2; d < len - 1; d += 5)
        {
            Vector3 pos = rs.horizontal ? new Vector3(x0 + d, 0.105f, z0) : new Vector3(x0, 0.105f, z0 + d);
            Vector3 sc = rs.horizontal ? new Vector3(2f, 0.01f, 0.18f) : new Vector3(0.18f, 0.01f, 2f);
            Kubus(transform, "Marka", pos, sc, mPutih);
        }
    }

    // ---------------------------------------------------------------- rumah
    int RumahDiRuas(Ruas rs)
    {
        int n = 0;
        float len = rs.horizontal ? rs.x1 - rs.x0 : rs.z1 - rs.z0;
        float start = rs.horizontal ? rs.x0 : rs.z0;
        for (float d = 0; d <= len; d += jarakRumah)
            foreach (int side in new[] { 1, -1 })
            {
                float along = start + d + R(-1.5f, 1.5f);
                float w = R(5f, 8f), dep = R(5f, 7f), h = R(2.7f, 3.3f);
                float off = rs.lebar / 2f + R(2f, 3.5f) + dep / 2f + 0.6f;
                float cx, cz, yaw; Kotak k;
                if (rs.horizontal)
                {
                    cx = along; cz = rs.z0 + side * off; yaw = side > 0 ? 180 : 0;
                    k = K(cx, cz, w / 2 + 0.6f, dep / 2 + 0.6f);
                }
                else
                {
                    cz = along; cx = rs.x0 + side * off; yaw = side > 0 ? 270 : 90;
                    k = K(cx, cz, dep / 2 + 0.6f, w / 2 + 0.6f);
                }
                if (!Bebas(k, 0.5f, 1.2f)) continue;
                bangunan.Add(k);
                BuatRumah(new Vector3(cx, 0.1f, cz), yaw, w, dep, h);
                n++;
            }
        return n;
    }

    void BuatRumah(Vector3 pos, float yaw, float w, float dep, float h)
    {
        Transform g = Kosong("Rumah", transform, pos, yaw);
        Material md = mDinding[rnd.Next(mDinding.Length)], ma = mAtap[rnd.Next(mAtap.Length)];
        Kubus(g, "Dinding", new Vector3(0, h / 2, 0), new Vector3(w, h, dep), md);
        // atap pelana, bubungan sejajar jalan (sumbu X lokal)
        PasangMesh("Atap", g, new Vector3(0, h, 0), Quaternion.Euler(0, 90, 0), Prisma(dep + 1.2f, w + 1.2f, Mathf.Min(w, dep) * 0.38f), ma);
        Kubus(g, "Pintu", new Vector3(R(-0.6f, 0.6f), 0.95f, dep / 2 + 0.02f), new Vector3(0.9f, 1.9f, 0.06f), mPintu);
        Kubus(g, "Teras", new Vector3(0, 0.08f, dep / 2 + 0.35f), new Vector3(2.4f, 0.16f, 0.7f), mBatu);
        foreach (int s in new[] { 1, -1 })
        {
            Kubus(g, "Jendela", new Vector3(s * (w / 2 - 1.2f), 1.6f, dep / 2 + 0.02f), new Vector3(0.9f, 0.8f, 0.06f), mKaca);
            Kubus(g, "JendelaSamping", new Vector3(s * (w / 2 + 0.02f), 1.6f, 0), new Vector3(0.06f, 0.8f, 0.9f), mKaca);
        }
    }

    // ---------------------------------------------------------------- balai desa
    void BuatBalaiDesa()
    {
        Transform g = Kosong("BalaiDesa", transform, Vector3.zero, 0);
        Kubus(g, "Pondasi", new Vector3(0, 0.35f, 0), new Vector3(18, 0.5f, 13), mBatu);
        float t0 = 0.6f;                                  // tinggi lantai
        Kubus(g, "Dinding", new Vector3(0, t0 + 2.25f, -0.8f), new Vector3(14, 4.5f, 8.4f), mDinding[1]);
        Kubus(g, "Pintu", new Vector3(0, t0 + 1.4f, 3.45f), new Vector3(2.4f, 2.8f, 0.1f), mPintu);
        foreach (int s in new[] { -1, 1 })
            foreach (float x in new[] { 3.6f, 5.8f })
                Kubus(g, "Jendela", new Vector3(s * x, t0 + 2.3f, 3.45f), new Vector3(1.4f, 1.4f, 0.1f), mKaca);
        // teras depan dengan tiang
        for (int i = 0; i < 6; i++)
            Silinder(g, "Tiang", new Vector3(-6.25f + i * 2.5f, t0 + 2.25f, 5.6f), 0.25f, 4.5f, mPutih);
        // tangga
        Kubus(g, "Tangga1", new Vector3(0, 0.3f, 7.0f), new Vector3(7, 0.3f, 0.8f), mBatu);
        Kubus(g, "Tangga2", new Vector3(0, 0.45f, 6.5f), new Vector3(7, 0.55f, 0.8f), mBatu);
        // atap bertingkat (limas)
        float ya = t0 + 4.5f;
        PasangMesh("Atap1", g, new Vector3(0, ya, 0.4f), Quaternion.identity, Limas(20, 15, 3.2f), mAtap[0]);
        Kubus(g, "Menara", new Vector3(0, ya + 2.1f, 0.4f), new Vector3(5f, 1.5f, 3.6f), mDinding[1]);
        PasangMesh("Atap2", g, new Vector3(0, ya + 2.85f, 0.4f), Quaternion.identity, Limas(8, 6, 2.2f), mAtap[0]);
        // papan nama
        Kubus(g, "Papan", new Vector3(0, t0 + 4.9f, 6.2f), new Vector3(6.5f, 0.9f, 0.15f), mMerah);
        GameObject tg = new GameObject("TeksBalai");
        tg.transform.SetParent(g, false); tg.transform.localPosition = new Vector3(0, t0 + 4.9f, 6.35f);
        TextMesh tm = tg.AddComponent<TextMesh>();
        tm.text = "BALAI DESA"; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; tm.characterSize = 0.1f; tm.color = Color.white;
#if UNITY_2022_2_OR_NEWER
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null)
        {
            tm.font = f;
            tg.GetComponent<MeshRenderer>().sharedMaterial = f.material;
        }
        // tiang bendera
        Silinder(g, "TiangBendera", new Vector3(9.5f, 4.6f, 8f), 0.08f, 9f, mTiang);
        Kubus(g, "BenderaMerah", new Vector3(10.4f, 8.5f, 8f), new Vector3(1.8f, 0.6f, 0.04f), mMerah);
        Kubus(g, "BenderaPutih", new Vector3(10.4f, 7.9f, 8f), new Vector3(1.8f, 0.6f, 0.04f), mPutih);
    }

    // ---------------------------------------------------------------- lampu jalan
    void LampuDiRuas(Ruas rs)
    {
        float len = rs.horizontal ? rs.x1 - rs.x0 : rs.z1 - rs.z0;
        float start = rs.horizontal ? rs.x0 : rs.z0;
        int i = 0;
        for (float d = 4; d < len; d += 16, i++)
        {
            float off = (i % 2 == 0 ? 1 : -1) * (rs.lebar / 2f + 0.8f);
            Vector3 pos = rs.horizontal ? new Vector3(start + d, 0.1f, rs.z0 + off) : new Vector3(rs.x0 + off, 0.1f, start + d);
            if (Mathf.Abs(pos.x) < 14 && Mathf.Abs(pos.z) < 14) continue;     // sudah ada lampu alun-alun
            Kotak kl = K(pos.x, pos.z, 0.3f, 0.3f);
            bool kena = false;                                               // jangan menabrak rumah
            foreach (Kotak b in bangunan) if (Tabrak(kl, b, 0.3f)) { kena = true; break; }
            if (kena) continue;
            Lampu(pos);
        }
    }

    void Lampu(Vector3 pos)
    {
        foreach (Vector3 q in posLampu) if ((q - pos).sqrMagnitude < 36f) return;   // minimal 6 m antar lampu
        posLampu.Add(pos);
        Transform g = Kosong("LampuJalan", transform, pos, 0);
        Silinder(g, "Tiang", new Vector3(0, 2.5f, 0), 0.08f, 5f, mTiang);
        Kubus(g, "Tutup", new Vector3(0, 5.15f, 0), new Vector3(0.6f, 0.08f, 0.6f), mTiang);
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Hapus(b.GetComponent<Collider>());
        b.name = "Bohlam"; b.transform.SetParent(g, false);
        b.transform.localPosition = new Vector3(0, 4.95f, 0); b.transform.localScale = Vector3.one * 0.45f;
        b.GetComponent<Renderer>().sharedMaterial = mLampu;
        if (malam)
        {
            Light l = b.AddComponent<Light>();
            l.type = LightType.Point; l.range = 14; l.intensity = 1.4f;
            l.color = C(0xFFD58A); l.shadows = LightShadows.None;
        }
    }

    // ---------------------------------------------------------------- pohon
    void BuatPohon()
    {
        float lim = panjangJalan + 10f;
        for (int i = 0; i < jumlahPohon; i++)
        {
            float x = R(-lim, lim), z = R(-lim, lim), s = R(0.8f, 1.25f);
            Kotak k = K(x, z, s, s);
            if (!Bebas(k, 1.5f, 0.8f)) continue;
            bangunan.Add(k);
            Transform g = Kosong("Pohon", transform, new Vector3(x, 0, z), R(0, 360));
            Silinder(g, "Batang", new Vector3(0, 1.1f * s, 0), 0.2f * s, 2.2f * s, mBatang);
            GameObject d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Hapus(d.GetComponent<Collider>());
            d.name = "Daun"; d.transform.SetParent(g, false);
            d.transform.localPosition = new Vector3(0, 2.8f * s, 0); d.transform.localScale = new Vector3(2.6f * s, 2.3f * s, 2.6f * s);
            d.GetComponent<Renderer>().sharedMaterial = mDaun[rnd.Next(mDaun.Length)];
        }
    }

    // petak sawah di lahan kosong (pematang coklat + padi hijau)
    void BuatSawah()
    {
        float lim = panjangJalan + 6f; int n = 0;
        for (int i = 0; i < 400 && n < jumlahSawah; i++)
        {
            float w = R(12f, 22f), d = R(10f, 18f), x = R(-lim, lim), z = R(-lim, lim);
            Kotak k = K(x, z, w / 2 + 0.4f, d / 2 + 0.4f);
            if (!Bebas(k, 1.5f, 1f)) continue;
            bangunan.Add(k); n++;
            Kubus(transform, "Pematang", new Vector3(x, 0.04f, z), new Vector3(w + 0.8f, 0.08f, d + 0.8f), mBund);
            Kubus(transform, "Sawah", new Vector3(x, 0.07f, z), new Vector3(w, 0.08f, d), mPadi[rnd.Next(mPadi.Length)]);
        }
    }

    void AturSiangMalam()
    {
#if UNITY_2023_1_OR_NEWER
        Light[] semua = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        Light[] semua = FindObjectsOfType<Light>();
#endif
        foreach (Light l in semua)
            if (l.type == LightType.Directional)
            {
                l.intensity = malam ? 0.12f : 1.1f;
                l.color = malam ? C(0x6F86C8) : C(0xFFF4E0);
            }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = malam ? C(0x161B2E) : C(0x8FA6C0);
    }

    // ---------------------------------------------------------------- pembuat bentuk
    Transform Kosong(string nama, Transform induk, Vector3 pos, float yaw)
    {
        GameObject g = new GameObject(nama);
        g.transform.SetParent(induk, false);
        g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        return g.transform;
    }

    void Kubus(Transform induk, string nama, Vector3 pos, Vector3 skala, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bool butuhCollider = tambahCollider && (nama == "Dinding" || nama == "Pondasi" || nama == "Pematang" || nama == "Menara");
        if (!butuhCollider) Hapus(g.GetComponent<Collider>());
        g.name = nama; g.transform.SetParent(induk, false);
        g.transform.localPosition = pos; g.transform.localScale = skala;
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    void Silinder(Transform induk, string nama, Vector3 pos, float r, float tinggi, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bool butuhCollider = tambahCollider && (nama == "Batang" || nama == "Tiang");
        if (!butuhCollider) Hapus(g.GetComponent<Collider>());
        g.name = nama; g.transform.SetParent(induk, false);
        g.transform.localPosition = pos; g.transform.localScale = new Vector3(2 * r, tinggi / 2f, 2 * r);
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    void PasangMesh(string nama, Transform induk, Vector3 pos, Quaternion rot, Mesh mesh, Material m)
    {
        GameObject g = new GameObject(nama);
        g.transform.SetParent(induk, false);
        g.transform.localPosition = pos; g.transform.localRotation = rot;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        if (tambahCollider && nama.StartsWith("Atap"))
        {
            var mc = g.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }
    }

    // mesh cembung: tiap muka (3/4 titik) dipecah segitiga, arah muka otomatis menghadap keluar, shading datar
    static Mesh BentukCembung(Vector3 pusat, params Vector3[][] muka)
    {
        List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
        foreach (Vector3[] m in muka)
        {
            Vector3 c = Vector3.zero; foreach (Vector3 p in m) c += p; c /= m.Length;
            for (int i = 1; i < m.Length - 1; i++)
            {
                Vector3 a = m[0], b = m[i], d = m[i + 1];
                if (Vector3.Dot(Vector3.Cross(b - a, d - a), c - pusat) < 0) { Vector3 tmp = b; b = d; d = tmp; }
                t.Add(v.Count); v.Add(a); t.Add(v.Count); v.Add(b); t.Add(v.Count); v.Add(d);
            }
        }
        Mesh mesh = new Mesh { name = "Atap" };
        mesh.SetVertices(v); mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    // atap pelana: lebar 'span' (X), panjang 'len' (Z), tinggi bubungan h
    static Mesh Prisma(float span, float len, float h)
    {
        float a = span / 2, b = len / 2;
        Vector3 A = new Vector3(-a, 0, -b), B = new Vector3(a, 0, -b), Cc = new Vector3(a, 0, b), D = new Vector3(-a, 0, b);
        Vector3 R1 = new Vector3(0, h, -b), R2 = new Vector3(0, h, b);
        return BentukCembung(new Vector3(0, h / 3, 0), new[] { A, D, R2, R1 }, new[] { B, Cc, R2, R1 },
                             new[] { A, B, R1 }, new[] { D, Cc, R2 }, new[] { A, B, Cc, D });
    }

    // atap limas: alas w x d, tinggi h
    static Mesh Limas(float w, float d, float h)
    {
        float a = w / 2, b = d / 2;
        Vector3 A = new Vector3(-a, 0, -b), B = new Vector3(a, 0, -b), Cc = new Vector3(a, 0, b), D = new Vector3(-a, 0, b);
        Vector3 P = new Vector3(0, h, 0);
        return BentukCembung(new Vector3(0, h / 4, 0), new[] { A, B, P }, new[] { B, Cc, P },
                             new[] { Cc, D, P }, new[] { D, A, P }, new[] { A, B, Cc, D });
    }
}

