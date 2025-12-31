using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public enum AudioType { SFX, Music, UI, Ambience }

[System.Serializable]
public class SoundData
{
    [Tooltip("程式呼叫用的 ID，例如 'BGM_Battle', 'SFX_Sword_Hit'")]
    public string soundID;
    public AudioClip clip;
    
    [Header("細節設定")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("音高隨機變化範圍 (0 = 無變化, 0.1 = 輕微變化)。用於 SFX 避免機械感。")]
    [Range(0f, 0.5f)] public float pitchVariance = 0f;
    
    [Tooltip("指定輸出軌道 (Master/Music/SFX/UI)")]
    public AudioMixerGroup outputGroup;
    public bool loop = false;
}

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library")]
public class AudioLibrarySO : ScriptableObject
{
    public List<SoundData> sounds = new List<SoundData>();

    [System.NonSerialized]
    private Dictionary<string, SoundData> soundDictionary;
    
    public void Initialize()
    {
        soundDictionary = new Dictionary<string, SoundData>();
        
        if (sounds == null) return;

        foreach (var sound in sounds)
        {
            if (sound == null || string.IsNullOrEmpty(sound.soundID)) continue;

            if (!soundDictionary.ContainsKey(sound.soundID))
            {
                soundDictionary.Add(sound.soundID, sound);
            }
            else
            {
                Debug.LogWarning($"[AudioLibrary] 重複的 Sound ID: {sound.soundID}");
            }
        }
        Debug.Log($"[AudioLibrary] 已初始化，載入 {soundDictionary.Count} 個音效資料。");
    }

    public SoundData GetSound(string id)
    {
        if (soundDictionary == null) 
        {
            Initialize();
        }
        if (soundDictionary == null)
        {
            Debug.LogError("[AudioLibrary] 嚴重錯誤：字典初始化失敗！");
            return null;
        }
        if (soundDictionary.TryGetValue(id, out SoundData data))
        {
            return data;
        }
        Debug.LogWarning($"[AudioLibrary] 找不到音效 ID: {id}。請檢查 Library 設定或 ID 拼字。");
        return null;
    }
    
    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Initialize();
        }
        #endif
    }
}