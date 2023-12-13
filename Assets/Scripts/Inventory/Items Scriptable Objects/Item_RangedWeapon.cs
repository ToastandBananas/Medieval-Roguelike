using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Ranged Weapon", menuName = "Inventory/Weapon - Ranged")]
    public class Item_RangedWeapon : Item_Weapon
    {
        [Header("Ranged Weapon Info")]
        [SerializeField] float reloadActionPointCostMultiplier = 1f;
        [SerializeField] ProjectileType projectileType;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.LeftHeldItem1;
                initialized = true;
            }
        }

        public float ReloadActionPointCostMultiplier => reloadActionPointCostMultiplier;
        public ProjectileType ProjectileType => projectileType;
    }
}
