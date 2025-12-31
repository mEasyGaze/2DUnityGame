using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("主面板")]
    [SerializeField] private GameObject shopPanel;

    [Header("資訊顯示")]
    [SerializeField] private TextMeshProUGUI shopNameText;
    [SerializeField] private TextMeshProUGUI playerMoneyText;
    [SerializeField] private TextMeshProUGUI traderMoneyText;

    [Header("物品容器")]
    [SerializeField] private Transform shopSlotsContainer;
    [SerializeField] private Transform playerSlotsContainer;

    [Header("預製件 (Prefab)")]
    [SerializeField] private GameObject slotPrefab;
    
    [Header("UI 拖拽視覺效果")]
    [SerializeField] private Image draggedItemIcon;

    [Header("關聯面板")]
    [SerializeField] private TransactionUI transactionUI;
    [SerializeField] private ShopItemDetailsPanel shopItemDetailsPanel; 
    [SerializeField] private Button closeButton;

    private ShopRuntimeData currentShopData;
    private List<ShopSlotUI> shopSlotUIs = new List<ShopSlotUI>();
    private List<InventorySlotUI> playerSlotUIs = new List<InventorySlotUI>();

    public Image DraggedItemIcon => draggedItemIcon;

    void Awake()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RegisterShopUI(this);
            ShopManager.Instance.OnTransactionCompleted += RefreshUI;
        }
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }
    
    void Start()
    {
        Hide();
        if(transactionUI) transactionUI.gameObject.SetActive(false);
        if(shopItemDetailsPanel) shopItemDetailsPanel.HideDetails();
        if(draggedItemIcon) draggedItemIcon.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnTransactionCompleted -= RefreshUI;
        }
    }

    public void Show(ShopRuntimeData shopData)
    {
        currentShopData = shopData;
        shopPanel.SetActive(true);
        RefreshUI();
    }

    public void Hide()
    {
        shopPanel.SetActive(false);
        if(transactionUI) transactionUI.gameObject.SetActive(false);
        if(shopItemDetailsPanel) shopItemDetailsPanel.HideDetails();
        if(draggedItemIcon) draggedItemIcon.gameObject.SetActive(false);
    }

    public void RefreshUI()
    {
        if (!shopPanel.activeSelf || currentShopData == null) return;
        
        shopNameText.text = currentShopData.BaseData.shopName; 
        playerMoneyText.text = $"你的金錢: {PlayerState.Instance.GetCurrentMoney():N0} G";
        traderMoneyText.text = $"商店資金: {currentShopData.CurrentFund:N0} G";

        if (traderMoneyText != null) 
        {
            traderMoneyText.text = $"商店資金: {currentShopData.CurrentFund:N0} G";
        }

        RenderSlots(shopSlotsContainer, currentShopData.CurrentStock);
        RenderSlots(playerSlotsContainer, InventoryManager.Instance.playerInventoryData.slots);
    }

    private void RenderSlots(Transform container, object itemSource)
    {
        foreach (Transform child in container) { Destroy(child.gameObject); }

        if (itemSource is List<ShopItem> shopItems)
        {
            shopSlotUIs.Clear();
            foreach (var shopItem in shopItems)
            {
                if(shopItem.quantity == 0) continue;

                GameObject slotGO = Instantiate(slotPrefab, container);
                var slotUI = slotGO.GetComponent<ShopSlotUI>();
                slotUI.Initialize(this, ShopSlotUI.SlotType.Shop); 
                slotUI.DisplayShopItem(shopItem);
                shopSlotUIs.Add(slotUI);
            }
        }
        else if (itemSource is List<InventorySlot> playerSlots)
        {
            foreach (var playerSlot in playerSlots)
            {
                if (playerSlot.IsEmpty()) continue;

                GameObject slotGO = Instantiate(slotPrefab, container);
                var slotUI = slotGO.GetComponent<ShopSlotUI>();
                slotUI.Initialize(this, ShopSlotUI.SlotType.Player); 
                slotUI.DisplayPlayerItem(playerSlot);
            }
        }
    }

    public void RequestTransaction(ShopSlotUI slot)
    {
        if (slot.DisplayedItem == null || currentShopData == null) return;
        
        if (slot.CurrentSlotType == ShopSlotUI.SlotType.Shop)
        {
            int maxCanBuyByMoney = slot.DisplayedItem.buyPrice > 0 ? PlayerState.Instance.GetCurrentMoney() / slot.DisplayedItem.buyPrice : int.MaxValue;
            int maxQuantity = slot.ShopItemStock.quantity == -1 ? maxCanBuyByMoney : Mathf.Min(maxCanBuyByMoney, slot.ShopItemStock.quantity);
            if (maxQuantity > 0)
            {
                transactionUI.Show(slot.DisplayedItem, TransactionUI.TransactionType.Buy, maxQuantity);
            }
        }
        else
        {
            if(slot.PlayerInventorySlot.item.canBeSold)
            {
                int maxCanSellByFund = slot.DisplayedItem.sellPrice > 0 ? currentShopData.CurrentFund / slot.DisplayedItem.sellPrice : int.MaxValue;
                int maxQuantity = Mathf.Min(slot.PlayerInventorySlot.quantity, maxCanSellByFund);

                if(maxQuantity > 0)
                {
                    transactionUI.Show(slot.PlayerInventorySlot.item, TransactionUI.TransactionType.Sell, maxQuantity, slot.PlayerInventorySlot);
                }
                else
                {
                    Debug.Log("商人沒錢了，無法向他出售物品。");
                }
            }
        }
    }

    public void ShowItemDetails(ShopSlotUI slot)
    {
        if (shopItemDetailsPanel == null) 
        {
            Debug.LogWarning("ShopUI 未指定 ShopItemDetailsPanel。");
            return;
        }

        if (slot.DisplayedItem != null)
        {
            shopItemDetailsPanel.ShowDetails(slot.DisplayedItem);
        }
        else
        {
            shopItemDetailsPanel.HideDetails();
        }
    }

    public void HandleDrop(ShopSlotUI sourceSlot)
    {
        bool onPlayerPanel = RectTransformUtility.RectangleContainsScreenPoint(playerSlotsContainer as RectTransform, Input.mousePosition);
        bool onShopPanel = RectTransformUtility.RectangleContainsScreenPoint(shopSlotsContainer as RectTransform, Input.mousePosition);
        
        if(sourceSlot.CurrentSlotType == ShopSlotUI.SlotType.Shop && onPlayerPanel)
        {
            Debug.Log("拖拽購買請求");
            RequestTransaction(sourceSlot);
        }
        else if(sourceSlot.CurrentSlotType == ShopSlotUI.SlotType.Player && onShopPanel)
        {
            Debug.Log("拖拽出售請求");
            RequestTransaction(sourceSlot);
        }
    }

    private void OnCloseButtonClicked()
    {
        ShopManager.Instance.CloseShop();
    }
}