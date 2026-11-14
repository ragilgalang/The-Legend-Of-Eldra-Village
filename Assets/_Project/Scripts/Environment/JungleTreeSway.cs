using UnityEngine;

/// <summary>
/// Pergerakan pohon tertiup angin. Ditambahkan otomatis oleh JungleWind ke setiap JungleTree.
/// - Batang: miring perlahan searah angin dari pangkal (makin tinggi pohon, makin besar ayunannya).
/// - Tajuk: bergetar halus di sekitar titik 65% tinggi batang, seperti daun yang diterpa angin.
/// Pohon besar bergoyang pelan dan berat, jadi sudut maksimalnya sengaja kecil.
/// </summary>
[DisallowMultipleComponent]
public class JungleTreeSway : MonoBehaviour
{
    [Header("Batang")]
    public float maxTrunkAngle = 1.8f;        // derajat saat angin kuat
    public bool scaleWithHeight = true;
    public float referenceHeight = 38f;

    [Header("Tajuk")]
    public float canopyFlutter = 1.6f;        // derajat
    public float flutterSpeed = 2.2f;
    [Range(0.3f, 1f)] public float canopyPivotHeight = 0.65f;

    JungleTree tree;
    Transform canopy;
    Quaternion baseRotation;
    Vector3 pivot;
    float phase;

    void Start()
    {
        tree = GetComponent<JungleTree>();
        canopy = transform.Find("Canopy");
        baseRotation = transform.rotation;      // rotasi awal dari generator (yaw + miring acak)
        phase = Random.value;
        float h = tree != null ? tree.height : 30f;
        pivot = new Vector3(0f, h * canopyPivotHeight, 0f);
    }

    void Update()
    {
        JungleWind w = JungleWind.Active;
        if (w == null) return;

        // --- batang: miring searah angin
        float heightFactor = (scaleWithHeight && tree != null)
            ? Mathf.Clamp(tree.height / referenceHeight, 0.5f, 1.6f) : 1f;
        float angle = w.Evaluate(transform.position, phase) * maxTrunkAngle * heightFactor;
        Vector3 axis = Vector3.Cross(Vector3.up, w.Direction3);
        transform.rotation = Quaternion.AngleAxis(angle, axis) * baseRotation;

        // --- tajuk: getaran kecil di sekitar pivot
        if (canopy != null)
        {
            float t = Time.time * flutterSpeed + phase * 20f;
            float amp = canopyFlutter * (0.25f + w.strength);
            Quaternion q = Quaternion.Euler(Mathf.Sin(t) * amp, 0f, Mathf.Sin(t * 1.31f + 1.7f) * amp);
            canopy.localRotation = q;
            canopy.localPosition = pivot - q * pivot;   // putar di sekitar pivot, bukan di pangkal pohon
        }
    }

    void OnDisable()
    {
        if (canopy != null) { canopy.localRotation = Quaternion.identity; canopy.localPosition = Vector3.zero; }
    }
}
