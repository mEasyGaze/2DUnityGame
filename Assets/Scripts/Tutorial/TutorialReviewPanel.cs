using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialReviewPanel : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject reviewPanel;
    [SerializeField] private Transform layoutContainer;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    [SerializeField] private Button closeButton;

    private TutorialSO currentTutorial;
    private int currentStepIndex;
    private GameObject currentLayoutInstance;

    public System.Action OnReviewClosed;

    void Awake()
    {
        previousButton.onClick.AddListener(OnPreviousClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }
    
    public void PopulateWithData(TutorialSO tutorial)
    {
        if (tutorial == null || tutorial.steps.Count == 0) return;

        currentTutorial = tutorial;
        currentStepIndex = 0;
        
        DisplayStep(currentStepIndex);
    }

    public void ClearAndNotify()
    {
        if (currentLayoutInstance != null)
        {
            Destroy(currentLayoutInstance);
            currentLayoutInstance = null;
        }
        OnReviewClosed?.Invoke();
    }

    private void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
        ClearAndNotify();
    }
    
    private void DisplayStep(int index)
    {
        if (currentTutorial == null || index < 0 || index >= currentTutorial.steps.Count) return;
        currentStepIndex = index;
        TutorialStep step = currentTutorial.steps[currentStepIndex];
        if (currentLayoutInstance != null)
        {
            Destroy(currentLayoutInstance);
        }
        if (step.layoutPrefab != null)
        {
            currentLayoutInstance = Instantiate(step.layoutPrefab, layoutContainer);
            var layoutView = currentLayoutInstance.GetComponent<TutorialLayoutView>();
            if (layoutView != null) layoutView.Populate(step);
        }
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        previousButton.interactable = currentStepIndex > 0;
        nextButtonText.text = (currentStepIndex == currentTutorial.steps.Count - 1) ? "完成" : "下一頁";
    }

    private void OnNextClicked()
    {
        if (currentStepIndex >= currentTutorial.steps.Count - 1)
        {
            OnCloseButtonClicked(); 
        }
        else
        {
            DisplayStep(currentStepIndex + 1);
        }
    }

    private void OnPreviousClicked()
    {
        if (currentStepIndex > 0)
        {
            DisplayStep(currentStepIndex - 1);
        }
    }
}