using InventorySystem;
using UnitSystem;
using UnityEngine;

namespace ActionSystem
{
    public class ReloadAction : BaseAction
    {
        ItemData projectileItemData;
        // bool isReloading;

        readonly int defaultActionPointCost = 200;
        
        public void QueueAction(ItemData projectileItemData)
        {
            this.projectileItemData = projectileItemData;
            QueueAction();
        }

        public override void TakeAction()
        {
            if (unit == null || unit.unitActionHandler.AvailableActions.Contains(this) == false)// || isReloading)
                return;

            StartAction();
            Reload();
        }

        void Reload()
        {
            unit.unitMeshManager.GetHeldRangedWeapon().LoadProjectile(projectileItemData);
            CompleteAction();
        }

        /*protected override void StartAction()
        {
            base.StartAction();
            // isReloading = true;
        }*/

        public override void CompleteAction()
        {
            base.CompleteAction();
            projectileItemData = null;
            // isReloading = false;

            if (unit.IsPlayer)
                unit.unitActionHandler.SetDefaultSelectedAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetActionPointsCost()
        {
            return Mathf.RoundToInt(defaultActionPointCost * (float)unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ReloadActionPointCostMultiplier);
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.RangedWeaponEquipped() && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded == false && unit.UnitEquipment.HasValidAmmunitionEquipped();

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => ActionSystem.ActionBarSection.Basic;

        public override bool ActionIsUsedInstantly() => true;

        public override int GetEnergyCost() => 0;

        public override string TooltipDescription()
        {
            return $"Reload your <b>{unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Name}</b>.";
        }
    }
}
