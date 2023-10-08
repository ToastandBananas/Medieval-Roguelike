using GridSystem;

public class ReloadAction : BaseAction
{
    bool isReloading;

    public override bool IsValidAction() => unit != null && unit.CharacterEquipment.RangedWeaponEquipped() && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded == false && unit.CharacterEquipment.HasValidAmmunitionEquipped();

    public override void TakeAction(GridPosition gridPosition)
    {
        if (isReloading) return;

        StartAction();
        Reload();
    }

    void Reload()
    {
        // StartCoroutine(StartReloadTimer());
        unit.unitMeshManager.GetHeldRangedWeapon().LoadProjectile();
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
        if (unit.IsPlayer)
            unit.unitActionHandler.SetSelectedActionType(unit.unitActionHandler.FindActionTypeByName("ShootAction"));
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

    public override int GetEnergyCost() => 0;

    public override string GetActionName() => "Reload";
}
