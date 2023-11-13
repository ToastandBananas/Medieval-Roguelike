using UnityEngine;

namespace InventorySystem
{
    public enum WeaponType { Bow, Crossbow, Throwing, Dagger, Sword, Axe, Mace, WarHammer, Spear, Polearm }

    public abstract class Weapon : HeldEquipment
    {
        [Header("Weapon Info")]
        [SerializeField] WeaponType weaponType = WeaponType.Sword;
        [SerializeField] bool isTwoHanded;
        [SerializeField] bool isVersatile;
        [SerializeField] bool canDualWield;

        [Header("Weapon Stats")]
        [SerializeField] Vector2 attackRange;
        [SerializeField] Vector2Int damageRange;

        [Header("Modifiers")]
        [SerializeField, Range(-1f, 1f)] float minBlockChanceAddOn;
        [SerializeField, Range(-1f, 1f)] float maxBlockChanceAddOn;
        [SerializeField, Range(-1f, 1f)] float minAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float maxAccuracyModifier;

        public WeaponType WeaponType => weaponType;
        public bool IsTwoHanded => isTwoHanded;
        public bool IsVersatile => isVersatile;
        public bool CanDualWield => canDualWield;

        public float MinRange => attackRange.x;
        public float MaxRange => attackRange.y;

        public int MinDamage => damageRange.x;
        public int MaxDamage => damageRange.y;

        public float MinBlockChanceAddOn => minBlockChanceAddOn;
        public float MaxBlockChanceAddOn => maxBlockChanceAddOn;
        public float MinAccuracyModifier => minAccuracyModifier;
        public float MaxAccuracyModifier => maxAccuracyModifier;
    }
}
