using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Leg Armor", menuName = "Inventory/Leg Armor")]
    public class Item_LegArmor : Item_Armor
    {
        [Header("Leg Armor Stats")]
        [SerializeField, Range(-1f, 1f)] float minDodgeChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxDodgeChanceModifier;
        [SerializeField, Range(-1f, 1f)] float minKnockbackChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxKnockbackChanceModifier;
        [SerializeField, Range(-2f, 2f)] float moveCostModifier;
        [SerializeField, Range(-5f, 5f)] float moveNoiseModifier;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.LegArmor;
                initialized = true;
            }
        }

        public float MinDodgeChanceModifier => minDodgeChanceModifier;
        public float MaxDodgeChanceModifier => maxDodgeChanceModifier;
        public float MinKnockbackChanceModifier => minKnockbackChanceModifier;
        public float MaxKnockbackChanceModifier => maxKnockbackChanceModifier;
        public float MoveCostModifier => moveCostModifier;
        public float MoveNoiseModifier => moveNoiseModifier;
    }
}
