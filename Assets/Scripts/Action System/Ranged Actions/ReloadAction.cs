using InventorySystem;
using UnityEngine;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public class ReloadAction : BaseAction
    {
        ItemData projectileItemData;

        readonly int defaultActionPointCost = 200;
        
        public void QueueAction(ItemData projectileItemData)
        {
            this.projectileItemData = projectileItemData;
            QueueAction();
        }

        public override void TakeAction()
        {
            StartAction();
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon.isLoaded)
                Unload(heldRangedWeapon);
            else
                Reload(heldRangedWeapon);
        }

        void Reload(HeldRangedWeapon heldRangedWeapon)
        {
            heldRangedWeapon.LoadProjectile(projectileItemData);
            CompleteAction();
        }

        void Unload(HeldRangedWeapon heldRangedWeapon)
        {
            heldRangedWeapon.UnloadProjectile();
            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();
            projectileItemData = null;

            if (unit.IsPlayer)
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int ActionPointsCost()
        {
            return Mathf.RoundToInt(defaultActionPointCost * (float)unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ReloadActionPointCostMultiplier);
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.RangedWeaponEquipped && (unit.unitMeshManager.GetHeldRangedWeapon().isLoaded || unit.UnitEquipment.HasValidAmmunitionEquipped());

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;

        public override bool ActionIsUsedInstantly() => true;

        public override int InitialEnergyCost() => 0;

        public override Sprite ActionIcon()
        {
            if (unit.UnitEquipment.RangedWeaponEquipped == false)
                return actionType.ActionIcon;

            if (unit.unitMeshManager.GetHeldRangedWeapon().isLoaded)
                return actionType.CancelActionIcon;
            return actionType.ActionIcon;
        }

        public override string ActionName()
        {
            if (unit.UnitEquipment.RangedWeaponEquipped == false)
                return "";

            HeldRangedWeapon rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (rangedWeapon.isLoaded)
                return $"Unload {StringUtilities.EnumToSpacedString(rangedWeapon.itemData.Item.Weapon.WeaponType)}";
            else
                return $"Reload {StringUtilities.EnumToSpacedString(rangedWeapon.itemData.Item.Weapon.WeaponType)}";
        }

        public override string TooltipDescription()
        {
            if (unit.UnitEquipment.RangedWeaponEquipped == false)
                return "";

            HeldRangedWeapon rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (rangedWeapon.isLoaded)
            {
                if (rangedWeapon.loadedProjectile != null)
                    return $"Unload the <b>{rangedWeapon.loadedProjectile.ItemData.Item.Name}</b> from your <b>{rangedWeapon.itemData.Item.Name}</b>.";
                else
                    return $"Unload your <b>{rangedWeapon.itemData.Item.Name}</b>.";
            }
            else
                return $"Reload your <b>{rangedWeapon.itemData.Item.Name}</b>.";
        }
    }
}
