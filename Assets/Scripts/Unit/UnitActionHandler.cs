using System.Collections;
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

    void Awake()
    {
        unit = GetComponent<Unit>(); 
        baseActionArray = GetComponents<BaseAction>();

        SetSelectedAction(GetAction<MoveAction>());
    }

    #region Action Queue
    public void QueueAction(BaseAction action, int APCost)
    {
        // if (isNPC) Debug.Log(name + " queued " + action);
        queuedAction = action;
        queuedAP = APCost;

        if (unit.isMyTurn && unit.stats.CurrentAP() > 0)
            StartCoroutine(GetNextQueuedAction());

        // Update AP text
        //if (IsNPC() == false)
        //APManager.Instance.UpdateLastAPUsed(APCost);
    }

    public IEnumerator GetNextQueuedAction()
    {
        if (unit.isDead)
        {
            ClearActionQueue();
            yield break;
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
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            }
        }
        else
            yield return null;
    }

    public virtual void FinishAction()
    {
        /*if (queuedAP != 0)
            queuedAP = 0;
        if (queuedAction != null)
            queuedAction = null;*/

        ClearActionQueue();

        // If the character has no AP remaining, end their turn
        if (unit.stats.CurrentAP() <= 0)
        {
            if (unit.isMyTurn)
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
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

    public LayerMask ActionsObstacleMask() => actionsObstacleMask;
}
