using UnityEngine;
using UnitSystem.ActionSystem.UI;

namespace UnitSystem.ActionSystem
{
    public class UnloadAction : BaseAction
    {
        readonly int defaultActionPointCost = 200;

        public override void TakeAction()
        {
            if (Unit == null || Unit.unitActionHandler.AvailableActions.Contains(this) == false)
            {
                CompleteAction();
                return;
            }

            StartAction();
            Unload();
        }

        void Unload()
        {
            Unit.unitMeshManager.GetHeldRangedWeapon().UnloadProjectile();
            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            if (Unit.IsPlayer)
                Unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

            Unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override int ActionPointsCost()
        {
            return Mathf.RoundToInt(defaultActionPointCost * (float)Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.RangedWeapon.ReloadActionPointCostMultiplier);
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment.RangedWeaponEquipped && Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;

        public override bool ActionIsUsedInstantly() => true;

        public override int InitialEnergyCost() => 0;

        public override string TooltipDescription()
        {
            if (Unit.unitMeshManager.GetHeldRangedWeapon().LoadedProjectile != null)
                return $"Unload the <b>{Unit.unitMeshManager.GetHeldRangedWeapon().LoadedProjectile.ItemData.Item.Name}</b> from your <b>{Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Name}</b>.";
            else
                return $"Unload your <b>{Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Name}</b>.";
        }
    }
}
