using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public class NPC : MonoBehaviour, IInteractable
{
    [Header("NPC 資訊")]
    [Tooltip("唯一的 NPC ID，用於任務和對話系統的內部識別")]
    [SerializeField] private string npcID;
    
    [Header("任務設定")]
    [Tooltip("此 NPC 提供的所有任務 ID 列表")]
    [SerializeField] private List<string> availableQuestIDs = new List<string>();

    [Header("對話設定")]
    [Tooltip("此 NPC 的主要對話檔案路徑，相對於 'Resources/GameData/Dialogues/'。例如: 'NPCs/npc_mayor'")]
    [SerializeField] private string dialogueFileName;

    [Tooltip("與此 NPC 互動時的入口對話 ID")]
    [SerializeField] private string mainDialogueID;

    [Header("商店設定")]
    [Tooltip("勾選此項，將此 NPC 標記為商人。")]
    [SerializeField] private bool isTrader = false;
    [Tooltip("如果此 NPC 是商人，請指定其商店庫存 ScriptableObject。")]
    [SerializeField] private ShopInventorySO shopInventory;

    [Header("任務狀態圖標")]
    [Tooltip("有可接取任務時顯示的圖標 (!)")]
    [SerializeField] private GameObject availableQuestIcon;
    [Tooltip("有可交付任務時顯示的圖標 (?)")]
    [SerializeField] private GameObject completableQuestIcon;
    [Tooltip("有進行中的任務（但未完成）時顯示的圖標")]
    [SerializeField] private GameObject inProgressQuestIcon;

    private bool isPlayerInRange = false;

    private void Awake()
    {
        SetAllIconsActive(false);
        StartCoroutine(SubscribeToLoadEvent());
    }

    private System.Collections.IEnumerator SubscribeToLoadEvent()
    {
        yield return new WaitUntil(() => SaveManager.Instance != null);
        SaveManager.OnGameLoadComplete += RefreshIconsOnLoad;
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += OnQuestStateChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestStateChanged;
            QuestManager.Instance.OnQuestUpdated += OnQuestStateChanged;
        }
        UpdateQuestIcons();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= OnQuestStateChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestStateChanged;
            QuestManager.Instance.OnQuestUpdated -= OnQuestStateChanged;
        }
        
        if (SaveManager.Instance != null)
        {
            SaveManager.OnGameLoadComplete -= RefreshIconsOnLoad;
        }
    }
    
    private void RefreshIconsOnLoad()
    {
        Debug.Log($"[NPC] 遊戲加載完成，正在為 {gameObject.name} 刷新任務圖標。");
        UpdateQuestIcons();
    }

    private void OnQuestStateChanged(string questID)
    {
        if (string.IsNullOrEmpty(questID))
        {
            UpdateQuestIcons();
            return;
        }
        
        Quest quest = QuestDatabase.GetQuest(questID);
        if (quest != null && (quest.giverNPCID == this.npcID || quest.handInNPCID == this.npcID))
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(UpdateIconsNextFrame());
            }
        }
    }
    
    private System.Collections.IEnumerator UpdateIconsNextFrame()
    {
        yield return null;
        UpdateQuestIcons();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            UpdateQuestIcons();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            UpdateQuestIcons();
        }
    }

    private void UpdateQuestIcons()
    {
        if (availableQuestIcon == null && completableQuestIcon == null && inProgressQuestIcon == null) return;
        if (QuestManager.Instance == null) return;
        
        SetAllIconsActive(false);
        if (!isPlayerInRange) return;
        
        bool hasCompletable = false;
        bool hasInProgress = false;

        var relatedQuests = QuestManager.Instance.GetInProgressOrCompletableQuestsForNPC(npcID);
        
        if (relatedQuests.Any(q => QuestManager.Instance.IsQuestCompletable(q.questID)))
        {
            hasCompletable = true;
        }

        if (!hasCompletable && relatedQuests.Any())
        {
            hasInProgress = true;
        }

        if (hasCompletable)
        {
            if (completableQuestIcon != null) completableQuestIcon.SetActive(true);
        }
        else if (hasInProgress)
        {
            if (inProgressQuestIcon != null) inProgressQuestIcon.SetActive(true);
        }
        else if (availableQuestIcon != null && QuestManager.Instance.HasAvailableQuests(npcID))
        {
            availableQuestIcon.SetActive(true);
        }
    }
    
    private void SetAllIconsActive(bool isActive)
    {
        if (availableQuestIcon != null) availableQuestIcon.SetActive(isActive);
        if (completableQuestIcon != null) completableQuestIcon.SetActive(isActive);
        if (inProgressQuestIcon != null) inProgressQuestIcon.SetActive(isActive);
    }


    private void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    public string GetNpcID() => npcID;
    public List<string> GetQuestList() => availableQuestIDs;
    public bool IsTrader() => isTrader;
    public ShopInventorySO GetShopInventory() => shopInventory;

    public void Interact()
    {
        Debug.Log($"[NPC] 與 {gameObject.name} (ID: {npcID}) 互動。");

        if (string.IsNullOrEmpty(dialogueFileName) || string.IsNullOrEmpty(mainDialogueID))
        {
            Debug.LogWarning($"[NPC] {gameObject.name} 未設定 dialogueFileName 或 mainDialogueID。");
            return;
        }
        QuestManager.Instance.SyncCollectionQuests();
        DialogueManager.Instance.StartDialogue(dialogueFileName, mainDialogueID, npcID);
    }
}