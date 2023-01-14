using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitActionHandler : MonoBehaviour
{
    GridPosition targetGridPosition;

    List<BaseAction> queuedActions = new List<BaseAction>();
    List<int> queuedAP = new List<int>();

    BaseAction[] baseActionArray;

    Unit unit;

    bool isPerformingAction;

    void Awake()
    {
        unit = GetComponent<Unit>(); 
        baseActionArray = GetComponents<BaseAction>();
    }

    #region Action Queue
    public void TakeTurn()
    {
        if (unit.IsMyTurn() && unit.IsDead() == false)
        {
            if (unit.Stats().CurrentAP() <= 0)
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            else
            {
                //vision.CheckEnemyVisibility();

                if (queuedActions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                else if (unit.IsNPC())
                    DetermineAction();
            }
        }
    }

    public void QueueAction(BaseAction action, int APCost)
    {
        // if (isNPC) Debug.Log(name + " queued " + action);
        queuedActions.Add(action);
        queuedAP.Add(APCost);

        if (unit.IsMyTurn() && unit.Stats().CurrentAP() > 0)
            StartCoroutine(GetNextQueuedAction());

        // Update AP text
        //if (IsNPC() == false)
        //APManager.Instance.UpdateLastAPUsed(APCost);
    }

    public IEnumerator GetNextQueuedAction()
    {
        if (unit.IsDead())
            yield break;

        if (queuedActions.Count > 0 && isPerformingAction == false)
        {
            int APRemainder = unit.Stats().UseAPAndGetRemainder(queuedAP[0]);
            if (APRemainder <= 0)
            {
                isPerformingAction = true;
                queuedActions[0].TakeAction(targetGridPosition, null);
                // if (isNPC == false) Debug.Log("Got next queued action. Actions still queued: " + actions.Count);
            }
            else
            {
                // if (isNPC == false) Debug.Log("Can't do next queued action yet. Remaining AP: " + APRemainder);
                queuedAP[0] = APRemainder;
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            }
        }
        else
            yield return null;
    }

    public void FinishAction()
    {
        if (queuedAP.Count > 0)
            queuedAP.Remove(queuedAP[0]);
        if (queuedActions.Count > 0)
            queuedActions.Remove(queuedActions[0]);

        isPerformingAction = false;

        // If the character has no AP remaining, end their turn
        if (unit.Stats().CurrentAP() <= 0)
            StartCoroutine(TurnManager.Instance.FinishTurn(unit));
        else if (GetAction<MoveAction>().IsMoving() == false) // Take another action
            TakeTurn();
    }

    public void ResetActionsQueue()
    {
        queuedActions.Clear();
        queuedAP.Clear();
    }

    // NPC Only
    public void DetermineAction()
    {
        switch (unit.StateController().CurrentState())
        {
            case State.Idle:
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
                break;
            case State.Patrol:
                break;
            case State.Wander:
                break;
            case State.Follow:
                break;
            case State.MoveToTarget:
                break;
            case State.Fight:
                break;
            case State.Flee:
                break;
            case State.Hunt:
                break;
            case State.FindFood:
                break;
            default:
                break;
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

    public List<BaseAction> QueuedActions() => queuedActions;
}
