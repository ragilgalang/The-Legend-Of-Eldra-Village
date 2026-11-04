using TMPro;
using UnityEngine;

public class ClockUI : MonoBehaviour
{
    [SerializeField] TMP_Text timeText;
    [SerializeField] RectTransform dial;   // opsional: ikon matahari/bulan yang berputar

    static readonly string[] phaseNames = { "Siang", "Senja", "Malam", "Fajar" };

    void Update()
    {
        var c = DayNightCycle.Instance;
        int h = Mathf.FloorToInt(c.Hour);
        int m = Mathf.FloorToInt((c.Hour - h) * 60f);
        timeText.text = $"Hari {c.DayCount}  {h:00}:{m:00}  ({phaseNames[(int)c.Phase]})";
        if (dial) dial.localRotation = Quaternion.Euler(0, 0, -c.Hour / 24f * 360f);
    }
}

