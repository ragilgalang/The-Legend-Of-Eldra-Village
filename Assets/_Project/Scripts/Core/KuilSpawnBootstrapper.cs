using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eldara
{
    /// <summary>
    /// Otomatis membangun Kuil Kebangkitan (SpawnUtama) di luar gerbang selatan desa
    /// dan memindahkan posisi awal pemain ke dalam kuil saat petualangan dimulai.
    /// </summary>
    public static class KuilSpawnBootstrapper
    {
        const float TinggiLantai = 0.5f;
        static readonly Vector3 PosisiKuil = new Vector3(0f, 0f, -90f);
        static readonly Quaternion RotasiKuil = Quaternion.identity; // Menghadap Utara (+Z, ke arah desa)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InisialisasiSpawn()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.ToLower().Contains("mainmenu")) return;

            // Cari atau buat Kuil Kebangkitan
            var sp = SetupKuilKebangkitan();

            // Pastikan SpawnManager ada di scene
            var manager = Object.FindFirstObjectByType<SpawnManager>();
            if (manager == null)
            {
                var mgrGo = new GameObject("SpawnManager");
                manager = mgrGo.AddComponent<SpawnManager>();
            }

            if (sp != null)
            {
                manager.spawnAwal = sp;
                manager.spawnRespawn = sp;
            }

            // Posisikan pemain tepat di dalam Kuil Kebangkitan
            PosisikanPemainKeSpawn(sp);
        }

        public static SpawnPoint SetupKuilKebangkitan()
        {
            var utamaGo = GameObject.Find("SpawnUtama");
            if (utamaGo != null)
            {
                var spExisting = utamaGo.GetComponent<SpawnPoint>();
                if (spExisting == null) spExisting = utamaGo.AddComponent<SpawnPoint>();
                spExisting.offsetSpawnY = TinggiLantai + 0.05f;
                spExisting.offsetSpawnZ = 0.5f;
                return spExisting;
            }

            // Cari atau buat root SpawnPoints
            var parentGo = GameObject.Find("SpawnPoints");
            if (parentGo == null) parentGo = new GameObject("SpawnPoints");

            // Siapkan Material
            var matBatu = AmbilAtauBuatMaterial("Spawn_Batu", new Color(0.55f, 0.55f, 0.52f), Color.black);
            var matAtap = AmbilAtauBuatMaterial("Spawn_AtapGelap", new Color(0.20f, 0.22f, 0.30f), Color.black);
            var coral = new Color(1f, 0.45f, 0.25f);
            var matCoral = AmbilAtauBuatMaterial("Spawn_GlowCoral", coral, coral * 2.5f);

            // Buat Root SpawnUtama
            utamaGo = new GameObject("SpawnUtama");
            utamaGo.transform.SetParent(parentGo.transform);
            utamaGo.transform.SetPositionAndRotation(PosisiKuil, RotasiKuil);

            var sp = utamaGo.AddComponent<SpawnPoint>();
            sp.jenis = JenisSpawn.Gabungan;
            sp.offsetSpawnY = TinggiLantai + 0.05f;
            sp.offsetSpawnZ = 0.5f;

            Transform r = utamaGo.transform;

            // 1. Fondasi Silinder
            var fondasi = BuatBagian(PrimitiveType.Cylinder, "Fondasi", r, matBatu, new Vector3(0, -0.25f, 0), new Vector3(9f, 0.75f, 9f), true);
            var colCyl = fondasi.GetComponent<Collider>();
            if (colCyl != null) Object.Destroy(colCyl);
            fondasi.AddComponent<MeshCollider>();

            // 2. Tangga Depan
            BuatTangga(r, matBatu, 4.4f, 3);

            // 3. Cincin Glow & Lantai Dalam
            BuatBagian(PrimitiveType.Cylinder, "CincinGlow", r, matCoral, new Vector3(0, TinggiLantai + 0.005f, 0), new Vector3(6.4f, 0.01f, 6.4f), false);
            BuatBagian(PrimitiveType.Cylinder, "LantaiDalam", r, matBatu, new Vector3(0, TinggiLantai + 0.015f, 0), new Vector3(5.6f, 0.01f, 5.6f), false);

            // 4. Tiang-tiang Batu
            const int jumlahTiang = 6;
            const float radiusTiang = 3.6f;
            var titik = new Vector3[jumlahTiang];
            for (int i = 0; i < jumlahTiang; i++)
            {
                float a = i * 360f / jumlahTiang * Mathf.Deg2Rad;
                titik[i] = new Vector3(Mathf.Cos(a) * radiusTiang, 0, Mathf.Sin(a) * radiusTiang);
                BuatBagian(PrimitiveType.Cylinder, "Tiang", r, matBatu,
                    new Vector3(titik[i].x, 2.25f, titik[i].z), new Vector3(0.5f, 1.75f, 0.5f), true);
            }

            // 5. Balok Penghubung Tiang
            for (int i = 0; i < jumlahTiang; i++)
            {
                Vector3 a = titik[i];
                Vector3 b = titik[(i + 1) % jumlahTiang];
                Vector3 selisih = b - a;
                float panjang = selisih.magnitude;
                float sudutY = -Mathf.Atan2(selisih.z, selisih.x) * Mathf.Rad2Deg;
                Vector3 tengah = (a + b) * 0.5f;
                BuatBagian(PrimitiveType.Cube, "Balok", r, matBatu,
                    new Vector3(tengah.x, 4.15f, tengah.z), new Vector3(panjang + 0.2f, 0.3f, 0.4f),
                    true, new Vector3(0, sudutY, 0));
            }

            // 6. Atap Kerucut
            BuatKerucut(r, matAtap, "Atap", 6, 5.4f, 2.2f, 0f, new Vector3(0, 4.3f, 0));
            BuatBagian(PrimitiveType.Sphere, "Puncak", r, matCoral, new Vector3(0, 6.65f, 0), new Vector3(0.35f, 0.35f, 0.35f), false);

            // 7. Altar dan Kristal Api Abadi
            BuatBagian(PrimitiveType.Cube, "AltarBawah", r, matBatu, new Vector3(0, 1.0f, -2.3f), new Vector3(1.8f, 1.0f, 1.1f), true);
            BuatBagian(PrimitiveType.Cube, "AltarAtas", r, matBatu, new Vector3(0, 1.57f, -2.3f), new Vector3(2.2f, 0.14f, 1.4f), true);
            BuatBagian(PrimitiveType.Cube, "Kristal", r, matCoral, new Vector3(0, 2.5f, -2.3f),
                new Vector3(0.6f, 0.6f, 0.6f), false, new Vector3(45f, 45f, 0f));

            // Cahaya Hangat Altar
            var lightGo = new GameObject("CahayaAltar");
            lightGo.transform.SetParent(r, false);
            lightGo.transform.localPosition = new Vector3(0, 2.5f, -2.3f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = coral;
            l.range = 9f;
            l.intensity = 3f;

            // 8. Lentera Gerbang Kuil
            for (int sx = -1; sx <= 1; sx += 2)
            {
                BuatBagian(PrimitiveType.Cube, "TiangLentera", r, matBatu, new Vector3(sx * 2.3f, 1.0f, 4.0f), new Vector3(0.5f, 1.0f, 0.5f), true);
                BuatBagian(PrimitiveType.Cube, "Api", r, matCoral, new Vector3(sx * 2.3f, 1.65f, 4.0f), new Vector3(0.3f, 0.3f, 0.3f), false);
            }

            Debug.Log("[Eldara] Kuil Kebangkitan (SpawnUtama) berhasil dibangun di pintu masuk desa.");
            return sp;
        }

        public static void PosisikanPemainKeSpawn(SpawnPoint sp)
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null)
            {
                var pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc != null) playerGo = pc.gameObject;
            }

            if (playerGo != null && sp != null)
            {
                var cc = playerGo.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                var rb = playerGo.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

                playerGo.transform.SetPositionAndRotation(sp.PosisiSpawn, sp.RotasiSpawn);
                Physics.SyncTransforms();

                if (cc != null) cc.enabled = true;
                Debug.Log($"[Eldara] Pemain diposisikan di Bangunan Spawn ({sp.PosisiSpawn}).");
            }
        }

        static GameObject BuatBagian(PrimitiveType type, string nama, Transform induk, Material mat,
            Vector3 pos, Vector3 skala, bool adaCollider = true, Vector3 rotasi = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = nama;
            go.transform.SetParent(induk, false);
            go.transform.localPosition = pos;
            go.transform.localScale = skala;
            if (rotasi != default) go.transform.localRotation = Quaternion.Euler(rotasi);

            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            if (!adaCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }

            return go;
        }

        static void BuatTangga(Transform induk, Material mat, float offsetZ, int jumlahAnakTangga)
        {
            float lebar = 2.4f;
            float tinggiTotal = TinggiLantai;
            float kedalamanAnak = 0.5f;
            float tinggiAnak = tinggiTotal / jumlahAnakTangga;

            for (int i = 0; i < jumlahAnakTangga; i++)
            {
                float z = offsetZ + i * kedalamanAnak;
                float y = (i + 0.5f) * tinggiAnak;
                float tinggiBalok = (jumlahAnakTangga - i) * tinggiAnak;
                float yBalok = tinggiTotal - tinggiBalok * 0.5f;

                BuatBagian(PrimitiveType.Cube, $"AnakTangga_{i}", induk, mat,
                    new Vector3(0, yBalok, z), new Vector3(lebar, tinggiBalok, kedalamanAnak), true);
            }
        }

        static void BuatKerucut(Transform induk, Material mat, string nama, int sisi, float radiusBawah, float tinggi, float putaranDeg, Vector3 pos)
        {
            var mesh = new Mesh { name = nama + "Mesh" };
            var v = new System.Collections.Generic.List<Vector3>();
            var t = new System.Collections.Generic.List<int>();

            Vector3 puncak = new Vector3(0, tinggi, 0);
            var alas = new Vector3[sisi];
            for (int i = 0; i < sisi; i++)
            {
                float a = (i * 360f / sisi + putaranDeg) * Mathf.Deg2Rad;
                alas[i] = new Vector3(Mathf.Cos(a) * radiusBawah, 0, Mathf.Sin(a) * radiusBawah);
            }

            // Sisi tegak
            for (int i = 0; i < sisi; i++)
            {
                Vector3 p0 = alas[i];
                Vector3 p1 = alas[(i + 1) % sisi];
                int idx = v.Count;
                v.Add(puncak); v.Add(p0); v.Add(p1);
                t.Add(idx); t.Add(idx + 1); t.Add(idx + 2);
            }

            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static Material AmbilAtauBuatMaterial(string nama, Color warna, Color emisi)
        {
            // Coba cari material yang sudah ada di memory
            var allMats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var m in allMats)
            {
                if (m != null && m.name == nama) return m;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = nama };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", warna);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", warna);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);

            if (emisi != Color.black && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emisi);
            }

            return mat;
        }
    }
}
