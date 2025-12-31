using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueObjectIdentifier))]
public class InteractableBarrier : MonoBehaviour, IInteractable
{  
    [Header("移除設定")]
    [SerializeField] private string requiredItemID;
    [SerializeField] private bool consumeItem = true;
    [SerializeField] private float removalTime = 1.5f;
    [SerializeField] private string actionText = "移除中...";

    private bool isBeingRemoved = false; 
    
    public void Interact()
    {
        if (isBeingRemoved) return;
        Item requiredItem = ItemDatabase.Instance.GetItemByID(requiredItemID);
        if (requiredItem == null)
        {
            Debug.LogError($"在物品資料庫中找不到ID為 {requiredItemID} 的物品！");
            return;
        }

        if (InventoryManager.Instance.HasItem(ItemDatabase.Instance.GetItemByID(requiredItemID)))
        {
            isBeingRemoved = true;
            if (removalTime > 0)
            {
                ExplorationUIManager.Instance.StartProgressBar(this.transform, removalTime, OnRemovalComplete, actionText);
            }
            else
            {
                OnRemovalComplete();
            }
        }
        else
        {
            Debug.Log($"缺少 {requiredItem.itemName} 來移除這個障礙物。");
        }
    }

    private void OnRemovalComplete()
    {
        Item requiredItem = ItemDatabase.Instance.GetItemByID(requiredItemID);
        if (requiredItem == null || !InventoryManager.Instance.HasItem(requiredItem)) 
        {
            isBeingRemoved = false;
            return;
        }
        Debug.Log($"使用物品移除了障礙物 {gameObject.name}");
        if (consumeItem)
        {
            InventoryManager.Instance.RemoveItem(requiredItem, 1);
        }
        
        var identifier = GetComponent<UniqueObjectIdentifier>();
        if (identifier != null && ScenePersistenceManager.Instance != null)
        {
            ScenePersistenceManager.Instance.RecordObjectDestruction(identifier.ID);
        }
        Destroy(gameObject); 
    }
}