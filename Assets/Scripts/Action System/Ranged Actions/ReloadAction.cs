using UnitSystem;
using UnityEngine;

namespace ActionSystem
{
    public class ReloadAction : BaseAction
    {
        // bool isReloading;

        readonly int defaultActionPointCost = 200;

        public override void TakeAction()
        {
            if (unit == null || unit.unitActionHandler.AvailableActions.Contains(this) == false)// || isReloading)
                return;

            StartAction();
            Reload();
        }

        void Reload()
        {
            unit.unitMeshManager.GetHeldRangedWeapon().LoadProjectile();
            CompleteAction();
        }

        protected override void StartAction()
        {
            base.StartAction();
            // isReloading = true;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();
            // isReloading = false;

            if (unit.IsPlayer)
                unit.unitActionHandler.SetDefaultSelectedAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetActionPointsCost()
        {
            // TODO: Determine cost by specific ranged weapon (set a multiplier of the cost in the ranged weapon's Scriptable Object)
            return Mathf.RoundToInt(defaultActionPointCost * (float)unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ReloadActionPointCostMultiplier);
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.RangedWeaponEquipped() && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded == false && unit.UnitEquipment.HasValidAmmunitionEquipped();

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool IsHotbarAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override int GetEnergyCost() => 0;
    }
}
