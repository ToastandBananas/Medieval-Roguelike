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
                Unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
            else
                base.QueueAction();
        }

        public override void QueueAction(GridPosition targetGridPosition)
        {
            if (!RangedWeaponIsLoaded())
                Unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
            else
                base.QueueAction(targetGridPosition);
        }

        public override void QueueAction(Unit targetEnemyUnit)
        {
            if (!RangedWeaponIsLoaded())
                Unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
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
            if (Unit.unitActionHandler.IsAttacking) return;

            if (TargetEnemyUnit == null)
            {
                if (Unit.unitActionHandler.TargetEnemyUnit != null)
                    TargetEnemyUnit = Unit.unitActionHandler.TargetEnemyUnit;
                else if (LevelGrid.HasUnitAtGridPosition(TargetGridPosition, out Unit targetUnit))
                    TargetEnemyUnit = targetUnit;
            }

            if (TargetEnemyUnit == null || TargetEnemyUnit.health.IsDead)
            {
                TargetEnemyUnit = null;
                Unit.unitActionHandler.SetTargetEnemyUnit(null);
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(Unit);
                return;
            }

            if (!RangedWeaponIsLoaded())
            {
                CompleteAction();
                Unit.unitActionHandler.SetTargetEnemyUnit(TargetEnemyUnit);

                // ReloadAction can become null if the Unit drops their weapon or switches their loadout
                ReloadAction reloadAction = Unit.unitActionHandler.GetAction<ReloadAction>();
                if (reloadAction != null)
                    reloadAction.QueueAction();
                else
                    TurnManager.Instance.StartNextUnitsTurn(Unit);
                return;
            }
            else if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
            {
                StartAction();
                Unit.StartCoroutine(DoAttack());
            }
            else
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(Unit);
                return;
            }
        }

        protected override IEnumerator DoAttack()
        {
            while (TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving || TargetEnemyUnit.unitAnimator.beingKnockedBack)
                yield return null;

            // If the target Unit moved out of range, queue a movement instead
            if (!IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
            {
                MoveToTargetInstead();
                yield break;
            }

            // The unit being attacked becomes aware of this unit
            Unit.vision.BecomeVisibleUnitOfTarget(TargetEnemyUnit, true);

            // We need to skip a frame in case the target Unit's meshes are being enabled
            yield return null;

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            HeldRangedWeapon heldRangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
            if (Unit.IsPlayer || TargetEnemyUnit.IsPlayer || Unit.unitMeshManager.IsVisibleOnScreen || TargetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (!Unit.unitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    Unit.unitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (Unit.unitActionHandler.TurnAction.isRotating)
                    yield return null;
                
                // If the target Unit moved out of range, queue a movement instead
                if (!IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetGridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }

                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                if (TargetEnemyUnit.unitActionHandler.TryBlockRangedAttack(Unit, heldRangedWeapon, false))
                {
                    // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
                    TargetEnemyUnit.unitActionHandler.TurnAction.RotateTowards_Unit(Unit, false);
                    Unit.unitActionHandler.targetUnits.TryGetValue(TargetEnemyUnit, out HeldItem itemBlockedWith);
                    if (itemBlockedWith != null)
                        itemBlockedWith.BlockAttack(Unit);
                }

                // Rotate towards the target and do the shoot animation
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                bool missedTarget = TryHitTarget(TargetEnemyUnit.GridPosition);
                bool attackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockRangedAttack(Unit, heldRangedWeapon, false);
                bool headShot = false;
                if (!missedTarget)
                    DamageTargets(heldRangedWeapon, heldRangedWeapon.LoadedProjectile.ItemData, headShot);

                // Rotate towards the target
                if (!Unit.unitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    Unit.unitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, true);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    TargetEnemyUnit.unitActionHandler.TurnAction.RotateTowards_Unit(Unit, true);

                heldRangedWeapon.RemoveProjectile();
                Unit.unitActionHandler.SetIsAttacking(false);
            }

            while (Unit.unitActionHandler.IsAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        public override void PlayAttackAnimation()
        {
            Unit.unitActionHandler.TurnAction.RotateTowardsAttackPosition(TargetEnemyUnit.WorldPosition);
            Unit.unitMeshManager.GetHeldRangedWeapon().DoDefaultAttack(TargetGridPosition);
        }

        public bool TryHitTarget(GridPosition targetGridPosition)
        {
            float random = Random.Range(0f, 1f);
            float rangedAccuracy = Unit.stats.RangedAccuracy(Unit.unitMeshManager.GetHeldRangedWeapon(), targetGridPosition, this);
            if (random <= rangedAccuracy)
                return true;
            return false;
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the projectile hits something, rather than in this method as is with non-ranged actions
            base.CompleteAction();
            if (Unit.IsPlayer)
            {
                Unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                if (!PlayerInput.Instance.AutoAttack)
                {
                    TargetEnemyUnit = null;
                    Unit.unitActionHandler.SetTargetEnemyUnit(null);
                }
                
                // ReloadAction can become null if the Unit drops their weapon or switches their loadout
                ReloadAction reloadAction = Unit.unitActionHandler.GetAction<ReloadAction>();
                if (reloadAction != null)
                    reloadAction.ActionBarSlot.UpdateIcon();
            }

            Unit.unitActionHandler.FinishAction();
        }

        public override int ActionPointsCost()
        {
            float cost = baseAPCost * ActionPointCostModifier_WeaponType(Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon);

            // If not facing the target position, add the cost of turning towards that position
            Unit.unitActionHandler.TurnAction.DetermineTargetTurnDirection(TargetGridPosition);
            cost += Unit.unitActionHandler.TurnAction.ActionPointsCost();
            return Mathf.RoundToInt(cost);
        }

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && !targetUnit.health.IsDead)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized * 100f);
                float distance = Vector3.Distance(Unit.WorldPosition, targetUnit.WorldPosition);
                if (distance < Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
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
                if (!unitAtGridPosition.health.IsDead && Unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized * 70f);

                    // Favor the targetEnemyUnit
                    if (Unit.unitActionHandler.TargetEnemyUnit != null && unitAtGridPosition == Unit.unitActionHandler.TargetEnemyUnit)
                        finalActionValue += 15f;

                    float distance = Vector3.Distance(Unit.WorldPosition, actionGridPosition.WorldPosition);
                    if (distance < Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
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
            if (Unit != null && Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
                return true;
            return false;
        }

        public override string TooltipDescription()
        {
            RangedWeapon rangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.RangedWeapon;
            if (rangedWeapon.WeaponType == WeaponType.Bow)
                return $"Draw your <b>{rangedWeapon.Name}'s</b> bowstring taut and release a deadly arrow towards your target.";
            else if (rangedWeapon.WeaponType == WeaponType.Crossbow)
                return $"Ready your <b>{rangedWeapon.Name}</b> and release a deadly bolt towards your target.";
            return "";
        }

        public bool RangedWeaponIsLoaded() => Unit.UnitEquipment.RangedWeaponEquipped && Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded;

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
            HeldRangedWeapon heldRangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon != null)
                return heldRangedWeapon.ItemData.Item.Weapon.MinRange;
            else
                return 100f;
        }

        public override float MaxAttackRange()
        {
            HeldRangedWeapon heldRangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon != null)
                return heldRangedWeapon.ItemData.Item.Weapon.MaxRange;
            else
                return 0f;
        }

        public override bool CanAttackThroughUnits()
        {
            HeldRangedWeapon heldRangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
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
