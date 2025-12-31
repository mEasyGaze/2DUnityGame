using UnityEngine;

[System.Serializable]
public class GroundItemState
{
    public string itemID;
    public int quantity;
    public Vector3 position;
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GroundItem : MonoBehaviour, ISceneSaveable 
{
    [Header("物品數據")]
    public Item itemData;
    public int quantity = 1;

    [Header("拾取提示 (可選)")]
    [SerializeField] private GameObject pickupPrompt;

    private SpriteRenderer spriteRenderer;
    private Collider2D itemCollider;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();
        if (itemCollider != null && !itemCollider.isTrigger)
        {
            Debug.LogWarning($"GroundItem '{gameObject.name}' 的 Collider2D 沒有設置為 Is Trigger，自動設置。", gameObject);
            itemCollider.isTrigger = true;
        }
        if(pickupPrompt != null) pickupPrompt.SetActive(false);
    }

    private void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        SetupVisuals();
    }

    void Start()
    {
        SetupVisuals();
        if (itemData == null)
        {
            Debug.LogError($"GroundItem '{gameObject.name}' 在 Start 時仍然沒有有效的 Item Data！Spawner 可能沒有正確賦值。", gameObject);
            if (itemCollider != null) itemCollider.enabled = false;
        }
    }

    public void SetupVisuals()
    {
        if (spriteRenderer == null) return;
        if (itemData != null)
        {
            spriteRenderer.sprite = itemData.icon;
            gameObject.name = $"GroundItem - {itemData.itemName} ({quantity})";
        } else {
            spriteRenderer.sprite = null;
            gameObject.name = "GroundItem - (No Item Data!)";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"玩家進入了 '{itemData?.itemName ?? "未知物品"}' 的拾取範圍。");
            if(pickupPrompt != null) pickupPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"玩家離開了 '{itemData?.itemName ?? "未知物品"}' 的拾取範圍。");
            if(pickupPrompt != null) pickupPrompt.SetActive(false);
        }
    }
    public bool AttemptPickup()
    {
        if (itemData == null)
        {
            Debug.LogError($"嘗試拾取 '{gameObject.name}' 但其 ItemData 為 null！", gameObject);
            return false;
        }

        Debug.Log($"玩家嘗試按下拾取鍵拾取 '{itemData.itemName}'");
        bool addedSuccessfully = InventoryManager.TriggerAddItem(itemData, quantity);

        if (addedSuccessfully)
        {
            Debug.Log($"成功拾取 {itemData.itemName} x{quantity}，銷毀地上物品。");
            // 可選：在銷毀前播放拾取音效
            // AudioManager.PlayPickupSound();
            Destroy(gameObject);
            return true;
        }
        else
        {
            Debug.LogWarning($"無法拾取 {itemData.itemName} x{quantity} (可能是背包已滿?)，物品保留在地上。");
            // 可選：播放背包已滿的音效
            // AudioManager.PlayInventoryFullSound();
            return false;
        }
    }

    public object CaptureState()
    {
        if (itemData == null) return null;

        return new GroundItemState
        {
            itemID = this.itemData.uniqueItemID,
            quantity = this.quantity,
            position = this.transform.position
        };
    }

    public void RestoreState(object stateData)
    {
        if (stateData is string stateJson)
        {
            var state = JsonUtility.FromJson<GroundItemState>(stateJson);
            this.itemData = ItemDatabase.Instance.GetItemByID(state.itemID);
            this.quantity = state.quantity;
            this.transform.position = state.position;
            
            SetupVisuals();
        }
    }
}