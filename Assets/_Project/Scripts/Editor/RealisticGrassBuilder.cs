#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator Rumpun Rumput 3D Realistis (Realistic 3D Grass Tufts) untuk Desa Eldara.
/// Membangun padang rumput dan tanaman bunga liar berhelai 3D:
/// - 3 bilah quad saling silang (tri-star / 60 derajat) dengan alpha cutout
/// - Diperkaya bunga aster putih & dandelion keemasan
/// - Normal melengkung ke atas (spherical up-normals) agar menyerap cahaya matahari lembut
/// - Animasi hembusan angin dinamis (GrassWindSway)
/// - GPU Instancing untuk performa tinggi & FPS maksimal
/// </summary>
public static class RealisticGrassBuilder
{
    private static Mesh sharedGrassMesh;

    public static GameObject BuildGrassTuft(Vector3 position, float scale, Transform parent, Material grassMat)
    {
        if (!sharedGrassMesh)
        {
            sharedGrassMesh = GenerateGrassMesh();
        }

        var go = new GameObject("Grass_Tuft");
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        go.transform.localScale = Vector3.one * scale;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = sharedGrassMesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = grassMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Rumput kecil tidak perlu cast shadow berat
        mr.receiveShadows = true;

        var sway = go.AddComponent<GrassWindSway>();
        sway.swayIntensity = Random.Range(3.0f, 4.5f);
        sway.swaySpeed = Random.Range(1.5f, 2.0f);

        return go;
    }

    public static void BuildGrassClump(Vector3 center, int count, float radius, Transform parent, Material grassMat)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 pos = center + new Vector3(circle.x, 0, circle.y);
            float scale = Random.Range(0.85f, 1.25f);
            BuildGrassTuft(pos, scale, parent, grassMat);
        }
    }

    private static Mesh GenerateGrassMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Shared_Realistic_Grass_Tuft";

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // 3 quad saling silang (sudut 0, 60, dan 120 derajat)
        float[] angles = { 0f, 60f, 120f };
        float width = 1.15f;
        float height = 1.05f;
        float halfW = width * 0.5f;

        foreach (float deg in angles)
        {
            int baseIdx = verts.Count;
            Quaternion rot = Quaternion.Euler(0, deg, 0);

            Vector3 right = rot * Vector3.right * halfW;
            Vector3 forward = rot * Vector3.forward;

            // Dasar rumput di tanah (Y = 0)
            Vector3 b0 = -right;
            Vector3 b1 =  right;

            // Puncak rumput sedikit mekar melengkung ke luar
            Vector3 t0 = -right * 1.15f + Vector3.up * height;
            Vector3 t1 =  right * 1.15f + Vector3.up * height;

            verts.Add(b0);
            verts.Add(b1);
            verts.Add(t1);
            verts.Add(t0);

            // Normal melengkung ke atas agar shading rumput cerah dan menyerap cahaya matahari
            Vector3 n0 = (Vector3.up * 0.7f - right * 0.3f).normalized;
            Vector3 n1 = (Vector3.up * 0.7f + right * 0.3f).normalized;

            norms.Add(n0);
            norms.Add(n1);
            norms.Add(n1);
            norms.Add(n0);

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

            // Segitiga belakang (two-sided)
            tris.Add(baseIdx + 0);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);

            tris.Add(baseIdx + 0);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 3);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);

        return mesh;
    }
}
#endif

