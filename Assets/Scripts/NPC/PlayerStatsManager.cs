using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class PlayerStatsManager : MonoBehaviour
{
    private static PlayerStatsManager _instance;

    public static PlayerStatsManager Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }
            
            _instance = FindObjectOfType<PlayerStatsManager>();
            
            if (_instance == null)
            {
                GameObject singletonObject = new GameObject("PlayerStatsManager (Auto-Generated)");
                _instance = singletonObject.AddComponent<PlayerStatsManager>();
                Debug.LogWarning("PlayerStatsManager was not found in the scene. A new instance has been auto-generated.");
            }
            return _instance;
        }
    }

    private PlayerStatsData statsData;
    private Dictionary<string, int> killCountDict = new Dictionary<string, int>();

    public static event System.Action<string, int> OnKillCountChanged;

    private readonly string fileName = "PlayerStats.json";

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadStats();
    }

    private void OnEnable()
    {
        BattleUnit.OnUnitDiedGlobal += HandleUnitDied;
    }

    private void OnDisable()
    {
        BattleUnit.OnUnitDiedGlobal -= HandleUnitDied;
    }

    private void HandleUnitDied(IBattleUnit_ReadOnly deadUnit)
    {
        if (!deadUnit.IsPlayerTeam && deadUnit.EnemyData != null)
        {
            AddKill(deadUnit.EnemyData.enemyID);
        }
    }

    public void AddKill(string enemyID, int amount = 1)
    {
        if (string.IsNullOrEmpty(enemyID)) return;

        if (killCountDict.ContainsKey(enemyID))
        {
            killCountDict[enemyID] += amount;
        }
        else
        {
            killCountDict[enemyID] = amount;
        }

        int newTotal = killCountDict[enemyID];
        Debug.Log($"[PlayerStatsManager] 擊殺數更新: {enemyID} - 當前總數: {newTotal}");

        OnKillCountChanged?.Invoke(enemyID, newTotal);
        // SaveStats(); 
    }

    public int GetKillCount(string enemyID)
    {
        if (killCountDict.TryGetValue(enemyID, out int count))
        {
            return count;
        }
        return 0;
    }

    #region 存檔與讀取
    public void SaveStats()
    {
        statsData.killCounters = killCountDict.Select(kvp => new KillCounterEntry { enemyID = kvp.Key, count = kvp.Value }).ToList();
        
        string json = JsonUtility.ToJson(statsData, true);
        File.WriteAllText(GetSavePath(), json);
        Debug.Log($"[PlayerStatsManager] 玩家統計數據已儲存至 {GetSavePath()}");
    }

    public void LoadStats()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            statsData = JsonUtility.FromJson<PlayerStatsData>(json);
        }
        else
        {
            statsData = new PlayerStatsData();
        }
        killCountDict = statsData.killCounters.ToDictionary(entry => entry.enemyID, entry => entry.count);
        Debug.Log("[PlayerStatsManager] 玩家統計數據已載入。");
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
    #endregion
}