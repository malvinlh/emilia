using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSliderHandler : MonoBehaviour
{
    public Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
        // pastikan range slider 0–1
        slider.minValue = 0f;
        slider.maxValue = 1f;

        // isi dengan nilai terakhir dari PlayerPrefs
        slider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);

        // tambahkan listener
        slider.onValueChanged.AddListener(v => {
            AudioManager.Instance.SetMasterVolume(v);
        });
    }
}
