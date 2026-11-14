using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Rumput prosedural untuk lantai hutan rimba.
/// - Rumput dibagi per "chunk" (petak 16 m) dan hanya chunk dekat kamera yang digambar.
/// - Bilah rumput punya 3 segmen melengkung; vertex color menyimpan data untuk shader angin:
///     R = bobot lentur (0 di akar -> 1 di ujung), G = fase acak per bilah, B = kekakuan acak.
/// - Rumput menghindari area batang pohon (JungleTree) dan membentuk rumpun serta petak tanah gundul.
/// Pasang di GameObject yang sama dengan JungleBiomeGenerator (area & terrain disamakan).
/// Rumput dibuat satu frame setelah pohon agar posisi pohon sudah ada.
/// </summary>
public class JungleGrass : MonoBehaviour
{
    [Header("Area")]
    public Terrain terrain;                          // samakan dengan JungleBiomeGenerator
    public Vector2 areaSize = new Vector2(200f, 200f);
    public int seed = 777;
    public bool generateOnStart = true;

    [Header("Kepadatan")]
    [Range(0.1f, 4f)] public float clumpsPerSquareMeter = 1.2f;   // rumpun per m2
    public Vector2Int bladesPerClump = new Vector2Int(4, 7);
    public float clumpRadius = 0.28f;
    public float trunkClearance = 0.4f;              // jarak aman di luar akar banir
    public float cellSize = 16f;                     // ukuran chunk

    [Header("Bilah Rumput")]
    public Vector2 bladeHeight = new Vector2(0.45f, 1.1f);
    public Vector2 bladeWidth = new Vector2(0.04f, 0.075f);
    public float patchNoiseScale = 0.07f;            // skala petak rumput tinggi/rendah/gundul

    [Header("Tampilan")]
    public Material grassMaterial;                   // kosong = dibuat otomatis (URP / Built-in)
    public Color rootColor = new Color(0.05f, 0.18f, 0.05f);
    public Color tipColor = new Color(0.33f, 0.60f, 0.17f);
    public float windBend = 0.6f;                    // lentur maksimal (meter) saat angin penuh
    public float drawDistance = 55f;

    [Header("Angin")]
    public bool autoCreateWind = true;               // otomatis menambah JungleWind bila belum ada

    struct Trunk { public Vector2 pos; public float r2; }

    readonly List<MeshRenderer> chunks = new List<MeshRenderer>();
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Trunk> trunks = new List<Trunk>();
    Transform root;
    float cullTimer;

    static readonly float[] RowT = { 0f, 0.45f, 0.8f };

    System.Collections.IEnumerator Start()
    {
        if (!generateOnStart) yield break;
        yield return null;   // tunggu pohon selesai dibuat
        Generate();
    }

    [ContextMenu("Generate Grass")]
    public void Generate()
    {
        Clear();

        Random.State old = Random.state;
        Random.InitState(seed);

        root = new GameObject("JungleGrass").transform;
        root.SetParent(transform, false);

        bool ownMaterial = grassMaterial == null;
        Material mat = ownMaterial ? MakeMaterial() : grassMaterial;

        CollectTrunks();

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var cols = new List<Color32>();
        var tris = new List<int>();

        int nx = Mathf.CeilToInt(areaSize.x / cellSize);
        int nz = Mathf.CeilToInt(areaSize.y / cellSize);

        for (int ix = 0; ix < nx; ix++)
        for (int iz = 0; iz < nz; iz++)
        {
            float x0 = -areaSize.x * 0.5f + ix * cellSize;
            float z0 = -areaSize.y * 0.5f + iz * cellSize;
            float x1 = Mathf.Min(x0 + cellSize, areaSize.x * 0.5f);
            float z1 = Mathf.Min(z0 + cellSize, areaSize.y * 0.5f);

            verts.Clear(); norms.Clear(); cols.Clear(); tris.Clear();

            int clumps = Mathf.RoundToInt((x1 - x0) * (z1 - z0) * clumpsPerSquareMeter);
            for (int c = 0; c < clumps; c++)
            {
                float cx = Random.Range(x0, x1);
                float cz = Random.Range(z0, z1);
                if (InsideTrunk(cx, cz)) continue;

                float patch = Mathf.PerlinNoise(cx * patchNoiseScale + 50f, cz * patchNoiseScale + 50f);
                if (patch < 0.18f) continue;                       // petak tanah gundul
                float heightScale = Mathf.Lerp(0.6f, 1.25f, patch);   // petak rumput tinggi

                float gy = Ground(cx, cz);
                int n = Random.Range(bladesPerClump.x, bladesPerClump.y + 1);
                for (int b = 0; b < n; b++)
                {
                    Vector2 o = Random.insideUnitCircle * clumpRadius;
                    AddBlade(verts, norms, cols, tris, new Vector3(cx + o.x, gy, cz + o.y), heightScale);
                }
            }

            if (verts.Count == 0) continue;

            var mesh = new Mesh { name = "GrassChunk", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            Bounds bnd = mesh.bounds; bnd.Expand(new Vector3(1.5f, 0.5f, 1.5f)); mesh.bounds = bnd; // ruang untuk goyangan
            meshes.Add(mesh);

            var go = new GameObject("Grass_" + ix + "_" + iz);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            chunks.Add(mr);
        }

        Random.state = old;

        if (autoCreateWind && FindFirstObjectByType<JungleWind>() == null)
            gameObject.AddComponent<JungleWind>();
    }

    [ContextMenu("Clear Grass")]
    public void Clear()
    {
        chunks.Clear();
        var old = transform.Find("JungleGrass");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
        }
        foreach (var m in meshes)
        {
            if (m == null) continue;
            if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
        }
        meshes.Clear();
    }

    // ------------------------------------------------------------- CULLING
    void Update()
    {
        cullTimer -= Time.deltaTime;
        if (cullTimer > 0f || chunks.Count == 0) return;
        cullTimer = 0.3f;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 cp = cam.transform.position;
        float sq = drawDistance * drawDistance;
        for (int i = 0; i < chunks.Count; i++)
        {
            if (chunks[i] == null) continue;
            chunks[i].enabled = (chunks[i].bounds.ClosestPoint(cp) - cp).sqrMagnitude < sq;
        }
    }

    // ------------------------------------------------------------- HELPERS
    void CollectTrunks()
    {
        trunks.Clear();
        foreach (var t in FindObjectsByType<JungleTree>(FindObjectsSortMode.None))
        {
            Vector3 p = t.transform.position - transform.position;
            float r = t.baseRadius * (1f + t.buttressFlare) + trunkClearance;
            trunks.Add(new Trunk { pos = new Vector2(p.x, p.z), r2 = r * r });
        }
    }

    bool InsideTrunk(float x, float z)
    {
        for (int i = 0; i < trunks.Count; i++)
        {
            float dx = trunks[i].pos.x - x, dz = trunks[i].pos.y - z;
            if (dx * dx + dz * dz < trunks[i].r2) return true;
        }
        return false;
    }

    float Ground(float lx, float lz)
    {
        if (terrain == null) return 0f;
        Vector3 w = new Vector3(lx, 0f, lz) + transform.position;
        return terrain.SampleHeight(w) + terrain.transform.position.y - transform.position.y;
    }

    // Satu bilah: 3 baris (kiri/kanan) + 1 titik ujung = 7 vertex, 5 segitiga.
    void AddBlade(List<Vector3> v, List<Vector3> n, List<Color32> col, List<int> idx, Vector3 p, float heightScale)
    {
        float h = Random.Range(bladeHeight.x, bladeHeight.y) * heightScale;
        float w = Random.Range(bladeWidth.x, bladeWidth.y);
        float yaw = Random.value * Mathf.PI * 2f;
        Vector3 fwd = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
        Vector3 right = new Vector3(-fwd.z, 0f, fwd.x);
        float curve = Random.Range(0.1f, 0.4f) * h;                 // lengkung alami ke depan
        byte phase = (byte)Random.Range(0, 256);
        byte stiff = (byte)Random.Range(0, 256);

        int b0 = v.Count;
        for (int i = 0; i < RowT.Length; i++)
        {
            float t = RowT[i];
            Vector3 c = p + Vector3.up * (h * t) + fwd * (curve * t * t);
            float hw = w * 0.5f * (1f - t * 0.7f);                  // meruncing ke ujung
            var cc = new Color32((byte)(t * 255f), phase, stiff, 255);
            v.Add(c - right * hw); col.Add(cc); n.Add(Vector3.up);
            v.Add(c + right * hw); col.Add(cc); n.Add(Vector3.up);
        }
        v.Add(p + Vector3.up * h + fwd * curve);
        col.Add(new Color32(255, phase, stiff, 255));
        n.Add(Vector3.up);

        // cull Off di shader, jadi arah putaran segitiga tidak berpengaruh
        idx.Add(b0);     idx.Add(b0 + 1); idx.Add(b0 + 2);
        idx.Add(b0 + 1); idx.Add(b0 + 3); idx.Add(b0 + 2);
        idx.Add(b0 + 2); idx.Add(b0 + 3); idx.Add(b0 + 4);
        idx.Add(b0 + 3); idx.Add(b0 + 5); idx.Add(b0 + 4);
        idx.Add(b0 + 4); idx.Add(b0 + 5); idx.Add(b0 + 6);
    }

    Material MakeMaterial()
    {
        bool srp = GraphicsSettings.currentRenderPipeline != null;
        Shader s = Shader.Find(srp ? "Jungle/GrassWindURP" : "Jungle/GrassWindBuiltin");
        if (s == null)
        {
            Debug.LogWarning("Shader rumput angin tidak ditemukan. Pastikan file .shader ada di project. Memakai shader bawaan tanpa angin.");
            s = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
        }
        var m = new Material(s);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", rootColor);
        if (m.HasProperty("_TipColor")) m.SetColor("_TipColor", tipColor);
        if (m.HasProperty("_WindBend")) m.SetFloat("_WindBend", windBend);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        return m;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Eldara/Biomes/Pasang Jungle Grass ke Objek Jungle")]
    public static void AttachJungleGrassMenu()
    {
        var biome = Object.FindFirstObjectByType<JungleBiomeGenerator>();
        GameObject target = biome != null ? biome.gameObject : (UnityEditor.Selection.activeGameObject != null ? UnityEditor.Selection.activeGameObject : new GameObject("Jungle_Grass"));
        if (target.GetComponent<JungleGrass>() == null)
            target.AddComponent<JungleGrass>();
        UnityEditor.Selection.activeGameObject = target;
        Debug.Log("[Eldara] Komponen JungleGrass berhasil dipasang ke " + target.name);
    }
#endif
}
