using System;
using UnityEngine;

public enum TimePhase { Day, Dusk, Night, Dawn }

/// <summary>
/// Siklus siang-malam Desa Eldara.
/// Nama field serialized (sun, sunColor, sunIntensity, fogColor, ...) sengaja TIDAK diubah
/// supaya SceneSetupTool (SerializedObject.FindProperty) tetap bekerja.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Pengaturan")]
    [SerializeField] float dayLengthInSeconds = 600f; // 10 menit = 1 hari
    [SerializeField, Range(0, 24)] float startHour = 7f;
    [Tooltip("Arah orbit matahari. 270 = terbit di Timur (+X), terbenam di Barat (-X).")]
    [SerializeField, Range(0f, 360f)] float sunAzimuth = 270f;

    [Header("Referensi Cahaya")]
    [SerializeField] Light sun;
    [SerializeField] Gradient sunColor;
    [SerializeField] AnimationCurve sunIntensity;

    [Header("Kabut")]
    [Tooltip("Jika mati, warna kabut otomatis mengikuti tinggi matahari (dasar = warna kabut scene).")]
    [SerializeField] bool useFogGradient = false;
    [SerializeField] Gradient fogColor;
    [SerializeField] Color nightFogColor = new Color(0.03f, 0.04f, 0.09f);

    [Header("Visual Matahari & Bulan di Langit (opsional)")]
    [Tooltip("Hapus bola Visual_Sun_Orb / Sun_Corona / Visual_Moon_Orb lama dari scene.")]
    [SerializeField] bool removeLegacyOrbs = true;
    [SerializeField] Transform sunVisual;
    [SerializeField] Transform moonVisual;
    [SerializeField] float celestialDistance = 160f;
    [SerializeField] Transform followTarget;

    public float Hour { get; private set; }
    public int DayCount { get; private set; } = 1;
    public TimePhase Phase { get; private set; }
    /// <summary>Waktu 0..1 (0 = tengah malam, 0.5 = tengah hari).</summary>
    public float NormalizedTime => Hour / 24f;

    public event Action<TimePhase> OnPhaseChanged;
    public event Action<int> OnNewDay;

    Color baseFogColor;

    // Di Edit Mode Awake belum jalan (Hour = 0), jadi pakai startHour untuk pratinjau.
    float CurrentHour => Application.isPlaying ? Hour : startHour;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        Hour = startHour;
        Phase = GetPhase(Hour);
        baseFogColor = RenderSettings.fogColor;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        EnsureCelestialVisuals();
        ApplyFog();
    }

    public void EnsureCelestialVisuals()
    {
        if (removeLegacyOrbs)
        {
            RemoveLegacy("Visual_Sun_Orb");
            RemoveLegacy("Sun_Corona");
            RemoveLegacy("Visual_Moon_Orb");
        }

        UpdateCelestialPositions();
    }

    void RemoveLegacy(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (!go) return;

        // Jangan hapus visual yang memang sengaja diassign di Inspector.
        if (go.transform == sunVisual || go.transform == moonVisual) return;

        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    Quaternion SunRotation(float t)
    {
        // t = 0.25 -> terbit (horizon), 0.5 -> puncak langit, 0.75 -> terbenam.
        return Quaternion.Euler(t * 360f - 90f, sunAzimuth, 0f);
    }

    public void UpdateCelestialPositions()
    {
        float t = CurrentHour / 24f;
        Quaternion rot = SunRotation(t);

        // Arah dari bumi menuju matahari; y > 0 berarti matahari di atas horizon.
        Vector3 toSun = rot * Vector3.back;
        float sunHeight = toSun.y;

        if (sun)
        {
            sun.transform.rotation = rot;
            if (sunColor != null) sun.color = sunColor.Evaluate(t);
            if (sunIntensity != null && sunIntensity.length > 0) sun.intensity = sunIntensity.Evaluate(t);

            // Saat matahari di bawah horizon cahayanya menyorot dari bawah tanah:
            // redupkan bayangan agar tidak muncul bayangan aneh di malam hari.
            sun.shadowStrength = Mathf.Clamp01(sunHeight * 5f);
        }

        if (sunVisual || moonVisual)
        {
            Vector3 origin = followTarget ? followTarget.position
                           : (Camera.main ? Camera.main.transform.position : Vector3.zero);

            if (sunVisual)
            {
                sunVisual.position = origin + toSun * celestialDistance;
                SetVisible(sunVisual, sunHeight > -0.05f);
            }
            if (moonVisual)
            {
                moonVisual.position = origin - toSun * celestialDistance;
                SetVisible(moonVisual, sunHeight < 0.05f);
            }
        }
    }

    static void SetVisible(Transform tr, bool visible)
    {
        if (tr.gameObject.activeSelf != visible) tr.gameObject.SetActive(visible);
    }

    void ApplyFog()
    {
        if (!RenderSettings.fog) return;

        float t = Hour / 24f;
        if (useFogGradient && fogColor != null)
        {
            RenderSettings.fogColor = fogColor.Evaluate(t);
            return;
        }

        float sunHeight = (SunRotation(t) * Vector3.back).y;
        float day = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((sunHeight + 0.1f) / 0.4f));
        RenderSettings.fogColor = Color.Lerp(nightFogColor, baseFogColor, day);
    }

    void Update()
    {
        if (dayLengthInSeconds <= 0f) return;

        Hour += (24f / dayLengthInSeconds) * Time.deltaTime;
        while (Hour >= 24f)
        {
            Hour -= 24f;
            DayCount++;
            OnNewDay?.Invoke(DayCount);
        }

        UpdateCelestialPositions();
        ApplyFog();

        var newPhase = GetPhase(Hour);
        if (newPhase != Phase)
        {
            Phase = newPhase;
            OnPhaseChanged?.Invoke(Phase);
        }
    }

    TimePhase GetPhase(float h)
    {
        if (h >= 6f && h < 17f) return TimePhase.Day;
        if (h >= 17f && h < 19f) return TimePhase.Dusk;
        if (h >= 19f || h < 5f) return TimePhase.Night;
        return TimePhase.Dawn;
    }

    /// <summary>Dipakai SaveSystem / debug. Selalu memicu OnPhaseChanged agar sistem lain ikut sinkron.</summary>
    public void SetTime(float hour, int day)
    {
        Hour = Mathf.Repeat(hour, 24f);
        DayCount = Mathf.Max(1, day);
        Phase = GetPhase(Hour);

        UpdateCelestialPositions();
        ApplyFog();
        OnPhaseChanged?.Invoke(Phase);
    }
}
