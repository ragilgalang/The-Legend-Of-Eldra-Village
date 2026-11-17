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

    /// <summary>
    /// Memastikan posisi tidak berada di atas pelataran batu alun-alun, jalanan, atau di dalam rumah
    /// </summary>
    public static bool IsPositionOnStoneOrRoad(Vector3 pos)
    {
        // 1. Alun-Alun Pusat (Central Plaza): lingkaran radius 9.5 meter (bersih mutlak tanpa rumput liar)
        if (pos.x * pos.x + pos.z * pos.z < 9.5f * 9.5f) return true;

        // 1b. Sungai Eldara (pusat X = -55, lebar 10m, panjang 96m) + tanggul batu + margin 0.6m
        //     Mencegah rumput/bunga mengambang di atas permukaan air atau menembus tanggul.
        if (Mathf.Abs(pos.x + 55f) < 6.6f && Mathf.Abs(pos.z) < 50f) return true;
        // Dek jembatan kayu sungai (Z = 0, lebar ~4.6m + ram)
        if (Mathf.Abs(pos.x + 55f) < 7.5f && Mathf.Abs(pos.z) < 3.4f) return true;

        // 2. Jalur Utama (Boulevard):
        // North Avenue: X = 0, Z = 8.5 s/d 42
        if (Mathf.Abs(pos.x) < 2.3f && pos.z >= 8.5f && pos.z <= 42f) return true;
        // South Avenue: X = 0, Z = -50 s/d -8.5
        if (Mathf.Abs(pos.x) < 2.3f && pos.z <= -8.5f && pos.z >= -50f) return true;
        // East Avenue: Z = 0, X = 8.5 s/d 68
        if (Mathf.Abs(pos.z) < 2.3f && pos.x >= 8.5f && pos.x <= 68f) return true;
        // West Avenue: Z = 0, X = -68 s/d -8.5
        if (Mathf.Abs(pos.z) < 2.3f && pos.x <= -8.5f && pos.x >= -68f) return true;

        // 3. Jalan Cabang Penghubung:
        // Cabang Barat Laut: Z = 24, X = -24 s/d -3
        if (Mathf.Abs(pos.z - 24f) < 2.0f && pos.x >= -24f && pos.x <= -3f) return true;
        // Cabang Timur Laut: Z = 24, X = 3 s/d 24
        if (Mathf.Abs(pos.z - 24f) < 2.0f && pos.x >= 3f && pos.x <= 24f) return true;
        // Cabang Pertanian & Danau (Barat): X = -28, Z = -16 s/d 14
        if (Mathf.Abs(pos.x + 28f) < 2.0f && pos.z >= -16f && pos.z <= 14f) return true;
        // Cabang Pandai Besi & Cabin (Timur): X = 28, Z = -16 s/d 14
        if (Mathf.Abs(pos.x - 28f) < 2.0f && pos.z >= -16f && pos.z <= 14f) return true;
        // Cabang Selatan: Z = -30, X = -20 s/d 22
        if (Mathf.Abs(pos.z + 30f) < 2.0f && pos.x >= -20f && pos.x <= 22f) return true;

        // 4. Area Tapak Bangunan (Mencegah rumput tembus ke dalam lantai rumah)
        if (Mathf.Abs(pos.x) < 7f && pos.z >= 36f && pos.z <= 46f) return true; // Balai Desa
        if (Mathf.Abs(pos.x + 26f) < 6f && Mathf.Abs(pos.z - 26f) < 6f) return true; // Rumah Gadang
        if (Mathf.Abs(pos.x - 26f) < 6f && Mathf.Abs(pos.z - 26f) < 6f) return true; // Joglo
        if (Mathf.Abs(pos.x + 28f) < 6f && Mathf.Abs(pos.z - 14f) < 6f) return true; // Petani
        if (Mathf.Abs(pos.x + 48f) < 7f && Mathf.Abs(pos.z - 14f) < 6f) return true; // Barn
        if (Mathf.Abs(pos.x - 28f) < 6f && Mathf.Abs(pos.z - 14f) < 6f) return true; // Pandai Besi
        if (Mathf.Abs(pos.x - 44f) < 6f && Mathf.Abs(pos.z - 14f) < 6f) return true; // Gudang Besi
        if (Mathf.Abs(pos.x - 28f) < 6f && Mathf.Abs(pos.z + 16f) < 6f) return true; // Cabin
        if (Mathf.Abs(pos.x - 48f) < 6f && Mathf.Abs(pos.z + 16f) < 6f) return true; // Villa
        if (Mathf.Abs(pos.x + 20f) < 6f && Mathf.Abs(pos.z + 34f) < 6f) return true; // Chalet
        if (Mathf.Abs(pos.x - 24f) < 6f && Mathf.Abs(pos.z + 34f) < 6f) return true; // Minka
        if (Mathf.Abs(pos.x + 10f) < 5f && Mathf.Abs(pos.z + 46f) < 5f) return true; // A-Frame

        return false;
    }

    public static GameObject BuildGrassTuft(Vector3 position, float scale, Transform parent, Material grassMat)
    {
        if (IsPositionOnStoneOrRoad(position)) return null;

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
        sway.swayIntensity = Random.Range(1.8f, 2.8f);
        sway.swaySpeed = Random.Range(1.2f, 1.8f);

        return go;
    }

    public static void BuildGrassClump(Vector3 center, int count, float radius, Transform parent, Material grassMat)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 pos = center + new Vector3(circle.x, 0, circle.y);
            if (IsPositionOnStoneOrRoad(pos)) continue;
            float scale = Random.Range(0.85f, 1.25f);
            BuildGrassTuft(pos, scale, parent, grassMat);

            // 35% kesempatan menumbuhkan bunga 3D timbul di dalam rumpun rumput
            if (Random.value < 0.35f)
            {
                RealisticFlowerBuilder.FlowerColor col = (RealisticFlowerBuilder.FlowerColor)Random.Range(0, 5);
                RealisticFlowerBuilder.BuildFlower(pos + new Vector3(Random.Range(-0.25f, 0.25f), 0, Random.Range(-0.25f, 0.25f)),
                    col, Random.Range(0.85f, 1.20f), parent);
            }
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

        // 5 quad saling silang membentuk rumpun mekar rimbun (0, 36, 72, 108, 144 derajat)
        float[] angles = { 0f, 36f, 72f, 108f, 144f };
        float width = 0.95f;
        float baseHeight = 0.85f;
        float halfW = width * 0.5f;

        for (int idxQuad = 0; idxQuad < angles.Length; idxQuad++)
        {
            float deg = angles[idxQuad];
            int baseIdx = verts.Count;
            Quaternion rot = Quaternion.Euler(0, deg, 0);

            // Variasi tinggi bilah: bilah tengah lebih tinggi dan timbul (0.95m), bilah luar melengkung (0.75m)
            float h = (idxQuad % 2 == 0) ? baseHeight * 1.12f : baseHeight * 0.88f;

            Vector3 right = rot * Vector3.right * halfW;
            Vector3 forward = rot * Vector3.forward;

            // Dasar rumput di tanah (Y = 0)
            Vector3 b0 = -right;
            Vector3 b1 =  right;

            // Puncak rumput mekar melengkung ke luar secara alami
            Vector3 t0 = -right * 1.25f + Vector3.up * h;
            Vector3 t1 =  right * 1.25f + Vector3.up * h;

            verts.Add(b0);
            verts.Add(b1);
            verts.Add(t1);
            verts.Add(t0);

            // Normal melengkung ke atas agar shading rumput cerah dan menyerap cahaya matahari lembut
            Vector3 n0 = (Vector3.up * 0.8f - right * 0.3f).normalized;
            Vector3 n1 = (Vector3.up * 0.8f + right * 0.3f).normalized;

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

