using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button slotButton;

    public InventorySlot AssignedSlot { get; private set; }
    private ItemDetailsPanel detailsPanel;

    void Awake()
    {
        ClearUI();
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
        detailsPanel = FindObjectOfType<ItemDetailsPanel>(true);
    }

    public void AssignSlot(InventorySlot slot)
    {
        AssignedSlot = slot;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (AssignedSlot == null || AssignedSlot.IsEmpty())
        {
            ClearUI();
        }
        else
        {
            itemIconImage.sprite = AssignedSlot.item.icon;
            itemIconImage.enabled = true;

            if (AssignedSlot.item.IsStackable() && AssignedSlot.quantity > 1)
            {
                quantityText.text = AssignedSlot.quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
            slotButton.interactable = true;
        }
    }

    private void ClearUI()
    {
        itemIconImage.sprite = null;
        itemIconImage.enabled = false;
        quantityText.text = "";
        quantityText.enabled = false;
        slotButton.interactable = false;
    }

    private void OnSlotClicked()
    {
        if (detailsPanel != null && AssignedSlot != null && !AssignedSlot.IsEmpty())
        {
            detailsPanel.ShowDetails(AssignedSlot);
        }
    }
}