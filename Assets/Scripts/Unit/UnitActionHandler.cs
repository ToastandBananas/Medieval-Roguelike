using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitActionHandler : MonoBehaviour
{
    public GridPosition targetGridPosition { get; protected set; }

    /// <summary>The Units targeted by the last attack and the item they blocked with (if they successfully blocked).</summary>
    public Dictionary<Unit, HeldItem> targetUnits { get; private set; }

    public BaseAction queuedAction { get; private set; }
    public BaseAction queuedAttack { get; private set; }
    public int queuedAP { get; protected set; }

    public BaseAction[] baseActionArray { get; private set; }
    public List<BaseAction> combatActions = new List<BaseAction>();
    public BaseAction selectedAction { get; private set; }
    public BaseAction lastQueuedAction { get; protected set; }

    public Unit unit { get; private set; }
    public Unit targetEnemyUnit { get; protected set; }
    public GridPosition targetAttackGridPosition { get; protected set; }
    public Interactable targetInteractable { get; protected set; }
    public GridPosition previousTargetEnemyGridPosition { get; private set; }

    [SerializeField] LayerMask attackObstacleMask;

    public bool isPerformingAction { get; protected set; }
    public bool canPerformActions { get; protected set; }

    public virtual void Awake()
    {
        unit = GetComponent<Unit>(); 
        baseActionArray = GetComponents<BaseAction>();

        // Determine which BaseActions are combat actions and add them to the combatActions list
        for (int i = 0; i < baseActionArray.Length; i++)
        {
            if (baseActionArray[i].IsAttackAction())
                combatActions.Add(baseActionArray[i]);
        }

        targetGridPosition = LevelGrid.GetGridPosition(transform.position);
        targetUnits = new Dictionary<Unit, HeldItem>();

        // Default to the MoveAction
        SetSelectedAction(GetAction<MoveAction>());
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

    public abstract void GetNextQueuedAction();

    public abstract void TakeTurn();

    public abstract void SkipTurn();

    public virtual void FinishAction() => ClearActionQueue(false);

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

    public virtual void SetSelectedAction(BaseAction action) => selectedAction = action;

    public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

    public bool IsAttacking() => GetAction<MeleeAction>().isAttacking || GetAction<ShootAction>().isShooting;

    public LayerMask AttackObstacleMask() => attackObstacleMask;
}
