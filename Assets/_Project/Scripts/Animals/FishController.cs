using UnityEngine;

/// <summary>
/// Kontroler Ikan Air Tawar / Piranha untuk Desa Eldara.
/// Berenang meliuk-liuk di kolam / parit air desa:
/// - Gerakan liukan ekor ikan alami (sinusoidal swimming wiggle)
/// - Berkeliling santai di area kolam air
/// </summary>
public class FishController : MonoBehaviour
{
    [Header("Parameter Berenang")]
    public float swimRadius = 3.5f;
    public float swimSpeed = 1.4f;
    public float wiggleSpeed = 8.0f;
    public float wiggleAngle = 18.0f;

    private Vector3 homeCenter;
    private Vector3 targetPos;
    private float retargetTimer;
    private Transform modelTransform;
    private Quaternion baseModelRot;

    void Awake()
    {
        homeCenter = transform.position;
        modelTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
        baseModelRot = modelTransform.localRotation;
        PickNewTarget();
    }

    void Update()
    {
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f || Vector3.Distance(transform.position, targetPos) < 0.3f)
        {
            PickNewTarget();
        }

        // 1. Gerak maju ke arah target
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.position += dir * (swimSpeed * Time.deltaTime);
            Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 3.5f);
        }

        // 2. Liukan badan ikan (Wiggle)
        if (modelTransform != null)
        {
            float yawWiggle = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAngle;
            float rollWiggle = Mathf.Cos(Time.time * wiggleSpeed * 0.5f) * 4.0f;
            modelTransform.localRotation = baseModelRot * Quaternion.Euler(0, yawWiggle, rollWiggle);
        }
    }

    private void PickNewTarget()
    {
        Vector2 circle = Random.insideUnitCircle * swimRadius;
        targetPos = homeCenter + new Vector3(circle.x, Random.Range(-0.2f, 0.2f), circle.y);
        retargetTimer = Random.Range(3.0f, 6.0f);
    }
}
