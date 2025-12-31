using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public string CurrentMusicID { get; private set; }

    [Header("設定")]
    [SerializeField] private AudioLibrarySO audioLibrary;
    [SerializeField] private AudioMixer audioMixer;

    [Header("音源配置")]
    [SerializeField] private AudioSource musicSource1;
    [SerializeField] private AudioSource musicSource2;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private GameObject sfxSourcePrefab;
    [SerializeField] private int sfxPoolSize = 10;

    private List<AudioSource> sfxPool;
    private bool isMusicSource1Playing = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioLibrary != null) audioLibrary.Initialize();
        InitializeSFXPool();
    }

    void Start()
    {
        float masterVol = PlayerPrefs.GetFloat("Vol_Master", 0.8f);
        float musicVol = PlayerPrefs.GetFloat("Vol_Music", 0.8f);
        float sfxVol = PlayerPrefs.GetFloat("Vol_SFX", 0.8f);

        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    private void InitializeSFXPool()
    {
        sfxPool = new List<AudioSource>();
        GameObject poolParent = new GameObject("SFX_Pool");
        poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            CreateSFXSource(poolParent.transform);
        }
    }

    private AudioSource CreateSFXSource(Transform parent)
    {
        GameObject go = Instantiate(sfxSourcePrefab, parent);
        AudioSource source = go.GetComponent<AudioSource>();
        go.SetActive(false);
        sfxPool.Add(source);
        return source;
    }

    #region 播放方法 (API)
    public void PlaySFX(string soundID, Vector3 position = default)
    {
        SoundData data = audioLibrary.GetSound(soundID);
        if (data == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.clip = data.clip;
        source.outputAudioMixerGroup = data.outputGroup;
        source.volume = data.volume;
        source.pitch = 1f + Random.Range(-data.pitchVariance, data.pitchVariance);
        
        source.gameObject.SetActive(true);
        source.Play();
        StartCoroutine(DisableSourceAfterPlay(source));
    }

    public void PlayUI(string soundID)
    {
        SoundData data = audioLibrary.GetSound(soundID);
        if (data == null) return;

        uiSource.outputAudioMixerGroup = data.outputGroup;
        uiSource.volume = data.volume;
        uiSource.pitch = 1f + Random.Range(-data.pitchVariance, data.pitchVariance);
        uiSource.PlayOneShot(data.clip);
    }

    public void PlayMusic(string soundID, float fadeDuration = 1.5f)
    {
        if (CurrentMusicID == soundID && IsMusicPlaying()) return;
        SoundData data = audioLibrary.GetSound(soundID);
        if (data == null) return;
        CurrentMusicID = soundID;

        AudioSource activeSource = isMusicSource1Playing ? musicSource1 : musicSource2;
        AudioSource newSource = isMusicSource1Playing ? musicSource2 : musicSource1;

        StartCoroutine(CrossFadeMusic(activeSource, newSource, data, fadeDuration));
        isMusicSource1Playing = !isMusicSource1Playing;
    }

    public void StopMusic(float fadeDuration = 1.0f)
    {
        CurrentMusicID = null;
        AudioSource activeSource = isMusicSource1Playing ? musicSource1 : musicSource2;
        StartCoroutine(FadeOut(activeSource, fadeDuration));
    }

    public void PauseMusic()
    {
        if (musicSource1.isPlaying) musicSource1.Pause();
        if (musicSource2.isPlaying) musicSource2.Pause();
    }

    public void ResumeMusic()
    {
        musicSource1.UnPause();
        musicSource2.UnPause();
    }

    public bool IsMusicPlaying()
    {
        AudioSource activeSource = isMusicSource1Playing ? musicSource1 : musicSource2;
        return activeSource.isPlaying;
    }
    #endregion

    #region 內部邏輯 & Coroutines
    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.gameObject.activeInHierarchy)
            {
                return source;
            }
        }
        return CreateSFXSource(sfxPool[0].transform.parent);
    }

    private IEnumerator DisableSourceAfterPlay(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length + 0.1f);
        source.gameObject.SetActive(false);
    }

    private IEnumerator CrossFadeMusic(AudioSource current, AudioSource next, SoundData nextData, float duration)
    {
        next.clip = nextData.clip;
        next.outputAudioMixerGroup = nextData.outputGroup;
        next.volume = 0;
        next.loop = nextData.loop;
        next.Play();

        float timer = 0;
        float startVolume = current.volume;
        float targetVolume = nextData.volume;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float progress = timer / duration;
            if (current.isPlaying) current.volume = Mathf.Lerp(startVolume, 0, progress);
            
            next.volume = Mathf.Lerp(0, targetVolume, progress);
            yield return null;
        }
        current.Stop();
        current.volume = startVolume;
        next.volume = targetVolume;
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0, timer / duration);
            yield return null;
        }
        source.Stop();
        source.volume = startVolume;
    }
    #endregion

    #region 設定 (Settings)
    public void SetMasterVolume(float value)
    {
        float db = (value <= 0.001f) ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("MasterVolume", db);
    }
    public void SetMusicVolume(float value)
    {
        float db = (value <= 0.001f) ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("MusicVolume", db);
    }
    public void SetSFXVolume(float value)
    {
        float db = (value <= 0.001f) ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("SFXVolume", db);
    }
    #endregion
}