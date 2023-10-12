using GridSystem;
using UnityEngine;
using InventorySystem;
using UnitSystem;

namespace ActionSystem
{
    public enum EquipmentActionType { Equip, Unequip, SwapWeaponSet }

    public class EquipmentAction : BaseAction
    {
        ItemData targetItemData;
        UnitEquipment targetItemDatasUnitEquipment; // In case the item is coming from a Unit's equipment
        EquipmentActionType currentEquipmentActionType;

        readonly int actionPointCostPerPound = 20;

        public void QueueAction(EquipmentActionType equipmentActionType, ItemData targetItemData, UnitEquipment targetItemDatasUnitEquipment)
        {
            currentEquipmentActionType = equipmentActionType;
            this.targetItemData = targetItemData;
            this.targetItemDatasUnitEquipment = targetItemDatasUnitEquipment;

            unit.unitActionHandler.QueueAction(this);
        }

        public override void TakeAction()
        {
            Debug.Log("Performing equipment action");
            switch (currentEquipmentActionType)
            {
                case EquipmentActionType.Equip:
                    break;
                case EquipmentActionType.Unequip:
                    break;
                case EquipmentActionType.SwapWeaponSet:
                    unit.UnitEquipment.SwapWeaponSet();
                    break;
            }

            CompleteAction();
        }

        public override int GetActionPointsCost()
        {
            int cost = 0;
            if (targetItemData != null)
            {
                cost += CalculateItemsActionPointCost(targetItemData);
            }
            else if (currentEquipmentActionType == EquipmentActionType.SwapWeaponSet)
            {
                if (unit.UnitEquipment.currentWeaponSet == WeaponSet.One)
                {
                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1]);

                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1]);

                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2]);

                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2]);
                }
                else
                {
                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem1]);

                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem1]);

                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.LeftHeldItem2]);

                    if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                        cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.RightHeldItem2]);
                }
            }

            return cost;
        }

        int CalculateItemsActionPointCost(ItemData itemData)
        {
            return Mathf.RoundToInt(itemData.Item.Weight / actionPointCostPerPound) * actionPointCostPerPound;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            targetItemData = null;
            targetItemDatasUnitEquipment = null;

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => true;

        public override bool IsHotbarAction() => false;

        public override bool IsValidAction() => unit.UnitEquipment != null;

        public override bool ActionIsUsedInstantly() => true;
    }
}
