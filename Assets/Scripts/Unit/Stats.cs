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
        // readonly int baseAP_PerSecond = 60;

        public int currentEnergy { get; private set; }
        readonly int baseEnergy = 20;
        readonly float energyRegenPerTurn = 0.25f;
        float energyRegenBuildup;

        public float currentCarryWeight { get; private set; }

        [SerializeField] Unit unit;

        [Header("Attributes")]
        [SerializeField] IntStat agility;
        [SerializeField] IntStat endurance;
        [SerializeField] IntStat speed;
        [SerializeField] IntStat strength;

        [Header("Weapon Skills")]
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
        [SerializeField] IntStat unarmedSkill;
        [SerializeField] IntStat warHammerSkill;

        [Header("Unarmed Combat")]
        [SerializeField] bool canFightUnarmed = true;
        [SerializeField] float unarmedAttackRange = 1.4f;
        [SerializeField] int baseUnarmedDamage = 5;

        readonly float maxBlockChance = 0.85f;
        readonly float maxDodgeChance = 0.85f;

        void Awake()
        {
            APUntilTimeTick = MaxAP();
            currentEnergy = MaxEnergy();
        }

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

        #region AP
        public void SetLastUsedAP(int amountUsed)
        {
            lastUsedAP = amountUsed;
            ActionSystemUI.UpdateActionPointsText();
        }

        public int MaxAP() => Mathf.RoundToInt(speed.GetValue() * 3f);

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
                    UnitManager.livingNPCs[i].stats.AddToAPPool(Mathf.RoundToInt((float)APUsedMultiplier(amount) * UnitManager.livingNPCs[i].stats.MaxAP()));

                    // Each NPCs move speed is set, based on how many moves they could potentially make with their pooled AP (to prevent staggered movements, slowing down the flow of the game)
                    UnitManager.livingNPCs[i].unitActionHandler.moveAction.SetMoveSpeed(amount);
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

        #region Blocking
        public int NaturalBlockPower => Mathf.RoundToInt(strength.GetValue() * 2.5f);

        public float ShieldBlockChance(HeldShield heldShield, Unit attackingUnit, Weapon weaponAttackingWith, bool attackerUsingOffhand, bool attackerBesideUnit)
        {
            float blockChance = shieldSkill.GetValue() *  0.75f;
            blockChance += blockChance * heldShield.itemData.BlockChanceAddOn / 100f;

            // Block chance is reduced depending on the attacker's weapon skill
            blockChance *= EnemyWeaponSkillBlockChanceModifier(attackingUnit, weaponAttackingWith, attackerUsingOffhand);

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                blockChance *= 0.5f;

            if (blockChance < 0f) 
                blockChance = 0f;
            else if (blockChance > maxBlockChance) 
                blockChance = maxBlockChance;

            //Debug.Log($"{unit.name}'s Shield Block Chance: " + blockChance);
            return blockChance;
        }

        public float WeaponBlockChance(HeldMeleeWeapon heldWeapon, Unit attackingUnit, Weapon weaponAttackingWith, bool attackerUsingOffhand, bool attackerBesideUnit, bool shieldEquipped)
        {
            Weapon weapon = heldWeapon.itemData.Item.Weapon;
            float blockChance = swordSkill.GetValue() * 0.6f * WeaponBlockModifier(weapon);
            blockChance += blockChance * weapon.BlockChanceAddOn / 100f;

            // Block chance is reduced depending on the attacker's weapon skill
            blockChance *= EnemyWeaponSkillBlockChanceModifier(attackingUnit, weaponAttackingWith, attackerUsingOffhand);

            if (shieldEquipped)
                blockChance *= 0.65f;

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                blockChance *= 0.5f;

            if (blockChance < 0f) 
                blockChance = 0f;
            else if (blockChance > maxBlockChance) 
                blockChance = maxBlockChance;

            //Debug.Log($"{unit.name}'s Weapon Block Chance: " + blockChance);
            return blockChance;
        }

        public int ShieldBlockPower(HeldShield heldShield) => NaturalBlockPower + Mathf.RoundToInt(shieldSkill.GetValue() * 0.5f) + heldShield.itemData.BlockPower;

        public int WeaponBlockPower(HeldMeleeWeapon heldWeapon)
        {
            Weapon weapon = heldWeapon.itemData.Item as Weapon;
            return Mathf.RoundToInt((NaturalBlockPower + Mathf.RoundToInt(WeaponSkill(weapon) * 0.5f)) * WeaponBlockModifier(weapon));
        }

        public float WeaponBlockModifier(Weapon weapon)
        {
            if (weapon == null)
                return 0f;

            switch (weapon.WeaponType)
            {
                case WeaponType.Throwing:
                    return 0.2f;
                case WeaponType.Dagger:
                    return 0.4f;
                case WeaponType.Sword:
                    if (weapon.IsTwoHanded)
                        return 1.4f;
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
                        return 0.75f;
                    return 0.65f;
                case WeaponType.Spear:
                    return 1f;
                case WeaponType.Polearm:
                    return 1.1f;
                default:
                    return 0f;
            }
        }

        float EnemyWeaponSkillBlockChanceModifier(Unit attackingUnit, Weapon weaponAttackingWith, bool attackerUsingOffhand)
        {
            float modifier = 1f - (attackingUnit.stats.WeaponSkill(weaponAttackingWith) / 100f * 0.4f);

            // Weapon skill effectiveness is reduced when dual wielding
            if (attackingUnit.UnitEquipment != null && attackingUnit.UnitEquipment.IsDualWielding())
            {
                if (attackerUsingOffhand)
                    modifier += modifier * (1f - GameManager.dualWieldSecondaryEfficiency);
                else
                    modifier += modifier * (1f - GameManager.dualWieldPrimaryEfficiency);
            }
            // Debug.Log("Enemy Weapon Skill Block Modifier: " + modifier);
            return modifier;
        }
        #endregion

        #region Carry Weight
        public float MaxCarryWeight => strength.GetValue() * 5f;

        public void AdjustCarryWeight(float amount) => currentCarryWeight += Mathf.RoundToInt(amount * 100f) / 100f;

        public void UpdateCarryWeight()
        {
            currentCarryWeight = 0f;
            if (unit.UnitInventoryManager != null)
                AdjustCarryWeight(unit.UnitInventoryManager.GetTotalInventoryWeight());

            if (unit.UnitEquipment != null)
                AdjustCarryWeight(unit.UnitEquipment.GetTotalEquipmentWeight());

            if (CarryWeightRatio() >= 2f)
                unit.unitActionHandler.moveAction.SetCanMove(false);
            else
                unit.unitActionHandler.moveAction.SetCanMove(true);

            if (unit.IsPlayer)
                InventoryUI.UpdatePlayerCarryWeightText();
        }

        public float CarryWeightRatio() => currentCarryWeight / MaxCarryWeight;

        public float EncumbranceMoveCostModifier()
        {
            float carryWeightRatio = CarryWeightRatio();
            if (carryWeightRatio <= 0.5f)
                return 1f;
            else
            {
                if (carryWeightRatio > 2f)
                    carryWeightRatio = 2f;

                return 1f + ((carryWeightRatio * 1.5f) - 0.5f);
            }
        }

        float EncumbranceDodgeChanceMultiplier()
        {
            float carryWeightRatio = CarryWeightRatio();
            if (carryWeightRatio <= 0.5f)
                return 1f;
            else
            {
                if (carryWeightRatio > 2f)
                    carryWeightRatio = 2f;

                // Calculate the dodge chance multiplier as a linear reduction
                if (carryWeightRatio <= 1f)
                    return 1f - (carryWeightRatio * 0.33f);
                else
                    return 1f - (carryWeightRatio * 0.5f);
            }
        }
        #endregion

        #region Dodging
        public float DodgeChance(Unit attackingUnit, Weapon weaponAttackingWith, bool attackerUsingOffhand, bool attackerBesideUnit)
        {
            float dodgeChance = 0f;
            if (CarryWeightRatio() < 2f)
                dodgeChance = agility.GetValue() / 1.35f * EncumbranceDodgeChanceMultiplier() * EnemyWeaponSkillDodgeChanceModifier(attackingUnit, weaponAttackingWith, attackerUsingOffhand);

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                dodgeChance *= 0.5f;

            if (dodgeChance < 0f) 
                dodgeChance = 0f;
            else if (dodgeChance > maxDodgeChance) 
                dodgeChance = maxDodgeChance;

            // Debug.Log(unit.name + "'s Dodge Chance: " + dodgeChance);
            return dodgeChance;
        }

        float EnemyWeaponSkillDodgeChanceModifier(Unit attackingUnit, Weapon weaponAttackingWith, bool attackerUsingOffhand)
        {
            float modifier = 1f - (attackingUnit.stats.WeaponSkill(weaponAttackingWith) / 100f * 0.5f);

            // Weapon skill effectiveness is reduced when dual wielding
            if (attackingUnit.UnitEquipment != null && attackingUnit.UnitEquipment.IsDualWielding())
            {
                if (attackerUsingOffhand)
                    modifier += modifier * (1f - GameManager.dualWieldSecondaryEfficiency);
                else
                    modifier += modifier * (1f - GameManager.dualWieldPrimaryEfficiency);
            }
            
            return modifier;
        }
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

        public int WeaponSkill(Weapon weapon)
        {
            if (weapon == null) // Unarmed
                return unarmedSkill.GetValue();

            switch (weapon.WeaponType)
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
                    Debug.LogError(weapon.WeaponType.ToString() + " has not been implemented in this method. Fix me!");
                    return 0;
            }
        }

        public IntStat Agility => agility;
        public IntStat Speed => speed;
        public IntStat Strength => strength;

        public IntStat AxeSkill => axeSkill;
        public IntStat BowSkill => bowSkill;
        public IntStat CrossbowSkill => crossbowSkill;
        public IntStat DaggerSkill => daggerSkill;
        public IntStat MaceSkill => maceSkill;
        public IntStat PolearmSkill => polearmSkill;
        public IntStat ShieldSkill => shieldSkill;
        public IntStat SpearSkill => spearSkill;
        public IntStat SwordSkill => swordSkill;
        public IntStat ThrowingSkill => throwingSkill;
        public IntStat UnarmedSkill => unarmedSkill;
        public IntStat WarHammerSkill => warHammerSkill;

        public bool CanFightUnarmed => canFightUnarmed;
        public float UnarmedAttackRange => unarmedAttackRange;
        public int BaseUnarmedDamage => baseUnarmedDamage;
    }
}
