using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using UnitSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    [System.Serializable]
    public class Inventory
    {
        [SerializeField] protected Unit myUnit;

        [Header("Slot Info")]
        [SerializeField] protected InventoryLayout inventoryLayout;
        int maxSlotsPerColumn;

        [Header("Items in Inventory")]
        [SerializeField] protected List<ItemData> itemDatas = new();

        public bool SlotVisualsCreated { get; protected set; }
        public bool HasBeenInitialized { get; protected set; }

        protected List<InventorySlot> slots;
        protected List<SlotCoordinate> slotCoordinates = new();

        protected Transform slotsParent;

        public virtual void Initialize()
        {
            slotCoordinates ??= new();

            CreateSlotCoordinates();
            SetSlotsList();

            if (myUnit != null && myUnit.IsPlayer)
                CreateSlotVisuals();
            else
                SetupItems();

            HasBeenInitialized = true;
        }

        public virtual bool TryAddItem(ItemData newItemData, Unit unitAdding, bool tryAddToExistingStacks = true)
        {
            if (newItemData == null || newItemData.Item == null)
                return false;

            if (ItemTypeAllowed(newItemData.Item.ItemType) == false)
                return false;

            if (newItemData.ShouldRandomize)
                newItemData.RandomizeData();

            return AddItem(newItemData, unitAdding, tryAddToExistingStacks);
        }

        protected bool AddItem(ItemData newItemData, Unit unitAdding, bool tryAddToExistingStacks = true)
        {
            Inventory originalInventory = newItemData.MyInventory;

            // If the new Item is a Shield, remove any projectiles and try to add them to the Unit's Inventories
            TryTakeStuckProjectiles(newItemData);

            // Combine stacks if possible
            if (newItemData.Item.MaxStackSize > 1 && tryAddToExistingStacks)
            {
                int startingStackSize = newItemData.CurrentStackSize;
                for (int i = 0; i < itemDatas.Count; i++)
                {
                    if (newItemData.CurrentStackSize <= 0)
                        break;

                    if (itemDatas[i] == newItemData || newItemData.IsEqual(itemDatas[i]) == false)
                        continue;

                    CombineStacks(newItemData, itemDatas[i]);

                    if (SlotVisualsCreated)
                        GetSlotFromItemData(itemDatas[i]).InventoryItem.UpdateStackSizeVisuals();
                }

                // If the entire stack was combined with another stack
                if (newItemData.CurrentStackSize <= 0)
                {
                    if (originalInventory != null && originalInventory != this)
                        originalInventory.RemoveItem(newItemData, true);

                    if (unitAdding != null && originalInventory != this)
                        unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, startingStackSize, null);

                    return true;
                }
                // If there's some left in the stack
                else
                {
                    if (newItemData.MyInventory != null && newItemData.MyInventory.SlotVisualsCreated)
                        newItemData.MyInventory.GetSlotFromItemData(newItemData).InventoryItem.UpdateStackSizeVisuals();

                    // If some was added to other stacks, queue an InventoryAction for the amount added
                    if (startingStackSize != newItemData.CurrentStackSize && unitAdding != null && originalInventory != this)
                        unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, startingStackSize - newItemData.CurrentStackSize, null);
                }
            }

            // If the item data hasn't been assigned a slot coordinate, do so now
            SlotCoordinate targetSlotCoordinate;
            if (newItemData.InventorySlotCoordinate == null || newItemData.InventorySlotCoordinate.myInventory != this)
            {
                targetSlotCoordinate = GetNextAvailableSlotCoordinate(newItemData);
                if (targetSlotCoordinate != null)
                {
                    newItemData.SetInventorySlotCoordinate(targetSlotCoordinate);
                    targetSlotCoordinate.SetupNewItem(newItemData);
                }
            }
            else
                targetSlotCoordinate = newItemData.InventorySlotCoordinate;

            if (targetSlotCoordinate != null)
            {
                if (originalInventory != null && originalInventory != this)
                    originalInventory.RemoveItem(newItemData, false);

                // Only add the item data if it hasn't been added yet
                if (itemDatas.Contains(newItemData) == false)
                    itemDatas.Add(newItemData);

                // Show the item's icon in the inventory UI
                if (SlotVisualsCreated)
                {
                    InventorySlot targetSlot = GetSlotFromCoordinate(targetSlotCoordinate.coordinate.x, targetSlotCoordinate.coordinate.y);
                    if (targetSlot != null)
                        SetupNewItem(targetSlot, newItemData); // Setup the slot's item data and sprites
                }

                if (unitAdding != null && originalInventory != this && InventoryUI.lastInventoryInteractedWith != this)
                    unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null);

                if (myUnit != null)
                    myUnit.Stats.UpdateCarryWeight();

                return true;
            }

            if (myUnit != null)
                myUnit.Stats.UpdateCarryWeight();
            return false;
        }

        public virtual bool TryAddItemAt(SlotCoordinate targetSlotCoordinate, ItemData newItemData, Unit unitAdding)
        {
            if (ItemTypeAllowed(newItemData.Item.ItemType) == false)
            {
                if (InventoryUI.isDraggingItem)
                    InventoryUI.ReplaceDraggedItem();
                return false;
            }

            InventoryUI.OverlappingMultipleItems(targetSlotCoordinate, newItemData, out SlotCoordinate overlappedItemsParentSlotCoordinate, out int overlappedItemCount);

            // Check if there's multiple items in the way or if the position is invalid
            if (overlappedItemCount >= 2)
            {
                if (InventoryUI.isDraggingItem)
                    InventoryUI.ReplaceDraggedItem();
                return false;
            }

            Inventory originalInventory = newItemData.MyInventory;

            // If there's only one item in the way
            if (overlappedItemCount == 1)
            {
                // Get a reference to the overlapped item's data and parent slot before we clear it out
                ItemData overlappedItemsData = overlappedItemsParentSlotCoordinate.itemData;

                // Remove the highlighting
                if (SlotVisualsCreated)
                    GetSlotFromCoordinate(targetSlotCoordinate).RemoveSlotHighlights();

                // If we're placing an item directly on top of the same type of item that is stackable and has more room in its stack
                if (overlappedItemsParentSlotCoordinate == targetSlotCoordinate && newItemData.Item.MaxStackSize > 1 && overlappedItemsData.CurrentStackSize < overlappedItemsData.Item.MaxStackSize && newItemData.IsEqual(overlappedItemsData))
                {
                    int startingStackSize = newItemData.CurrentStackSize;
                    CombineStacks(newItemData, overlappedItemsData);

                    // Update the overlapped item's stack size text
                    if (SlotVisualsCreated)
                    {
                        GetSlotFromCoordinate(overlappedItemsParentSlotCoordinate).InventoryItem.UpdateStackSizeVisuals();

                        if (newItemData.CurrentStackSize > 0 && originalInventory != null)
                        {
                            InventorySlot slot = originalInventory.GetSlotFromItemData(newItemData);
                            if (slot != null)
                            {
                                if (InventoryUI.isDraggingItem && InventoryUI.DraggedItem.ItemData == slot.ParentSlot().GetItemData())
                                    slot.HideItemIcon();
                                else
                                    slot.InventoryItem.UpdateStackSizeVisuals();
                            }
                        }
                    }

                    if (InventoryUI.isDraggingItem)
                    {
                        // If the dragged item has been depleted
                        if (newItemData.CurrentStackSize <= 0)
                        {
                            // If the slots are in different inventories
                            RemoveFromOrigin(newItemData, unitAdding);

                            // Hide the dragged item
                            InventoryUI.DisableDraggedItem();
                        }
                        else // If there's still some left in the dragged item's stack
                        {
                            // Update the dragged item's stack size and text
                            InventoryUI.DraggedItem.UpdateStackSizeVisuals();

                            // Re-enable the highlighting
                            GetSlotFromCoordinate(targetSlotCoordinate).HighlightSlots();

                            // Queue an InventoryAction for the amount added to the stack
                            if (unitAdding != null && originalInventory != this)
                                unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, startingStackSize - newItemData.CurrentStackSize, null);
                        }
                    }
                }
                else // If we're placing the item on top of another item
                {
                    // Clear out the overlapped item
                    Slot overlappedParentSlot = GetSlotFromCoordinate(overlappedItemsParentSlotCoordinate);
                    if (overlappedParentSlot.InventoryItem.MyInventory != null)
                        overlappedParentSlot.InventoryItem.MyInventory.RemoveItem(overlappedItemsData, true);

                    // If the slots are in different inventories
                    RemoveFromOrigin(newItemData, unitAdding);

                    // Setup the target slot's item data and sprites
                    SetupNewItem(GetSlotFromCoordinate(targetSlotCoordinate), newItemData);
                    targetSlotCoordinate.SetupNewItem(newItemData);

                    // If the new Item is a Shield, remove any projectiles and try to add them to the Unit's Inventories
                    TryTakeStuckProjectiles(newItemData);

                    if (itemDatas.Contains(newItemData) == false)
                        itemDatas.Add(newItemData);

                    // Setup the dragged item's data and sprite and start dragging the new item
                    InventoryUI.SetupDraggedItem(overlappedItemsData, null, this);

                    // Re-enable the highlighting
                    GetSlotFromCoordinate(targetSlotCoordinate).HighlightSlots();
                }
            }
            else // If there's no items in the way
            {
                // Clear out the dragged item's original slot
                if (InventoryUI.parentSlotDraggedFrom != null)
                    InventoryUI.parentSlotDraggedFrom.ClearItem();

                // If the slots are in different inventories
                RemoveFromOrigin(targetSlotCoordinate, newItemData, unitAdding);

                // Setup the target slot's item data and sprites
                if (SlotVisualsCreated)
                    SetupNewItem(GetSlotFromCoordinate(targetSlotCoordinate), newItemData);

                targetSlotCoordinate.SetupNewItem(newItemData);

                // If the new Item is a Shield, remove any projectiles and try to add them to the Unit's Inventories
                TryTakeStuckProjectiles(newItemData);

                if (itemDatas.Contains(newItemData) == false)
                    itemDatas.Add(newItemData);

                // Hide the dragged item
                if (InventoryUI.isDraggingItem)
                    InventoryUI.DisableDraggedItem();
            }

            if (unitAdding != null && originalInventory != this && InventoryUI.lastInventoryInteractedWith != this)
                unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null);

            if (myUnit != null)
                myUnit.Stats.UpdateCarryWeight();
            return true;
        }

        public void OnReloadProjectile(ItemData projectileItemData)
        {
            projectileItemData.AdjustCurrentStackSize(-1);

            // If there's still some in the stack
            if (projectileItemData.CurrentStackSize > 0)
            {
                if (SlotVisualsCreated)
                    GetSlotFromItemData(projectileItemData).InventoryItem.UpdateStackSizeVisuals();
                return;
            }
            
            // Else remove the item from this inventory
            RemoveItem(projectileItemData, true);
        }

        void CombineStacks(ItemData itemDataToTakeFrom, ItemData itemDataToCombineWith)
        {
            int roomInStack = itemDataToCombineWith.Item.MaxStackSize - itemDataToCombineWith.CurrentStackSize;

            // If there's more room in the stack than the new item's current stack size, add it all to the stack
            if (itemDataToTakeFrom.CurrentStackSize <= roomInStack)
            {
                itemDataToCombineWith.AdjustCurrentStackSize(itemDataToTakeFrom.CurrentStackSize);
                itemDataToTakeFrom.SetCurrentStackSize(0);
            }
            else // If the new item's stack size is greater than the amount of room in the other item's stack, add what we can
            {
                itemDataToCombineWith.AdjustCurrentStackSize(roomInStack);
                itemDataToTakeFrom.AdjustCurrentStackSize(-roomInStack);
            }
        }

        void RemoveFromOrigin(ItemData newItemData, Unit unitAdding)
        {
            // Clear out the parent slot the item was dragged from, if it exists
            if (InventoryUI.parentSlotDraggedFrom != null)
            {
                if (InventoryUI.parentSlotDraggedFrom.InventoryItem.MyInventory != null)
                {
                    if (InventoryUI.parentSlotDraggedFrom.InventoryItem.MyInventory.MyUnit != null)
                    {
                        // If the unitAdding is taking from a dead Unit's inventory
                        if (InventoryUI.parentSlotDraggedFrom.InventoryItem.MyInventory.MyUnit.HealthSystem.IsDead)
                        {
                            if (unitAdding != null)
                                unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null);
                        }
                        else if (InventoryUI.lastInventoryInteractedWith != this) // Otherwise, the Unit who has this item equipped can just remove it themselves
                            InventoryUI.parentSlotDraggedFrom.InventoryItem.MyInventory.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null);
                    }

                    InventoryUI.parentSlotDraggedFrom.InventoryItem.MyInventory.RemoveItem(newItemData, true);
                }
                else if (InventoryUI.parentSlotDraggedFrom.InventoryItem.MyUnitEquipment != null)
                {
                    // Queue an InventoryAction to account for unequipping the item
                    // If the unitAdding is taking from a dead Unit's equipment
                    if (InventoryUI.parentSlotDraggedFrom.InventoryItem.MyUnitEquipment.MyUnit.HealthSystem.IsDead)
                    {
                        if (unitAdding != null)
                            unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null, InventoryActionType.Unequip);
                    }
                    else // Otherwise, the Unit who has this item equipped can just remove it themselves
                        InventoryUI.parentSlotDraggedFrom.InventoryItem.MyUnitEquipment.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null, InventoryActionType.Unequip);

                    InventoryUI.parentSlotDraggedFrom.InventoryItem.MyUnitEquipment.RemoveEquipment(newItemData);
                }
            }
        }

        void RemoveFromOrigin(SlotCoordinate targetSlotCoordinate, ItemData newItemData, Unit unitAdding)
        {
            if (InventoryUI.isDraggingItem && targetSlotCoordinate.myInventory != InventoryUI.DraggedItem.MyInventory)
            {
                // Remove the item from its original character equipment
                if (InventoryUI.parentSlotDraggedFrom != null && InventoryUI.parentSlotDraggedFrom is EquipmentSlot)
                {
                    // Queue an InventoryAction to account for unequipping the item
                    // If the unitAdding is taking from a dead Unit's equipment
                    if (InventoryUI.parentSlotDraggedFrom.EquipmentSlot.UnitEquipment.MyUnit.HealthSystem.IsDead)
                    {
                        if (unitAdding != null)
                            unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null, InventoryActionType.Unequip);
                    }
                    else // Otherwise, the Unit who has this item equipped can just remove it themselves
                        InventoryUI.parentSlotDraggedFrom.EquipmentSlot.UnitEquipment.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null, InventoryActionType.Unequip);

                    InventoryUI.parentSlotDraggedFrom.EquipmentSlot.UnitEquipment.RemoveEquipment(InventoryUI.DraggedItem.ItemData);
                }
                // Remove the item from its original inventory
                else if (InventoryUI.DraggedItem.MyInventory != null)
                {
                    if (InventoryUI.DraggedItem.MyInventory.myUnit != null)
                    {
                        // If the unitAdding is taking from a dead Unit's inventory
                        if (InventoryUI.DraggedItem.MyInventory.MyUnit.HealthSystem.IsDead)
                        {
                            if (unitAdding != null)
                                unitAdding.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null);
                        }
                        else // Otherwise, the Unit who owns this item can just remove it themselves
                            InventoryUI.DraggedItem.MyInventory.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(newItemData, newItemData.CurrentStackSize, null);
                    }

                    InventoryUI.DraggedItem.MyInventory.RemoveItem(newItemData, true);
                    if (InventoryUI.DraggedItem.MyInventory is ContainerInventory)
                    {
                        // If we drag arrows out of a Loose Quiver
                        if (InventoryUI.DraggedItem.MyInventory.ContainerInventory.LooseItem != null && InventoryUI.DraggedItem.MyInventory.ContainerInventory.LooseItem is Interactable_LooseQuiverItem)
                            InventoryUI.DraggedItem.MyInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();
                        // If we drag arrows out of a Unit's equipped Quiver
                        else if (InventoryUI.DraggedItem.MyInventory.myUnit != null && InventoryUI.DraggedItem.MyInventory.ContainerInventory.containerInventoryManager == InventoryUI.DraggedItem.MyInventory.myUnit.QuiverInventoryManager && InventoryUI.DraggedItem.MyInventory.MyUnit.UnitEquipment.SlotVisualsCreated)
                            InventoryUI.DraggedItem.MyInventory.MyUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                    }
                }
            }
        }

        void TryTakeStuckProjectiles(ItemData newItemData)
        {
            if (newItemData.Item is Item_Shield == false || myUnit == null || myUnit.UnitEquipment == null || myUnit.UnitEquipment.ItemDataEquipped(newItemData) == false)
                return;

            // If we're unequipping a shield get any projectiles stuck in the shield and add them to our inventory or drop them
            HeldShield heldShield = null;
            if (myUnit.UnitMeshManager.leftHeldItem != null && myUnit.UnitMeshManager.leftHeldItem.ItemData == newItemData)
                heldShield = myUnit.UnitMeshManager.leftHeldItem as HeldShield;
            else if (myUnit.UnitMeshManager.rightHeldItem != null && myUnit.UnitMeshManager.rightHeldItem.ItemData == newItemData)
                heldShield = myUnit.UnitMeshManager.rightHeldItem as HeldShield;

            if (heldShield != null && heldShield.transform.childCount > 1)
            {
                for (int i = heldShield.transform.childCount - 1; i > 0; i--)
                {
                    if (heldShield.transform.GetChild(i).CompareTag("Loose Item") == false)
                        continue;

                    Interactable_LooseItem looseProjectile = heldShield.transform.GetChild(i).GetComponent<Interactable_LooseItem>();
                    if (myUnit.UnitInventoryManager.TryAddItemToInventories(looseProjectile.ItemData))
                        LooseItemPool.ReturnToPool(looseProjectile);
                    else
                    {
                        looseProjectile.transform.SetParent(LooseItemPool.Instance.LooseItemParent);
                        looseProjectile.MeshCollider.enabled = true;
                        looseProjectile.RigidBody.useGravity = true;
                        looseProjectile.RigidBody.isKinematic = false;
                        looseProjectile.JiggleItem();
                    }

                }
            }
        }

        public void RemoveItem(ItemData itemDataToRemove, bool clearSlotCoordinate = true)
        {
            if (itemDatas.Contains(itemDataToRemove) == false)
                return;

            InventorySlot inventorySlot = GetSlotFromItemData(itemDataToRemove);
            if (SlotVisualsCreated && inventorySlot != null)
                inventorySlot.ClearItem();
            else
                GetSlotCoordinateFromItemData(itemDataToRemove)?.ClearItem();

            if (clearSlotCoordinate)
                itemDataToRemove.SetInventorySlotCoordinate(null);

            itemDatas.Remove(itemDataToRemove);

            if (myUnit != null)
                myUnit.Stats.UpdateCarryWeight();

            if (this is ContainerInventory)
            {
                // If arrows are being removed from a Loose Quiver
                if (ContainerInventory.LooseItem != null && ContainerInventory.LooseItem is Interactable_LooseQuiverItem)
                    ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();
                // If arrows are being removed from an equipped Quiver
                else if (myUnit != null && ContainerInventory.containerInventoryManager == myUnit.QuiverInventoryManager && myUnit.UnitEquipment.SlotVisualsCreated)
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
            }
        }

        /// <summary>Setup the target slot's item data and sprites.</summary>
        protected void SetupNewItem(InventorySlot targetSlot, ItemData newItemData)
        {
            targetSlot.InventoryItem.SetItemData(newItemData);
            targetSlot.ShowSlotImage();
            targetSlot.SetupFullSlotSprites();
            targetSlot.InventoryItem.UpdateStackSizeVisuals();
        }

        protected SlotCoordinate GetNextAvailableSlotCoordinate(ItemData itemData)
        {
            int width = itemData.Item.Width;
            int height = itemData.Item.Height;

            // Loop through every slot coordinate and check if it will work as the parent slot for the new Item
            for (int i = 0; i < slotCoordinates.Count; i++)
            {
                if (slotCoordinates[i].isFull || slotCoordinates[i].parentSlotCoordinate.isFull)
                    continue;

                bool isAvailable = true;
                if (inventoryLayout.HasStandardSlotSize())
                {
                    // Check for empty slots within the item's dimensions
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            SlotCoordinate slotCoordinateToCheck = GetSlotCoordinate(slotCoordinates[i].coordinate.x - x, slotCoordinates[i].coordinate.y - y);
                            if (slotCoordinateToCheck == null || slotCoordinateToCheck.isFull || slotCoordinateToCheck.parentSlotCoordinate.isFull)
                            {
                                isAvailable = false;
                                break;
                            }
                        }

                        if (isAvailable == false)
                            break;
                    }
                }
                else if (ItemFitsInSingleSlot(itemData.Item) == false) // For non-standard slot sizes
                    isAvailable = false;

                if (isAvailable)
                {
                    // Debug.Log(slotCoordinates[i].name + " is available to place " + itemData.Item.name + " in " + name);
                    return slotCoordinates[i];
                }
            }
            return null;
        }

        public SlotCoordinate GetSlotCoordinateFromItemData(ItemData itemData)
        {
            for (int i = 0; i < slotCoordinates.Count; i++)
            {
                if (slotCoordinates[i].parentSlotCoordinate.itemData == itemData)
                    return slotCoordinates[i].parentSlotCoordinate;
            }
            return null;
        }

        public SlotCoordinate GetSlotCoordinate(int xCoord, int yCoord)
        {
            for (int i = 0; i < slotCoordinates.Count; i++)
            {
                if (slotCoordinates[i].coordinate.x == xCoord && slotCoordinates[i].coordinate.y == yCoord)
                    return slotCoordinates[i];
            }
            return null;
        }

        public InventorySlot GetSlotFromCoordinate(int xCoord, int yCoord)
        {
            if (xCoord <= 0 || yCoord <= 0)
                return null;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].slotCoordinate.coordinate.x == xCoord && slots[i].slotCoordinate.coordinate.y == yCoord)
                    return slots[i];
            }

            Debug.LogWarning("Invalid slot coordinate");
            return null;
        }

        public InventorySlot GetSlotFromCoordinate(SlotCoordinate slotCoordinate)
        {
            if (slotCoordinate == null)
                return null;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].slotCoordinate == slotCoordinate)
                    return slots[i];
            }

            Debug.LogWarning("Invalid slot coordinate");
            return null;
        }

        public bool ContainsItem(Item item)
        {
            for (int i = 0; i < itemDatas.Count; i++)
            {
                if (itemDatas[i].Item == item)
                    return true;
            }
            return false;
        }

        public bool ContainsItemData(ItemData itemData) => itemDatas.Contains(itemData);

        protected void CreateSlotCoordinates()
        {
            slotCoordinates.Clear();
            maxSlotsPerColumn = Mathf.CeilToInt((float)inventoryLayout.AmountOfSlots / inventoryLayout.MaxSlotsPerRow);

            int coordinateCount = 0;
            for (int y = 1; y < maxSlotsPerColumn + 1; y++)
            {
                for (int x = 1; x < inventoryLayout.MaxSlotsPerRow + 1; x++)
                {
                    if (coordinateCount == inventoryLayout.AmountOfSlots)
                        return;

                    slotCoordinates.Add(new SlotCoordinate(x, y, this));
                    coordinateCount++;
                }
            }
        }

        public void UpdateSlotCoordinates()
        {
            if (slotCoordinates.Count == inventoryLayout.AmountOfSlots) // Slot count didn't change, so no need to do anything
                return;

            CreateSlotCoordinates();

            if (slots.Count > 0 && SlotVisualsCreated == false)
                CreateSlotVisuals();
        }

        public void CreateSlotVisuals()
        {
            if (SlotVisualsCreated)
            {
                Debug.LogWarning($"Slot visuals for inventory, owned by {MyUnit.name}, has already been created...");
                return;
            }

            for (int i = 0; i < inventoryLayout.AmountOfSlots; i++)
            {
                InventorySlot newSlot = InventorySlotPool.Instance.GetSlotFromPool();
                newSlot.transform.SetParent(slotsParent);

                newSlot.SetMyInventory(this);
                newSlot.InventoryItem.SetMyInventory(this);

                newSlot.SetSlotCoordinate(GetSlotCoordinate((i % inventoryLayout.MaxSlotsPerRow) + 1, Mathf.FloorToInt((float)i / inventoryLayout.MaxSlotsPerRow) + 1));
                newSlot.name = $"Slot - {newSlot.slotCoordinate.name}";
                slots.Add(newSlot);

                if (InventoryLayout.HasStandardSlotSize() == false)
                    newSlot.InventoryItem.EnableIconImage(); // Shows the placeholder image

                newSlot.HideItemIcon();

                newSlot.gameObject.SetActive(true);
            }

            SlotVisualsCreated = true;
            SetupItems();
        }

        void SetSlotsList()
        {
            if (myUnit.IsPlayer)
            {
                slotsParent = InventoryUI.PlayerPocketsParent;
                slots = InventoryUI.playerPocketsSlots;
            }
            else
            {
                slotsParent = InventoryUI.NPCPocketsParent;
                slots = InventoryUI.npcPocketsSlots;
            }
        }

        public void RemoveSlots()
        {
            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    InventorySlotPool.Instance.ReturnToPool(slots[i]);
                }

                slots.Clear();
            }

            SlotVisualsCreated = false;
        }

        public void SetupItems()
        {
            for (int i = itemDatas.Count - 1; i >= 0; i--)
            {
                if (itemDatas[i].Item == null)
                {
                    itemDatas.Remove(itemDatas[i]);
                    continue;
                }

                itemDatas[i].RandomizeData();

                if (!TryAddItem(itemDatas[i], null, false))
                {
                    Debug.LogWarning($"{itemDatas[i].Item.name} can't fit in inventory...");
                    itemDatas.Remove(itemDatas[i]);
                }
            }
        }

        public InventorySlot GetSlotFromItemData(ItemData itemData)
        {
            if (!SlotVisualsCreated || slots == null)
                return null;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].GetItemData() == itemData)
                    return slots[i].ParentSlot() as InventorySlot;
            }
            return null;
        }

        public bool ItemTypeAllowed(ItemType itemType)
        {
            if (inventoryLayout.AllowedItemTypes.Length == 0)
                return true;

            for (int i = 0; i < inventoryLayout.AllowedItemTypes.Length; i++)
            {
                if (itemType == inventoryLayout.AllowedItemTypes[i])
                    return true;
            }
            return false;
        }

        ///<summary>Will return true if any Item Type from the array is allowed.</summary>
        public bool ItemTypeAllowed(ItemType[] itemTypes)
        {
            if (inventoryLayout.AllowedItemTypes.Length == 0)
                return true;

            for (int i = 0; i < inventoryLayout.AllowedItemTypes.Length; i++)
            {
                for (int j = 0; j < itemTypes.Length; j++)
                {
                    if (itemTypes[j] == inventoryLayout.AllowedItemTypes[i])
                        return true;
                }
            }
            return false;
        }

        ///<summary>Will return true if any Item Type from the array is contained in the inventories AllowedItemTypes array.</summary>
        public bool AllowedItemTypeContains(ItemType[] itemTypes)
        {
            for (int i = 0; i < inventoryLayout.AllowedItemTypes.Length; i++)
            {
                for (int j = 0; j < itemTypes.Length; j++)
                {
                    if (itemTypes[j] == inventoryLayout.AllowedItemTypes[i])
                        return true;
                }
            }
            return false;
        }

        public void OnCloseNPCInventory()
        {
            SlotVisualsCreated = false;

            // Clear out any slots already in the list, so we can start from scratch when we open another inventory
            RemoveSlots();
        }

        public bool ItemFitsInSingleSlot(Item item) => item.Width <= inventoryLayout.SlotWidth && item.Height <= inventoryLayout.SlotHeight;

        public ContainerInventory ContainerInventory => this as ContainerInventory;

        public List<ItemData> ItemDatas => itemDatas;

        public Unit MyUnit => myUnit;

        public void SetUnit(Unit newUnit) => myUnit = newUnit;

        public InventoryLayout InventoryLayout => inventoryLayout;

        public int MaxSlotsPerColumn => maxSlotsPerColumn;
    }
}
