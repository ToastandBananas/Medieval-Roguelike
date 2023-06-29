using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Slot parentSlot { get; protected set; }

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

        inventoryItem.SetupSprite(true);
    }

    public void HideSlotImage() => inventoryItem.DisableSprite();

    public void SetFullSlotSprite() => image.sprite = fullSlotSprite;

    protected void SetEmptySlotSprite() => image.sprite = emptySlotSprite;

    public InventoryItem InventoryItem() => inventoryItem;

    public abstract ItemData GetItemData();

    public abstract void ClearItem();

    public abstract bool IsFull();

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
