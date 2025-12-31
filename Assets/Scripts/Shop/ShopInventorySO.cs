using System;
using UnityEngine;
using System.Collections.Generic;

public enum UnlockConditionType 
{ 
    QuestCompleted,
    GameEventTriggered
}

[Serializable]
public class ShopUnlockableItem
{
    [Tooltip("需要解鎖的商品及其數量。")]
    public ShopItem itemToUnlock;

    [Tooltip("解鎖此商品的條件類型。")]
    public UnlockConditionType conditionType;
    
    [Tooltip("條件的唯一標識符，例如 QuestID 或一個自定義的 GameEvent ID。")]
    public string conditionID;
}

[System.Serializable]
public class ShopItem
{
    public Item item;
    [Tooltip("庫存數量, -1 代表無限供應")]
    public int quantity;
}

[CreateAssetMenu(fileName = "New Shop Inventory", menuName = "Shop/Shop Inventory")]
public class ShopInventorySO : ScriptableObject
{
    [Header("商店基本資訊")]
    public string shopName = "雜貨店";
    public int initialFund = 1000;

    [Header("固定商品清單")]
    [Tooltip("定義此商店總是會販賣的商品及其初始庫存。")]
    public List<ShopItem> fixedStock = new List<ShopItem>();

    [Header("階段式解鎖商品")]
    [Tooltip("這些商品只有在滿足特定條件（如完成某個任務）後才會出現在商店中。")]
    public List<ShopUnlockableItem> unlockableStock = new List<ShopUnlockableItem>();
    
    [Header("刷新與隨機商品設定")]
    [Tooltip("商店庫存刷新的時間間隔（以遊戲內天數計）。設為0或負數則永不刷新。")]
    public int refreshIntervalDays = 1;

    [Tooltip("每次刷新時，從下面的隨機池中抽取的商品數量。")]
    public int randomItemsPerRefresh = 3;
    
    [Tooltip("一個商品池，商店刷新時會從中隨機抽取商品加入庫存。")]
    public List<ShopItem> randomStockPool = new List<ShopItem>();
}