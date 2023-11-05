using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Inventory/Weapon - Melee")]
    public class MeleeWeapon : Weapon
    {
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
    }
}
