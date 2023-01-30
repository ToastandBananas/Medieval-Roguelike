using UnityEngine;

public abstract class Equipment : Item
{
    public abstract override bool IsBag();

    public abstract override bool IsConsumable();

    public abstract override bool IsEquipment();

    public abstract override bool IsKey();

    public abstract override bool IsMedicalSupply();

    public abstract override bool IsPortableContainer();

    public abstract override bool IsShield();

    public abstract override bool IsMeleeWeapon();

    public abstract override bool IsRangedWeapon();

    public abstract override bool IsWeapon();

    public abstract override bool IsWearable();
}
