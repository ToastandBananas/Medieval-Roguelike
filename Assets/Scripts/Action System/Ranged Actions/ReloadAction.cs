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
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (heldRangedWeapon.isLoaded)
                Unload(heldRangedWeapon);
            else
                Reload(heldRangedWeapon);
        }

        public override void OnActionSelected()
        {
            // If trying to reload a ranged weapon and the Player has a quiver with more than one type of projectile, bring up a context menu option asking which projectile to load up (if the ranged weapon is unloaded)
            if (unit.UnitEquipment.QuiverEquipped() && unit.UnitEquipment.RangedWeaponEquipped
                && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded == false && unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count > 1)
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

            if (unit.IsPlayer)
            {
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                actionBarSlot.UpdateIcon();
            }

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
