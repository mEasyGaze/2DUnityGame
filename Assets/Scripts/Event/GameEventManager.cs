using UnityEngine;
using System.Collections.Generic;
using System;

public class GameEventManager : MonoBehaviour, IGameSaveable
{
    public static GameEventManager Instance { get; private set; }
    private HashSet<string> triggeredEvents = new HashSet<string>();
    public static event Action<string> OnEventTriggered;

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

    public void TriggerEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return;

        if (triggeredEvents.Add(eventID))
        {
            Debug.Log($"[GameEventManager] 遊戲事件觸發: {eventID}");
            if (!isLoading)
            {
                OnEventTriggered?.Invoke(eventID);
            }
        }
    }

    public bool HasEventBeenTriggered(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;
        return triggeredEvents.Contains(eventID);
    }

    public HashSet<string> GetTriggeredEvents() => triggeredEvents;

    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.worldData.triggeredEvents = new List<string>(this.triggeredEvents);
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true;
        
        if (data.worldData != null && data.worldData.triggeredEvents != null)
        {
            this.triggeredEvents = new HashSet<string>(data.worldData.triggeredEvents);
        }
        else
        {
            this.triggeredEvents = new HashSet<string>();
        }
        isLoading = false;
    }
    #endregion
}