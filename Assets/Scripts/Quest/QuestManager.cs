using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour, IGameSaveable
{
    public static QuestManager Instance { get; private set; }

    private Dictionary<string, Quest.QuestStatus> questStatuses = new Dictionary<string, Quest.QuestStatus>();
    private Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
    private Dictionary<string, NPC> npcCache = new Dictionary<string, NPC>();
    private HashSet<string> permanentlyCompletedQuestIDs = new HashSet<string>();

    public event Action<string> OnQuestAccepted;
    public event Action<string> OnQuestCompleted;
    public event Action<string> OnQuestUpdated;

    private bool isLoading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        SaveManager.Instance.Register(this);
    }

    void OnEnable()
    {
        InventoryManager.OnItemQuantityChanged += HandleItemQuantityChanged;
    }

    void OnDisable()
    {
        InventoryManager.OnItemQuantityChanged -= HandleItemQuantityChanged;
    }

    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    private void Start()
    {
        CacheAllNPCs();
    }

    #region 任務狀態
    public void AcceptQuest(string questID)
    {
        if (GetQuestStatus(questID) != Quest.QuestStatus.NotStarted || !ArePrerequisitesMet(questID))
        {
            Debug.LogWarning($"[QuestManager] 無法接受任務 {questID}，狀態不符或前置未完成。");
            return;
        }

        Quest newQuest = QuestDatabase.GetQuest(questID);
        if (newQuest != null)
        {
            questStatuses[questID] = Quest.QuestStatus.InProgress;

            foreach (var objective in newQuest.objectives)
            {
                if (objective.type == QuestObjectiveType.Kill)
                {
                    objective.startingAmount = PlayerStatsManager.Instance.GetKillCount(objective.targetID);
                }
                // (未來可擴充)
            }
            activeQuests[questID] = newQuest;
            Debug.Log($"[QuestManager] 已接受任務: {newQuest.questName}");
            if (!isLoading) OnQuestAccepted?.Invoke(questID);

            SyncQuestProgress(newQuest);
        }
    }

    public void CompleteQuest(string questID)
    {
        if (GetQuestStatus(questID) != Quest.QuestStatus.InProgress) return;

        if (activeQuests.TryGetValue(questID, out Quest quest))
        {
            if (!quest.AreAllObjectivesComplete())
            {
                Debug.LogWarning($"[QuestManager] 嘗試完成任務 '{questID}' 但目標未達成。");
                return;
            }
            permanentlyCompletedQuestIDs.Add(questID);

            PlayerState playerState = FindObjectOfType<PlayerState>();
            if (playerState != null)
            {
                playerState.AddMoney(quest.moneyReward);
            }
            foreach (var reward in quest.itemRewards)
            {
                Item item = ItemDatabase.Instance.GetItemByID(reward.itemID);
                InventoryManager.Instance.AddItem(item, reward.amount);
            }
            if (quest.isRepeatable)
            {
                questStatuses[questID] = Quest.QuestStatus.NotStarted;
                quest.ResetProgress();
            }
            else
            {
                questStatuses[questID] = Quest.QuestStatus.Completed;
            }
            activeQuests.Remove(questID);
            Debug.Log($"[QuestManager] 已完成任務: {quest.questName}");
            if (!isLoading) OnQuestCompleted?.Invoke(questID);
        }
    }

    public Quest.QuestStatus GetQuestStatus(string questID)
    {
        if (questStatuses.TryGetValue(questID, out Quest.QuestStatus status))
        {
            return status;
        }
        return Quest.QuestStatus.NotStarted;
    }

    public bool ArePrerequisitesMet(string questID)
    {
        Quest quest = QuestDatabase.GetQuest(questID);
        if (quest == null) return false;

        foreach (var prereqID in quest.prerequisiteQuestIDs)
        {
            if (GetQuestStatus(prereqID) != Quest.QuestStatus.Completed)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsQuestCompletable(string questID)
    {
        if (activeQuests.TryGetValue(questID, out Quest quest))
        {
            if (!quest.AreAllObjectivesComplete())
            {
                return false;
            }

            foreach (var objective in quest.objectives)
            {
                if (objective.type == QuestObjectiveType.Collect)
                {
                    Item itemToCheck = ItemDatabase.Instance.GetItemByID(objective.targetID);
                    if (!InventoryManager.Instance.HasItem(itemToCheck, objective.requiredAmount))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        return false;
    }

    public bool IsObjectiveVisible(Quest quest, int objectiveIndex)
    {
        if (quest == null || objectiveIndex < 0 || objectiveIndex >= quest.objectives.Count)
        {
            return false;
        }
        QuestObjective objective = quest.objectives[objectiveIndex];

        if (objective.prerequisiteIndex < 0)
        {
            return true;
        }

        int prereqIndex = objective.prerequisiteIndex;
        if (prereqIndex >= 0 && prereqIndex < quest.objectives.Count)
        {
            var prereqObjective = quest.objectives[prereqIndex];
            bool isPrereqComplete = prereqObjective.IsComplete;
            return isPrereqComplete;
        }
        return false;
    }

    public bool HasQuestBeenCompleted(string questID)
    {
        return permanentlyCompletedQuestIDs.Contains(questID);
    }
    #endregion

    #region NPC
    private void CacheAllNPCs()
    {
        npcCache.Clear();
        NPC[] allNpcs = FindObjectsOfType<NPC>();
        foreach (var npc in allNpcs)
        {
            if (!npcCache.ContainsKey(npc.GetNpcID()))
            {
                npcCache.Add(npc.GetNpcID(), npc);
            }
        }
    }

    public List<Quest> GetAvailableQuestsForNPC(string npcID)
    {
        List<Quest> available = new List<Quest>();
        if (npcCache.TryGetValue(npcID, out NPC npc))
        {
            foreach (var questID in npc.GetQuestList())
            {
                if (GetQuestStatus(questID) == Quest.QuestStatus.NotStarted && ArePrerequisitesMet(questID))
                {
                    Quest q = QuestDatabase.GetQuest(questID);
                    if (q != null)
                    {
                        available.Add(q);
                    }
                }
            }
        }
        return available;
    }

    public List<Quest> GetInProgressOrCompletableQuestsForNPC(string npcID)
    {
        return activeQuests.Values.Where(q => q.handInNPCID == npcID).ToList();
    }

    public bool HasAvailableQuests(string npcID)
    {
        if (npcCache.TryGetValue(npcID, out NPC npc))
        {
            foreach (var questID in npc.GetQuestList())
            {
                if (GetQuestStatus(questID) == Quest.QuestStatus.NotStarted && ArePrerequisitesMet(questID))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool HasProgressOrCompletableQuests(string npcID)
    {
        return activeQuests.Values.Any(q => q.handInNPCID == npcID);
    }

    public List<Quest> GetActiveQuests()
    {
        return activeQuests.Values.ToList();
    }
    #endregion

    #region 任務進程
    public void AdvanceObjective(string questID, string targetID, QuestObjectiveType type, int amount = 1)
    {
        Debug.Log($"[QuestManager] 接收到精確進度更新請求：QuestID={questID}, TargetID={targetID}, Type={type}");
        if (activeQuests.TryGetValue(questID, out Quest quest))
        {
            bool updated = false;
            foreach (var objective in quest.objectives)
            {
                if (objective.type == type && objective.targetID == targetID && !objective.IsComplete)
                {
                    objective.currentAmount = Mathf.Min(objective.currentAmount + amount, objective.requiredAmount);
                    updated = true;
                    Debug.Log($"[QuestManager] 任務 '{quest.questName}' 的目標 '{objective.description}' 進度更新為: {objective.currentAmount}/{objective.requiredAmount}");
                }
            }
            if (updated)
            {
                if (!isLoading) OnQuestUpdated?.Invoke(quest.questID);
            }
        }
        else
        {
            Debug.LogWarning($"[QuestManager] 嘗試更新任務 '{questID}' 的進度，但該任務不在活躍列表中。");
        }
    }

    public void AdvanceObjective(string targetID, QuestObjectiveType type, int amount = 1)
    {
        if (type == QuestObjectiveType.Kill) return;

        foreach (var quest in activeQuests.Values.ToList())
        {
            bool updated = false;
            foreach (var objective in quest.objectives)
            {
                if (objective.type == type && objective.targetID == targetID && !objective.IsComplete)
                {
                    objective.currentAmount = Mathf.Min(objective.currentAmount + amount, objective.requiredAmount);
                    updated = true;
                }
            }
            if (updated)
            {
                Debug.Log($"[QuestManager] 任務 '{quest.questName}' 進度更新。");
                if (!isLoading) OnQuestUpdated?.Invoke(quest.questID);
            }
        }
    }

    private void OnKillCountUpdated(string enemyID, int newTotalCount)
    {
        foreach (var quest in activeQuests.Values)
        {
            bool wasUpdated = false;
            foreach (var objective in quest.objectives)
            {
                if (objective.type == QuestObjectiveType.Kill && objective.targetID == enemyID)
                {
                    int progress = newTotalCount - objective.startingAmount;
                    objective.currentAmount = Mathf.Min(progress, objective.requiredAmount);
                    wasUpdated = true;
                }
            }
            if (wasUpdated)
            {
                Debug.Log($"[QuestManager] 任務 '{quest.questName}' 因擊殺 '{enemyID}' 而進度更新。");
                OnQuestUpdated?.Invoke(quest.questID);
            }
        }
    }

    public void SyncAllQuestsProgress()
    {
        foreach (var quest in activeQuests.Values)
        {
            SyncQuestProgress(quest);
        }
    }

    public void SyncCollectionQuests()
    {
        foreach (var quest in activeQuests.Values)
        {
            bool updated = false;
            foreach (var objective in quest.objectives)
            {
                if (objective.type == QuestObjectiveType.Collect)
                {
                    Item item = ItemDatabase.Instance.GetItemByID(objective.targetID);
                    int itemCount = InventoryManager.Instance.GetItemCount(item);
                    if (objective.currentAmount != itemCount)
                    {
                        objective.currentAmount = itemCount;
                        updated = true;
                    }
                }
            }
            if (updated)
            {
                OnQuestUpdated?.Invoke(quest.questID);
            }
        }
    }

    private void SyncQuestProgress(Quest quest)
    {
        if (quest == null) return;

        bool wasUpdated = false;
        foreach (var objective in quest.objectives)
        {
            int oldAmount = objective.currentAmount;
            int newAmount = oldAmount;

            switch (objective.type)
            {
                case QuestObjectiveType.Collect:
                    Item item = ItemDatabase.Instance.GetItemByID(objective.targetID);
                    newAmount = InventoryManager.Instance.GetItemCount(item);
                    break;
                case QuestObjectiveType.Kill:
                    int totalKills = PlayerStatsManager.Instance.GetKillCount(objective.targetID);
                    newAmount = totalKills - objective.startingAmount;
                    break;
                    // 其他任務類型...
            }

            objective.currentAmount = Mathf.Clamp(newAmount, 0, objective.requiredAmount);

            if (objective.currentAmount != oldAmount)
            {
                wasUpdated = true;
            }
        }

        if (wasUpdated)
        {
            if (!isLoading) OnQuestUpdated?.Invoke(quest.questID);
        }
    }
    #endregion

    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.questData.questStatuses = questStatuses.Select(kvp => new QuestStatusEntry { questID = kvp.Key, status = kvp.Value }).ToList();
        data.questData.activeQuests = activeQuests.Select(kvp => new ActiveQuestData
        {
            questID = kvp.Key,
            objectiveProgress = kvp.Value.objectives.Select(obj => obj.currentAmount).ToList()
        }).ToList();
        data.questData.permanentlyCompletedQuestIDs = permanentlyCompletedQuestIDs.ToList();
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true;

        if (data.questData == null) return;
        
        questStatuses = data.questData.questStatuses.ToDictionary(e => e.questID, e => e.status);
        permanentlyCompletedQuestIDs = new HashSet<string>(data.questData.permanentlyCompletedQuestIDs ?? new List<string>());

        activeQuests.Clear();
        if (data.questData.activeQuests != null)
        {
            foreach (var activeQuestData in data.questData.activeQuests)
            {
                Quest questInstance = QuestDatabase.GetQuest(activeQuestData.questID);
                if (questInstance != null)
                {
                    for (int i = 0; i < questInstance.objectives.Count && i < activeQuestData.objectiveProgress.Count; i++)
                    {
                        questInstance.objectives[i].currentAmount = activeQuestData.objectiveProgress[i];
                    }
                    activeQuests[activeQuestData.questID] = questInstance;
                }
            }
        }
        isLoading = false;
    }

    private void HandleItemQuantityChanged(string changedItemID, int newTotalQuantity)
    {
        foreach (var quest in activeQuests.Values.ToList())
        {
            bool wasUpdated = false;
            foreach (var objective in quest.objectives)
            {
                if (objective.type == QuestObjectiveType.Collect && objective.targetID == changedItemID)
                {
                    if (!objective.IsComplete)
                    {
                        int oldAmount = objective.currentAmount;
                        objective.currentAmount = Mathf.Clamp(newTotalQuantity, 0, objective.requiredAmount);
                        
                        if(oldAmount != objective.currentAmount)
                        {
                            wasUpdated = true;
                        }
                    }
                }
            }
            
            if (wasUpdated)
            {
                Debug.Log($"[QuestManager] 因物品 '{changedItemID}' 數量變化，任務 '{quest.questName}' 進度已即時同步。");
                if (!isLoading) OnQuestUpdated?.Invoke(quest.questID);
            }
        }
    }
    #endregion
}