using UnityEngine;

public class AudioBackground : MonoBehaviour
{
    public static AudioBackground Instance { get; private set; }

    [Header("背景音樂設定")]
    [Tooltip("進入此場景或啟用此物件時，要播放的音樂 ID")]
    [SerializeField] private string bgmID;
    
    [Tooltip("淡入淡出時間 (秒)。設為 0 代表瞬間切換。")]
    [SerializeField] private float fadeDuration = 2.0f;

    [Tooltip("是否在播放新音樂前，強制立即停止上一首音樂？(解決重疊問題)")]
    [SerializeField] private bool forceStopPrevious = false;

    [Tooltip("是否在 Start 時自動播放")]
    [SerializeField] private bool playOnStart = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (playOnStart)
        {
            PlayBGM();
        }
    }

    public void PlayBGM()
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(bgmID)) return;
        if (AudioManager.Instance.CurrentMusicID == bgmID && AudioManager.Instance.IsMusicPlaying())
        {
            return;
        }

        if (forceStopPrevious)
        {
            AudioManager.Instance.StopMusic(0f);
        }
        AudioManager.Instance.PlayMusic(bgmID, fadeDuration);
    }
    
    public void ChangeBGM(string newID)
    {
        bgmID = newID;
        PlayBGM();
    }

    public void RestoreBGM()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.CurrentMusicID != bgmID)
        {
            Debug.Log($"[AudioBackground] 恢復場景音樂: {bgmID}");
            PlayBGM();
        }
    }
}