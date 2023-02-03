using UnityEngine;

[CreateAssetMenu(fileName = "New Shield", menuName = "Inventory/Item/Shield")]
public class Shield : Equipment
{
    public override bool IsBag() => false;

    public override bool IsConsumable() => false;

    public override bool IsEquipment() => true;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => false;

    public override bool IsPortableContainer() => false;

    public override bool IsRangedWeapon() => false;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => true;

    public override bool IsWeapon() => false;

    public override bool IsWearable() => false;
}