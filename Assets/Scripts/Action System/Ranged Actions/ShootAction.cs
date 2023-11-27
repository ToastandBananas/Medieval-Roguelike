using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;

namespace UnitSystem.ActionSystem
{
    public class ShootAction : BaseAttackAction
    {
        readonly int baseAPCost = 300;

        public override void QueueAction()
        {
            if (!RangedWeaponIsLoaded())
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
            else
                base.QueueAction();
        }

        public override void QueueAction(GridPosition targetGridPosition)
        {
            if (!RangedWeaponIsLoaded())
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
            else
                base.QueueAction(targetGridPosition);
        }

        public override void QueueAction(Unit targetEnemyUnit)
        {
            if (!RangedWeaponIsLoaded())
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
            else
                base.QueueAction(targetEnemyUnit);
        }

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);
            SetTargetEnemyUnit();
        }

        public override void TakeAction()
        {
            if (unit.unitActionHandler.isAttacking) return;

            if (TargetEnemyUnit == null)
            {
                if (unit.unitActionHandler.targetEnemyUnit != null)
                    TargetEnemyUnit = unit.unitActionHandler.targetEnemyUnit;
                else if (LevelGrid.HasUnitAtGridPosition(targetGridPosition, out Unit targetUnit))
                    TargetEnemyUnit = targetUnit;
            }

            if (TargetEnemyUnit == null || TargetEnemyUnit.health.IsDead)
            {
                TargetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }

            if (!RangedWeaponIsLoaded())
            {
                CompleteAction();
                unit.unitActionHandler.SetTargetEnemyUnit(TargetEnemyUnit);

                // ReloadAction can become null if the Unit drops their weapon or switches their loadout
                ReloadAction reloadAction = unit.unitActionHandler.GetAction<ReloadAction>();
                if (reloadAction != null)
                    reloadAction.QueueAction();
                else
                    TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }
            else if (IsInAttackRange(TargetEnemyUnit, unit.GridPosition, TargetEnemyUnit.GridPosition))
            {
                StartAction();
                unit.StartCoroutine(DoAttack());
            }
            else
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }
        }

        protected override IEnumerator DoAttack()
        {
            while (TargetEnemyUnit.unitActionHandler.moveAction.isMoving || TargetEnemyUnit.unitAnimator.beingKnockedBack)
                yield return null;

            // If the target Unit moved out of range, queue a movement instead
            if (!IsInAttackRange(TargetEnemyUnit, unit.GridPosition, TargetEnemyUnit.GridPosition))
            {
                MoveToTargetInstead();
                yield break;
            }

            // The unit being attacked becomes aware of this unit
            unit.vision.BecomeVisibleUnitOfTarget(TargetEnemyUnit, true);

            // We need to skip a frame in case the target Unit's meshes are being enabled
            yield return null;

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (unit.IsPlayer || TargetEnemyUnit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen || TargetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (!unit.unitActionHandler.turnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.turnAction.isRotating)
                    yield return null;
                
                // If the target Unit moved out of range, queue a movement instead
                if (!IsInAttackRange(TargetEnemyUnit, unit.GridPosition, targetGridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }

                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                if (TargetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, heldRangedWeapon, false))
                {
                    // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
                    TargetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, false);
                    unit.unitActionHandler.targetUnits.TryGetValue(TargetEnemyUnit, out HeldItem itemBlockedWith);
                    if (itemBlockedWith != null)
                        itemBlockedWith.BlockAttack(unit);
                }

                // Rotate towards the target and do the shoot animation
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                bool missedTarget = TryHitTarget(TargetEnemyUnit.GridPosition);
                bool attackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, heldRangedWeapon, false);
                bool headShot = false;
                if (!missedTarget)
                    DamageTargets(heldRangedWeapon, heldRangedWeapon.LoadedProjectile.ItemData, headShot);

                // Rotate towards the target
                if (!unit.unitActionHandler.turnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(TargetEnemyUnit, true);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    TargetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                heldRangedWeapon.RemoveProjectile();
                unit.unitActionHandler.SetIsAttacking(false);
            }

            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        public override void PlayAttackAnimation()
        {
            unit.unitActionHandler.turnAction.RotateTowardsAttackPosition(TargetEnemyUnit.WorldPosition);
            unit.unitMeshManager.GetHeldRangedWeapon().DoDefaultAttack(targetGridPosition);
        }

        public bool TryHitTarget(GridPosition targetGridPosition)
        {
            float random = Random.Range(0f, 1f);
            float rangedAccuracy = unit.stats.RangedAccuracy(unit.unitMeshManager.GetHeldRangedWeapon(), targetGridPosition, this);
            if (random <= rangedAccuracy)
                return true;
            return false;
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the projectile hits something, rather than in this method as is with non-ranged actions
            base.CompleteAction();
            if (unit.IsPlayer)
            {
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                if (!PlayerInput.Instance.AutoAttack)
                {
                    TargetEnemyUnit = null;
                    unit.unitActionHandler.SetTargetEnemyUnit(null);
                }
                
                // ReloadAction can become null if the Unit drops their weapon or switches their loadout
                ReloadAction reloadAction = unit.unitActionHandler.GetAction<ReloadAction>();
                if (reloadAction != null)
                    reloadAction.actionBarSlot.UpdateIcon();
            }

            unit.unitActionHandler.FinishAction();
        }

        public override int ActionPointsCost()
        {
            float cost = baseAPCost * ActionPointCostModifier_WeaponType(unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon);

            // If not facing the target position, add the cost of turning towards that position
            unit.unitActionHandler.turnAction.DetermineTargetTurnDirection(targetGridPosition);
            cost += unit.unitActionHandler.turnAction.ActionPointsCost();
            return Mathf.RoundToInt(cost);
        }

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && !targetUnit.health.IsDead)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized * 100f);
                float distance = Vector3.Distance(unit.WorldPosition, targetUnit.WorldPosition);
                if (distance < unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
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
                actionGridPosition = unit.GridPosition,
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
                if (!unitAtGridPosition.health.IsDead && unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized * 70f);

                    // Favor the targetEnemyUnit
                    if (unit.unitActionHandler.targetEnemyUnit != null && unitAtGridPosition == unit.unitActionHandler.targetEnemyUnit)
                        finalActionValue += 15f;

                    float distance = Vector3.Distance(unit.WorldPosition, actionGridPosition.WorldPosition);
                    if (distance < unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
                        finalActionValue = -1f;
                    else
                        finalActionValue -= distance * 1.5f;
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
                actionGridPosition = unit.GridPosition,
                actionValue = -1
            };
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && !unitAtGridPosition.health.IsDead && !unit.alliance.IsAlly(unitAtGridPosition) && unit.vision.IsDirectlyVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (unit != null && unit.UnitEquipment.RangedWeaponEquipped && unit.UnitEquipment.HasValidAmmunitionEquipped())
                return true;
            return false;
        }

        public override string TooltipDescription()
        {
            RangedWeapon rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.RangedWeapon;
            if (rangedWeapon.WeaponType == WeaponType.Bow)
                return $"Draw your <b>{rangedWeapon.Name}'s</b> bowstring taut and release a deadly arrow towards your target.";
            else if (rangedWeapon.WeaponType == WeaponType.Crossbow)
                return $"Ready your <b>{rangedWeapon.Name}</b> and release a deadly bolt towards your target.";
            return "";
        }

        public bool RangedWeaponIsLoaded() => unit.UnitEquipment.RangedWeaponEquipped && unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded;

        public override float AccuracyModifier() => 1f;

        public override int InitialEnergyCost() => 0;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Special;

        public override bool IsInterruptable() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool ActionIsUsedInstantly() => false;

        public override bool IsMeleeAttackAction() => false;

        public override bool IsRangedAttackAction() => true;

        public override float MinAttackRange()
        {
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon != null)
                return heldRangedWeapon.ItemData.Item.Weapon.MinRange;
            else
                return 100f;
        }

        public override float MaxAttackRange()
        {
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon != null)
                return heldRangedWeapon.ItemData.Item.Weapon.MaxRange;
            else
                return 0f;
        }

        public override bool CanAttackThroughUnits()
        {
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon != null)
            {
                if (heldRangedWeapon.ItemData.Item.Weapon.WeaponType == WeaponType.Bow)
                    return true;
                else
                    return false;
            }
            else
                return true;
        }
    }
}
