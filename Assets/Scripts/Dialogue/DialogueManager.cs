using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static event Action OnDialogueEnded;

    private DialogueUI dialogueUI;
    private Dictionary<string, DialogueDatabase> dialogueCache = new Dictionary<string, DialogueDatabase>();

    private Dialogue currentDialogue;
    private DialogueSegment currentSegment;
    private DialogueDatabase currentDialogueDB;
    private string currentNpcID;

    private List<DialogueLine> _activeChain;
    private bool isInChainMode = false;
    private int currentChainIndex = -1;

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

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopClosed += ContinueDialogueAfterShop;
        }
    }

    void Update()
    {
        if (isInChainMode && Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogueChain();
        }
    }
    
    void OnDestroy()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopClosed -= ContinueDialogueAfterShop;
        }
    }

    #region UI 管理
    public void RegisterDialogueUI(DialogueUI ui)
    {
        if (this.dialogueUI != null)
        {
            Debug.LogWarning("[DialogueManager] 嘗試註冊新的 DialogueUI，但已有一個存在。舊的將被覆蓋。");
        }
        this.dialogueUI = ui;
        Debug.Log("[DialogueManager] DialogueUI 已成功註冊。");
    }

    public void UnregisterDialogueUI(DialogueUI ui)
    {
        if (this.dialogueUI == ui)
        {
            this.dialogueUI = null;
            Debug.Log("[DialogueManager] DialogueUI 已成功反註冊。");
        }
    }
    #endregion

    #region 公開的對話流程控制
    public void StartDialogue(string relativePath, string dialogueID, string npcID = null, string startSegmentID = null)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.InDialogue);
        }

        _activeChain = null;
        if (dialogueUI == null)
        {
            Debug.LogError("[DialogueManager] 無法開始對話，因為沒有 DialogueUI 被註冊！請檢查場景中是否存在 DialogueUI 物件。");
            return;
        }
        
        currentNpcID = npcID;
        currentDialogueDB = LoadAndCacheDialogueFile(relativePath);
        if (currentDialogueDB == null)
        {
            Debug.LogError($"[DialogueManager] 無法開始對話，因為檔案 '{relativePath}.xml' 載入失敗。");
            return;
        }

        currentDialogue = currentDialogueDB.dialogues.FirstOrDefault(d => d.dialogueID == dialogueID);
        if (currentDialogue != null && currentDialogue.segments.Count > 0)
        {
            string targetSegmentID = startSegmentID;
            if (string.IsNullOrEmpty(targetSegmentID))
            {
                targetSegmentID = currentDialogue.segments[0].id;
            }
            ShowSegment(targetSegmentID);
        }
        else
        {
            Debug.LogError($"[DialogueManager] 在檔案 '{relativePath}.xml' 中找不到對話ID '{dialogueID}' 或該對話沒有任何片段。");
        }
    }

    public void EndDialogue()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.InDialogue)
        {
            GameManager.Instance.SetGameState(GameState.Exploration);
        }

        if (dialogueUI != null) dialogueUI.HideDialogue();
        currentDialogue = null;
        currentSegment = null;
        currentDialogueDB = null;
        currentNpcID = null;
        isInChainMode = false;
        _activeChain = null;

        Debug.Log("[DialogueManager] 對話結束，觸發 OnDialogueEnded 事件。");
        OnDialogueEnded?.Invoke();
    }

    public void AdvanceDialogueChain()
    {
        if (!isInChainMode || _activeChain == null) return;

        currentChainIndex++;

        if (currentChainIndex >= _activeChain.Count)
        {
            isInChainMode = false;
            _activeChain = null;
            dialogueUI.SetChainAdvanceActive(false);
            dialogueUI.HideContinuePrompt();

            ProcessActions(currentSegment.actions);

            if (currentDialogue != null)
            {
                ProcessOptionsForCurrentSegment();
            }
        }
        else
        {
            DialogueLine currentLine = _activeChain[currentChainIndex];
            dialogueUI.ShowDialogue(currentLine.speaker, currentLine.text);

            bool isLastLine = (currentChainIndex == _activeChain.Count - 1);
            if (isLastLine)
            {
                ProcessActions(currentSegment.actions);
                
                List<DialogueOption> availableOptions = GenerateAvailableOptionsForCurrentSegment();

                if (availableOptions.Count > 0)
                {
                    dialogueUI.ShowOptions(availableOptions, OnOptionSelected);
                    isInChainMode = false;
                    _activeChain = null;
                    dialogueUI.SetChainAdvanceActive(false);
                    dialogueUI.HideContinuePrompt();
                }
                else
                {
                    dialogueUI.ShowContinuePrompt();
                }
            }
            else
            {
                dialogueUI.ShowContinuePrompt();
            }
        }
    }
    #endregion

    #region 內部對話處理流程
    private void ShowSegment(string segmentID)
    {
        currentSegment = currentDialogue.segments.FirstOrDefault(s => s.id == segmentID);
        if (currentSegment == null)
        {
            Debug.LogWarning($"[DialogueManager] 在當前對話中找不到片段ID '{segmentID}'。結束對話。");
            EndDialogue();
            return;
        }

        if (currentSegment.branch != null && string.IsNullOrEmpty(currentSegment.dialogueText) && currentSegment.dialogueChain.Count == 0)
        {
            Debug.Log($"[DialogueManager] 檢測到純邏輯 Segment '{segmentID}'，立即處理其 Branch。");
            ProcessBranch(currentSegment.branch);
            return;
        }

        var effectiveDialogueChain = new List<DialogueLine>();
        if (!string.IsNullOrEmpty(currentSegment.dialogueText))
        {
            effectiveDialogueChain.Add(new DialogueLine
            {
                speaker = string.IsNullOrEmpty(currentSegment.speakerName) ? "旁白" : currentSegment.speakerName,
                text = currentSegment.dialogueText
            });
        }
        if (currentSegment.dialogueChain != null && currentSegment.dialogueChain.Count > 0)
        {
            effectiveDialogueChain.AddRange(currentSegment.dialogueChain);
        }

        if (effectiveDialogueChain.Count > 0)
        {
            _activeChain = effectiveDialogueChain;
            isInChainMode = true;
            currentChainIndex = -1;
            dialogueUI.SetChainAdvanceActive(true);
            AdvanceDialogueChain();
        }
        else
        {
            dialogueUI.ShowDialogue("", "");
            ProcessActions(currentSegment.actions);
            ProcessOptionsForCurrentSegment();
        }
    }
    
    private void ProcessOptionsForCurrentSegment()
    {
        dialogueUI.HideContinuePrompt();
        List<DialogueOption> availableOptions = GenerateAvailableOptionsForCurrentSegment();
        
        if (availableOptions.Count > 0)
        {
            dialogueUI.ShowOptions(availableOptions, OnOptionSelected);
        }
    }
    
    private void OnOptionSelected(DialogueOption option)
    {
        if (option.branch != null && option.branch.check != null)
        {
            ProcessBranch(option.branch);
        }
        else
        {
            ProcessActions(option.actions);
        }
    }

    private void ProcessBranch(DialogueBranch branch)
    {
        Debug.Log($"[DialogueManager] 正在處理 Branch。檢查類型: {branch.check?.type}, 值: {branch.check?.value}");
        bool result = ExecuteCheck(branch.check);
        ProcessActions(result ? branch.trueActions : branch.falseActions);
    }

    private void ProcessActions(List<DialogueAction> actions)
    {
        foreach (var action in actions)
        {
            ExecuteSingleAction(action);
        }

        var goToSegmentAction = actions.FirstOrDefault(a => a.ActionType == DialogueActionType.GoToSegment);
        if (goToSegmentAction != null)
        {
            ShowSegment(goToSegmentAction.value);
            return;
        }
        
        var startBattleAction = actions.FirstOrDefault(a => a.ActionType == DialogueActionType.StartBattle);
        if (startBattleAction != null)
        {
            EndDialogue();
            return;
        }
        
        var endDialogueAction = actions.FirstOrDefault(a => 
            a.ActionType == DialogueActionType.EndDialogue ||
            a.ActionType == DialogueActionType.ContinueDialogue ||
            a.ActionType == DialogueActionType.CloseDialogue);
        if (endDialogueAction != null)
        {
            EndDialogue();
            return;
        }
        
        var startDialogueAction = actions.FirstOrDefault(a => a.ActionType == DialogueActionType.StartDialogue);
        if (startDialogueAction != null)
        {
            string[] dialogueParts = startDialogueAction.value.Split(',');
            if (dialogueParts.Length == 2)
            {
                string newRelativePath = dialogueParts[0].Trim();
                string newDialogueID = dialogueParts[1].Trim();
                string startSegmentID = goToSegmentAction?.value;
                StartDialogue(newRelativePath, newDialogueID, this.currentNpcID, startSegmentID);
            }
            else
            {
                Debug.LogError($"[DialogueManager] StartDialogue 動作的格式錯誤: '{startDialogueAction.value}'。應為 'filePath,dialogueID'。");
            }
            return;
        }
    }
    
    private void ExecuteSingleAction(DialogueAction action)
    {
        switch (action.ActionType)
        {
            case DialogueActionType.AcceptQuest:
                QuestManager.Instance.AcceptQuest(action.value);
                break;

            case DialogueActionType.CompleteQuest:
                QuestManager.Instance.CompleteQuest(action.value);
                break;

            case DialogueActionType.AdvanceQuestObjective:
                {
                    string[] partsObjective = action.value.Split(',');
                    if (partsObjective.Length >= 2)
                    {
                        string questID = partsObjective[0].Trim();
                        string objectiveTargetID = partsObjective[1].Trim();
                        int.TryParse(partsObjective.Length > 2 ? partsObjective[2].Trim() : "1", out int amount);
                        QuestManager.Instance.AdvanceObjective(questID, objectiveTargetID, QuestObjectiveType.Talk, amount);
                    }
                    break;
                }

            case DialogueActionType.AddPartyMember:
                PartyManager.Instance?.AddMemberToHolder(action.value);
                break;

            case DialogueActionType.RemovePartyMember:
                PartyManager.Instance?.RemoveMemberCompletely(action.value);
                break;

            case DialogueActionType.AddItem:
                {
                    string[] partsAdd = action.value.Split(',');
                    Item itemToAdd = ItemDatabase.Instance.GetItemByID(partsAdd[0].Trim());
                    if (itemToAdd != null)
                    {
                        int.TryParse(partsAdd.Length > 1 ? partsAdd[1].Trim() : "1", out int amount);
                        InventoryManager.Instance.AddItem(itemToAdd, amount);
                    }
                    break;
                }

            case DialogueActionType.RemoveItem:
                {
                    string[] partsRemove = action.value.Split(',');
                    Item itemToRemove = ItemDatabase.Instance.GetItemByID(partsRemove[0].Trim());
                    if (itemToRemove != null)
                    {
                        int.TryParse(partsRemove.Length > 1 ? partsRemove[1].Trim() : "1", out int amount);
                        InventoryManager.Instance.RemoveItem(itemToRemove, amount);
                    }
                    break;
                }

            case DialogueActionType.AddMoney:
                {
                    if (int.TryParse(action.value, out int amount))
                    {
                        PlayerState.Instance?.AddMoney(amount);
                    }
                    break;
                }

            case DialogueActionType.OpenShop:
                {
                    var currentNPC = FindObjectsOfType<NPC>().FirstOrDefault(n => n.GetNpcID() == currentNpcID);
                    if (currentNPC != null && currentNPC.IsTrader())
                    {
                        dialogueUI?.HideDialogue();
                        ShopManager.Instance.OpenShop(currentNPC.GetShopInventory(), currentNPC, action.value);
                    }
                    break;
                }

            case DialogueActionType.TriggerGameEvent:
                GameEventManager.Instance?.TriggerEvent(action.value);
                break;

            case DialogueActionType.StartStoryScene:
                {
                    StorySceneData sceneToStart = Resources.Load<StorySceneData>($"GameData/StoryScenes/{action.value}");
                    if (sceneToStart != null)
                    {
                        GameEventManager.Instance?.TriggerEvent(sceneToStart.sceneID);
                        Debug.Log($"[DialogueManager] 觸發了劇情場景事件: {sceneToStart.sceneID}");
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueManager] 找不到 StorySceneData 於: GameData/StoryScenes/{action.value}");
                    }
                    break;
                }
            
            case DialogueActionType.StartBattle:
                GameObject triggerObject = GameObject.Find(action.value);
                if (triggerObject != null)
                {
                    BattleTrigger trigger = triggerObject.GetComponent<BattleTrigger>();
                    if (trigger != null)
                    {
                        Debug.Log($"[DialogueManager] 透過對話觸發了 BattleTrigger: {action.value}");
                        trigger.TriggerBattle();
                        // 觸發戰鬥後，通常需要結束當前對話
                        // EndDialogue();
                    }
                    else
                    {
                        Debug.LogError($"[DialogueManager] 找到了名為 '{action.value}' 的物件，但它上面沒有 BattleTrigger 腳本！");
                    }
                }
                else
                {
                    Debug.LogError($"[DialogueManager] 無法在場景中找到名為 '{action.value}' 的 BattleTrigger 物件！");
                }
                break;
        }
    }
    #endregion
    
    #region 資料處理與條件評估
    private DialogueDatabase LoadAndCacheDialogueFile(string relativePath)
    {
        if (dialogueCache.ContainsKey(relativePath))
        {
            return dialogueCache[relativePath];
        }

        string fullPath = $"GameData/Dialogues/{relativePath}";
        TextAsset xmlFile = Resources.Load<TextAsset>(fullPath);

        if (xmlFile == null)
        {
            Debug.LogError($"[DialogueManager] 找不到對話檔案於: Resources/{fullPath}.xml");
            return null;
        }

        XmlSerializer serializer = new XmlSerializer(typeof(DialogueDatabase));
        using (StringReader reader = new StringReader(xmlFile.text))
        {
            DialogueDatabase db = (DialogueDatabase)serializer.Deserialize(reader);
            dialogueCache[relativePath] = db; 
            Debug.Log($"[DialogueManager] 成功載入並快取對話檔案: {relativePath}.xml");
            return db;
        }
    }

    private List<DialogueOption> GenerateAvailableOptionsForCurrentSegment()
    {
        List<DialogueOption> availableOptions = new List<DialogueOption>();
        if (currentSegment == null) return availableOptions;

        foreach (var option in currentSegment.options)
        {
            if (CheckDisplayConditions(option.displayConditions))
            {
                availableOptions.Add(option);
            }
        }

        foreach (var dynamicRequest in currentSegment.dynamicOptions)
        {
            availableOptions.AddRange(GenerateDynamicOptions(dynamicRequest));
        }

        if (availableOptions.Count == 0)
        {
            if (currentSegment.isEnd)
            {
                availableOptions.Add(new DialogueOption
                {
                    text = "[離開]",
                    actions = new List<DialogueAction> { new DialogueAction { type = "EndDialogue" } }
                });
            }
            else if (currentSegment.isContinue)
            {
                availableOptions.Add(new DialogueOption
                {
                    text = "[繼續]",
                    actions = new List<DialogueAction> { new DialogueAction { type = "ContinueDialogue" } }
                });
            }
            else if (currentSegment.isClose)
            {
                availableOptions.Add(new DialogueOption
                {
                    text = "[關閉]",
                    actions = new List<DialogueAction> { new DialogueAction { type = "CloseDialogue" } }
                });
            }
        }
        return availableOptions;
    }

    private List<DialogueOption> GenerateDynamicOptions(DynamicOptions request)
    {
        List<DialogueOption> generated = new List<DialogueOption>();
        string targetNpcID = (request.npcID == "self" && !string.IsNullOrEmpty(currentNpcID)) ? currentNpcID : request.npcID;
        HashSet<string> exclusionSet = string.IsNullOrEmpty(request.excludeIDs) ? null : new HashSet<string>(request.excludeIDs.Split(',').Select(id => id.Trim()));

        if (request.SourceType == DynamicOptionSource.QuestManager)
        {
            List<Quest> quests = new List<Quest>();
            switch (request.RequestType)
            {
                case DynamicOptionType.AvailableQuests:
                    quests = QuestManager.Instance.GetAvailableQuestsForNPC(targetNpcID);
                    break;
                case DynamicOptionType.InProgressOrCompletableQuests:
                    quests = QuestManager.Instance.GetInProgressOrCompletableQuestsForNPC(targetNpcID);
                    break;
            }

            foreach (var quest in quests)
            {
                if (exclusionSet != null && exclusionSet.Contains(quest.questID)) continue;

                DialogueOption newOption = new DialogueOption
                {
                    text = request.optionTemplate.text.Replace("{questName}", quest.questName),
                    actions = request.optionTemplate.actions.Select(a => new DialogueAction { type = a.type, value = a.value.Replace("{questID}", quest.questID) }).ToList()
                };

                if (request.optionTemplate.branch != null)
                {
                    newOption.branch = new DialogueBranch
                    {
                        check = new DialogueCheck { type = request.optionTemplate.branch.check.type, value = request.optionTemplate.branch.check.value.Replace("{questID}", quest.questID) },
                        trueActions = request.optionTemplate.branch.trueActions.Select(a => new DialogueAction { type = a.type, value = a.value.Replace("{questID}", quest.questID) }).ToList(),
                        falseActions = request.optionTemplate.branch.falseActions.Select(a => new DialogueAction { type = a.type, value = a.value.Replace("{questID}", quest.questID) }).ToList()
                    };
                }
                generated.Add(newOption);
            }
        }
        return generated;
    }

    private bool ExecuteCheck(DialogueCheck check)
    {
        if (check == null) return false;
        string checkValue = check.value.Replace("self", currentNpcID);
        string[] parts = checkValue.Split(',');
        string id = parts.Length > 0 ? parts[0].Trim() : "";

        switch (check.CheckType)
        {
            case DialogueCheckType.QuestStatus:
                if (parts.Length < 2) return false;
                if (System.Enum.TryParse<Quest.QuestStatus>(parts[1].Trim(), true, out var status))
                {
                    return QuestManager.Instance.GetQuestStatus(id) == status;
                }
                return false;

            case DialogueCheckType.QuestCheck:
                if (parts.Length < 2) return false;
                string npcID = parts[1].Trim();
                switch (id)
                {
                    case "HasAvailableQuests": return QuestManager.Instance.HasAvailableQuests(npcID);
                    case "HasProgressOrCompletableQuests": return QuestManager.Instance.HasProgressOrCompletableQuests(npcID);
                    case "IsCompletable": return QuestManager.Instance.IsQuestCompletable(npcID); // 假設npcID參數是questID
                }
                return false;

            case DialogueCheckType.HasItem:
                if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out int requiredAmount)) return false;
                Item item = ItemDatabase.Instance.GetItemByID(id);
                return item != null && InventoryManager.Instance.HasItem(item, requiredAmount);

            case DialogueCheckType.GameEventTriggered:
                return GameEventManager.Instance.HasEventBeenTriggered(id);
        }
        return false;
    }

    private bool CheckDisplayConditions(List<DialogueCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true;

        foreach (var condition in conditions)
        {
            string[] parts = condition.value.Split(',');
            string id = parts[0];
            string value = parts.Length > 1 ? parts[1].Trim() : "";
            bool conditionMet = false;

            switch (condition.ConditionType)
            {
                case DialogueConditionType.QuestStatus:
                    if (System.Enum.TryParse<Quest.QuestStatus>(value, true, out var status))
                    {
                        conditionMet = QuestManager.Instance.GetQuestStatus(id) == status;
                    }
                    break;
                case DialogueConditionType.HasItem:
                    if (int.TryParse(value, out int requiredAmount))
                    {
                        Item item = ItemDatabase.Instance.GetItemByID(id);
                        conditionMet = item != null && InventoryManager.Instance.HasItem(item, requiredAmount);
                    }
                    break;
                case DialogueConditionType.QuestAvailable:
                    conditionMet = QuestManager.Instance.GetQuestStatus(id) == Quest.QuestStatus.NotStarted && QuestManager.Instance.ArePrerequisitesMet(id);
                    break;
                case DialogueConditionType.QuestCheck:
                    string npcIdToCheck = value.Replace("self", this.currentNpcID);
                    switch (id)
                    {
                        case "HasAvailableQuests": conditionMet = QuestManager.Instance.HasAvailableQuests(npcIdToCheck); break;
                        case "HasProgressOrCompletableQuests": conditionMet = QuestManager.Instance.HasProgressOrCompletableQuests(npcIdToCheck); break;
                    }
                    break;
                case DialogueConditionType.HasPartyMember:
                    conditionMet = PartyManager.Instance?.AllMembers.Any(m => m.memberDataSO_ID == id) ?? false;
                    break;
                case DialogueConditionType.IsTrader:
                    var npcToCheck = FindObjectsOfType<NPC>().FirstOrDefault(n => n.GetNpcID() == currentNpcID);
                    conditionMet = (npcToCheck != null && npcToCheck.IsTrader());
                    break;
                case DialogueConditionType.GameEventTriggered:
                    conditionMet = GameEventManager.Instance.HasEventBeenTriggered(id);
                    break;
            }
            if (!conditionMet) return false;
        }
        return true;
    }

    private void ContinueDialogueAfterShop(string segmentID)
    {
        if (currentDialogue == null) return;
        if (!string.IsNullOrEmpty(segmentID))
        {
            ShowSegment(segmentID);
        }
        else
        {
            EndDialogue();
        }
    }
    #endregion
}