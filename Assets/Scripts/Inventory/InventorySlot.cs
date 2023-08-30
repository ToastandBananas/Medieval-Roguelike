using UnityEngine;

public class InventorySlot : Slot
{
    public Inventory myInventory { get; private set; }

    public SlotCoordinate slotCoordinate { get; private set; }

    public void SetupFullSlotSprites()
    {
        int width = inventoryItem.itemData.Item().width;
        int height = inventoryItem.itemData.Item().height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToSetup = myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y);
                slotToSetup.SetFullSlotSprite();
            }
        }

        // slotCoordinate.SetParentSlotCoordinate(slotCoordinate);
        SetFullSlotSprite();
    }

    public override void SetupEmptySlotSprites()
    {
        int width = inventoryItem.itemData.Item().width;
        int height = inventoryItem.itemData.Item().height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y).SetEmptySlotSprite();
            }
        }
    }

    public override void ClearItem()
    {
        InventorySlot parentSlot = myInventory.GetSlotFromCoordinate(slotCoordinate.parentSlotCoordinate.coordinate.x, slotCoordinate.parentSlotCoordinate.coordinate.y);

        // Hide the item's sprite
        parentSlot.HideSlotImage();

        // Setup the empty slot sprites
        parentSlot.SetupEmptySlotSprites();

        // Clear the stack size text
        parentSlot.inventoryItem.ClearStackSizeText();

        // Remove parent slot references
        parentSlot.slotCoordinate.ClearItem();

        // Clear out the slot's item data
        parentSlot.inventoryItem.SetItemData(null);
    }

    public override bool IsFull() => slotCoordinate.parentSlotCoordinate != null && slotCoordinate.parentSlotCoordinate.itemData != null && slotCoordinate.parentSlotCoordinate.itemData.Item() != null;

    public override void HighlightSlots()
    {
        int width = InventoryUI.Instance.DraggedItem().itemData.Item().width;
        int height = InventoryUI.Instance.DraggedItem().itemData.Item().height;
        bool validSlot = !InventoryUI.Instance.DraggedItem_OverlappingMultipleItems();
        if (slotCoordinate.coordinate.x - width < 0 || slotCoordinate.coordinate.y - height < 0)
            validSlot = false;

        InventoryUI.Instance.SetValidDragPosition(validSlot);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToHighlight = myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y);
                if (slotToHighlight == null)
                    continue;

                slotToHighlight.SetEmptySlotSprite();

                if (validSlot)
                    slotToHighlight.image.color = Color.green;
                else
                    slotToHighlight.image.color = Color.red;
            }
        }
    }

    public override void RemoveSlotHighlights()
    {
        int width = InventoryUI.Instance.DraggedItem().itemData.Item().width;
        int height = InventoryUI.Instance.DraggedItem().itemData.Item().height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToHighlight = myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y);
                if (slotToHighlight == null)
                    continue;

                if (slotToHighlight.IsFull() && slotToHighlight.slotCoordinate.parentSlotCoordinate.itemData != InventoryUI.Instance.DraggedItem().itemData)
                    slotToHighlight.SetFullSlotSprite();

                slotToHighlight.image.color = Color.white;
            }
        }
    }

    public override ItemData GetItemData()
    {
        if (slotCoordinate.parentSlotCoordinate == null)
            return slotCoordinate.itemData;
        else
            return slotCoordinate.parentSlotCoordinate.itemData;
    }

    public override Slot GetParentSlot()
    {
        if (slotCoordinate == null || slotCoordinate.parentSlotCoordinate == null)
            return this;

        return myInventory.GetSlotFromCoordinate(slotCoordinate.parentSlotCoordinate);
    }

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetSlotCoordinate(SlotCoordinate coordinate) => slotCoordinate = coordinate;
}
