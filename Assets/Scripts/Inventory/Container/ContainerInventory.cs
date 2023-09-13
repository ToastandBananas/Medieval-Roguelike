using UnityEngine;

[System.Serializable]
public class ContainerInventory : Inventory
{
    ContainerInventoryManager containerInventoryManager;

    public override void Initialize()
    {
        if (myUnit != null && myUnit.CharacterEquipment().EquipSlotHasItem(EquipSlot.Back) && myUnit.CharacterEquipment().EquippedItemDatas()[(int)EquipSlot.Back].Item().IsBackpack())
            SetupInventoryLayoutFromBackpack((Backpack)myUnit.CharacterEquipment().EquippedItemDatas()[(int)EquipSlot.Back].Item());

        CreateSlotCoordinates();
        SetupItems();
    }

    public override bool TryAddItem(ItemData newItemData)
    {
        if (newItemData == null || newItemData.Item() == null)
            return false;

        if (newItemData.ShouldRandomize)
            newItemData.RandomizeData();

        bool itemAdded;
        if (containerInventoryManager.ParentInventory == this || containerInventoryManager.SubInventories.Length == 0 || containerInventoryManager.SubInventories[0] == null)
        {
            itemAdded = AddItem(newItemData);
            if (itemAdded == false)
            {
                for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
                {
                    if (itemAdded) 
                        continue;

                    itemAdded = containerInventoryManager.SubInventories[i].AddItem(newItemData);
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

    public void SetupSlots(ContainerSlotGroup containerSlotGroup)
    {
        slots = containerSlotGroup.Slots;
        slotsParent = containerSlotGroup.transform;
        containerSlotGroup.SetupRectTransform(inventoryLayout);
        InventoryUI.Instance.CreateSlotVisuals(this, slots, slotsParent);
    }

    public void ClearSlotsListReference() => slots = null;

    public void SetupInventoryLayoutFromBackpack(Backpack backpack)
    {
        if (backpack != null && containerInventoryManager.ParentInventory == this || containerInventoryManager.ParentInventory == null)
        {
            for (int i = 0; i < backpack.InventorySections.Length; i++)
            {
                if (i == 0)
                    inventoryLayout.SetLayoutValues(backpack.InventorySections[i]);
                else
                    containerInventoryManager.SubInventories[i - 1].inventoryLayout.SetLayoutValues(backpack.InventorySections[i]);
            }
        }
    }

    public ContainerInventory ParentInventory => containerInventoryManager.ParentInventory;

    public ContainerInventory[] SubInventories => containerInventoryManager.SubInventories;

    public void SetContainerInventoryManager(ContainerInventoryManager manager) => containerInventoryManager = manager;
}
