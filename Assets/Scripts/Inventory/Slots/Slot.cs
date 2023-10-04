using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // public Slot parentSlot { get; protected set; }

    [Header("Components")]
    [SerializeField] protected InventoryItem inventoryItem;
    [SerializeField] protected Image image;

    [Header("Sprites")]
    [SerializeField] Sprite emptySlotSprite;
    [SerializeField] protected Sprite fullSlotSprite;
    
    public virtual void ShowSlotImage()
    {
        if (inventoryItem.itemData == null || inventoryItem.itemData.Item == null)
        {
            Debug.LogWarning("There is no item in this slot...");
            return;
        }

        if (inventoryItem.itemData.Item.InventorySprite(inventoryItem.itemData) == null)
        {
            Debug.LogError($"Sprite for {inventoryItem.itemData.Item.name} is not yet set in the item's ScriptableObject");
            return;
        }

        inventoryItem.SetupIconSprite(true);
    }

    public void EnableSlotImage()
    {
        image.enabled = true;
    }

    public void DisableSlotImage()
    {
        image.enabled = false;
    }

    public void HideItemIcon()
    {
        inventoryItem.DisableIconImage();
    }

    public void SetFullSlotSprite(Sprite sprite = null)
    {
        if (sprite == null)
            image.sprite = fullSlotSprite;
        else
            image.sprite = sprite;
    }

    protected void SetEmptySlotSprite() => image.sprite = emptySlotSprite;

    public InventoryItem InventoryItem => inventoryItem;

    public InventorySlot InventorySlot => this as InventorySlot;

    public EquipmentSlot EquipmentSlot => this as EquipmentSlot;

    public abstract ItemData GetItemData();

    public abstract void ClearSlotVisuals();

    public abstract void ClearItem();

    public abstract bool IsFull();

    public abstract Slot ParentSlot();

    public abstract void HighlightSlots();

    public abstract void RemoveSlotHighlights();

    public abstract void SetupEmptySlotSprites();

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
