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
}
