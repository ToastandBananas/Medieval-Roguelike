using UnityEngine;
using UnitSystem.ActionSystem.UI;

namespace UnitSystem.ActionSystem
{
    public class UnloadAction : BaseAction
    {
        readonly int defaultActionPointCost = 200;

        public override void TakeAction()
        {
            if (unit == null || unit.unitActionHandler.AvailableActions.Contains(this) == false)
            {
                CompleteAction();
                return;
            }

            StartAction();
            Unload();
        }

        void Unload()
        {
            unit.unitMeshManager.GetHeldRangedWeapon().UnloadProjectile();
            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            if (unit.IsPlayer)
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetActionPointsCost()
        {
            return Mathf.RoundToInt(defaultActionPointCost * (float)unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ReloadActionPointCostMultiplier);
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.RangedWeaponEquipped && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;

        public override bool ActionIsUsedInstantly() => true;

        public override int GetEnergyCost() => 0;

        public override string TooltipDescription()
        {
            if (unit.unitMeshManager.GetHeldRangedWeapon().loadedProjectile != null)
                return $"Unload the <b>{unit.unitMeshManager.GetHeldRangedWeapon().loadedProjectile.ItemData.Item.Name}</b> from your <b>{unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Name}</b>.";
            else
                return $"Unload your <b>{unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Name}</b>.";
        }
    }
}
