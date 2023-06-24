using UnityEngine;

public class InventorySlot : Slot
{
    public Inventory myInventory { get; private set; }

    public Vector2 slotCoordinate { get; private set; }

    public void SetupAsParentSlot()
    {
        int width = inventoryItem.itemData.Item().width;
        int height = inventoryItem.itemData.Item().height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToSetup = myInventory.GetSlotFromCoordinate(new Vector2(slotCoordinate.x - x, slotCoordinate.y - y));
                slotToSetup.SetParentSlot(this);
                slotToSetup.SetFullSlotSprite();
            }
        }

        SetParentSlot(this);
        SetFullSlotSprite();
    }

    public void RemoveParentSlots()
    {
        int width = inventoryItem.itemData.Item().width;
        int height = inventoryItem.itemData.Item().height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                myInventory.GetSlotFromCoordinate(new Vector2(slotCoordinate.x - x, slotCoordinate.y - y)).SetParentSlot(null);
            }
        }

        SetParentSlot(null);
    }

    public override void SetupEmptySlotSprites()
    {
        int width = inventoryItem.itemData.Item().width;
        int height = inventoryItem.itemData.Item().height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToSetup = myInventory.GetSlotFromCoordinate(new Vector2(slotCoordinate.x - x, slotCoordinate.y - y));
                slotToSetup.SetEmptySlotSprite();
            }
        }
    }

    public override void ClearItem()
    {
        InventorySlot parentSlot = this.parentSlot as InventorySlot;

        // Hide the item's sprite
        parentSlot.HideSlotImage();

        // Setup the empty slot sprites
        parentSlot.SetupEmptySlotSprites();

        // Clear the stack size text
        parentSlot.inventoryItem.ClearStackSizeText();

        // Remove parent slot references
        parentSlot.RemoveParentSlots();

        // Clear out the slot's item data
        parentSlot.inventoryItem.SetItemData(null);
    }

    public override bool IsFull() => parentSlot != null && parentSlot.InventoryItem().itemData != null && parentSlot.InventoryItem().itemData.Item() != null;

    public override void HighlightSlots()
    {
        int width = InventoryUI.Instance.DraggedItem().itemData.Item().width;
        int height = InventoryUI.Instance.DraggedItem().itemData.Item().height;
        bool validSlot = !InventoryUI.Instance.DraggedItem_OverlappingMultipleItems();
        if (slotCoordinate.x - width < 0 || slotCoordinate.y - height < 0)
            validSlot = false;

        InventoryUI.Instance.SetValidDragPosition(validSlot);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToHighlight = myInventory.GetSlotFromCoordinate(new Vector2(slotCoordinate.x - x, slotCoordinate.y - y));
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
                InventorySlot slotToHighlight = myInventory.GetSlotFromCoordinate(new Vector2(slotCoordinate.x - x, slotCoordinate.y - y));
                if (slotToHighlight == null)
                    continue;

                if (slotToHighlight.parentSlot != null && slotToHighlight.parentSlot.InventoryItem().itemData != InventoryUI.Instance.DraggedItem().itemData && slotToHighlight.parentSlot.InventoryItem().itemData.Item() != null)
                    slotToHighlight.SetFullSlotSprite();

                slotToHighlight.image.color = Color.white;
            }
        }
    }

    public override ItemData GetItemData()
    {
        if (parentSlot == null)
            return inventoryItem.itemData;
        else
            return parentSlot.InventoryItem().itemData;
    }

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetParentSlot(InventorySlot parentSlot) => this.parentSlot = parentSlot;

    public void SetSlotCoordinate(Vector2 coord) => slotCoordinate = coord;
}
