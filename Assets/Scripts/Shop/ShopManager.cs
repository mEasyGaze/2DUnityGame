using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#region 運行數據
public class ShopRuntimeData
{
    public ShopInventorySO BaseData { get; private set; }
    public int CurrentFund { get; set; }
    public List<ShopItem> CurrentStock { get; set; }
    public int LastRefreshDay { get; set; }

    public ShopRuntimeData(ShopInventorySO so)
    {
        BaseData = so;
        ResetToDefault();
    }
    
    public void ResetToDefault()
    {
        CurrentFund = BaseData.initialFund;
        CurrentStock = BaseData.fixedStock.Select(item => new ShopItem { item = item.item, quantity = item.quantity }).ToList();
        LastRefreshDay = 0;
    }
    
    public void RefreshStock()
    {
        Debug.Log($"[ShopRuntimeData] 正在為商店 '{BaseData.shopName}' 刷新庫存...");
        ResetToDefault();

        foreach (var unlockable in BaseData.unlockableStock)
        {
            bool isUnlocked = false;
            switch (unlockable.conditionType)
            {
                case UnlockConditionType.QuestCompleted:
                    isUnlocked = QuestManager.Instance.HasQuestBeenCompleted(unlockable.conditionID);
                    break;
                case UnlockConditionType.GameEventTriggered:
                    isUnlocked = GameEventManager.Instance.HasEventBeenTriggered(unlockable.conditionID);
                    break;
            }

            if (isUnlocked)
            {
                AddItemToStock(unlockable.itemToUnlock);
                Debug.Log($"[ShopRuntimeData] 條件滿足，已解鎖商品: {unlockable.itemToUnlock.item.itemName}");
            }
        }
        
        if (BaseData.randomStockPool.Count > 0 && BaseData.randomItemsPerRefresh > 0)
        {
            List<ShopItem> poolCopy = new List<ShopItem>(BaseData.randomStockPool);
            int itemsToAdd = Mathf.Min(BaseData.randomItemsPerRefresh, poolCopy.Count);

            for (int i = 0; i < itemsToAdd; i++)
            {
                if (poolCopy.Count == 0) break;
                int randomIndex = UnityEngine.Random.Range(0, poolCopy.Count);
                AddItemToStock(poolCopy[randomIndex]);
                poolCopy.RemoveAt(randomIndex);
            }
        }
        LastRefreshDay = WorldTimeSystem.Instance.CurrentDay;
    }
    
    private void AddItemToStock(ShopItem itemToAdd)
    {
        ShopItem existingItem = CurrentStock.FirstOrDefault(si => si.item == itemToAdd.item);
        if (existingItem != null)
        {
            if (existingItem.quantity != -1)
            {
                existingItem.quantity += itemToAdd.quantity;
            }
        }
        else
        {
            CurrentStock.Add(new ShopItem { item = itemToAdd.item, quantity = itemToAdd.quantity });
        }
    }
}
#endregion

#region 管理器
public class ShopManager : MonoBehaviour, IGameSaveable
{
    public static ShopManager Instance { get; private set; }
    private ShopUI shopUI;

    private Dictionary<string, ShopRuntimeData> traderRuntimeData = new Dictionary<string, ShopRuntimeData>();
    private Dictionary<string, NPC> npcDatabase = new Dictionary<string, NPC>();
    private ShopRuntimeData currentShop;
    private NPC currentTrader;

    public event Action OnTransactionCompleted;
    public event Action<string> OnShopClosed;
    private string dialogueSegmentToContinue;
    
    private bool isLoading = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SaveManager.Instance.Register(this);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        CacheAllNPCs();
    }

    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CacheAllNPCs();
    }

    private void CacheAllNPCs()
    {
        npcDatabase.Clear();
        var allNpcs = FindObjectsOfType<NPC>(true);
        foreach (var npc in allNpcs)
        {
            if (!string.IsNullOrEmpty(npc.GetNpcID()) && !npcDatabase.ContainsKey(npc.GetNpcID()))
            {
                npcDatabase.Add(npc.GetNpcID(), npc);
            }
        }
        Debug.Log($"[ShopManager] 已緩存 {npcDatabase.Count} 個 NPC。");
    }
    
    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscape += HandleEscape;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscape -= HandleEscape;
        }
    }

    public void RegisterShopUI(ShopUI ui)
    {
        shopUI = ui;
    }

    public void OpenShop(ShopInventorySO shopData, NPC trader, string continueSegmentID)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.InShop);
        }
        if (shopUI == null) { Debug.LogError("商店UI未註冊!"); return; }

        currentTrader = trader;
        string traderID = trader.GetNpcID(); 

        if (!traderRuntimeData.TryGetValue(traderID, out currentShop))
        {
            Debug.Log($"[ShopManager] 首次與商人 '{traderID}' 互動，正在創建運行時數據...");
            currentShop = new ShopRuntimeData(shopData);
            currentShop.RefreshStock(); 
            traderRuntimeData[traderID] = currentShop;
        }

        int currentDay = WorldTimeSystem.Instance.CurrentDay;
        if (currentShop.BaseData.refreshIntervalDays > 0 &&
            currentDay >= currentShop.LastRefreshDay + currentShop.BaseData.refreshIntervalDays)
        {
            Debug.Log($"[ShopManager] 商人 '{traderID}' 的庫存已過期，正在刷新...");
            currentShop.RefreshStock();
        }
        dialogueSegmentToContinue = continueSegmentID;
        shopUI.Show(currentShop);
    }

    public void CloseShop()
    {
        if (shopUI != null) shopUI.Hide();
        
        if (!string.IsNullOrEmpty(dialogueSegmentToContinue))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.InDialogue);
            }
            OnShopClosed?.Invoke(dialogueSegmentToContinue);
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Exploration);
            }
            DialogueManager.Instance.EndDialogue();
        }
        currentShop = null;
        currentTrader = null;
        dialogueSegmentToContinue = null;
    }
    
    private void HandleEscape()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.InShop)
        {
            Debug.Log("[ShopManager] 透過 ESC 鍵關閉商店。");
            CloseShop();
        }
    }

    public bool BuyItem(Item itemToBuy, int quantity)
    {
        if (itemToBuy == null || quantity <= 0 || currentShop == null) return false;

        ShopItem shopItem = currentShop.CurrentStock.Find(si => si.item == itemToBuy);
        if (shopItem == null) { Debug.LogWarning($"商店不販賣 {itemToBuy.itemName}。"); return false; }
        if (shopItem.quantity != -1 && shopItem.quantity < quantity) { Debug.LogWarning($"商品 {itemToBuy.itemName} 庫存不足。"); return false; }

        int totalCost = itemToBuy.buyPrice * quantity;
        if (!PlayerState.Instance.SpendMoney(totalCost)) { Debug.LogWarning("金錢不足，無法購買。"); return false; }

        InventoryManager.TriggerAddItem(itemToBuy, quantity);
        if (shopItem.quantity != -1)
        {
            shopItem.quantity -= quantity;
        }
        currentShop.CurrentFund += totalCost;
        Debug.Log($"成功購買 {itemToBuy.itemName} x{quantity}, 花費 {totalCost} 元。");
        if (!isLoading) OnTransactionCompleted?.Invoke();
        return true;
    }

    public bool SellItem(InventorySlot playerSlot, int quantity)
    {
        if (playerSlot == null || playerSlot.IsEmpty() || quantity <= 0 || currentShop == null) return false;
        
        Item itemToSell = playerSlot.item;
        if (!itemToSell.canBeSold) { Debug.LogWarning($"物品 {itemToSell.itemName} 不可販賣。"); return false; }
        
        int totalValue = itemToSell.sellPrice * quantity;
        if(currentShop.CurrentFund < totalValue) { Debug.LogWarning($"商人資金不足，無法收購。"); return false; }

        bool removed = InventoryManager.Instance.RemoveItemFromSlot(playerSlot, quantity);
        if (!removed) { Debug.LogError("從玩家背包移除物品失敗，交易取消。"); return false; }
        
        PlayerState.Instance.AddMoney(totalValue);
        currentShop.CurrentFund -= totalValue;

        ShopItem shopItem = currentShop.CurrentStock.Find(si => si.item == itemToSell);
        if (shopItem != null) 
        {
            if (shopItem.quantity != -1) shopItem.quantity += quantity;
        }
        else
        {
            currentShop.CurrentStock.Add(new ShopItem { item = itemToSell, quantity = quantity });
        }
        Debug.Log($"成功販賣 {itemToSell.itemName} x{quantity}, 獲得 {totalValue} 元。");
        if (!isLoading) OnTransactionCompleted?.Invoke();
        return true;
    }
    #endregion

    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.shopManagerData.traderData.Clear();

        foreach (var pair in traderRuntimeData)
        {
            var stockToSave = pair.Value.CurrentStock
                .Select(si => new ShopItemForSave { itemID = si.item.uniqueItemID, quantity = si.quantity })
                .ToList();

            var runtimeData = new ShopRuntimeDataForSave
            {
                traderNpcID = pair.Key, 
                currentFund = pair.Value.CurrentFund,
                currentStock = stockToSave,
                lastRefreshDay = pair.Value.LastRefreshDay
            };
            data.shopManagerData.traderData.Add(runtimeData);
        }
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true;
        traderRuntimeData.Clear();

        if (data.shopManagerData == null || data.shopManagerData.traderData == null)
        {
            isLoading = false;
            return;
        }
            
        foreach (var savedData in data.shopManagerData.traderData)
        {
            string npcID = savedData.traderNpcID;
            if (npcDatabase.TryGetValue(npcID, out NPC correspondingNpc))
            {
                ShopInventorySO shopTemplate = correspondingNpc.GetShopInventory();
                if (shopTemplate == null)
                {
                    Debug.LogWarning($"[ShopManager] 找到了 NPC '{npcID}'，但他沒有商店數據模板，無法恢復其商店狀態。");
                    continue;
                }
                ShopRuntimeData runtimeData = new ShopRuntimeData(shopTemplate);
                runtimeData.CurrentFund = savedData.currentFund;
                runtimeData.LastRefreshDay = savedData.lastRefreshDay;
                
                runtimeData.CurrentStock = savedData.currentStock
                    .Select(savedItem => {
                        Item itemTemplate = ItemDatabase.Instance.GetItemByID(savedItem.itemID);
                        if (itemTemplate != null)
                        {
                            return new ShopItem { item = itemTemplate, quantity = savedItem.quantity };
                        }
                        return null;
                    })
                    .Where(item => item != null)
                    .ToList();
                
                traderRuntimeData[npcID] = runtimeData;
            }
            else
            {
                Debug.LogWarning($"[ShopManager] 在存檔中發現了商人 '{npcID}' 的數據，但在當前場景中找不到對應的 NPC。");
            }
        }
        Debug.Log($"[ShopManager] 已從存檔中恢復了 {traderRuntimeData.Count} 個商人的數據。");
        isLoading = false;
    }
    #endregion
}