using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public Unit unit { get; private set; }

    protected bool isActive;

    public virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public abstract void TakeAction(GridPosition gridPosition); 
    
    protected virtual void StartAction()
    {
        isActive = true;

        //if (unit.IsPlayer())
            //GridSystemVisual.HideGridVisual();
    }

    public virtual void CompleteAction()
    {
        isActive = false;
        if (unit.IsPlayer())
            UnitActionSystemUI.Instance.UpdateActionVisuals();
    }

    public EnemyAIAction GetBestEnemyAIActionFromList(List<EnemyAIAction> enemyAIActionList)
    {
        enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue);
        return enemyAIActionList[0];
    }

    public virtual List<GridPosition> GetValidActionGridPositionList(GridPosition startGridPosition) => null;

    public virtual List<GridPosition> GetValidActionGridPositionList_Secondary(GridPosition startGridPosition) => null;

    public bool IsActive() => isActive;

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

    public abstract int GetActionPointsCost(GridPosition targetGridPosition);

    public abstract bool IsValidAction();

    public abstract bool ActionIsUsedInstantly();

    public abstract string GetActionName();
}
