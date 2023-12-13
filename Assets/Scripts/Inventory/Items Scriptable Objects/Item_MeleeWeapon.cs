using UnityEngine;

namespace InventorySystem
{
    public enum MeleeAttackType { Slash, Thrust, Overhead }

    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/Weapon - Melee")]
    public class Item_MeleeWeapon : Item_Weapon
    {
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

        public MeleeAttackType DefaultMeleeAttackType => defaultMeleeAttackType;
    }
}
