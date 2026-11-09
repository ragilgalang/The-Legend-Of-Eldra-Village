#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator Anjing 3D Berdasarkan Spesifikasi Ras Biologis Nyata (Porting dari Skrip Blender).
/// Menghasilkan 6 ras anjing dengan proporsi anatomis, telinga, ekor, dan warna bulu autentik:
/// 1. Kintamani (Anjing Kintamani Bali - Bulu leher ruff singa, ekor melengkung indah, telinga tegak)
/// 2. Golden Retriever (Bulu emas tebal, dada bidang, telinga jatuh ke bawah, moncong hangat)
/// 3. German Shepherd (Postur atletis, pelana hitam kecokelatan, telinga tegak gagah)
/// 4. French Bulldog (Tubuh gempal berotot, telinga kelelawar lebar, moncong pendek)
/// 5. Dachshund (Badan panjang khas, kaki pendek kokoh, telinga panjang menjuntai)
/// 6. Singing Dog (Anjing rimba lincah, ekor lebat, telinga runcing)
/// 
/// 100% Anti-Melayang: Kaki anjing dirancang tepat menapak tanah di Y = 0 dengan tumpuan proporsional.
/// </summary>
public static class RealisticDogBuilder
{
    public enum DogBreed
    {
        Kintamani,
        GoldenRetriever,
        GermanShepherd,
        FrenchBulldog,
        Dachshund,
        SingingDog
    }

    public struct EarSpec
    {
        public Vector3 size;
        public float taper;
        public Vector3 off;
        public float tilt;
        public float pitch;
        public float anchor; // 1 = tegak, -1 = jatuh ke bawah
    }

    public struct DogSpec
    {
        public string name;
        public bool ruff;
        public float H, L;
        public float hip_z, mid_z, sh_z;
        public float hip_r, mid_r, chest_r;
        public float neck_len, neck_rise;
        public float neck_r0, neck_r1;
        public float sr, muz_len, muz_r;
        public bool jowl;
        public float leg_r, hind_r, paw_r;
        public float ly_f, ly_h, width;
        public Vector2[] tail;
        public float tail_r;
        public EarSpec ear;
        public Color lowColor, highColor;
        public Color? muzzleColor;
        public float back_x;
        public float z_lo, z_hi;
        public Color earColor;
        public float eye_f;
    }

    public static readonly Dictionary<DogBreed, DogSpec> BreedSpecs = new Dictionary<DogBreed, DogSpec>
    {
        [DogBreed.Kintamani] = new DogSpec
        {
            name = "Kintamani", ruff = true, H = 0.50f, L = 0.50f, hip_z = 0.35f, mid_z = 0.34f, sh_z = 0.37f,
            hip_r = 0.12f, mid_r = 0.108f, chest_r = 0.14f, neck_len = 0.14f, neck_rise = 45f,
            neck_r0 = 0.105f, neck_r1 = 0.095f, sr = 0.068f, muz_len = 0.08f, muz_r = 0.033f, jowl = false,
            leg_r = 0.04f, hind_r = 0.055f, paw_r = 0.035f, ly_f = 0.075f, ly_h = 0.085f, width = 0.95f,
            tail = new Vector2[] { new Vector2(-0.03f, 0.07f), new Vector2(-0.03f, 0.07f), new Vector2(0.02f, 0.06f), new Vector2(0.07f, 0.02f), new Vector2(0.07f, -0.04f) },
            tail_r = 0.04f,
            ear = new EarSpec { size = new Vector3(0.45f, 0.14f, 0.62f), taper = 0.55f, off = new Vector3(-0.1f, 0.62f, 0.8f), tilt = 10f, pitch = 0f, anchor = 1f },
            lowColor = new Color(0.88f, 0.84f, 0.76f), highColor = new Color(0.72f, 0.56f, 0.38f),
            muzzleColor = null, back_x = 9f, z_lo = 0.25f, z_hi = 0.45f,
            earColor = new Color(0.68f, 0.55f, 0.40f), eye_f = 0.15f
        },

        [DogBreed.GoldenRetriever] = new DogSpec
        {
            name = "Golden Retriever", ruff = true, H = 0.58f, L = 0.60f, hip_z = 0.42f, mid_z = 0.405f, sh_z = 0.42f,
            hip_r = 0.14f, mid_r = 0.125f, chest_r = 0.15f, neck_len = 0.22f, neck_rise = 50f,
            neck_r0 = 0.095f, neck_r1 = 0.075f, sr = 0.078f, muz_len = 0.11f, muz_r = 0.042f, jowl = true,
            leg_r = 0.045f, hind_r = 0.06f, paw_r = 0.04f, ly_f = 0.085f, ly_h = 0.095f, width = 0.9f,
            tail = new Vector2[] { new Vector2(-0.10f, -0.01f), new Vector2(-0.10f, -0.04f), new Vector2(-0.09f, 0f), new Vector2(-0.08f, 0.04f), new Vector2(-0.05f, 0.05f) },
            tail_r = 0.032f,
            ear = new EarSpec { size = new Vector3(0.5f, 0.14f, 0.9f), taper = -0.25f, off = new Vector3(-0.2f, 0.75f, 0.55f), tilt = 12f, pitch = 0f, anchor = -1f },
            lowColor = new Color(0.85f, 0.52f, 0.18f), highColor = new Color(0.65f, 0.32f, 0.08f),
            muzzleColor = null, back_x = 9f, z_lo = 0.2f, z_hi = 0.5f,
            earColor = new Color(0.55f, 0.25f, 0.05f), eye_f = 0.15f
        },

        [DogBreed.GermanShepherd] = new DogSpec
        {
            name = "German Shepherd", ruff = true, H = 0.62f, L = 0.66f, hip_z = 0.40f, mid_z = 0.42f, sh_z = 0.46f,
            hip_r = 0.14f, mid_r = 0.12f, chest_r = 0.155f, neck_len = 0.24f, neck_rise = 40f,
            neck_r0 = 0.09f, neck_r1 = 0.075f, sr = 0.075f, muz_len = 0.125f, muz_r = 0.04f, jowl = false,
            leg_r = 0.045f, hind_r = 0.062f, paw_r = 0.04f, ly_f = 0.085f, ly_h = 0.095f, width = 0.85f,
            tail = new Vector2[] { new Vector2(-0.08f, -0.04f), new Vector2(-0.07f, -0.11f), new Vector2(-0.05f, -0.12f), new Vector2(-0.03f, -0.12f) },
            tail_r = 0.03f,
            ear = new EarSpec { size = new Vector3(0.62f, 0.16f, 1.15f), taper = 0.75f, off = new Vector3(-0.25f, 0.65f, 0.75f), tilt = 8f, pitch = 0f, anchor = 1f },
            lowColor = new Color(0.55f, 0.30f, 0.10f), highColor = new Color(0.08f, 0.06f, 0.04f),
            muzzleColor = new Color(0.08f, 0.06f, 0.04f), back_x = 0.38f, z_lo = 0.44f, z_hi = 0.56f,
            earColor = new Color(0.10f, 0.07f, 0.05f), eye_f = 0.15f
        },

        [DogBreed.FrenchBulldog] = new DogSpec
        {
            name = "French Bulldog", ruff = false, H = 0.30f, L = 0.36f, hip_z = 0.215f, mid_z = 0.20f, sh_z = 0.21f,
            hip_r = 0.09f, mid_r = 0.085f, chest_r = 0.10f, neck_len = 0.09f, neck_rise = 35f,
            neck_r0 = 0.085f, neck_r1 = 0.08f, sr = 0.095f, muz_len = 0.035f, muz_r = 0.05f, jowl = true,
            leg_r = 0.034f, hind_r = 0.045f, paw_r = 0.03f, ly_f = 0.075f, ly_h = 0.075f, width = 1.0f,
            tail = new Vector2[] { new Vector2(-0.035f, 0.015f) },
            tail_r = 0.02f,
            ear = new EarSpec { size = new Vector3(0.6f, 0.14f, 0.95f), taper = 0.35f, off = new Vector3(-0.1f, 0.6f, 0.8f), tilt = 8f, pitch = 0f, anchor = 1f },
            lowColor = new Color(0.55f, 0.32f, 0.14f), highColor = new Color(0.40f, 0.22f, 0.09f),
            muzzleColor = new Color(0.08f, 0.06f, 0.04f), back_x = 9f, z_lo = 0.1f, z_hi = 0.25f,
            earColor = new Color(0.35f, 0.18f, 0.08f), eye_f = 0.19f
        },

        [DogBreed.Dachshund] = new DogSpec
        {
            name = "Dachshund", ruff = false, H = 0.23f, L = 0.60f, hip_z = 0.165f, mid_z = 0.15f, sh_z = 0.15f,
            hip_r = 0.06f, mid_r = 0.055f, chest_r = 0.075f, neck_len = 0.15f, neck_rise = 40f,
            neck_r0 = 0.052f, neck_r1 = 0.045f, sr = 0.05f, muz_len = 0.10f, muz_r = 0.027f, jowl = false,
            leg_r = 0.026f, hind_r = 0.036f, paw_r = 0.028f, ly_f = 0.05f, ly_h = 0.05f, width = 0.9f,
            tail = new Vector2[] { new Vector2(-0.12f, 0f), new Vector2(-0.12f, 0.03f), new Vector2(-0.12f, 0.05f) },
            tail_r = 0.018f,
            ear = new EarSpec { size = new Vector3(0.62f, 0.14f, 0.8f), taper = -0.2f, off = new Vector3(-0.15f, 0.78f, 0.45f), tilt = 10f, pitch = 0f, anchor = -1f },
            lowColor = new Color(0.42f, 0.15f, 0.04f), highColor = new Color(0.28f, 0.08f, 0.02f),
            muzzleColor = new Color(0.15f, 0.05f, 0.02f), back_x = 9f, z_lo = 0.1f, z_hi = 0.22f,
            earColor = new Color(0.22f, 0.06f, 0.02f), eye_f = 0.16f
        },

        [DogBreed.SingingDog] = new DogSpec
        {
            name = "Singing Dog", ruff = false, H = 0.45f, L = 0.52f, hip_z = 0.33f, mid_z = 0.31f, sh_z = 0.33f,
            hip_r = 0.10f, mid_r = 0.085f, chest_r = 0.115f, neck_len = 0.17f, neck_rise = 45f,
            neck_r0 = 0.07f, neck_r1 = 0.058f, sr = 0.06f, muz_len = 0.095f, muz_r = 0.03f, jowl = false,
            leg_r = 0.034f, hind_r = 0.048f, paw_r = 0.03f, ly_f = 0.065f, ly_h = 0.075f, width = 0.85f,
            tail = new Vector2[] { new Vector2(-0.05f, 0.04f), new Vector2(-0.06f, 0.08f), new Vector2(-0.04f, 0.10f), new Vector2(0f, 0.10f), new Vector2(0.04f, 0.06f) },
            tail_r = 0.026f,
            ear = new EarSpec { size = new Vector3(0.5f, 0.13f, 0.75f), taper = 0.45f, off = new Vector3(-0.15f, 0.6f, 0.8f), tilt = 14f, pitch = 0f, anchor = 1f },
            lowColor = new Color(0.90f, 0.85f, 0.75f), highColor = new Color(0.60f, 0.28f, 0.08f),
            muzzleColor = null, back_x = 9f, z_lo = 0.12f, z_hi = 0.26f,
            earColor = new Color(0.60f, 0.28f, 0.08f), eye_f = 0.16f
        }
    };

    private const string DogFolder = "Assets/_Project/Art/ProceduralAnimals";

    public static Mesh GetOrCreateDogMesh(DogBreed breed)
    {
        if (!AssetDatabase.IsValidFolder(DogFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
                AssetDatabase.CreateFolder("Assets", "_Project/Art");
            AssetDatabase.CreateFolder("Assets/_Project/Art", "ProceduralAnimals");
        }

        string meshPath = $"{DogFolder}/Mesh_Dog_{breed}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing) return existing;

        var mesh = GenerateDogMesh(breed);
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    public static Material GetOrCreateDogMaterial(DogBreed breed)
    {
        string matPath = $"{DogFolder}/M_Dog_{breed}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        var sp = BreedSpecs[breed];
        mat.SetColor("_BaseColor", Color.white); // Menggunakan vertex color
        mat.SetFloat("_Smoothness", 0.20f);
        mat.SetFloat("_Metallic", 0.0f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GENERATOR MESH ANJING SESUAI SPESIFIKASI ANATOMIS
    // ─────────────────────────────────────────────────────────────────────

    private static Vector3 ToUnity(float bx, float by, float bz)
    {
        // Konversi koordinat Blender (X=depan, Y=samping, Z=atas) -> Unity (Z=depan, X=samping, Y=atas)
        return new Vector3(by, bz, bx);
    }

    private static Mesh GenerateDogMesh(DogBreed breed)
    {
        var sp = BreedSpecs[breed];
        var mesh = new Mesh();
        mesh.name = $"Dog_{breed}";

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var cols  = new List<Color>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        float H = sp.H;
        float L = sp.L;
        float k = H / 0.5f;

        // ── 1. TULANG BELAKANG / TORSO ──
        Vector3 hipPos   = ToUnity(-L / 2f, 0, sp.hip_z);
        Vector3 midPos   = ToUnity(0, 0, sp.mid_z);
        Vector3 chestPos = ToUnity(L / 2f, 0, sp.sh_z);

        AddCylinderSegment(verts, norms, cols, uvs, tris, hipPos, midPos, sp.hip_r, sp.mid_r, sp, 10);
        AddCylinderSegment(verts, norms, cols, uvs, tris, midPos, chestPos, sp.mid_r, sp.chest_r, sp, 10);
        AddSphere(verts, norms, cols, uvs, tris, hipPos, sp.hip_r, sp);
        AddSphere(verts, norms, cols, uvs, tris, chestPos, sp.chest_r, sp);

        // ── 2. LEHER & KEPALA ──
        Vector3 nbPos = ToUnity(L / 2f + 0.04f * k, 0, sp.sh_z + sp.chest_r * 0.5f);
        float riseRad = sp.neck_rise * Mathf.Deg2Rad;
        Vector3 ntPos = nbPos + ToUnity(sp.neck_len * Mathf.Cos(riseRad), 0, sp.neck_len * Mathf.Sin(riseRad));

        AddCylinderSegment(verts, norms, cols, uvs, tris, chestPos, nbPos, sp.chest_r * 0.9f, sp.neck_r0, sp, 8);
        AddCylinderSegment(verts, norms, cols, uvs, tris, nbPos, ntPos, sp.neck_r0, sp.neck_r1, sp, 8);

        Vector3 skullPos = ntPos + ToUnity(0.04f * k, 0, 0.01f * k);
        // Tengkorak kepala
        AddEllipsoid(verts, norms, cols, uvs, tris, skullPos, new Vector3(sp.sr * sp.width, sp.sr, sp.sr * 1.15f), sp.highColor);

        // Bulu leher ruff singa lebat jika ada (Kintamani / Golden / Shepherd)
        if (sp.ruff)
        {
            Vector3 ruffPos = nbPos + ToUnity(0.03f * k, 0, -0.06f * k);
            AddEllipsoid(verts, norms, cols, uvs, tris, ruffPos, new Vector3(sp.neck_r0 * 1.25f, sp.neck_r0 * 1.15f, sp.neck_r0 * 1.2f), sp.lowColor);
        }

        // ── 3. MONCONG & HIDUNG ──
        Vector3 m1Pos = skullPos + ToUnity(sp.sr * 0.6f + 0.4f * sp.muz_len, 0, -sp.sr * 0.30f);
        Vector3 m2Pos = skullPos + ToUnity(sp.sr * 0.6f + sp.muz_len, 0, -sp.sr * 0.42f);

        Color muzCol = sp.muzzleColor ?? sp.highColor;
        AddCylinderSegment(verts, norms, cols, uvs, tris, skullPos, m1Pos, sp.sr * 0.65f, sp.muz_r * 1.1f, sp, 8, muzCol);
        AddCylinderSegment(verts, norms, cols, uvs, tris, m1Pos, m2Pos, sp.muz_r * 1.1f, sp.muz_r * 0.95f, sp, 8, muzCol);

        // Hidung hitam lembap
        Vector3 nosePos = m2Pos + ToUnity(sp.muz_r * 0.75f, 0, sp.muz_r * 0.2f);
        AddEllipsoid(verts, norms, cols, uvs, tris, nosePos, new Vector3(sp.muz_r * 0.55f, sp.muz_r * 0.4f, sp.muz_r * 0.35f), new Color(0.02f, 0.02f, 0.02f));

        // ── 4. MATA ──
        float er = sp.sr * sp.eye_f * 1.25f;
        foreach (int s in new int[] { 1, -1 })
        {
            Vector3 eyeOff = ToUnity(0.62f, s * 0.72f, 0.30f).normalized * (sp.sr * 1.01f);
            AddEllipsoid(verts, norms, cols, uvs, tris, skullPos + eyeOff, new Vector3(er, er, er), new Color(0.04f, 0.02f, 0.01f));
        }

        // ── 5. TELINGA ──
        var ear = sp.ear;
        foreach (int s in new int[] { 1, -1 })
        {
            Vector3 earCenter = skullPos + ToUnity(ear.off.x * sp.sr, s * ear.off.y * sp.sr, ear.off.z * sp.sr);
            Vector3 earRadii = new Vector3(ear.size.y * sp.sr, ear.size.z * sp.sr, ear.size.x * sp.sr);
            Quaternion earRot = Quaternion.Euler(ear.pitch, ear.anchor > 0 ? -s * ear.tilt : s * ear.tilt, 0);

            AddTaperedBlob(verts, norms, cols, uvs, tris, earCenter, earRadii, earRot, ear.taper, ear.anchor, sp.earColor);
        }

        // ── 6. EKOR INDAH ──
        Vector3 tailCurr = ToUnity(-L / 2f - sp.hip_r * 0.7f, 0, sp.hip_z + sp.hip_r * 0.15f);
        float tRadius = sp.tail_r * 1.15f;
        int nTail = sp.tail.Length;

        for (int i = 0; i < nTail; i++)
        {
            Vector2 delta = sp.tail[i];
            Vector3 tailNext = tailCurr + ToUnity(delta.x, 0, delta.y);
            float nextRadius = sp.tail_r * (1.0f - 0.55f * (i + 1) / (float)nTail);
            AddCylinderSegment(verts, norms, cols, uvs, tris, tailCurr, tailNext, tRadius, nextRadius, sp, 6);
            tailCurr = tailNext;
            tRadius = nextRadius;
        }

        // ── 7. KAKI DEPAN (2 KAKI) ──
        float elbow_z = sp.sh_z - sp.chest_r * 0.9f;
        float wrist_z = sp.paw_r * 2.2f + H * 0.06f;
        float lr = sp.leg_r;

        foreach (int s in new int[] { 1, -1 })
        {
            float y = s * sp.ly_f;
            Vector3 sh = ToUnity(L / 2f - 0.02f, y, sp.sh_z - sp.chest_r * 0.45f);
            Vector3 elb = ToUnity(L / 2f - 0.045f * k, y * 0.95f, elbow_z);
            Vector3 wr  = ToUnity(L / 2f - 0.03f * k, y * 0.95f, wrist_z);
            // Kaki depan tepat menapak tanah Y = 0 (lantai)
            Vector3 paw = ToUnity(L / 2f + 0.01f * k, y * 0.95f, sp.paw_r * 0.92f);

            AddCylinderSegment(verts, norms, cols, uvs, tris, sh, elb, lr * 1.5f, lr * 1.15f, sp, 6);
            AddCylinderSegment(verts, norms, cols, uvs, tris, elb, wr, lr * 1.15f, lr * 0.82f, sp, 6);
            AddCylinderSegment(verts, norms, cols, uvs, tris, wr, paw, lr * 0.82f, sp.paw_r, sp, 6);

            // Telapak kaki (Paw)
            AddEllipsoid(verts, norms, cols, uvs, tris, paw, new Vector3(sp.paw_r * 1.1f, sp.paw_r * 0.65f, sp.paw_r * 1.3f), sp.lowColor);
        }

        // ── 8. KAKI BELAKANG (2 KAKI) ──
        float hock_z = Mathf.Max(H * 0.28f, sp.paw_r * 3.2f);
        float stifle_z = hock_z + (sp.hip_z - hock_z) * 0.45f;
        float hr = sp.hind_r;

        foreach (int s in new int[] { 1, -1 })
        {
            float y = s * sp.ly_h;
            Vector3 pel = ToUnity(-L / 2f, y, sp.hip_z - sp.hip_r * 0.35f);
            Vector3 stif = ToUnity(-L / 2f + 0.07f * k, y * 0.95f, stifle_z);
            Vector3 hock = ToUnity(-L / 2f - 0.06f * k, y * 0.95f, hock_z);
            // Kaki belakang tepat menapak tanah Y = 0 (lantai)
            Vector3 paw = ToUnity(-L / 2f - 0.045f * k, y * 0.95f, sp.paw_r * 0.92f);

            AddCylinderSegment(verts, norms, cols, uvs, tris, pel, stif, hr * 1.4f, hr * 1.1f, sp, 6);
            AddCylinderSegment(verts, norms, cols, uvs, tris, stif, hock, hr * 1.1f, hr * 0.65f, sp, 6);
            AddCylinderSegment(verts, norms, cols, uvs, tris, hock, paw, hr * 0.65f, sp.paw_r, sp, 6);

            // Telapak kaki belakang (Paw)
            AddEllipsoid(verts, norms, cols, uvs, tris, paw, new Vector3(sp.paw_r * 1.1f, sp.paw_r * 0.65f, sp.paw_r * 1.3f), sp.lowColor);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetColors(cols);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPER GEOMETRI PROSEDURAL (SILINDER, ELIPSOID, BLOB)
    // ─────────────────────────────────────────────────────────────────────

    private static Color EvaluateCoatColor(Vector3 pos, DogSpec sp)
    {
        float t = Mathf.InverseLerp(sp.z_lo, sp.z_hi, pos.y);
        Color c = Color.Lerp(sp.lowColor, sp.highColor, t);
        return c;
    }

    private static void AddCylinderSegment(
        List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<Vector2> uvs, List<int> tris,
        Vector3 a, Vector3 b, float rA, float rB, DogSpec sp, int sides = 8, Color? overrideCol = null)
    {
        Vector3 dir = (b - a).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;

        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        Vector3 up = Vector3.Cross(side, dir).normalized;

        int startIndex = verts.Count;

        for (int step = 0; step <= 1; step++)
        {
            Vector3 center = step == 0 ? a : b;
            float r = step == 0 ? rA : rB;
            Color col = overrideCol ?? EvaluateCoatColor(center, sp);

            for (int s = 0; s <= sides; s++)
            {
                float angle = (s / (float)sides) * Mathf.PI * 2f;
                Vector3 radial = (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * r;
                Vector3 pos = center + radial;

                verts.Add(pos);
                norms.Add(radial.normalized);
                cols.Add(col);
                uvs.Add(new Vector2((float)s / sides, (float)step));
            }
        }

        int stride = sides + 1;
        for (int s = 0; s < sides; s++)
        {
            int b0 = startIndex + s;
            int b1 = b0 + 1;
            int t0 = startIndex + stride + s;
            int t1 = t0 + 1;

            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(b1); tris.Add(t0); tris.Add(t1);
        }
    }

    private static void AddSphere(
        List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<Vector2> uvs, List<int> tris,
        Vector3 center, float r, DogSpec sp, int rings = 6, int sectors = 8)
    {
        AddEllipsoid(verts, norms, cols, uvs, tris, center, Vector3.one * r, EvaluateCoatColor(center, sp), rings, sectors);
    }

    private static void AddEllipsoid(
        List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<Vector2> uvs, List<int> tris,
        Vector3 center, Vector3 radii, Color color, int rings = 8, int sectors = 10)
    {
        int startIndex = verts.Count;

        for (int r = 0; r <= rings; r++)
        {
            float v = (float)r / rings;
            float phi = v * Mathf.PI;
            float y = Mathf.Cos(phi) * radii.y;
            float sinPhi = Mathf.Sin(phi);

            for (int s = 0; s <= sectors; s++)
            {
                float u = (float)s / sectors;
                float theta = u * Mathf.PI * 2f;
                float x = Mathf.Cos(theta) * sinPhi * radii.x;
                float z = Mathf.Sin(theta) * sinPhi * radii.z;

                Vector3 localPos = new Vector3(x, y, z);
                verts.Add(center + localPos);
                norms.Add(new Vector3(x / (radii.x * radii.x), y / (radii.y * radii.y), z / (radii.z * radii.z)).normalized);
                cols.Add(color);
                uvs.Add(new Vector2(u, v));
            }
        }

        int stride = sectors + 1;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sectors; s++)
            {
                int b0 = startIndex + r * stride + s;
                int b1 = b0 + 1;
                int t0 = startIndex + (r + 1) * stride + s;
                int t1 = t0 + 1;

                tris.Add(b0); tris.Add(t0); tris.Add(b1);
                tris.Add(b1); tris.Add(t0); tris.Add(t1);
            }
        }
    }

    private static void AddTaperedBlob(
        List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<Vector2> uvs, List<int> tris,
        Vector3 center, Vector3 radii, Quaternion rot, float taper, float anchor, Color color, int rings = 7, int sectors = 8)
    {
        int startIndex = verts.Count;

        for (int r = 0; r <= rings; r++)
        {
            float v = (float)r / rings;
            float phi = v * Mathf.PI;
            float lz = Mathf.Cos(phi);
            float sinPhi = Mathf.Sin(phi);

            float sTaper = 1.0f - taper * (lz + 1.0f) * 0.5f;

            for (int s = 0; s <= sectors; s++)
            {
                float u = (float)s / sectors;
                float theta = u * Mathf.PI * 2f;

                float lx = Mathf.Cos(theta) * sinPhi * sTaper * radii.x;
                float ly = Mathf.Sin(theta) * sinPhi * sTaper * radii.y;
                float lzPos = (lz + anchor) * radii.z;

                Vector3 local = rot * new Vector3(lx, lzPos, ly);
                verts.Add(center + local);
                norms.Add((rot * new Vector3(lx, lz, ly)).normalized);
                cols.Add(color);
                uvs.Add(new Vector2(u, v));
            }
        }

        int stride = sectors + 1;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sectors; s++)
            {
                int b0 = startIndex + r * stride + s;
                int b1 = b0 + 1;
                int t0 = startIndex + (r + 1) * stride + s;
                int t1 = t0 + 1;

                tris.Add(b0); tris.Add(t0); tris.Add(b1);
                tris.Add(b1); tris.Add(t0); tris.Add(t1);
            }
        }
    }

    // Warna per ras untuk material Dog.fbx
    private static readonly System.Collections.Generic.Dictionary<DogBreed, Color> BreedColors =
        new System.Collections.Generic.Dictionary<DogBreed, Color>
    {
        [DogBreed.Kintamani]       = new Color(0.88f, 0.84f, 0.76f),  // Putih krem Kintamani
        [DogBreed.GoldenRetriever] = new Color(0.85f, 0.52f, 0.14f),  // Emas keemasan
        [DogBreed.GermanShepherd]  = new Color(0.32f, 0.18f, 0.06f),  // Hitam cokelat sadel
        [DogBreed.Dachshund]       = new Color(0.46f, 0.26f, 0.08f),  // Cokelat kopi
        [DogBreed.FrenchBulldog]   = new Color(0.52f, 0.30f, 0.14f),  // Brindle tan
        [DogBreed.SingingDog]      = new Color(0.82f, 0.74f, 0.58f),  // Kuning tawny
    };

    // Skala visual per ras agar proporsional tubuh berbeda-beda
    private static readonly System.Collections.Generic.Dictionary<DogBreed, float> BreedScales =
        new System.Collections.Generic.Dictionary<DogBreed, float>
    {
        [DogBreed.Kintamani]       = 0.145f,  // ~50 cm tinggi punggung
        [DogBreed.GoldenRetriever] = 0.165f,  // ~58 cm
        [DogBreed.GermanShepherd]  = 0.175f,  // ~62 cm
        [DogBreed.Dachshund]       = 0.085f,  // ~23 cm (pendek, panjang)
        [DogBreed.FrenchBulldog]   = 0.100f,  // ~30 cm (gempal)
        [DogBreed.SingingDog]      = 0.135f,  // ~45 cm
    };

    // Kecepatan berjalan per ras
    private static readonly System.Collections.Generic.Dictionary<DogBreed, float> BreedSpeeds =
        new System.Collections.Generic.Dictionary<DogBreed, float>
    {
        [DogBreed.Kintamani]       = 2.0f,
        [DogBreed.GoldenRetriever] = 1.95f,
        [DogBreed.GermanShepherd]  = 2.3f,
        [DogBreed.Dachshund]       = 1.4f,
        [DogBreed.FrenchBulldog]   = 1.3f,
        [DogBreed.SingingDog]      = 2.1f,
    };

    private const string FbxFolder = "Assets/_Project/Art/Animals";
    private const string MatsFolder = "Assets/_Project/Art/Animals/Materials";
    private const string AnimsFolder = "Assets/_Project/Art/Animals/Animations";

    // ─────────────────────────────────────────────────────────────────────
    //  SPAWNER ANJING DESA — MENGGUNAKAN Dog.fbx BERANIMASIKAN PENUH
    //  Kaki benar-benar melangkah, 100% anti-melayang, tanpa sliding
    // ─────────────────────────────────────────────────────────────────────

    public static GameObject SpawnDogBreed(
        DogBreed breed, Vector3 position, Quaternion rotation, Transform parent,
        string customName = null, string petMsg = null, bool canFollow = true)
    {
        var sp = BreedSpecs[breed];
        string dogName = customName ?? sp.name;

        // ── 1. Root GameObject ──────────────────────────────────────────
        var dogRoot = new GameObject($"Animal_Dog_{dogName.Replace(" ", "_")}");
        dogRoot.transform.SetParent(parent);
        position.y = 0f;  // Tepatkan ke tanah
        dogRoot.transform.position = position;
        dogRoot.transform.rotation = rotation;

        // ── 2. Load Dog.fbx (rigged, beranimasikan) ─────────────────────
        string fbxPath = $"{FbxFolder}/FBX/Dog.fbx";
        var dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        GameObject visual;
        if (dogPrefab != null)
        {
            visual = UnityEngine.Object.Instantiate(dogPrefab, dogRoot.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            // Rotasi Y = 180 derajat agar kepala/moncong anjing menghadap lurus ke Z-forward (+Z maju Unity)
            visual.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            float scale = BreedScales.ContainsKey(breed) ? BreedScales[breed] : 0.145f;
            visual.transform.localScale = Vector3.one * scale;

            // Terapkan material warna ras ke semua renderer di dalam Visual
            ApplyBreedColor(visual, breed);

            // ── 3. Animator — sambungkan ke AC_Dog.controller + Avatar ──
            var anim = visual.GetComponent<Animator>();
            if (!anim) anim = visual.AddComponent<Animator>();

            // Muat Avatar dari Dog.fbx
            Avatar dogAvatar = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is Avatar av) { dogAvatar = av; break; }
            }
            if (dogAvatar != null) anim.avatar = dogAvatar;

            // Muat AC_Dog.controller
            var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                $"{AnimsFolder}/AC_Dog.controller");
            if (ac != null) anim.runtimeAnimatorController = ac;

            // WAJIB: matikan root motion agar posisi dikontrol AnimalController
            anim.applyRootMotion = false;
            // AlwaysAnimate agar animasi tetap jalan meski kamera menjauh (di luar frustum)
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Pastikan Animator mulai di state Idle
            anim.SetBool("isMoving", false);
        }
        else
        {
            // Fallback: gunakan mesh prosedural (tanpa animasi) jika FBX tidak ada
            Debug.LogWarning($"[DogSpawner] Dog.fbx tidak ditemukan di {fbxPath}. Menggunakan mesh statis.");
            visual = new GameObject("Visual");
            visual.transform.SetParent(dogRoot.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            var mf = visual.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrCreateDogMesh(breed);
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GetOrCreateDogMaterial(breed);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        // ── 4. Collider proporsional (horizontal Z sepanjang tubuh 4 kaki) ──
        var col = dogRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, sp.H * 0.5f, 0);
        col.radius = Mathf.Max(sp.mid_r, sp.leg_r * 2.5f);
        col.height = sp.L * 1.1f;
        col.direction = 2; // Sumbu Z (arah hadap)

        // ── 5. AI Controller: Double Ground Lock [0, 0.25m], kaki tidak nyeret ──
        float dogSpeed = BreedSpeeds.ContainsKey(breed) ? BreedSpeeds[breed] : 1.85f;
        var ctrl = dogRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Dog;
        ctrl.animalName = dogName;
        ctrl.wanderRadius = breed == DogBreed.Kintamani || breed == DogBreed.GermanShepherd ? 8.0f : 6.0f;
        ctrl.moveSpeed = dogSpeed;
        ctrl.acceleration = 4.2f;
        ctrl.turnSpeed = 220f;
        ctrl.turnSmoothness = 5.2f;
        ctrl.clipWalkSpeed = dogSpeed * 0.95f;
        ctrl.petMessage = petMsg ?? $"Guk! {dogName} mengibaskan ekornya riang dan kini menemanimu!";
        ctrl.canFollowPlayer = canFollow;

        return dogRoot;
    }

    /// <summary>
    /// Menerapkan warna khas ras ke seluruh SkinnedMeshRenderer di dalam Visual (Dog.fbx).
    /// </summary>
    private static void ApplyBreedColor(GameObject visual, DogBreed breed)
    {
        if (!BreedColors.ContainsKey(breed)) return;
        Color breedColor = BreedColors[breed];

        // Pastikan material warna ras sudah ada
        string matName = $"M_Dog_{breed}";
        string matPath = $"{MatsFolder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetColor("_BaseColor", breedColor);
        mat.SetFloat("_Smoothness", 0.22f);
        mat.SetFloat("_Metallic", 0.0f);
        EditorUtility.SetDirty(mat);

        // Terapkan ke semua renderer (SkinnedMeshRenderer maupun MeshRenderer)
        foreach (var rend in visual.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            rend.sharedMaterials = mats;
        }
    }
}
#endif

