using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootDropItem
{
    public Item item;
    [Range(0f, 1f)]
    public float dropChance = 1f;
    public int minQuantity = 1;
    public int maxQuantity = 1;
}

public struct LootResult
{
    public List<Item> items;
    public int money;
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Exploration/Loot Table")]
public class LootTableSO : ScriptableObject
{
    [Header("金錢掉落設定")]
    [Range(0f, 1f)]
    [Tooltip("掉落金錢的總體機率")]
    public float moneyDropChance = 0.5f;
    public int minMoney = 5;
    public int maxMoney = 20;

    [Header("物品掉落設定")]
    [Tooltip("定義所有可能掉落的物品及其各自的機率")]
    public List<LootDropItem> possibleItems = new List<LootDropItem>();

    public LootResult GetLoot()
    {
        LootResult result = new LootResult
        {
            items = new List<Item>(),
            money = 0
        };

        if (Random.value <= moneyDropChance)
        {
            result.money = Random.Range(minMoney, maxMoney + 1);
        }
        foreach (var drop in possibleItems)
        {
            if (Random.value <= drop.dropChance)
            {
                int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                for (int i = 0; i < quantity; i++)
                {
                    result.items.Add(drop.item);
                }
            }
        }
        return result;
    }
}