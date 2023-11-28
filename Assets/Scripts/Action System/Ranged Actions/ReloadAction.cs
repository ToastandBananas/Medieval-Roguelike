using InventorySystem;
using UnityEngine;
using UnitSystem.ActionSystem.UI;
using Utilities;
using ContextMenu = GeneralUI.ContextMenu;

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
            HeldRangedWeapon heldRangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon.IsLoaded)
                Unload(heldRangedWeapon);
            else
                Reload(heldRangedWeapon);
        }

        public override void OnActionSelected()
        {
            // If trying to reload a ranged weapon and the Player has a quiver with more than one type of projectile, bring up a context menu option asking which projectile to load up (if the ranged weapon is unloaded)
            if (Unit.UnitEquipment.QuiverEquipped() && Unit.UnitEquipment.RangedWeaponEquipped
                && Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded == false && Unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count > 1)
            {
                ContextMenu.BuildReloadContextMenu();
            }
            else // Just reload with the only type of ammo equipped (or unload if already loaded)
                QueueAction();
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

            if (Unit.IsPlayer)
            {
                Unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                ActionBarSlot.UpdateIcon();
            }

            Unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override int ActionPointsCost()
        {
            return Mathf.RoundToInt(defaultActionPointCost * (float)Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.RangedWeapon.ReloadActionPointCostMultiplier);
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment.RangedWeaponEquipped && (Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded || Unit.UnitEquipment.HasValidAmmunitionEquipped());

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;

        public override bool ActionIsUsedInstantly() => true;

        public override int InitialEnergyCost() => 0;

        public override Sprite ActionIcon()
        {
            if (Unit.UnitEquipment.RangedWeaponEquipped == false)
                return ActionType.ActionIcon;

            if (Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded)
                return ActionType.CancelActionIcon;
            return ActionType.ActionIcon;
        }

        public override string ActionName()
        {
            if (Unit.UnitEquipment.RangedWeaponEquipped == false)
                return "";

            HeldRangedWeapon rangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
            if (rangedWeapon.IsLoaded)
                return $"Unload {StringUtilities.EnumToSpacedString(rangedWeapon.ItemData.Item.Weapon.WeaponType)}";
            else
                return $"Reload {StringUtilities.EnumToSpacedString(rangedWeapon.ItemData.Item.Weapon.WeaponType)}";
        }

        public override string TooltipDescription()
        {
            if (Unit.UnitEquipment.RangedWeaponEquipped == false)
                return "";

            HeldRangedWeapon rangedWeapon = Unit.unitMeshManager.GetHeldRangedWeapon();
            if (rangedWeapon.IsLoaded)
            {
                if (rangedWeapon.LoadedProjectile != null)
                    return $"Unload the <b>{rangedWeapon.LoadedProjectile.ItemData.Item.Name}</b> from your <b>{rangedWeapon.ItemData.Item.Name}</b>.";
                else
                    return $"Unload your <b>{rangedWeapon.ItemData.Item.Name}</b>.";
            }
            else
                return $"Reload your <b>{rangedWeapon.ItemData.Item.Name}</b>.";
        }
    }
}
