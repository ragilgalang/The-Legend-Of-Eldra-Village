using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] Health target;
    [SerializeField] Slider slider;

    void Start()
    {
        slider.maxValue = target.max;
        slider.value = target.Current;
        target.OnChanged += (cur, max) => { slider.maxValue = max; slider.value = cur; };
    }
}

