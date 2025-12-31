using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialLogUI : MonoBehaviour
{
    [Header("主面板與視圖")]
    [SerializeField] private GameObject buttonListView; 
    [SerializeField] private TutorialReviewPanel reviewPanel; 

    [Header("按鈕列表元件")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject logEntryPrefab;
    [SerializeField] private Button closeButton;
    
    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        if (reviewPanel != null)
        {
            reviewPanel.OnReviewClosed += OnReviewPanelClosed;
        }
    }

    private void OnEnable()
    {
        ShowButtonList();
        RefreshLogList();
    }
    
    public bool TryCloseInternalPanels()
    {
        if (reviewPanel != null && reviewPanel.gameObject.activeSelf)
        {
            reviewPanel.gameObject.SetActive(false);
            reviewPanel.ClearAndNotify();
            return true;
        }
        return false;
    }

    private void RefreshLogList()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        HashSet<string> completedIDs = TutorialManager.Instance.GetCompletedTutorials();

        foreach (string tutorialID in completedIDs)
        {
            TutorialSO tutorial = TutorialDatabase.GetTutorialByID(tutorialID);
            if (tutorial == null || string.IsNullOrEmpty(tutorial.tutorialTitle)) continue;

            GameObject entryGO = Instantiate(logEntryPrefab, contentContainer);
            entryGO.GetComponentInChildren<TextMeshProUGUI>().text = tutorial.tutorialTitle;
            
            Button entryButton = entryGO.GetComponent<Button>();
            entryButton.onClick.AddListener(() => {
                ShowReviewFor(tutorial);
            });
        }
    }
    
    private void ShowReviewFor(TutorialSO tutorial)
    {
        if (buttonListView != null) buttonListView.SetActive(false); 
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        if (reviewPanel != null)
        {
            reviewPanel.PopulateWithData(tutorial);
            reviewPanel.gameObject.SetActive(true);
        }
    }
    
    private void ShowButtonList()
    {
        if (buttonListView != null) buttonListView.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        if (reviewPanel != null)
        {
            reviewPanel.gameObject.SetActive(false);
        }
    }

    private void OnReviewPanelClosed()
    {
        ShowButtonList();
    }

    private void OnDestroy()
    {
        if (reviewPanel != null) reviewPanel.OnReviewClosed -= OnReviewPanelClosed;
    }
}