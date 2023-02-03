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

        if (unit.unitActionHandler.targetEnemyUnit == null || unit.unitActionHandler.targetEnemyUnit.health.IsDead())
        {
            unit.unitActionHandler.FinishAction();
            return;
        }

        StartAction(onActionComplete);

        if (RangedWeaponIsLoaded() == false)
        {
            CompleteAction();
            unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<ReloadAction>());
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
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
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
            unit.unitActionHandler.targetEnemyUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit
            unit.leftHeldItem.DoDefaultAttack();
            StartCoroutine(WaitToFinishAction());
        }
        else
        {
            unit.unitActionHandler.targetEnemyUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit
            unit.unitActionHandler.targetEnemyUnit.health.TakeDamage(unit.leftHeldItem.itemData.damage);

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
        Vector3 targetPos = unit.unitActionHandler.targetEnemyUnit.WorldPosition();
        while (isShooting)
        {
            float rotateSpeed = 10f;
            Vector3 lookPos = (new Vector3(targetPos.x, transform.position.y, targetPos.z) - unit.WorldPosition()).normalized;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(unit.unitActionHandler.GetAction<TurnAction>().currentDirection, unit.transform.position, false);
    }

    public bool IsInAttackRange(Unit enemyUnit)
    {
        float dist = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize();
        if (dist <= unit.GetRangedWeapon().MaxRange(unit.gridPosition, enemyUnit.gridPosition) && dist >= unit.GetRangedWeapon().itemData.item.Weapon().minRange)
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
        if (unit.IsPlayer() && PlayerActionInput.Instance.autoAttack == false)
            unit.unitActionHandler.SetTargetEnemyUnit(null);
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

    public bool RangedWeaponIsLoaded() => unit.GetRangedWeapon().isLoaded; 

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Shoot";

    public override bool IsValidAction()
    {
        if (unit.RangedWeaponEquipped())
            return true;
        return false;
    }
}
