using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BattleLog : MonoBehaviour
{
    public static BattleLog Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect;
    private Queue<string> logMessages = new Queue<string>();
    private int maxMessages = 20;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddLog(string message)
    {
        if (Instance == null || this == null) 
        {
            // Debug.LogWarning("嘗試在非戰鬥場景寫入戰鬥日誌，已忽略。");
            return;
        }
        
        if (logMessages.Count >= maxMessages)
        {
            logMessages.Dequeue();
        }
        logMessages.Enqueue($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        UpdateLogText();
    }

    private void UpdateLogText()
    {
        if (this == null || logText == null || scrollRect == null) return;

        logText.text = string.Join("\n", logMessages);

        if(gameObject.activeInHierarchy)
        {
            StopAllCoroutines(); 
            StartCoroutine(ScrollToBottomCoroutine());
        }
    }

    private IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}