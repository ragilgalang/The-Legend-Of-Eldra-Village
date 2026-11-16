using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sistem Jarak Render Global ala Minecraft untuk seluruh dunia Legenda Desa Eldara.
/// - Membagi pohon hutan liar dan padang rumput menjadi petak-petak (chunks 25m).
/// - Mengatur on/off otomatis untuk 8 zona ekspansi besar (hanya aktif saat pemain mendekat).
/// - Mendukung Scene View di Editor dan Play Mode, sehingga Unity 100% bebas lag & patah-patah!
/// </summary>
[ExecuteAlways]
public class ChunkRenderDistance : MonoBehaviour
{
    public static ChunkRenderDistance Instance { get; private set; }

    [System.Serializable]
    public class ZoneTarget
    {
        public string zoneName;
        public GameObject zoneObject;
        public Vector3 center;
        public float activationRadius = 150f;
    }

    [Header("Pengaturan Jarak Render Chunk (Pohon & Rumput)")]
    [Tooltip("Ukuran satu petak (chunk) dalam meter")]
    public float chunkSize = 25f;

    [Range(2, 12)]
    [Tooltip("Jarak Render dalam satuan petak/gugus (misal 4 gugus = 100 meter)")]
    public int renderDistance = 4;

    [Header("Jarak Render Zona Wilayah Besar")]
    public List<ZoneTarget> expansionZones = new List<ZoneTarget>();

    [Header("Atmosfer Kabut")]
    [Tooltip("Menyesuaikan kabut otomatis agar batas render terlihat lembut seperti Minecraft")]
    public bool syncFogDistance = true;

    [Header("Target Pemain")]
    public Transform playerTarget;

    // Koordinat chunk pemain terakhir
    int lastChunkX = int.MinValue;
    int lastChunkZ = int.MinValue;
    Vector3 lastViewerPos = new Vector3(float.MinValue, 0f, float.MinValue);
    float updateTimer;

    // Daftar semua chunk grid terdaftar: koordinat -> daftar GameObject chunk (bisa dari rumput, pohon hutan, dll.)
    readonly Dictionary<Vector2Int, List<GameObject>> registeredChunks = new Dictionary<Vector2Int, List<GameObject>>();

    void OnEnable()
    {
        Instance = this;
        if (Application.isPlaying && PlayerPrefs.HasKey("eldara_chunk_distance"))
        {
            renderDistance = Mathf.Clamp(PlayerPrefs.GetInt("eldara_chunk_distance", renderDistance), 2, 12);
        }
        FindPlayerTarget();
        AutoDiscoverChunks();
        UpdateAll(true);
    }

    /// <summary>Ubah jarak render chunk saat runtime dan simpan ke PlayerPrefs</summary>
    public void SetRenderDistance(int chunks)
    {
        renderDistance = Mathf.Clamp(chunks, 2, 12);
        PlayerPrefs.SetInt("eldara_chunk_distance", renderDistance);
        PlayerPrefs.Save();
        UpdateAll(true);
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Di Editor saat tidak Play Mode, JANGAN culling chunk agar objek di scene
        // tidak tersimpan dalam keadaan mati (m_IsActive: 0)!
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif

        updateTimer -= Time.deltaTime;
        if (updateTimer > 0f) return;
        updateTimer = 0.15f; // Cek ~6x per detik, sangat ringan

        Vector3 viewerPos = GetViewerPosition();
        int currentChunkX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        // Update jika pemain pindah chunk atau geser cukup jauh
        if (currentChunkX != lastChunkX || currentChunkZ != lastChunkZ || Vector3.Distance(viewerPos, lastViewerPos) > 5f)
        {
            lastChunkX = currentChunkX;
            lastChunkZ = currentChunkZ;
            lastViewerPos = viewerPos;
            UpdateAll();
        }

        if (syncFogDistance && RenderSettings.fog)
        {
            float maxDist = renderDistance * chunkSize;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = Mathf.Max(25f, (renderDistance - 2) * chunkSize);
            RenderSettings.fogEndDistance = maxDist;
        }
    }

    /// <summary>
    /// Otomatis mendeteksi dan mendaftarkan semua petak chunk (Chunk_X_Z) yang sudah ada di scene.
    /// Memastikan runtime Play Mode selalu mengenali semua chunk tanpa bergantung pada memory Editor.
    /// </summary>
    public void AutoDiscoverChunks()
    {
        registeredChunks.Clear();

        string[] containerNames = { "--- WILDERNESS ---", "--- GRASS FIELDS ---", "--- JUNGLE BIOME ---", "Jungle_Biome" };
        foreach (var cName in containerNames)
        {
            var container = GameObject.Find(cName);
            if (container == null) continue;

            if (!container.activeSelf) container.SetActive(true);

            for (int i = 0; i < container.transform.childCount; i++)
            {
                var child = container.transform.GetChild(i);
                if (child.name.StartsWith("Chunk_"))
                {
                    // Jika --- 3D WILDFLOWERS --- tersasar masuk ke dalam Chunk, keluarkan ke container utama!
                    var flowerChild = child.Find("--- 3D WILDFLOWERS ---");
                    if (flowerChild != null)
                    {
                        flowerChild.SetParent(container.transform, true);
                        flowerChild.gameObject.SetActive(true);
                    }

                    string[] parts = child.name.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int cx) && int.TryParse(parts[2], out int cz))
                    {
                        RegisterChunkInternal(new Vector2Int(cx, cz), child.gameObject);
                    }
                }
                else if (child.name == "--- 3D WILDFLOWERS ---")
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
    }

    void RegisterChunkInternal(Vector2Int coord, GameObject chunkObj)
    {
        if (chunkObj == null) return;
        if (!registeredChunks.TryGetValue(coord, out var list))
        {
            list = new List<GameObject>();
            registeredChunks[coord] = list;
        }
        if (!list.Contains(chunkObj))
            list.Add(chunkObj);
    }

    public void RegisterChunk(Vector2Int coord, GameObject chunkObj)
    {
        if (chunkObj == null) return;
        RegisterChunkInternal(coord, chunkObj);

        Vector3 viewerPos = GetViewerPosition();
        int currentChunkX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        bool visible = Mathf.Abs(coord.x - currentChunkX) <= renderDistance &&
                       Mathf.Abs(coord.y - currentChunkZ) <= renderDistance;

        chunkObj.SetActive(visible);
    }

    public void RegisterZone(string name, GameObject obj, Vector3 center, float radius)
    {
        if (obj == null) return;
        expansionZones.RemoveAll(z => z.zoneName == name || z.zoneObject == obj);
        expansionZones.Add(new ZoneTarget
        {
            zoneName = name,
            zoneObject = obj,
            center = center,
            activationRadius = radius
        });
    }

    public void ClearRegistry()
    {
        registeredChunks.Clear();
        expansionZones.Clear();
        lastChunkX = int.MinValue;
        lastChunkZ = int.MinValue;
    }

    public void UpdateAllChunks(bool force = false) => UpdateAll(force);

    public void UpdateAll(bool force = false)
    {
        Vector3 viewerPos = GetViewerPosition();
        int currentChunkX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        // 1. Update Grid Chunks (Pohon, semak, rumput)
        var toRemove = new List<Vector2Int>();
        foreach (var kvp in registeredChunks)
        {
            var list = kvp.Value;
            if (list == null || list.Count == 0) { toRemove.Add(kvp.Key); continue; }

            int dx = Mathf.Abs(kvp.Key.x - currentChunkX);
            int dz = Mathf.Abs(kvp.Key.y - currentChunkZ);
            bool shouldBeActive = dx <= renderDistance && dz <= renderDistance;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null) { list.RemoveAt(i); continue; }
                if (force || list[i].activeSelf != shouldBeActive)
                {
                    list[i].SetActive(shouldBeActive);
                }
            }
        }
        foreach (var k in toRemove) registeredChunks.Remove(k);

        // 2. Update Zona-Zona Ekspansi Besar
        for (int i = expansionZones.Count - 1; i >= 0; i--)
        {
            var z = expansionZones[i];
            if (z.zoneObject == null)
            {
                expansionZones.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(viewerPos, z.center);
            bool active = dist <= z.activationRadius;

            if (force || z.zoneObject.activeSelf != active)
            {
                z.zoneObject.SetActive(active);
            }
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
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) playerTarget = pc.transform;
        else
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) playerTarget = pObj.transform;
        }
    }

    /// <summary>
    /// Membagi sebuah GameObject induk (berisi banyak pohon/objek) ke dalam petak chunk 25m.
    /// </summary>
    public void ChunkifyContainer(Transform container)
    {
        if (container == null || container.childCount == 0) return;

        var chunkParents = new Dictionary<Vector2Int, Transform>();
        var children = new List<Transform>();
        for (int i = 0; i < container.childCount; i++)
        {
            children.Add(container.GetChild(i));
        }

        foreach (var child in children)
        {
            // Jangan masukkan chunk lain atau sub-container (seperti --- 3D WILDFLOWERS ---) ke dalam chunk!
            if (child.name.StartsWith("Chunk_") || child.name.StartsWith("---")) continue;

            Vector3 pos = child.position;
            int cx = Mathf.FloorToInt(pos.x / chunkSize);
            int cz = Mathf.FloorToInt(pos.z / chunkSize);
            var coord = new Vector2Int(cx, cz);

            if (!chunkParents.TryGetValue(coord, out Transform chunkT) || chunkT == null)
            {
                var chunkGO = new GameObject($"Chunk_{cx}_{cz}");
                chunkGO.transform.SetParent(container, true);
                chunkT = chunkGO.transform;
                chunkParents[coord] = chunkT;
                RegisterChunk(coord, chunkGO);
            }

            child.SetParent(chunkT, true);
        }
    }

#if UNITY_EDITOR
    public static void ApplyToEntireWorldMenu()
    {
        var manager = Object.FindObjectOfType<ChunkRenderDistance>();
        if (manager == null)
        {
            var go = new GameObject("--- CHUNK RENDER DISTANCE ---");
            manager = go.AddComponent<ChunkRenderDistance>();
            Undo.RegisterCreatedObjectUndo(go, "Create Chunk Render Distance");
        }

        manager.ClearRegistry();

        // 1. Chunkify Hutan Liar Keliling Desa
        var wilderness = GameObject.Find("--- WILDERNESS ---");
        if (wilderness != null) manager.ChunkifyContainer(wilderness.transform);

        // 2. Chunkify Padang Rumput
        var grassFields = GameObject.Find("--- GRASS FIELDS ---");
        if (grassFields != null) manager.ChunkifyContainer(grassFields.transform);

        // 3. Chunkify Hutan Rimba
        var jungle = GameObject.Find("Jungle_Biome") ?? GameObject.Find("--- JUNGLE BIOME ---");
        if (jungle != null) manager.ChunkifyContainer(jungle.transform);

        // 4. Daftarkan 4 Zona Ekspansi Utama
        var zNorth = GameObject.Find("Zone_North_MountainVillage");
        if (zNorth) manager.RegisterZone("Pegunungan Utara", zNorth, new Vector3(0f, 0f, 185f), 150f);

        var zSouth = GameObject.Find("Zone_South_SwampCamp");
        if (zSouth) manager.RegisterZone("Rawa Selatan", zSouth, new Vector3(0f, 0f, -185f), 150f);

        var zWest = GameObject.Find("Zone_West_FishingVillage");
        if (zWest) manager.RegisterZone("Desa Nelayan Barat", zWest, new Vector3(-175f, 0f, 0f), 150f);

        var zEast = GameObject.Find("Zone_East_CrystalMines");
        if (zEast) manager.RegisterZone("Tambang Kristal Timur", zEast, new Vector3(175f, 0f, 0f), 150f);

        // 5. Daftarkan 4 Zona Mega Ekspansi
        var mNorth = GameObject.Find("Zone_FarNorth_SnowPeak");
        if (mNorth) manager.RegisterZone("Puncak Salju Utara", mNorth, new Vector3(0f, 0f, 400f), 180f);

        var mSouth = GameObject.Find("Zone_FarSouth_FairyForest");
        if (mSouth) manager.RegisterZone("Hutan Peri Selatan", mSouth, new Vector3(0f, 0f, -400f), 180f);

        var mWest = GameObject.Find("Zone_FarWest_SeaTemple");
        if (mWest) manager.RegisterZone("Kuil Laut Barat", mWest, new Vector3(-400f, 0f, 0f), 180f);

        var mEast = GameObject.Find("Zone_FarEast_FireCanyon");
        if (mEast) manager.RegisterZone("Ngarai Api Timur", mEast, new Vector3(400f, 0f, 0f), 180f);

        // Di Editor, pastikan semua chunk tetap aktif tersimpan di scene agar tidak hilang
        foreach (var kvp in manager.registeredChunks)
        {
            if (kvp.Value == null) continue;
            foreach (var go in kvp.Value)
            {
                if (go != null) go.SetActive(true);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "⚡ Chunk Render Diterapkan ke Seluruh Peta!",
                $"Sistem Jarak Render ala Minecraft kini telah aktif untuk SELURUH dunia Eldara!\n\n" +
                $"• Hutan Rimba & Hutan Liar dibagi ke petak-petak chunk 25m.\n" +
                $"• Padang rumput & bunga 3D dikendalikan per chunk.\n" +
                $"• 8 Zona Ekspansi & Mega Ekspansi hanya aktif saat didekati pemain.\n" +
                $"• Jarak Render: {manager.renderDistance} Gugus ({manager.renderDistance * manager.chunkSize} Meter).\n\n" +
                $"Game dan Editor dijamin super lancar dan ringan!",
                "Keren!");
        }
    }
#endif
}
