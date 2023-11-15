using InventorySystem;
using UnityEngine;
using UnitSystem.ActionSystem.UI;

namespace UnitSystem.ActionSystem
{
    public abstract class BaseInventoryAction : BaseAction
    {
        readonly static int defaultAPCostPerPound = 20;
        readonly static int minimumAPCost = 50;
        readonly static float insideBagAPCostMultiplier = 0.2f;

        public static int GetItemsActionPointCost(ItemData itemData, int stackSize, ContainerInventoryManager itemsContainerInventoryManager)
        {
            float cost = CalculateItemsCost(itemData.Weight(), GetItemSizeMultiplier(itemData.Item.ItemSize), stackSize);

            if (itemsContainerInventoryManager != null)
            {
                for (int i = 0; i < itemsContainerInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    ItemData itemInContainer = itemsContainerInventoryManager.ParentInventory.ItemDatas[i];
                    cost += CalculateItemsCost(itemInContainer.Weight(), GetItemSizeMultiplier(itemInContainer.Item.ItemSize), itemInContainer.CurrentStackSize) * insideBagAPCostMultiplier;
                }

                for (int i = 0; i < itemsContainerInventoryManager.SubInventories.Length; i++)
                {
                    for (int j = 0; j < itemsContainerInventoryManager.SubInventories[i].ItemDatas.Count; j++)
                    {
                        ItemData itemInContainer = itemsContainerInventoryManager.SubInventories[i].ItemDatas[j];
                        cost += CalculateItemsCost(itemInContainer.Weight(), GetItemSizeMultiplier(itemInContainer.Item.ItemSize), itemInContainer.CurrentStackSize) * insideBagAPCostMultiplier;
                    }
                }
            }

            if (cost < minimumAPCost)
                cost = minimumAPCost;

            // Debug.Log($"Cost for {itemData.Item.Name}: {Mathf.RoundToInt(itemData.Item.Weight * defaultActionPointCostPerPound * GetItemSizeMultiplier(itemData.Item.ItemSize) * stackSize)}");
            return Mathf.RoundToInt(cost);
        }

        static float CalculateItemsCost(float itemWeight, float itemSizeMultiplier, int stackSize) => itemWeight * defaultAPCostPerPound * itemSizeMultiplier * stackSize;

        protected static float GetItemSizeMultiplier(ItemSize itemSize)
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

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.None;

        public override bool ActionIsUsedInstantly() => true;
    }
}
