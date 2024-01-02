using UnityEngine;

namespace InventorySystem
{
    public enum WeaponType { Bow, Crossbow, ThrowingWeapon, Dagger, Sword, Axe, Mace, WarHammer, Spear, Polearm }

    public abstract class Item_Weapon : Item_HeldEquipment
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
        [SerializeField] Vector2Int durabilityRange = Vector2Int.one;
        [SerializeField] Vector2Int minDamageRange = Vector2Int.one;
        [SerializeField] Vector2Int maxDamageRange = Vector2Int.one;

        [Header("Effectiveness Against Armor")]
        [SerializeField, Range(0f, 1f)] float minArmorPierce;
        [SerializeField, Range(0f, 1f)] float maxArmorPierce;
        [SerializeField, Range(0f, 5f)] float minArmorEffectiveness;
        [SerializeField, Range(0f, 5f)] float maxArmorEffectiveness;

        [Header("Modifiers")]
        [SerializeField, Range(-1f, 1f)] float minAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float maxAccuracyModifier;
        [SerializeField, Range(-1f, 1f)] float minFumbleChanceModifier;
        [SerializeField, Range(-1f, 1f)] float maxFumbleChanceModifier;
        [SerializeField, Range(-1f, 1f)] float minKnockbackModifier;
        [SerializeField, Range(-1f, 1f)] float maxKnockbackModifier;

        public WeaponType WeaponType => weaponType;
        public bool IsTwoHanded => isTwoHanded;
        public bool IsVersatile => isVersatile;
        public bool CanDualWield => canDualWield;

        public int MinDurability => durabilityRange.x;
        public int MaxDurability => durabilityRange.y;

        public float MinRange => attackRange.x;
        public float MaxRange => attackRange.y;

        public int MinMinimumDamage => minDamageRange.x;
        public int MaxMinimumDamage => minDamageRange.y;
        public int MinMaximumDamage => maxDamageRange.x;
        public int MaxMaximumDamage => maxDamageRange.y;

        public float MinArmorPierce => minArmorPierce;
        public float MaxArmorPierce => maxArmorPierce;
        public float MinArmorEffectiveness => minArmorEffectiveness;
        public float MaxArmorEffectiveness => maxArmorEffectiveness;

        public float MinAccuracyModifier => minAccuracyModifier;
        public float MaxAccuracyModifier => maxAccuracyModifier;
        public float MinFumbleChanceModifier => minFumbleChanceModifier;
        public float MaxFumbleChanceModifier => maxFumbleChanceModifier;
        public float MinKnockbackModifier => minKnockbackModifier;
        public float MaxKnockbackModifier => maxKnockbackModifier;
    }
}
