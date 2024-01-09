using GridSystem;
using InventorySystem;
using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace UnitSystem.ActionSystem.Actions
{
    public abstract class Action_BaseAttack : Action_Base
    {
        public Unit TargetEnemyUnit { get; protected set; }

        protected List<GridPosition> validGridPositionsList = new();
        protected List<GridPosition> nearestGridPositionsList = new();

        /// <summary>Durability damage to weapons will be multiplied by this amount.</summary>
        readonly float attackDurabilityDamageRatio = 0.2f;

        public virtual void QueueAction(Unit targetEnemyUnit)
        {
            TargetEnemyUnit = targetEnemyUnit;
            TargetGridPosition = targetEnemyUnit.GridPosition;
            QueueAction();
        }

        public override void QueueAction(GridPosition targetGridPosition)
        {
            TargetGridPosition = targetGridPosition;
            if (LevelGrid.HasUnitAtGridPosition(targetGridPosition, out Unit targetUnit))
                TargetEnemyUnit = targetUnit;
            QueueAction();
        }

        protected void MoveToTargetInstead()
        {
            CompleteAction();
            Unit.UnitActionHandler.SetIsAttacking(false);
            Unit.UnitActionHandler.MoveAction.QueueAction(GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        protected override void StartAction()
        {
            base.StartAction();
            Unit.UnitActionHandler.SetIsAttacking(true);
            if (Unit.IsPlayer && TargetEnemyUnit != null)
                TargetEnemyUnit.UnitActionHandler.NPCActionHandler.GoalPlanner.FightAction.SetStartChaseGridPosition(TargetEnemyUnit.GridPosition);
        }

        protected void SetTargetEnemyUnit()
        {
            if (LevelGrid.HasUnitAtGridPosition(TargetGridPosition, out Unit unitAtGridPosition))
            {
                Unit.UnitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);
                TargetEnemyUnit = unitAtGridPosition;
            }
            else if (Unit.UnitActionHandler.TargetEnemyUnit != null)
                TargetEnemyUnit = Unit.UnitActionHandler.TargetEnemyUnit;
        }

        public void DamageTarget(Unit targetUnit, HeldItem heldItemAttackingWith, ItemData itemDataHittingWith, HeldItem heldItemBlockedWith)
        {
            DamageBodyPart(targetUnit, GetBodyPartHit(targetUnit), heldItemAttackingWith, itemDataHittingWith, heldItemBlockedWith);
        }

        public void DamageBodyPart(Unit targetUnit, BodyPart bodyPart, HeldItem heldItemAttackingWith, ItemData itemHittingWith, HeldItem heldItemBlockedWith)
        {
            if (targetUnit == null || bodyPart == null || targetUnit.HealthSystem.IsDead)
                return;

            int damageDone = DealDamageToTarget(targetUnit, bodyPart, heldItemAttackingWith, itemHittingWith, heldItemBlockedWith);
            if (damageDone > 0)
                targetUnit.UnitActionHandler.InterruptActions(); // Only interrupt actions if the attack did damage

            TryKnockbackTargetUnit(targetUnit, heldItemAttackingWith, itemHittingWith, heldItemBlockedWith, damageDone);
        }

        public virtual BodyPartType[] AdjustedHitChance_BodyPartTypes => null;

        public virtual float[] AdjustedHitChance_Weights => null;

        protected virtual BodyPart GetBodyPartHit(Unit targetUnit) => targetUnit.HealthSystem.GetRandomBodyPartToHit(this);

        protected virtual float GetBaseDamage(ItemData heldItemAttackingWith, ItemData itemDataHittingWith)
        {
            float baseDamage;
            if (heldItemAttackingWith != null)
            {
                baseDamage = heldItemAttackingWith.Damage;
                if (Unit.UnitEquipment is UnitEquipment_Humanoid)
                {
                    if (Unit.UnitEquipment.IsDualWielding)
                    {
                        if (heldItemAttackingWith == Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData)
                            baseDamage *= Item_Weapon.dualWieldPrimaryEfficiency;
                        else
                            baseDamage *= Item_Weapon.dualWieldSecondaryEfficiency;
                    }
                    else if (Unit.UnitEquipment.HumanoidEquipment.InVersatileStance)
                        baseDamage *= Action_VersatileStance.damageModifier;
                }
            }
            else if (itemDataHittingWith != null && itemDataHittingWith.Item != null)
                baseDamage = itemDataHittingWith.Damage;
            else
                baseDamage = UnarmedAttackDamage();

            return baseDamage;
        }

        int DealDamageToTarget(Unit targetUnit, BodyPart bodyPartHit, HeldItem heldItemAttackingWith, ItemData itemHittingWith, HeldItem heldItemBlockedWith)
        {
            ItemData heldItemAttackingWithItemData = null;
            if (heldItemAttackingWith != null) heldItemAttackingWithItemData = heldItemAttackingWith.ItemData;

            float damageAfterArmor = 0f;

            // If the attack was blocked, damage the blocking item's durability, as well as the weapon attacking with
            if (heldItemBlockedWith != null)
            {
                float damage = GetBaseDamage(heldItemAttackingWithItemData, itemHittingWith) * GetEffectivenessAgainstArmor(heldItemAttackingWith, itemHittingWith);
                heldItemBlockedWith.ItemData.DamageDurability(targetUnit, damage);
                if (targetUnit.UnitEquipment.ItemDataEquipped(heldItemBlockedWith.ItemData))
                {
                    // Play the recoil animation & then lower the shield if not in Raise Shield Stance
                    heldItemBlockedWith.Recoil();

                    // Potentially fumble & drop the shield
                    heldItemBlockedWith.TryFumbleHeldItem();
                }

                Unit.StartCoroutine(DamageWeaponAgainstBlock(targetUnit, heldItemAttackingWith, itemHittingWith, heldItemBlockedWith));
                // damageAfterArmor -= GetTargetUnitBlockAmount(targetUnit, heldItemBlockedWith);
            }
            else // If the attack hit the target, determine/apply the final damage to the hit body part, damage their armor's durability, and damage the item attacking with
            {
                DamageArmorAndWeapon(targetUnit, bodyPartHit, heldItemAttackingWith, itemHittingWith, GetBaseDamage(heldItemAttackingWithItemData, itemHittingWith), out damageAfterArmor);
                if (damageAfterArmor < 0f) damageAfterArmor = 0f;
            }

            bodyPartHit.TakeDamage(Mathf.RoundToInt(damageAfterArmor), Unit);
            return Mathf.RoundToInt(damageAfterArmor);
        }

        IEnumerator DamageWeaponAgainstBlock(Unit targetUnit, HeldItem heldItemAttackingWith, ItemData itemHittingWith, HeldItem heldItemBlockedWith)
        {
            while (Unit.UnitActionHandler.IsAttacking) // Wait to finish attacking, in case the weapon is dropped (otherwise it would cause a null reference error for the held weapon in knockback)
                yield return null;

            if (heldItemBlockedWith == null || heldItemBlockedWith.ItemData == null || heldItemBlockedWith.ItemData.Item == null)
                yield break;

            if (heldItemAttackingWith != null)
            {
                if (heldItemAttackingWith.ItemData == null || heldItemAttackingWith.ItemData.Item == null)
                    yield break;

                if (heldItemBlockedWith is HeldShield)
                    heldItemAttackingWith.ItemData.DamageDurability(Unit, WeaponDurabilityDamage() + (targetUnit.Stats.BlockPower(heldItemBlockedWith as HeldShield) * attackDurabilityDamageRatio));
                else if (heldItemBlockedWith is HeldMeleeWeapon)
                    heldItemAttackingWith.ItemData.DamageDurability(Unit, WeaponDurabilityDamage() + (targetUnit.Stats.BlockPower(heldItemBlockedWith as HeldMeleeWeapon) * attackDurabilityDamageRatio));
            }
            else if (itemHittingWith != null && itemHittingWith.MaxDurability > 0)
            {
                if (itemHittingWith.Item == null)
                    yield break;

                if (heldItemBlockedWith is HeldShield)
                    itemHittingWith.DamageDurability(Unit, WeaponDurabilityDamage() + (targetUnit.Stats.BlockPower(heldItemBlockedWith as HeldShield) * attackDurabilityDamageRatio));
                else if (heldItemBlockedWith is HeldMeleeWeapon)
                    itemHittingWith.DamageDurability(Unit, WeaponDurabilityDamage() + (targetUnit.Stats.BlockPower(heldItemBlockedWith as HeldMeleeWeapon) * attackDurabilityDamageRatio));
            }
        }

        void DamageArmorAndWeapon(Unit targetUnit, BodyPart bodyPartHit, HeldItem heldItemAttackingWith, ItemData itemHittingWith, float damage, out float damageAfterArmor)
        {
            float effectivenessAgainstArmor = GetEffectivenessAgainstArmor(heldItemAttackingWith, itemHittingWith);
            float armorPierce = GetArmorPierce(heldItemAttackingWith, itemHittingWith);

            //Debug.Log($"Body Part: {targetUnit.name}'s {bodyPartHit.Name()} | Base Damage: {damage} | Armor Effectiveness: {effectivenessAgainstArmor} | Armor Pierce: {armorPierce}");

            // 1st layer of armor (e.g., Platemail)
            // Damage layer 1's durability (if there's any armor equipped in this layer) and calculate the ratio of damage done in case the armor breaks
            float durabilityDamage1 = damage * effectivenessAgainstArmor;                                                              //Debug.Log($"Durability Damage 1: {durabilityDamage1}");
            DamageArmorDurability_LayerOne(targetUnit, bodyPartHit, durabilityDamage1, out float startingArmorDurability1);            //Debug.Log($"Armor 1 Starting Durability: {startingArmorDurability1}");
            float durabilityDamageDoneRatio1 = Mathf.Min(1f, startingArmorDurability1 / durabilityDamage1);                            //Debug.Log($"Durability Damage Ratio 1: {durabilityDamageDoneRatio1}");

            // Reduce the damage from the 1st layer's "armor" value
            int defenseLayer1 = GetDefense_LayerOne(targetUnit, bodyPartHit);                                                          //Debug.Log($"{targetUnit.name}'s Defense Layer 1: {defenseLayer1}");
            float damageReduction1 = defenseLayer1 * durabilityDamageDoneRatio1;                                                       //Debug.Log($"Damage Reduction 1: {damageReduction1}");
            damage = Mathf.Max(0f, damage - damageReduction1);                                                                         //Debug.Log($"New Damage 1: {damage}");

            // Pierce layer 1 and calculate any excess damage if the armor breaks
            float piercedDamage1 = damage * armorPierce;                                                                               //Debug.Log($"Pierce Damage 1: {piercedDamage1}");
            float excessDamage1 = damage * (1f - armorPierce) * (1f - durabilityDamageDoneRatio1);                                     //Debug.Log($"Excess Damage 1: {excessDamage1}");
                        
            // Calculate the remaining damage to the 2nd layer
            float remainingDamage = piercedDamage1 + excessDamage1;                                                                    //Debug.Log($"Remaining Damage 1: {remainingDamage}");

            // 2nd layer of armor (e.g., Chainmail)
            // Damage layer 2's durability (if there's any armor equipped in this layer) and calculate the ratio of damage done in case the armor breaks
            float durabilityDamage2 = remainingDamage * effectivenessAgainstArmor;                                                     //Debug.Log($"Durability Damage 2: {durabilityDamage2}");
            DamageArmorDurability_LayerTwo(targetUnit, bodyPartHit, remainingDamage, out float startingArmorDurability2);              //Debug.Log($"Armor 2 Starting Durability: {startingArmorDurability2}");
            float durabilityDamageDoneRatio2 = Mathf.Min(1f, startingArmorDurability2 / durabilityDamage2);                            //Debug.Log($"Durability Damage Ratio 2: {durabilityDamageDoneRatio2}");

            // Reduce the damage from the 2nd layer's "armor" value
            int defenseLayer2 = GetDefense_LayerTwo(targetUnit, bodyPartHit);                                                          //Debug.Log($"{targetUnit.name}'s Defense Layer 2: {defenseLayer2}");
            float damageReduction2 = defenseLayer2 * durabilityDamageDoneRatio2;                                                       //Debug.Log($"Damage Reduction 2: {damageReduction2}");
            damage = Mathf.Max(0f, remainingDamage - damageReduction2);                                                                //Debug.Log($"New Damage 2: {damage}");

            // Pierce layer 2 and calculate any excess damage if the armor breaks
            float piercedDamage2 = damage * armorPierce;                                                                               //Debug.Log($"Pierce Damage 2: {piercedDamage2}");
            float excessDamage2 = damage * (1f - armorPierce) * (1f - durabilityDamageDoneRatio2);                                     //Debug.Log($"Excess Damage 2: {excessDamage2}");

            // Damage weapon's durability
            Unit.StartCoroutine(DamageWeaponAgainstArmor(heldItemAttackingWith, itemHittingWith, defenseLayer1, durabilityDamageDoneRatio1, defenseLayer2, durabilityDamageDoneRatio2));

            // Total damage to targetUnit is the sum of pierced and excess damage from both armor layers
            // Debug.Log($"Final Damage: {piercedDamage2 + excessDamage2}");
            damageAfterArmor = piercedDamage2 + excessDamage2;
        }

        IEnumerator DamageWeaponAgainstArmor(HeldItem heldItemAttackingWith, ItemData itemHittingWith, int defenseLayer1, float durabilityDamageDoneRatio1, int defenseLayer2, float durabilityDamageDoneRatio2)
        {
            while (Unit.UnitActionHandler.IsAttacking) // Wait to finish attacking, in case the weapon is dropped (otherwise it would cause a null reference error for the held weapon in knockback)
                yield return null;

            if (heldItemAttackingWith != null)
            {
                if (heldItemAttackingWith.ItemData != null && heldItemAttackingWith.ItemData.Item != null && heldItemAttackingWith.ItemData.Item is Item_MeleeWeapon)
                    heldItemAttackingWith.ItemData.DamageDurability(Unit, WeaponDurabilityDamage() + (defenseLayer1 * attackDurabilityDamageRatio * durabilityDamageDoneRatio1) + (defenseLayer2 * attackDurabilityDamageRatio * durabilityDamageDoneRatio2));
            }
            else if (itemHittingWith != null)
            {
                if (itemHittingWith.Item != null && itemHittingWith.Item is Item_MeleeWeapon)
                    itemHittingWith.DamageDurability(Unit, WeaponDurabilityDamage() + (defenseLayer1 * attackDurabilityDamageRatio * durabilityDamageDoneRatio1) + (defenseLayer2 * attackDurabilityDamageRatio * durabilityDamageDoneRatio2));
            }
        }

        protected virtual float GetEffectivenessAgainstArmor(HeldItem heldItemAttackingWith, ItemData itemHittingWith)
        {
            float effectivenessAgainstArmor;
            if (heldItemAttackingWith != null)
            {
                effectivenessAgainstArmor = heldItemAttackingWith.ItemData.EffectivenessAgainstArmor;
                if (itemHittingWith != null && itemHittingWith.Item is Item_Ammunition)
                    effectivenessAgainstArmor = (effectivenessAgainstArmor + itemHittingWith.EffectivenessAgainstArmor) / 2f;
            }
            else if (itemHittingWith != null)
                effectivenessAgainstArmor = itemHittingWith.EffectivenessAgainstArmor;
            else // If unarmed
                effectivenessAgainstArmor = Unit.Stats.UnarmedEffectivenessAgainstArmor;
            return effectivenessAgainstArmor * EffectivenessAgainstArmorModifier();
        }

        protected virtual float GetArmorPierce(HeldItem heldItemAttackingWith, ItemData itemHittingWith)
        {
            float armorPierce;
            if (heldItemAttackingWith != null)
            {
                armorPierce = heldItemAttackingWith.ItemData.ArmorPierce;
                if (itemHittingWith != null && itemHittingWith.Item is Item_Ammunition)
                    armorPierce = (armorPierce + itemHittingWith.ArmorPierce) / 2f;
            }
            else if (itemHittingWith != null)
                armorPierce = itemHittingWith.ArmorPierce;
            else // If unarmed
                armorPierce = Unit.Stats.UnarmedArmorPierce;

            armorPierce *= ArmorPierceModifier();
            return Mathf.Clamp01(armorPierce);
        }

        int GetDefense_LayerOne(Unit targetUnit, BodyPart bodyPartHit)
        {
            if (targetUnit == null || bodyPartHit == null || targetUnit.UnitEquipment == null)
                return 0;

            switch (bodyPartHit.BodyPartType)
            {
                case BodyPartType.Head:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Helm))
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Helm).Defense;
                    return 0;
                case BodyPartType.Torso:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.BodyArmor))
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Defense;
                    return 0;
                case BodyPartType.Arm:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.BodyArmor) && targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Item.BodyArmor.ProtectsArms)
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Defense;
                    return 0;
                case BodyPartType.Hand:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Gloves))
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Gloves).Defense;
                    return 0;
                case BodyPartType.Leg:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.BodyArmor) && targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Item.BodyArmor.ProtectsLegs)
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Defense;
                    return 0;
                case BodyPartType.Foot:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Boots))
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Boots).Defense;
                    return 0;
                default:
                    return 0;
            }
        }

        int GetDefense_LayerTwo(Unit targetUnit, BodyPart bodyPartHit)
        {
            if (targetUnit == null || bodyPartHit == null || targetUnit.UnitEquipment == null)
                return 0;

            switch (bodyPartHit.BodyPartType)
            {
                case BodyPartType.Torso:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Shirt))
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).Defense;
                    return 0;
                case BodyPartType.Arm:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Shirt) && targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).Item.Shirt.ProtectsArms)
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).Defense;
                    return 0;
                case BodyPartType.Leg:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.LegArmor))
                        return targetUnit.UnitEquipment.EquippedItemData(EquipSlot.LegArmor).Defense;
                    return 0;
                default:
                    return 0;
            }
        }

        void DamageArmorDurability_LayerOne(Unit targetUnit, BodyPart bodyPartHit, float durabilityDamage, out float startDurability)
        {
            startDurability = 0;
            if (targetUnit == null || bodyPartHit == null || targetUnit.UnitEquipment == null)
                return;

            switch (bodyPartHit.BodyPartType)
            {
                case BodyPartType.Head:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Helm))
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Helm).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Helm).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Torso:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.BodyArmor)) 
                    { 
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Arm:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.BodyArmor) && targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Item.BodyArmor.ProtectsArms) 
                    { 
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Hand:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Gloves))
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Gloves).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Gloves).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Leg:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.BodyArmor) && targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).Item.BodyArmor.ProtectsLegs)
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.BodyArmor).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Foot:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Boots))
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Boots).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Boots).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                default:
                    break;
            }
        }

        void DamageArmorDurability_LayerTwo(Unit targetUnit, BodyPart bodyPartHit, float durabilityDamage, out float startDurability)
        {
            startDurability = 0;
            if (targetUnit == null || bodyPartHit == null || targetUnit.UnitEquipment == null)
                return;

            switch (bodyPartHit.BodyPartType)
            {
                case BodyPartType.Torso:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Shirt))
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Arm:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.Shirt) && targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).Item.Shirt.ProtectsArms)
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.Shirt).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                case BodyPartType.Leg:
                    if (targetUnit.UnitEquipment.EquipSlotHasItem(EquipSlot.LegArmor))
                    {
                        startDurability = targetUnit.UnitEquipment.EquippedItemData(EquipSlot.LegArmor).CurrentDurability;
                        targetUnit.UnitEquipment.EquippedItemData(EquipSlot.LegArmor).DamageDurability(targetUnit, durabilityDamage);
                    }
                    break;
                default:
                    break;
            }
        }

        public virtual float WeaponDurabilityDamage() => 1f;

        protected virtual float EffectivenessAgainstArmorModifier() => 1f;

        protected virtual float ArmorPierceModifier() => 1f;

        /*protected float GetTargetUnitBlockAmount(Unit targetUnit, HeldItem heldItemBlockedWith)
        {
            float blockAmount = 0f;
            if (heldItemBlockedWith == null)
                return blockAmount;

            if (heldItemBlockedWith is HeldShield)
            {
                HeldShield shieldBlockedWith = heldItemBlockedWith as HeldShield;
                blockAmount = targetUnit.Stats.BlockPower(shieldBlockedWith);

                // Play the recoil animation & then lower the shield if not in Raise Shield Stance
                shieldBlockedWith.Recoil();

                // Potentially fumble & drop the shield
                shieldBlockedWith.TryFumbleHeldItem();
            }
            else if (heldItemBlockedWith is HeldMeleeWeapon)
            {
                HeldMeleeWeapon meleeWeaponBlockedWith = heldItemBlockedWith as HeldMeleeWeapon;
                blockAmount = targetUnit.Stats.BlockPower(meleeWeaponBlockedWith);

                if (targetUnit.UnitEquipment.IsDualWielding)
                {
                    if (meleeWeaponBlockedWith == targetUnit.UnitMeshManager.GetRightHeldMeleeWeapon())
                        blockAmount *= Item_Weapon.dualWieldPrimaryEfficiency;
                    else
                        blockAmount *= Item_Weapon.dualWieldSecondaryEfficiency;
                }

                // Play the recoil animation & then lower the weapon
                meleeWeaponBlockedWith.Recoil();

                // Potentially fumble & drop the weapon
                meleeWeaponBlockedWith.TryFumbleHeldItem();
            }

            return blockAmount;
        }*/

        protected void TryKnockbackTargetUnit(Unit targetUnit, HeldItem heldItemAttackingWith, ItemData itemDataHittingWith, HeldItem heldItemBlockedWith, float damageAmount)
        {
            // Don't try to knockback if the Unit died or they didn't take any damage due to armor absorbtion
            if ((damageAmount > 0f || heldItemBlockedWith != null) && !targetUnit.HealthSystem.IsDead)
            {
                Unit.Stats.TryKnockback(targetUnit, heldItemAttackingWith, itemDataHittingWith, heldItemBlockedWith);
                if (IsMeleeAttackAction())
                    targetUnit.HealthSystem.OnHitByMeleeAttack();
            }
        }

        public virtual void DamageTargets(HeldItem heldWeaponAttackingWith, ItemData itemDataHittingWith)
        {
            foreach (KeyValuePair<Unit, HeldItem> target in Unit.UnitActionHandler.TargetUnits)
            {
                Unit targetUnit = target.Key;
                HeldItem itemBlockedWith = target.Value;
                DamageTarget(targetUnit, heldWeaponAttackingWith, itemDataHittingWith, itemBlockedWith);
            }

            if (heldWeaponAttackingWith != null)
                heldWeaponAttackingWith.TryFumbleHeldItem();
        }

        public virtual IEnumerator WaitToDamageTargets(HeldItem heldWeaponAttackingWith, ItemData itemDataHittingWith)
        {
            if (heldWeaponAttackingWith != null)
                yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(heldWeaponAttackingWith.ItemData.Item as Item_Weapon));
            else
                yield return new WaitForSeconds(AnimationTimes.Instance.UnarmedAttackTime() * 0.5f);

            DamageTargets(heldWeaponAttackingWith, itemDataHittingWith);
        }

        public virtual int UnarmedAttackDamage()
        {
            float damage = Unit.Stats.UnarmedDamage;
            if (Unit.UnitEquipment.EquipSlotHasItem(EquipSlot.Gloves))
                damage *= Unit.UnitEquipment.EquippedItemData(EquipSlot.Gloves).UnarmedDamageMultiplier;
            return Mathf.RoundToInt(damage);
        }

        protected virtual IEnumerator DoAttack()
        {
            // Get the Units within the attack position
            List<Unit> targetUnits = ListPool<Unit>.Claim(); 
            foreach (GridPosition gridPosition in GetActionAreaGridPositions(TargetGridPosition))
            {
                if (LevelGrid.HasUnitAtGridPosition(gridPosition, out Unit targetUnit) == false)
                    continue;

                targetUnits.Add(targetUnit);

                // The unit being attacked becomes aware of this unit
                Unit.Vision.BecomeVisibleUnitOfTarget(targetUnit, targetUnit.UnitActionHandler.TargetUnits.Count == 1);
            }

            // We need to skip a frame in case the target Unit's meshes are being enabled due to becoming visible
            yield return null; 
            
            HeldMeleeWeapon primaryMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (Unit.IsPlayer || (TargetEnemyUnit != null && TargetEnemyUnit.IsPlayer) || Unit.UnitMeshManager.IsVisibleOnScreen || (TargetEnemyUnit != null && TargetEnemyUnit.UnitMeshManager.IsVisibleOnScreen))
            {
                if (TargetEnemyUnit != null)
                {
                    while (TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving || TargetEnemyUnit.UnitAnimator.beingKnockedBack)
                        yield return null;

                    // If the target Unit moved out of range, queue a movement instead
                    if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false)
                    {
                        MoveToTargetInstead();
                        yield break;
                    }
                }

                // Rotate towards the target
                if (targetUnits.Count == 1) // If there's only 1 target Unit in the attack area, they mind as well just face that target
                {
                    if (Unit.UnitActionHandler.TurnAction.IsFacingTarget(targetUnits[0].GridPosition) == false)
                        Unit.UnitActionHandler.TurnAction.RotateTowardsPosition(targetUnits[0].transform.position, false);
                }
                else
                {
                    if (Unit.UnitActionHandler.TurnAction.IsFacingTarget(TargetGridPosition) == false)
                        Unit.UnitActionHandler.TurnAction.RotateTowardsPosition(TargetGridPosition.WorldPosition, false);
                }

                // Wait to finish any rotations already in progress
                while (Unit.UnitActionHandler.TurnAction.isRotating)
                    yield return null;

                for (int i = 0; i < targetUnits.Count; i++)
                {
                    // The targetUnit tries to dodge, and if they fail that, they try to block instead
                    if (targetUnits[i].UnitActionHandler.TryDodgeAttack(Unit, primaryMeleeWeapon, this, false))
                        targetUnits[i].UnitAnimator.DoDodge(Unit, primaryMeleeWeapon, null);
                    else
                    {
                        // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
                        bool attackBlocked = targetUnits[i].UnitActionHandler.TryBlockMeleeAttack(Unit, primaryMeleeWeapon, this, false);
                        Unit.UnitActionHandler.TargetUnits.TryGetValue(targetUnits[i], out HeldItem itemBlockedWith);

                        // If the target is successfully blocking the attack
                        if (attackBlocked && itemBlockedWith != null)
                            itemBlockedWith.BlockAttack(Unit);
                    }
                }

                Unit.StartCoroutine(WaitToDamageTargets(primaryMeleeWeapon, primaryMeleeWeapon.ItemData));

                // Play the attack animations and handle blocking for each target
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Rotate towards the target
                if (Unit.UnitActionHandler.TurnAction.IsFacingTarget(TargetGridPosition) == false)
                    Unit.UnitActionHandler.TurnAction.RotateTowardsPosition(TargetGridPosition.WorldPosition, true);

                for (int i = 0; i < targetUnits.Count; i++)
                {
                    // The targetUnit tries to dodge, and if they fail that, they try to block instead
                    if (targetUnits[i].UnitActionHandler.TryDodgeAttack(Unit, primaryMeleeWeapon, this, false) == false)
                    {
                        // The targetUnit tries to block the attack and if they do, they face their attacker
                        if (targetUnits[i].UnitActionHandler.TryBlockMeleeAttack(Unit, primaryMeleeWeapon, this, false))
                            targetUnits[i].UnitActionHandler.TurnAction.RotateTowards_Unit(Unit, true);

                        // Damage this unit
                        DamageTargets(primaryMeleeWeapon, primaryMeleeWeapon.ItemData);
                    }
                }

                Unit.UnitActionHandler.SetIsAttacking(false);
            }

            ListPool<Unit>.Release(targetUnits);

            // Wait until the attack lands before completing the action
            while (Unit.UnitActionHandler.IsAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit); // This must remain outside of CompleteAction in case we need to call CompletAction early within MoveToTargetInstead
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            Unit.UnitActionHandler.TargetUnits.Clear();
        }

        public bool OtherUnitInTheWay(Unit unit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            Unit targetUnit = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unit == null || targetUnit == null)
                return false;

            // Check if there's a Unit in the way of the attack
            float raycastDistance = Vector3.Distance(startGridPosition.WorldPosition, targetGridPosition.WorldPosition);
            Vector3 attackDir = (targetGridPosition.WorldPosition - startGridPosition.WorldPosition).normalized;
            Vector3 offset = 0.1f * Vector3.up;
            if (Physics.Raycast(startGridPosition.WorldPosition + offset, attackDir, out RaycastHit hit, raycastDistance, unit.Vision.UnitsMask))
            {
                if (hit.collider.gameObject != unit.gameObject && hit.collider.gameObject != targetUnit.gameObject && unit.Vision.IsVisible(hit.collider.gameObject))
                    return true;
            }
            return false;
        }

        public override List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
        {
            validGridPositionsList.Clear();
            if (!LevelGrid.IsValidGridPosition(targetGridPosition))
                return validGridPositionsList;

            if (!IsInAttackRange(null, Unit.GridPosition, targetGridPosition))
                return validGridPositionsList;

            float sphereCastRadius = 0.1f;
            float raycastDistance = Vector3.Distance(Unit.WorldPosition, targetGridPosition.WorldPosition);
            Vector3 offset = 2f * Unit.ShoulderHeight * Vector3.up;
            Vector3 attackDir = (Unit.WorldPosition - targetGridPosition.WorldPosition).normalized;
            if (Physics.SphereCast(targetGridPosition.WorldPosition + offset, sphereCastRadius, attackDir, out _, raycastDistance, Unit.UnitActionHandler.AttackObstacleMask))
                return validGridPositionsList; // Blocked by an obstacle

            // Check if there's a Unit in the way of the attack
            if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, Unit.GridPosition, targetGridPosition))
                return validGridPositionsList;

            validGridPositionsList.Add(targetGridPosition);
            return validGridPositionsList;
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            float boundsDimension = (MaxAttackRange() * 2) + 0.1f;

            validGridPositionsList.Clear();
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes.AddRange(AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension))));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (!LevelGrid.IsValidGridPosition(nodeGridPosition))
                    continue;

                if (!IsInAttackRange(null, startGridPosition, nodeGridPosition))
                    continue;

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                float raycastDistance = Vector3.Distance(Unit.WorldPosition, nodeGridPosition.WorldPosition);
                Vector3 offset = 2f * Unit.ShoulderHeight * Vector3.up;
                Vector3 attackDir = (nodeGridPosition.WorldPosition - startGridPosition.WorldPosition).normalized;
                if (Physics.SphereCast(startGridPosition.WorldPosition + offset, sphereCastRadius, attackDir, out _, raycastDistance, Unit.UnitActionHandler.AttackObstacleMask))
                    continue;

                // Check if there's a Unit in the way of the attack (but only if the attack can't be performed through or over other Units)
                if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, startGridPosition, nodeGridPosition))
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public virtual GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            nearestGridPositionsList.Clear();
            List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
            gridPositions.AddRange(GetValidGridPositionsInRange(targetUnit));
            float nearestDistance = 100000f;

            // First find the nearest valid Grid Positions to the Player
            for (int i = 0; i < gridPositions.Count; i++)
            {
                float distance = Vector3.Distance(gridPositions[i].WorldPosition, startGridPosition.WorldPosition);
                if (distance < nearestDistance)
                {
                    nearestGridPositionsList.Clear();
                    nearestGridPositionsList.Add(gridPositions[i]);
                    nearestDistance = distance;
                }
                else if (Mathf.Approximately(distance, nearestDistance))
                    nearestGridPositionsList.Add(gridPositions[i]);
            }

            GridPosition nearestGridPosition = startGridPosition;
            float nearestDistanceToTarget = 100000f;
            for (int i = 0; i < nearestGridPositionsList.Count; i++)
            {
                // Get the Grid Position that is closest to the target Grid Position
                float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition, targetUnit.WorldPosition);
                if (distance < nearestDistanceToTarget)
                {
                    nearestDistanceToTarget = distance;
                    nearestGridPosition = nearestGridPositionsList[i];
                }
            }

            ListPool<GridPosition>.Release(gridPositions);
            return nearestGridPosition;
        }

        protected List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float boundsDimension = (MaxAttackRange() * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes.AddRange(AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension))));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (!LevelGrid.IsValidGridPosition(nodeGridPosition))
                    continue;

                // If Grid Position has a Unit there already
                if (LevelGrid.HasUnitAtGridPosition(nodeGridPosition, out _))
                    continue;

                // If target is out of attack range from this Grid Position
                if (!IsInAttackRange(null, nodeGridPosition, targetUnit.GridPosition))
                    continue;

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                Vector3 unitOffset = 2f * Unit.ShoulderHeight * Vector3.up;
                Vector3 targetUnitOffset = 2f * targetUnit.ShoulderHeight * Vector3.up;
                float raycastDistance = Vector3.Distance(nodeGridPosition.WorldPosition + unitOffset, targetUnit.WorldPosition + targetUnitOffset);
                Vector3 attackDir = (nodeGridPosition.WorldPosition + unitOffset - (targetUnit.WorldPosition + targetUnitOffset)).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + targetUnitOffset, sphereCastRadius, attackDir, out _, raycastDistance, Unit.UnitActionHandler.AttackObstacleMask))
                    continue;

                if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, nodeGridPosition, targetUnit.GridPosition))
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            List<GridPosition> attackGridPositions = ListPool<GridPosition>.Claim();
            attackGridPositions.AddRange(GetActionAreaGridPositions(targetGridPosition));
            for (int i = 0; i < attackGridPositions.Count; i++)
            {
                if (!LevelGrid.HasUnitAtGridPosition(attackGridPositions[i], out Unit unitAtGridPosition))
                    continue;
                
                if (unitAtGridPosition.HealthSystem.IsDead)
                    continue;

                if (Unit.Alliance.IsAlly(unitAtGridPosition))
                    continue;

                if (!Unit.Vision.IsDirectlyVisible(unitAtGridPosition))
                    continue;

                // If the loop makes it to this point, then it found a valid unit
                ListPool<GridPosition>.Release(attackGridPositions);
                return true;
            }

            ListPool<GridPosition>.Release(attackGridPositions);
            return false;
        }

        protected float ActionPointCostModifier_WeaponType(Item_Weapon weapon)
        {
            if (weapon == null) // Unarmed
                return 0.5f;

            switch (weapon.WeaponType)
            {
                case WeaponType.Bow:
                    return 1f;
                case WeaponType.Crossbow:
                    return 0.2f;
                case WeaponType.ThrowingWeapon:
                    return 0.6f;
                case WeaponType.Dagger:
                    return 0.55f;
                case WeaponType.Sword:
                    return 1f;
                case WeaponType.Axe:
                    return 1.35f;
                case WeaponType.Mace:
                    return 1.3f;
                case WeaponType.WarHammer:
                    return 1.5f;
                case WeaponType.Spear:
                    return 0.85f;
                case WeaponType.Polearm:
                    return 1.25f;
                default:
                    Debug.LogError(weapon.WeaponType.ToString() + " has not been implemented in this method. Fix me!");
                    return 1f;
            }
        }

        public virtual bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            // Check for obstacles
            if (targetUnit != null)
            {
                if (!Unit.Vision.IsInLineOfSight_SphereCast(targetUnit))
                    return false;
            }
            else if (!Unit.Vision.IsInLineOfSight_SphereCast(targetGridPosition))
                return false;

            // Check if there's a Unit in the way of the attack (but only for actions that can't attack through or over other Units)
            if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, startGridPosition, targetGridPosition))
                return false;

            float distance = Vector3.Distance(startGridPosition.WorldPosition, targetGridPosition.WorldPosition);
            if (distance < MinAttackRange() || distance > MaxAttackRange())
                return false;
            return true;
        }

        public virtual bool IsInAttackRange(Unit targetUnit)
        {
            if (targetUnit == null)
                return IsInAttackRange(null, Unit.GridPosition, TargetGridPosition);
            else
                return IsInAttackRange(targetUnit, Unit.GridPosition, targetUnit.GridPosition);
        }

        public abstract float MinAttackRange();
        public abstract float MaxAttackRange();

        public abstract bool CanShowAttackRange();

        /// <summary>A weapon's accuracy will be multiplied by this amount at the end of the calculation, rather than directly added or subtracted. Affects Ranged Accuracy & Dodge Chance.</summary>
        public abstract float AccuracyModifier();

        public abstract bool CanAttackThroughUnits();

        public abstract bool IsMeleeAttackAction();
        public abstract bool IsRangedAttackAction();

        public abstract void PlayAttackAnimation();
    }
}
