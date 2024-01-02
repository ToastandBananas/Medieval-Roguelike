using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Body Armor", menuName = "Inventory/Body Armor")]
    public class Item_BodyArmor : Item_VisibleArmor
    {
        [Header("Body Armor Stats")]
        [SerializeField, Range(-1f, 1f)] float minDodgeChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxDodgeChanceModifier;
        [SerializeField, Range(-1f, 1f)] float minKnockbackChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxKnockbackChanceModifier;
        [SerializeField, Range(-2f, 2f)] float moveCostModifier;
        [SerializeField, Range(-5f, 5f)] float moveNoiseModifier;

        [Header("Secondary Protection")]
        [SerializeField] bool protectsArms;
        [SerializeField] bool protectsLegs;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.BodyArmor;
                initialized = true;
            }
        }

        public float MinDodgeChanceModifier => minDodgeChanceModifier;
        public float MaxDodgeChanceModifier => maxDodgeChanceModifier;
        public float MinKnockbackChanceModifier => minKnockbackChanceModifier;
        public float MaxKnockbackChanceModifier => maxKnockbackChanceModifier;
        public float MoveCostModifier => moveCostModifier;
        public float MoveNoiseModifier => moveNoiseModifier;

        public bool ProtectsArms => protectsArms;
        public bool ProtectsLegs => protectsLegs;
    }
}
