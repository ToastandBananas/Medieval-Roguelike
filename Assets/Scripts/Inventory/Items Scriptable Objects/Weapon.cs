using UnityEngine;

public enum WeaponType { Bow, Crossbow, Throwing, Dagger, Sword, Axe, Mace, WarHammer, Spear, Polearm }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class Weapon : HeldEquipment
{
    [Header("Weapon Info")]
    [SerializeField] WeaponType weaponType = WeaponType.Sword;
    [SerializeField] bool isTwoHanded;
    [SerializeField] bool canDualWield;

    [Header("Range")]
    [SerializeField] float minRange = 1f;
    [SerializeField] float maxRange = 1.4f;

    [Header("Damage")]
    [SerializeField] int minDamage = 1;
    [SerializeField] int maxDamage = 5;

    [Header("Modifiers")]
    [SerializeField] float blockChanceAddOn;
    [Range(-100f, 100f)][SerializeField] float minAccuracyModifier;
    [Range(-100f, 100f)][SerializeField] float maxAccuracyModifier;

    public WeaponType WeaponType => weaponType;
    public bool IsTwoHanded => isTwoHanded;
    public bool CanDualWield => canDualWield;

    public float MinRange => minRange;
    public float MaxRange => maxRange;

    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;

    public float BlockChanceAddOn => blockChanceAddOn;
    public float MinAccuracyModifier => minAccuracyModifier;
    public float MaxAccuracyModifier => maxAccuracyModifier;

    public override bool IsBag() => false;

    public override bool IsConsumable() => false;

    public override bool IsEquipment() => true;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => itemType == ItemType.MeleeWeapon;

    public override bool IsPortableContainer() => false;

    public override bool IsRangedWeapon() => itemType == ItemType.Bow;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => false;

    public override bool IsWeapon() => true;

    public override bool IsWearable() => false;
}
