using UnityEngine;
using System.Collections.Generic;
using System.Linq;
    
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<Item> allItems;

    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("ItemDatabase not found in Resources folder! Please create one.");
                }
            }
            return _instance;
        }
    }
    
    public Item GetItemByID(string uniqueID)
    {
        return allItems.FirstOrDefault(item => item.uniqueItemID == uniqueID);
    }
}