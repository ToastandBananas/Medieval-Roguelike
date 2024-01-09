using InventorySystem;
using UnityEngine;
using UnitSystem.ActionSystem.UI;

namespace UnitSystem.ActionSystem.Actions
{
    public abstract class Action_BaseInventory : Action_Base
    {
        readonly static int defaultAPCostPerPound = 20;
        readonly static int minimumAPCost = 50;
        readonly static float insideBagAPCostMultiplier = 0.2f;

        public static int GetItemsActionPointCost(ItemData itemData, int stackSize, InventoryManager_Container itemsContainerInventoryManager)
        {
            float cost = CalculateItemsCost(itemData.Weight(), stackSize);

            if (itemsContainerInventoryManager != null)
            {
                for (int i = 0; i < itemsContainerInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    ItemData itemInContainer = itemsContainerInventoryManager.ParentInventory.ItemDatas[i];
                    cost += CalculateItemsCost(itemInContainer.Weight(), itemInContainer.CurrentStackSize) * insideBagAPCostMultiplier;
                }

                for (int i = 0; i < itemsContainerInventoryManager.SubInventories.Length; i++)
                {
                    for (int j = 0; j < itemsContainerInventoryManager.SubInventories[i].ItemDatas.Count; j++)
                    {
                        ItemData itemInContainer = itemsContainerInventoryManager.SubInventories[i].ItemDatas[j];
                        cost += CalculateItemsCost(itemInContainer.Weight(), itemInContainer.CurrentStackSize) * insideBagAPCostMultiplier;
                    }
                }
            }

            if (cost < minimumAPCost)
                cost = minimumAPCost;

            // Debug.Log($"Cost for {itemData.Item.Name}: {Mathf.RoundToInt(itemData.Item.Weight * defaultActionPointCostPerPound * GetItemSizeMultiplier(itemData.Item.ItemSize) * stackSize)}");
            return Mathf.RoundToInt(cost);
        }

        static float CalculateItemsCost(float itemWeight, int stackSize) => itemWeight * defaultAPCostPerPound * stackSize;

        public override void CompleteAction()
        {
            base.CompleteAction();

            Unit.UnitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override int EnergyCost() => 0;

        public override bool CanQueueMultiple() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.None;

        public override bool ActionIsUsedInstantly() => true;
    }
}
