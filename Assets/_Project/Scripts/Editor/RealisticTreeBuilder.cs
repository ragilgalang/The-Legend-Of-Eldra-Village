#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator Pohon Realistis 3D (SpeedTree-Style Procedural Generator) untuk Desa Eldara.
/// Menghasilkan pohon dengan:
/// - Batang kayu bertekstur kulit kayu PBR (bark) dengan akar kokoh di tanah
/// - Tajuk dedaunan lebat dari kartu daun (leaf cards) bersilang dengan alpha cutout
/// - Spherical Normals (rahasia pencahayaan halus bervolume tanpa sudut poligonal kasar)
/// - Animasi hembusan angin dinamis (TreeWindSway)
/// </summary>
public static class RealisticTreeBuilder
{
    public static GameObject BuildOakTree(Vector3 position, float scale, Transform parent, Material barkMat, Material leafMat)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.Oak);
    }

    public static GameObject BuildPineTree(Vector3 position, float scale, Transform parent, Material barkMat, Material leafMat)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.Pine);
    }

    public static GameObject BuildBirchTree(Vector3 position, float scale, Transform parent, Material barkMat, Material leafMat)
    {
        return BuildTreeInternal(position, scale, parent, barkMat, leafMat, TreeType.Birch);
    }

    public enum TreeType { Oak, Pine, Birch }

    private static GameObject BuildTreeInternal(Vector3 position, float scale, Transform parent, Material barkMat, Material leafMat, TreeType type)
    {
        string treeName = type == TreeType.Oak ? "Realistic_Oak_Tree"
                        : type == TreeType.Pine ? "Realistic_Pine_Tree"
                        : "Realistic_Birch_Tree";

        var treeRoot = new GameObject(treeName);
        treeRoot.transform.SetParent(parent);
        treeRoot.transform.position = position;
        treeRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        treeRoot.transform.localScale = Vector3.one * scale;

        // ── 1. BATANG POHON (TRUNK & BRANCHES) ──
        var trunkGo = new GameObject("Trunk");
        trunkGo.transform.SetParent(treeRoot.transform, false);

        var trunkFilter = trunkGo.AddComponent<MeshFilter>();
        var trunkRenderer = trunkGo.AddComponent<MeshRenderer>();
        trunkRenderer.sharedMaterial = barkMat;

        Mesh trunkMesh = GenerateTrunkMesh(type);
        trunkFilter.sharedMesh = trunkMesh;

        // Collider batang agar pemain tidak tembus
        var col = treeRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, (type == TreeType.Pine ? 5f : 3.5f), 0);
        col.radius = type == TreeType.Oak ? 0.65f : 0.45f;
        col.height = type == TreeType.Pine ? 10f : 7f;

        // ── 2. TAJUK DEDAUNAN REALISTIS (FOLIAGE CARDS WITH SPHERICAL NORMALS) ──
        var canopyGo = new GameObject("Canopy_Foliage");
        canopyGo.transform.SetParent(treeRoot.transform, false);

        var canopyFilter = canopyGo.AddComponent<MeshFilter>();
        var canopyRenderer = canopyGo.AddComponent<MeshRenderer>();
        canopyRenderer.sharedMaterial = leafMat;
        canopyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        canopyRenderer.receiveShadows = true;

        Mesh canopyMesh = GenerateCanopyMesh(type);
        canopyFilter.sharedMesh = canopyMesh;

        // ── 3. ANIMASI ANGIN DINAMIS ──
        var sway = treeRoot.AddComponent<TreeWindSway>();
        sway.swayIntensity = type == TreeType.Pine ? 1.8f : Random.Range(2.2f, 3.2f);
        sway.swaySpeed = Random.Range(0.95f, 1.25f);

        return treeRoot;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GENERASI BATANG & CABANG
    // ─────────────────────────────────────────────────────────────────────
    private static Mesh GenerateTrunkMesh(TreeType type)
    {
        var mesh = new Mesh();
        mesh.name = "Procedural_Trunk";

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        int radialSegments = 10;
        int heightSegments = type == TreeType.Pine ? 8 : 7;
        float totalHeight = type == TreeType.Pine ? 12f : (type == TreeType.Oak ? 8.5f : 7.0f);
        float baseRadius = type == TreeType.Oak ? 0.65f : (type == TreeType.Pine ? 0.48f : 0.38f);
        float topRadius = type == TreeType.Pine ? 0.08f : 0.28f;

        // Buat silinder batang bertingkat dengan akar mekar di bawah
        for (int y = 0; y <= heightSegments; y++)
        {
            float t = y / (float)heightSegments;
            float currentY = t * totalHeight;
            // Flare akar di bagian paling bawah
            float rootFlare = (y == 0) ? 1.45f : (y == 1 ? 1.15f : 1.0f);
            float currentRadius = Mathf.Lerp(baseRadius, topRadius, t) * rootFlare;

            for (int r = 0; r <= radialSegments; r++)
            {
                float angle = (r / (float)radialSegments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Tambahkan sedikit gelombang organik pada lingkar batang
                float bump = 1.0f + Mathf.Sin(angle * 3f + y) * 0.06f;

                Vector3 pos = new Vector3(cos * currentRadius * bump, currentY, sin * currentRadius * bump);
                Vector3 norm = new Vector3(cos, 0.1f, sin).normalized;

                vertices.Add(pos);
                normals.Add(norm);
                uvs.Add(new Vector2(r / (float)radialSegments * 2f, t * 4f));
            }
        }

        // Segitiga batang
        int stride = radialSegments + 1;
        for (int y = 0; y < heightSegments; y++)
        {
            for (int r = 0; r < radialSegments; r++)
            {
                int b0 = y * stride + r;
                int b1 = b0 + 1;
                int t0 = (y + 1) * stride + r;
                int t1 = t0 + 1;

                triangles.Add(b0); triangles.Add(t0); triangles.Add(b1);
                triangles.Add(b1); triangles.Add(t0); triangles.Add(t1);
            }
        }

        // Jika pohon Oak atau Birch, tambahkan 4 dahan cabang utama yang menopang tajuk
        if (type != TreeType.Pine)
        {
            int branchCount = type == TreeType.Oak ? 5 : 4;
            float branchStartHeight = totalHeight * 0.6f;

            for (int b = 0; b < branchCount; b++)
            {
                float bAngle = (b / (float)branchCount) * Mathf.PI * 2f + (b * 0.4f);
                Vector3 bDir = new Vector3(Mathf.Cos(bAngle), 0.65f, Mathf.Sin(bAngle)).normalized;
                Vector3 bStart = new Vector3(0, branchStartHeight + (b * 0.4f), 0);
                float bLen = Random.Range(2.2f, 3.4f);
                Vector3 bEnd = bStart + bDir * bLen;

                AddBranchCylinder(vertices, normals, uvs, triangles, bStart, bEnd, 0.22f, 0.08f);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        return mesh;
    }

    private static void AddBranchCylinder(
        List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
        Vector3 start, Vector3 end, float startR, float endR)
    {
        int segs = 6;
        int startIndex = verts.Count;
        Vector3 dir = (end - start).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.01f) side = Vector3.right;
        Vector3 up = Vector3.Cross(side, dir).normalized;

        for (int step = 0; step <= 2; step++)
        {
            float t = step / 2f;
            Vector3 center = Vector3.Lerp(start, end, t);
            float r = Mathf.Lerp(startR, endR, t);

            for (int i = 0; i <= segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 offset = (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                verts.Add(center + offset);
                norms.Add(offset.normalized);
                uvs.Add(new Vector2(i / (float)segs, t));
            }
        }

        int stride = segs + 1;
        for (int step = 0; step < 2; step++)
        {
            for (int i = 0; i < segs; i++)
            {
                int b0 = startIndex + step * stride + i;
                int b1 = b0 + 1;
                int t0 = startIndex + (step + 1) * stride + i;
                int t1 = t0 + 1;

                tris.Add(b0); tris.Add(t0); tris.Add(b1);
                tris.Add(b1); tris.Add(t0); tris.Add(t1);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GENERASI TAJUK DAUN (LEAF CARDS & SPHERICAL NORMALS)
    // ─────────────────────────────────────────────────────────────────────
    private static Mesh GenerateCanopyMesh(TreeType type)
    {
        var mesh = new Mesh();
        mesh.name = "Procedural_Canopy";

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        if (type == TreeType.Pine)
        {
            // Cemara/Pinus: 6 tingkat mahkota berbentuk kerucut
            int tiers = 6;
            for (int t = 0; t < tiers; t++)
            {
                float prog = t / (float)tiers;
                float y = Mathf.Lerp(3.5f, 11.5f, prog);
                float radius = Mathf.Lerp(3.2f, 0.7f, prog);
                int cardCount = 6 + (int)((1f - prog) * 6);

                Vector3 tierCenter = new Vector3(0, y, 0);

                for (int c = 0; c < cardCount; c++)
                {
                    float angle = (c / (float)cardCount) * Mathf.PI * 2f + (t * 0.45f);
                    Vector3 pos = tierCenter + new Vector3(Mathf.Cos(angle) * radius, Random.Range(-0.2f, 0.2f), Mathf.Sin(angle) * radius);
                    float cardSize = Mathf.Lerp(2.2f, 1.4f, prog);

                    AddCrossedLeafCard(vertices, normals, uvs, triangles, pos, tierCenter, cardSize, angle * Mathf.Rad2Deg);
                }
            }
        }
        else
        {
            // Oak / Birch: Tajuk bulat awan lebat (cloud dome) dengan 70-110 kartu daun
            int clusterCount = type == TreeType.Oak ? 85 : 55;
            float canopyCenterY = type == TreeType.Oak ? 6.5f : 5.5f;
            Vector3 canopyCenter = new Vector3(0, canopyCenterY, 0);
            float domeRadiusX = type == TreeType.Oak ? 3.8f : 2.5f;
            float domeRadiusY = type == TreeType.Oak ? 2.8f : 2.2f;

            for (int i = 0; i < clusterCount; i++)
            {
                // Distribusi titik dalam volume bola/kubah
                Vector3 randDir = Random.insideUnitSphere;
                // Buat bagian bawah agak datar dan bagian atas cembung
                randDir.y = Mathf.Abs(randDir.y) * 1.1f - 0.2f;

                Vector3 pos = canopyCenter + new Vector3(randDir.x * domeRadiusX, randDir.y * domeRadiusY, randDir.z * domeRadiusX);
                float cardSize = type == TreeType.Oak ? Random.Range(2.0f, 2.7f) : Random.Range(1.6f, 2.2f);
                float rotDeg = Random.Range(0f, 360f);

                AddCrossedLeafCard(vertices, normals, uvs, triangles, pos, canopyCenter, cardSize, rotDeg);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        return mesh;
    }

    /// <summary>
    /// Menambahkan 2 atau 3 kartu daun bersilang (cross-quads) pada satu titik kluster.
    /// Menggunakan SPHERICAL NORMALS yang menghadap ke luar dari pusat tajuk pohon
    /// agar pencahayaan matahari memancar lembut dan bervolume seperti daun asli.
    /// </summary>
    private static void AddCrossedLeafCard(
        List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
        Vector3 clusterCenter, Vector3 mainCanopyCenter, float size, float baseRotation)
    {
        // Tiap kluster terdiri dari 2 quad saling silang (salib X) + 1 quad miring
        float half = size * 0.5f;

        float[] angles = { baseRotation, baseRotation + 90f };

        foreach (float deg in angles)
        {
            int baseIdx = verts.Count;
            Quaternion rot = Quaternion.Euler(Random.Range(-15f, 15f), deg, Random.Range(-10f, 10f));

            Vector3 right = rot * Vector3.right * half;
            Vector3 up    = rot * Vector3.up * half;

            Vector3 v0 = clusterCenter - right - up;
            Vector3 v1 = clusterCenter + right - up;
            Vector3 v2 = clusterCenter + right + up;
            Vector3 v3 = clusterCenter - right + up;

            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            // RAHASIA REALISME: SPHERICAL NORMALS
            // Normal dihitung dari arah pusat tajuk pohon ke posisi vertex,
            // sehingga matahari menyinari daun seperti volume bola yang lembut!
            norms.Add((v0 - mainCanopyCenter).normalized);
            norms.Add((v1 - mainCanopyCenter).normalized);
            norms.Add((v2 - mainCanopyCenter).normalized);
            norms.Add((v3 - mainCanopyCenter).normalized);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            // Segitiga depan
            tris.Add(baseIdx + 0);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 1);

            tris.Add(baseIdx + 0);
            tris.Add(baseIdx + 3);
            tris.Add(baseIdx + 2);

            // Segitiga belakang (agar tampak dari kedua sisi)
            tris.Add(baseIdx + 0);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);

            tris.Add(baseIdx + 0);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 3);
        }
    }
}
#endif
