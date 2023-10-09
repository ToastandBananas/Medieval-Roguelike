using UnityEngine;
using UnitSystem;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable")]
    public class Consumable : Item
    {
        [Header("Item/Sprite Change Thresholds")]
        [SerializeField] ItemChangeThreshold[] itemChangeThresholds;

        public override bool Use(Unit unit, ItemData itemData, int amountToUse = 1)
        {
            ItemChangeThreshold currentItemChangeThreshold = ItemChangeThreshold.GetCurrentItemChangeThreshold(itemData, itemChangeThresholds);

            base.Use(unit, itemData, amountToUse);

            if (ItemChangeThreshold.ThresholdReached(itemData, true, currentItemChangeThreshold, itemChangeThresholds, out ItemChangeThreshold newThreshold))
            {
                if (itemData.MyInventory() != null)
                {
                    SlotCoordinate itemsSlotCoordinate = itemData.MyInventory().GetSlotCoordinateFromItemData(itemData);
                    if (newThreshold.NewItem == currentItemChangeThreshold.NewItem)
                    {
                        if (itemsSlotCoordinate.myInventory.slotVisualsCreated)
                            itemsSlotCoordinate.myInventory.GetSlotFromCoordinate(itemsSlotCoordinate).InventoryItem.SetupIconSprite(true);
                    }
                    else
                    {
                        itemData.MyInventory().RemoveItem(itemData);
                        itemData.SetItem(newThreshold.NewItem);
                        itemData.MyInventory().TryAddItemAt(itemData.MyInventory().GetSlotCoordinate(itemsSlotCoordinate.coordinate.x - width + newThreshold.NewItem.Width, itemsSlotCoordinate.coordinate.y - height + newThreshold.NewItem.Height), itemData);
                    }
                }
            }

            return true;
        }

        public override Sprite InventorySprite(ItemData itemData = null)
        {
            if (itemData == null)
                return base.InventorySprite();

            ItemChangeThreshold itemChangeThreshold = ItemChangeThreshold.GetCurrentItemChangeThreshold(itemData, itemChangeThresholds);
            if (itemChangeThreshold != null && itemChangeThreshold.NewSprite != null)
                return itemChangeThreshold.NewSprite;
            return base.InventorySprite();
        }

        public ItemChangeThreshold[] ItemChangeThresholds => itemChangeThresholds;
    }
}
