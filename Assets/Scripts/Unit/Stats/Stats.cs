using UnityEngine;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.UI;
using InventorySystem;
using WorldSystem;
using Utilities;
using GridSystem;
using System.Collections.Generic;

namespace UnitSystem
{
    public class Stats : MonoBehaviour
    {
        public delegate void OnKnockbackHandler();
        public event OnKnockbackHandler OnKnockbackTarget;
        public delegate void OnFailedKnockbackHandler(GridPosition targetUnitGridPosition);
        public event OnFailedKnockbackHandler OnFailedToKnockbackTarget;

        public int CurrentAP { get; private set; }
        public int PooledAP { get; private set; }
        public int LastPooledAP { get; private set; }
        public int APUntilTimeTick { get; private set; }
        public int LastUsedAP { get; private set; }
        // readonly int baseAP_PerSecond = 60;

        public List<BaseAction> EnergyUseActions { get; private set; }

        public int CurrentEnergy { get; private set; }
        readonly int baseEnergy = 20;
        readonly float energyRegenPerTurn = 0.25f;
        float energyRegenBuildup, energyUseBuildup;

        public float CurrentCarryWeight { get; private set; }

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
        [SerializeField] float unarmedAttackRange = 1.5f;
        [SerializeField] int baseUnarmedDamage = 5;

        readonly float maxKnockbackChance = 0.9f;
        readonly float maxBlockChance = 0.85f;
        readonly float maxDodgeChance = 0.85f;
        readonly float maxRangedAccuracy = 0.9f;
        readonly float accuracyModifierPerHeightDifference = 0.1f;

        void Awake()
        {
            EnergyUseActions = new List<BaseAction>();

            APUntilTimeTick = MaxAP();
            CurrentEnergy = MaxEnergy();
        }

        void UpdateUnit()
        {
            if (unit.IsPlayer)
                TimeSystem.IncreaseTime();

            unit.vision.UpdateVision();

            // We already are running this after the Move and Turn actions are complete, so no need to run it again
            if (unit.unitActionHandler.LastQueuedAction is MoveAction == false && unit.unitActionHandler.LastQueuedAction is TurnAction == false)
                unit.vision.FindVisibleUnitsAndObjects();

            UpdateEnergy();

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
            LastUsedAP = amountUsed;
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
                    UnitManager.livingNPCs[i].unitActionHandler.MoveAction.SetTravelDistanceSpeedMultiplier();
                }
            }
            else
            {
                // Debug.Log("AP used: " + amount);
                CurrentAP -= amount;

                if (CurrentAP < 0)
                {
                    Debug.LogWarning($"Trying to use more AP than {unit.name} has...");
                    CurrentAP = 0;
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

        public void ReplenishAP() => CurrentAP = MaxAP();

        public void AddToCurrentAP(int amountToAdd) => CurrentAP += amountToAdd;

        public void AddToAPPool(int amountToAdd)
        {
            PooledAP += amountToAdd;
            LastPooledAP = PooledAP;
        }

        public void GetAPFromPool()
        {
            int APDifference = MaxAP() - CurrentAP;
            if (PooledAP > APDifference)
            {
                PooledAP -= APDifference;
                CurrentAP += APDifference;
            }
            else
            {
                CurrentAP += PooledAP;
                PooledAP = 0;
            }
        }

        public int UseAPAndGetRemainder(int amount)
        {
            // Debug.Log("Current AP: " + currentAP);
            int remainingAmount;
            if (CurrentAP >= amount)
            {
                UseAP(amount);
                remainingAmount = 0;
            }
            else
            {
                remainingAmount = amount - CurrentAP;
                UseAP(CurrentAP);
            }

            //Debug.Log("Remaining amount: " + remainingAmount);
            return remainingAmount;
        }

        public float APUsedMultiplier(int amountAPUsed) => ((float)amountAPUsed) / MaxAP();
        #endregion

        #region Blocking
        public int NaturalBlockPower => Mathf.RoundToInt(strength.GetValue() * 0.4f);

        public float ShieldBlockChance(HeldShield heldShield, Unit attackingUnit, HeldItem weaponAttackingWith, bool attackerUsingOffhand, bool attackerBesideUnit)
        {
            float blockChance = shieldSkill.GetValue() / 100f * 0.75f;
            float baseBlockChance = blockChance;

            // Add the shield's bonus block chance
            blockChance += baseBlockChance * heldShield.ItemData.BlockChanceModifier;

            // Block chance affected by height differences between this Unit and the attackingUnit
            blockChance += baseBlockChance * accuracyModifierPerHeightDifference * TacticsUtilities.CalculateHeightDifferenceToTarget(unit.GridPosition, attackingUnit.GridPosition);

            if (heldShield.CurrentHeldItemStance == HeldItemStance.RaiseShield)
                blockChance += baseBlockChance * RaiseShieldAction.blockChanceModifier;

            // Block chance is reduced depending on the attacker's weapon skill
            blockChance *= EnemyWeaponSkill_BlockChanceModifier(attackingUnit, weaponAttackingWith, attackerUsingOffhand);

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                blockChance *= 0.5f;

            if (blockChance < 0f) 
                blockChance = 0f;
            else if (blockChance > maxBlockChance) 
                blockChance = maxBlockChance;

            // Debug.Log($"{unit.name}'s Shield Block Chance: " + blockChance);
            return blockChance;
        }

        public float WeaponBlockChance(HeldMeleeWeapon heldWeapon, Unit attackingUnit, HeldItem weaponBeingAttackedWith, bool attackerUsingOffhand, bool attackerBesideUnit, bool shieldEquipped)
        {
            Weapon weapon = heldWeapon.ItemData.Item.Weapon;
            float blockChance = swordSkill.GetValue() / 100f * 0.6f * WeaponBlockModifier(weapon);
            float baseBlockChance = blockChance;

            // Add the weapon's bonus block chance
            blockChance += baseBlockChance * heldWeapon.ItemData.BlockChanceModifier;

            // Block chance affected by height differences between this Unit and the attackingUnit
            blockChance += baseBlockChance * accuracyModifierPerHeightDifference * TacticsUtilities.CalculateHeightDifferenceToTarget(unit.GridPosition, attackingUnit.GridPosition);

            // Block chance is reduced depending on the attacker's weapon skill
            blockChance *= EnemyWeaponSkill_BlockChanceModifier(attackingUnit, weaponBeingAttackedWith, attackerUsingOffhand);

            // Block chance reduced if already wielding a shield in other hand
            if (shieldEquipped)
                blockChance *= 0.65f;

            // If attacker is directly beside the Unit
            if (attackerBesideUnit)
                blockChance *= 0.5f;

            if (blockChance < 0f) 
                blockChance = 0f;
            else if (blockChance > maxBlockChance) 
                blockChance = maxBlockChance;

            // Debug.Log($"{unit.name}'s Weapon Block Chance: " + blockChance);
            return blockChance;
        }

        public int BlockPower(HeldShield heldShield) => NaturalBlockPower + Mathf.RoundToInt(shieldSkill.GetValue() * 0.5f) + heldShield.ItemData.BlockPower;

        public int BlockPower(HeldMeleeWeapon heldWeapon)
        {
            Weapon weapon = heldWeapon.ItemData.Item as Weapon;
            return Mathf.RoundToInt((NaturalBlockPower + Mathf.RoundToInt(WeaponSkill(weapon) * 0.5f)) * WeaponBlockModifier(weapon));
        }

        public float WeaponBlockModifier(Weapon weapon)
        {
            if (weapon == null)
                return 0f;

            switch (weapon.WeaponType)
            {
                case WeaponType.ThrowingWeapon:
                    return 0.2f;
                case WeaponType.Dagger:
                    return 0.4f;
                case WeaponType.Sword:
                    if (weapon.IsTwoHanded)
                        return 1.4f;
                    return 1.3f;
                case WeaponType.Axe:
                    if (weapon.IsTwoHanded)
                        return 1f;
                    return 0.9f;
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
                    Debug.LogError(weapon.WeaponType.ToString() + " has not been implemented in this method. Fix me!");
                    return 1f;
            }
        }

        ///<summary>A lower number is worse for the Unit being attacked, as their block chance will be multiplied by this.</summary>
        float EnemyWeaponSkill_BlockChanceModifier(Unit attackingUnit, HeldItem weaponAttackingWith, bool attackerUsingOffhand)
        {
            Weapon weapon = null;
            if (weaponAttackingWith != null)
                weapon = weaponAttackingWith.ItemData.Item.Weapon;

            float modifier = 1f - (attackingUnit.stats.WeaponSkill(weapon) / 100f * 0.4f);
            float baseModifier = modifier;
            if (weaponAttackingWith != null)
                modifier -= baseModifier * weaponAttackingWith.ItemData.AccuracyModifier;

            // Weapon skill effectiveness is reduced when dual wielding
            if (attackingUnit.UnitEquipment != null && attackingUnit.UnitEquipment.IsDualWielding)
            {
                if (attackerUsingOffhand)
                    modifier += baseModifier * (1f - Weapon.dualWieldSecondaryEfficiency);
                else
                    modifier += baseModifier * (1f - Weapon.dualWieldPrimaryEfficiency);
            }

            if (modifier < 0f)
                modifier = 0f;

            // Debug.Log(attackingUnit.name + " Weapon Skill Block Modifier against " + unit.name + ": " + modifier);
            return modifier;
        }
        #endregion

        #region Carry Weight
        public float MaxCarryWeight => strength.GetValue() * 5f;

        public void AdjustCarryWeight(float amount) => CurrentCarryWeight += Mathf.RoundToInt(amount * 100f) / 100f;

        public void UpdateCarryWeight()
        {
            CurrentCarryWeight = 0f;
            if (unit.UnitInventoryManager != null)
                AdjustCarryWeight(unit.UnitInventoryManager.GetTotalInventoryWeight());

            if (unit.UnitEquipment != null)
                AdjustCarryWeight(unit.UnitEquipment.GetTotalEquipmentWeight());

            if (CarryWeightRatio >= 2f)
                unit.unitActionHandler.MoveAction.SetCanMove(false);
            else
                unit.unitActionHandler.MoveAction.SetCanMove(true);

            if (unit.IsPlayer)
                InventoryUI.UpdatePlayerCarryWeightText();
        }

        public float CarryWeightRatio => CurrentCarryWeight / MaxCarryWeight;

        public float EncumbranceMoveCostModifier()
        {
            float carryWeightRatio = CarryWeightRatio;
            if (carryWeightRatio <= 0.5f)
                return 1f;
            else
            {
                if (carryWeightRatio > 2f)
                    carryWeightRatio = 2f;

                if (carryWeightRatio <= 1f)
                    return 1f + ((carryWeightRatio * 1.5f) - 0.5f);
                else
                    return 1f + ((Mathf.Pow(carryWeightRatio, 2) * 1.5f) - 0.5f);
            }
        }
        #endregion

        #region Dodging
        public float DodgeChance(Unit attackingUnit, HeldItem weaponBeingAttackedWith, BaseAttackAction attackAction, bool attackerUsingOffhand, bool attackerBesideUnit)
        {
            float dodgeChance;
            if (CarryWeightRatio < 2f)
                dodgeChance = agility.GetValue() / 100f * 0.75f * EncumbranceDodgeChanceMultiplier() * EnemyWeaponSkillDodgeChanceModifier(attackingUnit, weaponBeingAttackedWith, attackerUsingOffhand);
            else
                return 0f;

            float baseDodgeChance = dodgeChance;

            // Dodge chance affected by the inverse of the attack action's accuracy modifier
            dodgeChance += baseDodgeChance * (1f - attackAction.AccuracyModifier());

            // Dodge chance affected by height differences between this Unit and the attackingUnit
            dodgeChance += baseDodgeChance * accuracyModifierPerHeightDifference * TacticsUtilities.CalculateHeightDifferenceToTarget(unit.GridPosition, attackingUnit.GridPosition);

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

        ///<summary>A lower number is worse for the Unit being attacked, as their dodge chance will be multiplied by this.</summary>
        float EncumbranceDodgeChanceMultiplier()
        {
            float carryWeightRatio = CarryWeightRatio;
            if (carryWeightRatio <= 0.5f)
                return 1f;
            else
            {
                if (carryWeightRatio > 2f)
                    carryWeightRatio = 2f;

                // Calculate the dodge chance multiplier as a linear reduction, until the carryWeightRatio is greater than 1, which it will then be an exponential reduction
                if (carryWeightRatio <= 1f)
                    return 1f - (carryWeightRatio * 0.2f);
                else
                    return 1f - (Mathf.Pow(carryWeightRatio, 2) * 0.2f);
            }
        }

        ///<summary>A lower number is worse for the Unit being attacked, as their dodge chance will be multiplied by this.</summary>
        float EnemyWeaponSkillDodgeChanceModifier(Unit attackingUnit, HeldItem weaponAttackingWith, bool attackerUsingOffhand)
        {
            Weapon weapon = null;
            if (weaponAttackingWith != null)
                weapon = weaponAttackingWith.ItemData.Item.Weapon;

            float modifier = 1f - (attackingUnit.stats.WeaponSkill(weapon) / 100f * 0.5f);
            float baseModifier = modifier;

            if (weaponAttackingWith != null)
                modifier -= baseModifier * weaponAttackingWith.ItemData.AccuracyModifier;

            // Weapon skill effectiveness is reduced when dual wielding
            if (attackingUnit.UnitEquipment != null && attackingUnit.UnitEquipment.IsDualWielding)
            {
                if (attackerUsingOffhand)
                    modifier += baseModifier * (1f - Weapon.dualWieldSecondaryEfficiency);
                else
                    modifier += baseModifier * (1f - Weapon.dualWieldPrimaryEfficiency);
            }

            if (modifier < 0f)
                modifier = 0f;

            // Debug.Log($"{attackingUnit.name}'s Weapon Skill Dodge Chance Modifier against {unit.name}: {modifier}");
            return modifier;
        }

        /// <summary>Does not take into account block chance, but does take into account dodge chance (and ranged accuracy, for ranged attacks). Only used for the Hit Chance Tooltip.</summary>
        public float HitChance(Unit targetUnit, BaseAttackAction actionToUse)
        {
            float hitChance = 0f;
            if (actionToUse.IsMeleeAttackAction())
            {
                if (unit.UnitEquipment != null)
                {
                    if (unit.UnitEquipment.IsDualWielding)
                    {
                        bool attackerBesideUnit = targetUnit.unitActionHandler.TurnAction.AttackerBesideUnit(unit);
                        float mainWeaponHitChance = targetUnit.stats.DodgeChance(unit, unit.unitMeshManager.GetRightHeldMeleeWeapon(), actionToUse, false, attackerBesideUnit);
                        float secondaryWeaponHitChance = targetUnit.stats.DodgeChance(unit, unit.unitMeshManager.GetLeftHeldMeleeWeapon(), actionToUse, true, attackerBesideUnit);
                        hitChance = 1f - (mainWeaponHitChance * secondaryWeaponHitChance);
                    }
                    else if (unit.UnitEquipment.MeleeWeaponEquipped)
                        hitChance = 1f - targetUnit.stats.DodgeChance(unit, unit.unitMeshManager.GetPrimaryHeldMeleeWeapon(), actionToUse, false, targetUnit.unitActionHandler.TurnAction.AttackerBesideUnit(unit));
                    else
                        hitChance = 1f - targetUnit.stats.DodgeChance(unit, null, actionToUse, false, targetUnit.unitActionHandler.TurnAction.AttackerBesideUnit(unit));
                }
                else
                    hitChance = 1f - targetUnit.stats.DodgeChance(unit, null, actionToUse, false, targetUnit.unitActionHandler.TurnAction.AttackerBesideUnit(unit));
            }
            else if (actionToUse.IsRangedAttackAction())
            {
                if (unit.UnitEquipment != null)
                {
                    if (actionToUse is ThrowAction)
                    {
                        ThrowAction throwAction = actionToUse as ThrowAction;
                        hitChance = ThrowingAccuracy(throwAction.ItemDataToThrow, targetUnit.GridPosition, actionToUse) * (1f - targetUnit.stats.DodgeChance(unit, null, actionToUse, false, targetUnit.unitActionHandler.TurnAction.AttackerBesideUnit(unit)));
                    }
                    else
                    {
                        HeldItem rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
                        if (unit.UnitEquipment.RangedWeaponEquipped)
                            hitChance = RangedAccuracy(rangedWeapon, targetUnit.GridPosition, actionToUse) * (1f - targetUnit.stats.DodgeChance(unit, rangedWeapon, actionToUse, false, targetUnit.unitActionHandler.TurnAction.AttackerBesideUnit(unit)));
                    }
                }
            }

            return Mathf.RoundToInt(hitChance * 1000f) / 1000f;
        }
        #endregion

        #region Energy
        public int MaxEnergy() => Mathf.RoundToInt(baseEnergy + (endurance.GetValue() * 10f));

        public void UseEnergy(int amount)
        {
            if (amount <= 0)
                return;

            CurrentEnergy -= amount;
            if (CurrentEnergy < 0)
                CurrentEnergy = 0;

            if (unit.IsPlayer)
                ActionSystemUI.UpdateEnergyText();
        }

        public void ReplenishEnergy()
        {
            CurrentEnergy = MaxEnergy();

            if (unit.IsPlayer)
                ActionSystemUI.UpdateEnergyText();
        }

        public void AddToCurrentEnergy(int amountToAdd)
        {
            CurrentEnergy += amountToAdd;
            if (CurrentEnergy > MaxEnergy())
                CurrentEnergy = MaxEnergy();

            if (unit.IsPlayer)
                ActionSystemUI.UpdateEnergyText();
        }

        void UpdateEnergy()
        {
            // Regenerate Energy
            energyRegenBuildup += energyRegenPerTurn;
            if (energyRegenBuildup >= 1f)
            {
                int amountToAdd = Mathf.FloorToInt(energyRegenBuildup);
                AddToCurrentEnergy(amountToAdd);
                energyRegenBuildup -= amountToAdd;
            }

            // Use Energy from Actions (stances, etc.)
            for (int i = 0; i < EnergyUseActions.Count; i++)
            {
                if (HasEnoughEnergy(Mathf.CeilToInt(EnergyUseActions[i].EnergyCostPerTurn())))
                    energyUseBuildup += EnergyUseActions[i].EnergyCostPerTurn();
                else
                    EnergyUseActions[i].CancelAction();
            }

            int amountToUse = Mathf.FloorToInt(energyUseBuildup);
            UseEnergy(amountToUse);
            energyUseBuildup -= amountToUse;
        }

        public bool HasEnoughEnergy(int energyCost) => CurrentEnergy >= energyCost;
        #endregion

        #region Knockback
        public bool TryKnockback(Unit targetUnit, HeldItem heldItemAttackingWith, ItemData weaponItemDataHittingWith, bool attackBlocked)
        {
            float random = Random.Range(0f, 1f);
            if (random <= unit.stats.KnockbackChance(heldItemAttackingWith, weaponItemDataHittingWith, targetUnit, attackBlocked))
            {
                targetUnit.unitAnimator.Knockback(unit);
                OnKnockbackTarget?.Invoke();
                return true;
            }

            if (targetUnit.unitActionHandler.MoveAction.AboutToMove)
                OnFailedToKnockbackTarget?.Invoke(targetUnit.unitActionHandler.MoveAction.NextTargetGridPosition);
            else
                OnFailedToKnockbackTarget?.Invoke(targetUnit.GridPosition);

            return false;
        }

        public float KnockbackChance(HeldItem heldItem, ItemData weaponHitWith, Unit targetUnit, bool attackBlocked)
        {
            Weapon weapon = null;
            if (heldItem != null && heldItem.ItemData.Item is Weapon)
                weapon = heldItem.ItemData.Item as Weapon;
            else if (weaponHitWith != null && weaponHitWith.Item is Weapon)
                weapon = weaponHitWith.Item as Weapon;

            float knockbackChance = WeaponKnockbackChance(weapon);
            float baseKnockbackChance = knockbackChance;

            // Less likely to knock back a stronger opponent and more likely to knockback a weaker one
            knockbackChance += strength.GetValue() / 100f * 0.25f;
            knockbackChance -= targetUnit.stats.strength.GetValue() / 100f * 0.25f;

            if (heldItem != null)
                knockbackChance += baseKnockbackChance * heldItem.ItemData.KnockbackChanceModifier;
            else if (weaponHitWith != null)
                knockbackChance += baseKnockbackChance * weaponHitWith.KnockbackChanceModifier;

            // Knockback effectiveness is reduced when dual wielding
            if (heldItem != null && unit.UnitEquipment.IsDualWielding)
            {
                if (heldItem == unit.unitMeshManager.GetPrimaryHeldMeleeWeapon())
                    knockbackChance *= Weapon.dualWieldPrimaryEfficiency;
                else
                    knockbackChance *= Weapon.dualWieldSecondaryEfficiency;
            }

            if (heldItem != null && heldItem.CurrentHeldItemStance == HeldItemStance.SpearWall)
                knockbackChance *= SpearWallAction.knockbackChanceModifier;

            if (attackBlocked)
                knockbackChance *= 0.25f;

            if (knockbackChance > maxKnockbackChance)
                knockbackChance = maxKnockbackChance;

            // Debug.Log($"{unit.name}'s knockback chance (was blocked = {attackBlocked}): {knockbackChance}");
            return knockbackChance;
        }

        float WeaponKnockbackChance(Weapon weapon)
        {
            if (weapon == null) // Unarmed
                return 0.1f;

            return weapon.WeaponType switch
            {
                WeaponType.Bow => 0.05f,
                WeaponType.Crossbow => 0.2f,
                WeaponType.ThrowingWeapon => 0.025f,
                WeaponType.Dagger => 0.01f,
                WeaponType.Sword => 0.1f,
                WeaponType.Axe => 0.15f,
                WeaponType.Mace => 0.2f,
                WeaponType.WarHammer => 0.3f,
                WeaponType.Spear => 0.1f,
                WeaponType.Polearm => 0.25f,
                _ => 0.1f,
            };
        }
        #endregion

        #region Ranged
        public float RangedAccuracy(HeldItem rangedWeapon, GridPosition targetGridPosition, BaseAttackAction attackAction)
        {
            if (rangedWeapon == null)
                return 0f;

            float accuracy = WeaponSkill(rangedWeapon.ItemData.Item.Weapon) / 100f * 0.5f; // 0.5% (0.005) chance per weapon skill
            float baseAccuracy = accuracy;

            // Accuracy affected by height differences between this Unit and the attackingUnit
            accuracy += baseAccuracy * accuracyModifierPerHeightDifference * TacticsUtilities.CalculateHeightDifferenceToTarget(unit.GridPosition, targetGridPosition);

            // Accuracy affected by the weapon & action accuracy modifiers
            accuracy += baseAccuracy * rangedWeapon.ItemData.AccuracyModifier;
            accuracy += baseAccuracy * attackAction.AccuracyModifier();

            // Accuracy affected by distance to target (but only over a certain value)
            float minDistanceBeforeAccuracyLoss = 3.5f;
            float distanceToTarget = Vector3.Distance(unit.WorldPosition, targetGridPosition.WorldPosition);
            if (distanceToTarget > minDistanceBeforeAccuracyLoss)
                accuracy -= accuracy * (distanceToTarget - minDistanceBeforeAccuracyLoss) * 0.05f; // 5% loss per distance over the minDistanceBeforeAccuracyLoss

            if (accuracy < 0f)
                accuracy = 0f;
            else if (accuracy > maxRangedAccuracy)
                accuracy = maxRangedAccuracy;

            // Debug.Log($"{unit.name}'s ranged accuracy: {accuracy}");
            return accuracy;
        }

        public float ThrowingAccuracy(ItemData itemDataToThrow, GridPosition targetGridPosition, BaseAttackAction attackAction)
        {
            if (itemDataToThrow == null)
                return 0f;

            float accuracy = throwingSkill.GetValue() / 100f * 0.5f; // 0.5% (0.005) chance per throwing skill
            float baseAccuracy = accuracy;

            // Accuracy affected by height differences between this Unit and the attackingUnit
            accuracy += baseAccuracy * accuracyModifierPerHeightDifference * TacticsUtilities.CalculateHeightDifferenceToTarget(unit.GridPosition, targetGridPosition);

            // Accuracy affected by the action's accuracy modifier
            accuracy += baseAccuracy * attackAction.AccuracyModifier();

            // Accuracy affected by distance to target (but only over a certain value)
            float minDistanceBeforeAccuracyLoss = 2.5f;
            float distanceToTarget = Vector3.Distance(unit.WorldPosition, targetGridPosition.WorldPosition);
            if (distanceToTarget > minDistanceBeforeAccuracyLoss)
                accuracy -= accuracy * (distanceToTarget - minDistanceBeforeAccuracyLoss) * 0.05f; // 5% loss per distance over the minDistanceBeforeAccuracyLoss

            if (accuracy < 0f)
                accuracy = 0f;
            else if (accuracy > maxRangedAccuracy)
                accuracy = maxRangedAccuracy;

            // Debug.Log($"{unit.name}'s throwing accuracy: {accuracy}");
            return accuracy;
        }

        public float MaxThrowRange(Item thrownItem)
        {
            float throwRange = (strength.GetValue() / 12f) + (throwingSkill.GetValue() / 12f); // 8.33 distance at 100 skill for each
            throwRange -= thrownItem.Weight / 2f; // Minus 0.5 per pound
            if (throwRange < ThrowAction.minMaxThrowDistance)
                throwRange = ThrowAction.minMaxThrowDistance;
            else if (throwRange > ThrowAction.maxThrowDistance)
                throwRange = ThrowAction.maxThrowDistance;
            return throwRange;
        }
        #endregion

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
                case WeaponType.ThrowingWeapon:
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
        public IntStat Endurance => endurance;
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
