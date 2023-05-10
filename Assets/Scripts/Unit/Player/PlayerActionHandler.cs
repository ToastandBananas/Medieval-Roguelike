using System;
using UnityEngine;

public class PlayerActionHandler : UnitActionHandler
{
    public event EventHandler OnSelectedActionChanged;

    public override void Awake()
    {
        canPerformActions = true;
        base.Awake();
    }

    public override void TakeTurn()
    {
        if (unit.isMyTurn && unit.health.IsDead() == false)
        {
            unit.vision.FindVisibleUnitsAndObjects();

            if (canPerformActions == false)
            {
                TurnManager.Instance.FinishTurn(unit);
                return;
            }
            else if (targetInteractable != null)
            {
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetInteractable.gridPosition) <= 1.4f)
                {
                    GetAction<InteractAction>().SetTargetInteractableGridPosition(targetInteractable.gridPosition);
                    QueueAction(GetAction<InteractAction>());
                }
            }
            else if (queuedAction == null)
            {
                // If the queued attack is not a default attack
                if (queuedAttack != null && queuedAttack.IsDefaultAttackAction() == false)
                {
                    // If the target attack position is in range and there are valid units within the attack area
                    if (queuedAttack.IsInAttackRange(null, unit.gridPosition, targetAttackGridPosition) && queuedAttack.IsValidUnitInActionArea(targetAttackGridPosition))
                    {
                        if (queuedAttack.IsRangedAttackAction())
                        {
                            Unit closestEnemy = unit.vision.GetClosestEnemy(true);

                            // If the closest enemy or target attack positions are too close, cancel the Player's current action
                            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, closestEnemy.gridPosition) < 1.4f || TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetAttackGridPosition) < unit.GetRangedWeapon().itemData.item.Weapon().minRange)
                            {
                                CancelAction();
                                return;
                            }
                        }

                        // Queue the attack action
                        QueueAction(queuedAttack, targetAttackGridPosition);
                    }
                    else // If there's no unit in the attack area or the target attack position is out of range, cancel the action
                    {
                        CancelAction();
                        return;
                    }
                }
                // If there's a target enemy and either an attack wasn't queued, or the queued attack is a default attack
                else if (targetEnemyUnit != null)
                {
                    // If the target enemy is dead, cancel the action
                    if (targetEnemyUnit.health.IsDead())
                    {
                        CancelAction();
                        return;
                    }

                    // Handle default ranged attack
                    if (unit.RangedWeaponEquipped())
                    {
                        Unit closestEnemy = unit.vision.GetClosestEnemy(true);

                        // If the closest or target enemies are too close, cancel the Player's current action
                        if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, closestEnemy.gridPosition) < 1.4f || TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetEnemyUnit.gridPosition) < unit.GetRangedWeapon().itemData.item.Weapon().minRange)
                        {
                            CancelAction();
                            return;
                        }
                        else if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit))
                        {
                            // Shoot the target enemy
                            ClearActionQueue(true);
                            if (unit.GetRangedWeapon().isLoaded)
                                QueueAction(GetAction<ShootAction>(), targetEnemyUnit.gridPosition);
                            else
                                QueueAction(GetAction<ReloadAction>());
                        }
                        else // If they're out of the shoot range, move towards the enemy
                            QueueAction(GetAction<MoveAction>(), GetAction<ShootAction>().GetNearestAttackPosition(unit.gridPosition, targetEnemyUnit));
                    }
                    // Handle default melee attack
                    else if (unit.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed())
                    {
                        if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit))
                        {
                            // Melee attack the target enemy
                            ClearActionQueue(false);
                            QueueAction(GetAction<MeleeAction>(), targetEnemyUnit.gridPosition);
                        }
                        else // If they're out of melee range, move towards the enemy
                            QueueAction(GetAction<MoveAction>(), GetAction<MeleeAction>().GetNearestAttackPosition(unit.gridPosition, targetEnemyUnit));
                    }
                }
            }

            if (queuedAction != null)
                GetNextQueuedAction();
            else
            {
                unit.UnblockCurrentPosition();
                GridSystemVisual.UpdateGridVisual();
            }
        }
    }

    public override void SkipTurn()
    {
        lastQueuedAction = null;
        unit.stats.UseAP(unit.stats.APUntilTimeTick);
        TurnManager.Instance.FinishTurn(unit);
    }

    public override void GetNextQueuedAction()
    {
        if (unit.health.IsDead())
        {
            ClearActionQueue(true);
            GridSystemVisual.HideGridVisual();
            return;
        }

        if (queuedAction != null && isPerformingAction == false)
        {
            unit.stats.UseAP(queuedAP);

            if (queuedAction != null) // This can become null after a time tick update
            {
                isPerformingAction = true;
                queuedAction.TakeAction(targetGridPosition);
            }
            else
                CancelAction();
        }
    }

    public override void SetSelectedAction(BaseAction action)
    {
        base.SetSelectedAction(action);
        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }
}
