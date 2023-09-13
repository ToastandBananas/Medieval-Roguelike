using UnityEngine;

[System.Serializable]
public class InventoryLayout
{
    [SerializeField] int amountOfSlots;
    [SerializeField] int maxSlots = 20;
    [SerializeField] int maxSlotsPerRow = 10;

    public int AmountOfSlots => amountOfSlots;

    public int MaxSlots => maxSlots;

    public int MaxSlotsPerRow => maxSlotsPerRow;

    public void SetLayoutValues(int amountOfSlots, int maxSlots, int maxSlotsPerRow)
    {
        this.amountOfSlots = amountOfSlots;
        this.maxSlots = maxSlots;
        this.maxSlotsPerRow = maxSlotsPerRow;
    }

    public void SetLayoutValues(InventoryLayout inventoryLayout)
    {
        amountOfSlots = inventoryLayout.amountOfSlots;
        maxSlots = inventoryLayout.maxSlots;
        maxSlotsPerRow = inventoryLayout.maxSlotsPerRow;
    }
}
