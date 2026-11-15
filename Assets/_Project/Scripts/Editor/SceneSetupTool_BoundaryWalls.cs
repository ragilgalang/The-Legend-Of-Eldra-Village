#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dinding batas tak terlihat di tepi peta 1000 x 1000 m (Ground = ±500 m).
/// Menggantikan "pohon pembatas" hijau-kuning di ±490 m pada SetupMegaExpansion().
/// Tidak ada MeshRenderer, hanya 4 BoxCollider — nol beban render.
/// </summary>
public static partial class SceneSetupTool
{
    const string OuterBoundaryName = "--- MAP BOUNDARY (OUTER) ---";

    /// <summary>
    /// half = jarak sisi DALAM dinding dari pusat peta (495 m = 5 m sebelum tepi Ground).
    /// height = tinggi dinding (60 m, tidak bisa dilompati).
    /// </summary>
    static GameObject BuildMapBoundaryWalls(Transform parent = null, float half = 495f,
                                            float height = 60f, float thickness = 4f)
    {
        var old = GameObject.Find(OuterBoundaryName);
        while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(OuterBoundaryName); }

        var root = new GameObject(OuterBoundaryName);
        if (parent) root.transform.SetParent(parent);
        root.transform.position = Vector3.zero;

        float c = half + thickness * 0.5f;          // pusat dinding
        float len = (half + thickness) * 2f;        // panjang dinding (menutup sudut)

        var walls = new (string name, Vector3 center, Vector3 size)[]
        {
            ("Barrier_North", new Vector3(0f,  height * 0.5f,  c), new Vector3(len, height, thickness)),
            ("Barrier_South", new Vector3(0f,  height * 0.5f, -c), new Vector3(len, height, thickness)),
            ("Barrier_East",  new Vector3( c,  height * 0.5f, 0f), new Vector3(thickness, height, len)),
            ("Barrier_West",  new Vector3(-c,  height * 0.5f, 0f), new Vector3(thickness, height, len)),
        };

        foreach (var w in walls)
        {
            var go = new GameObject(w.name);
            go.transform.SetParent(root.transform);
            go.transform.position = w.center;
            var col = go.AddComponent<BoxCollider>();
            col.size = w.size;
            col.isTrigger = false;
        }
        return root;
    }

    /// <summary>
    /// Untuk scene yang sudah jadi: hapus pohon pembatas di tepi peta lalu pasang dinding tak terlihat,
    /// tanpa membangun ulang seluruh dunia.
    /// </summary>
    public static void ReplaceBorderTreesWithWalls()
    {
        var mega = GameObject.Find("--- MEGA EXPANSION ---");
        int removed = 0;

        if (mega)
        {
            for (int i = mega.transform.childCount - 1; i >= 0; i--)
            {
                var child = mega.transform.GetChild(i);
                if (child.name.StartsWith("Zone_") || child.name.StartsWith("MegaRoad") || child.name == OuterBoundaryName)
                    continue;

                var rends = child.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) continue;

                var b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);

                bool singleTree = Mathf.Max(b.size.x, b.size.z) < 60f;
                bool atEdge = Mathf.Max(Mathf.Abs(b.center.x), Mathf.Abs(b.center.z)) > 470f;
                if (singleTree && atEdge)
                {
                    Object.DestroyImmediate(child.gameObject);
                    removed++;
                }
            }
        }

        BuildMapBoundaryWalls(mega ? mega.transform : null);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Eldara] {removed} pohon pembatas dihapus, dinding tak terlihat dipasang di ±495 m.");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Selesai",
                $"{removed} pohon pembatas dihapus.\nDinding tak terlihat terpasang di tepi peta (±495 m).", "OK");
    }
}
#endif
