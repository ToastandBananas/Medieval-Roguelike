using InventorySystem;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem.ActionSystem 
{
    public class UnequipAction : BaseInventoryAction
    {
        List<EquipSlot> targetEquipSlots = new List<EquipSlot>();
        ContainerInventoryManager itemsContainerInventoryManager;

        static readonly float unequipAPCostMultiplier = 0.75f;

        public void QueueAction(EquipSlot targetEquipSlot, ContainerInventoryManager itemsContainerInventoryManager)
        {
            this.itemsContainerInventoryManager = itemsContainerInventoryManager;

            targetEquipSlots.Add(targetEquipSlot);
            unit.unitActionHandler.QueueAction(this);
        }

        public override void TakeAction()
        {
            if (targetEquipSlots.Count > 0)
                unit.UnitEquipment.UnequipItem(targetEquipSlots[0]);

            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            targetEquipSlots.RemoveAt(0);
        }

        public static int GetItemsUnequipActionPointCost(ItemData itemData, int stackSize, ContainerInventoryManager itemsContainerInventoryManager)
        {
            // Debug.Log($"Unequip Cost of {itemData.Item.Name}: {Mathf.RoundToInt(EquipAction.GetItemsEquipActionPointCost(itemData, stackSize) * unequipActionPointCostMultiplier)}");
            return Mathf.RoundToInt(EquipAction.GetItemsEquipActionPointCost(itemData, stackSize, itemsContainerInventoryManager) * unequipAPCostMultiplier);
        }

        public override int ActionPointsCost()
        {
            int cost = 0;
            if (unit.UnitEquipment.EquipSlotIsFull(targetEquipSlots[targetEquipSlots.Count - 1]))
                cost += GetItemsUnequipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlots[targetEquipSlots.Count - 1]], unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlots[targetEquipSlots.Count - 1]].CurrentStackSize, itemsContainerInventoryManager);
            else
                Debug.LogWarning($"{targetEquipSlots[targetEquipSlots.Count - 1]} is not full, yet {unit.name} is trying to unequip from it...");

            itemsContainerInventoryManager = null;
            return cost;
        }

        public override bool IsValidAction() => unit.UnitEquipment != null;

        public override bool IsInterruptable() => false;

        public override bool CanBeClearedFromActionQueue() => false;

        public override string TooltipDescription() => "";
    }
}
