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
        if (!followTarget)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) followTarget = p.transform;
        }

        // Buat bola visual Matahari jika belum ada
        if (!sunVisual)
        {
            var existingSun = GameObject.Find("Visual_Sun_Orb");
            if (existingSun) sunVisual = existingSun.transform;
            else
            {
                var sunGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sunGo.name = "Visual_Sun_Orb";
                sunGo.transform.SetParent(transform);
                sunGo.transform.localScale = Vector3.one * 16f; // Bola matahari besar di langit
                var col = sunGo.GetComponent<Collider>();
                if (col) DestroyImmediate(col);

                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                var mat = new Material(shader);
                mat.name = "M_Sun_Glow";
                Color sunGlow = new Color(1f, 0.92f, 0.55f, 1f);
                mat.SetColor("_BaseColor", sunGlow);
                mat.color = sunGlow;
                sunGo.GetComponent<Renderer>().sharedMaterial = mat;

                // Cincin pendaran cahaya (Corona Glow Ring)
                var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                halo.name = "Sun_Corona";
                halo.transform.SetParent(sunGo.transform, false);
                halo.transform.localScale = Vector3.one * 2.2f;
                var hCol = halo.GetComponent<Collider>();
                if (hCol) DestroyImmediate(hCol);
                halo.GetComponent<Renderer>().sharedMaterial = mat;

                sunVisual = sunGo.transform;
            }
        }

        // Buat bola visual Bulan jika belum ada
        if (!moonVisual)
        {
            var existingMoon = GameObject.Find("Visual_Moon_Orb");
            if (existingMoon) moonVisual = existingMoon.transform;
            else
            {
                var moonGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moonGo.name = "Visual_Moon_Orb";
                moonGo.transform.SetParent(transform);
                moonGo.transform.localScale = Vector3.one * 10f; // Bola bulan perak di langit
                var col = moonGo.GetComponent<Collider>();
                if (col) DestroyImmediate(col);

                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                var mat = new Material(shader);
                mat.name = "M_Moon_Glow";
                Color moonGlow = new Color(0.85f, 0.92f, 1.0f, 1f);
                mat.SetColor("_BaseColor", moonGlow);
                mat.color = moonGlow;
                moonGo.GetComponent<Renderer>().sharedMaterial = mat;

                moonVisual = moonGo.transform;
            }
        }

        // Posisikan matahari dan bulan langsung tinggi di langit (bukan tertinggal di tanah (0,0,0) saat di editor)
        UpdateCelestialPositions();
    }

    public void UpdateCelestialPositions()
    {
        float t = Hour / 24f;
        if (t <= 0.0001f) t = 0.35f; // Default siang (jam ~08:30) jika belum berjalan

        Vector3 centerPos = followTarget ? followTarget.position : (Camera.main ? Camera.main.transform.position : Vector3.zero);
        centerPos.y = 0f;

        if (sun)
        {
            // Perputaran rotasi sudut matahari di langit (Terbit di Timur, Terbenam di Barat)
            sun.transform.rotation = Quaternion.Euler(t * 360f - 90f, 170f, 0);
            if (sunColor != null) sun.color = sunColor.Evaluate(t);
            if (sunIntensity != null && sunIntensity.length > 0) sun.intensity = sunIntensity.Evaluate(t);

            // Pergerakan bola Matahari di langit mengikuti busur orbit matahari (tinggi di angkasa)
            if (sunVisual)
            {
                sunVisual.position = centerPos - sun.transform.forward * celestialDistance;
                sunVisual.LookAt(centerPos);
            }

            // Pergerakan bola Bulan di sisi seberang orbit (terbit saat malam)
            if (moonVisual)
            {
                moonVisual.position = centerPos + sun.transform.forward * celestialDistance;
                moonVisual.LookAt(centerPos);
            }
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
