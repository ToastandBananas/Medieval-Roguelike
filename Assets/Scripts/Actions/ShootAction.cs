using System;
using System.Collections;
using UnityEngine;

public class ShootAction : BaseAction
{
    Unit targetEnemyUnit;

    public bool isShooting { get; private set; }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isShooting) return;

        StartAction(onActionComplete);

        if (RangedWeaponIsLoaded() == false)
        {
            CompleteAction();
            unit.unitActionHandler.FinishAction();
            unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<ReloadAction>(), unit.gridPosition);
            return;
        }
        else if (IsInAttackRange(targetEnemyUnit))
            Shoot();
        else
        {
            CompleteAction();
            unit.unitActionHandler.FinishAction();
            unit.unitActionHandler.TakeTurn();
            return;
        }
    }

    void Shoot()
    {
        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            StartCoroutine(RotateTowardsTarget());
            unit.leftHeldItem.DoDefaultAttack();
            StartCoroutine(WaitToFinishAction());
        }
        else
        {
            targetEnemyUnit.healthSystem.TakeDamage(unit.leftHeldItem.itemData.damage);

            CompleteAction();
            unit.unitActionHandler.FinishAction();
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
        }
    }

    IEnumerator WaitToFinishAction()
    {
        if (unit.leftHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetAttackAnimationTime(unit.leftHeldItem.itemData.item as Weapon));
        else
            yield return new WaitForSeconds(0.5f);

        CompleteAction();
        unit.unitActionHandler.FinishAction();
    }

    IEnumerator RotateTowardsTarget()
    {
        while (isShooting)
        {
            float rotateSpeed = 10f;
            Vector3 lookPos = (new Vector3(targetEnemyUnit.WorldPosition().x, transform.position.y, targetEnemyUnit.WorldPosition().z) - unit.WorldPosition()).normalized;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(unit.unitActionHandler.GetAction<TurnAction>().currentDirection, unit.transform.position, false);
    }

    public bool IsInAttackRange(Unit enemyUnit)
    {
        float attackRange = 10f;
        if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= attackRange)
            return true;
        return false;
    }

    protected override void StartAction(Action onActionComplete)
    {
        base.StartAction(onActionComplete);
        isShooting = true;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();
        isShooting = false;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        if (RangedWeaponIsLoaded() == false)
            return 10;
        return 300;
    }

    public bool RangedWeaponIsLoaded() => unit.GetEquippedRangedWeapon().isLoaded; 

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Shoot";

    public override bool IsValidAction()
    {
        if (unit.RangedWeaponEquipped())
            return true;
        return false;
    }

    public void SetTargetEnemyUnit(Unit target) => targetEnemyUnit = target;
}
