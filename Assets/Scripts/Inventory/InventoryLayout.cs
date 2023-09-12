using UnityEngine;

[System.Serializable]
public class InventoryLayout
{
    [SerializeField] int amountOfSlots = 20;
    [SerializeField] int maxSlots = 20;
    [SerializeField] int maxSlotsPerRow = 10;

    public int AmountOfSlots => amountOfSlots;

    public int MaxSlots => maxSlots;

    public int MaxSlotsPerRow => maxSlotsPerRow;
}
