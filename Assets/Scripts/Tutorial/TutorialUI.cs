using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("主面板與容器")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Transform layoutContainer;

    [Header("導航按鈕")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;

    private GameObject currentLayoutInstance;
    private TutorialLayoutView currentLayoutView;

    private TutorialSO currentTutorial;
    private int currentStepIndex;
    private bool isReviewMode;

    void Awake()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.RegisterUI(this);
        }
        
        previousButton.onClick.AddListener(OnPreviousClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        skipButton.onClick.AddListener(OnSkipClicked);
        Hide();
    }

    public void Show(TutorialSO tutorial, bool reviewMode)
    {
        currentTutorial = tutorial;
        isReviewMode = reviewMode;
        currentStepIndex = 0;
        
        tutorialPanel.SetActive(true);
        DisplayStep(currentStepIndex);
    }

    public void Hide()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (currentLayoutInstance != null) Destroy(currentLayoutInstance);
    }

    private void DisplayStep(int index)
    {
        if (currentTutorial == null || index < 0 || index >= currentTutorial.steps.Count) return;

        currentStepIndex = index;
        TutorialStep step = currentTutorial.steps[currentStepIndex];

        if (currentLayoutInstance != null) Destroy(currentLayoutInstance);
        if (step.layoutPrefab != null)
        {
            currentLayoutInstance = Instantiate(step.layoutPrefab, layoutContainer);
            currentLayoutView = currentLayoutInstance.GetComponent<TutorialLayoutView>();
            if (currentLayoutView != null)
            {
                currentLayoutView.Populate(step);
            }
        }
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        previousButton.interactable = currentStepIndex > 0;

        if (currentStepIndex == currentTutorial.steps.Count - 1)
        {
            nextButtonText.text = isReviewMode ? "關閉" : "完成";
        }
        else
        {
            nextButtonText.text = "下一頁";
        }

        skipButton.gameObject.SetActive(!isReviewMode);
    }

    private void OnNextClicked()
    {
        if (currentStepIndex >= currentTutorial.steps.Count - 1)
        {
            FinishTutorial();
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

    private void OnSkipClicked()
    {
        FinishTutorial();
    }
    
    private void FinishTutorial()
    {
        TutorialManager.Instance.EndTutorial();
    }
}