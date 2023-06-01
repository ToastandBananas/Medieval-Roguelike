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
    Slot slotDraggedFrom;

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
        if (GameControls.gamePlayActions.menuSelect.WasPressed)
        {
            // If we're clicking on a slot that has an item in it and we're not already dragging one
            if (isDraggingItem)
                return;

            if (activeSlot == null)
                return;

            if (activeSlot.parentSlot == null || activeSlot.parentSlot.InventoryItem().ItemData().Item() == null)
                return;

            isDraggingItem = true;
            slotDraggedFrom = activeSlot;
            Cursor.visible = false;

            // Pickup the item
            SetupDraggedItem(activeSlot.parentSlot.InventoryItem().ItemData());
        }
        else if (GameControls.gamePlayActions.menuSelect.IsPressed)
        {
            if (isDraggingItem == false)
                return;

            Vector2 offset = draggedItem.GetDraggedItemOffset();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 localPosition);
            draggedItem.RectTransform().localPosition = localPosition + offset;
        }
        else if (GameControls.gamePlayActions.menuSelect.WasReleased)
        {
            if (isDraggingItem == false)
                return;

            // Place the item

            isDraggingItem = false;
            slotDraggedFrom = null;
            Cursor.visible = true;
        }
    }

    public bool DraggedItem_OverlappingMultipleItems()
    {
        int width = draggedItem.itemData.Item().width;
        int height = draggedItem.itemData.Item().height;
        ItemData overlappedItemData = null;
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
                        overlappedItemData = slotToCheck.parentSlot.InventoryItem().itemData;
                    else if (overlappedItemData != slotToCheck.parentSlot.InventoryItem().itemData)
                        return true;
                }
            }
        }
        return false;
    }

    public void SetActiveSlot(Slot slot) => activeSlot = slot;

    public InventoryItem DraggedItem() => draggedItem;

    public void SetupDraggedItem(ItemData newItemData)
    {
        slotDraggedFrom = activeSlot.parentSlot;

        draggedItem.SetMyInventory(activeSlot.myInventory);
        draggedItem.SetItemData(newItemData);

        draggedItem.SetupDraggedSprite();
        activeSlot.parentSlot.InventoryItem().DisableSprite();
        activeSlot.parentSlot.RemoveFilledSlotSprites();
    }

    public void DisableDraggedItem()
    {
        slotDraggedFrom = null;
        draggedItem.ItemData().ClearItemData();
        draggedItem.DisableSprite();
    }

    public InventorySlot InventorySlotPrefab() => inventorySlotPrefab;

    public void SetValidDragPosition(bool valid) => validDragPosition = valid;
}
