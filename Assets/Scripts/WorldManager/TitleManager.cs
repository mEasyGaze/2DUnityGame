using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("主選單按鈕")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("UI 面板")]
    [Tooltip("請拖入場景中的 SaveLoadUI 物件 (預設應設為隱藏)")]
    [SerializeField] private SaveLoadUI saveLoadPanel;
    
    [Tooltip("請拖入設定面板物件 (預設應設為隱藏)")]
    [SerializeField] private GameObject settingsPanel;
    
    [Tooltip("設定面板中的關閉按鈕")]
    [SerializeField] private Button closeSettingsButton;

    [Header("遊戲設定")]
    [Tooltip("點擊新遊戲後要載入的第一個場景名稱 (例如 'Town_Beginner')")]
    [SerializeField] private string startingSceneName = "Town_Beginner";

    [SerializeField] private LoadingScreen loadingScreen; 

    private void Start()
    {
        newGameButton.onClick.AddListener(OnNewGameClicked);
        loadGameButton.onClick.AddListener(OnLoadGameClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
        }
        if (saveLoadPanel != null) saveLoadPanel.HidePanel();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableGameplayInput(true);
        }
        Time.timeScale = 1f;
        UISoundAutoHook.HookEntireScene();
    }

    private void OnNewGameClicked()
    {
        Debug.Log("開始新遊戲...");
        if (loadingScreen != null)
        {
            loadingScreen.Show();
            loadingScreen.UpdateProgress(0, "正在前往新世界...");
        }
        SceneManager.LoadScene(startingSceneName);
    }

    private void OnLoadGameClicked()
    {
        if (saveLoadPanel != null)
        {
            saveLoadPanel.ShowPanel(false); 
        }
        else
        {
            Debug.LogError("SaveLoadUI 未指定！");
        }
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.Log("設定功能尚未實作 (UI面板未指定)。");
        }
    }

    private void OnCloseSettingsClicked()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("退出遊戲");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}