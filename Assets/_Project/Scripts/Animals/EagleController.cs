using UnityEngine;

/// <summary>
/// Kontroler AI Elang Perkasa (Eagle) Desa Eldara.
/// Elang terbang melayang melingkar di angkasa desa (Sky Soaring Path):
/// - Radius orbit melingkar luas di atas Balai Desa dan Menara Pertahanan
/// - Dynamic banking (sayap miring alami ke dalam lingkaran belokan)
/// - Siklus mengepak (flapping) dan melayang bebas (thermal gliding)
/// - Menghadirkan atmosfer RPG alam liar yang hidup saat pemain melihat ke langit
/// </summary>
public class EagleController : MonoBehaviour
{
    [Header("Parameter Terbang")]
    public Vector3 flightCenter = new Vector3(0f, 24f, 5f);
    public float orbitRadius = 42.0f;
    public float flightSpeed = 12.0f;
    public float bankAngle = 20.0f;

    [Header("Variasi Ketinggian")]
    public float altitudeVariation = 3.5f;
    public float altitudeSpeed = 0.35f;

    private float currentAngle;
    private float flapCycleTimer;
    private bool isFlapping;
    private Transform modelTransform;
    private Quaternion baseModelRot;

    void Awake()
    {
        modelTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
        baseModelRot = modelTransform.localRotation;
        currentAngle = Random.Range(0f, 360f);
    }

    void Update()
    {
        // 1. Perbarui sudut orbit
        float angularSpeed = (flightSpeed / orbitRadius) * Mathf.Rad2Deg;
        currentAngle += angularSpeed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;

        float rad = currentAngle * Mathf.Deg2Rad;

        // 2. Hitung posisi baru di langit (orbit melingkar dengan variasi ketinggian)
        float currentY = flightCenter.y + Mathf.Sin(Time.time * altitudeSpeed) * altitudeVariation;
        Vector3 newPos = new Vector3(
            flightCenter.x + Mathf.Cos(rad) * orbitRadius,
            currentY,
            flightCenter.z + Mathf.Sin(rad) * orbitRadius
        );

        transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 8f);

        // 3. Arah terbang elang (tangen lingkaran)
        Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;

        // Sedikit pitch naik/turun sesuai variasi ketinggian
        float pitchAngle = Mathf.Cos(Time.time * altitudeSpeed) * -5.0f;

        Quaternion targetFlightRot = Quaternion.LookRotation(tangent, Vector3.up)
                                   * Quaternion.Euler(pitchAngle, 0, -bankAngle);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetFlightRot, Time.deltaTime * 4.0f);

        // 4. Siklus kepakan sayap elang (Gliding vs Flapping)
        flapCycleTimer += Time.deltaTime;
        if (!isFlapping && flapCycleTimer > 6.5f)
        {
            isFlapping = true;
            flapCycleTimer = 0f;
        }
        else if (isFlapping && flapCycleTimer > 2.5f)
        {
            isFlapping = false;
            flapCycleTimer = 0f;
        }

        // Animasi procedural kepakan elang
        if (modelTransform != null)
        {
            float flap = isFlapping ? Mathf.Sin(Time.time * 8.5f) * 0.08f : 0f;
            modelTransform.localPosition = new Vector3(0, flap, 0);
        }
    }
}
