using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory myInventory { get; private set; }
    public Slot parentSlot { get; private set; }

    public Vector2 slotCoordinate { get; private set; }

    [Header("Components")]
    [SerializeField] InventoryItem inventoryItem;
    [SerializeField] Image image;

    [Header("Sprites")]
    [SerializeField] Sprite emptySlotSprite;
    [SerializeField] Sprite fullSlotSprite;
    
    public void ShowSlotImage()
    {
        if (inventoryItem.itemData == null || inventoryItem.itemData.Item() == null)
        {
            Debug.LogWarning("There is no item in this slot...");
            return;
        }

        if (inventoryItem.itemData.Item().inventorySprite == null)
        {
            Debug.LogError($"Sprite for {inventoryItem.itemData.Item().name} is not yet set in the item's ScriptableObject");
            return;
        }

        inventoryItem.SetupSprite();
    }

    public void HideSlotImage() => inventoryItem.DisableSprite();

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

    public void SetupEmptySlotSprites()
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

    public void ClearItem()
    {
        Slot parentSlot = this.parentSlot;

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

    void SetFullSlotSprite() => image.sprite = fullSlotSprite;

    void SetEmptySlotSprite() => image.sprite = emptySlotSprite;

    public bool IsFull() => parentSlot != null && parentSlot.inventoryItem.itemData != null && parentSlot.inventoryItem.itemData.Item() != null;

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetParentSlot(Slot parentSlot) => this.parentSlot = parentSlot;

    public void SetSlotCoordinate(Vector2 coord) => slotCoordinate = coord;

    public InventoryItem InventoryItem() => inventoryItem;

    void HighlightSlots()
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

    public void RemoveSlotHighlights()
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

                if (slotToHighlight.parentSlot != null && slotToHighlight.parentSlot.inventoryItem.itemData != InventoryUI.Instance.DraggedItem().itemData && slotToHighlight.parentSlot.inventoryItem.itemData.Item() != null)
                    slotToHighlight.SetFullSlotSprite();

                slotToHighlight.image.color = Color.white;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        InventoryUI.Instance.SetActiveSlot(this);

        if (InventoryUI.Instance.isDraggingItem)
            HighlightSlots();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryUI.Instance.activeSlot == this)
            InventoryUI.Instance.SetActiveSlot(null);

        if (InventoryUI.Instance.isDraggingItem)
            RemoveSlotHighlights();
    }
}
