using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransactionUI : MonoBehaviour
{
    public enum TransactionType { Buy, Sell }

    [Header("UI 元件")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TMP_InputField quantityInputField;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Item targetItem;
    private InventorySlot targetPlayerSlot;
    private TransactionType currentType;
    private int currentQuantity;
    private int unitPrice;

    void Awake()
    {
        quantitySlider.onValueChanged.AddListener(OnSliderChanged);
        quantityInputField.onValueChanged.AddListener(OnInputChanged);
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
    }

    public void Show(Item item, TransactionType type, int maxQuantity, InventorySlot playerSlot = null)
    {
        if (maxQuantity <= 0) return;
        targetItem = item;
        currentType = type;
        targetPlayerSlot = playerSlot;
        transform.SetAsLastSibling();
        panel.SetActive(true);
        titleText.text = (type == TransactionType.Buy) ? "購買物品" : "販賣物品";
        itemNameText.text = item.itemName;
        unitPrice = (type == TransactionType.Buy) ? item.buyPrice : item.sellPrice;
        
        quantitySlider.minValue = 1;
        quantitySlider.maxValue = maxQuantity;
        quantitySlider.value = 1;
        
        OnSliderChanged(1);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void OnSliderChanged(float value)
    {
        currentQuantity = Mathf.RoundToInt(value);
        quantityInputField.text = currentQuantity.ToString();
        UpdateTotalPrice();
    }
    
    private void OnInputChanged(string value)
    {
        if (int.TryParse(value, out int inputQty))
        {
            inputQty = Mathf.Clamp(inputQty, (int)quantitySlider.minValue, (int)quantitySlider.maxValue);
            currentQuantity = inputQty;
            quantitySlider.value = currentQuantity;
        }
        UpdateTotalPrice();
    }

    private void UpdateTotalPrice()
    {
        totalPriceText.text = $"總價: {(currentQuantity * unitPrice):N0}";
    }

    private void OnConfirm()
    {
        if (currentType == TransactionType.Buy)
        {
            ShopManager.Instance.BuyItem(targetItem, currentQuantity);
        }
        else
        {
            ShopManager.Instance.SellItem(targetPlayerSlot, currentQuantity);
        }
        Hide();
    }
}