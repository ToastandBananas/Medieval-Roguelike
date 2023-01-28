using System;
using UnityEngine;

public class MeleeAction : BaseAction
{
    Unit targetEnemyUnit;

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isAttacking) return;

        StartAction(onActionComplete);

        if (IsInAttackRange(targetEnemyUnit))
            Attack();
        else
        {
            CompleteAction();
            unit.unitActionHandler.FinishAction();
            unit.unitActionHandler.TakeTurn();
        }

        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    public void Attack()
    {
        Debug.Log(unit + " attacked " + targetEnemyUnit); 
        
        CompleteAction();
        unit.unitActionHandler.FinishAction();
    }

    public bool IsInAttackRange(Unit enemyUnit)
    {
        float attackRange = 1.4f;
        if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= attackRange)
            return true;
        return false;
    }

    protected override void StartAction(Action onActionComplete)
    {
        base.StartAction(onActionComplete);

        isAttacking = true;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();

        isAttacking = false;
    }

    public void SetTargetEnemyUnit(Unit target) => targetEnemyUnit = target;

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int finalActionValue = 0;

        if (IsValidAction())
        {
            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            // Target the Unit with the lowest health and/or the nearest target
            finalActionValue += 200 + Mathf.RoundToInt((1 - targetUnit.healthSystem.CurrentHealthNormalized()) * 100f);
        }

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = finalActionValue
        };
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        return 300;
    }

    public override bool IsValidAction()
    {
        return true;
    }

    public bool isAttacking { get; private set; }

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Melee Attack";
}
