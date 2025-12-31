using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveLoadUI : MonoBehaviour
{
    [Header("UI 連結")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private ConfirmationPanelUI confirmationPanel;

    private bool isSaveMode = true;
    private bool isTitleScreenMode = false;
    private const int MAX_SLOTS = 3;

    void Awake()
    {
        closeButton.onClick.AddListener(HidePanel);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.Hide();
    }

    public void SetTitleScreenMode(bool isTitle)
    {
        isTitleScreenMode = isTitle;
    }

    public void ShowPanel(bool forSaving)
    {
        isSaveMode = forSaving;
        mainPanel.SetActive(true);
        if (titleText != null)
        {
            titleText.text = isSaveMode ? "儲存進度" : "讀取進度";
        }
        RefreshSlots();
    }

    public void HidePanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    private void RefreshSlots()
    {
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsContainer);
            SaveSlotUI slotUI = slotGO.GetComponent<SaveSlotUI>();
            GameSaveData summary = SaveManager.Instance.GetSaveFileSummary(i);
            slotUI.Setup(i, summary, OnSlotClicked, OnDeleteRequested);
            if (!isSaveMode && summary == null)
            {
                slotUI.SetInteractable(false);
            }
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (isSaveMode)
        {
            if (StoryManager.Instance != null && StoryManager.Instance.IsStorySceneActive)
            {
                Debug.LogWarning("劇情演出中，禁止存檔。");
                return;
            }
            
            if (SaveManager.Instance.DoesSaveFileExist(slotIndex))
            {
                confirmationPanel.Show(
                    "覆蓋存檔？",
                    $"您確定要覆蓋槽位 {slotIndex + 1} 的進度嗎？此操作無法復原。",
                    () => {
                        SaveManager.Instance.SaveGame(slotIndex);
                        RefreshSlots();
                    }
                );
            }
            else
            {
                SaveManager.Instance.SaveGame(slotIndex);
                RefreshSlots();
            }
        }
        else
        {
            if (SaveManager.Instance.DoesSaveFileExist(slotIndex))
            {
                GameSaveData summary = SaveManager.Instance.GetSaveFileSummary(slotIndex);
                string currentVersion = Application.version;
                if (summary != null && summary.gameVersion != currentVersion)
                {
                    confirmationPanel.Show(
                        "版本不匹配",
                        $"此存檔來自舊版本 ({summary.gameVersion})，當前遊戲版本為 {currentVersion}。\n繼續載入可能導致未知問題，您確定要繼續嗎？",
                        () => PerformLoad(slotIndex)
                    );
                    return;
                }
                bool isAtTitleScreen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title";
                string confirmMessage;
                if (isAtTitleScreen)
                {
                    confirmMessage = $"是否要讀取槽位 {slotIndex + 1} 的存檔？";
                }
                else
                {
                    confirmMessage = $"您確定要讀取槽位 {slotIndex + 1} 的進度嗎？任何未儲存的進度將會遺失。";
                }
                confirmationPanel.Show(
                    "讀取遊戲？",
                    confirmMessage,
                    () => PerformLoad(slotIndex)
                );
            }
            else
            {
                Debug.Log($"槽位 {slotIndex + 1} 是空的，無法讀取。");
            }
        }
    }

    private void PerformLoad(int slotIndex)
    {
        SaveManager.Instance.LoadGame(slotIndex);
        
        var gameSystemUI = FindObjectOfType<GameSystemUI>();
        if (gameSystemUI != null)
        {
            gameSystemUI.CloseAllPanels();
        }
        else
        {
            HidePanel();
        }
    }

    private void OnDeleteRequested(int slotIndex)
    {
        confirmationPanel.Show(
            "刪除存檔？",
            $"您確定要永久刪除槽位 {slotIndex + 1} 的存檔嗎？此操作無法復原。",
            () => {
                SaveManager.Instance.DeleteSaveFile(slotIndex);
                RefreshSlots();
            }
        );
    }
}