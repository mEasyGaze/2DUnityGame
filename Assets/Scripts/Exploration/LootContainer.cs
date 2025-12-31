using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootContainerState
{
    public bool hasBeenLooted;
}

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueObjectIdentifier))]
public class LootContainer : MonoBehaviour, IInteractable, ISceneSaveable
{
    [Header("容器內容")]
    [Tooltip("定義容器內固定的物品和數量")]
    [SerializeField] private List<ItemReward> fixedLoot = new List<ItemReward>();

    [Header("交互設定")]
    [SerializeField] private float searchTime = 2f;
    [SerializeField] private bool isLocked = false;
    [Tooltip("如果上鎖，需要用來解鎖的鑰匙物品ID")]
    [SerializeField] private string keyItemID;

    [Header("狀態與視覺")]
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private string actionText = "搜刮中...";

    [Header("掉落設定")]
    [Tooltip("物品掉落在容器周圍的擴散半徑")]
    [SerializeField] private float spawnRadius = 1.0f;

    private bool hasBeenLooted = false;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact()
    {
        if (hasBeenLooted) return;

        if (isLocked)
        {
            Item key = ItemDatabase.Instance.GetItemByID(keyItemID);
            if (key == null || !InventoryManager.Instance.HasItem(key))
            {
                Debug.Log("這個容器是鎖著的，需要對應的鑰匙。");
                return;
            }
        }
        ExplorationUIManager.Instance.StartProgressBar(this.transform, searchTime, OnSearchComplete, actionText);
    }

    private void OnSearchComplete()
    {
        if (hasBeenLooted) return;
        hasBeenLooted = true;
        
        Vector3 spawnCenter = this.transform.position;

        foreach (var reward in fixedLoot)
        {
            Item item = ItemDatabase.Instance.GetItemByID(reward.itemID);
            if (item != null)
            {
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = spawnCenter + (Vector3)offset;

                InventoryManager.Instance.SpawnGroundItem(item, reward.amount, spawnPosition);
            }
        }
        Debug.Log($"從 {gameObject.name} 中搜刮了 {fixedLoot.Count} 種物品，它們已掉落在周圍。");
        ApplyLootedStateVisuals();
    }

    private void ApplyLootedStateVisuals()
    {
        if (spriteRenderer != null && openedSprite != null)
        {
            spriteRenderer.sprite = openedSprite;
        }
        var collider = GetComponent<Collider2D>();
        if(collider != null) collider.enabled = false;
    }

    #region 存檔資料
    public object CaptureState()
    {
        return new LootContainerState
        {
            hasBeenLooted = this.hasBeenLooted
        };
    }

    public void RestoreState(object stateData)
    {
        if (stateData is string stateJson)
        {
            var state = JsonUtility.FromJson<LootContainerState>(stateJson);
            
            this.hasBeenLooted = state.hasBeenLooted;
            
            if (this.hasBeenLooted)
            {
                if (spriteRenderer != null && openedSprite != null)
                {
                    spriteRenderer.sprite = openedSprite;
                }
                var collider = GetComponent<Collider2D>();
                if(collider != null) collider.enabled = false;
            }
        }
        else
        {
            Debug.LogError($"[LootContainer] RestoreState 接收到的數據類型錯誤，期望為 string，實際為 {stateData.GetType()}");
        }
    }
    #endregion
}