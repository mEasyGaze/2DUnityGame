using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class BattleItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private System.Action<Item> onItemClickedCallback;
    private Inventory targetInventory;

    void Awake()
    {
        if (itemPanel == null) itemPanel = this.gameObject;
        itemPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            this.targetInventory = InventoryManager.Instance.playerInventoryData;
            InventoryManager.Instance.OnInventoryChanged += RefreshPanel;
            RefreshPanel();
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshPanel;
        }
    }

    public void ShowPanel(System.Action<Item> onItemClicked)
    {
        this.onItemClickedCallback = onItemClicked;
        itemPanel.SetActive(true);
        RefreshPanel();
    }

    public void HidePanel()
    {
        itemPanel.SetActive(false);
    }

    private void RefreshPanel()
    {
        if (!itemPanel.activeSelf) return;

        if (targetInventory == null)
        {
            if (InventoryManager.Instance != null)
            {
                targetInventory = InventoryManager.Instance.playerInventoryData;
            }
            else
            {
                Debug.LogError("BattleItemUI 無法獲取 targetInventory！");
                return;
            }
        }

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        var consumableSlots = targetInventory.slots
            .Where(s => !s.IsEmpty() && s.item.itemType == ItemType.Consumable)
            .ToList();

        foreach (var slotData in consumableSlots)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.AssignSlot(slotData);

                var button = slotUI.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnSlotClicked(slotData.item));
                }
                slotUIs.Add(slotUI);
            }
        }
    }

    private void OnSlotClicked(Item clickedItem)
    {
        if (onItemClickedCallback != null)
        {
            onItemClickedCallback(clickedItem);
        }
    }
}