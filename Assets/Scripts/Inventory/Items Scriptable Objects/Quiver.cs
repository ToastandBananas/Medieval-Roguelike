using UnityEngine;

[CreateAssetMenu(fileName = "New Quiver", menuName = "Inventory/Item/Quiver")]
public class Quiver : Equipment
{
    [SerializeField] ProjectileType allowedProjectileType;
    [SerializeField] int ammoStackCount = 2;

    public int AmmoStackCount => ammoStackCount;

    public ProjectileType AllowedProjectileType => allowedProjectileType;

    public override bool IsBackpack() => false;

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
