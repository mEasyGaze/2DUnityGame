using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TutorialManager : MonoBehaviour, IGameSaveable
{
    public static TutorialManager Instance { get; private set; }
    private HashSet<string> completedTutorials = new HashSet<string>();
    private TutorialUI tutorialUI;
    private TutorialSO currentTutorial;
    private bool isReviewMode;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscape += HandleEscape;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscape -= HandleEscape;
        }
    }

    public void RegisterUI(TutorialUI ui) => tutorialUI = ui;

    public void ShowTutorial(string tutorialID, bool isReview = false)
    {
        if (completedTutorials.Contains(tutorialID) && !isReview)
        {
            Debug.Log($"教學 '{tutorialID}' 已完成，不再顯示。");
            return;
        }
        if (tutorialUI == null)
        {
            Debug.LogError("[TutorialManager] TutorialUI 未註冊，無法顯示教學！");
            return;
        }
        TutorialSO tutorial = TutorialDatabase.GetTutorialByID(tutorialID);
        if (tutorial == null || tutorial.steps.Count == 0) return;
        
        this.currentTutorial = tutorial;
        this.isReviewMode = isReview;
        
        if (!isReview)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.InTutorial);
            }
        }
        tutorialUI.Show(tutorial, isReview); 
    }

    public void EndTutorial()
    {
        if (tutorialUI != null) tutorialUI.Hide();
        if (!isReviewMode && currentTutorial != null)
        {
            MarkAsCompleted(currentTutorial.tutorialID);
            
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.InTutorial)
            {
                StartCoroutine(RestoreExplorationStateNextFrame());
            }
        }
        currentTutorial = null;
    }
    
    private IEnumerator RestoreExplorationStateNextFrame()
    {
        yield return null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Exploration);
        }
    }
    
    public void EndTutorial(TutorialSO tutorial, bool isReviewMode)
    {
        this.currentTutorial = tutorial;
        this.isReviewMode = isReviewMode;
        EndTutorial();
    }

    private void MarkAsCompleted(string tutorialID)
    {
        if (completedTutorials.Add(tutorialID))
        {
            Debug.Log($"[TutorialManager] 已將教學 '{tutorialID}' 標記為完成。");
        }
    }

    private void HandleEscape()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.InTutorial)
        {
            Debug.Log("[TutorialManager] 透過 ESC 鍵關閉教學。");
            EndTutorial();
        }
    }

    public HashSet<string> GetCompletedTutorials() => completedTutorials;
    
    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.tutorialData.completedTutorials = this.completedTutorials.ToList();
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        if (data.tutorialData != null && data.tutorialData.completedTutorials != null)
        {
            this.completedTutorials = new HashSet<string>(data.tutorialData.completedTutorials);
        }
        else
        {
            this.completedTutorials = new HashSet<string>();
        }
    }
    #endregion
}