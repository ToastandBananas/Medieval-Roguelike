using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Shield", menuName = "Inventory/Shield")]
    public class Shield : HeldEquipment
    {
        [Header("Block Power")]
        [Tooltip("The amount of damage that will be blocked")]
        [SerializeField] Vector2Int blockPowerRange;

        [Header("Shield Bash Damage")]
        [SerializeField] Vector2Int damageRange;

        [Header("Modifiers")]
        [SerializeField, Range(-1f, 1f)] float minBlockChanceAddOn;
        [SerializeField, Range(-1f, 1f)] float maxBlockChanceAddOn;

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

        public int MinDamage => damageRange.x;
        public int MaxDamage => damageRange.y;

        public float MinBlockChanceAddOn => minBlockChanceAddOn;
        public float MaxBlockChanceAddOn => maxBlockChanceAddOn;
    }
}
