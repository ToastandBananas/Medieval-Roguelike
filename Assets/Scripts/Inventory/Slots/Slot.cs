using GeneralUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem
{
    public abstract class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Components")]
        [SerializeField] protected InventoryItem inventoryItem;
        [SerializeField] protected Image image;

        [Header("Sprites")]
        [SerializeField] Sprite emptySlotSprite;
        [SerializeField] protected Sprite fullSlotSprite;

        public virtual void ShowSlotImage()
        {
            if (inventoryItem.ItemData == null || inventoryItem.ItemData.Item == null)
            {
                Debug.LogWarning("There is no item in this slot...");
                return;
            }

            if (inventoryItem.ItemData.Item.InventorySprite(inventoryItem.ItemData) == null)
            {
                Debug.LogError($"Sprite for {inventoryItem.ItemData.Item.name} is not yet set in the item's ScriptableObject");
                return;
            }

            inventoryItem.SetupIconSprite(true);
        }

        public virtual void EnableSlotImage()
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

        protected void SetEmptySlotSprite()
        {
            image.sprite = emptySlotSprite;
        }

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
            InventoryUI.SetActiveSlot(this);

            if (TooltipManager.CurrentSlot == null || TooltipManager.CurrentSlot.ParentSlot() != ParentSlot())
            {
                TooltipManager.SetCurrentSlot(ParentSlot());

                if (InventoryUI.IsDraggingItem == false && GetItemData() != null)
                    TooltipManager.ShowInventoryTooltips(this);

                TooltipManager.ClearUnitTooltips();
            }

            if (InventoryUI.IsDraggingItem)
                HighlightSlots();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (InventoryUI.activeSlot == this)
                InventoryUI.SetActiveSlot(null);

            if (InventoryUI.IsDraggingItem)
                RemoveSlotHighlights();
            else
                TooltipManager.ClearInventoryTooltips();
        }
    }
}
