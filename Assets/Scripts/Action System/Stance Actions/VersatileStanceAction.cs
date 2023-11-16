using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem 
{
    public class VersatileStanceAction : BaseStanceAction
    {
        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.Versatile;

        public override void TakeAction()
        {
            StartAction();
            SwitchStance();
        }

        public override void SwitchStance()
        {
            unit.UnitEquipment.SwitchVersatileStance();
            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override bool IsValidAction() => unit.UnitEquipment.IsDualWielding == false && unit.UnitEquipment.MeleeWeaponEquipped && unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weapon.IsVersatile;

        public override Sprite ActionIcon()
        {
            Debug.Log(IsValidAction() + " | " + unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().currentHeldItemStance.ToString());
            if (IsValidAction() == false)
                return actionType.ActionIcon;

            if (unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().currentHeldItemStance != HeldItemStance())
                return actionType.ActionIcon;
            return actionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon heldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldMeleeWeapon == null) 
            {
                Debug.LogWarning($"Melee weapon is null, yet {unit.name} has a VersatileStanceActon available to them...");
                return "";
            }

            if (heldMeleeWeapon.currentHeldItemStance != HeldItemStance())
                return $"Grip your <b>{heldMeleeWeapon.itemData.Item.Name}</b> with both hands, <b>increasing</b> both <b>damage</b> and <b>AP cost</b>.";
            else
                return $"Grip your <b>{heldMeleeWeapon.itemData.Item.Name}</b> with one hand, <b>decreasing</b> both <b>damage</b> and <b>AP cost</b>.";
        }

        public override string ActionName()
        {
            if (unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().currentHeldItemStance != HeldItemStance())
                return "Two-Hand Weapon Stance";
            else
                return "One-Hand Weapon Stance";
        }

        public override int GetEnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;
    }
}
