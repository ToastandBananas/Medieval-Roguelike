using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Shield", menuName = "Inventory/Shield")]
    public class Item_Shield : Item_HeldEquipment
    {
        [Header("Shield Stats")]
        [Tooltip("The amount of damage that will be blocked")]
        [SerializeField] Vector2Int blockPowerRange;
        [SerializeField] Vector2Int durabilityRange = Vector2Int.one;

        [Header("Shield Bash Damage")]
        [SerializeField] Vector2Int minDamageRange;
        [SerializeField] Vector2Int maxDamageRange;

        [Header("Modifiers")]
        [SerializeField, Range(-1f, 1f)] float minBlockChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxBlockChanceModifier;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.LeftHeldItem1;
                initialized = true;
            }
        }

        public int MinBlockPower => blockPowerRange.x;
        public int MaxBlockPower => blockPowerRange.y;

        public int MinDurability => durabilityRange.x;
        public int MaxDurability => durabilityRange.y;

        public int MinMinimumDamage => minDamageRange.x;
        public int MaxMinimumDamage => minDamageRange.y;
        public int MinMaximumDamage => maxDamageRange.x;
        public int MaxMaximumDamage => maxDamageRange.y;

        public float MinBlockChanceModifier => minBlockChanceModifier;
        public float MaxBlockChanceModifier => maxBlockChanceModifier;
    }
}
