using UnityEngine;

/// <summary>
/// Animasi riak angin dinamis untuk rumpun rumput 3D Desa Eldara.
/// Mengayunkan rumput dan bunga liar mengikuti hembusan angin secara organik.
/// </summary>
public class GrassWindSway : MonoBehaviour
{
    [Header("Parameter Angin")]
    public Vector3 windDirection = new Vector3(1f, 0f, 0.25f);
    public float swayIntensity = 3.5f;
    public float swaySpeed = 1.8f;

    private Quaternion baseRotation;
    private float phaseOffset;
    private float gustPhase;

    void Awake()
    {
        baseRotation = transform.localRotation;
        phaseOffset = transform.position.x * 0.35f + transform.position.z * 0.42f;
        gustPhase = Random.Range(0f, 50f);
    }

    void Update()
    {
        float time = Time.time * swaySpeed;

        // Gelombang angin utama + hembusan kencang
        float gust = Mathf.Sin(time * 0.4f + gustPhase);
        gust = (gust > 0.4f) ? (gust - 0.4f) * 1.5f : 0f;

        float wave1 = Mathf.Sin(time * 1.5f + phaseOffset);
        float wave2 = Mathf.Sin(time * 2.8f + phaseOffset * 1.3f) * 0.35f;

        float currentAngle = (wave1 + wave2 + gust) * swayIntensity;

        Vector3 windNorm = windDirection.sqrMagnitude > 0.001f ? windDirection.normalized : Vector3.right;
        Vector3 tiltAxis = Vector3.Cross(windNorm, Vector3.up);

        transform.localRotation = baseRotation * Quaternion.AngleAxis(currentAngle, tiltAxis);
    }
}

