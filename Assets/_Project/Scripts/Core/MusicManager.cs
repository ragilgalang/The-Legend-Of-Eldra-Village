using UnityEngine;

// Pengatur Musik Latar
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] AudioSource dayTrack, nightTrack, bossTrack, bellSource;
    [SerializeField] AudioClip bellClip;
    [SerializeField] float fadeSpeed = 0.5f, maxVolume = 0.7f;

    bool boss;

    void Awake() => Instance = this;

    void Start()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged += OnPhase;
        SetVol(dayTrack, 0f); SetVol(nightTrack, 0f); SetVol(bossTrack, 0f);
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

    void Update()
    {
        if (!DayNightCycle.Instance) return;

        var phase = DayNightCycle.Instance.Phase;
        bool night = phase == TimePhase.Night || phase == TimePhase.Dusk;

        float step = fadeSpeed * Time.unscaledDeltaTime;
        Fade(dayTrack,   (!boss && !night) ? maxVolume : 0f, step);
        Fade(nightTrack, (!boss && night)  ? maxVolume : 0f, step);
        Fade(bossTrack,  boss              ? maxVolume : 0f, step);
    }

    void Fade(AudioSource s, float target, float step)
    {
        if (s) s.volume = Mathf.MoveTowards(s.volume, target, step);
    }

    void SetVol(AudioSource s, float v) { if (s) s.volume = v; }
}
