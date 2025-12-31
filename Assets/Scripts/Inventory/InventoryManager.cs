using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour, IGameSaveable
{
    #region 核心數據
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InventoryManager>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("InventoryManager");
                    _instance = singletonObject.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }

    [Header("背包數據 (ScriptableObject)")]
    public Inventory playerInventoryData;

    [HideInInspector]
    public InventoryUI playerInventoryUI { get; private set; }
    public event Action OnInventoryChanged;
    public static event Action<string, int> OnItemQuantityChanged;
    public static event Action<bool> OnItemSelectionModeChanged;

    [Header("拋棄物品設置")]
    [SerializeField] private GameObject groundItemPrefab;
    
    private bool isLoading = false;
    public bool IsSelectingTarget { get; private set; } = false;
    private Item itemPendingUsage;
    #endregion

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        if (playerInventoryData == null) Debug.LogError("Player Inventory Data not assigned in InventoryManager!");
        SaveManager.Instance.Register(this);
    }
    
    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscape += HandleEscape;
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscape -= HandleEscape;
        }
    }

    private void HandleEscape()
    {
        if (IsSelectingTarget)
        {
            CancelItemSelectionMode();
        }
    }

    #region UI 管理
    public void RegisterInventoryUI(InventoryUI ui)
    {
        if (this.playerInventoryUI != null)
        {
            Debug.LogWarning("[InventoryManager] 嘗試註冊新的 InventoryUI，但已有一個存在。舊的將被覆蓋。");
        }
        this.playerInventoryUI = ui;
        
        if (this.playerInventoryUI != null && playerInventoryData != null)
        {
            this.playerInventoryUI.InitializeUI(playerInventoryData);
            Debug.Log("[InventoryManager] InventoryUI 已成功註冊並初始化。");
        }
    }

    public void UnregisterInventoryUI(InventoryUI ui)
    {
        if (this.playerInventoryUI == ui)
        {
            this.playerInventoryUI = null;
            Debug.Log("[InventoryManager] InventoryUI 已成功反註冊。");
        }
    }

    public void ToggleInventoryUI() => playerInventoryUI?.TogglePanel();
    public void OpenInventoryUI() => playerInventoryUI?.ShowPanel();
    public void CloseInventoryUI() => playerInventoryUI?.HidePanel();
    #endregion

    #region 物品數據庫查詢
    private Dictionary<string, Item> _itemDatabaseCache;
    public Item GetItemDataByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;
        if (_itemDatabaseCache == null)
        {
            _itemDatabaseCache = new Dictionary<string, Item>();
            Item[] items = Resources.LoadAll<Item>("Items");
            foreach (Item item in items)
            {
                if (!_itemDatabaseCache.ContainsKey(item.uniqueItemID))
                {
                    _itemDatabaseCache.Add(item.uniqueItemID, item);
                }
            }
            Debug.Log($"已加載 {_itemDatabaseCache.Count} 個物品到數據庫緩存。");
        }

        if (_itemDatabaseCache.TryGetValue(itemID, out Item foundItem))
        {
            return foundItem;
        }
        Debug.LogWarning($"InventoryManager: 找不到 ID 為 '{itemID}' 的物品數據。");
        return null;
    }
    #endregion

    #region 物品操作邏輯 (Add, Remove...)
    public static bool TriggerAddItem(Item itemToAdd, int quantity)
    {
        if (Instance == null)
        {
            Debug.LogError("InventoryManager 實例不存在，無法添加物品！");
            return false;
        }
        if (itemToAdd == null || quantity <= 0)
        {
            Debug.LogWarning("嘗試添加無效的物品或數量。");
            return false;
        }
        return Instance.AddItem(itemToAdd, quantity);
    }

    public bool AddItem(Item itemToAdd, int quantity = 1)
    {
        if (playerInventoryData == null) 
        {
            Debug.LogError("Player Inventory Data 未在 InventoryManager 中設置！");
            return false;
        }
        if (itemToAdd == null || quantity <= 0) return false;
        int initialQuantity = quantity;

        bool addedSuccessfully = false;

        // 1. 嘗試堆疊
        foreach (InventorySlot slot in playerInventoryData.slots)
        {
            if (!slot.IsEmpty() && slot.item == itemToAdd && slot.item.IsStackable())
            {
                int spaceLeft = slot.GetRemainingSpace();
                if (spaceLeft > 0)
                {
                    int amountToAdd = Mathf.Min(quantity, spaceLeft);
                    slot.AddQuantity(amountToAdd);
                    quantity -= amountToAdd;
                    addedSuccessfully = true;

                    if (quantity <= 0) break;
                }
            }
        }

        // 2. 嘗試放入空格子
        if (quantity > 0)
        {
            foreach (InventorySlot slot in playerInventoryData.slots)
            {
                if (slot.IsEmpty())
                {
                    int amountToAdd = Mathf.Min(quantity, itemToAdd.maxStackAmount);
                    slot.SetSlot(itemToAdd, amountToAdd);
                    quantity -= amountToAdd;
                    addedSuccessfully = true;

                    if (quantity <= 0) break;
                }
            }
        }

        if (addedSuccessfully)
        {
            if (!isLoading)
            {
                OnInventoryChanged?.Invoke();
                playerInventoryUI?.RefreshUI();
                
                int actuallyAdded = initialQuantity - quantity;
                if (actuallyAdded > 0)
                {
                    QuestManager.Instance?.AdvanceObjective(itemToAdd.uniqueItemID, QuestObjectiveType.Collect, actuallyAdded);
                    OnItemQuantityChanged?.Invoke(itemToAdd.uniqueItemID, GetItemCount(itemToAdd));
                }
            }
            if (quantity > 0)
            {
                Debug.LogWarning($"背包已滿。未能完全添加 {itemToAdd.itemName}，剩餘 {quantity} 個。");
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"背包已滿，無法添加 {itemToAdd.itemName}。");
            return false;
        }
    }

    public bool RemoveItem(Item itemToRemove, int quantity = 1)
    {
        if (itemToRemove == null || quantity <= 0) return false;

        int quantityToRemove = quantity;
        bool removedAny = false;
        List<InventorySlot> slotsToUpdate = new List<InventorySlot>();
        for (int i = playerInventoryData.slots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = playerInventoryData.slots[i];
            if (!slot.IsEmpty() && slot.item == itemToRemove)
            {
                int amountRemoved = slot.RemoveQuantity(quantityToRemove);
                quantityToRemove -= amountRemoved;
                removedAny = true;

                if (quantityToRemove <= 0)
                {
                    break;
                }
            }
        }

        if (removedAny)
        {
            if (!isLoading)
            {
                OnInventoryChanged?.Invoke();
                playerInventoryUI?.RefreshUI();
                OnItemQuantityChanged?.Invoke(itemToRemove.uniqueItemID, GetItemCount(itemToRemove));
            }
        }
        return removedAny && quantityToRemove <= 0;
    }

    public bool RemoveItemFromSlot(InventorySlot slotToRemoveFrom, int quantity)
    {
        if (slotToRemoveFrom == null || slotToRemoveFrom.IsEmpty() || quantity <= 0)
        {
            Debug.LogWarning("嘗試從無效或空的格子移除物品，或數量無效。");
            return false;
        }
        if (quantity > slotToRemoveFrom.quantity)
        {
            Debug.LogWarning($"嘗試移除的數量 ({quantity}) 超過格子擁有的數量 ({slotToRemoveFrom.quantity})。");
            quantity = slotToRemoveFrom.quantity;
        }
        int actualRemoved = slotToRemoveFrom.RemoveQuantity(quantity);
        if (actualRemoved == quantity)
        {
            if (!isLoading)
            {
                OnInventoryChanged?.Invoke();
                playerInventoryUI?.RefreshUI();
                Item removedItem = slotToRemoveFrom.item;
                if (removedItem != null) 
                {
                    OnItemQuantityChanged?.Invoke(removedItem.uniqueItemID, GetItemCount(removedItem));
                }
            }
            return true;
        }
        else
        {
            Debug.LogError($"從格子移除物品時發生錯誤！預期移除 {quantity}, 實際移除 {actualRemoved}。");
            if (actualRemoved > 0) {
                OnInventoryChanged?.Invoke();
                playerInventoryUI?.RefreshUI();
            }
            return false;
        }
    }

    public void SpawnGroundItem(Item itemData, int quantity, Vector3 position)
    {
        if (groundItemPrefab == null)
        {
            Debug.LogError("InventoryManager 沒有在 Inspector 中指定 GroundItem Prefab！無法生成地上物品。");
            return;
        }
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning("嘗試生成無效數據或數量的地上物品。");
            return;
        }
        GameObject dropped = Instantiate(groundItemPrefab, position, Quaternion.identity);
        GroundItem gi = dropped.GetComponent<GroundItem>();
        if (gi != null)
        {
            gi.itemData = itemData;
            gi.quantity = quantity;
            gi.SetupVisuals();
            var identifier = dropped.GetComponent<UniqueObjectIdentifier>();
            if (identifier == null) identifier = dropped.AddComponent<UniqueObjectIdentifier>();
            
            identifier.SetID(System.Guid.NewGuid().ToString());
            identifier.IsRuntimeInstantiated = true;

            Debug.Log($"在 {position} 生成了地上物品：{itemData.itemName} x{quantity} (ID: {identifier.ID})");
        }
        else
        {
            Debug.LogError("指定的 GroundItem Prefab 上沒有找到 GroundItem 腳本！");
            Destroy(dropped);
        }
    }
    #endregion
    
    #region 物品查詢
    public bool HasItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int count = 0;
        foreach (InventorySlot slot in playerInventoryData.slots)
        {
            if (!slot.IsEmpty() && slot.item == item)
            {
                count += slot.quantity;
                if (count >= quantity)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public int GetItemCount(Item item)
    {
         if (item == null) return 0;
        int count = 0;
        foreach (InventorySlot slot in playerInventoryData.slots)
        {
            if (!slot.IsEmpty() && slot.item == item)
            {
                count += slot.quantity;
            }
        }
        return count;
    }
    #endregion

    #region 道具圖標
    public void StartItemSelectionMode(Item item)
    {
        itemPendingUsage = item;
        IsSelectingTarget = true;
        OnItemSelectionModeChanged?.Invoke(true);
        UpdateCursorState();
        Debug.Log($"[InventoryManager] 進入目標選擇模式: {item.itemName}");
    }

    public void ConfirmItemUsageOnMember(MemberInstance member)
    {
        if (!IsSelectingTarget || itemPendingUsage == null) return;
        if (itemPendingUsage.healAmount > 0)
        {
            member.currentHP += itemPendingUsage.healAmount;
            if (member.currentHP > member.MaxHP) member.currentHP = member.MaxHP;
            Debug.Log($"對成員 {member.BaseData.memberName} 使用了 {itemPendingUsage.itemName}，HP 恢復。");
            PartyManager.Instance.NotifyPartyUpdated();
        }
        RemoveItem(itemPendingUsage, 1);
        int remainingCount = GetItemCount(itemPendingUsage);
        if (remainingCount > 0)
        {
            UpdateCursorState();
        }
        else
        {
            if (BattleVFXManager.Instance != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;
                BattleVFXManager.Instance.ShowText(worldPos, "物品已用盡", VFXType.Text_Info);
            }
            CancelItemSelectionMode();
        }
    }

    public void CancelItemSelectionMode()
    {
        IsSelectingTarget = false;
        itemPendingUsage = null;
        OnItemSelectionModeChanged?.Invoke(false);
        if (CursorManager.Instance != null) 
        {
            CursorManager.Instance.ResetCursor();
            CursorManager.Instance.HideTooltip();
        }
        Debug.Log("[InventoryManager] 結束目標選擇模式");
    }

    private void UpdateCursorState()
    {
        if (CursorManager.Instance != null && itemPendingUsage != null)
        {
            CursorManager.Instance.SetCursorIcon(itemPendingUsage.icon);
            int count = GetItemCount(itemPendingUsage);
            CursorManager.Instance.SetQuantityText(count.ToString());
            CursorManager.Instance.ShowTooltip("左鍵: 使用\n右鍵: 取消");
        }
    }

    void Update()
    {
        if (IsSelectingTarget && Input.GetMouseButtonDown(1))
        {
            CancelItemSelectionMode();
        }
    }
    #endregion
    
    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.inventoryData.slots = playerInventoryData.slots.Select(slot => 
        {
            if (slot.IsEmpty())
            {
                return new InventorySlotData { itemID = null, quantity = 0 };
            }
            else
            {
                return new InventorySlotData { itemID = slot.item.uniqueItemID, quantity = slot.quantity };
            }
        }).ToList();
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true; 
        
        if (data.inventoryData != null && data.inventoryData.slots != null)
        {
            playerInventoryData.slots.Clear();
            foreach (var slotData in data.inventoryData.slots)
            {
                if (string.IsNullOrEmpty(slotData.itemID) || slotData.quantity <= 0)
                {
                    playerInventoryData.slots.Add(new InventorySlot());
                }
                else
                {
                    Item itemTemplate = ItemDatabase.Instance.GetItemByID(slotData.itemID);
                    if (itemTemplate != null)
                    {
                        playerInventoryData.slots.Add(new InventorySlot(itemTemplate, slotData.quantity));
                    }
                    else
                    {
                        Debug.LogWarning($"讀取背包存檔時找不到 ID 為 '{slotData.itemID}' 的物品，該格子將被清空。");
                        playerInventoryData.slots.Add(new InventorySlot());
                    }
                }
            }

            while(playerInventoryData.slots.Count < playerInventoryData.capacity)
            {
                playerInventoryData.slots.Add(new InventorySlot());
            }
        }
        isLoading = false;
        OnInventoryChanged?.Invoke();
        
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SyncCollectionQuests();
        }
    }
    #endregion
}