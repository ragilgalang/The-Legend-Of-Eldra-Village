#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eldara.EditorTools
{
    public static class SpawnSetupTool
    {
        const string FolderRoot = "Assets/Eldara";
        const string FolderMat = "Assets/Eldara/Materials";

        [MenuItem("Eldara/Buat Spawn Point")]
        public static void Buat()
        {
#if UNITY_2023_1_OR_NEWER
            var manager = Object.FindFirstObjectByType<SpawnManager>();
#else
            var manager = Object.FindObjectOfType<SpawnManager>();
#endif
            if (manager == null)
            {
                var go = new GameObject("SpawnManager");
                Undo.RegisterCreatedObjectUndo(go, "Buat SpawnManager");
                manager = go.AddComponent<SpawnManager>();
            }

            var induk = GameObject.Find("SpawnPoints");
            if (induk == null)
            {
                induk = new GameObject("SpawnPoints");
                Undo.RegisterCreatedObjectUndo(induk, "Buat SpawnPoints");
            }

            // Bersihkan marker lama jika sudah ada di dalam SpawnPoints agar tidak dobel
            var oldAwal = induk.transform.Find("SpawnAwal");
            if (oldAwal != null) Object.DestroyImmediate(oldAwal.gameObject);
            var oldRespawn = induk.transform.Find("SpawnRespawn");
            if (oldRespawn != null) Object.DestroyImmediate(oldRespawn.gameObject);

            Vector3 pusat = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;

            Vector3 posAwal = KeTanah(pusat);
            Vector3 posRespawn = KeTanah(pusat + Vector3.right * 8f);

            var awal = BuatMarker("SpawnAwal", posAwal, new Color(0.2f, 0.85f, 0.55f), JenisSpawn.Awal, induk.transform);
            var respawn = BuatMarker("SpawnRespawn", posRespawn, new Color(1f, 0.5f, 0.3f), JenisSpawn.Respawn, induk.transform);

            Undo.RecordObject(manager, "Atur SpawnManager");
            manager.spawnAwal = awal;
            manager.spawnRespawn = respawn;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = awal.gameObject;

            Debug.Log("[Eldara] Spawn awal & respawn dibuat. Geser di Scene view sesuai kebutuhan, lalu pastikan pemain ber-tag 'Player'.");

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Spawn Point Berhasil Dibuat!",
                    "Objek SpawnManager dan 2 marker (SpawnAwal & SpawnRespawn) telah dipasang di scene.\n\n" +
                    "• Marker Hijau: Titik Spawn Awal pemain.\n" +
                    "• Marker Coral: Titik Respawn saat pemain mati.\n\n" +
                    "Silakan geser posisi marker di Scene View sesuai keinginanmu, lalu simpan scene (Ctrl+S).",
                    "Siap!");
            }
        }

        static Vector3 KeTanah(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 100f, Vector3.down, out var hit, 500f))
                return hit.point;
            return p;
        }

        static SpawnPoint BuatMarker(string nama, Vector3 pos, Color warna, JenisSpawn jenis, Transform induk)
        {
            var root = new GameObject(nama);
            Undo.RegisterCreatedObjectUndo(root, "Buat " + nama);
            root.transform.SetParent(induk, true);
            root.transform.position = pos;

            var sp = root.AddComponent<SpawnPoint>();
            sp.jenis = jenis;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = sp.radius;
            trigger.center = Vector3.up;

            var mat = BuatMaterial(nama, warna);

            var alas = Anak(PrimitiveType.Cylinder, "Alas", root.transform, mat,
                new Vector3(0f, 0.1f, 0f), new Vector3(3f, 0.1f, 3f), true);

            Anak(PrimitiveType.Cylinder, "Tiang", root.transform, mat,
                new Vector3(0f, 1.6f, 0f), new Vector3(0.15f, 1.5f, 0.15f), false);

            Anak(PrimitiveType.Sphere, "Cahaya", root.transform, mat,
                new Vector3(0f, 3.1f, 0f), new Vector3(0.45f, 0.45f, 0.45f), false);

            Anak(PrimitiveType.Cube, "Panah", root.transform, mat,
                new Vector3(0f, 0.22f, 1.1f), new Vector3(0.35f, 0.04f, 0.8f), false);

            var lampu = new GameObject("Lampu");
            lampu.transform.SetParent(root.transform, false);
            lampu.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            var l = lampu.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = warna;
            l.range = 8f;
            l.intensity = 2f;

            return sp;
        }

        static GameObject Anak(PrimitiveType tipe, string nama, Transform induk, Material mat,
            Vector3 lokalPos, Vector3 skala, bool simpanCollider)
        {
            var o = GameObject.CreatePrimitive(tipe);
            o.name = nama;
            o.transform.SetParent(induk, false);
            o.transform.localPosition = lokalPos;
            o.transform.localScale = skala;
            o.GetComponent<Renderer>().sharedMaterial = mat;

            if (!simpanCollider)
            {
                var c = o.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            return o;
        }

        static Material BuatMaterial(string nama, Color warna)
        {
            if (!AssetDatabase.IsValidFolder(FolderRoot)) AssetDatabase.CreateFolder("Assets", "Eldara");
            if (!AssetDatabase.IsValidFolder(FolderMat)) AssetDatabase.CreateFolder(FolderRoot, "Materials");

            string path = $"{FolderMat}/{nama}.mat";
            var ada = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (ada != null) return ada;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", warna);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", warna);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", warna * 0.6f);

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
#endif
