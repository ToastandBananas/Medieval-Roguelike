using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    [SerializeField] protected Unit myUnit;
    protected Transform slotsParent;

    [Header("Slot Counts")]
    [SerializeField] protected InventoryLayout inventoryLayout;
    int maxSlotsPerColumn;

    [Header("Items in Inventory")]
    [SerializeField] protected List<ItemData> itemDatas = new List<ItemData>();

    protected List<InventorySlot> slots;
    protected List<SlotCoordinate> slotCoordinates;

    protected bool slotVisualsCreated;
    protected bool hasBeenInitialized;

    public virtual void Initialize()
    {
        if (slotCoordinates == null)
            slotCoordinates = new List<SlotCoordinate>();

        CreateSlotCoordinates();
        SetSlotsList();
        
        if (myUnit.IsPlayer())
            CreateSlotVisuals();
        else
            SetupItems();

        hasBeenInitialized = true;
    }

    public virtual bool TryAddItem(ItemData newItemData)
    {
        if (newItemData == null || newItemData.Item == null)
            return false;

        if (newItemData.ShouldRandomize)
            newItemData.RandomizeData();

        return AddItem(newItemData);
    }

    protected bool AddItem(ItemData newItemData)
    {
        SlotCoordinate targetSlotCoordinate;
        Inventory originalInventory = newItemData.MyInventory();

        // If the item data hasn't been assigned a slot coordinate, do so now
        if (newItemData.InventorySlotCoordinate() == null || newItemData.InventorySlotCoordinate().myInventory != this)
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
            if (originalInventory != null && originalInventory != this)
                originalInventory.RemoveItem(newItemData);

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

    public bool TryAddItemAt(SlotCoordinate targetSlotCoordinate, ItemData newItemData)
    {
        InventoryUI.Instance.OverlappingMultipleItems(targetSlotCoordinate, newItemData, out SlotCoordinate overlappedItemsParentSlotCoordinate, out int overlappedItemCount);

        // Check if there's multiple items in the way or if the position is invalid
        if ((InventoryUI.Instance.isDraggingItem && InventoryUI.Instance.validDragPosition == false) || overlappedItemCount >= 2)
            return false;

        // If there's only one item in the way
        if (overlappedItemCount == 1)
        {
            Debug.Log("1 in the way");
            // Get a reference to the overlapped item's data and parent slot before we clear it out
            ItemData overlappedItemsData = overlappedItemsParentSlotCoordinate.itemData;

            // Remove the highlighting
            if (slotVisualsCreated)
                GetSlotFromCoordinate(targetSlotCoordinate).RemoveSlotHighlights();

            // If the slots are in different inventories
            if (targetSlotCoordinate.myInventory != newItemData.MyInventory())
            {
                itemDatas.Add(newItemData);

                // Remove the item from its original character equipment
                if (InventoryUI.Instance.isDraggingItem)
                {
                    if (InventoryUI.Instance.parentSlotDraggedFrom is EquipmentSlot)
                    {
                        EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                        InventoryUI.Instance.DraggedItem.myCharacterEquipment.RemoveEquipmentMesh(equipmentSlotDraggedFrom.EquipSlot);
                        InventoryUI.Instance.DraggedItem.myCharacterEquipment.EquippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot] = null;
                    }
                    else // Remove the item from its original inventory
                        InventoryUI.Instance.DraggedItem.myInventory.ItemDatas.Remove(newItemData);
                }
            }

            // If we're placing an item directly on top of the same type of item that is stackable and has more room in its stack
            if (overlappedItemsParentSlotCoordinate == targetSlotCoordinate && newItemData.Item == overlappedItemsData.Item && newItemData.Item.maxStackSize > 1 && overlappedItemsData.CurrentStackSize < overlappedItemsData.Item.maxStackSize)
            {
                int remainingStack = newItemData.CurrentStackSize;

                // If we can't fit the entire stack, add what we can to the overlapped item's stack size
                if (overlappedItemsData.CurrentStackSize + remainingStack > overlappedItemsData.Item.maxStackSize)
                {
                    remainingStack -= overlappedItemsData.Item.maxStackSize - overlappedItemsData.CurrentStackSize;
                    overlappedItemsData.SetCurrentStackSize(overlappedItemsData.Item.maxStackSize);
                }
                else // If we can fit the entire stack, add it to the overlapped item's stack size
                {
                    overlappedItemsData.AdjustCurrentStackSize(remainingStack);
                    remainingStack = 0;
                }

                // Update the overlapped item's stack size text
                if (slotVisualsCreated)
                    GetSlotFromCoordinate(overlappedItemsParentSlotCoordinate).InventoryItem.UpdateStackSizeText();

                if (InventoryUI.Instance.isDraggingItem)
                {
                    // If the dragged item has been depleted
                    if (remainingStack == 0)
                    {
                        // Clear out the parent slot the item was dragged from, if it exists
                        if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                            InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

                        // Hide the dragged item
                        InventoryUI.Instance.DisableDraggedItem();
                    }
                    else // If there's still some left in the dragged item's stack
                    {
                        // Update the dragged item's stack size and text
                        InventoryUI.Instance.DraggedItem.itemData.SetCurrentStackSize(remainingStack);
                        InventoryUI.Instance.DraggedItem.UpdateStackSizeText();

                        // Re-enable the highlighting
                        GetSlotFromCoordinate(targetSlotCoordinate).HighlightSlots();
                    }
                }
            }
            else // If we're placing the item partially on top of another item
            {
                // Clear out the overlapped item
                GetSlotFromCoordinate(overlappedItemsParentSlotCoordinate).ClearItem();

                // Clear out the dragged item
                if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                    InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

                // Setup the target slot's item data and sprites
                SetupNewItem(GetSlotFromCoordinate(targetSlotCoordinate), newItemData);
                targetSlotCoordinate.SetupNewItem(newItemData);

                // Setup the dragged item's data and sprite and start dragging the new item
                InventoryUI.Instance.SetupDraggedItem(overlappedItemsData, null, this);

                // Re-enable the highlighting
                GetSlotFromCoordinate(targetSlotCoordinate).HighlightSlots();
            }
        }
        // If trying to place the item back into the slot it came from
        else if (InventoryUI.Instance.isDraggingItem && GetSlotFromCoordinate(targetSlotCoordinate) == InventoryUI.Instance.parentSlotDraggedFrom)
        {
            // Place the dragged item back to where it came from
            InventoryUI.Instance.ReplaceDraggedItem();
        }
        else // If there's no items in the way
        {
            // If the slots are in different inventories
            if (targetSlotCoordinate.myInventory != InventoryUI.Instance.DraggedItem.myInventory)
            {
                itemDatas.Add(newItemData);

                if (InventoryUI.Instance.isDraggingItem)
                {
                    // Remove the item from its original character equipment
                    if (InventoryUI.Instance.parentSlotDraggedFrom != null && InventoryUI.Instance.parentSlotDraggedFrom is EquipmentSlot)
                    {
                        EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                        InventoryUI.Instance.DraggedItem.myCharacterEquipment.RemoveEquipmentMesh(equipmentSlotDraggedFrom.EquipSlot);
                        InventoryUI.Instance.DraggedItem.myCharacterEquipment.EquippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot] = null;
                    }
                    // Remove the item from its original inventory
                    else if (InventoryUI.Instance.DraggedItem.myInventory != null)
                        InventoryUI.Instance.DraggedItem.myInventory.ItemDatas.Remove(newItemData);
                }
            }

            // Clear out the dragged item's original slot
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            // Setup the target slot's item data and sprites
            if (slotVisualsCreated)
                SetupNewItem(GetSlotFromCoordinate(targetSlotCoordinate), newItemData);

            targetSlotCoordinate.SetupNewItem(newItemData);

            // Hide the dragged item
            if (InventoryUI.Instance.isDraggingItem)
                InventoryUI.Instance.DisableDraggedItem();
        }

        InventoryUI.Instance.ClearParentSlotDraggedFrom();
        return true;
    }

    public void RemoveItem(ItemData itemDataToRemove)
    {
        if (itemDatas.Contains(itemDataToRemove) == false)
            return;

        if (slotVisualsCreated)
            GetSlotFromItemData(itemDataToRemove).ClearItem();
        else
            GetSlotCoordinateFromItemData(itemDataToRemove).ClearItem();

        itemDatas.Remove(itemDataToRemove);
    }

    /// <summary>Setup the target slot's item data and sprites.</summary>
    protected void SetupNewItem(InventorySlot targetSlot, ItemData newItemData)
    {
        targetSlot.InventoryItem.SetItemData(newItemData);
        targetSlot.ShowSlotImage();
        targetSlot.SetupFullSlotSprites();
        targetSlot.InventoryItem.UpdateStackSizeText();
    }

    protected SlotCoordinate GetNextAvailableSlotCoordinate(ItemData itemData)
    {
        int width = itemData.Item.width;
        int height = itemData.Item.height;

        for (int i = 0; i < slotCoordinates.Count; i++)
        {
            if (slotCoordinates[i].isFull || slotCoordinates[i].parentSlotCoordinate.isFull)
                continue;

            bool isAvailable = true;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SlotCoordinate slotCoordinateToCheck = GetSlotCoordinate(slotCoordinates[i].coordinate.x - x, slotCoordinates[i].coordinate.y - y);
                    if (slotCoordinateToCheck == null || slotCoordinateToCheck.isFull || slotCoordinateToCheck.parentSlotCoordinate.isFull)
                    {
                        isAvailable = false;
                        break;
                    }

                    if (isAvailable == false)
                        break;
                }
            }

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
        maxSlotsPerColumn = Mathf.CeilToInt((float)inventoryLayout.MaxSlots / inventoryLayout.MaxSlotsPerRow);

        int coordinateCount = 0;
        for (int y = 1; y < maxSlotsPerColumn + 1; y++)
        {
            for (int x = 1; x < inventoryLayout.MaxSlotsPerRow + 1; x++)
            {
                if (coordinateCount == inventoryLayout.AmountOfSlots || coordinateCount == inventoryLayout.MaxSlots)
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

        slotCoordinates.Clear();
        CreateSlotCoordinates();

        if (slots.Count > 0 && slotVisualsCreated == false)
            CreateSlotVisuals();
    }

    protected void CreateSlotVisuals()
    {
        if (slotVisualsCreated)
        {
            Debug.LogWarning($"Slot visuals for inventory, owned by {MyUnit.name}, has already been created...");
            return;
        }

        // Clear out any slots already in the list, so we can start from scratch
        RemoveSlots();

        for (int i = 0; i < inventoryLayout.AmountOfSlots; i++)
        {
            InventorySlot newSlot = InventorySlotPool.Instance.GetSlotFromPool();
            newSlot.transform.SetParent(slotsParent);
            newSlot.SetSlotCoordinate(GetSlotCoordinate((i % inventoryLayout.MaxSlotsPerRow) + 1, Mathf.FloorToInt((float)i / inventoryLayout.MaxSlotsPerRow) + 1));
            newSlot.name = $"Slot - {newSlot.slotCoordinate.name}";

            newSlot.SetMyInventory(this);
            newSlot.InventoryItem.SetMyInventory(this);
            slots.Add(newSlot);

            newSlot.gameObject.SetActive(true);

            if (i == inventoryLayout.MaxSlots - 1)
                break;
        }

        slotVisualsCreated = true;
        SetupItems();
    }

    void SetSlotsList()
    {
        if (myUnit.IsPlayer())
        {
            slotsParent = InventoryUI.Instance.PlayerPocketsParent;
            slots = InventoryUI.Instance.playerPocketsSlots;
        }
        else
        {
            slotsParent = InventoryUI.Instance.NPCPocketsParent;
            slots = InventoryUI.Instance.npcPocketsSlots;
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

        slotVisualsCreated = false;
    }

    public void SetupItems()
    {
        for (int i = 0; i < itemDatas.Count; i++)
        {
            if (itemDatas[i].Item == null)
                continue;

            itemDatas[i].RandomizeData();

            if (TryAddItem(itemDatas[i]) == false)
                Debug.LogError($"{itemDatas[i].Item.name} can't fit in inventory...");
        }
    }

    public InventorySlot GetSlotFromItemData(ItemData itemDataToDrop)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].GetItemData() == itemDataToDrop)
                return slots[i];
        }
        return null;
    }

    public List<ItemData> ItemDatas => itemDatas;

    public Unit MyUnit => myUnit;

    public void SetUnit(Unit newUnit) => myUnit = newUnit;

    public InventoryLayout InventoryLayout => inventoryLayout;

    public int MaxSlotsPerColumn => maxSlotsPerColumn;

    public bool SlotVisualsCreated => slotVisualsCreated;

    public bool HasBeenInitialized => hasBeenInitialized;
}
