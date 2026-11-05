using UnityEngine;

/// <summary>
/// Sistem Animasi Angin Organik untuk Pepohonan dan Semak Desa Eldara.
/// Menggerakkan batang dan tajuk pohon mengikuti hembusan angin dinamis:
/// - Ayunan batang utama berfrekuensi rendah (terasa berat dan kokoh)
/// - Riak dedaunan tajuk berfrekuensi tinggi (fluttering leaves)
/// - Offset fase spasial acak per pohon (pohon tidak bergoyang serempak seperti robot)
/// - Efek hembusan angin (wind gusts) berkala
/// - Partikel guguran dedaunan lembut di bawah pohon desa utama
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

    [Tooltip("Apakah pohon ini memunculkan efek guguran daun lembut?")]
    public bool enableFallingLeaves = false;

    private Quaternion baseRotation;
    private float spatialPhase;
    private float gustPhase;
    private Transform[] childBranches;
    private Quaternion[] initialChildRotations;

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
            }
        }
    }

    void Start()
    {
        if (enableFallingLeaves)
        {
            SetupFallingLeavesParticle();
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
    }

    void SetupFallingLeavesParticle()
    {
        var psGo = new GameObject("FallingLeaves_FX");
        psGo.transform.SetParent(transform, false);
        psGo.transform.localPosition = new Vector3(0, 7f, 0);

        var ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.42f, 0.78f, 0.22f, 0.85f),
            new Color(0.88f, 0.72f, 0.25f, 0.85f)
        );
        main.gravityModifier = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = 3.0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 4.0f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        var leafShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                         ?? Shader.Find("Particles/Standard Unlit") 
                         ?? Shader.Find("Universal Render Pipeline/Lit");
        if (leafShader)
        {
            renderer.sharedMaterial = new Material(leafShader);
        }
    }
}
