public class ReloadAction : BaseAction
{
    bool isReloading;

    public override bool IsValidAction() => unit.RangedWeaponEquipped() && unit.GetRangedWeapon().isLoaded == false;

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
        TurnManager.Instance.StartNextUnitsTurn(unit);
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

    public override int GetActionPointsCost()
    {
        return 100;
    }

    public override bool ActionIsUsedInstantly() => true;

    public override bool IsAttackAction() => false;

    public override bool IsMeleeAttackAction() => false;

    public override bool IsRangedAttackAction() => false;

    public override string GetActionName() => "Reload";
}
