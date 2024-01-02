using UnityEngine;

namespace InventorySystem
{
    public enum MeleeAttackType { Slash, Thrust, Overhead }

    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/Weapon - Melee")]
    public class Item_MeleeWeapon : Item_Weapon
    {
        [SerializeField, Range(-1f, 1f)] float minBlockChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxBlockChanceModifier;

        [Header("Default Attack Type")]
        [SerializeField] MeleeAttackType defaultMeleeAttackType;

        void OnEnable()
        {
            if (initialized == false)
            {
                if (IsTwoHanded)
                    equipSlot = EquipSlot.LeftHeldItem1;
                else
                    equipSlot = EquipSlot.RightHeldItem1;

                initialized = true;
            }
        }

        public float MinBlockChanceModifier => minBlockChanceModifier;
        public float MaxBlockChanceModifier => maxBlockChanceModifier;

        public MeleeAttackType DefaultMeleeAttackType => defaultMeleeAttackType;
    }
}
