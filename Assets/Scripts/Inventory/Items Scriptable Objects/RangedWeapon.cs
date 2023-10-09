using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Ranged Weapon", menuName = "Inventory/Weapon - Ranged")]
    public class RangedWeapon : Weapon
    {
        [Header("Ranged Weapon Info")]
        [SerializeField] ProjectileType projectileType;

        public ProjectileType ProjectileType => projectileType;
    }
}
