using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory/Inventory Data")]
public class Inventory : ScriptableObject
{
    [Header("Inventory Settings")]
    public string inventoryName = "Player Inventory";
    public int capacity = 20;

    [Header("Inventory Contents")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    private void OnEnable()
    {
        while (slots.Count < capacity)
        {
            slots.Add(new InventorySlot());
        }
    }

    public void InitializeSlots()
    {
        slots.Clear();
        for (int i = 0; i < capacity; i++)
        {
            slots.Add(new InventorySlot());
        }
    }
}