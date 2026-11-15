#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eldara.EditorTools
{
    /// <summary>
    /// Membangun satu bangunan spawn (Kuil Kebangkitan) yang dipakai untuk
    /// spawn awal sekaligus respawn saat mati, di luar desa dan menghadap desa.
    /// Menu: Eldara > Buat Bangunan Spawn
    /// </summary>
    public static class SpawnSetupTool
    {
        const string FolderRoot = "Assets/Eldara";
        const string FolderMat = "Assets/Eldara/Materials";
        const float TinggiLantai = 0.5f;

        static Material batu, atapGelap, glowCoral;

        [MenuItem("Eldara/Buat Bangunan Spawn")]
        public static void BukaJendela()
        {
            EditorWindow.GetWindow<SpawnSetupWindow>(true, "Bangunan Spawn");
        }

        public static void Buat(float jarakDariDesa = 40f, float arahDeg = 180f, bool paksaBuatUlang = false)
        {
            CariDesa(out Vector3 pusatDesa, out float radiusDesa);

            float a = arahDeg * Mathf.Deg2Rad;
            Vector3 arah = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));

            var utamaGo = GameObject.Find("SpawnUtama");
            if (paksaBuatUlang && utamaGo != null)
            {
                Undo.DestroyObjectImmediate(utamaGo);
                utamaGo = null;
            }

            if (utamaGo == null)
            {
                HapusJikaAda("SpawnAwal");
                HapusJikaAda("SpawnRespawn");
            }

            Vector3 pos = KeTanah(pusatDesa + arah * (radiusDesa + jarakDariDesa));
            Quaternion rot = HadapKe(pos, pusatDesa);

            if (utamaGo != null)
            {
                Undo.RecordObject(utamaGo.transform, "Pindahkan spawn");
                utamaGo.transform.SetPositionAndRotation(pos, rot);

                // Perbarui offset komponen SpawnPoint jika masih nilai lama
                var spLama = utamaGo.GetComponent<SpawnPoint>();
                if (spLama != null)
                {
                    spLama.offsetSpawnY = TinggiLantai + 1.05f;
                    spLama.offsetSpawnZ = 0.5f;
                    EditorUtility.SetDirty(spLama);
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log($"[Eldara] Kuil spawn dipindah ke {jarakDariDesa:0} m dari tepi desa.");
                return;
            }

            SiapkanMaterial();

            var manager = Object.FindObjectOfType<SpawnManager>(true);
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

            var utama = BuatKuil(pos, rot, induk.transform);

            Undo.RecordObject(manager, "Atur SpawnManager");
            manager.spawnAwal = utama;
            manager.spawnRespawn = utama;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = utama.gameObject;

            Debug.Log($"[Eldara] Kuil spawn dibuat {jarakDariDesa:0} m dari tepi desa, menghadap desa. Pemain harus ber-tag 'Player'.");
        }

        static void CariDesa(out Vector3 pusat, out float radius)
        {
            pusat = Vector3.zero;
            radius = 0f;

            Transform akar = null;
            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "DesaGenerator") { akar = mb.transform; break; }
            }
            if (akar == null)
            {
                foreach (var n in new[] { "Desa", "DesaEldara", "Desa Eldara", "Village" })
                {
                    var g = GameObject.Find(n);
                    if (g != null) { akar = g.transform; break; }
                }
            }

            Bounds b = new Bounds();
            bool ada = false;

            if (akar != null)
            {
                foreach (var r in akar.GetComponentsInChildren<Renderer>())
                {
                    if (!ada) { b = r.bounds; ada = true; } else b.Encapsulate(r.bounds);
                }
                if (!ada) { pusat = akar.position; return; }
            }
            else
            {
                foreach (var r in Object.FindObjectsOfType<Renderer>())
                {
                    if (r.GetComponentInParent<SpawnPoint>() != null) continue;
                    if (r.bounds.size.x > 150f || r.bounds.size.z > 150f) continue;
                    if (!ada) { b = r.bounds; ada = true; } else b.Encapsulate(r.bounds);
                }
                if (!ada) return;
                Debug.LogWarning("[Eldara] Objek desa tidak ditemukan; pusat desa dihitung dari semua objek di scene.");
            }

            pusat = b.center;
            pusat.y = 0f; // Ketinggian dasar desa selalu di bidang datar Y = 0
            radius = Mathf.Max(b.extents.x, b.extents.z);
        }

        static Quaternion HadapKe(Vector3 dari, Vector3 target)
        {
            Vector3 d = target - dari;
            d.y = 0f;
            return d.sqrMagnitude < 0.01f ? Quaternion.identity : Quaternion.LookRotation(d.normalized);
        }

        // ------------------------------------------------------------------
        // Kuil Kebangkitan: spawn awal sekaligus respawn saat mati
        // ------------------------------------------------------------------
        static SpawnPoint BuatKuil(Vector3 pos, Quaternion rot, Transform induk)
        {
            var sp = SiapkanRoot("SpawnUtama", pos, rot, JenisSpawn.Gabungan, induk);
            var r = sp.transform;

            // Fondasi: pakai MeshCollider agar berupa silinder pipih sejati,
            // BUKAN CapsuleCollider bawaan Unity yang melengkung 4.5m ke atas menembus atap!
            var fondasi = Bagian(PrimitiveType.Cylinder, "Fondasi", r, batu, new Vector3(0, -0.25f, 0), new Vector3(9f, 0.75f, 9f), false);
            fondasi.AddComponent<MeshCollider>();

            Tangga(r, batu, 4.4f, 3);
            Bagian(PrimitiveType.Cylinder, "CincinGlow", r, glowCoral,
                new Vector3(0, TinggiLantai + 0.005f, 0), new Vector3(6.4f, 0.01f, 6.4f), false);
            Bagian(PrimitiveType.Cylinder, "LantaiDalam", r, batu,
                new Vector3(0, TinggiLantai + 0.015f, 0), new Vector3(5.6f, 0.01f, 5.6f), false);

            const int jumlahTiang = 6;
            const float radiusTiang = 3.6f;
            var titik = new Vector3[jumlahTiang];
            for (int i = 0; i < jumlahTiang; i++)
            {
                float a = i * 360f / jumlahTiang * Mathf.Deg2Rad;
                titik[i] = new Vector3(Mathf.Cos(a) * radiusTiang, 0, Mathf.Sin(a) * radiusTiang);
                Bagian(PrimitiveType.Cylinder, "Tiang", r, batu,
                    new Vector3(titik[i].x, 2.25f, titik[i].z), new Vector3(0.5f, 1.75f, 0.5f));
            }

            for (int i = 0; i < jumlahTiang; i++)
            {
                Vector3 a = titik[i];
                Vector3 b = titik[(i + 1) % jumlahTiang];
                Vector3 selisih = b - a;
                float panjang = selisih.magnitude;
                float sudutY = -Mathf.Atan2(selisih.z, selisih.x) * Mathf.Rad2Deg;
                Vector3 tengah = (a + b) * 0.5f;
                Bagian(PrimitiveType.Cube, "Balok", r, batu,
                    new Vector3(tengah.x, 4.15f, tengah.z), new Vector3(panjang + 0.2f, 0.3f, 0.4f),
                    true, new Vector3(0, sudutY, 0));
            }

            Kerucut(r, atapGelap, "Atap", 6, 5.4f, 2.2f, 0f, new Vector3(0, 4.3f, 0));
            Bagian(PrimitiveType.Sphere, "Puncak", r, glowCoral, new Vector3(0, 6.65f, 0), new Vector3(0.35f, 0.35f, 0.35f), false);

            Bagian(PrimitiveType.Cube, "AltarBawah", r, batu, new Vector3(0, 1.0f, -2.3f), new Vector3(1.8f, 1.0f, 1.1f));
            Bagian(PrimitiveType.Cube, "AltarAtas", r, batu, new Vector3(0, 1.57f, -2.3f), new Vector3(2.2f, 0.14f, 1.4f));
            Bagian(PrimitiveType.Cube, "Kristal", r, glowCoral, new Vector3(0, 2.5f, -2.3f),
                new Vector3(0.6f, 0.6f, 0.6f), false, new Vector3(45f, 45f, 0f));
            Cahaya(r, new Vector3(0, 2.5f, -2.3f), new Color(1f, 0.45f, 0.25f), 9f, 3f);

            for (int sx = -1; sx <= 1; sx += 2)
            {
                Bagian(PrimitiveType.Cube, "TiangLentera", r, batu, new Vector3(sx * 2.3f, 1.0f, 4.0f), new Vector3(0.5f, 1.0f, 0.5f));
                Bagian(PrimitiveType.Cube, "Api", r, glowCoral, new Vector3(sx * 2.3f, 1.65f, 4.0f), new Vector3(0.3f, 0.3f, 0.3f), false);
            }

            return sp;
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------
        static SpawnPoint SiapkanRoot(string nama, Vector3 pos, Quaternion rot, JenisSpawn jenis, Transform induk)
        {
            var root = new GameObject(nama);
            Undo.RegisterCreatedObjectUndo(root, "Buat " + nama);
            root.transform.SetParent(induk, true);
            root.transform.SetPositionAndRotation(pos, rot);

            var sp = root.AddComponent<SpawnPoint>();
            sp.jenis = jenis;
            sp.radius = 3f;
            sp.offsetSpawnY = TinggiLantai + 1.05f; // 0.5f + 1.05f = 1.55f (ketinggian tengah CharacterController)
            sp.offsetSpawnZ = 0.5f;                 // 0.5m maju ke arah tangga/depan

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 4f, 6f);
            trigger.center = new Vector3(0, 2.5f, 0);
            return sp;
        }

        static GameObject Bagian(PrimitiveType tipe, string nama, Transform induk, Material mat,
            Vector3 pos, Vector3 skala, bool simpanCollider = true, Vector3? euler = null)
        {
            var o = GameObject.CreatePrimitive(tipe);
            o.name = nama;
            o.transform.SetParent(induk, false);
            o.transform.localPosition = pos;
            o.transform.localScale = skala;
            if (euler.HasValue) o.transform.localRotation = Quaternion.Euler(euler.Value);
            o.GetComponent<Renderer>().sharedMaterial = mat;

            if (!simpanCollider)
            {
                var c = o.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            return o;
        }

        static void Tangga(Transform induk, Material mat, float zMulai, int jumlah)
        {
            float h = TinggiLantai / (jumlah + 1);
            for (int k = 0; k < jumlah; k++)
            {
                float top = h * (jumlah - k);
                Bagian(PrimitiveType.Cube, "Anak Tangga", induk, mat,
                    new Vector3(0, top * 0.5f, zMulai + 0.4f + k * 0.8f),
                    new Vector3(3.2f, top, 0.8f));
            }
        }

        static void Cahaya(Transform induk, Vector3 pos, Color warna, float range, float intensitas)
        {
            var go = new GameObject("Lampu");
            go.transform.SetParent(induk, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = warna;
            l.range = range;
            l.intensity = intensitas;
        }

        static void Kerucut(Transform induk, Material mat, string nama, int sisi, float radius,
            float tinggi, float rotasiDeg, Vector3 pos)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            var puncak = new Vector3(0, tinggi, 0);

            for (int i = 0; i < sisi; i++)
            {
                float a0 = (rotasiDeg + 360f * i / sisi) * Mathf.Deg2Rad;
                float a1 = (rotasiDeg + 360f * (i + 1) / sisi) * Mathf.Deg2Rad;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);

                int s = v.Count;
                v.Add(p0); v.Add(puncak); v.Add(p1);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);

                s = v.Count;
                v.Add(p0); v.Add(p1); v.Add(puncak);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }

            var mesh = new Mesh { name = nama + "Mesh" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>();
        }

        static Vector3 KeTanah(Vector3 p)
        {
            p.y = 0f;

            var t = Terrain.activeTerrain;
            if (t != null)
            {
                Vector3 tp = t.transform.position;
                Vector3 ukuran = t.terrainData.size;
                if (p.x >= tp.x && p.x <= tp.x + ukuran.x && p.z >= tp.z && p.z <= tp.z + ukuran.z)
                {
                    p.y = t.SampleHeight(p) + tp.y;
                    return p;
                }
            }

            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                p.y = ground.transform.position.y;
                return p;
            }

            if (Physics.Raycast(new Vector3(p.x, 30f, p.z), Vector3.down, out var hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.name.Contains("Tree") && !hit.collider.name.Contains("Leaf") &&
                    !hit.collider.name.Contains("Bush") && !hit.collider.name.Contains("Roof") &&
                    !hit.collider.name.Contains("Atap") && !hit.collider.name.Contains("Spawn"))
                {
                    p.y = hit.point.y;
                    return p;
                }
            }

            p.y = 0f;
            return p;
        }

        static void HapusJikaAda(string nama)
        {
            var o = GameObject.Find(nama);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

        static void SiapkanMaterial()
        {
            batu = BuatMaterial("Spawn_Batu", new Color(0.55f, 0.55f, 0.52f), Color.black);
            atapGelap = BuatMaterial("Spawn_AtapGelap", new Color(0.20f, 0.22f, 0.30f), Color.black);

            var coral = new Color(1f, 0.45f, 0.25f);
            glowCoral = BuatMaterial("Spawn_GlowCoral", coral, coral * 2f);
        }

        static Material BuatMaterial(string nama, Color warna, Color emisi)
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
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);

            if (emisi != Color.black && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emisi);
            }

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }

    public class SpawnSetupWindow : EditorWindow
    {
        float jarakDariDesa = 40f;
        float arah = 180f;

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Satu kuil (Kuil Kebangkitan) dipakai untuk spawn awal dan respawn saat mati, diletakkan di luar desa dan menghadap desa.\n" +
                "Jika sudah ada di scene, tombol di bawah memindahkannya (bangunan lama pendopo/kuil terpisah akan diganti).",
                MessageType.Info);

            jarakDariDesa = EditorGUILayout.Slider("Jarak dari tepi desa (m)", jarakDariDesa, 20f, 300f);
            arah = EditorGUILayout.Slider("Arah dari desa (derajat)", arah, 0f, 360f);
            EditorGUILayout.LabelField("Arah: 0 = utara (+Z), 90 = timur (+X), 180 = selatan (-Z, gerbang utama)", EditorStyles.miniLabel);

            GUILayout.Space(6);
            if (GUILayout.Button("Buat / Pindahkan Kuil Spawn", GUILayout.Height(30)))
                SpawnSetupTool.Buat(jarakDariDesa, arah);

            if (GUILayout.Button("Bangun Ulang Kuil dari Nol", GUILayout.Height(24)))
                SpawnSetupTool.Buat(jarakDariDesa, arah, true);
        }
    }
}
#endif
