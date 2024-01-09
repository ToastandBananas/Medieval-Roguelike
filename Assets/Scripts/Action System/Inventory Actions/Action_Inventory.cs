using InventorySystem;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    // We use Equip & Unequip for when we need to use the Action Point cost for these actions, but don't actually want to queue an Equip or Unequip action
    public enum InventoryActionType { Default, Equip, Unequip, Drop }

    public class Action_Inventory : Action_BaseInventory
    {
        ItemData targetItemData;
        int itemCount;
        InventoryManager_Container itemsContainerInventoryManager;
        InventoryActionType inventoryActionType;

        readonly float dropActionPointCostMultiplier = 0.2f;

        public void QueueAction(ItemData targetItemData, int itemCount, InventoryManager_Container itemsContainerInventoryManager, InventoryActionType inventoryActionType = InventoryActionType.Default)
        {
            this.targetItemData = targetItemData;
            this.itemCount = itemCount;
            this.itemsContainerInventoryManager = itemsContainerInventoryManager;
            this.inventoryActionType = inventoryActionType;
            QueueAction();
        }

        public override void TakeAction()
        {
            CompleteAction();
        }

        public override int ActionPointsCost()
        {
            int cost;
            if (inventoryActionType == InventoryActionType.Default)
                cost = GetItemsActionPointCost(targetItemData, itemCount, itemsContainerInventoryManager);
            else if (inventoryActionType == InventoryActionType.Drop)
                cost = Mathf.RoundToInt(GetItemsActionPointCost(targetItemData, itemCount, itemsContainerInventoryManager) * dropActionPointCostMultiplier);
            else if (inventoryActionType == InventoryActionType.Unequip)
                cost = Action_Unequip.GetItemsUnequipActionPointCost(targetItemData, itemCount, itemsContainerInventoryManager);
            else
                cost = Action_Equip.GetItemsEquipActionPointCost(targetItemData, itemCount, itemsContainerInventoryManager);

            itemsContainerInventoryManager = null;
            return cost;
        }

        public override int EnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => true;

        public override bool CanBeClearedFromActionQueue() => false;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override string TooltipDescription() => "";
    }
}
