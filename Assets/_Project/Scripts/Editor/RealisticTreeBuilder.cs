#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator Pohon Realistis 3D (Algoritma Porting Langsung dari Script Blender).
/// Menghasilkan struktur pohon biologis alami dengan:
/// - Pertumbuhan rekursif fraktal (Level 0: Batang, Level 1: Dahan utama, Level 2: Cabang, Level 3: Ranting)
/// - Penampang cabang melingkar halus dengan Parallel Transport Frame (7 sisi, taper halus, flare akar di dasar)
/// - Dedaunan 3D anatomis botani berurat: 6 baris x 3 titik (bilah kiri, tulang daun utama / rachis, bilah kanan)
/// - Lipatan tulang daun (center fold), lengkungan gravitasi (sag droop), dan profil kontur sinus alami
/// - Pewarnaan vertex color multi-tone: Tulang daun lebih terang berpendar, gradasi daun teduh-terang
/// - Palet musim autentik: SUMMER (hijau zamrud lebat), SPRING (hijau segar muda), AUTUMN (keemasan hangat)
/// - Mesh caching berkinerja tinggi untuk GPU Instancing & FPS maksimal di Unity
/// </summary>
public static class RealisticTreeBuilder
{
    public enum TreeSeason { Summer, Autumn, Spring, Winter }
    public enum TreeType { Oak, Pine, Birch, AutumnOak }

    private static readonly Dictionary<string, (Mesh trunk, Mesh foliage)> MeshCache = 
        new Dictionary<string, (Mesh, Mesh)>();

    public static void ClearCache()
    {
        MeshCache.Clear();
    }

    public static GameObject BuildOakTree(Vector3 position, float scale, Transform parent, Material barkMat = null, Material leafMat = null)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.Oak);
    }

    public static GameObject BuildBirchTree(Vector3 position, float scale, Transform parent, Material barkMat = null, Material leafMat = null)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.Birch);
    }

    public static GameObject BuildPineTree(Vector3 position, float scale, Transform parent, Material barkMat = null, Material leafMat = null)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.Pine);
    }

    public static GameObject BuildAutumnOakTree(Vector3 position, float scale, Transform parent, Material barkMat = null, Material leafMat = null)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.AutumnOak);
    }

    private const string TreeMeshFolder = "Assets/_Project/Art/ProceduralTrees";

    private static (Mesh trunk, Mesh foliage) GetOrCreateTreeMeshes(TreeType type, int variantIndex)
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/ProceduralTrees"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
                AssetDatabase.CreateFolder("Assets", "_Project/Art");
            AssetDatabase.CreateFolder("Assets/_Project/Art", "ProceduralTrees");
        }

        string trunkAssetPath = $"{TreeMeshFolder}/Mesh_Trunk_{type}_v{variantIndex}.asset";
        string foliageAssetPath = $"{TreeMeshFolder}/Mesh_Foliage_{type}_v{variantIndex}.asset";

        var trunkMesh = AssetDatabase.LoadAssetAtPath<Mesh>(trunkAssetPath);
        var foliageMesh = AssetDatabase.LoadAssetAtPath<Mesh>(foliageAssetPath);

        if (trunkMesh && foliageMesh)
        {
            return (trunkMesh, foliageMesh);
        }

        int seed = (variantIndex * 17) + (int)type * 31;
        (trunkMesh, foliageMesh) = GenerateTreeMeshes(type, seed);

        if (AssetDatabase.LoadAssetAtPath<Mesh>(trunkAssetPath) == null)
            AssetDatabase.CreateAsset(trunkMesh, trunkAssetPath);
        else
            EditorUtility.CopySerialized(trunkMesh, AssetDatabase.LoadAssetAtPath<Mesh>(trunkAssetPath));

        if (AssetDatabase.LoadAssetAtPath<Mesh>(foliageAssetPath) == null)
            AssetDatabase.CreateAsset(foliageMesh, foliageAssetPath);
        else
            EditorUtility.CopySerialized(foliageMesh, AssetDatabase.LoadAssetAtPath<Mesh>(foliageAssetPath));

        AssetDatabase.SaveAssets();
        return (trunkMesh, foliageMesh);
    }

    private static GameObject BuildTreeInternal(
        Vector3 position, float scale, Transform parent, 
        Material barkMat, Material leafMat, TreeType type)
    {
        string treeName = type == TreeType.Oak ? "Realistic_Oak_Tree"
                        : type == TreeType.Birch ? "Realistic_Birch_Tree"
                        : type == TreeType.Pine ? "Realistic_Pine_Tree"
                        : "Realistic_Autumn_Oak";

        // Material fallback jika null
        if (!barkMat)
        {
            barkMat = AssetMaterialSetup.SetupProceduralBarkMaterial(birch: type == TreeType.Birch);
        }
        if (!leafMat)
        {
            string seasonStr = type == TreeType.AutumnOak ? "AUTUMN" 
                             : type == TreeType.Birch ? "SPRING" 
                             : "SUMMER";
            leafMat = AssetMaterialSetup.SetupProceduralFoliageMaterial(seasonStr);
        }

        // Variasi seed acak (1 sampai 3) agar pohon bervariasi secara alami
        int variantIndex = Random.Range(1, 4);
        var (trunkMesh, foliageMesh) = GetOrCreateTreeMeshes(type, variantIndex);

        var treeRoot = new GameObject(treeName);
        treeRoot.transform.SetParent(parent);
        treeRoot.transform.position = position;
        treeRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        treeRoot.transform.localScale = Vector3.one * scale;

        // 1. Batang & Dahan (Trunk)
        var trunkGo = new GameObject("Trunk");
        trunkGo.transform.SetParent(treeRoot.transform, false);
        var trunkFilter = trunkGo.AddComponent<MeshFilter>();
        var trunkRenderer = trunkGo.AddComponent<MeshRenderer>();
        trunkRenderer.sharedMaterial = barkMat;
        trunkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        trunkRenderer.receiveShadows = true;
        trunkFilter.sharedMesh = trunkMesh;

        // 2. Dedaunan 3D Anatomis (Canopy Foliage)
        var canopyGo = new GameObject("Canopy_Foliage");
        canopyGo.transform.SetParent(treeRoot.transform, false);
        var canopyFilter = canopyGo.AddComponent<MeshFilter>();
        var canopyRenderer = canopyGo.AddComponent<MeshRenderer>();
        canopyRenderer.sharedMaterial = leafMat;
        canopyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        canopyRenderer.receiveShadows = true;
        canopyFilter.sharedMesh = foliageMesh;

        // 3. Collider Batang
        float height = type == TreeType.Pine ? 11f : (type == TreeType.Birch ? 7.5f : 8.5f);
        float radius = type == TreeType.Pine ? 0.45f : (type == TreeType.Birch ? 0.35f : 0.65f);
        var col = treeRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, height * 0.45f, 0);
        col.radius = radius;
        col.height = height;

        // 4. Animasi Angin & Napas Canopy
        var sway = treeRoot.AddComponent<TreeWindSway>();
        sway.swayIntensity = type == TreeType.Pine ? 1.5f : Random.Range(2.0f, 2.7f);
        sway.swaySpeed = Random.Range(0.95f, 1.25f);

        return treeRoot;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GENERATOR MESH PROSEDURAL BERDASARKAN LOGIKA SCRIPT BLENDER
    // ─────────────────────────────────────────────────────────────────────

    private static readonly Color[] PaletteSummer = new Color[]
    {
        new Color(0.03f, 0.16f, 0.02f),
        new Color(0.05f, 0.22f, 0.03f),
        new Color(0.08f, 0.28f, 0.04f),
        new Color(0.04f, 0.18f, 0.05f)
    };

    private static readonly Color[] PaletteSpring = new Color[]
    {
        new Color(0.12f, 0.40f, 0.04f),
        new Color(0.20f, 0.50f, 0.06f),
        new Color(0.30f, 0.55f, 0.08f)
    };

    private static readonly Color[] PaletteAutumn = new Color[]
    {
        new Color(0.55f, 0.25f, 0.02f),
        new Color(0.70f, 0.40f, 0.03f),
        new Color(0.60f, 0.10f, 0.02f),
        new Color(0.45f, 0.38f, 0.04f),
        new Color(0.20f, 0.25f, 0.03f)
    };

    private static readonly Color[] PalettePine = new Color[]
    {
        new Color(0.02f, 0.12f, 0.03f),
        new Color(0.03f, 0.15f, 0.04f),
        new Color(0.05f, 0.18f, 0.05f)
    };

    private static (Mesh trunk, Mesh foliage) GenerateTreeMeshes(TreeType type, int seed)
    {
        var prevRandomState = Random.state;
        Random.InitState(seed);

        // Buffer untuk geometri batang
        var barkVerts = new List<Vector3>();
        var barkNorms = new List<Vector3>();
        var barkUvs   = new List<Vector2>();
        var barkTris  = new List<int>();

        // Buffer untuk dedaunan anatomis berurat 3D
        var leafVerts = new List<Vector3>();
        var leafNorms = new List<Vector3>();
        var leafCols  = new List<Color>();
        var leafUvs   = new List<Vector2>();
        var leafTris  = new List<int>();

        Color[] palette = (type == TreeType.AutumnOak) ? PaletteAutumn
                        : (type == TreeType.Birch) ? PaletteSpring
                        : (type == TreeType.Pine) ? PalettePine
                        : PaletteSummer;

        float trunkLength = type == TreeType.Pine ? 4.8f : (type == TreeType.Birch ? 3.6f : 3.4f);
        float trunkRadius = type == TreeType.Pine ? 0.24f : (type == TreeType.Birch ? 0.20f : 0.28f);
        float leafSize    = type == TreeType.Pine ? 0.22f : 0.32f;
        float leafDensity = type == TreeType.Pine ? 0.85f : 1.0f;

        Vector3 startPos = Vector3.zero;
        Vector3 initialDir = new Vector3(Random.Range(-0.04f, 0.04f), 1.0f, Random.Range(-0.04f, 0.04f)).normalized;

        Vector3 crownCenter = new Vector3(0, trunkLength * 1.6f, 0);

        // Rekursi pertumbuhan pohon sesuai algoritma Blender
        GrowBranch(
            startPos, initialDir, trunkLength, trunkRadius, 0,
            barkVerts, barkNorms, barkUvs, barkTris,
            leafVerts, leafNorms, leafCols, leafUvs, leafTris,
            palette, leafSize, leafDensity, crownCenter, type
        );

        // Buat Mesh Batang
        var trunkMesh = new Mesh();
        trunkMesh.name = $"Procedural_Bark_{type}_{seed}";
        trunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        trunkMesh.SetVertices(barkVerts);
        trunkMesh.SetNormals(barkNorms);
        trunkMesh.SetUVs(0, barkUvs);
        trunkMesh.SetTriangles(barkTris, 0);
        trunkMesh.RecalculateBounds();

        // Buat Mesh Dedaunan
        var foliageMesh = new Mesh();
        foliageMesh.name = $"Procedural_Foliage_{type}_{seed}";
        foliageMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        foliageMesh.SetVertices(leafVerts);
        foliageMesh.SetNormals(leafNorms);
        foliageMesh.SetColors(leafCols);
        foliageMesh.SetUVs(0, leafUvs);
        foliageMesh.SetTriangles(leafTris, 0);
        foliageMesh.RecalculateBounds();

        Random.state = prevRandomState;

        return (trunkMesh, foliageMesh);
    }

    private static Vector3 RandVec()
    {
        return new Vector3(
            Random.Range(-1.0f, 1.0f),
            Random.Range(-1.0f, 1.0f),
            Random.Range(-1.0f, 1.0f)
        );
    }

    // ─────────────────────────────────────────────────────────────────────
    //  REKURSI CABANG & RANTING (PORTING FUNGSI grow() BLENDER)
    // ─────────────────────────────────────────────────────────────────────

    private static readonly int[] ChildCount = new int[] { 7, 5, 5 };
    private static readonly float[] ChildLength = new float[] { 0f, 2.7f, 1.7f, 0.9f };
    private static readonly Vector2[] ChildAngle = new Vector2[]
    {
        Vector2.zero,
        new Vector2(50f, 72f), // Level 1
        new Vector2(35f, 60f), // Level 2
        new Vector2(30f, 55f)  // Level 3
    };
    private static readonly Vector2[] ChildStart = new Vector2[]
    {
        new Vector2(0.45f, 0.95f), // Level 0
        new Vector2(0.25f, 0.95f), // Level 1
        new Vector2(0.20f, 1.00f)  // Level 2
    };

    private const int LEAF_PER_POINT = 6;
    private const int MAX_LEVEL = 3;

    private static void GrowBranch(
        Vector3 start, Vector3 direction, float length, float radius, int level,
        List<Vector3> barkVerts, List<Vector3> barkNorms, List<Vector2> barkUvs, List<int> barkTris,
        List<Vector3> leafVerts, List<Vector3> leafNorms, List<Color> leafCols, List<Vector2> leafUvs, List<int> leafTris,
        Color[] palette, float leafSize, float leafDensity, Vector3 crownCenter, TreeType treeType)
    {
        int segs = Mathf.Max(5, (int)(length * 5f));
        float step = length / segs;

        List<Vector3> pts = new List<Vector3>(segs + 1);
        List<float> radii = new List<float>(segs + 1);

        Vector3 p = start;
        Vector3 d = direction.normalized;

        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            float taper = 1.0f - 0.85f * t;
            float r = Mathf.Max(radius * taper, 0.006f);

            // Pangkal batang pohon melebar (Buttress root flare)
            if (level == 0)
            {
                r *= 1.0f + 0.7f * Mathf.Max(0.0f, 1.0f - i / 3.0f);
            }

            pts.Add(p);
            radii.Add(r);

            float wobble = (level == 0) ? 0.10f : 0.22f;
            float up = (level < 2) ? 0.03f : 0.07f;

            if (treeType == TreeType.Pine && level > 0)
            {
                // Cabang pinus condong sedikit mendatar & melengkung ke bawah
                up = -0.04f;
            }

            d = (d + RandVec() * wobble + Vector3.up * up).normalized;
            p += d * step;
        }

        // Tambahkan tabung cabang ke mesh kulit kayu
        AddTube(barkVerts, barkNorms, barkUvs, barkTris, pts, radii, sides: 7);

        // Tambahkan dedaunan anatomis pada ranting tingkat 2 ke atas
        if (palette != null && palette.Length > 0 && level >= 2)
        {
            int first = (level == 3) ? 1 : 3;
            int perPoint = Mathf.Max(1, (int)(LEAF_PER_POINT * leafDensity * (level == 3 ? 1.0f : 0.6f)));

            for (int i = first; i < pts.Count; i++)
            {
                Vector3 localDir = (pts[i] - pts[i - 1]).normalized;
                for (int lp = 0; lp < perPoint; lp++)
                {
                    Vector3 ld = localDir * 0.5f + RandVec() * 0.9f;
                    ld.y = ld.y * 0.6f - 0.1f;

                    float size = leafSize * Random.Range(0.75f, 1.2f);
                    Vector3 offset = RandVec() * 0.03f;
                    Color baseCol = palette[Random.Range(0, palette.Length)];

                    AddLeaf(
                        leafVerts, leafNorms, leafCols, leafUvs, leafTris,
                        pts[i] + offset, ld, size, baseCol, crownCenter
                    );
                }
            }

            // Kuncup dedaunan rimbun di ujung ranting
            if (level == 3)
            {
                Vector3 localDir = (pts[pts.Count - 1] - pts[pts.Count - 2]).normalized;
                int terminalCount = Mathf.Max(1, (int)(3 * leafDensity));
                for (int lp = 0; lp < terminalCount; lp++)
                {
                    Vector3 ld = localDir * 0.8f + RandVec() * 0.5f;
                    float size = leafSize * Random.Range(0.9f, 1.25f);
                    Color baseCol = palette[Random.Range(0, palette.Length)];

                    AddLeaf(
                        leafVerts, leafNorms, leafCols, leafUvs, leafTris,
                        pts[pts.Count - 1], ld, size, baseCol, crownCenter
                    );
                }
            }
        }

        // Tumbuhkan anak cabang (child branches)
        if (level < MAX_LEVEL)
        {
            int count = ChildCount[level];
            float lo = ChildStart[level].x;
            float hi = ChildStart[level].y;

            for (int k = 0; k < count; k++)
            {
                float f = lo + (hi - lo) * (k + Random.value * 0.8f) / count;
                int idx = Mathf.Min((int)(f * segs), segs - 1);
                Vector3 bBase = pts[idx];
                Vector3 tdir = (pts[idx + 1] - pts[idx]).normalized;

                Vector3 perp = Vector3.Cross(tdir, RandVec()).normalized;
                if (perp.sqrMagnitude < 0.01f) perp = Vector3.Cross(tdir, Vector3.up).normalized;
                perp = Quaternion.AngleAxis(Random.Range(0f, 360f), tdir) * perp;

                float angleMin = ChildAngle[level + 1].x;
                float angleMax = ChildAngle[level + 1].y;

                if (treeType == TreeType.Pine && level == 0)
                {
                    // Sudut cabang pinus lebih mendatar (65 - 82 derajat)
                    angleMin = 65f;
                    angleMax = 82f;
                }

                float aRad = Random.Range(angleMin, angleMax) * Mathf.Deg2Rad;
                Vector3 cd = (tdir * Mathf.Cos(aRad) + perp * Mathf.Sin(aRad)).normalized;
                float clen = ChildLength[level + 1] * Random.Range(0.75f, 1.15f) * (1.15f - 0.4f * f);
                float crad = radii[idx] * 0.55f;

                GrowBranch(
                    bBase, cd, clen, crad, level + 1,
                    barkVerts, barkNorms, barkUvs, barkTris,
                    leafVerts, leafNorms, leafCols, leafUvs, leafTris,
                    palette, leafSize, leafDensity, crownCenter, treeType
                );
            }
        }

        // Cabang pucuk meneruskan arah (Apical continuation shoot)
        if ((level == 1 || level == 2) && length > 1.0f)
        {
            GrowBranch(
                pts[pts.Count - 1], d, length * 0.35f, radii[radii.Count - 1] * 0.9f,
                level < 2 ? level + 1 : 3,
                barkVerts, barkNorms, barkUvs, barkTris,
                leafVerts, leafNorms, leafCols, leafUvs, leafTris,
                palette, leafSize, leafDensity, crownCenter, treeType
            );
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GEOMETRI TABUNG CABANG (PORTING add_tube() DENGAN PARALLEL TRANSPORT)
    // ─────────────────────────────────────────────────────────────────────

    private static void AddTube(
        List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
        List<Vector3> pts, List<float> radii, int sides = 7)
    {
        int n = pts.Count;
        if (n < 2) return;

        Vector3[] tangents = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 a = pts[Mathf.Max(i - 1, 0)];
            Vector3 b = pts[Mathf.Min(i + 1, n - 1)];
            Vector3 delta = b - a;
            tangents[i] = delta.sqrMagnitude > 1e-6f ? delta.normalized : Vector3.up;
        }

        Vector3 t0 = tangents[0];
        Vector3 refVec = Mathf.Abs(t0.x) < 0.9f ? Vector3.right : Vector3.forward;
        Vector3 normal = Vector3.Cross(t0, refVec).normalized;

        List<int[]> rings = new List<int[]>(n);
        float vAccum = 0f;

        for (int i = 0; i < n; i++)
        {
            Vector3 t = tangents[i];
            normal = normal - t * Vector3.Dot(normal, t);
            if (normal.sqrMagnitude < 1e-6f)
            {
                normal = Vector3.Cross(t, Vector3.up);
                if (normal.sqrMagnitude < 1e-6f) normal = Vector3.Cross(t, Vector3.right);
            }
            normal.Normalize();
            Vector3 binorm = Vector3.Cross(t, normal).normalized;

            if (i > 0)
            {
                vAccum += Vector3.Distance(pts[i], pts[i - 1]) * 0.9f;
            }

            int[] ring = new int[sides + 1];
            float rad = radii[i];

            for (int s = 0; s <= sides; s++)
            {
                float frac = (float)s / sides;
                float angle = 2f * Mathf.PI * frac;
                Vector3 radialDir = normal * Mathf.Cos(angle) + binorm * Mathf.Sin(angle);
                Vector3 pos = pts[i] + radialDir * rad;

                ring[s] = verts.Count;
                verts.Add(pos);
                norms.Add(radialDir.normalized);
                uvs.Add(new Vector2(frac, vAccum));
            }
            rings.Add(ring);
        }

        for (int i = 0; i < n - 1; i++)
        {
            for (int s = 0; s < sides; s++)
            {
                int s2 = s + 1;
                int r0_s = rings[i][s];
                int r0_s2 = rings[i][s2];
                int r1_s = rings[i + 1][s];
                int r1_s2 = rings[i + 1][s2];

                tris.Add(r0_s);
                tris.Add(r1_s);
                tris.Add(r0_s2);

                tris.Add(r0_s2);
                tris.Add(r1_s);
                tris.Add(r1_s2);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DEDAUNAN ANATOMIS BOTANI 3D BERURAT (PORTING add_leaf() BLENDER)
    // ─────────────────────────────────────────────────────────────────────

    private const int LEAF_ROWS = 6;

    private static void AddLeaf(
        List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<Vector2> uvs, List<int> tris,
        Vector3 pos, Vector3 direction, float size, Color baseColor, Vector3 crownCenter, float droop = 0.35f)
    {
        Vector3 y = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
        Vector3 x = Vector3.Cross(y, Vector3.up);
        if (x.sqrMagnitude < 1e-4f) x = Vector3.right;
        x.Normalize();

        // Putar sedikit di sekitar sumbu daun supaya tidak kaku/seragam
        float rotAngle = Random.Range(-35f, 35f);
        Quaternion rot = Quaternion.AngleAxis(rotAngle, y);
        x = rot * x;

        Vector3 z = Vector3.Cross(x, y).normalized;
        float width = size * Random.Range(0.42f, 0.55f);

        int[][] rows = new int[LEAF_ROWS][];

        for (int i = 0; i < LEAF_ROWS; i++)
        {
            float t = i / (float)(LEAF_ROWS - 1);
            float sinVal = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f));
            float w = (sinVal > 0f) ? width * 0.5f * Mathf.Pow(sinVal, 0.9f) : 0f;
            float fold = w * 0.28f;                     // Lipatan alami di sepanjang tulang daun
            float sag = -droop * size * t * t;          // Ujung daun melengkung turun terpengaruh gravitasi

            Vector3 center = pos + y * (size * t) + z * sag;
            Vector3 leftPos  = center - x * w + z * fold;
            Vector3 midPos   = center;
            Vector3 rightPos = center + x * w + z * fold;

            float shade = Random.Range(0.9f, 1.1f);
            Color bladeCol = new Color(
                Mathf.Min(1.0f, baseColor.r * shade),
                Mathf.Min(1.0f, baseColor.g * shade),
                Mathf.Min(1.0f, baseColor.b * shade),
                1.0f
            );
            Color veinCol = new Color(
                Mathf.Min(1.0f, bladeCol.r * 1.35f + 0.02f),
                Mathf.Min(1.0f, bladeCol.g * 1.35f + 0.02f),
                Mathf.Min(1.0f, bladeCol.b * 1.35f + 0.02f),
                1.0f
            );

            // Normal lembut gabungan permukaan daun & radial tajuk
            Vector3 softNorm = (z * 0.7f + (midPos - crownCenter).normalized * 0.3f).normalized;

            int idxL = verts.Count;
            verts.Add(leftPos);
            norms.Add(softNorm);
            cols.Add(bladeCol);
            uvs.Add(new Vector2(0f, t));

            int idxM = verts.Count;
            verts.Add(midPos);
            norms.Add(softNorm);
            cols.Add(veinCol);
            uvs.Add(new Vector2(0.5f, t));

            int idxR = verts.Count;
            verts.Add(rightPos);
            norms.Add(softNorm);
            cols.Add(bladeCol);
            uvs.Add(new Vector2(1f, t));

            rows[i] = new int[] { idxL, idxM, idxR };
        }

        for (int i = 0; i < LEAF_ROWS - 1; i++)
        {
            int[] a = rows[i];
            int[] b = rows[i + 1];

            // Quad Kiri: (a[0], a[1], b[1], b[0])
            // Sisi Depan
            tris.Add(a[0]); tris.Add(b[1]); tris.Add(a[1]);
            tris.Add(a[0]); tris.Add(b[0]); tris.Add(b[1]);
            // Sisi Belakang (Double-Sided)
            tris.Add(a[0]); tris.Add(a[1]); tris.Add(b[1]);
            tris.Add(a[0]); tris.Add(b[1]); tris.Add(b[0]);

            // Quad Kanan: (a[1], a[2], b[2], b[1])
            // Sisi Depan
            tris.Add(a[1]); tris.Add(b[2]); tris.Add(a[2]);
            tris.Add(a[1]); tris.Add(b[1]); tris.Add(b[2]);
            // Sisi Belakang (Double-Sided)
            tris.Add(a[1]); tris.Add(a[2]); tris.Add(b[2]);
            tris.Add(a[1]); tris.Add(b[2]); tris.Add(b[1]);
        }
    }
}
#endif
