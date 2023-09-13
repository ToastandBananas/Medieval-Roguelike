using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // public Slot parentSlot { get; protected set; }

    [Header("Components")]
    [SerializeField] protected InventoryItem inventoryItem;
    [SerializeField] protected Image image;

    [Header("Sprites")]
    [SerializeField] Sprite emptySlotSprite;
    [SerializeField] Sprite fullSlotSprite;
    
    public virtual void ShowSlotImage()
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

    public void HideSlotImage() => inventoryItem.DisableIconImage();

    public void SetFullSlotSprite() => image.sprite = fullSlotSprite;

    protected void SetEmptySlotSprite() => image.sprite = emptySlotSprite;

    public InventoryItem InventoryItem() => inventoryItem;

    public abstract ItemData GetItemData();

    public abstract void ClearItem();

    public abstract bool IsFull();

    public abstract Slot GetParentSlot();

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ContextMenu.Instance.IsActive)
                ContextMenu.Instance.DisableContextMenu();
            else
                ContextMenu.Instance.BuildContextMenu();
        }
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (ContextMenu.Instance.IsActive)
                ContextMenu.Instance.DisableContextMenu();
        }

        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            if (ContextMenu.Instance.IsActive)
                ContextMenu.Instance.DisableContextMenu();
            /*else if (this is InventorySlot)
                UseItem();
            else if (this is EquipmentSlot)
                UnequipItem();*/
        }
    }
}
