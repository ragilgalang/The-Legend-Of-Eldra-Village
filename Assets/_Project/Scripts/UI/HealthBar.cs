using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] Health target;
    [SerializeField] Slider slider;

    void Start()
    {
        if (!target)
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (p) target = p.GetComponent<Health>();
        }
        if (!slider) slider = GetComponent<Slider>();
        if (!slider || !target) return;

        slider.maxValue = target.max;
        slider.value = target.Current;
        target.OnChanged += (cur, max) =>
        {
            if (slider)
            {
                slider.maxValue = max;
                slider.value = cur;
            }
        };
    }
}

