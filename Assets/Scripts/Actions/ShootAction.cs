using System;
using System.Collections;
using UnityEngine;

public class ShootAction : BaseAction
{
    public bool isShooting { get; private set; }
    bool nextAttackFree;

    void Start()
    {
        unit.unitActionHandler.GetAction<MoveAction>().OnStopMoving += MoveAction_OnStopMoving;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isShooting) return;

        if (unit.unitActionHandler.targetEnemyUnit == null || unit.unitActionHandler.targetEnemyUnit.isDead)
        {
            unit.unitActionHandler.FinishAction();
            return;
        }

        StartAction(onActionComplete);

        if (RangedWeaponIsLoaded() == false)
        {
            CompleteAction();
            unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<ReloadAction>(), unit.gridPosition);
            return;
        }
        else if (IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))
        {
            if (unit.unitActionHandler.GetAction<TurnAction>().IsFacingTarget(unit.unitActionHandler.targetEnemyUnit.gridPosition))
                Shoot();
            else
            {
                nextAttackFree = true;
                CompleteAction();
                unit.unitActionHandler.GetAction<TurnAction>().SetTargetPosition(unit.unitActionHandler.GetAction<TurnAction>().targetDirection);
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>(), unit.unitActionHandler.targetEnemyUnit.gridPosition);
            }
        }
        else
        {
            CompleteAction();
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
            unit.unitActionHandler.targetEnemyUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit
            unit.unitActionHandler.targetEnemyUnit.healthSystem.TakeDamage(unit.leftHeldItem.itemData.damage);

            CompleteAction();
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
        }
    }

    IEnumerator WaitToFinishAction()
    {
        if (unit.leftHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetWeaponAttackAnimationTime(unit.leftHeldItem.itemData.item as Weapon));
        else
            yield return new WaitForSeconds(0.5f);

        CompleteAction();
    }

    IEnumerator RotateTowardsTarget()
    {
        while (isShooting)
        {
            float rotateSpeed = 10f;
            Vector3 lookPos = (new Vector3(unit.unitActionHandler.targetEnemyUnit.WorldPosition().x, transform.position.y, unit.unitActionHandler.targetEnemyUnit.WorldPosition().z) - unit.WorldPosition()).normalized;
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
        unit.unitActionHandler.FinishAction();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        if (nextAttackFree || RangedWeaponIsLoaded() == false)
        {
            nextAttackFree = false;
            return 0;
        }
        return 300;
    }

    void MoveAction_OnStopMoving(object sender, EventArgs e) => nextAttackFree = false;

    public bool RangedWeaponIsLoaded() => unit.GetEquippedRangedWeapon().isLoaded; 

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Shoot";

    public override bool IsValidAction()
    {
        if (unit.RangedWeaponEquipped())
            return true;
        return false;
    }
}
