using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReloadAction : BaseAction
{
    public event EventHandler OnStartReload;
    public event EventHandler OnStopReload;

    [SerializeField] float timeToReload = 1f;
    float stateTimer;

    IEnumerator StartReloadTimer()
    {
        while (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                CompleteAction();
                OnStopReload?.Invoke(this, EventArgs.Empty);
            }

            yield return null;
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        stateTimer = timeToReload;

        OnStartReload?.Invoke(this, EventArgs.Empty);

        StartAction(onActionComplete);

        StartCoroutine(StartReloadTimer());
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GridPosition();
        return new List<GridPosition>
        {
            unitGridPosition
        };
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int testActionValue = 0;
        if (IsValidAction())
            testActionValue = 200;

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = testActionValue
        };
    }

    public override string GetActionName() => "Reload";

    public override int GetActionPointsCost()
    {
        // TODO: Different types of ranged weapons cost different amounts
        return 10;
    }

    public override bool ActionIsUsedInstantly() => true;

    public override bool IsValidAction()
    {
        HeldItem leftHeldItem = unit.LeftHeldItem();
        if (leftHeldItem == null || leftHeldItem is RangedWeapon == false)
            return false;
        else
        {
            RangedWeapon rangedWeapon = (RangedWeapon)leftHeldItem;
            if (rangedWeapon.IsLoaded())
                return false;
            else
                return true;
        }
    }
}
