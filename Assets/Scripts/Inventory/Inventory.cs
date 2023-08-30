using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] Transform slotsParent;
    [SerializeField] Unit myUnit;

    [Header("Slot Counts")]
    [SerializeField] int amountOfSlots = 24;
    [SerializeField] int maxSlots = 24;
    [SerializeField] int maxSlotsPerRow = 12;
    int maxSlotsPerColumn;

    [Header("Items in Inventory")]
    [SerializeField] List<ItemData> itemDatas = new List<ItemData>();

    List<InventorySlot> slots = new List<InventorySlot>();
    List<SlotCoordinate> slotCoordinates = new List<SlotCoordinate>();

    bool slotVisualsCreated;

    void Awake()
    {
        CreateSlotCoordinates();

        if (myUnit.CompareTag("Player"))
            CreateSlotVisuals();
        else
            SetupItems();
    }

    void CreateSlotVisuals()
    {
        if (slotVisualsCreated)
        {
            Debug.LogWarning($"Slot visuals for {name}, owned by {myUnit.name}, has already been created...");
            return;
        }

        if (slots.Count > 0)
        {
            // Clear out any slots already in the list, so we can start from scratch
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].RemoveSlotHighlights();
                slots[i].ClearItem();
                slots[i].SetSlotCoordinate(null);
                slots[i].gameObject.SetActive(false);
            }

            slots.Clear();
        }

        for (int i = 0; i < amountOfSlots; i++)
        {
            InventorySlot newSlot = Instantiate(InventoryUI.Instance.InventorySlotPrefab(), slotsParent);

            newSlot.SetSlotCoordinate(GetSlotCoordinate((i % maxSlotsPerRow) + 1, Mathf.FloorToInt(i / maxSlotsPerRow) + 1));
            newSlot.name = $"Slot - {newSlot.slotCoordinate.name}";

            newSlot.SetMyInventory(this);
            newSlot.InventoryItem().SetMyInventory(this);
            slots.Add(newSlot);

            if (i == maxSlots - 1)
                break;
        }

        slotVisualsCreated = true;

        SetupItems();
    }

    public void SetupItems()
    {
        for (int i = 0; i < itemDatas.Count; i++)
        {
            if (itemDatas[i].Item() == null)
                continue;

            if (itemDatas[i].HasBeenInitialized() == false)
                itemDatas[i].RandomizeData();

            if (TryAddItem(itemDatas[i]) == false)
                Debug.LogError($"{itemDatas[i].Item().name} can't fit in {name} inventory...");
        }
    }

    public bool TryAddItem(ItemData newItemData)
    {
        if (newItemData.Item() != null)
        {
            SlotCoordinate targetSlotCoordinate;
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
                if (itemDatas.Contains(newItemData) == false)
                    itemDatas.Add(newItemData);

                if (slotVisualsCreated)
                    ShowItemInInventory(newItemData, targetSlotCoordinate);

                return true;
            }
        }
        return false;
    }

    void ShowItemInInventory(ItemData itemData, SlotCoordinate slotCoordinate)
    {
        InventorySlot slot = GetSlotFromCoordinate(slotCoordinate.coordinate.x, slotCoordinate.coordinate.y);
        if (slot != null)
        {
            // Setup the slot's item data and sprites
            SetupNewItem(slot, itemData);
        }
    }

    public bool TryAddDraggedItemAt(InventorySlot targetSlot, ItemData newItemData)
    {
        // Check if there's multiple items in the way or if the position is invalid
        if (InventoryUI.Instance.validDragPosition == false || InventoryUI.Instance.draggedItemOverlapCount == 2)
            return false;

        // If there's only one item in the way
        if (InventoryUI.Instance.draggedItemOverlapCount == 1)
        {
            // Get a reference to the overlapped item's data and parent slot before we clear it out
            Slot overlappedItemsParentSlot = InventoryUI.Instance.overlappedItemsParentSlot;
            ItemData overlappedItemsData = overlappedItemsParentSlot.InventoryItem().itemData;
            ItemData newDraggedItemData;

            // Remove the highlighting
            targetSlot.RemoveSlotHighlights();

            // If the slots are in different inventories
            if (targetSlot.myInventory != InventoryUI.Instance.DraggedItem().myInventory)
            {
                // Create a new ItemData and assign it to the new inventory
                newDraggedItemData = new ItemData();
                newDraggedItemData.TransferData(newItemData);
                itemDatas.Add(newDraggedItemData);

                // Remove the item from its original character equipment
                if (InventoryUI.Instance.parentSlotDraggedFrom is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem().myCharacterEquipment.EquippedItemDatas()[(int)equipmentSlotDraggedFrom.EquipSlot()] = newItemData;
                }
                // Remove the item from its original inventory
                else
                    InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);
            }
            else // Else, just get a reference to the dragged item's data before we clear it out
                newDraggedItemData = newItemData;

            // If we're placing an item directly on top of the same type of item that is stackable and has more room in its stack
            if (overlappedItemsParentSlot == targetSlot && newDraggedItemData.Item() == overlappedItemsData.Item() && newDraggedItemData.Item().maxStackSize > 1 && overlappedItemsData.CurrentStackSize() < overlappedItemsData.Item().maxStackSize)
            {
                int remainingStack = newDraggedItemData.CurrentStackSize();

                // If we can't fit the entire stack, add what we can to the overlapped item's stack size
                if (overlappedItemsData.CurrentStackSize() + remainingStack > overlappedItemsData.Item().maxStackSize)
                {
                    remainingStack -= overlappedItemsData.Item().maxStackSize - overlappedItemsData.CurrentStackSize();
                    overlappedItemsData.SetCurrentStackSize(overlappedItemsData.Item().maxStackSize);
                }
                else // If we can fit the entire stack, add it to the overlapped item's stack size
                {
                    overlappedItemsData.AdjustCurrentStackSize(remainingStack);
                    remainingStack = 0;
                }

                // Update the overlapped item's stack size text
                overlappedItemsParentSlot.InventoryItem().UpdateStackSizeText();

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
                    InventoryUI.Instance.DraggedItem().itemData.SetCurrentStackSize(remainingStack);
                    InventoryUI.Instance.DraggedItem().UpdateStackSizeText();

                    // Re-enable the highlighting
                    targetSlot.HighlightSlots();
                }
            }
            else
            {
                // Clear out the overlapped item
                overlappedItemsParentSlot.ClearItem();

                // Clear out the dragged item
                if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                    InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

                // Setup the target slot's item data and sprites
                SetupNewItem(targetSlot, newDraggedItemData);
                targetSlot.slotCoordinate.SetupNewItem(newDraggedItemData);

                // Setup the dragged item's data and sprite and start dragging the new item
                InventoryUI.Instance.SetupDraggedItem(overlappedItemsData, null, this);

                // Re-enable the highlighting
                targetSlot.HighlightSlots();
            }
        }
        // If trying to place the item back into the slot it came from
        else if (targetSlot == InventoryUI.Instance.parentSlotDraggedFrom)
        {
            // Place the dragged item back to where it came from
            InventoryUI.Instance.ReplaceDraggedItem();
        }
        else // If there's no items in the way
        {
            ItemData newDraggedItemData;

            // If the slots are in different inventories
            if (targetSlot.myInventory != InventoryUI.Instance.DraggedItem().myInventory)
            {
                // Create a new ItemData and assign it to the new inventory
                newDraggedItemData = new ItemData();
                newDraggedItemData.TransferData(newItemData);
                itemDatas.Add(newDraggedItemData);

                // Remove the item from its original character equipment
                if (InventoryUI.Instance.parentSlotDraggedFrom != null && InventoryUI.Instance.parentSlotDraggedFrom is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem().myCharacterEquipment.EquippedItemDatas()[(int)equipmentSlotDraggedFrom.EquipSlot()] = null;
                }
                // Remove the item from its original inventory
                else if (InventoryUI.Instance.DraggedItem().myInventory != null)
                    InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);
            }
            else // Else, just get a reference to the dragged item's data before we clear it out
                newDraggedItemData = newItemData;

            // Clear out the dragged item's original slot
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            // Setup the target slot's item data and sprites
            SetupNewItem(targetSlot, newDraggedItemData);
            targetSlot.slotCoordinate.SetupNewItem(newDraggedItemData);

            // Hide the dragged item
            InventoryUI.Instance.DisableDraggedItem();
        }

        return true;
    }

    /// <summary>Setup the target slot's item data and sprites.</summary>
    void SetupNewItem(InventorySlot targetSlot, ItemData newItemData)
    {
        targetSlot.InventoryItem().SetItemData(newItemData);
        targetSlot.ShowSlotImage();
        targetSlot.SetupFullSlotSprites();
        targetSlot.InventoryItem().UpdateStackSizeText();
    }

    SlotCoordinate GetNextAvailableSlotCoordinate(ItemData itemData)
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;

        for (int i = 0; i < slotCoordinates.Count; i++)
        {
            if (slotCoordinates[i].isFull)
                continue;

            bool isAvailable = true;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SlotCoordinate slotCoordinateToCheck = GetSlotCoordinate(slotCoordinates[i].coordinate.x - x, slotCoordinates[i].coordinate.y - y);
                    if (slotCoordinateToCheck == null || slotCoordinateToCheck.isFull)
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
                Debug.Log(slotCoordinates[i].name + " is available to place " + itemData.Item().name + " in " + name);
                return slotCoordinates[i];
            }
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
            if (itemDatas[i].Item() == item)
                return true;
        }
        return false;
    }

    void CreateSlotCoordinates()
    {
        maxSlotsPerColumn = Mathf.CeilToInt(maxSlots / maxSlotsPerRow);

        int coordinateCount = 0;
        for (int y = 1; y < maxSlotsPerColumn + 1; y++)
        {
            for (int x = 1; x < maxSlotsPerRow + 1; x++)
            {
                if (coordinateCount == amountOfSlots)
                    return;

                slotCoordinates.Add(new SlotCoordinate(x, y, this));
                coordinateCount++;
            }
        }
    }

    public void UpdateSlotCoordinates()
    {
        if (slotCoordinates.Count == amountOfSlots) // Slot count didn't change, so no need to do anything
            return;

        slotCoordinates.Clear();
        CreateSlotCoordinates();

        if (slots.Count > 0 && slotVisualsCreated == false)
            CreateSlotVisuals();
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

    public List<ItemData> ItemDatas() => itemDatas;

    public Unit MyUnit() => myUnit;
}
