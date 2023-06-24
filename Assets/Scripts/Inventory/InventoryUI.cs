using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] InventoryItem draggedItem;

    [Header("Inventories")]
    [SerializeField] Inventory pocketsInventory;
    [SerializeField] Inventory backpackInventory;

    [Header("Character Equipment")]
    [SerializeField] CharacterEquipment playerEquipment;

    [Header("Prefab")]
    [SerializeField] InventorySlot inventorySlotPrefab;

    public Slot activeSlot { get; private set; }

    public bool isDraggingItem { get; private set; }
    public bool validDragPosition { get; private set; }
    public int draggedItemOverlapCount { get; private set; }
    public Slot parentSlotDraggedFrom { get; private set; }
    public Slot overlappedItemsParentSlot { get; private set; }

    RectTransform rectTransform;

    WaitForSeconds stopDraggingDelay = new WaitForSeconds(0.05f);

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
                if (activeSlot == null || activeSlot.IsFull() == false)
                    return;

                // "Pickup" the item by hiding the item's sprite and showing that same sprite on the draggedItem object
                if (activeSlot is InventorySlot)
                    SetupDraggedItem(activeSlot.parentSlot.InventoryItem().itemData, activeSlot.parentSlot, activeSlot.parentSlot.InventoryItem().myInventory);
                else
                    SetupDraggedItem(activeSlot.parentSlot.InventoryItem().itemData, activeSlot, activeSlot.InventoryItem().myCharacterEquipment);

                activeSlot.parentSlot.SetupEmptySlotSprites();
                activeSlot.parentSlot.InventoryItem().DisableSprite();
                activeSlot.parentSlot.InventoryItem().ClearStackSizeText();

                activeSlot.HighlightSlots();
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
                {
                    if (activeSlot is InventorySlot)
                    {
                        InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                        activeInventorySlot.myInventory.TryAddDraggedItemAt(activeInventorySlot, draggedItem.itemData);
                    }
                    else
                    {
                        EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                        activeEquipmentSlot.MyCharacterEquipment().TryAddDraggedItemAt(activeEquipmentSlot, draggedItem.itemData);
                    }
                }
                else if (EventSystem.current.IsPointerOverGameObject() == false)
                    draggedItem.DropItem();
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
                Slot slotToCheck;
                if (activeSlot is InventorySlot)
                {
                    InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                    slotToCheck = activeInventorySlot.myInventory.GetSlotFromCoordinate(new Vector2(activeInventorySlot.slotCoordinate.x - x, activeInventorySlot.slotCoordinate.y - y));
                }
                else
                    slotToCheck = activeSlot;

                if (slotToCheck == null)
                    continue;

                if (slotToCheck.IsFull())
                {
                    if (slotToCheck.GetItemData() == draggedItem.itemData)
                        continue;

                    if (overlappedItemData == null)
                    {
                        overlappedItemData = slotToCheck.GetItemData();
                        if (slotToCheck is InventorySlot)
                        {
                            InventorySlot inventorySlotToCheck = slotToCheck as InventorySlot;
                            overlappedItemsParentSlot = inventorySlotToCheck.parentSlot;
                        }
                        else
                            overlappedItemsParentSlot = slotToCheck;

                        draggedItemOverlapCount++;
                    }
                    else if (overlappedItemData != slotToCheck.GetItemData())
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
        if (parentSlotDraggedFrom is InventorySlot)
        {
            InventorySlot parentInventorySlotDraggedFrom = parentSlotDraggedFrom as InventorySlot;
            parentInventorySlotDraggedFrom.SetupAsParentSlot();
        }
        else
            parentSlotDraggedFrom.SetFullSlotSprite();

        parentSlotDraggedFrom.InventoryItem().UpdateStackSizeText();

        // Hide the dragged item
        DisableDraggedItem();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, Inventory inventoryDraggedFrom)
    {
        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;

        draggedItem.SetMyInventory(inventoryDraggedFrom);
        draggedItem.SetMyCharacterEquipment(null);
        draggedItem.SetItemData(newItemData);
        draggedItem.UpdateStackSizeText();
        draggedItem.SetupDraggedSprite();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, CharacterEquipment characterEquipmentDraggedFrom)
    {
        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;

        draggedItem.SetMyInventory(null);
        draggedItem.SetMyCharacterEquipment(characterEquipmentDraggedFrom);
        draggedItem.SetItemData(newItemData);
        draggedItem.UpdateStackSizeText();
        draggedItem.SetupDraggedSprite();
    }

    public void DisableDraggedItem()
    {
        if (activeSlot != null)
            activeSlot.RemoveSlotHighlights();

        Cursor.visible = true;
        isDraggingItem = false;
        parentSlotDraggedFrom = null;
        draggedItemOverlapCount = 0;

        StartCoroutine(DelayStopDraggingItem());
        draggedItem.DisableSprite();
        draggedItem.ClearStackSizeText();
    }

    IEnumerator DelayStopDraggingItem()
    {
        yield return stopDraggingDelay;
        draggedItem.SetItemData(null);
    }

    public void SetActiveSlot(Slot slot) => activeSlot = slot;

    public InventoryItem DraggedItem() => draggedItem;

    public InventorySlot InventorySlotPrefab() => inventorySlotPrefab;

    public void SetValidDragPosition(bool valid) => validDragPosition = valid;
}
