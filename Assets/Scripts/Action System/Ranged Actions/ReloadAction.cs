using GridSystem;
using UnitSystem;

namespace ActionSystem
{
    public class ReloadAction : BaseAction
    {
        bool isReloading;

        public override void TakeAction()
        {
            if (unit == null || isReloading) return;

            StartAction();
            Reload();
        }

        void Reload()
        {
            // StartCoroutine(StartReloadTimer());
            unit.unitMeshManager.GetHeldRangedWeapon().LoadProjectile();
            CompleteAction();
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
                unit.unitActionHandler.SetDefaultSelectedAction();
            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetActionPointsCost()
        {
            return 100;
        }

        public override bool CanQueueMultiple() => false;

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.RangedWeaponEquipped() && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded == false && unit.UnitEquipment.HasValidAmmunitionEquipped();

        public override bool IsHotbarAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override int GetEnergyCost() => 0;
    }
}
