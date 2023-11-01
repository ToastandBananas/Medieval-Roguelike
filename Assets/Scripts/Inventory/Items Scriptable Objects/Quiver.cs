using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Quiver", menuName = "Inventory/Quiver")]
    public class Quiver : WearableContainer
    {
        [SerializeField] Sprite equippedSprite;
        [SerializeField] ProjectileType allowedProjectileType;

        public Sprite EquippedSprite => equippedSprite;
        public ProjectileType AllowedProjectileType => allowedProjectileType;
    }
}
