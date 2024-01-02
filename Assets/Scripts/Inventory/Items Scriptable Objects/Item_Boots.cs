using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Boots", menuName = "Inventory/Boots")]
    public class Item_Boots : Item_Armor
    {
        [Header("Boots Stats")]
        [SerializeField, Range(-1f, 1f)] float minKnockbackChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxKnockbackChanceModifier;
        [SerializeField, Range(-5f, 5f)] float moveNoiseModifier;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Boots;
                initialized = true;
            }
        }

        public float MinKnockbackChanceModifier => minKnockbackChanceModifier;
        public float MaxKnockbackChanceModifier => maxKnockbackChanceModifier;
        public float MoveNoiseModifier => moveNoiseModifier;
    }
}
