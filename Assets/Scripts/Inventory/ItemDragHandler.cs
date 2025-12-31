using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryUI inventoryUI;
    private InventorySlotUI sourceSlotUI;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 startPosition;
    private Image draggedIcon;

    public void Initialize(InventoryUI ui, InventorySlotUI slot)
    {
        inventoryUI = ui;
        sourceSlotUI = slot;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>(); // Ensure it exists
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (sourceSlotUI == null || sourceSlotUI.AssignedSlot == null || sourceSlotUI.AssignedSlot.IsEmpty())
        {
            eventData.pointerDrag = null;
            return;
        }
        Debug.Log($"Begin Drag on: {sourceSlotUI.AssignedSlot.item.itemName}");
        CreateDraggedIcon();
        startPosition = draggedIcon.rectTransform.position;
        originalParent = transform;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        draggedIcon.rectTransform.position = Input.mousePosition;

    }

     private void CreateDraggedIcon()
    {
        GameObject draggedObject = new GameObject("Dragged Icon");
        draggedObject.transform.SetParent(inventoryUI.transform);
        draggedObject.transform.SetAsLastSibling();
        draggedObject.AddComponent<CanvasRenderer>();

        draggedIcon = draggedObject.AddComponent<Image>();
        draggedIcon.sprite = sourceSlotUI.AssignedSlot.item.icon;
        draggedIcon.rectTransform.sizeDelta = sourceSlotUI.GetComponent<RectTransform>().sizeDelta; // Match size
        draggedIcon.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedIcon.rectTransform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("End Drag");

        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;

        if (draggedIcon != null)
        {
            Destroy(draggedIcon.gameObject);
            draggedIcon = null;
        }

        InventorySlotUI targetSlotUI = inventoryUI.GetSlotUIUnderPointer(eventData);

        if (targetSlotUI != null && targetSlotUI != sourceSlotUI)
        {
            Debug.Log($"Dropped onto slot index: {inventoryUI.GetSlotIndex(targetSlotUI)}");
            inventoryUI.HandleDrop(sourceSlotUI, targetSlotUI);
        }
        else
        {
            Debug.Log("Drop failed or dropped on self.");
        }
    }
}