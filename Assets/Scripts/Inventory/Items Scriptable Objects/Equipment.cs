using UnityEngine;
using ActionSystem;
using UnitSystem;

namespace InventorySystem
{
    public abstract class Equipment : Item
    {
        [Header("Equipment Info")]
        [SerializeField] protected EquipSlot equipSlot;

        [Header("Actions")]
        [SerializeField] ActionType[] actionTypes;

        public override bool Use(Unit unit, ItemData itemData, int amountToUse = 1)
        { 
            bool canEquipItem = unit.UnitEquipment.CanEquipItemAt(itemData, equipSlot);
            unit.unitActionHandler.GetAction<EquipAction>().QueueAction(itemData, equipSlot);
            return canEquipItem;
        }

        public EquipSlot EquipSlot => equipSlot;

        public ActionType[] ActionTypes => actionTypes;
    }
}
