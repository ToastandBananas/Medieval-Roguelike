using GridSystem;
using InventorySystem;
using System.Collections;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using ContextMenu = GeneralUI.ContextMenu;

namespace UnitSystem.ActionSystem
{
    public class ThrowAction : BaseAttackAction
    {
        ItemData itemDataToThrow;

        readonly int minThrowDistance = 2;
        readonly int defaultMaxThrowDistance = 6;

        public override void OnActionSelected()
        {
            ContextMenu.BuildThrowWeaponContextMenu();
        }

        public override int ActionPointsCost()
        {
            return 200;
        }

        public override void TakeAction()
        {
            if (itemDataToThrow == null || !IsInAttackRange(null, unit.GridPosition, targetGridPosition))
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }

            StartAction();
            unit.StartCoroutine(DoAttack());
        }

        protected override IEnumerator DoAttack()
        {
            if (targetEnemyUnit != null)
            {
                while (targetEnemyUnit.unitActionHandler.moveAction.isMoving || targetEnemyUnit.unitAnimator.beingKnockedBack)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (!IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }
            }

            if (unit.IsPlayer || targetEnemyUnit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen || targetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (!unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition))
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.turnAction.isRotating)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (targetEnemyUnit != null && !IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetGridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }

                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                if (targetEnemyUnit != null && targetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, null, false))
                {
                    // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
                    targetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, false);
                    unit.unitActionHandler.targetUnits.TryGetValue(targetEnemyUnit, out HeldItem itemBlockedWith);
                    if (itemBlockedWith != null)
                        itemBlockedWith.BlockAttack(unit);
                }

                // Rotate towards the target and do the throw animation
                PlayAttackAnimation();
            }
            else
            {
                // Rotate towards the target
                if (targetEnemyUnit != null && !unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition))
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, true);

                bool missedTarget = TryHitTarget(itemDataToThrow, targetGridPosition);

                bool attackBlocked = false;
                if (targetEnemyUnit != null)
                    attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, null, false);

                bool headShot = false;
                if (!missedTarget)
                    DamageTargets(null, headShot);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    targetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                unit.unitActionHandler.SetIsAttacking(false);
            }

            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        public bool TryHitTarget(ItemData itemDataToThrow, GridPosition targetGridPosition)
        {
            float random = Random.Range(0f, 1f);
            float rangedAccuracy = unit.stats.ThrowingAccuracy(itemDataToThrow, targetGridPosition, this);
            if (random <= rangedAccuracy)
                return true;
            return false;
        }

        public override void PlayAttackAnimation()
        {
            throw new System.NotImplementedException();
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the thrown item hits something, rather than in this method as is with non-ranged actions
            base.CompleteAction();
            if (unit.IsPlayer)
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

            unit.unitActionHandler.FinishAction();
        }

        public override bool IsValidAction()
        {
            return true;
        }

        public override int InitialEnergyCost()
        {
            return 10;
        }

        public override string TooltipDescription()
        {
            return "Throw an item.";
        }

        public void SetItemToThrow(ItemData itemDataToThrow) => this.itemDataToThrow = itemDataToThrow;

        public override float AccuracyModifier() => 1f;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool ActionIsUsedInstantly() => false;

        public override bool IsMeleeAttackAction() => false;

        public override bool IsRangedAttackAction() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;

        public override float MinAttackRange()
        {
            return minThrowDistance;
        }

        public override float MaxAttackRange()
        {
            return defaultMaxThrowDistance;
        }

        public override bool CanAttackThroughUnits()
        {
            return false;
        }
    }
}
