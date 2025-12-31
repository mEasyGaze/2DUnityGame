using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class ExplorationUIManager : MonoBehaviour
{
    public static ExplorationUIManager Instance { get; private set; }

    [Header("UI 預製件")]
    [SerializeField] private ExploreProgressBar progressBarPrefab;
    
    [Header("UI 畫布標籤")]
    [Tooltip("請確保您場景中的主UI畫布(Canvas)被設置了這個標籤(Tag)")]
    [SerializeField] private string uiCanvasTag = "UICanvas";
    private Canvas parentCanvas;

    [Header("定位設定")]
    [Tooltip("進度條相對於目標物件的屏幕位置偏移量 (像素)")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0, 50f);

    private ExploreProgressBar activeProgressBar;
    private Action onProgressCompleteCallback;
    public bool IsProgressBarActive { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindCanvas();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[ExplorationUIManager] 偵測到新場景 '{scene.name}' 已加載，正在重新尋找Canvas...");
        FindCanvas();
    }
    
    private void FindCanvas()
    {
        GameObject canvasGO = GameObject.FindWithTag(uiCanvasTag);
        if (canvasGO != null)
        {
            parentCanvas = canvasGO.GetComponent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError($"[ExplorationUIManager] 找到了帶有 '{uiCanvasTag}' 標籤的物件，但它上面沒有 Canvas 組件！");
            }
        }
        else
        {
            Debug.LogError($"[ExplorationUIManager] 在場景中找不到帶有 '{uiCanvasTag}' 標籤的 Canvas！請檢查您的場景設置。");
        }
    }

    public void StartProgressBar(Transform targetTransform, float duration, Action onComplete, string actionText)
    {
        if (parentCanvas == null)
        {
            Debug.LogError("[ExplorationUIManager] 無法啟動進度條，因為 parentCanvas 為空！");
            FindCanvas();
            if (parentCanvas == null) return;
        }

        if (activeProgressBar != null && activeProgressBar.gameObject.activeInHierarchy)
        {
            activeProgressBar.Cancel();
        }
        
        if (activeProgressBar == null || activeProgressBar.transform.parent != parentCanvas.transform)
        {
            if (activeProgressBar != null) Destroy(activeProgressBar.gameObject);
            activeProgressBar = Instantiate(progressBarPrefab, parentCanvas.transform);
        }
        
        onProgressCompleteCallback = onComplete;
        activeProgressBar.gameObject.SetActive(true);
        
        IsProgressBarActive = true;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetTransform.position);

        activeProgressBar.transform.position = (Vector2)screenPosition + positionOffset;
        activeProgressBar.StartProgress(duration, actionText);
    }
    
    public void OnProgressComplete()
    {
        IsProgressBarActive = false;
        onProgressCompleteCallback?.Invoke();
        onProgressCompleteCallback = null;
    }

    public void CancelCurrentProgress()
    {
        if (IsProgressBarActive && activeProgressBar != null)
        {
            activeProgressBar.Cancel();
            IsProgressBarActive = false;
            onProgressCompleteCallback = null; 
            Debug.Log("[ExplorationUIManager] 進度條已被取消。");
        }
    }
}