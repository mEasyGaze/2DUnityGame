using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class SaveManager : MonoBehaviour, IGameSaveable
{
    public static SaveManager Instance { get; private set; }
    public static event Action OnGameLoadComplete;
    private readonly string fileNameTemplate = "SaveSlot_{0}.json";
    private GameSaveData currentSessionData; 
    private List<IGameSaveable> saveableEntities = new List<IGameSaveable>();
    private LoadingScreen loadingScreen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Register(this);
        
        if (currentSessionData == null)
        {
            currentSessionData = new GameSaveData();
        }
    }

    #region 註冊機制
    public void Register(IGameSaveable entity)
    {
        if (!saveableEntities.Contains(entity))
        {
            saveableEntities.Add(entity);
        }
    }

    public void Unregister(IGameSaveable entity)
    {
        saveableEntities.Remove(entity);
    }
    #endregion

    private string GetSavePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, string.Format(fileNameTemplate, slotIndex));
    }

    public void SaveSceneStateToMemory()
    {
        if (currentSessionData == null) currentSessionData = new GameSaveData();
        foreach (var entity in saveableEntities)
        {
            entity.PopulateSaveData(currentSessionData);
        }
        if (ScenePersistenceManager.Instance != null)
        {
            ScenePersistenceManager.Instance.PopulateSaveData(currentSessionData);
        }
        Debug.Log("<color=yellow>[SaveManager]</color> 已將當前場景狀態保存到內存。");
    }

    public void LoadSceneStateFromMemory()
    {
        if (currentSessionData == null) return;
        
        foreach (var entity in saveableEntities)
        {
            entity.LoadFromSaveData(currentSessionData);
        }
        
        if (ScenePersistenceManager.Instance != null)
        {
            ScenePersistenceManager.Instance.LoadFromSaveData(currentSessionData);
        }
        Debug.Log("<color=yellow>[SaveManager]</color> 已從內存恢復當前場景狀態。");
    }

    public void SaveGame(int slotIndex)
    {
        SaveSceneStateToMemory();
        currentSessionData.gameVersion = Application.version;
        currentSessionData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        currentSessionData.sceneNames.Clear();
        currentSessionData.sceneSaveDataList.Clear();
        foreach(var pair in currentSessionData.sceneData)
        {
            pair.Value.OnBeforeSerialize();
            currentSessionData.sceneNames.Add(pair.Key);
            currentSessionData.sceneSaveDataList.Add(pair.Value);
        }
        string json = JsonUtility.ToJson(currentSessionData, true);
        string path = GetSavePath(slotIndex);
        string backupPath = path + ".bak";
        try
        {
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, true);
            }
            File.WriteAllText(path, json);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            Debug.Log($"<color=cyan>[SaveManager]</color> 遊戲已安全儲存至槽位 {slotIndex}。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 保存至槽位 {slotIndex} 時發生嚴重錯誤: {e.Message}");
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, path, true);
            }
        }
    }

    public void LoadGame(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        string backupPath = path + ".bak";

        if (!File.Exists(path) && File.Exists(backupPath))
        {
            Debug.LogWarning($"[SaveManager] 找不到主存檔，但發現備份檔案。正在嘗試從備份恢復...");
            File.Copy(backupPath, path, true);
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"找不到槽位 {slotIndex} 的存檔檔案！");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            currentSessionData = JsonUtility.FromJson<GameSaveData>(json);
            currentSessionData.sceneData = new Dictionary<string, SceneSaveData>();
            if (currentSessionData.sceneNames != null && currentSessionData.sceneSaveDataList != null)
            {
                for(int i = 0; i < currentSessionData.sceneNames.Count; i++)
                {
                    if (i < currentSessionData.sceneSaveDataList.Count)
                    {
                        currentSessionData.sceneSaveDataList[i].OnAfterDeserialize();
                        currentSessionData.sceneData[currentSessionData.sceneNames[i]] = currentSessionData.sceneSaveDataList[i];
                    }
                }
            }
            StartCoroutine(LoadSceneAndApplyData());
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 讀取槽位 {slotIndex} 存檔失敗，檔案可能已損壞: {e.Message}");
            currentSessionData = null;
        }
    }

    public void DeleteSaveFile(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"<color=orange>[SaveManager]</color> 已刪除槽位 {slotIndex} 的存檔檔案。");
        }
    }

    private IEnumerator LoadSceneAndApplyData()
    {
        loadingScreen = FindObjectOfType<LoadingScreen>(true);
        loadingScreen?.Show();
        loadingScreen?.UpdateProgress(0.1f, "正在載入場景...");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentSessionData.worldData.sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            loadingScreen?.UpdateProgress(0.1f + asyncLoad.progress * 0.5f, "正在構建世界...");
            yield return null;
        }
        
        loadingScreen?.UpdateProgress(0.6f, "準備完成...");
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        yield return new WaitForEndOfFrame(); 

        loadingScreen?.UpdateProgress(0.7f, "正在恢復進度...");
        Debug.Log("<color=yellow>[SaveManager]</color> 開始靜默數據恢復...");
        
        LoadSceneStateFromMemory();
        
        Debug.Log("<color=yellow>[SaveManager]</color> 靜默數據恢復完成。");
        loadingScreen?.UpdateProgress(0.9f, "正在同步狀態...");
        Debug.Log("<color=green>[SaveManager]</color> 廣播 OnGameLoadComplete 事件...");
        OnGameLoadComplete?.Invoke();
        Debug.Log("<color=green>[SaveManager]</color> 全局刷新事件已廣播。");

        if (currentSessionData.worldData != null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.SetPosition(currentSessionData.worldData.playerPosition);
            }
        }

        loadingScreen?.UpdateProgress(1.0f, "載入完成！");
        yield return new WaitForSecondsRealtime(0.5f);
        if (loadingScreen != null)
        {
            yield return StartCoroutine(loadingScreen.FadeOut(0.5f));
        }
        Debug.Log("<color=cyan>[SaveManager]</color> 遊戲讀取完成！");
    }
    
    public bool DoesSaveFileExist(int slotIndex)
    {
        return File.Exists(GetSavePath(slotIndex));
    }

    public GameSaveData GetSaveFileSummary(int slotIndex)
    {
        if (!DoesSaveFileExist(slotIndex)) return null;
        try
        {
            string json = File.ReadAllText(GetSavePath(slotIndex));
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 讀取槽位 {slotIndex} 的存檔摘要時失敗: {e.Message}");
            return null;
        }
    }

    #region 世界數據
    public void PopulateSaveData(GameSaveData data)
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            data.worldData.sceneName = SceneManager.GetActiveScene().name;
            data.worldData.playerPosition = player.transform.position;
        }
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        if (data.worldData != null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.SetPosition(data.worldData.playerPosition);
                Debug.Log($"[SaveManager] 已恢復玩家位置至: {data.worldData.playerPosition}");
            }
        }
    }
    #endregion
}