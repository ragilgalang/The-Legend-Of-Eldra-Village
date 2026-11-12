using System.Collections;
using UnityEngine;

/// <summary>
/// Sistem cuaca dinamis Desa Eldara.
/// Mendukung: Cerah, Mendung, Hujan, Badai, Kabut Tebal, Salju (di zona Puncak Salju).
/// Terintegrasi dengan DayNightCycle untuk kabut malam & hujan senja.
/// Tambahkan ke GameObject Managers lewat SceneSetupTool.
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance { get; private set; }

    public enum WeatherType { Clear, Cloudy, Rain, Storm, HeavyFog, Snow }

    [Header("Cuaca Saat Ini")]
    [SerializeField] WeatherType currentWeather = WeatherType.Clear;
    [SerializeField, Range(0f, 1f)] float intensity = 0f;

    [Header("Durasi Cuaca (detik)")]
    [SerializeField] float minWeatherDuration = 90f;
    [SerializeField] float maxWeatherDuration = 300f;

    [Header("Referensi")]
    [SerializeField] Light sun;
    [SerializeField] ParticleSystem rainParticles;
    [SerializeField] ParticleSystem snowParticles;

    [Header("Override Kabut")]
    [SerializeField] bool overrideFog = true;
    [SerializeField] float clearFogDensity  = 0.004f;
    [SerializeField] float rainFogDensity   = 0.018f;
    [SerializeField] float stormFogDensity  = 0.035f;
    [SerializeField] float fogFogDensity    = 0.055f;

    // State internal
    float weatherTimer;
    float nextChangeDuration;
    float baseSunIntensity = 1.25f;

    public WeatherType Current => currentWeather;
    public float Intensity    => intensity;

    // Event: subscribers bisa bereaksi saat cuaca berubah
    public event System.Action<WeatherType> OnWeatherChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        nextChangeDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
        weatherTimer = 0f;
        ApplyWeather(currentWeather, 0f);
    }

    void Update()
    {
        weatherTimer += Time.deltaTime;
        if (weatherTimer >= nextChangeDuration)
        {
            weatherTimer = 0f;
            nextChangeDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
            PickNextWeather();
        }

        // Transisi intensity perlahan
        float targetIntensity = GetTargetIntensity(currentWeather);
        intensity = Mathf.MoveTowards(intensity, targetIntensity, Time.deltaTime * 0.15f);
        UpdateFog();
        UpdateSunDim();
    }

    void PickNextWeather()
    {
        // Peluang cuaca berdasarkan fase hari
        bool isNight = DayNightCycle.Instance &&
                       (DayNightCycle.Instance.Phase == TimePhase.Night ||
                        DayNightCycle.Instance.Phase == TimePhase.Dusk);

        // Bobot cuaca: index = WeatherType
        float[] weights = isNight
            ? new float[] { 0.25f, 0.20f, 0.20f, 0.10f, 0.25f, 0.00f }  // malam lebih berkabut
            : new float[] { 0.45f, 0.25f, 0.15f, 0.05f, 0.05f, 0.05f }; // siang lebih cerah

        float roll = Random.value;
        float cumulative = 0f;
        WeatherType next = WeatherType.Clear;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) { next = (WeatherType)i; break; }
        }

        if (next != currentWeather)
            StartCoroutine(TransitionWeather(next));
    }

    IEnumerator TransitionWeather(WeatherType next)
    {
        // Fade out current
        float elapsed = 0f;
        float fadeDur = 8f;
        float startIntensity = intensity;
        while (elapsed < fadeDur)
        {
            intensity = Mathf.Lerp(startIntensity, 0f, elapsed / fadeDur);
            UpdateParticles();
            elapsed += Time.deltaTime;
            yield return null;
        }
        intensity = 0f;
        UpdateParticles();

        currentWeather = next;
        OnWeatherChanged?.Invoke(next);

        // Fade in new
        elapsed = 0f;
        float endIntensity = GetTargetIntensity(next);
        while (elapsed < fadeDur)
        {
            intensity = Mathf.Lerp(0f, endIntensity, elapsed / fadeDur);
            UpdateParticles();
            UpdateFog();
            UpdateSunDim();
            elapsed += Time.deltaTime;
            yield return null;
        }
        intensity = endIntensity;
        UpdateParticles();
        ApplyWeather(next, intensity);
    }

    void ApplyWeather(WeatherType w, float i)
    {
        UpdateFog();
        UpdateSunDim();
        UpdateParticles();
    }

    void UpdateFog()
    {
        if (!overrideFog || !RenderSettings.fog) return;

        float targetDensity = currentWeather switch
        {
            WeatherType.Rain     => Mathf.Lerp(clearFogDensity, rainFogDensity,  intensity),
            WeatherType.Storm    => Mathf.Lerp(rainFogDensity,  stormFogDensity, intensity),
            WeatherType.HeavyFog => Mathf.Lerp(clearFogDensity, fogFogDensity,   intensity),
            WeatherType.Cloudy   => Mathf.Lerp(clearFogDensity, clearFogDensity * 1.5f, intensity),
            WeatherType.Snow     => Mathf.Lerp(clearFogDensity, rainFogDensity,  intensity),
            _                    => clearFogDensity,
        };

        RenderSettings.fogDensity = Mathf.MoveTowards(
            RenderSettings.fogDensity, targetDensity, Time.deltaTime * 0.003f);

        // Warna kabut saat hujan → abu gelap
        Color targetFogColor = currentWeather == WeatherType.Storm
            ? new Color(0.35f, 0.37f, 0.4f)
            : new Color(0.7f, 0.76f, 0.82f);
        RenderSettings.fogColor = Color.Lerp(
            RenderSettings.fogColor, targetFogColor, Time.deltaTime * 0.5f);
    }

    void UpdateSunDim()
    {
        if (!sun) return;
        float dimFactor = currentWeather switch
        {
            WeatherType.Storm    => Mathf.Lerp(1f, 0.3f, intensity),
            WeatherType.Rain     => Mathf.Lerp(1f, 0.6f, intensity),
            WeatherType.Cloudy   => Mathf.Lerp(1f, 0.75f, intensity),
            WeatherType.HeavyFog => Mathf.Lerp(1f, 0.55f, intensity),
            WeatherType.Snow     => Mathf.Lerp(1f, 0.7f, intensity),
            _                    => 1f,
        };
        // DayNightCycle mengatur intensitas dasarnya; kami hanya mengalikan dengan dimFactor
        // agar tidak conflik — DayNightCycle tetap jadi master, WeatherSystem hanya redupkan
        sun.intensity = Mathf.MoveTowards(sun.intensity, sun.intensity * dimFactor, Time.deltaTime * 0.1f);
    }

    void UpdateParticles()
    {
        // Rain / Storm
        if (rainParticles)
        {
            bool shouldRain = currentWeather == WeatherType.Rain ||
                              currentWeather == WeatherType.Storm;
            if (shouldRain && !rainParticles.isPlaying) rainParticles.Play();
            if (!shouldRain && rainParticles.isPlaying)  rainParticles.Stop();
            var em = rainParticles.emission;
            em.rateOverTime = shouldRain
                ? (currentWeather == WeatherType.Storm ? 800f * intensity : 350f * intensity)
                : 0f;
        }
        // Snow
        if (snowParticles)
        {
            bool shouldSnow = currentWeather == WeatherType.Snow;
            if (shouldSnow && !snowParticles.isPlaying) snowParticles.Play();
            if (!shouldSnow && snowParticles.isPlaying)  snowParticles.Stop();
            var em = snowParticles.emission;
            em.rateOverTime = shouldSnow ? 200f * intensity : 0f;
        }
    }

    float GetTargetIntensity(WeatherType w) => w switch
    {
        WeatherType.Clear    => 0f,
        WeatherType.Cloudy   => 0.6f,
        WeatherType.Rain     => 0.85f,
        WeatherType.Storm    => 1f,
        WeatherType.HeavyFog => 0.9f,
        WeatherType.Snow     => 0.75f,
        _                    => 0f,
    };

    // API publik: paksa cuaca dari luar (mis. trigger quest)
    public void ForceWeather(WeatherType w)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionWeather(w));
    }
}

