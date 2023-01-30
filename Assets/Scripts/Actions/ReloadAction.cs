using System;
using System.Collections;
using UnityEngine;

public class ReloadAction : BaseAction
{
    [SerializeField] float timeToReload = 0.1f;
    float stateTimer;

    bool isReloading;

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override bool IsValidAction() => unit.RangedWeaponEquipped() && unit.GetEquippedRangedWeapon().isLoaded == false;

    IEnumerator StartReloadTimer()
    {
        while (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                CompleteAction();
                unit.unitActionHandler.FinishAction();
            }

            yield return null;
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isReloading) return;

        stateTimer = timeToReload;

        StartAction(onActionComplete);

        Reload();
    }

    void Reload()
    {
        StartCoroutine(StartReloadTimer());

        unit.GetEquippedRangedWeapon().LoadProjectile(); 
        
        //CompleteAction();
        //unit.unitActionHandler.FinishAction();

        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    protected override void StartAction(Action onActionComplete)
    {
        base.StartAction(onActionComplete);
        isReloading = true;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();
        isReloading = false;
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        return 100;
    }

    public override bool ActionIsUsedInstantly() => true;

    public override string GetActionName() => "Reload";
}
