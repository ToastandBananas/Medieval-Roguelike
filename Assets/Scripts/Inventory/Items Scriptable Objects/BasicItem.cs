using UnityEngine;

[CreateAssetMenu(fileName = "New Basic Item", menuName = "Inventory/Item/BasicItem")]
public class BasicItem : Item
{
    public override bool IsBag() => false;

    public override bool IsConsumable() => false;

    public override bool IsEquipment() => false;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => false;

    public override bool IsPortableContainer() => false;

    public override bool IsRangedWeapon() => false;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => false;

    public override bool IsWeapon() => false;

    public override bool IsWearable() => false;
}