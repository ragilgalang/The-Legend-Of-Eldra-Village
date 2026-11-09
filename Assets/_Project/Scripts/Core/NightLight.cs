using System.Collections;
using UnityEngine;

public class NightLight : MonoBehaviour
{
    [SerializeField] Light lightSource;
    [SerializeField] Renderer emissiveRenderer;       // opsional: jendela/lentera
    [SerializeField] Color emissionColor = new Color(1f, 0.7f, 0.3f);
    [SerializeField] float intensityOn = 2f, fadeTime = 1.5f;
    [SerializeField] Vector2 randomDelay = new Vector2(0f, 4f);   // agar tidak serentak
    [SerializeField] bool flicker = true;

    float target;
    float current;

    public void Configure(Light light, Renderer emissive = null, Color? emission = null, float intensity = 2f, bool enableFlicker = true)
    {
        lightSource = light;
        emissiveRenderer = emissive;
        if (emission.HasValue) emissionColor = emission.Value;
        intensityOn = intensity;
        flicker = enableFlicker;
    }

    void Start()
    {
        var c = DayNightCycle.Instance;
        if (c != null)
        {
            c.OnPhaseChanged += OnPhase;
            bool nightNow = c.Phase == TimePhase.Dusk || c.Phase == TimePhase.Night;
            current = target = nightNow ? intensityOn : 0f;
            Apply(current);
        }
        else
        {
            current = target = intensityOn;
            Apply(current);
        }
    }

    void OnDestroy()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged -= OnPhase;
    }

    void OnPhase(TimePhase p) => StartCoroutine(Switch(p == TimePhase.Dusk || p == TimePhase.Night));

    IEnumerator Switch(bool on)
    {
        yield return new WaitForSeconds(Random.Range(randomDelay.x, randomDelay.y));
        target = on ? intensityOn : 0f;
    }

    void Update()
    {
        current = Mathf.MoveTowards(current, target, intensityOn / fadeTime * Time.deltaTime);
        float f = (flicker && current > 0.1f)
            ? Mathf.PerlinNoise(Time.time * 6f, transform.position.x) * 0.3f + 0.85f
            : 1f;
        Apply(current * f);
    }

    void Apply(float v)
    {
        if (lightSource) { lightSource.intensity = v; lightSource.enabled = v > 0.01f; }
        if (emissiveRenderer)
        {
            emissiveRenderer.material.EnableKeyword("_EMISSION");
            emissiveRenderer.material.SetColor("_EmissionColor", emissionColor * v);
        }
    }
}

