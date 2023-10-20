using InventorySystem;
using UnityEngine;

namespace ActionSystem
{
    // We use Equip & Unequip for when we need to use the Action Point cost for these actions, but don't actually want to queue an Equip or Unequip action
    public enum InventoryActionType { Default, Equip, Unequip }

    public class InventoryAction : BaseInventoryAction
    {
        ItemData targetItemData;
        int itemCount;
        InventoryActionType inventoryActionType;

        public void QueueAction(ItemData targetItemData, int itemCount, InventoryActionType inventoryActionType = InventoryActionType.Default)
        {
            this.targetItemData = targetItemData;
            this.itemCount = itemCount;
            this.inventoryActionType = inventoryActionType;
            QueueAction();
        }

        public override void TakeAction()
        {
            CompleteAction();
        }

        public override int GetActionPointsCost()
        {
            if (inventoryActionType == InventoryActionType.Default)
                return GetItemsActionPointCost(targetItemData, itemCount);
            else if (inventoryActionType == InventoryActionType.Unequip)
                return UnequipAction.GetItemsUnequipActionPointCost(targetItemData, itemCount);
            else
                return EquipAction.GetItemsEquipActionPointCost(targetItemData, itemCount);
        }

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => true;

        public override bool IsHotbarAction() => false;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;
    }
}
