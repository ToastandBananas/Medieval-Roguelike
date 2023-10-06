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

        if (myUnit != null && containerInventoryManager == myUnit.BackpackInventoryManager && myUnit.CharacterEquipment.EquipSlotHasItem(EquipSlot.Back) && myUnit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item is Backpack)
            SetupInventoryLayoutFromItem((Backpack)myUnit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item);
        else if (myUnit != null && containerInventoryManager == myUnit.QuiverInventoryManager && myUnit.CharacterEquipment.EquipSlotHasItem(EquipSlot.Quiver) && myUnit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver)
            SetupInventoryLayoutFromItem((Quiver)myUnit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item);
        else if (looseItem != null && looseItem.ItemData != null)
            SetupInventoryLayoutFromItem(looseItem.ItemData.Item);
        
        if (slotCoordinates == null)
            slotCoordinates = new List<SlotCoordinate>();

        CreateSlotCoordinates();
        SetupItems();

        hasBeenInitialized = true;
    }

    public override bool TryAddItem(ItemData newItemData, bool tryAddToExistingStacks = true)
    {
        if (newItemData == null || newItemData.Item == null)
            return false;

        if (ItemTypeAllowed(newItemData.Item.ItemType) == false)
            return false;

        if (newItemData.ShouldRandomize)
            newItemData.RandomizeData();

        bool itemAdded;
        if (containerInventoryManager.ParentInventory == this || containerInventoryManager.SubInventories.Length == 0 || containerInventoryManager.SubInventories[0] == null)
        {
            itemAdded = AddItem(newItemData, tryAddToExistingStacks);
            if (itemAdded == false)
            {
                for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
                {
                    if (itemAdded) 
                        continue;

                    itemAdded = containerInventoryManager.SubInventories[i].AddItem(newItemData, tryAddToExistingStacks);
                }
            }
        }
        else
            itemAdded = AddItem(newItemData, tryAddToExistingStacks);

        return itemAdded;
    }

    public override bool TryAddItemAt(SlotCoordinate targetSlotCoordinate, ItemData newItemData)
    {
        bool added = base.TryAddItemAt(targetSlotCoordinate, newItemData);

        if (looseItem != null && looseItem is LooseQuiverItem)
            looseItem.LooseQuiverItem.UpdateArrowMeshes();
        else if (slotVisualsCreated && myUnit != null && containerInventoryManager == myUnit.QuiverInventoryManager)
            myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

        return added;
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
            else if (item is Quiver)
            {
                Quiver quiver = item as Quiver;
                inventorySections = quiver.InventorySections;
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

            for (int i = containerInventoryManager.SubInventories.Length; i >= 0; i--)
            {
                if (i >= inventorySections.Length)
                    containerInventoryManager.SubInventories[i - 1].inventoryLayout.SetLayoutValues(0, 2, 1, 1, null, null);
            }
        }
        else if (containerInventoryManager.ParentInventory != null && containerInventoryManager.ParentInventory != this) // We only want to run this on the Parent Inventory
            containerInventoryManager.ParentInventory.SetupInventoryLayoutFromItem(item);
    }

    public ContainerInventory ParentInventory => containerInventoryManager.ParentInventory;

    public ContainerInventory[] SubInventories => containerInventoryManager.SubInventories;

    public LooseItem LooseItem => looseItem;

    public void SetLooseItem(LooseItem newLooseItem) => looseItem = newLooseItem;

    public void SetContainerInventoryManager(ContainerInventoryManager manager) => containerInventoryManager = manager;
}
