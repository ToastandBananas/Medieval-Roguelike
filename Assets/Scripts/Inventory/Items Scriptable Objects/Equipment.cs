using UnityEngine;
using ActionSystem;

namespace InventorySystem
{
    public abstract class Equipment : Item
    {
        [Header("Equipment Info")]
        [SerializeField] EquipSlot equipSlot;

        [Header("Actions")]
        [SerializeField] ActionType[] actionTypes;

        public EquipSlot EquipSlot => equipSlot;

        public ActionType[] ActionTypes => actionTypes;
    }
}
