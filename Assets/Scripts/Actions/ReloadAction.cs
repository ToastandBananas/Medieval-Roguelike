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

    public override bool IsValidAction() => unit.RangedWeaponEquipped() && unit.GetRangedWeapon().isLoaded == false;

    IEnumerator StartReloadTimer()
    {
        stateTimer = timeToReload;

        while (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                CompleteAction();

            yield return null;
        }
    }

    public override void TakeAction(GridPosition gridPosition)
    {
        if (isReloading) return;

        StartAction();
        Reload();
    }

    void Reload()
    {
        // StartCoroutine(StartReloadTimer());
        unit.GetRangedWeapon().LoadProjectile();
        CompleteAction();
        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    protected override void StartAction()
    {
        base.StartAction();
        isReloading = true;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();
        isReloading = false;
        if (unit.IsPlayer())
            unit.unitActionHandler.SetSelectedAction(unit.unitActionHandler.GetAction<ShootAction>());
        unit.unitActionHandler.FinishAction();
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        return 100;
    }

    public override bool ActionIsUsedInstantly() => true;

    public override string GetActionName() => "Reload";
}
