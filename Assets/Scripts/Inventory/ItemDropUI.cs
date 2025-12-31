using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ItemDropUI : MonoBehaviour
{
    [Header("UI 元素參考")]
    [SerializeField] private GameObject dropPanel;
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI maxQuantityText;

    private InventorySlot targetSlot;
    private Item targetItem;
    private int currentSelectedQuantity;

    // 事件：當確認拋棄時觸發 (可以讓其他系統監聽，例如音效)
    public event Action<Item, int> OnConfirmDropAction;

    void Awake()
    {
        confirmButton.onClick.AddListener(ConfirmDrop);
        cancelButton.onClick.AddListener(CancelDrop);
        quantitySlider.onValueChanged.AddListener(UpdateQuantityText);

        if (dropPanel != null)
        {
            dropPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void ShowPanel(InventorySlot slotToDropFrom)
    {
        if (slotToDropFrom == null || slotToDropFrom.IsEmpty())
        {
            Debug.LogWarning("嘗試為空或無效的格子顯示拋棄面板。");
            HidePanel();
            return;
        }

        targetSlot = slotToDropFrom;
        targetItem = slotToDropFrom.item;

        quantitySlider.minValue = 1;
        quantitySlider.maxValue = targetSlot.quantity;
        quantitySlider.wholeNumbers = true;
        quantitySlider.value = 1;

        UpdateQuantityText(quantitySlider.value);
        if(maxQuantityText != null) maxQuantityText.text = $"最多: {targetSlot.quantity}";
        if (dropPanel != null) dropPanel.SetActive(true);
        else gameObject.SetActive(true);

        // 可選：讓此面板處於最上層 (如果你的 UI 結構複雜)
        // transform.SetAsLastSibling();
    }

    private void HidePanel()
    {
        if (dropPanel != null) dropPanel.SetActive(false);
        else gameObject.SetActive(false);
        targetSlot = null;
        targetItem = null;
    }

    private void UpdateQuantityText(float value)
    {
        currentSelectedQuantity = Mathf.RoundToInt(value);
        if (quantityText != null)
        {
            quantityText.text = currentSelectedQuantity.ToString();
        }
    }

    private void ConfirmDrop()
    {
        if (targetSlot == null || targetItem == null)
        {
            Debug.LogError("確認拋棄時目標格子或物品為空！");
            HidePanel();
            return;
        }
        int quantityToDrop = currentSelectedQuantity;
        Debug.Log($"確認拋棄：物品 '{targetItem.itemName}', 數量 {quantityToDrop}");

        // 1. 通知 InventoryManager 從指定格子移除指定數量
        bool removed = InventoryManager.Instance.RemoveItemFromSlot(targetSlot, quantityToDrop);

        if (removed)
        {
            // 2. 如果移除成功，在玩家位置生成 GroundItem
            InventoryManager.Instance.SpawnGroundItem(targetItem, quantityToDrop, GetPlayerPosition());
            OnConfirmDropAction?.Invoke(targetItem, quantityToDrop);
        }
        else
        {
            Debug.LogWarning("從背包移除物品失敗 (可能是數量問題?)，未生成地上物品。");
        }
        HidePanel();
    }

    private void CancelDrop()
    {
        Debug.Log("取消拋棄。");
        HidePanel();
    }

    private Vector3 GetPlayerPosition()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            return player.transform.position + player.transform.up * -0.3f;
        }
        else
        {
            Debug.LogError("找不到玩家物件來確定拋棄位置！");
            return Vector3.zero;
        }
    }

    void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmDrop);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelDrop);
        if (quantitySlider != null) quantitySlider.onValueChanged.RemoveListener(UpdateQuantityText);
    }
}