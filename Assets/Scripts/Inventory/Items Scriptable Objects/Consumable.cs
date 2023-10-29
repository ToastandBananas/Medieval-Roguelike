using UnityEngine;
using UnitSystem;
using InteractableObjects;
using ActionSystem;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable")]
    public class Consumable : Item
    {
        [Header("Item/Sprite Change Thresholds")]
        [SerializeField] ItemChangeThreshold[] itemChangeThresholds;

        [Header("Consumable Info")]
        [SerializeField] float consumeAPCostMultiplier = 1f;

        public override bool Use(Unit unit, ItemData itemData, Slot slotUsingFrom, LooseItem looseItemUsing, int amountToUse = 1)
        {
            // For each part of a consumable we're trying to eat, we should queue a separate ConsumeAction. That way we're not incurring a huge AP cost at once and consuming can be interrupted.
            for (int i = 0; i < amountToUse; i++)
            {
                unit.unitActionHandler.GetAction<ConsumeAction>().QueueAction(itemData);
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

        public float ConsumeAPCostMultiplier => consumeAPCostMultiplier;
    }
}
