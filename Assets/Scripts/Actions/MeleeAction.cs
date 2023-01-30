using System;
using UnityEngine;

public class MeleeAction : BaseAction
{
    Unit targetEnemyUnit;

    public bool isAttacking { get; private set; }

    [SerializeField] bool canFightUnarmed;

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
            return;
        }

        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    public void Attack()
    {
        // Debug.Log(unit + " attacked " + targetEnemyUnit);

        unit.unitAnimator.StartMeleeAttack();

        if (unit.rightHeldItem != null)
            unit.rightHeldItem.DoDefaultAttack();
        
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

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        float finalActionValue = 0;
        Unit targetUnit = null;

        if (IsValidAction())
        {
            targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            if (targetUnit != null)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += targetUnit.healthSystem.CurrentHealthNormalized() * 100f;
                finalActionValue -= TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetUnit.gridPosition);
            }
        }

        return new EnemyAIAction
        {
            unit = targetUnit,
            gridPosition = gridPosition,
            actionValue = Mathf.RoundToInt(finalActionValue)
        };
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        return 300;
    }

    public override bool IsValidAction()
    {
        if (unit.MeleeWeaponEquipped() || canFightUnarmed)
            return true;
        return false;
    }

    public void SetTargetEnemyUnit(Unit target) => targetEnemyUnit = target;

    public bool CanFightUnarmed() => canFightUnarmed;

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Melee Attack";
}
