using UnityEngine;
using ActionSystem;
using InventorySystem;
using WorldSystem;
using Utilities;

namespace UnitSystem
{
    public class Stats : MonoBehaviour
    {
        public int currentAP { get; private set; }
        public int pooledAP { get; private set; }
        public int APUntilTimeTick { get; private set; }
        public int lastUsedAP { get; private set; }
        readonly int baseAP_PerSecond = 60;

        public int currentEnergy { get; private set; }
        readonly int baseEnergy = 20;
        readonly float energyRegenPerTurn = 0.25f;
        float energyRegenBuildup;

        [Header("Attributes")]
        [SerializeField] IntStat endurance;
        [SerializeField] IntStat speed;
        [SerializeField] IntStat strength;

        [Header("Skills")]
        [SerializeField] IntStat axeSkill;
        [SerializeField] IntStat bowSkill;
        [SerializeField] IntStat crossbowSkill;
        [SerializeField] IntStat daggerSkill;
        [SerializeField] IntStat maceSkill;
        [SerializeField] IntStat polearmSkill;
        [SerializeField] IntStat shieldSkill;
        [SerializeField] IntStat spearSkill;
        [SerializeField] IntStat swordSkill;
        [SerializeField] IntStat throwingSkill;
        [SerializeField] IntStat warHammerSkill;

        [Header("Unarmed Combat")]
        [SerializeField] bool canFightUnarmed = true;
        [SerializeField] float unarmedAttackRange = 1.4f;
        [SerializeField] int baseUnarmedDamage = 5;

        Unit unit;

        void Awake()
        {
            unit = GetComponent<Unit>();

            APUntilTimeTick = MaxAP();
            currentEnergy = MaxEnergy();
        }

        #region AP
        public void SetLastUsedAP(int amountUsed)
        {
            lastUsedAP = amountUsed;
            ActionSystemUI.UpdateActionPointsText();
        }

        public int MaxAP() => Mathf.RoundToInt((baseAP_PerSecond * TimeSystem.defaultTimeTickInSeconds) + (speed.GetValue() * 5f));

        public void UseAP(int amount)
        {
            if (amount <= 0)
                return;

            if (unit.IsPlayer)
            {
                UpdateAPUntilTimeTick(amount);
                SetLastUsedAP(amount);

                for (int i = 0; i < UnitManager.livingNPCs.Count; i++)
                {
                    // Every time the Player takes an action that costs AP, a correlating amount of AP is added to each NPCs AP pool (based off percentage of the Player's MaxAP used)
                    UnitManager.livingNPCs[i].stats.AddToAPPool(Mathf.RoundToInt(UnitManager.livingNPCs[i].stats.APUsedMultiplier(amount) * UnitManager.livingNPCs[i].stats.MaxAP()));

                    // Each NPCs move speed is set, based on how many moves they could potentially make with their pooled AP (to prevent staggered movements, slowing down the flow of the game)
                    UnitManager.livingNPCs[i].unitActionHandler.GetAction<MoveAction>().SetMoveSpeed(amount);
                }
            }
            else
            {
                // Debug.Log("AP used: " + amount);
                currentAP -= amount;

                if (currentAP < 0)
                {
                    Debug.LogWarning($"Trying to use more AP than {unit.name} has...");
                    currentAP = 0;
                }

                UpdateAPUntilTimeTick(amount);
            }
        }

        public void UpdateAPUntilTimeTick(int amountAPUsed)
        {
            if (amountAPUsed < APUntilTimeTick)
                APUntilTimeTick -= amountAPUsed;
            else
            {
                amountAPUsed -= APUntilTimeTick;
                APUntilTimeTick = MaxAP();
                UpdateUnit();
                UpdateAPUntilTimeTick(amountAPUsed);
            }
        }

        public void ReplenishAP() => currentAP = MaxAP();

        public void AddToCurrentAP(int amountToAdd) => currentAP += amountToAdd;

        public void AddToAPPool(int amountToAdd) => pooledAP += amountToAdd;

        public void GetAPFromPool()
        {
            int APDifference = MaxAP() - currentAP;
            if (pooledAP > APDifference)
            {
                pooledAP -= APDifference;
                currentAP += APDifference;
            }
            else
            {
                currentAP += pooledAP;
                pooledAP = 0;
            }
        }

        public int UseAPAndGetRemainder(int amount)
        {
            // Debug.Log("Current AP: " + currentAP);
            int remainingAmount;
            if (currentAP >= amount)
            {
                UseAP(amount);
                remainingAmount = 0;
            }
            else
            {
                remainingAmount = amount - currentAP;
                UseAP(currentAP);
            }

            //Debug.Log("Remaining amount: " + remainingAmount);
            return remainingAmount;
        }

        public float APUsedMultiplier(int amountAPUsed) => ((float)amountAPUsed) / MaxAP();
        #endregion

        #region Energy
        public int MaxEnergy() => Mathf.RoundToInt(baseEnergy + (endurance.GetValue() * 10f));

        public void UseEnergy(int amount)
        {
            if (amount <= 0)
                return;

            currentEnergy -= amount;
            if (currentEnergy < 0)
            {
                Debug.LogWarning($"Trying to use more energy than {unit.name} has...");
                currentEnergy = 0;
            }

            if (unit.IsPlayer)
                ActionSystemUI.UpdateEnergyText();
        }

        public void ReplenishEnergy()
        {
            currentEnergy = MaxEnergy();

            if (unit.IsPlayer)
                ActionSystemUI.UpdateEnergyText();
        }

        public void AddToCurrentEnergy(int amountToAdd)
        {
            currentEnergy += amountToAdd;
            if (currentEnergy > MaxEnergy())
                currentEnergy = MaxEnergy();

            if (unit.IsPlayer)
                ActionSystemUI.UpdateEnergyText();
        }

        void RegenerateEnergy()
        {
            energyRegenBuildup += energyRegenPerTurn;
            if (energyRegenBuildup >= 1f)
            {
                int amountToAdd = Mathf.FloorToInt(energyRegenBuildup);
                AddToCurrentEnergy(amountToAdd);
                energyRegenBuildup -= amountToAdd;
            }
        }

        public bool HasEnoughEnergy(int energyCost) => currentEnergy >= energyCost;
        #endregion

        void UpdateUnit()
        {
            if (unit.IsPlayer)
                TimeSystem.IncreaseTime();

            unit.vision.UpdateVision();

            // We already are running this after the Move and Turn actions are complete, so no need to run it again
            if (unit.unitActionHandler.lastQueuedAction is MoveAction == false && unit.unitActionHandler.lastQueuedAction is TurnAction == false)
                unit.vision.FindVisibleUnitsAndObjects();

            RegenerateEnergy();

            /*
            unit.status.UpdateBuffs();
            unit.status.UpdateInjuries();
            unit.status.RegenerateStamina();
            unit.nutrition.DrainStaminaBonus();

            if (unit.unitActionHandler.canPerformActions || unit.IsPlayer)
            {
                unit.nutrition.DrainNourishment();
                unit.nutrition.DrainWater();
                unit.nutrition.DrainNausea();
            }
            */
        }

        public IntStat Speed() => speed;

        public IntStat Strength() => strength;

        public IntStat AxeSkill() => axeSkill;

        public IntStat BowSkill() => bowSkill;

        public IntStat CrossbowSkill() => crossbowSkill;

        public IntStat DaggerSkill() => daggerSkill;

        public IntStat MaceSkill() => maceSkill;

        public IntStat PolearmSkill() => polearmSkill;

        public IntStat ShieldSkill() => shieldSkill;

        public IntStat SpearSkill() => spearSkill;

        public IntStat SwordSkill() => swordSkill;

        public IntStat ThrowingSkill() => throwingSkill;

        public IntStat WarHammerSkill() => warHammerSkill;

        public int NaturalBlockPower() => Mathf.RoundToInt(strength.GetValue() * 2.5f);

        public float ShieldBlockChance(HeldShield heldShield, bool attackerBesideUnit)
        {
            float blockChance = shieldSkill.GetValue() * 2f;
            blockChance = Mathf.RoundToInt((blockChance + heldShield.ItemData.Item.Shield.BlockChanceAddOn) * 100f) / 100f;

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                blockChance *= 0.5f;

            if (blockChance < 0f) blockChance = 0f;
            return blockChance;
        }

        public float WeaponBlockChance(HeldMeleeWeapon heldWeapon, bool attackerBesideUnit, bool shieldEquipped)
        {
            Weapon weapon = heldWeapon.ItemData.Item.Weapon;
            float blockChance = swordSkill.GetValue() * 2f * WeaponBlockModifier(weapon);
            blockChance = Mathf.RoundToInt((blockChance + weapon.BlockChanceAddOn) * 100f) / 100f;

            if (shieldEquipped)
                blockChance *= 0.65f;

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                blockChance *= 0.5f;

            if (blockChance < 0f) blockChance = 0f;
            return blockChance;
        }

        public int ShieldBlockPower(HeldShield heldShield) => NaturalBlockPower() + (ShieldSkill().GetValue() * 2) + heldShield.ItemData.BlockPower;

        public int WeaponBlockPower(HeldMeleeWeapon heldWeapon)
        {
            Weapon weapon = heldWeapon.ItemData.Item.Weapon;
            return Mathf.RoundToInt((NaturalBlockPower() + (WeaponSkill(weapon.WeaponType) * 2)) * WeaponBlockModifier(weapon));
        }

        public float RangedAccuracy(ItemData rangedWeaponItemData)
        {
            float accuracy = 0f;
            if (rangedWeaponItemData.Item.Weapon.WeaponType == WeaponType.Bow)
            {
                accuracy = bowSkill.GetValue() * 4f;
                accuracy = Mathf.RoundToInt((accuracy + rangedWeaponItemData.AccuracyModifier) * 100f) / 100f;
            }

            if (accuracy < 0f) accuracy = 0f;
            return accuracy;
        }

        public int WeaponSkill(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Bow:
                    return bowSkill.GetValue();
                case WeaponType.Crossbow:
                    return crossbowSkill.GetValue();
                case WeaponType.Throwing:
                    return throwingSkill.GetValue();
                case WeaponType.Dagger:
                    return daggerSkill.GetValue();
                case WeaponType.Sword:
                    return swordSkill.GetValue();
                case WeaponType.Axe:
                    return axeSkill.GetValue();
                case WeaponType.Mace:
                    return maceSkill.GetValue();
                case WeaponType.WarHammer:
                    return warHammerSkill.GetValue();
                case WeaponType.Spear:
                    return spearSkill.GetValue();
                case WeaponType.Polearm:
                    return polearmSkill.GetValue();
                default:
                    Debug.LogError(weaponType.ToString() + " has not been implemented in this method. Fix me!");
                    return 0;
            }
        }

        public float WeaponBlockModifier(Weapon weapon)
        {
            switch (weapon.WeaponType)
            {
                case WeaponType.Throwing:
                    return 0.2f;
                case WeaponType.Dagger:
                    return 0.4f;
                case WeaponType.Sword:
                    if (weapon.IsTwoHanded)
                        return 1.45f;
                    return 1.3f;
                case WeaponType.Axe:
                    if (weapon.IsTwoHanded)
                        return 0.9f;
                    return 0.8f;
                case WeaponType.Mace:
                    if (weapon.IsTwoHanded)
                        return 1.2f;
                    return 1.1f;
                case WeaponType.WarHammer:
                    if (weapon.IsTwoHanded)
                        return 0.8f;
                    return 0.65f;
                case WeaponType.Spear:
                    return 1f;
                case WeaponType.Polearm:
                    return 1.1f;
                default:
                    return 0f;
            }
        }

        public bool CanFightUnarmed => canFightUnarmed;

        public float UnarmedAttackRange => unarmedAttackRange;

        public int BaseUnarmedDamage => baseUnarmedDamage;
    }
}
