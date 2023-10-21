using UnityEngine;
using ActionSystem;
using UnitSystem;
using InteractableObjects;

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

            unit.unitActionHandler.GetAction<EquipAction>().QueueAction(itemData, equipSlot, itemsContainerInventoryManager);
            return canEquipItem;
        }

        public EquipSlot EquipSlot => equipSlot;

        public ActionType[] ActionTypes => actionTypes;
    }
}
