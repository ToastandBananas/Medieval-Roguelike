using UnityEngine;

public class InventorySlot : Slot
{
    public Inventory myInventory { get; private set; }

    public SlotCoordinate slotCoordinate { get; private set; }

    [SerializeField] Sprite centerFullSlotSprite;

    [Header("Edge Sprites")]
    [SerializeField] Sprite topFullSlotSprite;
    [SerializeField] Sprite bottomFullSlotSprite;
    [SerializeField] Sprite leftFullSlotSprite;
    [SerializeField] Sprite rightFullSlotSprite;

    [Header("Corner Sprites")]
    [SerializeField] Sprite topLeftFullSlotSprite;
    [SerializeField] Sprite topRightFullSlotSprite;
    [SerializeField] Sprite bottomLeftFullSlotSprite;
    [SerializeField] Sprite bottomRightFullSlotSprite; // Done

    [Header("Half Sprites")]
    [SerializeField] Sprite topHalfFullSlotSprite;
    [SerializeField] Sprite bottomHalfFullSlotSprite; // Done
    [SerializeField] Sprite leftHalfFullSlotSprite;
    [SerializeField] Sprite rightHalfFullSlotSprite; // Done
    [SerializeField] Sprite horizontalHalfFullSlotSprite;
    [SerializeField] Sprite verticalHalfFullSlotSprite;

    public void SetupFullSlotSprites()
    {
        int width = inventoryItem.itemData.Item.width;
        int height = inventoryItem.itemData.Item.height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToSetup = myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y);
                if (width == 1 && height == 1) // Single slot item
                    slotToSetup.SetFullSlotSprite(fullSlotSprite);
                else if (x == 0 & y == 0) // Bottom Right
                {
                    if (width == 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(bottomHalfFullSlotSprite);
                    else if (width > 1 && height == 1)
                        slotToSetup.SetFullSlotSprite(rightHalfFullSlotSprite);
                    else if (width > 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(bottomRightFullSlotSprite);
                }
                else if (width > 2 && height > 2 && x > 0 && x < width - 1 && y > 0 && y < height - 1) // Center
                {
                    slotToSetup.SetFullSlotSprite(centerFullSlotSprite);
                }
                else if (x == 0 && y == height - 1) // Top Right
                {
                    if (width == 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(topHalfFullSlotSprite);
                    else if (width > 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(topRightFullSlotSprite);
                }
                else if (x == width - 1 && y == 0) // Bottom Left
                {
                    if (width > 1 && height == 1)
                        slotToSetup.SetFullSlotSprite(leftHalfFullSlotSprite);
                    else if (width > 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(bottomLeftFullSlotSprite);
                }
                else if (x == width - 1 && y == height - 1) // Top Left
                {
                    if (width == 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(topHalfFullSlotSprite);
                    else if (width > 1 && height == 1)
                        slotToSetup.SetFullSlotSprite(leftHalfFullSlotSprite);
                    else if (width > 1 && height > 1)
                        slotToSetup.SetFullSlotSprite(topLeftFullSlotSprite);
                }
                else if (x == width - 1) // Left Edge
                {
                    if (width == 1 && height > 2 && y > 0 && y < height - 1)
                        slotToSetup.SetFullSlotSprite(verticalHalfFullSlotSprite);
                    else
                        slotToSetup.SetFullSlotSprite(leftFullSlotSprite);
                }
                else if (x == 0) // Right Edge
                {
                    if (width == 1 && height > 2 && y > 0 && y < height - 1)
                        slotToSetup.SetFullSlotSprite(verticalHalfFullSlotSprite);
                    else
                        slotToSetup.SetFullSlotSprite(rightFullSlotSprite);
                }
                else if (y == 0) // Bottom Edge
                {
                    if (height == 1 && width > 2 && x > 0 && x < width - 1)
                        slotToSetup.SetFullSlotSprite(horizontalHalfFullSlotSprite);
                    else
                        slotToSetup.SetFullSlotSprite(bottomFullSlotSprite);
                }
                else if (y == height - 1) // Top Edge
                {
                    if (height == 1 && width > 2 && x > 0 && x < width - 1)
                        slotToSetup.SetFullSlotSprite(horizontalHalfFullSlotSprite);
                    else
                        slotToSetup.SetFullSlotSprite(topFullSlotSprite);
                }
                else // Fallback
                    slotToSetup.SetFullSlotSprite(centerFullSlotSprite);
            }
        }
    }

    public override void SetupEmptySlotSprites()
    {
        if (inventoryItem.itemData == null || inventoryItem.itemData.Item == null)
            return;

        int width = inventoryItem.itemData.Item.width;
        int height = inventoryItem.itemData.Item.height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y).SetEmptySlotSprite();
            }
        }
    }

    public override void ClearSlotVisuals()
    {
        InventorySlot parentSlot = myInventory.GetSlotFromCoordinate(slotCoordinate.parentSlotCoordinate.coordinate.x, slotCoordinate.parentSlotCoordinate.coordinate.y);

        // Hide the item's sprite
        parentSlot.HideSlotImage();

        // Setup the empty slot sprites
        parentSlot.SetupEmptySlotSprites();

        // Clear the stack size text
        parentSlot.inventoryItem.ClearStackSizeText();

        // Clear out the slot's item data
        parentSlot.inventoryItem.SetItemData(null);
    }

    public override void ClearItem()
    {
        ClearSlotVisuals();

        // Clear the slot coordinates
        myInventory.GetSlotFromCoordinate(slotCoordinate.parentSlotCoordinate).slotCoordinate.ClearItem();
    }

    public override bool IsFull() => slotCoordinate.parentSlotCoordinate != null && slotCoordinate.parentSlotCoordinate.itemData != null && slotCoordinate.parentSlotCoordinate.itemData.Item != null;

    public override void HighlightSlots()
    {
        int width = InventoryUI.Instance.DraggedItem.itemData.Item.width;
        int height = InventoryUI.Instance.DraggedItem.itemData.Item.height;
        bool validSlot = !InventoryUI.Instance.OverlappingMultipleItems(slotCoordinate, InventoryUI.Instance.DraggedItem.itemData, out SlotCoordinate overlappedItemsParentSlotCoordinate, out int overlappedItemCount);
        if (slotCoordinate.coordinate.x - width < 0 || slotCoordinate.coordinate.y - height < 0)
            validSlot = false;

        if (InventoryUI.Instance.parentSlotDraggedFrom is ContainerEquipmentSlot)
        {
            ContainerEquipmentSlot containerEquipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as ContainerEquipmentSlot;
            if (containerEquipmentSlotDraggedFrom.containerInventoryManager.ContainsAnyItems())
                validSlot = false;
        }

        InventoryUI.Instance.SetValidDragPosition(validSlot);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToHighlight = myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y);
                if (slotToHighlight == null)
                    continue;

                if (validSlot)
                    slotToHighlight.image.color = Color.green;
                else
                    slotToHighlight.image.color = Color.red;
            }
        }
    }

    public override void RemoveSlotHighlights()
    {
        int width = InventoryUI.Instance.DraggedItem.itemData.Item.width;
        int height = InventoryUI.Instance.DraggedItem.itemData.Item.height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToHighlight = myInventory.GetSlotFromCoordinate(slotCoordinate.coordinate.x - x, slotCoordinate.coordinate.y - y);
                if (slotToHighlight == null)
                    continue;

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

    public override Slot ParentSlot()
    {
        if (slotCoordinate == null || slotCoordinate.parentSlotCoordinate == null)
            return this;

        return myInventory.GetSlotFromCoordinate(slotCoordinate.parentSlotCoordinate);
    }

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetSlotCoordinate(SlotCoordinate coordinate) => slotCoordinate = coordinate;
}
