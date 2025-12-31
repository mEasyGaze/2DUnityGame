using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum SlotType { Shop, Player }

    [Header("UI元件")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private CanvasGroup canvasGroup;

    public Item DisplayedItem { get; private set; }
    public ShopItem ShopItemStock { get; private set; }
    public InventorySlot PlayerInventorySlot { get; private set; }
    public SlotType CurrentSlotType { get; private set; }

    private ShopUI parentUI;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void Initialize(ShopUI ui, SlotType type)
    {
        parentUI = ui;
        CurrentSlotType = type;
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void DisplayShopItem(ShopItem shopItem)
    {
        ShopItemStock = shopItem;
        DisplayedItem = shopItem.item;
        PlayerInventorySlot = null;
        UpdateUI();
    }

    public void DisplayPlayerItem(InventorySlot playerSlot)
    {
        PlayerInventorySlot = playerSlot;
        DisplayedItem = playerSlot.item;
        ShopItemStock = null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (DisplayedItem == null)
        {
            ClearUI();
            return;
        }
        itemIcon.sprite = DisplayedItem.icon;
        itemIcon.enabled = true;
        if (CurrentSlotType == SlotType.Shop)
        {
            string qtyStr = ShopItemStock.quantity == -1 ? "∞" : ShopItemStock.quantity.ToString();
            quantityText.text = qtyStr;
            quantityText.enabled = true;
            priceText.text = $"${DisplayedItem.buyPrice}";
            priceText.enabled = DisplayedItem.canBeBought;
        }
        else
        {
            quantityText.text = PlayerInventorySlot.quantity > 1 ? PlayerInventorySlot.quantity.ToString() : "";
            quantityText.enabled = PlayerInventorySlot.quantity > 1;
            priceText.text = DisplayedItem.canBeSold ? $"${DisplayedItem.sellPrice}" : "不可出售";
            priceText.enabled = true;
        }
    }

    private void ClearUI()
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        quantityText.text = "";
        priceText.text = "";
        DisplayedItem = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DisplayedItem == null) return;
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            Debug.Log($"雙擊了 {DisplayedItem.itemName}");
            parentUI.RequestTransaction(this);
            lastClickTime = -1f;
        }
        else
        {
            lastClickTime = Time.time;
        }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            parentUI.ShowItemDetails(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (CurrentSlotType == SlotType.Shop)
            {
                ShopManager.Instance.BuyItem(DisplayedItem, 1);
            }
            else if (PlayerInventorySlot != null && !PlayerInventorySlot.IsEmpty())
            {
                ShopManager.Instance.SellItem(PlayerInventorySlot, 1);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DisplayedItem == null)
        {
            eventData.pointerDrag = null;
            return;
        }
        if (parentUI == null)
        {
            Debug.LogError("ShopSlotUI 的 parentUI 引用為空！拖拽無法執行。");
            eventData.pointerDrag = null;
            return;
        }
        if (parentUI.DraggedItemIcon == null)
        {
            Debug.LogError("ShopUI 上的 DraggedItemIcon 未在 Inspector 中指定！請檢查 ShopUI 的配置。");
            eventData.pointerDrag = null;
            return;
        }
        parentUI.DraggedItemIcon.sprite = DisplayedItem.icon;
        parentUI.DraggedItemIcon.gameObject.SetActive(true);
        parentUI.DraggedItemIcon.rectTransform.position = Input.mousePosition;
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentUI.DraggedItemIcon.gameObject.activeSelf)
        {
            parentUI.DraggedItemIcon.rectTransform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        parentUI.DraggedItemIcon.gameObject.SetActive(false);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        parentUI.HandleDrop(this);
    }
}