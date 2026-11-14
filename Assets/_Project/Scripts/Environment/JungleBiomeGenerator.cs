using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generator biome Hutan Rimba: pohon raksasa (emergent), pohon menengah, semak lantai hutan,
/// kabut lembap, dan pencahayaan redup khas hutan tropis.
/// Cara pakai: taruh di GameObject kosong -> Play, atau klik kanan komponen -> "Generate Jungle".
/// </summary>
public class JungleBiomeGenerator : MonoBehaviour
{
    [Header("Area")]
    public Terrain terrain;                       // opsional; kosong = pakai tanah datar otomatis
    public Vector2 areaSize = new Vector2(200f, 200f);
    public int seed = 12345;
    public bool generateOnStart = true;

    [Header("Kepadatan")]
    public int giantTrees = 28;                   // pohon raksasa, batang sangat besar
    public int midTrees = 70;                     // pohon lapis tengah
    public int bushes = 350;                      // semak / paku-pakuan
    public float minTreeSpacing = 7f;

    [Header("Ukuran Pohon Raksasa")]
    public Vector2 giantHeight = new Vector2(38f, 55f);
    public Vector2 giantRadius = new Vector2(1.6f, 2.6f);

    [Header("Ukuran Pohon Menengah")]
    public Vector2 midHeight = new Vector2(20f, 30f);
    public Vector2 midRadius = new Vector2(0.8f, 1.3f);

    [Header("Atmosfer")]
    public bool setupAtmosphere = true;
    public Color fogColor = new Color(0.16f, 0.27f, 0.19f);
    [Range(0f, 0.08f)] public float fogDensity = 0.016f;

    Transform container;
    readonly List<Vector4> placed = new List<Vector4>(); // x,z,radius,-

    void Start()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("Generate Jungle")]
    public void Generate()
    {
        Clear();
        Random.InitState(seed);
        placed.Clear();

        container = new GameObject("JungleBiome").transform;
        container.SetParent(transform, false);

        if (terrain == null) CreateGround();

        // 1) pohon raksasa
        for (int i = 0; i < giantTrees; i++)
            SpawnTree("GiantTree", giantHeight, giantRadius, 11f, 36, 0.6f, 9f, i);

        // 2) pohon menengah
        for (int i = 0; i < midTrees; i++)
            SpawnTree("MidTree", midHeight, midRadius, 6f, 18, 0.55f, 5f, 1000 + i);

        // 3) semak lantai hutan
        for (int i = 0; i < bushes; i++) SpawnBush(i);

        if (setupAtmosphere) SetupAtmosphere();
    }

    [ContextMenu("Clear Jungle")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    // ------------------------------------------------------------- SPAWNING
    void SpawnTree(string label, Vector2 hRange, Vector2 rRange, float canopyR, int leaves,
                   float canopyStart, float clusterBase, int idx)
    {
        float h = Random.Range(hRange.x, hRange.y);
        float r = Random.Range(rRange.x, rRange.y);

        if (!FindSpot(r + minTreeSpacing * 0.5f, out Vector3 pos)) return;

        var go = new GameObject(label + "_" + idx);
        go.transform.SetParent(container, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(Random.Range(-1.5f, 1.5f), Random.value * 360f, Random.Range(-1.5f, 1.5f));

        var t = go.AddComponent<JungleTree>();
        t.height = h;
        t.baseRadius = r;
        t.topRadius = r * 0.32f;
        t.buttressCount = Random.Range(4, 8);
        t.buttressFlare = Random.Range(1.0f, 1.8f);
        t.buttressHeight = h * 0.12f;
        t.canopyRadius = canopyR * (h / 38f);
        t.leafClusters = leaves;
        t.canopyStart = canopyStart;
        t.clusterSize = new Vector2(clusterBase * 0.55f, clusterBase);
        t.leafTint = Color.Lerp(new Color(0.08f, 0.38f, 0.10f), new Color(0.22f, 0.55f, 0.15f), Random.value);
        t.Build(seed + idx * 31);
    }

    void SpawnBush(int idx)
    {
        if (!FindSpot(0.6f, out Vector3 pos, true)) return;

        var parts = new List<CombineInstance>();
        Mesh lumpy = JungleTree.LumpySphere();
        if (lumpy == null) return;

        int n = Random.Range(3, 6);
        for (int i = 0; i < n; i++)
        {
            Vector3 p = new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(0.3f, 0.9f), Random.Range(-1.1f, 1.1f));
            float s = Random.Range(0.8f, 1.6f);
            parts.Add(new CombineInstance
            {
                mesh = lumpy,
                transform = Matrix4x4.TRS(p, Quaternion.Euler(0, Random.value * 360f, 0), new Vector3(s, s * 0.6f, s))
            });
        }

        var go = new GameObject("Bush_" + idx);
        go.transform.SetParent(container, false);
        go.transform.position = pos;
        go.AddComponent<MeshFilter>().sharedMesh = JungleTree.Combine(parts);
        var mr = go.AddComponent<MeshRenderer>();
        Color c = Color.Lerp(new Color(0.10f, 0.40f, 0.12f), new Color(0.25f, 0.55f, 0.18f), Random.value);
        mr.sharedMaterial = JungleTree.MakeMaterial(c, 0.1f);
    }

    bool FindSpot(float radius, out Vector3 pos, bool ignoreSpacing = false)
    {
        pos = Vector3.zero;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float z = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);

            bool ok = true;
            if (!ignoreSpacing)
            {
                for (int i = 0; i < placed.Count; i++)
                {
                    float dx = placed[i].x - x, dz = placed[i].y - z;
                    float need = placed[i].z + radius;
                    if (dx * dx + dz * dz < need * need) { ok = false; break; }
                }
            }
            if (!ok) continue;

            pos = new Vector3(x, GroundHeight(x, z), z) + transform.position;
            if (!ignoreSpacing) placed.Add(new Vector4(x, z, radius, 0));
            return true;
        }
        return false;
    }

    float GroundHeight(float localX, float localZ)
    {
        if (terrain == null) return 0f;
        Vector3 w = new Vector3(localX, 0, localZ) + transform.position;
        return terrain.SampleHeight(w) + terrain.transform.position.y - transform.position.y;
    }

    // ------------------------------------------------------------- GROUND
    void CreateGround()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "JungleGround";
        g.transform.SetParent(container, false);
        g.transform.localScale = new Vector3(areaSize.x / 10f * 1.3f, 1f, areaSize.y / 10f * 1.3f);
        g.GetComponent<MeshRenderer>().sharedMaterial = JungleTree.MakeMaterial(new Color(0.16f, 0.12f, 0.07f), 0.05f);
    }

    // ----------------------------------------------------------- ATMOSPHERE
    void SetupAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.20f, 0.30f, 0.20f);

        Light sun = FindFirstObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            var l = new GameObject("JungleSun");
            sun = l.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.95f, 0.75f);
        sun.intensity = 0.9f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, 30f, 0f);

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fogColor;
        }
    }

#if UNITY_EDITOR
    [MenuItem("GameObject/3D Object/Jungle Biome", false, 10)]
    [MenuItem("Eldara/Biomes/Buat Objek Jungle Biome Lengkap")]
    public static void CreateJungleBiomeObject()
    {
        var go = new GameObject("Jungle_Biome");
        var gen = go.AddComponent<JungleBiomeGenerator>();
        var grass = go.AddComponent<JungleGrass>();
        var wind = go.AddComponent<JungleWind>();
        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create Jungle Biome");
        Debug.Log("[Eldara] Objek Jungle Biome lengkap (Generator + Grass + Wind) berhasil dibuat!");
    }
#endif
}
