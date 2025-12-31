using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemDetailsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private Button closeButton;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(HideDetails);
        HideDetails();
    }

    public void ShowDetails(Item item)
    {
        if (item == null)
        {
            HideDetails();
            return;
        }
        detailsPanel.SetActive(true);
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = (item.icon != null);
        }
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        if (itemTypeText != null) itemTypeText.text = $"類型: {item.itemType}";
    }

    public void HideDetails()
    {
        if(detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
    }
}