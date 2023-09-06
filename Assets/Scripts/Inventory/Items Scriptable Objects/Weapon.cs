using UnityEngine;

public enum WeaponType { Bow, Crossbow, Throwing, Dagger, Sword, Axe, Mace, WarHammer, Spear, Polearm }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Item/Weapon")]
public class Weapon : Equipment
{
    [Header("Weapon Info")]
    public WeaponType weaponType = WeaponType.Sword;
    public bool isTwoHanded;
    public bool canDualWield;

    [Header("Range")]
    public float minRange = 1f;
    public float maxRange = 1.4f;

    [Header("Damage")]
    public int minDamage = 1;
    public int maxDamage = 5;

    [Header("Modifiers")]
    public float blockChanceAddOn;
    [Range(-100f, 100f)] public float minAccuracyModifier;
    [Range(-100f, 100f)] public float maxAccuracyModifier;

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
