using UnityEngine;
using UnitSystem.ActionSystem;
using UnitSystem;
using InteractableObjects;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public abstract class Equipment : Item
    {
        [Header("Equipment Info")]
        [SerializeField] protected EquipSlot equipSlot;

        [Header("Actions")]
        [SerializeField] ActionType[] actionTypes;

        public override bool Use(Unit unit, ItemData itemData, Slot slotUsingFrom, LooseItem looseItemUsing, int amountToUse = 1)
        { 
            bool canEquipItem = unit.UnitEquipment.CanEquipItemAt(itemData, equipSlot);

            ContainerInventoryManager itemsContainerInventoryManager = null;
            if (slotUsingFrom != null && slotUsingFrom is ContainerEquipmentSlot)
                itemsContainerInventoryManager = slotUsingFrom.EquipmentSlot.ContainerEquipmentSlot.containerInventoryManager;
            else if (looseItemUsing != null && looseItemUsing is LooseContainerItem)
                itemsContainerInventoryManager = looseItemUsing.LooseContainerItem.ContainerInventoryManager;

            unit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemData, amountToUse, itemsContainerInventoryManager, InventoryActionType.Equip);
            unit.UnitActionHandler.GetAction<Action_Equip>().TakeActionImmediately(itemData, equipSlot, itemsContainerInventoryManager);
            return canEquipItem;
        }

        public bool HasAccessToAction(Action_Base baseAction) => HasAccessToAction(baseAction.ActionType);

        public bool HasAccessToAction(ActionType actionType)
        {
            for (int i = 0; i < actionTypes.Length; i++)
            {
                if (actionTypes[i] == actionType)
                    return true;
            }
            return false;
        }

        public EquipSlot EquipSlot => equipSlot;

        public ActionType[] ActionTypes => actionTypes;
    }
}
