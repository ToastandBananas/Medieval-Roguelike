using InventorySystem;
using UnitSystem;
using UnityEngine;

namespace ActionSystem
{
    public abstract class BaseInventoryAction : BaseAction
    {
        protected readonly int defaultActionPointCostPerPound = 20;
        readonly int nearestMultipleToRoundTo = 5;

        protected virtual int GetItemsActionPointCost(ItemData itemData, int stackSize)
        {
            int costPerPound = defaultActionPointCostPerPound;
            if (itemData.Item is Equipment)
            {
                if (UnitEquipment.IsHeldItemEquipSlot(itemData.Item.Equipment.EquipSlot))
                    costPerPound = 15; 
                else
                {
                    switch (itemData.Item.Equipment.EquipSlot)
                    {
                        case EquipSlot.Helm:
                            costPerPound = 25;
                            break;
                        case EquipSlot.BodyArmor:
                            costPerPound = 35;
                            break;
                        case EquipSlot.Shirt:
                            costPerPound = 30;
                            break;
                        case EquipSlot.Gloves:
                            costPerPound = 30;
                            break;
                        case EquipSlot.Boots:
                            costPerPound = 30;
                            break;
                        case EquipSlot.Back:
                            costPerPound = 15;
                            break;
                        case EquipSlot.Quiver:
                            costPerPound = 25;
                            break;
                    }
                }
            }

            // Debug.Log($"Cost for {itemData.Item.name}: " + CalculateItemsActionPointCost(itemData.Item.Weight, costPerPound, GetItemSizeMultiplier(itemData.Item.ItemSize), itemData.CurrentStackSize));
            return CalculateItemsActionPointCost(itemData.Item.Weight, costPerPound, GetItemSizeMultiplier(itemData.Item.ItemSize), stackSize);
        }

        int CalculateItemsActionPointCost(float itemWeight, int costPerPound, float itemSizeMultiplier, int stackSize) => Mathf.RoundToInt(itemWeight * costPerPound * itemSizeMultiplier * stackSize / nearestMultipleToRoundTo) * nearestMultipleToRoundTo;

        float GetItemSizeMultiplier(ItemSize itemSize)
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
