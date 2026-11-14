using UnityEngine;

/// <summary>
/// Gerbang portal: saat Player (CharacterController) masuk ke trigger,
/// Player dipindahkan ke 'destination' dengan efek kilat layar.
/// Dipakai oleh SceneSetupTool.JungleBiome (portal desa -> jungle dan portal kembali).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PortalGate : MonoBehaviour
{
    [Header("Tujuan")]
    public Transform destination;
    public string portalLabel = "Portal";

    [Header("Perilaku")]
    public float cooldown = 3f;
    public float flashDuration = 0.7f;
    public Color flashColor = new Color(0.4f, 1f, 0.6f, 1f);

    [Header("Visual")]
    public Renderer[] energyRenderers;   // lapisan energi (tekstur digulirkan)
    public Vector2[] scrollSpeeds;       // kecepatan gulir tiap lapisan (UV per detik)
    public Light glowLight;
    public float lightBase = 2.5f;
    public float lightPulse = 0.7f;
    public Transform orbitPivot;         // pivot bola cahaya yang berputar (sumbu Z lokal)
    public float orbitSpeed = 45f;

    // Cooldown dibagi antar semua portal agar tidak bolak-balik saat baru tiba
    static float s_nextAllowedTime;

    Material[] _mats;
    string[] _texProp;
    float _flash;

    void Reset()
    {
        var box = GetComponent<BoxCollider>();
        if (box) box.isTrigger = true;
    }

    void Awake()
    {
        // Pastikan BoxCollider selalu isTrigger
        var box = GetComponent<BoxCollider>();
        if (box) box.isTrigger = true;

        int n = energyRenderers != null ? energyRenderers.Length : 0;
        _mats = new Material[n];
        _texProp = new string[n];
        for (int i = 0; i < n; i++)
        {
            if (!energyRenderers[i]) continue;
            _mats[i] = energyRenderers[i].material; // instance per portal
            _texProp[i] = _mats[i].HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        }

        // Jika destination null, cari Arrival_Jungle / Arrival_Village berdasarkan nama portal
        if (!destination)
        {
            string arrivalName = portalLabel.Contains("Desa") ? "Arrival_Village" : "Arrival_Jungle";
            var arr = GameObject.Find(arrivalName);
            if (arr) destination = arr.transform;
            if (destination)
                Debug.LogWarning($"[PortalGate] '{portalLabel}': destination auto-linked ke '{arrivalName}'. " +
                                 "Set manual di Inspector untuk hasil stabil.");
            else
                Debug.LogError($"[PortalGate] '{portalLabel}': destination NULL dan tidak ditemukan '{arrivalName}' di scene! " +
                               "Jalankan ulang Setup Prototype Scene.");
        }
    }

    void OnDestroy()
    {
        if (_mats == null) return;
        foreach (var m in _mats) if (m) Destroy(m);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (_mats != null && scrollSpeeds != null)
        {
            for (int i = 0; i < _mats.Length && i < scrollSpeeds.Length; i++)
            {
                if (!_mats[i]) continue;
                Vector2 o = _mats[i].GetTextureOffset(_texProp[i]) + scrollSpeeds[i] * dt;
                o.x = Mathf.Repeat(o.x, 1f);
                o.y = Mathf.Repeat(o.y, 1f);
                _mats[i].SetTextureOffset(_texProp[i], o);
            }
        }

        if (glowLight) glowLight.intensity = lightBase + Mathf.Sin(Time.time * 2.2f) * lightPulse;
        if (orbitPivot) orbitPivot.Rotate(0f, 0f, orbitSpeed * dt, Space.Self);
        if (_flash > 0f) _flash -= dt;
    }

    void OnTriggerEnter(Collider other)
    {
        // Debug: selalu log siapa yang masuk trigger
        Debug.Log($"[PortalGate] '{portalLabel}' OnTriggerEnter: {other.name} (tag={other.tag})");

        if (!destination)
        {
            Debug.LogError($"[PortalGate] '{portalLabel}': destination NULL — teleport dibatalkan!");
            return;
        }

        if (Time.time < s_nextAllowedTime)
        {
            Debug.Log($"[PortalGate] '{portalLabel}': cooldown aktif, skip.");
            return;
        }

        // Cari CharacterController di object itu sendiri atau parent-nya
        var cc = other.GetComponent<CharacterController>()
                 ?? other.GetComponentInParent<CharacterController>();

        if (!cc)
        {
            Debug.Log($"[PortalGate] '{portalLabel}': tidak ada CharacterController di {other.name}.");
            return;
        }

        // Terima player apa pun tag-nya (tag "Player" disarankan tapi tidak wajib)
        // Tolak hanya jika jelas bukan player (tidak punya CharacterController)
        Debug.Log($"[PortalGate] '{portalLabel}': TELEPORT {cc.name} → {destination.position}");

        s_nextAllowedTime = Time.time + cooldown;

        bool wasEnabled = cc.enabled;
        cc.enabled = false;
        cc.transform.SetPositionAndRotation(destination.position, destination.rotation);
        cc.enabled = wasEnabled;
        Physics.SyncTransforms();

        // Segera perbarui ChunkRenderDistance seketika agar pohon jungle langsung tampil tanpa culling
        var crd = ChunkRenderDistance.Instance ?? FindFirstObjectByType<ChunkRenderDistance>();
        if (crd != null) crd.UpdateAll(true);

        _flash = flashDuration;
    }

    void OnGUI()
    {
        if (_flash <= 0f || Event.current.type != EventType.Repaint) return;
        var c = flashColor;
        c.a = Mathf.Clamp01(_flash / Mathf.Max(0.01f, flashDuration));
        GUI.color = c;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnDrawGizmos()
    {
        if (!destination) return;
        Gizmos.color = new Color(0.3f, 1f, 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.up * 2f, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.6f);
    }
}
