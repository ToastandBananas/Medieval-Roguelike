using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public class MeleeAction : BaseAttackAction
    {
        readonly int baseAPCost = 300;

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);
            SetTargetEnemyUnit();
        }

        public override int ActionPointsCost()
        {
            float cost;
            if (Unit.UnitEquipment != null)
            {
                Unit.UnitEquipment.GetEquippedWeapons(out Weapon primaryWeapon, out Weapon secondaryWeapon);
                if (primaryWeapon != null)
                {
                    if (secondaryWeapon != null)
                    {
                        cost = baseAPCost / 2f * ActionPointCostModifier_WeaponType(primaryWeapon);
                        cost += baseAPCost / 2f * ActionPointCostModifier_WeaponType(secondaryWeapon);
                    }
                    else
                        cost = baseAPCost * ActionPointCostModifier_WeaponType(primaryWeapon);
                }
                else
                    cost = baseAPCost * ActionPointCostModifier_WeaponType(null);

                if (Unit.UnitEquipment.InVersatileStance)
                    cost *= VersatileStanceAction.APCostModifier;
            }
            else
                cost = baseAPCost * ActionPointCostModifier_WeaponType(null);

            // If not facing the target position, add the cost of turning towards that position
            Unit.unitActionHandler.TurnAction.DetermineTargetTurnDirection(TargetGridPosition);
            cost += Unit.unitActionHandler.TurnAction.ActionPointsCost();
            return Mathf.RoundToInt(cost);
        }

        public override void TakeAction()
        {
            if (Unit.unitActionHandler.IsAttacking) return;

            if (TargetEnemyUnit == null)
                SetTargetEnemyUnit();

            if (TargetEnemyUnit == null || TargetEnemyUnit.health.IsDead)
            {
                TargetEnemyUnit = null;
                Unit.unitActionHandler.SetTargetEnemyUnit(null);
                Unit.unitActionHandler.FinishAction();
                return;
            }

            if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
            {
                StartAction();
                Unit.StartCoroutine(DoAttack());
            }
            else
                MoveToTargetInstead();
        }

        public void DoOpportunityAttack(Unit targetEnemyUnit)
        {
            // Unit's can't opportunity attack while moving
            if (Unit.unitActionHandler.MoveAction.IsMoving)
                return;

            TargetEnemyUnit = targetEnemyUnit;
            Unit.unitActionHandler.SetIsAttacking(true);
            Unit.StartCoroutine(Attack());
        }
        
        void AttackTarget(HeldItem heldWeaponAttackingWith, ItemData weaponItemData, bool isUsingOffhandWeapon)
        {
            bool attackDodged = TargetEnemyUnit.unitActionHandler.TryDodgeAttack(Unit, heldWeaponAttackingWith, this, isUsingOffhandWeapon);
            if (attackDodged)
                TargetEnemyUnit.unitAnimator.DoDodge(Unit, heldWeaponAttackingWith, null);
            else
            {
                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                bool attackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(Unit, heldWeaponAttackingWith, isUsingOffhandWeapon);
                Unit.unitActionHandler.targetUnits.TryGetValue(TargetEnemyUnit, out HeldItem itemBlockedWith);

                if (attackBlocked && itemBlockedWith != null)
                    itemBlockedWith.BlockAttack(Unit);
            }

            Unit.StartCoroutine(WaitToDamageTargets(heldWeaponAttackingWith, weaponItemData));
        }

        protected override IEnumerator DoAttack()
        {
            while (TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving || TargetEnemyUnit.unitAnimator.beingKnockedBack)
                yield return null;

            // If the target Unit moved out of range, queue a movement instead
            if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false)
            {
                MoveToTargetInstead();
                yield break;
            }

            StartCoroutine(Attack());

            // Wait until the attack lands before completing the action
            while (Unit.unitActionHandler.IsAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        IEnumerator Attack()
        {
            // The unit being attacked becomes aware of this unit
            Unit.vision.BecomeVisibleUnitOfTarget(TargetEnemyUnit, true);

            // We need to skip a frame in case the target Unit's meshes are being enabled
            yield return null;
            
            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (Unit.IsPlayer || TargetEnemyUnit.IsPlayer || Unit.unitMeshManager.IsVisibleOnScreen || TargetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (Unit.unitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition) == false)
                    Unit.unitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (Unit.unitActionHandler.TurnAction.isRotating)
                    yield return null;
                
                // Play the attack animations and handle blocking for the target
                if (Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                {
                    Unit.unitAnimator.DoDefaultUnarmedAttack();
                    AttackTarget(null, null, false);
                }
                else if (Unit.UnitEquipment.IsDualWielding)
                {
                    float distanceToTarget = Vector3.Distance(Unit.WorldPosition, TargetEnemyUnit.WorldPosition);
                    HeldMeleeWeapon primaryHeldWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    bool primaryWeaponInRange = primaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && primaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;
                    bool secondaryWeaponInRange = secondaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && secondaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;

                    if (primaryWeaponInRange && secondaryWeaponInRange) // Dual wield attack
                    {
                        Unit.unitAnimator.StartDualMeleeAttack();
                        primaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                        AttackTarget(primaryHeldWeapon, primaryHeldWeapon.ItemData, false);

                        yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(primaryHeldWeapon.ItemData.Item as Weapon) / 2f) + 0.05f);

                        secondaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                        AttackTarget(secondaryHeldWeapon, secondaryHeldWeapon.ItemData, true);
                    }
                    else
                    {
                        Unit.unitAnimator.StartMeleeAttack();

                        if (primaryWeaponInRange) // Primary weapon only attack
                        {
                            primaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                            AttackTarget(primaryHeldWeapon, primaryHeldWeapon.ItemData, false);
                        }
                        else // Secondary weapon only attack
                        {
                            secondaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                            AttackTarget(secondaryHeldWeapon, secondaryHeldWeapon.ItemData, false);
                        }
                    }
                }
                else
                {
                    // Primary weapon attack
                    Unit.unitAnimator.StartMeleeAttack();
                    if (Unit.UnitEquipment.MeleeWeaponEquipped)
                    {
                        HeldMeleeWeapon primaryMeleeWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                        primaryMeleeWeapon.DoDefaultAttack(TargetGridPosition);
                        AttackTarget(primaryMeleeWeapon, primaryMeleeWeapon.ItemData, false);
                    }
                    else // Fallback to unarmed attack
                    {
                        Unit.unitAnimator.DoDefaultUnarmedAttack();
                        AttackTarget(null, null, false);
                    }
                }
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Try to dodge or block the attack
                bool attackDodged = false;
                bool attackBlocked = false;
                bool headShot = false;
                if (Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped) // Unarmed or has a ranged weapon equipped, but no ammo
                {
                    attackDodged = TargetEnemyUnit.unitActionHandler.TryDodgeAttack(Unit, null, this, false);
                    if (attackDodged == false)
                    {
                        attackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(Unit, null, false);
                        DamageTargets(null, null, headShot);
                    }
                }
                else if (Unit.UnitEquipment.IsDualWielding) // Dual wield attack
                {
                    float distanceToTarget = Vector3.Distance(Unit.WorldPosition, TargetEnemyUnit.WorldPosition);
                    HeldMeleeWeapon primaryHeldWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    bool primaryWeaponInRange = primaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && primaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;
                    bool secondaryWeaponInRange = secondaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && secondaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;
                    
                    bool mainAttackDodged = false;
                    bool mainAttackBlocked = false;
                    if (primaryWeaponInRange)
                    {
                        mainAttackDodged = TargetEnemyUnit.unitActionHandler.TryDodgeAttack(Unit, Unit.unitMeshManager.rightHeldItem, this, false);
                        if (mainAttackDodged == false)
                        {
                            mainAttackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(Unit, Unit.unitMeshManager.rightHeldItem, false);
                            DamageTargets(Unit.unitMeshManager.rightHeldItem as HeldMeleeWeapon, Unit.unitMeshManager.rightHeldItem.ItemData, headShot);
                        }
                    }

                    bool offhandAttackDodged = false;
                    bool offhandAttackBlocked = false;
                    if (secondaryWeaponInRange)
                    {
                        offhandAttackDodged = TargetEnemyUnit.unitActionHandler.TryDodgeAttack(Unit, Unit.unitMeshManager.leftHeldItem, this, true);
                        if (offhandAttackDodged == false)
                        {
                            offhandAttackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(Unit, Unit.unitMeshManager.leftHeldItem, true);
                            DamageTargets(Unit.unitMeshManager.leftHeldItem as HeldMeleeWeapon, Unit.unitMeshManager.leftHeldItem.ItemData, headShot);
                        }
                    }

                    if (mainAttackDodged || offhandAttackDodged)
                        attackDodged = true;

                    if (mainAttackBlocked || offhandAttackBlocked)
                        attackBlocked = true;
                }
                else
                {
                    HeldMeleeWeapon primaryMeleeWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                    attackDodged = TargetEnemyUnit.unitActionHandler.TryDodgeAttack(Unit, primaryMeleeWeapon, this, false);
                    if (attackDodged == false)
                    {
                        attackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(Unit, primaryMeleeWeapon, false);
                        if (primaryMeleeWeapon != null)
                            DamageTargets(primaryMeleeWeapon, primaryMeleeWeapon.ItemData, headShot); // Right hand weapon attack
                        else
                            DamageTargets(null, null, headShot); // Fallback to unarmed damage
                    }
                }

                // Rotate towards the target
                if (Unit.unitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition) == false)
                    Unit.unitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // If the attack was dodged or blocked and the defending unit isn't facing their attacker, turn to face the attacker
                if (attackDodged || attackBlocked)
                    TargetEnemyUnit.unitActionHandler.TurnAction.RotateTowards_Unit(Unit, true);

                Unit.unitActionHandler.SetIsAttacking(false);
            }
        }

        public override void PlayAttackAnimation() { }

        public override void CompleteAction()
        {
            base.CompleteAction();
            
            if (Unit.IsPlayer)
            {
                Unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                if (PlayerInput.Instance.AutoAttack == false)
                {
                    TargetEnemyUnit = null;
                    Unit.unitActionHandler.SetTargetEnemyUnit(null);
                }
            }

            Unit.unitActionHandler.FinishAction();
        }

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized * 100f);
                float distance = Vector3.Distance(Unit.WorldPosition, targetUnit.WorldPosition);
                float minAttackRange = 1f;
                if (Unit.UnitEquipment.IsDualWielding)
                    minAttackRange = Mathf.Min(Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MinRange, Unit.unitMeshManager.GetLeftHeldMeleeWeapon().ItemData.Item.Weapon.MinRange);
                else if (Unit.UnitEquipment.MeleeWeaponEquipped)
                    minAttackRange = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MinRange;

                if (distance < minAttackRange)
                    finalActionValue = 0f;
                else
                    finalActionValue -= distance * 10f;

                return new NPCAIAction
                {
                    baseAction = this,
                    actionGridPosition = targetUnit.GridPosition,
                    actionValue = Mathf.RoundToInt(finalActionValue)
                };
            }

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = Unit.GridPosition,
                actionValue = -1
            };
        }

        public override NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
        {
            float finalActionValue = 0f;

            // Make sure there's a Unit at this grid position
            if (LevelGrid.HasUnitAtGridPosition(actionGridPosition, out Unit unitAtGridPosition))
            {
                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                if (unitAtGridPosition.health.IsDead == false && Unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized * 70f);

                    // Favor the targetEnemyUnit
                    if (Unit.unitActionHandler.TargetEnemyUnit != null && unitAtGridPosition == Unit.unitActionHandler.TargetEnemyUnit)
                        finalActionValue += 15f;
                }
                else
                    finalActionValue = -1f;

                return new NPCAIAction
                {
                    baseAction = this,
                    actionGridPosition = actionGridPosition,
                    actionValue = Mathf.RoundToInt(finalActionValue)
                };
            }

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = Unit.GridPosition,
                actionValue = -1
            };
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && !unitAtGridPosition.health.IsDead && !Unit.alliance.IsAlly(unitAtGridPosition) && Unit.vision.IsDirectlyVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (Unit != null && (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.stats.CanFightUnarmed))
                return true;
            return false;
        }

        public override string TooltipDescription()
        {
            if (Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                return "Engage in <b>hand-to-hand</b> combat, delivering a swift and powerful strike to your target.";
            else if (Unit.UnitEquipment.IsDualWielding)
                return $"Deliver coordinated strikes with your <b>{Unit.unitMeshManager.rightHeldItem.ItemData.Item.Name}</b> and <b>{Unit.unitMeshManager.leftHeldItem.ItemData.Item.Name}</b>.";
            else
                return $"Deliver a decisive strike to your target using your <b>{Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Name}</b>.";
        }

        public override float AccuracyModifier() => 1f;

        public override int InitialEnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Special;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanAttackThroughUnits() => false;

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;

        public override float MinAttackRange()
        {
            if (Unit.UnitEquipment == null || Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                return 1f;
            else
            {
                HeldMeleeWeapon primaryHeldWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (Unit.UnitEquipment.IsDualWielding)
                {
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    return Mathf.Min(primaryHeldWeapon.ItemData.Item.Weapon.MinRange, secondaryHeldWeapon.ItemData.Item.Weapon.MinRange);
                }
                else if (primaryHeldWeapon != null)
                    return primaryHeldWeapon.ItemData.Item.Weapon.MinRange;
                else
                    return 1f;
            }
        }

        public override float MaxAttackRange()
        {
            if (Unit.UnitEquipment == null || Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                return Unit.stats.UnarmedAttackRange;
            else
            {
                HeldMeleeWeapon primaryHeldWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (Unit.UnitEquipment.IsDualWielding)
                {
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    return Mathf.Max(primaryHeldWeapon.ItemData.Item.Weapon.MaxRange, secondaryHeldWeapon.ItemData.Item.Weapon.MaxRange);
                }
                else if (primaryHeldWeapon != null)
                    return primaryHeldWeapon.ItemData.Item.Weapon.MaxRange;
                else
                    return Unit.stats.UnarmedAttackRange;
            }
        }
    }
}
