using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class GameEventListener : MonoBehaviour
{
    [Header("事件監聽設定")]
    [Tooltip("此監聽器要響應的遊戲事件的唯一ID。")]
    [SerializeField] private string eventIDToListenFor;

    [Header("觸發行為控制")]
    [Tooltip("勾選此項，此監聽器只會成功觸發一次。之後即使再監聽到相同事件，也不會執行動作。")]
    [SerializeField] private bool triggerOnceGlobally = true;

    [Tooltip("勾選此項，當物件初始化時，如果該事件曾經發生過，則立即執行響應動作（例如銷毀物件）。這對於場景狀態恢復非常重要。")]
    [SerializeField] private bool checkOnStart = true;

    [Header("可選：直接觸發教學")]
    [Tooltip("（可選）當事件觸發時，直接啟動此ID對應的教學。")]
    [SerializeField] private string tutorialIDToTrigger;
    
    [Tooltip("如果觸發了教學，此監聽器是否應只觸發一次？")]
    [SerializeField] private bool triggerTutorialOnce = true;

    [Header("響應動作")]
    [Tooltip("當監聽到對應的事件ID時，要執行的通用動作。")]
    [SerializeField] private UnityEvent onEventTriggered;

    private bool hasTriggeredTutorial = false;

    private void OnEnable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.OnEventTriggered += HandleEventTriggered;
        }
        else
        {
            StartCoroutine(DelayedSubscribe());
        }
    }

    private void Start()
    {
        if (checkOnStart)
        {
            StartCoroutine(CheckStateOnStart());
        }
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.OnEventTriggered -= HandleEventTriggered;
        }
    }

    private IEnumerator DelayedSubscribe()
    {
        yield return new WaitUntil(() => GameEventManager.Instance != null);
        GameEventManager.OnEventTriggered += HandleEventTriggered;
    }

    private IEnumerator CheckStateOnStart()
    {
        yield return new WaitUntil(() => GameEventManager.Instance != null && SaveManager.Instance != null);
        yield return null;
        if (GameEventManager.Instance.HasEventBeenTriggered(eventIDToListenFor))
        {
            if (triggerOnceGlobally)
            {
                Debug.Log($"[GameEventListener] 初始化檢查：事件 '{eventIDToListenFor}' 歷史記錄中已存在，立即執行響應動作。");
                onEventTriggered.Invoke();
            }
        }
    }

    private void HandleEventTriggered(string triggeredID)
    {
        if (triggeredID == eventIDToListenFor)
        {
            if (triggerOnceGlobally)
            {
                string listenerRespondedEventID = $"Listener_{gameObject.scene.name}_{gameObject.name}_{eventIDToListenFor}";
                if (GameEventManager.Instance.HasEventBeenTriggered(listenerRespondedEventID))
                {
                    Debug.Log($"[GameEventListener] '{gameObject.name}' 監聽到事件 '{triggeredID}'，但記錄顯示它已響應過，已忽略。");
                    return;
                }
                GameEventManager.Instance.TriggerEvent(listenerRespondedEventID);
            }
            Debug.Log($"[GameEventListener] 物件 '{gameObject.name}' 監聽到事件 '{triggeredID}'，並執行響應動作。");
            onEventTriggered.Invoke();
            
            if (!string.IsNullOrEmpty(tutorialIDToTrigger))
            {
                if (triggerTutorialOnce && hasTriggeredTutorial)
                {
                    return;
                }
                if (TutorialManager.Instance != null && !TutorialManager.Instance.GetCompletedTutorials().Contains(tutorialIDToTrigger))
                {
                    Debug.Log($"...並直接觸發教學 '{tutorialIDToTrigger}'。");
                    TutorialManager.Instance.ShowTutorial(tutorialIDToTrigger);
                }
                hasTriggeredTutorial = true;
            }
        }
    }
}