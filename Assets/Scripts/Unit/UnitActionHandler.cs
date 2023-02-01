using UnityEngine;

public class UnitActionHandler : MonoBehaviour
{
    public GridPosition targetGridPosition { get; protected set; }

    public BaseAction queuedAction { get; private set; }
    public int queuedAP { get; private set; }

    BaseAction[] baseActionArray;
    public BaseAction selectedAction { get; private set; }

    public Unit unit { get; private set; }
    public Unit targetEnemyUnit { get; protected set; }
    public GridPosition previousTargetEnemyGridPosition { get; private set; }

    [SerializeField] LayerMask shootObstacleMask;

    public bool isPerformingAction { get; private set; }
    public bool canPerformActions { get; protected set; }

    void Awake()
    {
        unit = GetComponent<Unit>(); 
        baseActionArray = GetComponents<BaseAction>();

        if (unit.IsPlayer()) canPerformActions = true;

        SetSelectedAction(GetAction<MoveAction>());
    }

    public virtual void TakeTurn()
    {
        if (unit.isMyTurn && unit.health.IsDead() == false)
        {
            if (canPerformActions == false || unit.stats.CurrentAP() <= 0)
            {
                TurnManager.Instance.FinishTurn(unit);
                return;
            }
            else if (targetEnemyUnit != null && queuedAction == null)
            {
                if (unit.RangedWeaponEquipped())
                {
                    if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit))
                    {
                        ClearActionQueue(); 
                        
                        if (unit.GetEquippedRangedWeapon().isLoaded)
                            QueueAction(GetAction<ShootAction>(), targetEnemyUnit.gridPosition);
                        else
                            QueueAction(GetAction<ReloadAction>(), targetEnemyUnit.gridPosition);
                        return;
                    }
                }
                else if (unit.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed())
                {
                    if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit))
                    {
                        ClearActionQueue();
                        QueueAction(GetAction<MeleeAction>(), targetEnemyUnit.gridPosition);
                        return;
                    }
                }
            }

            if (queuedAction != null)
                GetNextQueuedAction();
            else
                unit.UnblockCurrentPosition();
        }
    }

    #region Action Queue
    public void QueueAction(BaseAction action, GridPosition targetGridPosition)
    {
        // if (isNPC) Debug.Log(name + " queued " + action);
        queuedAction = action;
        queuedAP = action.GetActionPointsCost(targetGridPosition);

        if (unit.isMyTurn)
        {
            if (canPerformActions == false)
                TurnManager.Instance.FinishTurn(unit);
            else if (GetAction<MoveAction>().isMoving == false && unit.stats.CurrentAP() > 0)
                GetNextQueuedAction();
        }

        // Update AP text
        //if (IsNPC() == false)
        //APManager.Instance.UpdateLastAPUsed(APCost);
    }

    public void GetNextQueuedAction()
    {
        if (unit.health.IsDead())
        {
            ClearActionQueue();
            return;
        }

        if (queuedAction != null && isPerformingAction == false)
        {
            int APRemainder = unit.stats.UseAPAndGetRemainder(queuedAP);
            if (APRemainder <= 0)
            {
                isPerformingAction = true;
                queuedAction.TakeAction(targetGridPosition, null);
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

    public virtual void FinishAction()
    {
        ClearActionQueue();

        // If the character has no AP remaining, end their turn
        if (unit.stats.CurrentAP() <= 0)
        {
            if (unit.isMyTurn)
                TurnManager.Instance.FinishTurn(unit);
        }
    }

    public void CancelAction()
    {
        ClearActionQueue();

        MoveAction moveAction = GetAction<MoveAction>();
        if (moveAction.finalTargetGridPosition != unit.gridPosition)
            moveAction.SetFinalTargetGridPosition(moveAction.nextTargetGridPosition);

        // If the Unit isn't moving, they might still be in a move animation, so cancel that
        if (moveAction.isMoving == false)
            unit.unitAnimator.StopMovingForward();

        unit.unitActionHandler.SetTargetEnemyUnit(null);
    }

    public void ClearActionQueue()
    {
        // Debug.Log("Clearing action queue");
        queuedAction = null;
        queuedAP = 0;
        isPerformingAction = false;
    }
    #endregion

    #region Combat
    public void AttackTargetEnemy()
    {
        if (GetAction<TurnAction>().IsFacingTarget(targetEnemyUnit.gridPosition))
        {
            if (unit.RangedWeaponEquipped())
            {
                if (unit.GetEquippedRangedWeapon().isLoaded)
                    QueueAction(GetAction<ShootAction>(), targetEnemyUnit.gridPosition);
                else
                    QueueAction(GetAction<ReloadAction>(), targetEnemyUnit.gridPosition);
            }
            else
                QueueAction(GetAction<MeleeAction>(), targetEnemyUnit.gridPosition);
        }
        else
        {
            GetAction<TurnAction>().SetTargetPosition(GetAction<TurnAction>().targetDirection);
            QueueAction(GetAction<TurnAction>(), targetEnemyUnit.gridPosition);
        }
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

    public void SetTargetEnemyUnit(Unit target)
    {
        if (target == null && unit.IsPlayer())
            Debug.Log("Setting target enemy to NULL");
        targetEnemyUnit = target;
        if (target != null)
            previousTargetEnemyGridPosition = target.gridPosition;
    }

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public void SetSelectedAction(BaseAction action) => selectedAction = action;

    public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

    public LayerMask ShootObstacleMask() => shootObstacleMask;
}
