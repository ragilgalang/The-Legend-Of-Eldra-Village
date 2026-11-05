using UnityEngine;

/// <summary>
/// Sistem Animasi Angin Organik untuk Pepohonan dan Semak Desa Eldara.
/// Menggerakkan batang dan tajuk pohon mengikuti hembusan angin dinamis:
/// - Ayunan batang utama berfrekuensi rendah (terasa berat dan kokoh)
/// - Riak dedaunan tajuk berfrekuensi tinggi (fluttering leaves)
/// - Offset fase spasial acak per pohon (pohon tidak bergoyang serempak seperti robot)
/// - Efek hembusan angin (wind gusts) berkala
/// - Animasi "napas" canopy saat hembusan kuat (scale pulsate)
/// NOTE: Particle system dihapus untuk menghindari error "Velocity curves must be in same mode"
/// </summary>
public class TreeWindSway : MonoBehaviour
{
    [Header("Parameter Angin")]
    [Tooltip("Arah hembusan angin utama")]
    public Vector3 windDirection = new Vector3(1f, 0f, 0.25f);

    [Tooltip("Kekuatan ayunan angin (derajat kemiringan)")]
    [Range(0.5f, 6.0f)]
    public float swayIntensity = 2.4f;

    [Tooltip("Kecepatan ayunan angin")]
    [Range(0.2f, 3.0f)]
    public float swaySpeed = 1.1f;

    // Field ini dipertahankan agar tidak breaking change serialisasi lama,
    // tapi particle system tidak lagi dibuat (hapus error 999+ velocity curves)
    [HideInInspector]
    public bool enableFallingLeaves = false;

    private Quaternion baseRotation;
    private float spatialPhase;
    private float gustPhase;
    private Transform[] childBranches;
    private Quaternion[] initialChildRotations;
    private Transform canopyTransform;
    private Vector3 canopyBaseScale;

    void Awake()
    {
        baseRotation = transform.localRotation;

        // Hitung offset fase spasial berdasarkan koordinat dunia agar tiap pohon bergoyang dengan irama unik
        spatialPhase = (transform.position.x * 0.173f + transform.position.z * 0.269f);
        gustPhase = Random.Range(0f, 100f);

        // Cache child transforms untuk animasi sekunder dedaunan/cabang
        int childCount = transform.childCount;
        if (childCount > 0)
        {
            childBranches = new Transform[childCount];
            initialChildRotations = new Quaternion[childCount];
            for (int i = 0; i < childCount; i++)
            {
                childBranches[i] = transform.GetChild(i);
                initialChildRotations[i] = childBranches[i].localRotation;

                // Cache canopy untuk animasi skala "napas"
                if (childBranches[i] != null && childBranches[i].name == "Canopy_Foliage")
                {
                    canopyTransform = childBranches[i];
                    canopyBaseScale = canopyTransform.localScale;
                }
            }
        }
    }

    void Update()
    {
        float time = Time.time * swaySpeed;

        // 1. Variasi Hembusan Angin Dinamis (Wind Gusts) menggunakan kombinasi trigonometri
        float gust = Mathf.Sin(time * 0.35f + gustPhase) * 0.5f + 0.5f;
        gust = gust * gust; // Hembusan datang berkala secara natural

        // 2. Ayunan Batang Utama (Low Frequency Sway)
        float primaryWave = Mathf.Sin(time * 1.2f + spatialPhase);
        float secondaryWave = Mathf.Sin(time * 2.1f + spatialPhase * 1.5f) * 0.35f;
        float currentAngle = (primaryWave + secondaryWave + gust * 0.8f) * swayIntensity;

        // Vektor sumbu ayunan tegak lurus dengan arah angin
        Vector3 windNorm = windDirection.sqrMagnitude > 0.001f ? windDirection.normalized : Vector3.right;
        Vector3 tiltAxis = Vector3.Cross(windNorm, Vector3.up);

        // Sedikit puntiran (torsion) saat ditiup angin
        float torsionAngle = Mathf.Cos(time * 0.8f + spatialPhase) * (swayIntensity * 0.25f);

        Quaternion swayRotation = Quaternion.AngleAxis(currentAngle, tiltAxis) 
                                * Quaternion.AngleAxis(torsionAngle, windNorm);

        transform.localRotation = baseRotation * swayRotation;

        // 3. Animasi Riak Sekunder Tajuk / Dedaunan (Child Leaves Flutter)
        if (childBranches != null)
        {
            for (int i = 0; i < childBranches.Length; i++)
            {
                if (!childBranches[i]) continue;
                if (childBranches[i].GetComponent<ParticleSystem>()) continue;

                float branchPhase = spatialPhase + (i * 1.35f);
                float flutterAngle = Mathf.Sin(time * 3.2f + branchPhase) * (swayIntensity * 0.45f);
                float flutterTilt = Mathf.Cos(time * 2.7f + branchPhase) * (swayIntensity * 0.3f);

                Quaternion branchSway = Quaternion.AngleAxis(flutterAngle, tiltAxis)
                                      * Quaternion.AngleAxis(flutterTilt, Vector3.up);

                childBranches[i].localRotation = initialChildRotations[i] * branchSway;
            }
        }

        // 4. Animasi "Napas" Canopy — scale pulsate saat hembusan angin kuat
        // Efek ini menyimulasikan gerakan massa dedaunan yang membuka-menutup seperti bernapas
        if (canopyTransform != null)
        {
            float breathe = 1.0f + Mathf.Sin(time * 1.8f + spatialPhase * 0.7f) * 0.022f * (1f + gust);
            canopyTransform.localScale = canopyBaseScale * breathe;
        }
    }
}
