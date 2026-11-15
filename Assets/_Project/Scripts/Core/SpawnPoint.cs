using UnityEngine;

namespace Eldara
{
    public enum JenisSpawn { Awal, Respawn, Gabungan }

    /// <summary>
    /// Penanda titik spawn. Pasang di GameObject kosong, atur jenisnya,
    /// lalu daftarkan ke SpawnManager.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnPoint : MonoBehaviour
    {
        public JenisSpawn jenis = JenisSpawn.Awal;

        [Tooltip("Jika aktif (jenis Respawn), pemain yang masuk area ini menjadikan titik ini checkpoint.")]
        public bool jadiCheckpoint = false;

        public float radius = 2f;

        [Tooltip("Tinggi titik muncul pemain di atas posisi objek (lantai bangunan + ruang CharacterController).")]
        public float offsetSpawnY = 1.55f;

        [Tooltip("Offset maju/mundur titik muncul pemain (ke arah tangga/depan).")]
        public float offsetSpawnZ = 0.5f;

        public Vector3 PosisiSpawn => transform.position + transform.rotation * new Vector3(0f, offsetSpawnY, offsetSpawnZ);
        public Quaternion RotasiSpawn => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        void OnTriggerEnter(Collider other)
        {
            if (!jadiCheckpoint || jenis == JenisSpawn.Awal) return;
            if (!other.CompareTag("Player")) return;
            if (SpawnManager.Instance != null) SpawnManager.Instance.SetCheckpoint(this);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Color warna = jenis == JenisSpawn.Awal
                ? new Color(0.2f, 0.85f, 0.55f)
                : jenis == JenisSpawn.Gabungan
                    ? new Color(1f, 0.8f, 0.3f)
                    : new Color(1f, 0.5f, 0.3f);

            Gizmos.color = warna;
            Vector3 p = PosisiSpawn;

            UnityEditor.Handles.color = warna;
            UnityEditor.Handles.DrawWireDisc(p - Vector3.up * 0.95f, Vector3.up, radius);

            Gizmos.DrawLine(p, p + Vector3.up * 2f);
            Gizmos.DrawWireSphere(p + Vector3.up * 2f, 0.25f);

            Vector3 depan = transform.forward * radius;
            Gizmos.DrawLine(p + Vector3.up * 0.1f, p + Vector3.up * 0.1f + depan);
            Gizmos.DrawLine(p + Vector3.up * 0.1f + depan,
                p + Vector3.up * 0.1f + depan * 0.75f + transform.right * 0.3f);
            Gizmos.DrawLine(p + Vector3.up * 0.1f + depan,
                p + Vector3.up * 0.1f + depan * 0.75f - transform.right * 0.3f);

            string label = jenis == JenisSpawn.Gabungan ? "SPAWN AWAL & RESPAWN"
                : jenis == JenisSpawn.Awal ? "SPAWN AWAL"
                : (jadiCheckpoint ? "CHECKPOINT" : "RESPAWN");
            UnityEditor.Handles.Label(p + Vector3.up * 2.4f, label);
        }
#endif
    }
}
