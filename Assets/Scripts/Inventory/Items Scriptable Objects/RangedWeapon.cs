using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Ranged Weapon", menuName = "Inventory/Weapon - Ranged")]
    public class RangedWeapon : Weapon
    {
        [Header("Ranged Weapon Info")]
        [SerializeField] float reloadActionPointCostMultiplier = 1f;
        [SerializeField] ProjectileType projectileType;

        public float ReloadActionPointCostMultiplier => reloadActionPointCostMultiplier;
        public ProjectileType ProjectileType => projectileType;
    }
}
