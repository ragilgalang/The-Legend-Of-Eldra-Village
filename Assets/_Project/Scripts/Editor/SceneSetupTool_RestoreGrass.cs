#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Eldara > Pulihkan Rumput & Bunga Desa
/// Mengaktifkan kembali semua rumput dan bunga 3D yang sempat dinonaktifkan oleh culling,
/// mengeluarkan bunga dari chunk petak 0,0, dan menyinkronkan ke ChunkRenderDistance.
/// </summary>
public static partial class SceneSetupTool
{
    public static void RestoreVillageGrassAndFlowers()
    {
        if (Application.isPlaying)
        {
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Stop!", "Hentikan Play Mode dulu sebelum memulihkan rumput & bunga.", "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        GameObject grassRoot = null;
        GameObject wildernessRoot = null;
        GameObject jungleRoot = null;
        GameObject flowerRoot = null;

        foreach (var r in roots)
        {
            if (r.name == "--- GRASS FIELDS ---") grassRoot = r;
            else if (r.name == "--- WILDERNESS ---") wildernessRoot = r;
            else if (r.name == "--- JUNGLE BIOME ---") jungleRoot = r;
            else if (r.name == "--- 3D WILDFLOWERS ---") flowerRoot = r;
        }

        // Jika --- GRASS FIELDS --- belum ada atau kosong sama sekali, bangun ulang
        if (grassRoot == null || grassRoot.transform.childCount == 0)
        {
            var grassMat = AssetMaterialSetup.SetupGrassMaterial();
            SetupRealisticGrassFields(grassMat);
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == "--- GRASS FIELDS ---") grassRoot = r;
            }
        }

        int activatedCount = 0;

        // 1. Pastikan container utama aktif
        if (grassRoot != null)
        {
            grassRoot.SetActive(true);

            // Cari --- 3D WILDFLOWERS --- jika ada di dalam grassRoot atau di dalam salah satu Chunk
            if (flowerRoot == null)
            {
                var fChild = grassRoot.transform.Find("--- 3D WILDFLOWERS ---");
                if (fChild != null) flowerRoot = fChild.gameObject;
            }

            // Periksa semua child grassRoot (chunk atau rumpun rumput)
            for (int i = 0; i < grassRoot.transform.childCount; i++)
            {
                var ch = grassRoot.transform.GetChild(i);

                // Jika --- 3D WILDFLOWERS --- tersasar masuk ke dalam Chunk, keluarkan!
                var fSub = ch.Find("--- 3D WILDFLOWERS ---");
                if (fSub != null)
                {
                    flowerRoot = fSub.gameObject;
                    flowerRoot.transform.SetParent(grassRoot.transform, true);
                    flowerRoot.SetActive(true);
                }

                if (!ch.gameObject.activeSelf)
                {
                    ch.gameObject.SetActive(true);
                    activatedCount++;
                }

                // Aktifkan juga seluruh cucunya
                for (int j = 0; j < ch.childCount; j++)
                {
                    var grandchild = ch.GetChild(j);
                    if (!grandchild.gameObject.activeSelf)
                    {
                        grandchild.gameObject.SetActive(true);
                        activatedCount++;
                    }
                }
            }
        }

        // 2. Pastikan --- 3D WILDFLOWERS --- dan semua bunganya aktif
        if (flowerRoot != null)
        {
            flowerRoot.SetActive(true);
            for (int i = 0; i < flowerRoot.transform.childCount; i++)
            {
                var fl = flowerRoot.transform.GetChild(i);
                if (!fl.gameObject.activeSelf)
                {
                    fl.gameObject.SetActive(true);
                    activatedCount++;
                }
            }
        }

        // 3. Pastikan pohon hutan keliling desa (--- WILDERNESS ---) juga aktif
        if (wildernessRoot != null)
        {
            wildernessRoot.SetActive(true);
            for (int i = 0; i < wildernessRoot.transform.childCount; i++)
            {
                var ch = wildernessRoot.transform.GetChild(i);
                if (!ch.gameObject.activeSelf)
                {
                    ch.gameObject.SetActive(true);
                    activatedCount++;
                }
            }
        }

        // 4. Pastikan Jungle Biome juga aktif
        if (jungleRoot != null)
        {
            jungleRoot.SetActive(true);
        }

        // 5. Daftarkan dan sinkronkan ke ChunkRenderDistance
        var crd = ChunkRenderDistance.Instance ?? Object.FindObjectOfType<ChunkRenderDistance>(true);
        if (crd != null)
        {
            crd.AutoDiscoverChunks();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Eldara] Rumput & Bunga Desa berhasil dipulihkan! ({activatedCount} objek diaktifkan kembali).");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Rumput & Bunga Desa Pulih!",
                $"Semua rumput dan bunga 3D di desa berhasil diaktifkan kembali!\n" +
                $"Sebanyak {activatedCount} objek/petak telah dihidupkan.\n\n" +
                "Tekan Ctrl + S untuk menyimpan scene.", "Alhamdulillah, Mantap!");
        }
    }
}
#endif
