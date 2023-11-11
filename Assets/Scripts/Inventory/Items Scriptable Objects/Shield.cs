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
        [SerializeField] Vector2 blockChanceAddOnRange;

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

        public float MinBlockChanceAddOn => blockChanceAddOnRange.x;
        public float MaxBlockChanceAddOn => blockChanceAddOnRange.y;
    }
}
