using UnityEngine;
using InventorySystem;

namespace ActionSystem
{
    public class SwapWeaponSetAction : BaseInventoryAction
    {
        readonly static float swapAPMultiplier = 0.5f;

        public override void TakeAction()
        {
            unit.UnitEquipment.SwapWeaponSet();
            CompleteAction();
        }

        public override int GetActionPointsCost()
        {
            int cost = 0;
            if (unit.UnitEquipment.currentWeaponSet == WeaponSet.One) // Weapon Set 1 --> Weapon Set 2
            {
                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                    cost += UnequipAction.GetItemsUnequipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1], 1, null);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                    cost += UnequipAction.GetItemsUnequipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1], 1, null);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                    cost += EquipAction.GetItemsEquipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2], 1, null);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                    cost += EquipAction.GetItemsEquipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2], 1, null);
            }
            else // Weapon Set 2 --> Weapon Set 1
            {
                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                    cost += UnequipAction.GetItemsUnequipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2], 1, null);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                    cost += UnequipAction.GetItemsUnequipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2], 1, null);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                    cost += EquipAction.GetItemsEquipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1], 1, null);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                    cost += EquipAction.GetItemsEquipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1], 1, null);
            }

            // Swapping a weapon set shouldn't cost as much as actually equipping and unequipping the items
            cost = Mathf.RoundToInt(cost * swapAPMultiplier);

            // Debug.Log($"Swap Weapon Set Cost for {unit.name}: {cost}");
            return cost;
        }

        public override bool IsInterruptable() => false;

        public override bool IsValidAction() => unit.UnitEquipment != null;

        public override bool CanBeClearedFromActionQueue() => false;

        public override string TooltipDescription() => "";
    }
}
