using System;
using UnityEngine;

public enum TimePhase { Day, Dusk, Night, Dawn }

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Pengaturan")]
    [SerializeField] float dayLengthInSeconds = 600f; // 10 menit = 1 hari
    [SerializeField, Range(0, 24)] float startHour = 7f;

    [Header("Referensi Cahaya")]
    [SerializeField] Light sun;
    [SerializeField] Gradient sunColor;
    [SerializeField] AnimationCurve sunIntensity;
    [SerializeField] Gradient fogColor;

    [Header("Visual Matahari & Bulan di Langit")]
    [SerializeField] Transform sunVisual;
    [SerializeField] Transform moonVisual;
    [SerializeField] float celestialDistance = 160f;
    [SerializeField] Transform followTarget;

    public float Hour { get; private set; }
    public int DayCount { get; private set; } = 1;
    public TimePhase Phase { get; private set; }

    public event Action<TimePhase> OnPhaseChanged;
    public event Action<int> OnNewDay;

    void Awake()
    {
        Instance = this;
        Hour = startHour;
        Phase = GetPhase(Hour);
    }

    void Start()
    {
        EnsureCelestialVisuals();
    }

    public void EnsureCelestialVisuals()
    {
        // Bersihkan objek bola/cincin matahari visual lama di scene agar tidak menghalangi pemandangan desa dan elang terbang
        var oldSun = GameObject.Find("Visual_Sun_Orb");
        if (oldSun)
        {
            if (Application.isPlaying) Destroy(oldSun);
            else DestroyImmediate(oldSun);
        }
        var oldCorona = GameObject.Find("Sun_Corona");
        if (oldCorona)
        {
            if (Application.isPlaying) Destroy(oldCorona);
            else DestroyImmediate(oldCorona);
        }
        var oldMoon = GameObject.Find("Visual_Moon_Orb");
        if (oldMoon)
        {
            if (Application.isPlaying) Destroy(oldMoon);
            else DestroyImmediate(oldMoon);
        }
        sunVisual = null;
        moonVisual = null;

        UpdateCelestialPositions();
    }

    public void UpdateCelestialPositions()
    {
        float t = Hour / 24f;
        if (t <= 0.0001f) t = 0.35f; // Default siang (jam ~08:30) jika belum berjalan

        if (sun)
        {
            // Perputaran rotasi sudut matahari di langit (Terbit di Timur, Terbenam di Barat)
            sun.transform.rotation = Quaternion.Euler(t * 360f - 90f, 170f, 0);
            if (sunColor != null) sun.color = sunColor.Evaluate(t);
            if (sunIntensity != null && sunIntensity.length > 0) sun.intensity = sunIntensity.Evaluate(t);
        }
    }

    void Update()
    {
        Hour += (24f / dayLengthInSeconds) * Time.deltaTime;
        if (Hour >= 24f) { Hour -= 24f; DayCount++; OnNewDay?.Invoke(DayCount); }

        float t = Hour / 24f;
        UpdateCelestialPositions();

        if (fogColor != null) RenderSettings.fogColor = fogColor.Evaluate(t);

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

    public void SetTime(float hour, int day)
    {
        Hour = hour;
        DayCount = day;
        Phase = GetPhase(Hour);
        OnPhaseChanged?.Invoke(Phase);
    }
}
