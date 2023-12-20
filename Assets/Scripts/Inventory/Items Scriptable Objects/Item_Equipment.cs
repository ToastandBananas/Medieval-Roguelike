using UnityEngine;
using UnitSystem.ActionSystem;
using UnitSystem;
using InteractableObjects;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public abstract class Item_Equipment : Item
    {
        [Header("Equipment Info")]
        [SerializeField] protected EquipSlot equipSlot;

        [Header("Actions")]
        [SerializeField] ActionType[] actionTypes;

        public override bool Use(Unit unit, ItemData itemData, Slot slotUsingFrom, Interactable_LooseItem looseItemUsing, int amountToUse = 1)
        {
            EquipSlot targetEquipSlot = equipSlot;
            if (itemData.Item is Item_HeldEquipment && (!unit.UnitEquipment.CapableOfEquippingHeldItem(itemData, equipSlot, false)
                || ((itemData.Item is Item_Weapon == false || !itemData.Item.Weapon.IsTwoHanded) && unit.UnitEquipment.EquipSlotHasItem(targetEquipSlot)
                    && (unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlot].Item is Item_Weapon == false || !unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlot].Item.Weapon.IsTwoHanded))))
            {
                EquipSlot oppositeEquipSlot = unit.UnitEquipment.GetOppositeHeldItemEquipSlot(equipSlot);
                if (unit.UnitEquipment.CapableOfEquippingHeldItem(itemData, oppositeEquipSlot, false))
                    targetEquipSlot = oppositeEquipSlot;
            }

            bool canEquipItem = unit.UnitEquipment.CanEquipItemAt(itemData, targetEquipSlot);
            if (canEquipItem)
            {
                ContainerInventoryManager itemsContainerInventoryManager = null;
                if (slotUsingFrom != null && slotUsingFrom is ContainerEquipmentSlot)
                    itemsContainerInventoryManager = slotUsingFrom.EquipmentSlot.ContainerEquipmentSlot.containerInventoryManager;
                else if (looseItemUsing != null && looseItemUsing is Interactable_LooseContainerItem)
                    itemsContainerInventoryManager = looseItemUsing.LooseContainerItem.ContainerInventoryManager;

                unit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemData, amountToUse, itemsContainerInventoryManager, InventoryActionType.Equip);
                unit.UnitActionHandler.GetAction<Action_Equip>().TakeActionImmediately(itemData, targetEquipSlot, itemsContainerInventoryManager);
            }

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
