using InventorySystem;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_Unequip : Action_BaseInventory
    {
        readonly List<EquipSlot> targetEquipSlots = new();
        InventoryManager_Container itemsContainerInventoryManager;

        static readonly float unequipAPCostMultiplier = 0.75f;

        public void QueueAction(EquipSlot targetEquipSlot, InventoryManager_Container itemsContainerInventoryManager)
        {
            this.itemsContainerInventoryManager = itemsContainerInventoryManager;

            targetEquipSlots.Add(targetEquipSlot);
            Unit.UnitActionHandler.QueueAction(this);
        }

        public override void TakeAction()
        {
            if (targetEquipSlots.Count > 0)
                Unit.UnitEquipment.UnequipItem(targetEquipSlots[0]);

            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            targetEquipSlots.RemoveAt(0);
        }

        public static int GetItemsUnequipActionPointCost(ItemData itemData, int stackSize, InventoryManager_Container itemsContainerInventoryManager)
        {
            // Debug.Log($"Unequip Cost of {itemData.Item.Name}: {Mathf.RoundToInt(EquipAction.GetItemsEquipActionPointCost(itemData, stackSize) * unequipActionPointCostMultiplier)}");
            return Mathf.RoundToInt(Action_Equip.GetItemsEquipActionPointCost(itemData, stackSize, itemsContainerInventoryManager) * unequipAPCostMultiplier);
        }

        public override int ActionPointsCost()
        {
            int cost = 0;
            if (Unit.UnitEquipment.EquipSlotIsFull(targetEquipSlots[targetEquipSlots.Count - 1]))
                cost += GetItemsUnequipActionPointCost(Unit.UnitEquipment.EquippedItemData(targetEquipSlots[targetEquipSlots.Count - 1]), Unit.UnitEquipment.EquippedItemData(targetEquipSlots[targetEquipSlots.Count - 1]).CurrentStackSize, itemsContainerInventoryManager);
            else
                Debug.LogWarning($"{targetEquipSlots[targetEquipSlots.Count - 1]} is not full, yet {Unit.name} is trying to unequip from it...");

            itemsContainerInventoryManager = null;
            return cost;
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment != null;

        public override bool IsInterruptable() => false;

        public override bool CanBeClearedFromActionQueue() => false;

        public override string TooltipDescription() => "";
    }
}
