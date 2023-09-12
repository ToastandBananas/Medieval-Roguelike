using UnityEngine;

public class ContainerInventory : Inventory
{
    [Header("Sub Inventories")]
    [SerializeField] int subInventoryCount;
    [SerializeField] ContainerInventory parentInventory;
    [SerializeField] ContainerInventory[] subInventories;

    public override void Awake()
    {
        CreateSlotCoordinates();
        SetupItems();
    }

    public override bool TryAddItem(ItemData newItemData)
    {
        if (newItemData == null || newItemData.Item() == null)
            return false;

        if (newItemData.HasBeenRandomized == false)
            newItemData.RandomizeData();

        bool itemAdded;
        if (parentInventory == this || subInventories.Length == 0 || subInventories[0] == null)
        {
            itemAdded = AddItem(newItemData);
            if (itemAdded == false)
            {
                for (int i = 0; i < subInventories.Length; i++)
                {
                    if (itemAdded) 
                        continue;

                    itemAdded = subInventories[i].AddItem(newItemData);
                }
            }
        }
        else
            itemAdded = AddItem(newItemData);

        return itemAdded;
    }

    bool AddItem(ItemData newItemData)
    {
        SlotCoordinate targetSlotCoordinate;

        // If the item data hasn't been assigned a slot coordinate, do so now
        if (newItemData.InventorySlotCoordinate() == null)
        {
            targetSlotCoordinate = GetNextAvailableSlotCoordinate(newItemData);
            if (targetSlotCoordinate != null)
            {
                newItemData.SetInventorySlotCoordinate(targetSlotCoordinate);
                targetSlotCoordinate.SetupNewItem(newItemData);
            }
        }
        else
            targetSlotCoordinate = newItemData.InventorySlotCoordinate();

        if (targetSlotCoordinate != null)
        {
            // Only add the item data if it hasn't been added yet
            if (itemDatas.Contains(newItemData) == false)
                itemDatas.Add(newItemData);

            // Show the item's icon in the inventory UI
            if (slotVisualsCreated)
            {
                InventorySlot targetSlot = GetSlotFromCoordinate(targetSlotCoordinate.coordinate.x, targetSlotCoordinate.coordinate.y);
                if (targetSlot != null)
                    SetupNewItem(targetSlot, newItemData); // Setup the slot's item data and sprites
            }

            return true;
        }

        return false;
    }
}
