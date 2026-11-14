using System.Collections;
using UnityEngine;

/// <summary>
/// Pengendali angin global untuk hutan rimba. Satu komponen untuk seluruh scene.
/// - Mengirim parameter angin ke shader rumput lewat global shader property.
/// - Menyediakan Evaluate() agar goyangan pohon (CPU) memakai rumus yang sama dengan shader rumput,
///   sehingga rumput dan pohon bergerak serempak dalam gelombang yang sama.
/// Pasang di GameObject mana saja (misalnya yang sama dengan JungleBiomeGenerator).
/// </summary>
public class JungleWind : MonoBehaviour
{
    public static JungleWind Active { get; private set; }

    [Header("Arah & Kekuatan")]
    [Range(0f, 360f)] public float directionAngle = 35f;   // 0 = ke arah +X, 90 = ke arah +Z
    [Range(0f, 1f)] public float strength = 0.5f;

    [Header("Gelombang")]
    public float waveSpeed = 1.6f;      // kecepatan gelombang (rad/detik)
    public float waveLength = 18f;      // jarak antar puncak gelombang (meter)

    [Header("Hembusan")]
    [Range(0f, 1f)] public float gustAmount = 0.5f;   // seberapa kuat angin naik-turun
    public float gustSpeed = 0.35f;

    [Header("Pohon")]
    public bool autoAttachTreeSway = true;   // otomatis menambah JungleTreeSway ke semua JungleTree

    static readonly int WindId = Shader.PropertyToID("_JungleWind");
    static readonly int ParamId = Shader.PropertyToID("_JungleWindParams");

    Vector2 dir = Vector2.right;
    float freq = 0.35f;

    public Vector3 Direction3 { get { return new Vector3(dir.x, 0f, dir.y); } }

    void OnEnable() { Active = this; Push(); }
    void OnDisable() { if (Active == this) Active = null; }

    IEnumerator Start()
    {
        if (!autoAttachTreeSway) yield break;
        yield return null;   // tunggu generator selesai membuat pohon
        AttachTreeSway();
    }

    void Update() { Push(); }

    void Push()
    {
        float a = directionAngle * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        freq = Mathf.PI * 2f / Mathf.Max(0.1f, waveLength);

        // _JungleWind        = (arahX, arahZ, kekuatan, kecepatan)
        // _JungleWindParams  = (frekuensi, hembusan, waktu, kecepatan hembusan)
        Shader.SetGlobalVector(WindId, new Vector4(dir.x, dir.y, strength, waveSpeed));
        Shader.SetGlobalVector(ParamId, new Vector4(freq, gustAmount, Time.time, gustSpeed));
    }

    /// <summary>
    /// Dorongan angin di posisi dunia tertentu. Rumus identik dengan WindOffset() di shader rumput.
    /// Nilai kira-kira 0..1.2 (searah angin), phase01 = fase acak 0..1.
    /// </summary>
    public float Evaluate(Vector3 worldPos, float phase01)
    {
        Vector2 perp = new Vector2(-dir.y, dir.x);
        Vector2 p = new Vector2(worldPos.x, worldPos.z);
        float along = Vector2.Dot(p, dir);
        float wave = Mathf.Sin(along * freq - Time.time * waveSpeed)
                   + 0.5f * Mathf.Sin(Vector2.Dot(p, perp) * freq * 1.7f - Time.time * waveSpeed * 1.6f + phase01 * 6.2831f);
        float gust = 1f + gustAmount * Mathf.Sin(along * 0.05f - Time.time * gustSpeed);
        return strength * gust * (1f + 0.35f * wave);
    }

    [ContextMenu("Attach Tree Sway")]
    public void AttachTreeSway()
    {
        foreach (var t in FindObjectsByType<JungleTree>(FindObjectsSortMode.None))
            if (t.GetComponent<JungleTreeSway>() == null)
                t.gameObject.AddComponent<JungleTreeSway>();
    }
}
