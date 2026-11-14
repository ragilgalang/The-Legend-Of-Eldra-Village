using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sistem Jarak Render ala Minecraft (Chunk-based Render Distance).
/// Membagi dunia/hutan menjadi petak (gugus / chunk) berukuran 20-25m.
/// Hanya petak dalam radius "Jarak Render" di sekitar pemain yang aktif/dirender.
/// Petak di luar jarak render otomatis di-nonaktifkan (0% beban GPU & CPU).
/// </summary>
[ExecuteAlways]
public class ChunkRenderDistance : MonoBehaviour
{
    public static ChunkRenderDistance Instance { get; private set; }

    [Header("Pengaturan Jarak Render (ala Minecraft)")]
    [Tooltip("Ukuran satu petak (chunk) dalam meter")]
    public float chunkSize = 25f;

    [Range(2, 12)]
    [Tooltip("Jarak Render dalam satuan gugus/chunk (misal 4 gugus = 100 meter)")]
    public int renderDistance = 4;

    [Header("Atmosfer Kabut")]
    [Tooltip("Otomatis menyesuaikan jarak kabut dengan jarak render agar batas chunk tidak terlihat pop-in (seperti Minecraft)")]
    public bool syncFogDistance = true;

    [Header("Target Kamera / Pemain")]
    public Transform playerTarget;

    // Koordinat chunk pemain terakhir
    int lastChunkX = int.MinValue;
    int lastChunkZ = int.MinValue;
    float updateTimer;

    // Daftar semua chunk terdaftar: koordinat grid -> GameObject chunk
    readonly Dictionary<Vector2Int, GameObject> registeredChunks = new Dictionary<Vector2Int, GameObject>();

    void OnEnable()
    {
        Instance = this;
        FindPlayerTarget();
        UpdateAllChunks(true);
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer > 0f) return;
        updateTimer = 0.2f; // Cek 5x per detik, sangat ringan!

        Vector3 viewerPos = GetViewerPosition();
        int currentChunkX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        if (currentChunkX != lastChunkX || currentChunkZ != lastChunkZ)
        {
            lastChunkX = currentChunkX;
            lastChunkZ = currentChunkZ;
            UpdateAllChunks();
        }

        if (syncFogDistance && RenderSettings.fog)
        {
            float maxDist = renderDistance * chunkSize;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = Mathf.Max(20f, (renderDistance - 2) * chunkSize);
            RenderSettings.fogEndDistance = maxDist;
        }
    }

    /// <summary>
    /// Mendaftarkan petak chunk ke sistem render distance.
    /// </summary>
    public void RegisterChunk(Vector2Int coord, GameObject chunkObj)
    {
        if (chunkObj == null) return;
        registeredChunks[coord] = chunkObj;

        // Cek visibilitas langsung saat didaftarkan
        Vector3 viewerPos = GetViewerPosition();
        int currentChunkX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        bool visible = Mathf.Abs(coord.x - currentChunkX) <= renderDistance &&
                       Mathf.Abs(coord.y - currentChunkZ) <= renderDistance;

        chunkObj.SetActive(visible);
    }

    /// <summary>
    /// Membersihkan semua chunk yang terdaftar.
    /// </summary>
    public void ClearRegistry()
    {
        registeredChunks.Clear();
        lastChunkX = int.MinValue;
        lastChunkZ = int.MinValue;
    }

    /// <summary>
    /// Memperbarui status aktif/nonaktif semua chunk berdasarkan posisi pemain.
    /// </summary>
    public void UpdateAllChunks(bool force = false)
    {
        Vector3 viewerPos = GetViewerPosition();
        int currentChunkX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        var keysToRemove = new List<Vector2Int>();

        foreach (var kvp in registeredChunks)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
                continue;
            }

            int dx = Mathf.Abs(kvp.Key.x - currentChunkX);
            int dz = Mathf.Abs(kvp.Key.y - currentChunkZ);
            bool shouldBeActive = dx <= renderDistance && dz <= renderDistance;

            if (force || kvp.Value.activeSelf != shouldBeActive)
            {
                kvp.Value.SetActive(shouldBeActive);
            }
        }

        foreach (var k in keysToRemove)
        {
            registeredChunks.Remove(k);
        }
    }

    Vector3 GetViewerPosition()
    {
        if (Application.isPlaying)
        {
            if (playerTarget == null) FindPlayerTarget();
            if (playerTarget != null) return playerTarget.position;
            if (Camera.main != null) return Camera.main.transform.position;
        }
#if UNITY_EDITOR
        else
        {
            // Di Scene View Editor, ikuti kamera editor!
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                return SceneView.lastActiveSceneView.camera.transform.position;
            }
            if (Camera.main != null) return Camera.main.transform.position;
        }
#endif
        return Vector3.zero;
    }

    void FindPlayerTarget()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) playerTarget = pc.transform;
        else
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) playerTarget = pObj.transform;
        }
    }

#if UNITY_EDITOR
    [MenuItem("Eldara/Biomes/Pasang Sistem Jarak Render (Minecraft)")]
    public static void AttachRenderDistanceManager()
    {
        var existing = FindFirstObjectByType<ChunkRenderDistance>();
        if (existing == null)
        {
            var go = new GameObject("--- CHUNK RENDER DISTANCE ---");
            existing = go.AddComponent<ChunkRenderDistance>();
            Undo.RegisterCreatedObjectUndo(go, "Create Chunk Render Distance");
        }
        Selection.activeGameObject = existing.gameObject;
        Debug.Log("[Eldara] Sistem Jarak Render Chunk (ala Minecraft) berhasil aktif di Scene!");
    }
#endif
}
