using System;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    enum State
    {
        Waiting = 0,
        TakingTurn = 10,
        Busy = 20
    }

    State state;
    float timer;

    void Awake()
    {
        state = State.Waiting;
    }

    void Start()
    {
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }

    void Update()
    {
        if (TurnSystem.Instance.IsPlayerTurn())
            return;

        switch (state)
        {
            case State.Waiting:
                break;
            case State.TakingTurn:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (TryTakeEnemyAIAction(SetTakingTurnState))
                        state = State.Busy;
                    else
                        TurnSystem.Instance.NextTurn(); // No more enemies have actions that they can take so end the enemies turn
                }
                break;
            case State.Busy:
                break;
        }
    }

    void SetTakingTurnState()
    {
        // Set the state back to TakingTurn, so that the next enemy Unit can try to take their turn
        state = State.TakingTurn;
        timer = 0.5f;
    }

    void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if (TurnSystem.Instance.IsPlayerTurn() == false)
        {
            state = State.TakingTurn;
            timer = 2f;
        }
    }

    bool TryTakeEnemyAIAction(Action onEnemyAIActionComplete)
    {
        for (int i = 0; i < UnitManager.Instance.EnemyUnitsList().Count; i++)
        {
            if (TryTakeEnemyAIAction(UnitManager.Instance.EnemyUnitsList()[i], onEnemyAIActionComplete))
                return true;
        }

        return false;
    }

    bool TryTakeEnemyAIAction(Unit enemyUnit, Action onEnemyAIActionComplete)
    {
        EnemyAIAction bestEnemyAIAction = null;
        BaseAction bestBaseAction = null;
        
        // Cycle through every available action for this enemy
        foreach (BaseAction baseAction in enemyUnit.GetBaseActionArray())
        {
            // If enemy cannot afford this action
            if (enemyUnit.CanSpendActionPointsToTakeAction(baseAction) == false)
                continue;

            // Find the best action and action position for it to take
            if (bestEnemyAIAction == null)
            {
                bestEnemyAIAction = baseAction.GetBestEnemyAIAction();
                bestBaseAction = baseAction;
            }
            else
            {
                EnemyAIAction testEnemyAIAction = baseAction.GetBestEnemyAIAction();

                if (testEnemyAIAction != null && testEnemyAIAction.actionValue > bestEnemyAIAction.actionValue)
                {
                    bestEnemyAIAction = testEnemyAIAction;
                    bestBaseAction = baseAction;
                }
            }
        }

        // Try to take the action
        if (bestEnemyAIAction != null)
        {
            if (bestBaseAction is MoveAction)
            {
                if (enemyUnit.TrySpendActionPointsToMove(bestEnemyAIAction.gridPosition))
                {
                    // Action is possible
                    UnitActionSystem.Instance.SetActiveAIUnit(enemyUnit);
                    bestBaseAction.TakeAction(bestEnemyAIAction.gridPosition, onEnemyAIActionComplete);
                    return true;
                }
            }
            else if (bestBaseAction is InteractAction)
            {
                if (enemyUnit.TrySpendActionPointsToInteract(bestEnemyAIAction.gridPosition))
                {
                    // Action is possible
                    UnitActionSystem.Instance.SetActiveAIUnit(enemyUnit);
                    bestBaseAction.TakeAction(bestEnemyAIAction.gridPosition, onEnemyAIActionComplete);
                    return true;
                }
            }
            else if (enemyUnit.TrySpendActionPointsToTakeAction(bestBaseAction))
            {
                // Action is possible
                UnitActionSystem.Instance.SetActiveAIUnit(enemyUnit);
                bestBaseAction.TakeAction(bestEnemyAIAction.gridPosition, onEnemyAIActionComplete);
                return true;
            }
        }

        // No action was possible
        return false;
    }
}
