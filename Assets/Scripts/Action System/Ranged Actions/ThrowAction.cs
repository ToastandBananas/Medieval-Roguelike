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
        public ItemData ItemDataToThrow { get; private set; }

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
            if (ItemDataToThrow == null || !IsInAttackRange(null, unit.GridPosition, targetGridPosition))
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
            if (TargetEnemyUnit != null)
            {
                while (TargetEnemyUnit.unitActionHandler.moveAction.isMoving || TargetEnemyUnit.unitAnimator.beingKnockedBack)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (!IsInAttackRange(TargetEnemyUnit, unit.GridPosition, TargetEnemyUnit.GridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }
            }

            if (unit.IsPlayer || TargetEnemyUnit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen || TargetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (!unit.unitActionHandler.turnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.turnAction.isRotating)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (TargetEnemyUnit != null && !IsInAttackRange(TargetEnemyUnit, unit.GridPosition, targetGridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }

                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                if (TargetEnemyUnit != null && TargetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, null, false))
                {
                    // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
                    TargetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, false);
                    unit.unitActionHandler.targetUnits.TryGetValue(TargetEnemyUnit, out HeldItem itemBlockedWith);
                    if (itemBlockedWith != null)
                        itemBlockedWith.BlockAttack(unit);
                }

                // Rotate towards the target and do the throw animation
                PlayAttackAnimation();
            }
            else
            {
                // Rotate towards the target
                if (TargetEnemyUnit != null && !unit.unitActionHandler.turnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(TargetEnemyUnit, true);

                bool hitTarget = TryHitTarget(ItemDataToThrow, targetGridPosition);

                bool attackBlocked = false;
                if (TargetEnemyUnit != null)
                    attackBlocked = TargetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, null, false);

                bool headShot = false;
                if (hitTarget)
                    DamageTargets(null, ItemDataToThrow, headShot);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    TargetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

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
            unit.unitActionHandler.turnAction.RotateTowardsAttackPosition(TargetEnemyUnit.WorldPosition);
            if (unit.unitMeshManager.rightHeldItem != null && ItemDataToThrow == unit.unitMeshManager.rightHeldItem.ItemData)
                unit.unitMeshManager.rightHeldItem.StartThrow();
            else if (unit.unitMeshManager.leftHeldItem != null && ItemDataToThrow == unit.unitMeshManager.leftHeldItem.ItemData)
                unit.unitMeshManager.leftHeldItem.StartThrow();
            else if (ItemDataToThrow.MyInventory != null)
            {

            }
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the thrown item hits something, rather than in this method as is with non-ranged actions
            base.CompleteAction();
            if (unit.IsPlayer)
            {
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                TargetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
            }

            unit.unitActionHandler.FinishAction();
        }

        public override bool IsValidAction() => ItemDataToThrow != null || unit.UnitEquipment.MeleeWeaponEquipped;

        public override int InitialEnergyCost()
        {
            return 10;
        }

        public override string TooltipDescription()
        {
            return "Throw an item.";
        }

        public void SetItemToThrow(ItemData itemDataToThrow) => ItemDataToThrow = itemDataToThrow;

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
