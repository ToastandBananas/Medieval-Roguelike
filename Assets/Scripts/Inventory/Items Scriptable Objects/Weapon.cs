using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Item/Weapon")]
public class Weapon : Equipment
{
    public int minDamage = 1;
    public int maxDamage = 5;

    public bool isOneHanded = true;

    public override bool IsBag() => false;

    public override bool IsConsumable() => false;

    public override bool IsEquipment() => true;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => itemType == ItemType.MeleeWeapon;

    public override bool IsPortableContainer() => false;

    public override bool IsRangedWeapon() => itemType == ItemType.RangedWeapon;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => false;

    public override bool IsWeapon() => true;

    public override bool IsWearable() => false;
}
