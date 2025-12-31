using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemValueText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    [Header("關聯 UI (新增)")]
    [SerializeField] private ItemDropUI itemDropUI;
    private InventorySlot currentSlot;
    private Item currentItem => currentSlot?.item;

    void Awake()
    {
        if (useButton != null) useButton.onClick.AddListener(OnUseButtonClicked);
        if (dropButton != null) dropButton.onClick.AddListener(OnDropButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(HideDetails);
        if (itemDropUI == null)
        {
            Debug.LogError("ItemDetailsPanel 沒有指定 ItemDropUI！");
        }
        HideDetails();
    }

    public void ShowDetails(InventorySlot slotToShow)
    {
        if (slotToShow == null || slotToShow.IsEmpty())
        {
            HideDetails();
            return;
        }

        currentSlot = slotToShow;
        Item item = currentItem;

        gameObject.SetActive(true);

        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = (item.icon != null);
        }
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        if (itemTypeText != null) itemTypeText.text = $"類型: {item.itemType}";
        if (itemValueText != null)
        {
            string valueStr = "";
            if(item.canBeBought) valueStr += $"購買: {item.buyPrice} ";
            if(item.canBeSold) valueStr += $"販賣: {item.sellPrice}";
            itemValueText.text = valueStr.Trim();
        }

        if (useButton != null)
        {
            bool canUse = (item.itemType == ItemType.Consumable || item.itemType == ItemType.Equipment);
            useButton.gameObject.SetActive(canUse);
        }

        if (dropButton != null)
        {
            bool canDrop = (item.itemType != ItemType.QuestItem);
            dropButton.gameObject.SetActive(canDrop);
            dropButton.interactable = canDrop;
        }
    }

    public void HideDetails()
    {
        currentSlot = null;
        gameObject.SetActive(false);
    }

    private void OnUseButtonClicked()
    {
        if (currentSlot == null || currentItem == null) return;
        if (currentItem.itemType == ItemType.Consumable)
        {
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetCursorIcon(currentItem.icon);
            }
            InventoryManager.Instance.StartItemSelectionMode(currentItem);
            var partyBattleUI = FindObjectOfType<PartyBattleUI>(true);
            if (partyBattleUI != null && !partyBattleUI.gameObject.activeSelf)
            {
                partyBattleUI.TogglePanel();
            }
            HideDetails();
            return;
        }
        Debug.Log($"嘗試直接使用/裝備 {currentItem.itemName}");

        bool consumedOrEquipped = false;

        switch (currentItem.itemType)
        {
            case ItemType.Consumable:
                Debug.Log("消耗了！");
                InventoryManager.Instance.RemoveItemFromSlot(currentSlot, 1);
                consumedOrEquipped = true;
                break;
            case ItemType.Equipment:
                Debug.Log("裝備了！");
                // 發送到裝備管理器...
                // 如果裝備系統獨立於背包，則從格子移除
                // InventoryManager.Instance.RemoveItemFromSlot(currentSlot, 1);
                // consumedOrEquipped = true; // 如果裝備後從背包移除
                break;
            default:
                break;
        }

        // 如果物品被消耗/裝備，可能需要隱藏詳情面板或更新其狀態
        if (consumedOrEquipped)
        {
            if (currentSlot.IsEmpty())
            {
                HideDetails();
            }
            else
            {
                // 否則，只刷新顯示 (如果數量變化)
                // ShowDetails(currentSlot); // 重新調用以刷新數量等 (如果UI沒自動更新)
            }
            // InventoryManager 里的 RemoveItemFromSlot 應該已經調用了 RefreshUI
        }
    }

    private void OnDropButtonClicked()
    {
        if (currentSlot == null || currentItem == null || itemDropUI == null)
        {
            Debug.LogWarning("無法拋棄：當前格子無效或未指定 ItemDropUI。");
            return;
        }
        if (currentItem.itemType == ItemType.QuestItem) {
            Debug.Log("任務物品不可拋棄。");
            return;
        }
        Debug.Log($"按下拋棄按鈕，準備打開數量選擇面板：{currentItem.itemName}");
        itemDropUI.ShowPanel(currentSlot);
        HideDetails();
    }
}