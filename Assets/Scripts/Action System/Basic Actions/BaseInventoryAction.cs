using InventorySystem;
using UnitSystem;
using UnityEngine;

namespace ActionSystem
{
    public abstract class BaseInventoryAction : BaseAction
    {
        protected readonly int actionPointCostPerPound = 20;

        protected virtual int CalculateItemsActionPointCost(ItemData itemData)
        {
            return Mathf.RoundToInt(itemData.Item.Weight * itemData.CurrentStackSize * actionPointCostPerPound / 5) * 5;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => true;

        public override bool IsHotbarAction() => false;

        public override bool ActionIsUsedInstantly() => true;
    }
}
