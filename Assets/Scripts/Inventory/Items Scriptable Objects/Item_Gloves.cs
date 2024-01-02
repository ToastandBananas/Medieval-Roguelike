using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Gloves", menuName = "Inventory/Gloves")]
    public class Item_Gloves : Item_Armor
    {
        [Header("Gloves Stats")]
        [SerializeField, Range(-1f, 1f)] float minAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float maxAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float minFumbleChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxFumbleChanceModifier;
        [SerializeField, Range(-1f, 1f)] float minUnarmedDamageMultiplier;
        [SerializeField, Range(-1f, 1f)] float maxUnarmedDamageMultiplier;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Gloves;
                initialized = true;
            }
        }

        public float MinAccuracyModifier => minAccuracyModifier;
        public float MaxAccuracyModifier => maxAccuracyModifier;
        public float MinFumbleChanceModifier => minFumbleChanceModifier;
        public float MaxFumbleChanceModifier => maxFumbleChanceModifier;
        public float MinUnarmedDamageMultiplier => minUnarmedDamageMultiplier;
        public float MaxUnarmedDamageMultiplier => maxUnarmedDamageMultiplier;
    }
}
