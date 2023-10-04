using UnityEngine;

[CreateAssetMenu(fileName = "New Quiver", menuName = "Inventory/Quiver")]
public class Quiver : Wearable
{
    [Header("Inventory")]
    [SerializeField] Sprite equippedSprite;
    [SerializeField] ProjectileType allowedProjectileType;
    [SerializeField] InventoryLayout[] inventorySections = new InventoryLayout[1];

    public Sprite EquippedSprite => equippedSprite;
    public InventoryLayout[] InventorySections => inventorySections;
    public ProjectileType AllowedProjectileType => allowedProjectileType;
}
