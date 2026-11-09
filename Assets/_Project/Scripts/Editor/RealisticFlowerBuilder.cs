#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator Bunga 3D Timbul Realistis (Volumetric 3D Wildflowers) untuk Desa Eldara.
/// Menghasilkan tanaman bunga 3D fisik dengan batang, daun, kelopak mekar berlapis,
/// dan goyangan angin dinamis:
/// - 5 Varietas warna: Poppy Merah, Dandelion Emas, Bluebell Biru, Lavender Ungu, Aster Putih.
/// - Berdiri tegak di atas tanah (tinggi 0.55m - 0.85m) sehingga benar-benar "timbul" dari rumput.
/// - Dilengkapi animasi goyangan angin dinamis (GrassWindSway).
/// - Material PBR dengan penerimaan bayangan lembut (soft shadows).
/// </summary>
public static class RealisticFlowerBuilder
{
    public enum FlowerColor
    {
        RedPoppy,        // Bunga Poppy Merah Cerah
        GoldenDandelion, // Bunga Dandelion / Marigold Kuning Emas
        SkyBluebell,     // Bunga Bluebell Biru Langit
        MysticPurple,    // Bunga Lavender / Violet Ungu
        WhiteDaisy       // Bunga Aster Chamomile Putih
    }

    private static Mesh sharedFlowerMesh;
    private static Material stemMaterial;
    private static Dictionary<FlowerColor, Material> blossomMaterials = new Dictionary<FlowerColor, Material>();

    private static Material GetStemMaterial()
    {
        if (stemMaterial) return stemMaterial;

        string matPath = "Assets/_Project/Art/MedievalVillage/Materials/M_Flower_Stem.mat";
        stemMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!stemMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            stemMaterial = new Material(shader);
            stemMaterial.name = "M_Flower_Stem";
            stemMaterial.color = new Color(0.22f, 0.58f, 0.16f, 1f); // Hijau tangkai segar
            stemMaterial.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(stemMaterial, matPath);
        }
        return stemMaterial;
    }

    private static Material GetBlossomMaterial(FlowerColor color)
    {
        if (blossomMaterials.TryGetValue(color, out var existingMat) && existingMat != null)
            return existingMat;

        string matName = $"M_Flower_{color}";
        string matPath = $"Assets/_Project/Art/MedievalVillage/Materials/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        Color blossomCol;
        switch (color)
        {
            case FlowerColor.RedPoppy:
                blossomCol = new Color(0.92f, 0.14f, 0.18f, 1f); // Merah ruby menyala
                break;
            case FlowerColor.GoldenDandelion:
                blossomCol = new Color(1.0f, 0.82f, 0.10f, 1f); // Kuning emas dandelion
                break;
            case FlowerColor.SkyBluebell:
                blossomCol = new Color(0.20f, 0.56f, 0.98f, 1f); // Biru royal cerah
                break;
            case FlowerColor.MysticPurple:
                blossomCol = new Color(0.70f, 0.35f, 0.88f, 1f); // Ungu lavender anggun
                break;
            case FlowerColor.WhiteDaisy:
            default:
                blossomCol = new Color(0.98f, 0.98f, 0.95f, 1f); // Putih bersih chamomile
                break;
        }

        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = matName;
            mat.color = blossomCol;
            mat.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.color = blossomCol;
        }

        blossomMaterials[color] = mat;
        return mat;
    }

    /// <summary>
    /// Membuat satu tanaman bunga 3D timbul lengkap dengan tangkai, daun, dan mahkota mekar
    /// </summary>
    public static GameObject BuildFlower(Vector3 position, FlowerColor color, float scale, Transform parent)
    {
        if (RealisticGrassBuilder.IsPositionOnStoneOrRoad(position)) return null;

        if (!sharedFlowerMesh)
        {
            sharedFlowerMesh = GenerateFlowerMesh();
        }

        var go = new GameObject($"Flower_{color}");
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        go.transform.localScale = Vector3.one * scale;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = sharedFlowerMesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new Material[] { GetStemMaterial(), GetBlossomMaterial(color) };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        var sway = go.AddComponent<GrassWindSway>();
        sway.swayIntensity = Random.Range(3.0f, 4.5f);
        sway.swaySpeed = Random.Range(1.4f, 2.0f);

        return go;
    }

    /// <summary>
    /// Menanam serumpun bunga liar 3D (3 s/d 8 tangkai) mekar bersama
    /// </summary>
    public static void BuildFlowerClump(Vector3 center, int count, float radius, Transform parent, FlowerColor? preferredColor = null)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 pos = center + new Vector3(circle.x, 0, circle.y);
            if (RealisticGrassBuilder.IsPositionOnStoneOrRoad(pos)) continue;

            FlowerColor color = preferredColor ?? (FlowerColor)Random.Range(0, 5);
            float scale = Random.Range(0.85f, 1.25f);
            BuildFlower(pos, color, scale, parent);
        }
    }

    /// <summary>
    /// Menanam garis batas taman bunga (flower border / bed) di tepi jalan atau pekarangan
    /// </summary>
    public static void BuildFlowerBorder(Vector3 start, Vector3 end, int count, float width, Transform parent, FlowerColor? preferredColor = null)
    {
        Vector3 dir = end - start;
        Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (float)Mathf.Max(1, count - 1);
            float offset = Random.Range(-width * 0.5f, width * 0.5f);
            Vector3 pos = Vector3.Lerp(start, end, t) + right * offset;

            if (RealisticGrassBuilder.IsPositionOnStoneOrRoad(pos)) continue;

            FlowerColor color = preferredColor ?? (FlowerColor)Random.Range(0, 5);
            float scale = Random.Range(0.85f, 1.25f);
            BuildFlower(pos, color, scale, parent);
        }
    }

    /// <summary>
    /// Menghasilkan Mesh 3D Prosedural: Submesh 0 (Tangkai + Daun) dan Submesh 1 (Kelopak Bunga Mekar)
    /// </summary>
    private static Mesh GenerateFlowerMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Shared_Realistic_Flower_Mesh";
        mesh.subMeshCount = 2;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();

        var trisStem = new List<int>();
        var trisBlossom = new List<int>();

        float stemHeight = 0.65f;
        float stemWidth = 0.05f;
        float stemHalf = stemWidth * 0.5f;

        // ═════════════════════════════════════════════════════════════════
        // SUBMESH 0: TANGKAI HIJAU & DAUN-DAUN SAMPING (Stem & Leaves)
        // ═════════════════════════════════════════════════════════════════
        // Tangkai: 2 quad bersilangan 90 derajat vertikal
        float[] stemAngles = { 0f, 90f };
        foreach (float deg in stemAngles)
        {
            int baseIdx = verts.Count;
            Quaternion rot = Quaternion.Euler(0, deg, 0);
            Vector3 r = rot * Vector3.right * stemHalf;

            // Sedikit melengkung lembut ke arah depan agar organik
            Vector3 b0 = -r;
            Vector3 b1 =  r;
            Vector3 t0 = -r + new Vector3(0.04f, stemHeight, 0.04f);
            Vector3 t1 =  r + new Vector3(0.04f, stemHeight, 0.04f);

            verts.Add(b0); verts.Add(b1); verts.Add(t1); verts.Add(t0);
            norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));

            // Two-sided
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 2); trisStem.Add(baseIdx + 1);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 3); trisStem.Add(baseIdx + 2);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 1); trisStem.Add(baseIdx + 2);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 2); trisStem.Add(baseIdx + 3);
        }

        // Daun 1 (Sisi Kiri Bawah, Y = 0.22m)
        {
            int baseIdx = verts.Count;
            Vector3 root = new Vector3(0f, 0.22f, 0f);
            Vector3 tip = new Vector3(-0.16f, 0.26f, 0.08f);
            Vector3 side = new Vector3(0.02f, 0.04f, 0.07f);

            verts.Add(root);
            verts.Add(root + side);
            verts.Add(tip);
            verts.Add(root - side);

            Vector3 leafNorm = Vector3.Cross(tip - root, side).normalized;
            norms.Add(leafNorm); norms.Add(leafNorm); norms.Add(leafNorm); norms.Add(leafNorm);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0.5f, 1)); uvs.Add(new Vector2(0, 1));

            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 2); trisStem.Add(baseIdx + 1);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 3); trisStem.Add(baseIdx + 2);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 1); trisStem.Add(baseIdx + 2);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 2); trisStem.Add(baseIdx + 3);
        }

        // Daun 2 (Sisi Kanan Atas, Y = 0.38m)
        {
            int baseIdx = verts.Count;
            Vector3 root = new Vector3(0f, 0.38f, 0f);
            Vector3 tip = new Vector3(0.18f, 0.42f, -0.06f);
            Vector3 side = new Vector3(-0.02f, 0.04f, 0.07f);

            verts.Add(root);
            verts.Add(root + side);
            verts.Add(tip);
            verts.Add(root - side);

            Vector3 leafNorm = Vector3.Cross(tip - root, side).normalized;
            norms.Add(leafNorm); norms.Add(leafNorm); norms.Add(leafNorm); norms.Add(leafNorm);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0.5f, 1)); uvs.Add(new Vector2(0, 1));

            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 2); trisStem.Add(baseIdx + 1);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 3); trisStem.Add(baseIdx + 2);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 1); trisStem.Add(baseIdx + 2);
            trisStem.Add(baseIdx + 0); trisStem.Add(baseIdx + 2); trisStem.Add(baseIdx + 3);
        }

        // ═════════════════════════════════════════════════════════════════
        // SUBMESH 1: MAHKOTA KELOPAK BUNGA MEKAR (Flower Blossom Star)
        // ═════════════════════════════════════════════════════════════════
        Vector3 blossomCenter = new Vector3(0.04f, stemHeight, 0.04f);
        float petalRadius = 0.22f; // Diameter bunga ~44cm (sangat jelas timbul & terlihat indah)
        float petalCupHeight = 0.12f;

        // 4 quad kelopak bersilangan (0, 45, 90, 135 derajat) membentuk bunga mekar 8 arah
        float[] petalAngles = { 0f, 45f, 90f, 135f };
        foreach (float deg in petalAngles)
        {
            int baseIdx = verts.Count;
            Quaternion rot = Quaternion.Euler(0, deg, 0);
            Vector3 r = rot * Vector3.right * petalRadius;

            // Dasar kelopak di pucuk tangkai
            Vector3 b0 = blossomCenter - r * 0.25f;
            Vector3 b1 = blossomCenter + r * 0.25f;

            // Ujung kelopak melengkung mekar ke luar dan ke atas
            Vector3 t0 = blossomCenter - r + Vector3.up * petalCupHeight;
            Vector3 t1 = blossomCenter + r + Vector3.up * petalCupHeight;

            verts.Add(b0); verts.Add(b1); verts.Add(t1); verts.Add(t0);

            // Normal melengkung ke atas (spherical cup) agar kelopak menyerap cahaya matahari hangat
            Vector3 n0 = (Vector3.up * 0.8f - r * 0.4f).normalized;
            Vector3 n1 = (Vector3.up * 0.8f + r * 0.4f).normalized;
            norms.Add(n0); norms.Add(n1); norms.Add(n1); norms.Add(n0);

            uvs.Add(new Vector2(0.25f, 0f));
            uvs.Add(new Vector2(0.75f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            // Two-sided
            trisBlossom.Add(baseIdx + 0); trisBlossom.Add(baseIdx + 2); trisBlossom.Add(baseIdx + 1);
            trisBlossom.Add(baseIdx + 0); trisBlossom.Add(baseIdx + 3); trisBlossom.Add(baseIdx + 2);
            trisBlossom.Add(baseIdx + 0); trisBlossom.Add(baseIdx + 1); trisBlossom.Add(baseIdx + 2);
            trisBlossom.Add(baseIdx + 0); trisBlossom.Add(baseIdx + 2); trisBlossom.Add(baseIdx + 3);
        }

        // Piringan pusat putik bunga (Center disk / Pistil)
        int centerBaseIdx = verts.Count;
        float diskR = 0.08f;
        int diskSegments = 8;
        verts.Add(blossomCenter + Vector3.up * 0.03f); // Pusat
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= diskSegments; i++)
        {
            float ang = (i % diskSegments) * Mathf.PI * 2f / diskSegments;
            Vector3 pt = blossomCenter + new Vector3(Mathf.Cos(ang) * diskR, 0.035f, Mathf.Sin(ang) * diskR);
            verts.Add(pt);
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + Mathf.Cos(ang) * 0.5f, 0.5f + Mathf.Sin(ang) * 0.5f));
        }

        for (int i = 1; i <= diskSegments; i++)
        {
            trisBlossom.Add(centerBaseIdx);
            trisBlossom.Add(centerBaseIdx + i);
            trisBlossom.Add(centerBaseIdx + i + 1);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);

        mesh.SetTriangles(trisStem, 0);
        mesh.SetTriangles(trisBlossom, 1);
        mesh.RecalculateBounds();

        return mesh;
    }
}
#endif
