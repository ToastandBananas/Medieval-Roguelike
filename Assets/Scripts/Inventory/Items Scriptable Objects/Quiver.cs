using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Quiver", menuName = "Inventory/Quiver")]
    public class Quiver : WearableContainer
    {
        [SerializeField] Sprite equippedSprite;
        [SerializeField] ProjectileType allowedProjectileType;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Quiver;
                initialized = true;
            }
        }

        public Sprite EquippedSprite => equippedSprite;
        public ProjectileType AllowedProjectileType => allowedProjectileType;
    }
}
