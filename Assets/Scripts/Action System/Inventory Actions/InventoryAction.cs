using GridSystem;
using InventorySystem;
using UnitSystem;
using UnityEngine;

namespace ActionSystem
{
    public class InventoryAction : BaseAction
    {
        ItemData targetItemData;
        UnitEquipment targetItemDatasUnitEquipment; // In case the item is coming from a Unit's equipment

        readonly int actionPointCostPerPound = 10;

        public void SetTarget(ItemData targetItemData, UnitEquipment targetItemDatasUnitEquipment)
        {
            this.targetItemData = targetItemData;
            this.targetItemDatasUnitEquipment = targetItemDatasUnitEquipment;
        }

        public override void TakeAction()
        {
            Debug.Log("Performing inventory action");
        }

        public override int GetActionPointsCost()
        {
            int cost = 0;
            if (targetItemData != null)
            {
                cost += Mathf.RoundToInt(targetItemData.Item.Weight / actionPointCostPerPound) * actionPointCostPerPound;
            }

            return cost;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetEnergyCost() => 0;

        public override bool IsHotbarAction() => false;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;
    }
}
