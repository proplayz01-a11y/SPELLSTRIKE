using UnityEngine;
using UnityEngine.UI;

/// <summary>Small functional audio control for the isolated V2 Options preview.</summary>
public sealed class MenuV2VolumeSetting : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    public void Configure(Slider slider) => volumeSlider = slider;

    private void OnEnable()
    {
        if (volumeSlider == null) return;
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SpellStrike_MainVolume", 1f));
        Apply(volumeSlider.value);
        volumeSlider.onValueChanged.AddListener(Apply);
    }

    private void OnDisable()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(Apply);
    }

    private static void Apply(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SpellStrike_MainVolume", AudioListener.volume);
    }
}
