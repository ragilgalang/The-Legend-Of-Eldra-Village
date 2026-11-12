using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger otomatis ganti musik ambient saat pemain memasuki zona tertentu.
/// Letakkan pada sebuah GameObject dengan BoxCollider (Is Trigger = true) di tiap batas zona.
/// Jika ZoneMusicTrigger.cs belum ada di project, SceneSetupTool akan membuatnya sebagai
/// BoxCollider biasa dengan komponen ini.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneMusicTrigger : MonoBehaviour
{
    public enum ZoneType
    {
        Village,        // Desa Utama — musik hangat medieval
        Mountain,       // Pegunungan Utara — musik epik orkestra
        Swamp,          // Rawa Selatan — musik misterius dark ambient
        Fishing,        // Desa Nelayan Barat — musik ceria rakyat
        Mines,          // Tambang Timur — musik industrial gelap
        SnowPeak,       // Puncak Salju — musik dingin orkestra
        FairyForest,    // Hutan Peri — musik ajaib & ceria
        SeaTemple,      // Kuil Laut — musik megah + ombak
        FireCanyon,     // Ngarai Api — musik intens battle-ready
    }

    [SerializeField] ZoneType zone = ZoneType.Village;
    [SerializeField] string playerTag = "Player";

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        ZoneMusicManager.Instance?.EnterZone(zone);
        WeatherSystem.Instance?.OnZoneEnter(zone);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        ZoneMusicManager.Instance?.ExitZone(zone);
    }
}

/// <summary>
/// Manager musik per zona. Terintegrasi dengan MusicManager yang sudah ada.
/// Mengelola AudioSource terpisah per zona dan melakukan cross-fade otomatis.
/// </summary>
public class ZoneMusicManager : MonoBehaviour
{
    public static ZoneMusicManager Instance { get; private set; }

    [System.Serializable]
    public struct ZoneTrack
    {
        public ZoneMusicTrigger.ZoneType zone;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [Header("Track per Zona")]
    [SerializeField] ZoneTrack[] zoneTracks;
    [SerializeField] float crossFadeDuration = 2.5f;
    [SerializeField, Range(0f, 1f)] float masterVolume = 0.65f;

    AudioSource sourceA, sourceB;   // double-buffer cross-fade
    bool useA = true;

    ZoneMusicTrigger.ZoneType currentZone = ZoneMusicTrigger.ZoneType.Village;
    Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        sourceA = CreateSource("ZoneMusicA");
        sourceB = CreateSource("ZoneMusicB");
    }

    AudioSource CreateSource(string goName)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop           = true;
        src.playOnAwake    = false;
        src.spatialBlend   = 0f;   // 2D (ambient seluruh layar)
        src.volume         = 0f;
        return src;
    }

    public void EnterZone(ZoneMusicTrigger.ZoneType zone)
    {
        if (zone == currentZone) return;
        currentZone = zone;

        AudioClip clip  = null;
        float targetVol = masterVolume;
        foreach (var t in zoneTracks)
        {
            if (t.zone == zone) { clip = t.clip; targetVol = t.volume * masterVolume; break; }
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossFade(clip, targetVol));
    }

    public void ExitZone(ZoneMusicTrigger.ZoneType zone)
    {
        // Saat keluar zona, kembali ke musik village
        EnterZone(ZoneMusicTrigger.ZoneType.Village);
    }

    IEnumerator CrossFade(AudioClip nextClip, float targetVol)
    {
        var outSrc = useA ? sourceA : sourceB;
        var inSrc  = useA ? sourceB : sourceA;
        useA = !useA;

        if (nextClip)
        {
            inSrc.clip   = nextClip;
            inSrc.volume = 0f;
            inSrc.Play();
        }

        float elapsed = 0f;
        float startVol = outSrc.volume;
        while (elapsed < crossFadeDuration)
        {
            float t = elapsed / crossFadeDuration;
            outSrc.volume = Mathf.Lerp(startVol, 0f,         t);
            inSrc.volume  = Mathf.Lerp(0f,       targetVol,  t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        outSrc.volume = 0f;
        inSrc.volume  = targetVol;
        outSrc.Stop();
    }
}

/// <summary>Extension untuk WeatherSystem agar bisa bereaksi terhadap zona.</summary>
public static class WeatherSystemZoneExtension
{
    public static void OnZoneEnter(this WeatherSystem ws, ZoneMusicTrigger.ZoneType zone)
    {
        if (!ws) return;
        switch (zone)
        {
            case ZoneMusicTrigger.ZoneType.SnowPeak:
                ws.ForceWeather(WeatherSystem.WeatherType.Snow);
                break;
            case ZoneMusicTrigger.ZoneType.Swamp:
                ws.ForceWeather(WeatherSystem.WeatherType.HeavyFog);
                break;
            case ZoneMusicTrigger.ZoneType.FireCanyon:
                ws.ForceWeather(WeatherSystem.WeatherType.Clear);
                break;
            case ZoneMusicTrigger.ZoneType.FairyForest:
                ws.ForceWeather(WeatherSystem.WeatherType.Cloudy);
                break;
            // Zona lain biarkan sistem cuaca berjalan acak sendiri
        }
    }
}
