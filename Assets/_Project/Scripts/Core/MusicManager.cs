using UnityEngine;

// Pengatur Musik Latar — Versi 2 (dengan dukungan zona via ZoneMusicManager)
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Track Utama (Desa)")]
    [SerializeField] AudioSource dayTrack, nightTrack, bossTrack, bellSource;
    [SerializeField] AudioClip bellClip;
    [SerializeField] float fadeSpeed = 0.5f, maxVolume = 0.7f;

    [Header("Zona Musik (opsional)")]
    [Tooltip("Jika ZoneMusicManager aktif, volume dayTrack/nightTrack dikurangi " +
             "agar tidak bentrok saat pemain di dalam zona.")]
    [SerializeField, Range(0f, 1f)] float villageOnlyVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] float villageInZoneVolume = 0.15f;

    bool boss;
    bool inZone;    // apakah pemain sedang di dalam zona non-village

    void Awake() => Instance = this;

    void Start()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged += OnPhase;
        SetVol(dayTrack, 0f); SetVol(nightTrack, 0f); SetVol(bossTrack, 0f);

        // Dengarkan event zona dari ZoneMusicManager jika ada
        // (kita check via ZoneMusicTrigger events — tidak perlu coupling langsung)
    }

    void OnDestroy()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged -= OnPhase;
    }

    void OnPhase(TimePhase p)
    {
        if ((p == TimePhase.Dusk || p == TimePhase.Dawn) && bellClip && bellSource)
            bellSource.PlayOneShot(bellClip);
    }

    public void SetBoss(bool on) => boss = on;

    /// <summary>Dipanggil oleh ZoneMusicManager saat pemain masuk/keluar zona non-village.</summary>
    public void SetInZone(bool value) => inZone = value;

    void Update()
    {
        if (!DayNightCycle.Instance) return;

        var phase = DayNightCycle.Instance.Phase;
        bool night = phase == TimePhase.Night || phase == TimePhase.Dusk;

        // Kurangi volume track utama desa saat di dalam zona (supaya musik zona lebih dominan)
        float effectiveMax = (inZone && !boss) ? villageInZoneVolume : maxVolume;

        float step = fadeSpeed * Time.unscaledDeltaTime;
        Fade(dayTrack,   (!boss && !night) ? effectiveMax : 0f, step);
        Fade(nightTrack, (!boss && night)  ? effectiveMax : 0f, step);
        Fade(bossTrack,  boss              ? maxVolume    : 0f, step);
    }

    void Fade(AudioSource s, float target, float step)
    {
        if (s) s.volume = Mathf.MoveTowards(s.volume, target, step);
    }

    void SetVol(AudioSource s, float v) { if (s) s.volume = v; }
}
