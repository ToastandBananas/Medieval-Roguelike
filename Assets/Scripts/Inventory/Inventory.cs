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

    public List<InventorySlot> slots { get; private set; }

    void Awake()
    {
        maxSlotsPerColumn = Mathf.CeilToInt(maxSlots / maxSlotsPerRow);

        slots = new List<InventorySlot>();

        for (int i = 0; i < amountOfSlots; i++)
        {
            InventorySlot newSlot = Instantiate(InventoryUI.Instance.InventorySlotPrefab(), slotsParent);
            newSlot.SetSlotCoordinate(new Vector2((i % maxSlotsPerRow) + 1, Mathf.FloorToInt(i / maxSlotsPerRow) + 1));
            newSlot.name = $"Slot - {newSlot.slotCoordinate}";
            newSlot.SetMyInventory(this);
            newSlot.InventoryItem().SetMyInventory(this);
            slots.Add(newSlot);

            // Debug.Log(newSlot.name + ": " + newSlot.slotCoordinate);

            if (i == maxSlots - 1)
                break;
        }

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
            InventorySlot slot = GetNextAvailableInventorySlot(newItemData);
            if (slot != null)
            {
                // Setup the slot's item data and sprites
                SetupNewItem(slot, newItemData);
                return true;
            }
        }
        return false;
    }

    public bool TryAddDraggedItemAt(Slot targetSlot, ItemData newItemData)
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

            // If the slots are in different inventories
            if (targetSlot.myInventory != InventoryUI.Instance.DraggedItem().myInventory)
            {
                // Create a new ItemData and assign it to the new inventory
                newDraggedItemData = new ItemData();
                newDraggedItemData.TransferData(newItemData);
                itemDatas.Add(newDraggedItemData);

                // Remove the item from its original inventory
                InventoryUI.Instance.DraggedItem().myInventory.itemDatas.Remove(newItemData);
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

                // Setup the dragged item's data and sprite and start dragging the new item
                InventoryUI.Instance.SetupDraggedItem(overlappedItemsData, null, this);
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

                // Remove the item from its original inventory
                InventoryUI.Instance.DraggedItem().myInventory.itemDatas.Remove(newItemData);
            }
            else // Else, just get a reference to the dragged item's data before we clear it out
                newDraggedItemData = newItemData;

            // Clear out the dragged item's original slot
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            // Setup the target slot's item data and sprites
            SetupNewItem(targetSlot, newDraggedItemData);

            // Hide the dragged item
            InventoryUI.Instance.DisableDraggedItem();
        }

        return true;
    }

    /// <summary>Setup the target slot's item data and sprites.</summary>
    void SetupNewItem(Slot targetSlot, ItemData newItemData)
    {
        targetSlot.InventoryItem().SetItemData(newItemData);
        targetSlot.ShowSlotImage();
        targetSlot.SetupAsParentSlot();
        targetSlot.InventoryItem().UpdateStackSizeText();
    }

    InventorySlot GetNextAvailableInventorySlot(ItemData itemData)
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsFull())
                continue;

            bool isAvailable = true;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    InventorySlot slotToCheck = GetSlotFromCoordinate(new Vector2(slots[i].slotCoordinate.x - x, slots[i].slotCoordinate.y - y));
                    if (slotToCheck == null || slotToCheck.IsFull())
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
                // Debug.Log(slots[i].name + " is available to place " + itemData.Item().name + " in " + name);
                return slots[i];
            }
        }
        return null;
    }

    public InventorySlot GetSlotFromCoordinate(Vector2 slotCoordinate)
    {
        if (slotCoordinate.x <= 0 || slotCoordinate.y <= 0)
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

    public List<ItemData> ItemDatas() => itemDatas;

    public Unit MyUnit() => myUnit;
}
