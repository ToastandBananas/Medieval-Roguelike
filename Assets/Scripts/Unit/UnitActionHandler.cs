using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitActionHandler : MonoBehaviour
{
    public GridPosition targetGridPosition { get; private set; }

    public List<BaseAction> queuedActions { get; private set; }
    public List<int> queuedAP { get; private set; }

    BaseAction[] baseActionArray;
    public BaseAction selectedAction { get; private set; }

    public Unit unit { get; private set; }

    [SerializeField] public LayerMask actionsObstacleMask { get; private set; }

    public bool isPerformingAction { get; private set; }

    void Awake()
    {
        queuedActions = new List<BaseAction>();
        queuedAP = new List<int>();

        unit = GetComponent<Unit>(); 
        baseActionArray = GetComponents<BaseAction>();

        SetSelectedAction(GetAction<MoveAction>());
    }

    #region Action Queue
    public void QueueAction(BaseAction action, int APCost)
    {
        // if (isNPC) Debug.Log(name + " queued " + action);
        queuedActions.Add(action);
        queuedAP.Add(APCost);

        if (unit.isMyTurn && unit.stats.CurrentAP() > 0)
            StartCoroutine(GetNextQueuedAction());

        // Update AP text
        //if (IsNPC() == false)
        //APManager.Instance.UpdateLastAPUsed(APCost);
    }

    public IEnumerator GetNextQueuedAction()
    {
        if (unit.isDead)
            yield break;

        if (queuedActions.Count > 0 && isPerformingAction == false)
        {
            int APRemainder = unit.stats.UseAPAndGetRemainder(queuedAP[0]);
            if (APRemainder <= 0)
            {
                isPerformingAction = true;
                queuedActions[0].TakeAction(targetGridPosition, null);
                // if (isNPC == false) Debug.Log("Got next queued action. Actions still queued: " + actions.Count);
            }
            else
            {
                // if (isNPC == false) Debug.Log("Can't do next queued action yet. Remaining AP: " + APRemainder);
                isPerformingAction = false;
                queuedAP[0] = APRemainder;
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            }
        }
        else
            yield return null;
    }

    public virtual void FinishAction()
    {
        if (queuedAP.Count > 0)
            queuedAP.Remove(queuedAP[0]);
        if (queuedActions.Count > 0)
            queuedActions.Remove(queuedActions[0]);

        isPerformingAction = false;

        // If the character has no AP remaining, end their turn
        if (unit.stats.CurrentAP() <= 0)
        {
            if (unit.isMyTurn)
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
        }
    }

    public void ResetActionsQueue()
    {
        queuedActions.Clear();
        queuedAP.Clear();
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
}
