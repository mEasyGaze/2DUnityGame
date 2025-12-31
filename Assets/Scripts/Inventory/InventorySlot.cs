using System;

[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int quantity;

    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    public InventorySlot(Item item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public bool IsEmpty() => item == null || quantity <= 0;

    public int GetRemainingSpace()
    {
        if (IsEmpty()) return int.MaxValue;
        return item.maxStackAmount - quantity;
    }

    public int AddQuantity(int amountToAdd)
    {
        if (IsEmpty()) return amountToAdd;

        int spaceAvailable = GetRemainingSpace();
        int amountCanAdd = Math.Min(amountToAdd, spaceAvailable);

        quantity += amountCanAdd;
        return amountToAdd - amountCanAdd;
    }

    public int RemoveQuantity(int amountToRemove)
    {
        if (IsEmpty()) return 0;

        int amountCanRemove = Math.Min(amountToRemove, quantity);
        quantity -= amountCanRemove;

        if (quantity <= 0)
        {
            ClearSlot();
        }
        return amountCanRemove;
    }

    public void SetSlot(Item newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
        if (quantity <= 0 && item != null)
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        item = null;
        quantity = 0;
    }

    public void SwapData(InventorySlot otherSlot)
    {
        Item tempItem = item;
        int tempQuantity = quantity;

        SetSlot(otherSlot.item, otherSlot.quantity);
        otherSlot.SetSlot(tempItem, tempQuantity);
    }
}