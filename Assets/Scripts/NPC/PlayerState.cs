using UnityEngine;
using System;

public class PlayerState : MonoBehaviour, IGameSaveable
{
    public static PlayerState Instance { get; private set; }

    [Header("玩家狀態")]
    [SerializeField] private int money = 100;
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int experienceToNextLevel = 100;

    public static event Action<int> OnMoneyChanged;
    public static event Action<int> OnLevelChanged;
    public static event Action<int, int> OnExperienceChanged;
    
    private bool isLoading = false;

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

    void Start()
    {
        if (!isLoading)
        {
            OnMoneyChanged?.Invoke(money);
            OnLevelChanged?.Invoke(level);
            OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel);
        }
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        Debug.Log($"[PlayerState] 獲得金錢：{amount}。目前總額：{money}");
        if (!isLoading) OnMoneyChanged?.Invoke(money);
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || money < amount)
        {
            if (money < amount && amount > 0)
            {
                Debug.LogWarning($"[PlayerState] 金錢不足！需要 {amount}, 但只有 {money}。");
            }
            return false;
        }
        money -= amount;
        Debug.Log($"[PlayerState] 花費金錢：{amount}。目前餘額：{money}");
        if (!isLoading) OnMoneyChanged?.Invoke(money);
        return true;
    }

    public void GainExperience(int amount)
    {
        if (amount <= 0) return;
        currentExperience += amount;
        Debug.Log($"[PlayerState] 獲得經驗值：{amount}。目前經驗：{currentExperience}/{experienceToNextLevel}");
        while (currentExperience >= experienceToNextLevel)
        {
            currentExperience -= experienceToNextLevel;
            LevelUp();
        }
        if (!isLoading) OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel);
    }

    private void LevelUp()
    {
        level++;
        experienceToNextLevel += 50; 
        Debug.LogWarning($"[PlayerState] 等級提升！目前等級: {level}");
        if (!isLoading) OnLevelChanged?.Invoke(level); 
        if (!isLoading) OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel); 
    }

    public int GetCurrentMoney() => money;
    public int GetCurrentLevel() => level;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceToNextLevel() => experienceToNextLevel;
    
    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.playerStateData.money = this.money;
        data.playerStateData.level = this.level;
        data.playerStateData.currentExperience = this.currentExperience;
        data.playerStateData.experienceToNextLevel = this.experienceToNextLevel;
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true;
        
        this.money = data.playerStateData.money;
        this.level = data.playerStateData.level;
        this.currentExperience = data.playerStateData.currentExperience;
        this.experienceToNextLevel = data.playerStateData.experienceToNextLevel;

        isLoading = false;
    }
    #endregion
}