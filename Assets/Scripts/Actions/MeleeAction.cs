using System;
using System.Collections;
using UnityEngine;

public class MeleeAction : BaseAction
{
    public Unit targetEnemyUnit { get; private set; }

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

    }

    public void Attack()
    {
        if (unit.leftHeldItem != null && unit.leftHeldItem.itemData.item.IsMeleeWeapon() && unit.rightHeldItem != null && unit.rightHeldItem.itemData.item.IsMeleeWeapon())
        {
            // Do a dual wield attack
            unit.unitAnimator.StartDualMeleeAttack();
            unit.rightHeldItem.DoDefaultAttack();
            StartCoroutine(unit.leftHeldItem.DelayDoDefaultAttack());
        }
        else if (unit.rightHeldItem != null)
        {
            unit.unitAnimator.StartMeleeAttack();
            unit.rightHeldItem.DoDefaultAttack();
        }
        else if (unit.leftHeldItem != null)
        {
            unit.unitAnimator.StartMeleeAttack();
            unit.leftHeldItem.DoDefaultAttack();
        }

        StartCoroutine(WaitToFinishAction());
    }

    IEnumerator WaitToFinishAction()
    {
        if (unit.rightHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetAttackAnimationTime(unit.rightHeldItem.itemData.item as Weapon) / 2f);
        else
            yield return new WaitForSeconds(0.5f);

        CompleteAction();
        unit.unitActionHandler.FinishAction();
        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
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
