using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class WorldTimeSystem : MonoBehaviour, IGameSaveable
{
    public static WorldTimeSystem Instance { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public static event Action<int> OnDayChanged;

    [SerializeField] private float secondsPerDay = 60f;
    private float dayTimer = 0f;
    
    private bool isLoading = false;

    private readonly string[] timePausedScenes = new string[] { "BattleScene", "Title", "TitleScene" }; // 請確認您的標題場景名稱是 "Title" 還是 "TitleScene"

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SaveManager.Instance.Register(this);
    }

    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPause = false;
        foreach (string sceneName in timePausedScenes)
        {
            if (scene.name == sceneName)
            {
                shouldPause = true;
                break;
            }
        }
        if (shouldPause)
        {
            this.enabled = false;
            Debug.Log($"[WorldTimeSystem] 進入 {scene.name}，時間系統已暫停。");
        }
        else
        {
            this.enabled = true;
            Debug.Log($"[WorldTimeSystem] 進入 {scene.name}，時間系統繼續運行。");
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameState.Exploration)
        {
            return;
        }
        dayTimer += Time.deltaTime;
        if (dayTimer >= secondsPerDay)
        {
            dayTimer -= secondsPerDay;
            AdvanceDay();
        }
    }

    private void AdvanceDay()
    {
        CurrentDay++;
        Debug.Log($"[WorldTimeSystem] 新的一天開始了！今天是第 {CurrentDay} 天。");
        if (!isLoading) OnDayChanged?.Invoke(CurrentDay);
    }

    public void PassDays(int days)
    {
        for (int i = 0; i < days; i++)
        {
            AdvanceDay();
        }
    }

    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.worldTimeData.currentDay = this.CurrentDay;
        data.worldTimeData.dayTimer = this.dayTimer;
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true; 
        
        if (data.worldTimeData != null)
        {
            this.CurrentDay = data.worldTimeData.currentDay;
            this.dayTimer = data.worldTimeData.dayTimer;
        }
        isLoading = false;
    }
    #endregion
}