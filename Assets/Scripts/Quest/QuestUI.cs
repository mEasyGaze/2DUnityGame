using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questListItemPrefab;

    [Header("詳情面板")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI questObjectivesText;
    [SerializeField] private TextMeshProUGUI questRewardsText;

    private List<GameObject> currentListItems = new List<GameObject>();

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += HandleQuestChanged;
            QuestManager.Instance.OnQuestCompleted += HandleQuestChanged;
            QuestManager.Instance.OnQuestUpdated += HandleQuestChanged;
        }
        SaveManager.OnGameLoadComplete += RefreshUI;

        if (questPanel.activeSelf)
        {
            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= HandleQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestChanged;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestChanged;
        }
        SaveManager.OnGameLoadComplete -= RefreshUI;
    }

    private void HandleQuestChanged(string ignoredQuestID)
    {
        RefreshUI();
    }

    void Start()
    {
        questPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        bool isActive = !questPanel.activeSelf;
        questPanel.SetActive(isActive);
        if (isActive)
        {
            RefreshUI();
        }
    }

    #region UI
    public void RefreshUI() 
    {
        if (this == null || questPanel == null || !questPanel.activeSelf || QuestManager.Instance == null) return;

        QuestManager.Instance.SyncCollectionQuests();

        foreach (var item in currentListItems)
        {
            if (item != null) Destroy(item);
        }
        currentListItems.Clear();

        List<Quest> activeQuests = QuestManager.Instance.GetActiveQuests();

        foreach (var quest in activeQuests)
        {
            GameObject listItem = Instantiate(questListItemPrefab, questListContainer);
            TextMeshProUGUI textComponent = listItem.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null) textComponent.text = quest.questName;

            Button button = listItem.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (this != null) ShowQuestDetails(quest);
                });
            }
            currentListItems.Add(listItem);
        }

        if (activeQuests.Count > 0)
        {
            ShowQuestDetails(activeQuests[0]);
        }
        else
        {
            ClearDetails();
        }
    }

    private void ShowQuestDetails(Quest quest)
    {
        if (quest == null)
        {
            ClearDetails();
            return;
        }
        questNameText.text = quest.questName;
        questDescriptionText.text = quest.description;

        string objectivesStr = "";
        bool allObjectivesComplete = true;

        for (int i = 0; i < quest.objectives.Count; i++)
        {
            if (QuestManager.Instance.IsObjectiveVisible(quest, i))
            {
                var obj = quest.objectives[i];
                string statusIcon;
                string colorTagStart = "";
                string colorTagEnd = "";

                if (obj.IsComplete)
                {
                    statusIcon = "✓";
                    colorTagStart = "<color=#78FF78>";
                    colorTagEnd = "</color>";
                }
                else
                {
                    statusIcon = "☐";
                    colorTagStart = "<color=#CCCCCC>";
                    colorTagEnd = "</color>";

                    allObjectivesComplete = false;
                }
                objectivesStr += $"{colorTagStart}{statusIcon} {obj.description} ({obj.currentAmount} / {obj.requiredAmount}){colorTagEnd}\n";
            }
            else
            {
                allObjectivesComplete = false;
            }
        }

        if (allObjectivesComplete)
        {
            objectivesStr += "<color=#FFD700><b>所有目標均已完成！\n請前往交付任務。</b></color>";
        }
        questObjectivesText.text = objectivesStr.TrimEnd('\n');

        string rewardsStr = "獎勵:\n";
        bool hasAnyReward = false;
        if (quest.moneyReward > 0)
        {
            rewardsStr += $"- 金錢: {quest.moneyReward}\n";
            hasAnyReward = true;
        }
        if (quest.itemRewards != null)
        {
            foreach (var reward in quest.itemRewards)
            {
                Item item = ItemDatabase.Instance.GetItemByID(reward.itemID);
                if (item != null)
                {
                    rewardsStr += $"- {item.itemName} x{reward.amount}\n";
                    hasAnyReward = true;
                }
            }
        }

        if (!hasAnyReward)
        {
            rewardsStr += "- 無";
        }
        questRewardsText.text = rewardsStr;
    }

    private void ClearDetails()
    {
        questNameText.text = "沒有進行中的任務";
        questDescriptionText.text = "";
        questObjectivesText.text = "";
        questRewardsText.text = "";
    }
    #endregion
}