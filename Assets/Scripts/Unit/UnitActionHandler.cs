using UnityEngine;

public class UnitActionHandler : MonoBehaviour
{
    public GridPosition targetGridPosition { get; private set; }

    public BaseAction queuedAction { get; private set; }
    public int queuedAP { get; private set; }

    BaseAction[] baseActionArray;
    public BaseAction selectedAction { get; private set; }

    public Unit unit { get; private set; }

    [SerializeField] LayerMask actionsObstacleMask;

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
        if (unit.isMyTurn && unit.isDead == false)
        {
            if (canPerformActions == false || unit.stats.CurrentAP() <= 0)
                TurnManager.Instance.FinishTurn(unit);
            else
            {
                // unit.vision.CheckEnemyVisibility();

                if (queuedAction != null)
                    GetNextQueuedAction();
                else
                    unit.UnblockCurrentPosition();
            }
        }
    }

    #region Action Queue
    public void QueueAction(BaseAction action, int APCost)
    {
        // if (isNPC) Debug.Log(name + " queued " + action);
        queuedAction = action;
        queuedAP = APCost;

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
        if (unit.isDead)
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
    }

    public void ClearActionQueue()
    {
        queuedAction = null;
        queuedAP = 0;
        isPerformingAction = false;
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

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public void SetSelectedAction(BaseAction action) => selectedAction = action;

    public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

    public LayerMask ActionsObstacleMask() => actionsObstacleMask;
}
