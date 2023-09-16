using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ContainerInventory : Inventory
{
    [SerializeField] LooseItem looseItem;

    public ContainerInventoryManager containerInventoryManager { get; private set; }

    public ContainerInventory(Unit myUnit, LooseItem looseItem, ContainerInventoryManager containerInventoryManager)
    {
        this.myUnit = myUnit;
        this.looseItem = looseItem;
        this.containerInventoryManager = containerInventoryManager;

        inventoryLayout = new InventoryLayout();
    }

    public override void Initialize()
    {
        // TODO: Figure out out to discern between an equipped container vs one that's in a Unit's inventory and then set it up based off its Item

        if (myUnit != null && myUnit.CharacterEquipment.EquipSlotHasItem(EquipSlot.Back) && myUnit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item.IsBag())
            SetupInventoryLayoutFromItem((Backpack)myUnit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item);
        else if (looseItem != null && looseItem.ItemData != null)
            SetupInventoryLayoutFromItem(looseItem.ItemData.Item);

        slotCoordinates = new List<SlotCoordinate>();
        CreateSlotCoordinates();
        SetupItems();

        hasBeenInitialized = true;
    }

    public override bool TryAddItem(ItemData newItemData)
    {
        if (newItemData == null || newItemData.Item == null)
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
        CreateSlotVisuals();
    }

    public void SetupInventoryLayoutFromItem(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("Item is null for this container inventory...");
            return;
        }

        if (containerInventoryManager.ParentInventory == this || containerInventoryManager.ParentInventory == null)
        {
            InventoryLayout[] inventorySections = null;
            if (item is Backpack)
            {
                Backpack backpack = item as Backpack;
                inventorySections = backpack.InventorySections;
            }

            if (inventorySections == null)
            {
                Debug.LogWarning($"{item.name} is not the type of item that can be used as a container...");
                return;
            }

            if (containerInventoryManager.SubInventories.Length < inventorySections.Length)
                containerInventoryManager.IncreaseSubInventoriesArraySize(inventorySections.Length - 1);

            for (int i = 0; i < inventorySections.Length; i++)
            {
                if (i == 0)
                    inventoryLayout.SetLayoutValues(inventorySections[i]);
                else
                    containerInventoryManager.SubInventories[i - 1].inventoryLayout.SetLayoutValues(inventorySections[i]);
            }
        }
        else if (containerInventoryManager.ParentInventory != null && containerInventoryManager.ParentInventory != this) // We only want to run this on the Parent Inventory
            containerInventoryManager.ParentInventory.SetupInventoryLayoutFromItem(item);
    }

    public ContainerInventory ParentInventory => containerInventoryManager.ParentInventory;

    public ContainerInventory[] SubInventories => containerInventoryManager.SubInventories;

    public LooseItem LooseItem => looseItem;

    public void SetContainerInventoryManager(ContainerInventoryManager manager) => containerInventoryManager = manager;
}
