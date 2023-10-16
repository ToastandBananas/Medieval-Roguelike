using UnityEngine;
using InventorySystem;

namespace ActionSystem
{
    public class SwapWeaponSetAction : BaseInventoryAction
    {
        public override void TakeAction()
        {
            unit.UnitEquipment.SwapWeaponSet();
            CompleteAction();
        }

        public override int GetActionPointsCost()
        {
            int cost = 0;
            if (unit.UnitEquipment.currentWeaponSet == WeaponSet.One)
            {
                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1], 1);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1], 1);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2], 1);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2], 1);
            }
            else
            {
                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1], 1);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1], 1);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2], 1);

                if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                    cost += GetItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2], 1);
            }

            return cost;
        }

        public override bool IsValidAction() => unit.UnitEquipment != null;
    }
}
