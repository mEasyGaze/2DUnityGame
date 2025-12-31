using UnityEngine;
using System;
public enum ItemType { Material, Equipment, Consumable, QuestItem, Misc }
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class Item : ScriptableObject
{
    [Header("基本資訊")]
    public string itemName = "New Item";
    [TextArea(3, 5)]
    public string description = "Item Description";
    public Sprite icon = null;
    public ItemType itemType = ItemType.Misc;
    public int maxStackAmount = 1;

    [Header("識別")]
    [Tooltip("Unique ID for this item type. Generate one if needed.")]
    public string uniqueItemID;

    [Header("交易資訊")]
    public bool canBeBought = true;
    public int buyPrice = 10;
    public bool canBeSold = true;
    public int sellPrice = 5;

    [Header("消耗品效果 (僅當類型為 Consumable)")]
    [Tooltip("如果物品類型是 Consumable，則恢復的生命值。")]
    public int healAmount = 0;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueItemID))
        {
            uniqueItemID = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
        if (itemType != ItemType.Consumable)
        {
            healAmount = 0;
        }
    }

    public bool IsStackable()
    {
        return maxStackAmount > 1;
    }
}