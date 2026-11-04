using UnityEngine;
using UnityEngine.UI;

public class DamageVignette : MonoBehaviour
{
    [SerializeField] Health player;
    [SerializeField] Image overlay;
    [SerializeField] float peakAlpha = 0.45f, fadeSpeed = 2f;

    float alpha, lastHp;

    void Start()
    {
        lastHp = player.Current;
        player.OnChanged += (cur, max) =>
        {
            if (cur < lastHp) alpha = peakAlpha;
            lastHp = cur;
        };
    }

    void Update()
    {
        alpha = Mathf.MoveTowards(alpha, 0f, fadeSpeed * Time.deltaTime);
        var c = overlay.color; c.a = alpha; overlay.color = c;
    }
}

