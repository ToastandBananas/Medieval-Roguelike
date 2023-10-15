using InventorySystem;
using System.Collections.Generic;
using UnityEngine;

namespace ActionSystem 
{
    public class UnequipAction : BaseInventoryAction
    {
        List<EquipSlot> targetEquipSlots = new List<EquipSlot>();

        public void QueueAction(EquipSlot targetEquipSlot)
        {
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

        public override int GetActionPointsCost()
        {
            int cost = 0;
            if (unit.UnitEquipment.EquipSlotIsFull(targetEquipSlots[targetEquipSlots.Count - 1]))
                cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlots[targetEquipSlots.Count - 1]], unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlots[targetEquipSlots.Count - 1]].CurrentStackSize);
            else
                Debug.LogWarning($"{targetEquipSlots[targetEquipSlots.Count - 1]} is not full, yet {unit.name} is trying to unequip from it...");
            return cost;
        }

        public override bool IsValidAction() => unit.UnitEquipment != null;
    }
}
