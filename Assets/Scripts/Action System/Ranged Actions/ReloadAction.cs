using GridSystem;
using UnitSystem;

namespace ActionSystem
{
    public class ReloadAction : BaseAction
    {
        bool isReloading;

        public void QueueAction() => unit.unitActionHandler.QueueAction(this);

        public override void TakeAction()
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

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.RangedWeaponEquipped() && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded == false && unit.UnitEquipment.HasValidAmmunitionEquipped();

        public override bool IsHotbarAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override bool IsAttackAction() => false;

        public override bool IsMeleeAttackAction() => false;

        public override bool IsRangedAttackAction() => false;

        public override int GetEnergyCost() => 0;
    }
}
