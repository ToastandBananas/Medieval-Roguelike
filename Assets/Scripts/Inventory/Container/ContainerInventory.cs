using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using UnitSystem;

namespace InventorySystem
{
    [System.Serializable]
    public class ContainerInventory : Inventory
    {
        [SerializeField] Interactable_LooseItem looseItem;

        public ContainerInventoryManager containerInventoryManager { get; private set; }

        public ContainerInventory(Unit myUnit, Interactable_LooseItem looseItem, ContainerInventoryManager containerInventoryManager)
        {
            this.myUnit = myUnit;
            this.looseItem = looseItem;
            this.containerInventoryManager = containerInventoryManager;

            inventoryLayout = new InventoryLayout();
        }

        public override void Initialize()
        {
            if (looseItem != null && looseItem.ItemData != null)
                SetupInventoryLayoutFromItem(looseItem.ItemData.Item);
            else if (myUnit != null) 
            {
                if (containerInventoryManager == myUnit.BackpackInventoryManager && myUnit.UnitEquipment.BackpackEquipped())
                    SetupInventoryLayoutFromItem((Item_Backpack)myUnit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item);
                else if (containerInventoryManager == myUnit.BeltInventoryManager && myUnit.UnitEquipment.BeltBagEquipped())
                    SetupInventoryLayoutFromItem((Item_Belt)myUnit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Belt].Item);
                else if (containerInventoryManager == myUnit.QuiverInventoryManager && myUnit.UnitEquipment.QuiverEquipped())
                    SetupInventoryLayoutFromItem((Item_Quiver)myUnit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item);
            }

            if (slotCoordinates == null)
                slotCoordinates = new List<SlotCoordinate>();

            CreateSlotCoordinates();
            SetupItems();

            HasBeenInitialized = true;
        }

        public override bool TryAddItem(ItemData newItemData, Unit unitAdding, bool tryAddToExistingStacks = true)
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
                itemAdded = AddItem(newItemData, unitAdding, tryAddToExistingStacks);
                if (itemAdded == false)
                {
                    for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
                    {
                        if (itemAdded)
                            continue;

                        itemAdded = containerInventoryManager.SubInventories[i].AddItem(newItemData, unitAdding, tryAddToExistingStacks);
                    }
                }
            }
            else
                itemAdded = AddItem(newItemData, unitAdding, tryAddToExistingStacks);

            return itemAdded;
        }

        public override bool TryAddItemAt(SlotCoordinate targetSlotCoordinate, ItemData newItemData, Unit unitAdding)
        {
            bool added = base.TryAddItemAt(targetSlotCoordinate, newItemData, unitAdding);

            if (looseItem != null && looseItem is LooseQuiverItem)
                looseItem.LooseQuiverItem.UpdateArrowMeshes();
            else if (SlotVisualsCreated && myUnit != null && containerInventoryManager == myUnit.QuiverInventoryManager)
                myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

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

            if (containerInventoryManager.ParentInventory == this || containerInventoryManager.ParentInventory == null) // We only want to run this on the Parent Inventory
            {
                InventoryLayout[] inventorySections = null;
                if (item is Item_Backpack)
                    inventorySections = item.Backpack.InventorySections;
                else if (item is Item_Belt)
                    inventorySections = item.Belt.InventorySections;
                else if (item is Item_Quiver)
                    inventorySections = item.Quiver.InventorySections;

                if (inventorySections == null)
                {
                    Debug.LogWarning($"{item.name} is not the type of item that can be used as a container...");
                    return;
                }

                if (containerInventoryManager.SubInventories.Length < inventorySections.Length - 1)
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
                        containerInventoryManager.SubInventories[i - 1].inventoryLayout.SetLayoutValues(0, 0, 1, 1, null, null);
                }
            }
            else if (containerInventoryManager.ParentInventory != null && containerInventoryManager.ParentInventory != this)
                containerInventoryManager.ParentInventory.SetupInventoryLayoutFromItem(item);
        }

        public ContainerInventory ParentInventory => containerInventoryManager.ParentInventory;

        public ContainerInventory[] SubInventories => containerInventoryManager.SubInventories;

        public Interactable_LooseItem LooseItem => looseItem;

        public void SetLooseItem(Interactable_LooseItem newLooseItem) => looseItem = newLooseItem;

        public void SetContainerInventoryManager(ContainerInventoryManager manager) => containerInventoryManager = manager;
    }
}
