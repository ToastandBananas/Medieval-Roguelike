using InventorySystem;

namespace ActionSystem
{
    public class InventoryAction : BaseInventoryAction
    {
        ItemData targetItemData;
        int itemCount;

        public void QueueAction(ItemData targetItemData, int itemCount)
        {
            this.targetItemData = targetItemData;
            this.itemCount = itemCount;
            QueueAction();
        }

        public override void TakeAction()
        {
            CompleteAction();
        }

        public override int GetActionPointsCost()
        {
            return GetItemsActionPointCost(targetItemData, itemCount);
        }

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => true;

        public override bool IsHotbarAction() => false;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;
    }
}
