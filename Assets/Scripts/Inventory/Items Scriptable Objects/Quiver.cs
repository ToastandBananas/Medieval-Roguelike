using UnityEngine;

[CreateAssetMenu(fileName = "New Quiver", menuName = "Inventory/Quiver")]
public class Quiver : Wearable
{
    [SerializeField] ProjectileType allowedProjectileType;

    [SerializeField] InventoryLayout[] inventorySections = new InventoryLayout[1];

    public InventoryLayout[] InventorySections => inventorySections;

    public ProjectileType AllowedProjectileType => allowedProjectileType;

    public override bool IsBag() => false;

    public override bool IsConsumable() => false;

    public override bool IsEquipment() => true;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => false;

    public override bool IsPortableContainer() => true;

    public override bool IsRangedWeapon() => false;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => false;

    public override bool IsWeapon() => false;

    public override bool IsWearable() => false;
}
