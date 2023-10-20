using InteractableObjects;
using InventorySystem;
using UnitSystem;
using UnityEngine;

namespace ActionSystem
{
    public abstract class BaseInventoryAction : BaseAction
    {
        protected readonly static int defaultActionPointCostPerPound = 20;

        public static int GetItemsActionPointCost(ItemData itemData, int stackSize)
        {
            // Debug.Log($"Cost for {itemData.Item.Name}: {Mathf.RoundToInt(itemData.Item.Weight * defaultActionPointCostPerPound * GetItemSizeMultiplier(itemData.Item.ItemSize) * stackSize)}");
            return Mathf.RoundToInt(itemData.Item.Weight * defaultActionPointCostPerPound * GetItemSizeMultiplier(itemData.Item.ItemSize) * stackSize);
        }

        static float GetItemSizeMultiplier(ItemSize itemSize)
        {
            switch (itemSize)
            {
                case ItemSize.ExtraSmall:
                    return 0.25f;
                case ItemSize.VerySmall:
                    return 0.5f;
                case ItemSize.Small:
                    return 0.75f;
                case ItemSize.Medium:
                    return 1f;
                case ItemSize.Large:
                    return 1.25f;
                case ItemSize.VeryLarge:
                    return 1.5f;
                case ItemSize.ExtraLarge:
                    return 1.75f;
                default:
                    return 1f;
            }
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => true;

        public override bool IsHotbarAction() => false;

        public override bool ActionIsUsedInstantly() => true;
    }
}
