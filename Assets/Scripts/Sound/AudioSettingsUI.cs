using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    
    void Start()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            masterSlider.value = PlayerPrefs.GetFloat("Vol_Master", 0.8f);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            musicSlider.value = PlayerPrefs.GetFloat("Vol_Music", 0.8f);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            sfxSlider.value = PlayerPrefs.GetFloat("Vol_SFX", 0.8f);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
        OnMasterVolumeChanged(masterSlider.value);
        OnMusicVolumeChanged(musicSlider.value);
        OnSFXVolumeChanged(sfxSlider.value);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
            PlayerPrefs.SetFloat("Vol_Master", value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            PlayerPrefs.SetFloat("Vol_Music", value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            PlayerPrefs.SetFloat("Vol_SFX", value);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        PlayerPrefs.Save();
    }
}