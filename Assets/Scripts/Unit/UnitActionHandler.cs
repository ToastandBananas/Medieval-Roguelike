using System.Collections;
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
                    Unit closestEnemy = unit.vision.GetClosestEnemy(true);

                    // If the closest enemy is too close, cancel the Player's current action
                    if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, closestEnemy.gridPosition) < unit.GetRangedWeapon().itemData.item.Weapon().minRange)
                    {
                        CancelAction();
                        return;
                    }
                    else if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit))
                    {
                        // Shoot the target enemy
                        ClearActionQueue(true);
                        if (unit.GetRangedWeapon().isLoaded)
                            QueueAction(GetAction<ShootAction>());
                        else
                            QueueAction(GetAction<ReloadAction>());
                        return;
                    }
                    else // If they're out of the shoot range, move towards the enemy
                    {
                        SetTargetGridPosition(LevelGrid.Instance.GetNearestSurroundingGridPosition(targetEnemyUnit.gridPosition, unit.gridPosition));
                        QueueAction(GetAction<MoveAction>());
                    }
                }
                else if (unit.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed())
                {
                    if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit))
                    {
                        // Melee attack the target enemy
                        ClearActionQueue(false);
                        QueueAction(GetAction<MeleeAction>());
                        return;
                    }
                    else // If they're out of melee range, move towards the enemy
                    {
                        SetTargetGridPosition(LevelGrid.Instance.GetNearestSurroundingGridPosition(targetEnemyUnit.gridPosition, unit.gridPosition));
                        QueueAction(GetAction<MoveAction>());
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
    public void QueueAction(BaseAction action)
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
            ClearActionQueue(true);
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
        ClearActionQueue(false);

        // If the character has no AP remaining, end their turn
        if (unit.stats.CurrentAP() <= 0)
        {
            if (unit.isMyTurn)
                TurnManager.Instance.FinishTurn(unit);
        }
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

        if (moveAction.finalTargetGridPosition != unit.gridPosition)
        {
            SetTargetGridPosition(moveAction.nextTargetGridPosition);
            moveAction.SetFinalTargetGridPosition(moveAction.nextTargetGridPosition);
        }

        unit.unitActionHandler.SetTargetEnemyUnit(null);
    }

    public void ClearActionQueue(bool stopMoveAnimation)
    {
        // Debug.Log("Clearing action queue");
        queuedAction = null;
        queuedAP = 0;
        isPerformingAction = false;

        // If the Unit isn't moving, they might still be in a move animation, so cancel that
        if (stopMoveAnimation && GetAction<MoveAction>().isMoving == false)
            unit.unitAnimator.StopMovingForward();
    }
    #endregion

    #region Combat
    public void AttackTargetEnemy()
    {
        if (GetAction<TurnAction>().IsFacingTarget(targetEnemyUnit.gridPosition))
        {
            if (unit.RangedWeaponEquipped())
            {
                if (unit.GetRangedWeapon().isLoaded)
                    QueueAction(GetAction<ShootAction>());
                else
                    QueueAction(GetAction<ReloadAction>());
            }
            else
                QueueAction(GetAction<MeleeAction>());
        }
        else
        {
            GetAction<TurnAction>().SetTargetPosition(GetAction<TurnAction>().targetDirection);
            QueueAction(GetAction<TurnAction>());
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
        targetEnemyUnit = target;
        if (target != null)
            previousTargetEnemyGridPosition = target.gridPosition;
    }

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public void SetSelectedAction(BaseAction action) => selectedAction = action;

    public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

    public LayerMask ShootObstacleMask() => shootObstacleMask;
}
