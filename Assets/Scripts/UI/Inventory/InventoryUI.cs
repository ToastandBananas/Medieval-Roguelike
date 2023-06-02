using UnityEngine;
using static Pathfinding.RVO.SimulatorBurst;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] InventoryItem draggedItem;

    [Header("Inventories")]
    [SerializeField] Inventory pocketsInventory;
    [SerializeField] Inventory backpackInventory;

    [Header("Prefab")]
    [SerializeField] InventorySlot inventorySlotPrefab;

    public Slot activeSlot { get; private set; }

    public bool isDraggingItem { get; private set; }
    public bool validDragPosition { get; private set; }
    public int draggedItemOverlapCount { get; private set; }
    public Slot parentSlotDraggedFrom { get; private set; }
    public Slot overlappedItemsParentSlot { get; private set; }

    RectTransform rectTransform;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one InventoryUI! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rectTransform = GetComponent<RectTransform>();

        draggedItem.DisableSprite();
    }

    void Update()
    {
        // If we're not already dragging an item
        if (isDraggingItem == false)
        {
            // If we select an item
            if (GameControls.gamePlayActions.menuSelect.WasPressed)
            {
                if (activeSlot == null)
                    return;

                if (activeSlot.parentSlot == null || activeSlot.parentSlot.InventoryItem().itemData.Item() == null)
                    return;

                // "Pickup" the item by hiding the item's sprite and showing that same sprite on the draggedItem object
                SetupDraggedItem(activeSlot.parentSlot.InventoryItem().itemData, activeSlot.parentSlot, activeSlot.parentSlot.myInventory);

                activeSlot.parentSlot.InventoryItem().DisableSprite();
                activeSlot.parentSlot.SetupEmptySlotSprites();
            }
        }
        else // If we are dragging an item
        {
            // The dragged item should follow the mouse position
            Vector2 offset = draggedItem.GetDraggedItemOffset();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 localMousePosition);
            draggedItem.RectTransform().localPosition = localMousePosition + offset;

            // If we try to place an item
            if (GameControls.gamePlayActions.menuSelect.WasPressed)
            {
                // Try placing the item
                if (activeSlot != null)
                    activeSlot.myInventory.TryAddDraggedItemAt(activeSlot, draggedItem.itemData);
            }
        }
    }

    public bool DraggedItem_OverlappingMultipleItems()
    {
        int width = draggedItem.itemData.Item().width;
        int height = draggedItem.itemData.Item().height;
        ItemData overlappedItemData = null;
        draggedItemOverlapCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventorySlot slotToCheck = activeSlot.myInventory.GetSlotFromCoordinate(new Vector2(activeSlot.slotCoordinate.x - x, activeSlot.slotCoordinate.y - y));
                if (slotToCheck == null)
                    continue;

                if (slotToCheck.IsFull())
                {
                    if (slotToCheck.parentSlot.InventoryItem().itemData == draggedItem.itemData)
                        continue;

                    if (overlappedItemData == null)
                    {
                        overlappedItemData = slotToCheck.parentSlot.InventoryItem().itemData;
                        overlappedItemsParentSlot = slotToCheck.parentSlot;
                        draggedItemOverlapCount++;
                    }
                    else if (overlappedItemData != slotToCheck.parentSlot.InventoryItem().itemData)
                    {
                        draggedItemOverlapCount++;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void ReplaceDraggedItem()
    {
        // No need to setup the ItemData since it hasn't changed, so just show the item's sprite and change the slot's color/remove highlighting
        activeSlot.RemoveSlotHighlights();
        parentSlotDraggedFrom.ShowSlotImage();
        parentSlotDraggedFrom.SetupAsParentSlot();

        // Hide the dragged item
        DisableDraggedItem();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, Inventory inventoryDraggedFrom)
    {
        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;

        draggedItem.SetMyInventory(inventoryDraggedFrom);
        draggedItem.SetItemData(newItemData);
        draggedItem.SetupDraggedSprite();
    }

    public void DisableDraggedItem()
    {
        activeSlot.RemoveSlotHighlights();

        Cursor.visible = true;
        isDraggingItem = false;
        parentSlotDraggedFrom = null;
        draggedItemOverlapCount = 0;

        draggedItem.SetItemData(null);
        draggedItem.DisableSprite();
    }

    public void SetActiveSlot(Slot slot) => activeSlot = slot;

    public InventoryItem DraggedItem() => draggedItem;

    public InventorySlot InventorySlotPrefab() => inventorySlotPrefab;

    public void SetValidDragPosition(bool valid) => validDragPosition = valid;
}
