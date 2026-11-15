using System;
using System.Collections;
using UnityEngine;

namespace Eldara
{
    /// <summary>
    /// Mengatur spawn awal pemain dan respawn saat pemain mati.
    /// Dari skrip kesehatan/nyawa, panggil: SpawnManager.Instance.PemainMati();
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [Header("Pemain")]
        [Tooltip("Kosongkan jika pemain sudah ada di scene (dicari lewat tag).")]
        public GameObject prefabPemain;
        public Transform pemain;
        public string tagPemain = "Player";

        [Header("Titik spawn")]
        public SpawnPoint spawnAwal;
        public SpawnPoint spawnRespawn;

        [Header("Respawn")]
        public float jedaRespawn = 1.5f;
        [Tooltip("Jika pemain jatuh di bawah Y ini, dianggap mati.")]
        public float batasJatuhY = -30f;
        public bool pakaiCheckpoint = true;

        public event Action<Transform> SaatSpawnAwal;
        public event Action<Transform> SaatMati;
        public event Action<Transform> SaatRespawn;

        SpawnPoint checkpointAktif;
        bool sedangRespawn;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            SpawnAwalPemain();

            // Otomatis dengarkan kematian dari komponen Health jika ada
            if (pemain != null)
            {
                var h = pemain.GetComponent<Health>();
                if (h != null)
                {
                    h.OnDied += _ => PemainMati();
                }
            }
        }

        void Update()
        {
            if (pemain != null && !sedangRespawn && pemain.position.y < batasJatuhY)
                PemainMati();
        }

        public void SpawnAwalPemain()
        {
            if (!SiapkanPemain()) return;
            if (spawnAwal == null) { Debug.LogWarning("[SpawnManager] Spawn awal belum diatur."); return; }

            Pindahkan(spawnAwal);
            checkpointAktif = null;
            SaatSpawnAwal?.Invoke(pemain);
        }

        public void SetCheckpoint(SpawnPoint titik)
        {
            if (pakaiCheckpoint) checkpointAktif = titik;
        }

        public void PemainMati()
        {
            if (sedangRespawn) return;
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            sedangRespawn = true;
            SaatMati?.Invoke(pemain);

            yield return new WaitForSeconds(jedaRespawn);

            SpawnPoint tujuan = (pakaiCheckpoint && checkpointAktif != null)
                ? checkpointAktif
                : (spawnRespawn != null ? spawnRespawn : spawnAwal);

            if (tujuan != null && pemain != null)
            {
                Pindahkan(tujuan);

                // Pulihkan darah jika ada komponen Health
                var h = pemain.GetComponent<Health>();
                if (h != null)
                {
                    h.Heal(h.max);
                }

                SaatRespawn?.Invoke(pemain);
            }
            else
            {
                Debug.LogWarning("[SpawnManager] Titik respawn belum diatur.");
            }

            sedangRespawn = false;
        }

        bool SiapkanPemain()
        {
            if (pemain != null) return true;

            GameObject ada = null;
            try { ada = GameObject.FindWithTag(tagPemain); } catch (UnityException) { }

            if (ada != null) { pemain = ada.transform; return true; }

            if (prefabPemain != null && spawnAwal != null)
            {
                pemain = Instantiate(prefabPemain, spawnAwal.PosisiSpawn, spawnAwal.RotasiSpawn).transform;
                return true;
            }

            Debug.LogWarning("[SpawnManager] Pemain tidak ditemukan. Beri tag 'Player' atau isi prefabPemain.");
            return false;
        }

        void Pindahkan(SpawnPoint titik)
        {
            var cc = pemain.GetComponent<CharacterController>();
            var rb = pemain.GetComponent<Rigidbody>();

            if (cc != null) cc.enabled = false;

            pemain.SetPositionAndRotation(titik.PosisiSpawn, titik.RotasiSpawn);

            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }

            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }
    }
}
