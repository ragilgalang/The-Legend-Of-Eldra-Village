using UnityEngine;

/// <summary>
/// Kontroler AI Elang Perkasa (Eagle) Desa Eldara.
/// Elang terbang melayang melingkar di angkasa desa (Sky Soaring Path):
/// - Radius orbit melingkar luas di atas Balai Desa dan Menara Pertahanan
/// - Dynamic banking (sayap miring alami ke dalam lingkaran belokan)
/// - Siklus kepakan sayap realistis: mengepak aktif (flapping) naik-turun dan melayang anggun (thermal soaring)
/// - Sinkronisasi kecepatan Animator sehingga sayap mengepak nyata naik turun seperti burung
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

    [Header("Animasi Kepakan Sayap Burung")]
    [Tooltip("Kecepatan kepakan sayap elang saat mengepak aktif (naik turun cepat).")]
    public float flapSpeed = 1.35f;
    [Tooltip("Kecepatan kepakan sayap saat melayang bebas / soaring (sayap terbentang luas).")]
    public float glideSpeed = 0.25f;
    [Tooltip("Durasi fase kepakan sayap aktif (detik).")]
    public float flapDuration = 4.5f;
    [Tooltip("Durasi fase melayang bebas (detik).")]
    public float glideDuration = 4.0f;
    [Tooltip("Jika dicentang, elang akan selalu mengepakkan sayap terus menerus tanpa fase melayang diam.")]
    public bool continuousFlapping = false;

    private float currentAngle;
    private float flapCycleTimer;
    private bool isFlapping = true;
    private Transform modelTransform;
    private Animator animator;

    void Awake()
    {
        modelTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
        animator = GetComponentInChildren<Animator>();
        currentAngle = Random.Range(0f, 360f);
    }

    void Start()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.speed = flapSpeed;
            // Pastikan state Flying aktif jika tersedia
            if (animator.HasState(0, Animator.StringToHash("Flying")))
            {
                animator.Play("Flying", 0, Random.Range(0f, 1f));
            }
        }
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

        // 4. Siklus Kepakan Sayap Elang (Flapping Aktif vs Gliding Soaring)
        if (!continuousFlapping)
        {
            flapCycleTimer += Time.deltaTime;
            if (!isFlapping && flapCycleTimer > glideDuration)
            {
                isFlapping = true;
                flapCycleTimer = 0f;
            }
            else if (isFlapping && flapCycleTimer > flapDuration)
            {
                isFlapping = false;
                flapCycleTimer = 0f;
            }
        }
        else
        {
            isFlapping = true;
        }

        // 5. Sinkronisasi Animator Kecepatan Kepakan Sayap
        if (animator != null)
        {
            float targetAnimSpeed = isFlapping ? flapSpeed : glideSpeed;
            animator.speed = Mathf.Lerp(animator.speed, targetAnimSpeed, Time.deltaTime * 4.0f);
        }

        // 6. Sedikit undulasi tubuh naik-turun selaras dorongan kepakan sayap
        if (modelTransform != null)
        {
            float flapBob = isFlapping ? Mathf.Sin(Time.time * flapSpeed * 6.28f) * 0.12f : 0f;
            modelTransform.localPosition = new Vector3(0, flapBob, 0);
        }
    }
}
