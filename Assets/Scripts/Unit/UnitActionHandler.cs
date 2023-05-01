using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitActionHandler : MonoBehaviour
{
    public event EventHandler OnSelectedActionChanged;

    public GridPosition targetGridPosition { get; protected set; }

    /// <summary>The Units targeted by the last attack and the item they blocked with (if they successfully blocked).</summary>
    public Dictionary<Unit, HeldItem> targetUnits { get; private set; }

    public BaseAction queuedAction { get; private set; }
    public BaseAction queuedAttack { get; private set; }
    public int queuedAP { get; private set; }

    public BaseAction[] baseActionArray { get; private set; }
    public BaseAction selectedAction { get; private set; }
    public BaseAction lastQueuedAction { get; private set; }

    public Unit unit { get; private set; }
    public Unit targetEnemyUnit { get; protected set; }
    public GridPosition targetAttackGridPosition { get; protected set; }
    public Interactable targetInteractable { get; protected set; }
    public GridPosition previousTargetEnemyGridPosition { get; private set; }

    [SerializeField] LayerMask attackObstacleMask;

    public bool isPerformingAction { get; private set; }
    public bool canPerformActions { get; protected set; }

    void Awake()
    {
        unit = GetComponent<Unit>(); 
        baseActionArray = GetComponents<BaseAction>();

        if (unit.IsPlayer()) canPerformActions = true;

        targetGridPosition = LevelGrid.GetGridPosition(transform.position);
        targetUnits = new Dictionary<Unit, HeldItem>();

        SetSelectedAction(GetAction<MoveAction>());
    }

    public virtual void TakeTurn()
    {
        if (unit.isMyTurn && unit.health.IsDead() == false)
        {
            unit.vision.FindVisibleUnits();

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
                // Debug.Log(queuedAttack);
                // If the queued attack is not a default attack
                if (queuedAttack != null && queuedAttack.IsDefaultAttackAction() == false)
                {
                    Debug.Log(unit.name + " is trying to attack grid position with a queuedAttack!");
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
            //else if (queuedAction == null && targetGridPosition != unit.gridPosition)
            //QueueAction(GetAction<MoveAction>());

            if (queuedAction != null)
                GetNextQueuedAction();
            else
            {
                unit.UnblockCurrentPosition();
                GridSystemVisual.UpdateGridVisual();
            }
        }
    }

    #region Action Queue
    public void QueueAction(BaseAction action, GridPosition targetGridPosition)
    {
        SetTargetGridPosition(targetGridPosition);
        QueueAction(action);
    }

    public void QueueAction(BaseAction action)
    {
        GridSystemVisual.HideGridVisual();

        // if (unit.IsPlayer()) Debug.Log(name + " queued " + action);
        queuedAction = action;
        lastQueuedAction = action;
        queuedAP = action.GetActionPointsCost();

        // If the action changed while getting the action point cost (such as when running into a door)
        if (action != queuedAction)
            return;

        if (unit.isMyTurn)
        {
            if (canPerformActions == false)
                TurnManager.Instance.FinishTurn(unit);
            else if (GetAction<MoveAction>().isMoving == false)
                GetNextQueuedAction();
        }

        SetSelectedAction(GetAction<MoveAction>());
    }

    public void GetNextQueuedAction()
    {
        if (unit.health.IsDead())
        {
            ClearActionQueue(true);
            if (unit.IsPlayer())
                GridSystemVisual.HideGridVisual();
            return;
        }

        if (queuedAction != null && isPerformingAction == false)
        {
            if (unit.IsPlayer())
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
            else
            {
                int APRemainder = unit.stats.UseAPAndGetRemainder(queuedAP);
                if (unit.health.IsDead())
                {
                    ClearActionQueue(true);
                    if (unit.IsPlayer())
                        GridSystemVisual.HideGridVisual();
                    return;
                }

                if (APRemainder <= 0)
                {
                    if (queuedAction != null) // This can become null after a time tick update
                    {
                        isPerformingAction = true;
                        queuedAction.TakeAction(targetGridPosition);
                    }
                    else
                    {
                        CancelAction();
                        TurnManager.Instance.FinishTurn(unit);
                    }
                    // if (isNPC == false) Debug.Log("Got next queued action. Actions still queued: " + actions.Count);
                }
                else
                {
                    // if (isNPC == false) Debug.Log("Can't do next queued action yet. Remaining AP: " + APRemainder);
                    isPerformingAction = false;
                    queuedAP = APRemainder;
                    TurnManager.Instance.FinishTurn(unit);
                }
            }
        }
        else if (queuedAction == null && unit.IsNPC())
        {
            Debug.Log("Queued action is null for " + unit.name);
            TurnManager.Instance.FinishTurn(unit);
        }
    }

    public virtual void FinishAction()
    {
        ClearActionQueue(false);
    }

    public virtual void SkipTurn()
    {
        lastQueuedAction = null;
        unit.stats.UseAP(unit.stats.APUntilTimeTick);
        TurnManager.Instance.FinishTurn(unit);
    }

    public IEnumerator CancelAction()
    {
        MoveAction moveAction = GetAction<MoveAction>();
        if (queuedAction != moveAction)
        {
            while (isPerformingAction)
            {
                yield return null;
            }
        }

        ClearActionQueue(true);
        SetTargetInteractable(null);
        SetTargetEnemyUnit(null);
        SetQueuedAttack(null);

        if (moveAction.finalTargetGridPosition != unit.gridPosition)
        {
            SetTargetGridPosition(moveAction.nextTargetGridPosition);
            moveAction.SetFinalTargetGridPosition(moveAction.nextTargetGridPosition);
        }


        GridSystemVisual.UpdateGridVisual();
    }

    public void ClearActionQueue(bool stopMoveAnimation)
    {
        if (queuedAction != null && queuedAction.IsAttackAction())
            queuedAttack = null;

        // Debug.Log("Clearing action queue");
        queuedAction = null;
        queuedAP = 0;
        isPerformingAction = false;

        // If the Unit isn't moving, they might still be in a move animation, so cancel that
        if (stopMoveAnimation && GetAction<MoveAction>().isMoving == false)
            unit.unitAnimator.StopMovingForward();
    }

    public bool AttackQueued() => queuedAction is MeleeAction || queuedAction is ShootAction;
    #endregion

    #region Combat
    public void AttackTargetGridPosition()
    {
        if (GetAction<TurnAction>().IsFacingTarget(targetAttackGridPosition))
        {
            if (selectedAction.IsAttackAction())
                QueueAction(selectedAction, targetAttackGridPosition);
            else if (unit.RangedWeaponEquipped())
            {
                if (unit.GetRangedWeapon().isLoaded)
                    QueueAction(GetAction<ShootAction>(), targetAttackGridPosition);
                else
                    QueueAction(GetAction<ReloadAction>());
            }
            else if (unit.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed())
                QueueAction(GetAction<MeleeAction>(), targetAttackGridPosition);
        }
        else
        {
            SetQueuedAttack(selectedAction);
            QueueAction(GetAction<TurnAction>());
        }
    }

    public bool IsInAttackRange(Unit targetUnit)
    {
        if ((selectedAction.IsAttackAction() && selectedAction.IsInAttackRange(targetUnit)) 
            || (unit.RangedWeaponEquipped() && GetAction<ShootAction>().IsInAttackRange(targetUnit)) 
            || ((unit.MeleeWeaponEquipped() || (unit.RangedWeaponEquipped() == false && GetAction<MeleeAction>().CanFightUnarmed())) && GetAction<MeleeAction>().IsInAttackRange(targetUnit)))
            return true;
        return false;
    }
    #endregion

    public T GetAction<T>() where T : BaseAction
    {
        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction is T)
                return (T)baseAction;
        }
        return null;
    }

    public void SetPreviousTargetEnemyGridPosition(GridPosition newGridPosition) => previousTargetEnemyGridPosition = newGridPosition;

    public virtual void SetTargetEnemyUnit(Unit target)
    {
        targetEnemyUnit = target;
        if (target != null)
        {
            targetAttackGridPosition = target.gridPosition;
            previousTargetEnemyGridPosition = target.gridPosition;
        }
    }

    public void SetTargetInteractable(Interactable interactable) => targetInteractable = interactable;

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public void SetTargetAttackGridPosition(GridPosition targetAttackGridPosition) => this.targetAttackGridPosition = targetAttackGridPosition;

    public void SetQueuedAttack(BaseAction attackAction)
    {
        if (attackAction != null && attackAction.IsAttackAction())
            queuedAttack = attackAction;
        else
            queuedAttack = null;
    }

    public void SetSelectedAction(BaseAction action)
    {
        selectedAction = action;
        if (unit.IsPlayer())
            OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

    public bool IsAttacking() => GetAction<MeleeAction>().isAttacking || GetAction<ShootAction>().isShooting;

    public LayerMask AttackObstacleMask() => attackObstacleMask;
}
