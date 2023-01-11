using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public static event EventHandler OnAnyActionStarted;
    public static event EventHandler OnAnyActionCompleted;

    protected Action onActionComplete;

    protected Unit unit;
    protected bool isActive;

    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositionList = GetValidActionGridPositionList();
        return validGridPositionList.Contains(gridPosition);
    }

    // Get a list of all valid Grid Positions for this Action
    public abstract List<GridPosition> GetValidActionGridPositionList();

    protected void StartAction(Action onActionComplete)
    {
        isActive = true;
        this.onActionComplete = onActionComplete;

        OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
    }

    public virtual void CompleteAction()
    {
        isActive = false;
        onActionComplete();

        OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();
        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();

        for (int i = 0; i < validActionGridPositionList.Count; i++)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(validActionGridPositionList[i]);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            if (AllEnemyAIActionValuesAreZero(enemyAIActionList))
                return null;
            else if (this is MoveAction)
            {
                if (AllEnemyAIActionsAreEqual(enemyAIActionList))
                {
                    if (unit.GetAction<ShootAction>().GetTargetCountAtPosition(unit.GridPosition()) == 0)
                        return GetEnemyAIActionNearestToEnemy(enemyAIActionList);
                    else
                        return GetEnemyAIActionNearestToCurrentPosition(enemyAIActionList);
                }
                else
                    return GetBestEnemyAIActionFromList(enemyAIActionList);
            }
            else
                return GetBestEnemyAIActionFromList(enemyAIActionList);
        }

        // No possible Enemy AI Actions
        return null;
    }

    EnemyAIAction GetBestEnemyAIActionFromList(List<EnemyAIAction> enemyAIActionList)
    {
        enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue);
        return enemyAIActionList[0];
    }

    EnemyAIAction GetEnemyAIActionNearestToEnemy(List<EnemyAIAction> enemyAIActionList)
    {
        float nearestEnemyDistance = float.MaxValue;
        EnemyAIAction nearestEnemyAIActionToEnemy = enemyAIActionList[0];

        // Iterate through each MoveAction possible
        for (int i = 0; i < enemyAIActionList.Count; i++)
        {
            foreach (Unit testUnit in UnitManager.Instance.UnitsList())
            {
                if (testUnit.IsEnemy(unit.GetCurrentFaction()))
                {
                    float distanceToEnemy = Vector3.Distance(LevelGrid.Instance.GetWorldPosition(enemyAIActionList[i].gridPosition), LevelGrid.Instance.GetWorldPosition(testUnit.GridPosition()) / LevelGrid.Instance.GridSize());
                    if (distanceToEnemy < nearestEnemyDistance)
                    {
                        nearestEnemyDistance = distanceToEnemy;
                        nearestEnemyAIActionToEnemy = enemyAIActionList[i];
                    }
                }
            }
        }

        return nearestEnemyAIActionToEnemy;
    }

    EnemyAIAction GetEnemyAIActionNearestToCurrentPosition(List<EnemyAIAction> enemyAIActionList)
    {
        float nearestDistance = float.MaxValue;
        EnemyAIAction nearestEnemyAIActionToCurrentPosition = enemyAIActionList[0];

        // Iterate through each MoveAction possible
        for (int i = 0; i < enemyAIActionList.Count; i++)
        {
            float distanceToGridPosition = Vector3.Distance(LevelGrid.Instance.GetWorldPosition(enemyAIActionList[i].gridPosition), LevelGrid.Instance.GetWorldPosition(unit.GridPosition()) / LevelGrid.Instance.GridSize());
            if (distanceToGridPosition < nearestDistance)
            {
                nearestDistance = distanceToGridPosition;
                nearestEnemyAIActionToCurrentPosition = enemyAIActionList[i];
            }
        }

        return nearestEnemyAIActionToCurrentPosition;
    }

    bool AllEnemyAIActionsAreEqual(List<EnemyAIAction> enemyAIActionList)
    {
        bool allMoveActionValuesEqual = true;
        int firstActionValue = enemyAIActionList[0].actionValue;
        for (int i = 1; i < enemyAIActionList.Count; i++)
        {
            if (enemyAIActionList[i].actionValue != firstActionValue)
            {
                allMoveActionValuesEqual = false;
                break;
            }
        }

        return allMoveActionValuesEqual;
    }

    bool AllEnemyAIActionValuesAreZero(List<EnemyAIAction> enemyAIActionList)
    {
        bool allMoveActionValuesZero = true;
        for (int i = 0; i < enemyAIActionList.Count; i++)
        {
            if (enemyAIActionList[i].actionValue != 0)
            {
                allMoveActionValuesZero = false;
                break;
            }
        }

        return allMoveActionValuesZero;
    }

    public bool IsActive() => isActive;

    public abstract int GetActionPointsCost();

    public abstract bool IsValidAction();

    public abstract bool ActionIsUsedInstantly();

    public abstract string GetActionName();
}
