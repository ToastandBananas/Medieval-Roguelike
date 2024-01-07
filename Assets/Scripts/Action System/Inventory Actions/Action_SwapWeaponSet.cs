using UnityEngine;
using InventorySystem;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_SwapWeaponSet : Action_BaseInventory
    {
        readonly static float swapAPMultiplier = 0.5f;

        public override void QueueAction()
        {
            Unit.UnitActionHandler.ClearActionQueue(true);
            if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitMeshManager.GetHeldRangedWeapon().IsLoaded)
                Unit.UnitActionHandler.GetAction<Action_Reload>().QueueAction();

            base.QueueAction();
        }

        public override void TakeAction()
        {
            Unit.UnitEquipment.HumanoidEquipment.SwapWeaponSet();
            CompleteAction();
        }

        public override int ActionPointsCost()
        {
            int cost = 0;
            if (Unit.UnitEquipment.HumanoidEquipment.CurrentWeaponSet == WeaponSet.One) // Weapon Set 1 --> Weapon Set 2
            {
                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                    cost += Action_Unequip.GetItemsUnequipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1], 1, null);

                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                    cost += Action_Unequip.GetItemsUnequipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1], 1, null);

                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                    cost += Action_Equip.GetItemsEquipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2], 1, null);

                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                    cost += Action_Equip.GetItemsEquipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2], 1, null);
            }
            else // Weapon Set 2 --> Weapon Set 1
            {
                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                    cost += Action_Unequip.GetItemsUnequipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2], 1, null);

                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                    cost += Action_Unequip.GetItemsUnequipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2], 1, null);

                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                    cost += Action_Equip.GetItemsEquipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1], 1, null);

                if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                    cost += Action_Equip.GetItemsEquipActionPointCost(Unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1], 1, null);
            }

            // Swapping a weapon set shouldn't cost as much as actually equipping and unequipping the items
            cost = Mathf.RoundToInt(cost * swapAPMultiplier);

            // Debug.Log($"Swap Weapon Set Cost for {unit.name}: {cost}");
            return cost;
        }

        public override bool CanQueueMultiple() => false;

        public override bool IsInterruptable() => false;

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment != null;

        public override bool CanBeClearedFromActionQueue() => false;

        public override string TooltipDescription() => "";
    }
}
