using UnityEngine;

namespace InventorySystem
{
    [System.Serializable]
    public class InventoryLayout
    {
        [Header("Inventory Dimensions")]
        [SerializeField] int amountOfSlots;
        [SerializeField] int maxSlotsPerRow = 10;

        [Header("Slot Size")]
        [SerializeField] int slotWidth = 1;
        [SerializeField] int slotHeight = 1;

        [Header("Placeholder Icon")]
        [SerializeField] Sprite placeholderSprite;

        [Header("Item Types")]
        [SerializeField] ItemType[] allowedItemTypes;

        public int AmountOfSlots => amountOfSlots;
        public int MaxSlotsPerRow => maxSlotsPerRow;

        public int SlotWidth => slotWidth;
        public int SlotHeight => slotHeight;

        public Sprite PlaceholderSprite => placeholderSprite;

        public ItemType[] AllowedItemTypes => allowedItemTypes;

        public InventoryLayout()
        {

        }

        public void SetLayoutValues(int amountOfSlots, int maxSlotsPerRow, int slotWidth, int slotHeight, ItemType[] allowedItemTypes, Sprite placeholderSprite)
        {
            this.amountOfSlots = amountOfSlots;
            this.maxSlotsPerRow = maxSlotsPerRow;
            this.slotWidth = slotWidth;
            this.slotHeight = slotHeight;
            this.allowedItemTypes = allowedItemTypes;
            this.placeholderSprite = placeholderSprite;
        }

        public void SetLayoutValues(InventoryLayout inventoryLayout)
        {
            amountOfSlots = inventoryLayout.amountOfSlots;
            maxSlotsPerRow = inventoryLayout.maxSlotsPerRow;
            slotWidth = inventoryLayout.slotWidth;
            slotHeight = inventoryLayout.slotHeight;
            allowedItemTypes = inventoryLayout.allowedItemTypes;
            placeholderSprite = inventoryLayout.placeholderSprite;
        }

        public bool HasStandardSlotSize => slotWidth == 1 && slotHeight == 1;
    }
}
