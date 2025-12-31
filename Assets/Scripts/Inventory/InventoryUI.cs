using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    private Inventory targetInventory; 

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private ItemDetailsPanel detailsPanel;
    
    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RegisterInventoryUI(this);
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
        }
        else
        {
            Debug.LogError("[InventoryUI] 找不到 InventoryManager 的實例來進行註冊！");
        }
        SaveManager.OnGameLoadComplete += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
            InventoryManager.Instance.UnregisterInventoryUI(this);
        }
        SaveManager.OnGameLoadComplete -= RefreshUI;
    }
    
    void Start()
    {
        detailsPanel = FindObjectOfType<ItemDetailsPanel>(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (detailsPanel != null) detailsPanel.HideDetails();
        UISoundAutoHook.HookEntireScene();
    }

    public void InitializeUI(Inventory inventory)
    {
        targetInventory = inventory;
        if (targetInventory == null)
        {
            Debug.LogError("傳入的 Inventory Data 為空，無法初始化UI！");
            return;
        }

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();
        
        if (targetInventory.slots.Count != targetInventory.capacity)
        {
            targetInventory.InitializeSlots();
        }

        for (int i = 0; i < targetInventory.capacity; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.AssignSlot(targetInventory.slots[i]);
                slotUIs.Add(slotUI);

                ItemDragHandler dragHandler = slotGO.GetComponent<ItemDragHandler>();
                if (dragHandler == null) dragHandler = slotGO.AddComponent<ItemDragHandler>();
                dragHandler.Initialize(this, slotUI);
            }
        }
    }

    public void RefreshUI()
    {
        if (targetInventory == null && InventoryManager.Instance != null)
        {
            InitializeUI(InventoryManager.Instance.playerInventoryData);
        }
        if (targetInventory == null || slotUIs.Count != targetInventory.capacity) return;

        if (slotUIs.Count != targetInventory.slots.Count)
        {
            InitializeUI(targetInventory);
        }

        for (int i = 0; i < targetInventory.capacity; i++)
        {
            if (i < slotUIs.Count && i < targetInventory.slots.Count)
            {
                slotUIs[i].AssignSlot(targetInventory.slots[i]);
                slotUIs[i].UpdateUI();
            }
        }
    }

    public void TogglePanel()
    {
        bool isActive = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isActive);

        if (!isActive && detailsPanel != null)
        {
            detailsPanel.HideDetails();
        }
    }

    public void ShowPanel()
    {
        inventoryPanel.SetActive(true);
        RefreshUI();
    }

     public void HidePanel()
    {
        inventoryPanel.SetActive(false);
        if (detailsPanel != null)
        {
            detailsPanel.HideDetails();
        }
    }
    
    public bool IsPanelActive()
    {
        return inventoryPanel.activeSelf;
    }

    public void HandleDrop(InventorySlotUI draggedSlotUI, InventorySlotUI targetSlotUI)
    {
        if (draggedSlotUI == null || targetSlotUI == null || draggedSlotUI == targetSlotUI)
        {
            Debug.Log("HandleDrop: 無效的來源或目標，或來源與目標相同。");
            return;
        }

        InventorySlot sourceSlot = draggedSlotUI.AssignedSlot;
        InventorySlot targetSlot = targetSlotUI.AssignedSlot;

        if (sourceSlot.IsEmpty()) { return; }
        // 情況 1: 目標 Slot 為空
        if (targetSlot.IsEmpty())
        {
            Debug.Log($"物品 '{sourceSlot.item.itemName}' 移動到空格子 {GetSlotIndex(targetSlotUI)}。");
            sourceSlot.SwapData(targetSlot);
        }
        // 情況 2: 目標 Slot 不為空
        else
        {
            if (sourceSlot.item == targetSlot.item && targetSlot.item.IsStackable())
            {
                int spaceAvailable = targetSlot.GetRemainingSpace();
                if (spaceAvailable > 0)
                {
                    int amountToTransfer = Math.Min(sourceSlot.quantity, spaceAvailable);

                    if (amountToTransfer > 0)
                    {
                        Debug.Log($"堆疊物品 '{targetSlot.item.itemName}'：從格子 {GetSlotIndex(draggedSlotUI)} 轉移 {amountToTransfer} 個到格子 {GetSlotIndex(targetSlotUI)}。");
                        targetSlot.quantity += amountToTransfer;
                        sourceSlot.RemoveQuantity(amountToTransfer);
                    }
                    else
                    {
                        Debug.Log($"物品 '{targetSlot.item.itemName}' 相同但目標格子已滿，執行交換。");
                        sourceSlot.SwapData(targetSlot);
                    }
                }
                else
                {
                    Debug.Log($"物品 '{targetSlot.item.itemName}' 相同但目標格子已滿，執行交換。");
                    sourceSlot.SwapData(targetSlot);
                }
            }
            // 情況 3: 物品不同，或不可堆疊
            else
            {
                Debug.Log($"物品不同或不可堆疊，交換格子 {GetSlotIndex(draggedSlotUI)} 和 {GetSlotIndex(targetSlotUI)} 的內容。");
                sourceSlot.SwapData(targetSlot);
            }
        }
        draggedSlotUI.UpdateUI();
        targetSlotUI.UpdateUI();

        // 可選：如果詳情面板正顯示被操作的物品，也刷新詳情面板
        if (detailsPanel != null && detailsPanel.gameObject.activeSelf)
        {
            // 檢查詳情面板當前顯示的是哪個物品，如果與 source 或 target 相關，則刷新
            // (這部分邏輯需要 ItemDetailsPanel 提供方法來獲取當前物品，或 InventoryUI 追蹤哪個 Slot 被點擊)
            // 簡單起見，可以先隱藏詳情面板
            // detailsPanel.HideDetails();
        }
    }

    public int GetSlotIndex(InventorySlotUI slotUI)
    {
        return slotUIs.IndexOf(slotUI);
    }
    public InventorySlotUI GetSlotUIUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            InventorySlotUI slotUI = result.gameObject.GetComponentInParent<InventorySlotUI>(); // Check parents too
            if (slotUI != null && slotUIs.Contains(slotUI))
            {
                return slotUI;
            }
        }
        return null;
    }
}