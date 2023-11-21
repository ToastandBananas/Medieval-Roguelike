using UnityEngine;

namespace InventorySystem
{
    public enum WeaponType { Bow, Crossbow, Throwing, Dagger, Sword, Axe, Mace, WarHammer, Spear, Polearm }

    public abstract class Weapon : HeldEquipment
    {
        public static float dualWieldPrimaryEfficiency = 0.8f;
        public static float dualWieldSecondaryEfficiency = 0.6f;

        [Header("Weapon Info")]
        [SerializeField] WeaponType weaponType = WeaponType.Sword;
        [SerializeField] bool isTwoHanded;
        [SerializeField] bool isVersatile;
        [SerializeField] bool canDualWield;

        [Header("Weapon Stats")]
        [SerializeField] Vector2 attackRange = Vector2.one;
        [SerializeField] Vector2Int damageRange = Vector2Int.one;

        [Header("Modifiers")]
        [SerializeField, Range(-1f, 1f)] float minAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float maxAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float minBlockChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxBlockChanceModifier;
        [SerializeField, Range(-1f, 1f)] float minKnockbackModifier;
        [SerializeField, Range(-1f, 1f)] float maxKnockbackModifier;

        public WeaponType WeaponType => weaponType;
        public bool IsTwoHanded => isTwoHanded;
        public bool IsVersatile => isVersatile;
        public bool CanDualWield => canDualWield;

        public float MinRange => attackRange.x;
        public float MaxRange => attackRange.y;

        public int MinDamage => damageRange.x;
        public int MaxDamage => damageRange.y;

        public float MinAccuracyModifier => minAccuracyModifier;
        public float MaxAccuracyModifier => maxAccuracyModifier;
        public float MinBlockChanceModifier => minBlockChanceModifier;
        public float MaxBlockChanceModifier => maxBlockChanceModifier;
        public float MinKnockbackModifier => minKnockbackModifier;
        public float MaxKnockbackModifier => maxKnockbackModifier;
    }
}
