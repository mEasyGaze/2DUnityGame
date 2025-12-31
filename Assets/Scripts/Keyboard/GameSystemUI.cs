using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameSystemUI : MonoBehaviour
{
    [Header("主面板")]
    [SerializeField] private GameObject systemMainPanel;

    [Header("子面板")]
    [SerializeField] private KeybindingUI keybindingPanel;
    [SerializeField] private TutorialLogUI tutorialLogPanel;
    [SerializeField] private SaveLoadUI saveLoadPanel;
    [SerializeField] private AudioSettingsUI audioSettingsPanel; 

    [Header("控制按鈕")]
    [SerializeField] private Button openKeybindingButton;
    [SerializeField] private Button openTutorialLogButton;
    [SerializeField] private Button openSaveButton;
    [SerializeField] private Button openLoadButton;
    [SerializeField] private Button openAudioSettingsButton;
    [SerializeField] private Button closeMainPanelButton;
    
    private List<GameObject> allSubPanels;

    void Start()
    {
        UISoundAutoHook.HookEntireScene();
    }

    void Awake()
    {
        allSubPanels = new List<GameObject>();
        if (keybindingPanel != null) allSubPanels.Add(keybindingPanel.gameObject);
        if (openAudioSettingsButton != null) 
        {
            openAudioSettingsButton.onClick.AddListener(ShowAudioSettingsPanel);
        }
        if (audioSettingsPanel != null) 
        {
            allSubPanels.Add(audioSettingsPanel.gameObject);
            audioSettingsPanel.Hide();
        }
        if (tutorialLogPanel != null) allSubPanels.Add(tutorialLogPanel.gameObject);
        if (saveLoadPanel != null)
        {
            allSubPanels.Add(saveLoadPanel.gameObject);
            saveLoadPanel.SetTitleScreenMode(false);
        }
        if (openKeybindingButton != null) openKeybindingButton.onClick.AddListener(ShowKeybindingPanel);
        if (openTutorialLogButton != null) openTutorialLogButton.onClick.AddListener(ShowTutorialLogPanel);
        if (openSaveButton != null) openSaveButton.onClick.AddListener(ShowSavePanel);
        if (openLoadButton != null) openLoadButton.onClick.AddListener(ShowLoadPanel);
        if (closeMainPanelButton != null) closeMainPanelButton.onClick.AddListener(CloseAllPanels);
        
        CloseAllPanels();
    }

    public void ToggleMainPanel()
    {
        bool isActive = systemMainPanel != null && systemMainPanel.activeSelf;
        if (isActive)
        {
            CloseAllPanels();
        }
        else
        {
            ShowMainPanel();
        }
    }

    private void ShowMainPanel()
    {
        if (systemMainPanel != null) systemMainPanel.SetActive(true);
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }
    }
    
    private void HideAllSubPanels()
    {
        foreach (var panel in allSubPanels)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }
        }
    }

    public void ShowKeybindingPanel()
    {
        HideAllSubPanels();
        if (keybindingPanel != null)
        {
            keybindingPanel.gameObject.SetActive(true);
        }
    }

    public void ShowAudioSettingsPanel()
    {
        HideAllSubPanels();
        if (audioSettingsPanel != null)
        {
            audioSettingsPanel.Show();
        }
    }

    public void ShowTutorialLogPanel()
    {
        HideAllSubPanels();
        if (tutorialLogPanel != null)
        {
            tutorialLogPanel.gameObject.SetActive(true);
        }
    }
    
    public void ShowSavePanel()
    {
        HideAllSubPanels();
        if (saveLoadPanel != null)
        {
            saveLoadPanel.ShowPanel(true); 
        }
    }

    public void ShowLoadPanel()
    {
        HideAllSubPanels();
        if (saveLoadPanel != null)
        {
            saveLoadPanel.ShowPanel(false);
        }
    }
    
    public void CloseAllPanels()
    {
        if (systemMainPanel != null) systemMainPanel.SetActive(false);
        HideAllSubPanels();
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Exploration);
        }
    }

    public void TryCloseSubPanels()
    {
        bool anySubPanelActive = false;
        if (keybindingPanel != null && keybindingPanel.gameObject.activeSelf) anySubPanelActive = true;
        if (tutorialLogPanel != null && tutorialLogPanel.gameObject.activeSelf) anySubPanelActive = true;
        if (saveLoadPanel != null && saveLoadPanel.gameObject.activeSelf) anySubPanelActive = true;
        if (anySubPanelActive)
        {
            HideAllSubPanels();
            if (keybindingPanel != null) keybindingPanel.ClosePanelWithoutSaving();
            if (tutorialLogPanel != null) tutorialLogPanel.TryCloseInternalPanels();
        }
        else
        {
            CloseAllPanels();
        }
    }
    
    public bool IsAnyPanelActive()
    {
        return systemMainPanel != null && systemMainPanel.activeSelf;
    }
}