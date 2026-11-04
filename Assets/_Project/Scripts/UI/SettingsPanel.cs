using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] Slider volumeSlider, sensitivitySlider;

    const string VolKey = "volume", SensKey = "sensitivity";

    void Start()
    {
        volumeSlider.minValue = 0f;       volumeSlider.maxValue = 1f;
        sensitivitySlider.minValue = 0.03f; sensitivitySlider.maxValue = 0.4f;

        volumeSlider.value = PlayerPrefs.GetFloat(VolKey, 0.8f);
        sensitivitySlider.value = PlayerPrefs.GetFloat(SensKey, 0.12f);

        volumeSlider.onValueChanged.AddListener(SetVolume);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        SetVolume(volumeSlider.value);
        SetSensitivity(sensitivitySlider.value);
    }

    void SetVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolKey, v);
    }

    void SetSensitivity(float s)
    {
        if (CameraRig.Instance) CameraRig.Instance.Sensitivity = s;   // hanya ada di scene game
        PlayerPrefs.SetFloat(SensKey, s);
    }
}

