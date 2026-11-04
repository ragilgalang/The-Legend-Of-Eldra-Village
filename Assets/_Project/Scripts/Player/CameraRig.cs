using UnityEngine;
using UnityEngine.InputSystem;

public enum CameraMode { FirstPerson, ThirdBack, ThirdFront }

[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance { get; private set; }

    [Header("Target")]
    [SerializeField] Transform target;                 // objek Player
    [SerializeField] Renderer[] playerRenderers;       // semua mesh model pemain

    [Header("Kontrol")]
    [SerializeField] CameraMode startMode = CameraMode.ThirdBack;
    [SerializeField] float sensitivity = 0.12f;
    [SerializeField] float minPitch = -70f, maxPitch = 70f;
    [SerializeField] float transitionSpeed = 8f;

    [Header("Orang Pertama")]
    [SerializeField] float fpHeight = 1.65f;
    [SerializeField] float fpFov = 75f;

    [Header("Orang Ketiga")]
    [SerializeField] float tpHeight = 1.6f;
    [SerializeField] float backDistance = 4.5f;
    [SerializeField] float frontDistance = 3.5f;
    [SerializeField] float tpFov = 60f;

    [Header("Tabrakan Kamera")]
    [SerializeField] LayerMask collisionMask = ~0;     // kecualikan layer Player dan Enemy
    [SerializeField] float collisionRadius = 0.25f;

    public CameraMode Mode { get; private set; }
    public bool IsFirstPerson => Mode == CameraMode.FirstPerson;
    public float AimYaw => yaw;                        // arah hadap/gerak pemain

    Camera cam;
    float yaw, pitch = 12f;
    float curHeight, curDistance, curFov, yawOffset;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        yaw = target.eulerAngles.y;
        Mode = startMode;
        curHeight = TargetHeight();
        curDistance = TargetDistance();
        curFov = TargetFov();
        yawOffset = TargetYawOffset();
        ApplyRenderers();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.vKey.wasPressedThisFrame) CycleMode();
    }

    void LateUpdate()
    {
        // Putar pandangan hanya saat kursor terkunci (tidak saat dialog)
        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue();
            yaw += d.x * sensitivity;
            pitch = Mathf.Clamp(pitch - d.y * sensitivity, minPitch, maxPitch);
        }

        // Transisi halus menuju nilai mode aktif
        float k = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
        curHeight   = Mathf.Lerp(curHeight, TargetHeight(), k);
        curDistance = Mathf.Lerp(curDistance, TargetDistance(), k);
        curFov      = Mathf.Lerp(curFov, TargetFov(), k);
        yawOffset   = Mathf.LerpAngle(yawOffset, TargetYawOffset(), k);
        cam.fieldOfView = curFov;

        Quaternion rot = Quaternion.Euler(pitch, yaw + yawOffset, 0f);
        Vector3 pivot = target.position + Vector3.up * curHeight;

        // Cegah kamera menembus dinding
        float dist = curDistance;
        if (dist > 0.05f)
        {
            Vector3 backDir = -(rot * Vector3.forward);
            if (Physics.SphereCast(pivot, collisionRadius, backDir, out RaycastHit hit,
                    dist, collisionMask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0f, hit.distance - 0.05f);
        }

        transform.position = pivot - rot * Vector3.forward * dist;
        transform.rotation = rot;

        // Sembunyikan model saat kamera sudah masuk kepala
        UpdateRendererVisibility();
    }

    public void CycleMode()
    {
        Mode = (CameraMode)(((int)Mode + 1) % 3);
        ApplyRenderers();
    }

    public void SetMode(CameraMode mode)
    {
        Mode = mode;
        ApplyRenderers();
    }

    float TargetHeight()    => IsFirstPerson ? fpHeight : tpHeight;
    float TargetFov()       => IsFirstPerson ? fpFov : tpFov;
    float TargetDistance()  => Mode switch
    {
        CameraMode.FirstPerson => 0f,
        CameraMode.ThirdFront  => frontDistance,
        _                      => backDistance
    };
    float TargetYawOffset() => Mode == CameraMode.ThirdFront ? 180f : 0f;

    void ApplyRenderers() => UpdateRendererVisibility();

    void UpdateRendererVisibility()
    {
        // Model hanya disembunyikan jika kamera benar-benar sudah dekat
        bool hide = IsFirstPerson && curDistance < 0.3f;
        var mode = hide ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                        : UnityEngine.Rendering.ShadowCastingMode.On;
        foreach (var r in playerRenderers)
        {
            if (!r) continue;
            r.shadowCastingMode = mode;
            r.forceRenderingOff = hide && mode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                                  ? false : false;
        }
        // Untuk menyembunyikan penuh tapi tetap bayangan: gunakan ShadowsOnly (sudah cukup).
    }
}
