using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum QuestListenType { OnAccepted, OnCompleted, IsCompletable }

[System.Serializable]
public class QuestStateListenerState
{
    public bool hasAlreadyTriggered;
    // (可選) 如果 IsCompletable 類型的監聽器需要更精細的控制，可以把 hasTriggeredForCompletable 也放進來
    // public bool hasTriggeredForCompletable;
}

public class QuestStateListener : MonoBehaviour, ISceneSaveable
{
    [Header("監聽設定")]
    [Tooltip("要監聽的任務的唯一ID。")]
    [SerializeField] private string questIDToListenFor;
    
    [Tooltip("選擇要監聽的任務狀態事件類型。")]
    [SerializeField] private QuestListenType listenType = QuestListenType.OnCompleted;

    [Header("觸發行為")]
    [Tooltip("勾選此項，此監聽器在觸發一次後即失效，防止讀檔或重複滿足條件時重複觸發。")]
    [SerializeField] private bool triggerOnce = true;

    [Header("響應動作")]
    [Tooltip("當監聽到任務達到指定狀態時，要執行的動作。")]
    [SerializeField] private UnityEvent onStateMatched;

    private bool hasTriggeredForCompletable = false;
    private bool hasAlreadyTriggered_runtime = false;

    void OnEnable()
    {
        StartCoroutine(SubscribeAfterInitialization());
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    private IEnumerator SubscribeAfterInitialization()
    {
        yield return new WaitUntil(() => QuestManager.Instance != null && SaveManager.Instance != null);
        SubscribeToEvents();
        yield return null; 
        CheckInitialState();
    }

    private void SubscribeToEvents()
    {
        if (QuestManager.Instance == null) return;
        switch (listenType)
        {
            case QuestListenType.OnAccepted:
                QuestManager.Instance.OnQuestAccepted += HandleQuestStateChange;
                break;
            case QuestListenType.OnCompleted:
                QuestManager.Instance.OnQuestCompleted += HandleQuestStateChange;
                break;
            case QuestListenType.IsCompletable:
                QuestManager.Instance.OnQuestUpdated += HandleQuestStateChange;
                break;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (QuestManager.Instance == null) return;
        switch (listenType)
        {
            case QuestListenType.OnAccepted:
                QuestManager.Instance.OnQuestAccepted -= HandleQuestStateChange;
                break;
            case QuestListenType.OnCompleted:
                QuestManager.Instance.OnQuestCompleted -= HandleQuestStateChange;
                break;
            case QuestListenType.IsCompletable:
                QuestManager.Instance.OnQuestUpdated -= HandleQuestStateChange;
                break;
        }
    }

    private void CheckInitialState()
    {
        if (triggerOnce && hasAlreadyTriggered_runtime) return;
        bool isMatched = false;
        var questStatus = QuestManager.Instance.GetQuestStatus(questIDToListenFor);
        switch (listenType)
        {
            case QuestListenType.OnAccepted:
                isMatched = (questStatus == Quest.QuestStatus.InProgress || questStatus == Quest.QuestStatus.Completed);
                break;
            case QuestListenType.OnCompleted:
                isMatched = (questStatus == Quest.QuestStatus.Completed);
                break;
            case QuestListenType.IsCompletable:
                isMatched = (questStatus == Quest.QuestStatus.InProgress && QuestManager.Instance.IsQuestCompletable(questIDToListenFor));
                break;
        }

        if (isMatched)
        {
            HandleQuestStateChange(questIDToListenFor);
        }
    }

    private void HandleQuestStateChange(string changedQuestID)
    {
        if (changedQuestID != questIDToListenFor) return;
        if (triggerOnce && hasAlreadyTriggered_runtime)
        {
            return;
        }

        bool shouldTrigger = false;
        switch (listenType)
        {
            case QuestListenType.OnAccepted:
            case QuestListenType.OnCompleted:
                shouldTrigger = true;
                break;
            
            case QuestListenType.IsCompletable:
                if (QuestManager.Instance.IsQuestCompletable(questIDToListenFor) && !hasTriggeredForCompletable)
                {
                    shouldTrigger = true;
                    hasTriggeredForCompletable = true;
                }
                break;
        }

        if (shouldTrigger)
        {
            Debug.Log($"[QuestStateListener] 物件 '{gameObject.name}' 監聽到任務 '{changedQuestID}' 狀態匹配，正在執行響應動作。");
            onStateMatched.Invoke();
            
            hasAlreadyTriggered_runtime = true;
        }
    }
    
    public object CaptureState()
    {
        return new QuestStateListenerState
        {
            hasAlreadyTriggered = this.hasAlreadyTriggered_runtime
        };
    }

    public void RestoreState(object stateData)
    {
        if (stateData is string stateJson)
        {
            var state = JsonUtility.FromJson<QuestStateListenerState>(stateJson);
            this.hasAlreadyTriggered_runtime = state.hasAlreadyTriggered;
        }
    }
}