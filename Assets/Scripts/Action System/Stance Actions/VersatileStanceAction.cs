using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem 
{
    public class VersatileStanceAction : BaseStanceAction
    {
        public static float damageModifier = 1.25f;
        public static float APCostModifier = 1.35f;

        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.Versatile;

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weight * 0.5f);

        public override void TakeAction()
        {
            StartAction();
            SwitchStance();
        }

        public override void SwitchStance()
        {
            SwitchVersatileStance();
            CompleteAction();
        }

        public void SwitchVersatileStance()
        {
            if (unit.UnitEquipment.IsDualWielding || unit.UnitEquipment.MeleeWeaponEquipped == false)
                return;

            HeldMeleeWeapon primaryHeldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon != null)
            {
                if (primaryHeldMeleeWeapon.itemData.Item.Weapon.IsVersatile == false || primaryHeldMeleeWeapon.itemData.Item.Weapon.IsTwoHanded)
                    return;

                primaryHeldMeleeWeapon.HeldMeleeWeapon.SwitchVersatileStance();
            }
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override void CancelAction()
        {
            base.CancelAction();
            HeldMeleeWeapon primaryHeldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon != null && primaryHeldMeleeWeapon.currentHeldItemStance == HeldItemStance())
                primaryHeldMeleeWeapon.HeldMeleeWeapon.SwitchVersatileStance();
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.IsDualWielding == false && unit.UnitEquipment.MeleeWeaponEquipped && unit.UnitEquipment.ShieldEquipped == false && unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weapon.IsVersatile;

        public override Sprite ActionIcon()
        {
            HeldMeleeWeapon primaryHeldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon == null)
                return actionType.ActionIcon;

            if (primaryHeldMeleeWeapon.currentHeldItemStance != HeldItemStance())
                return actionType.ActionIcon;
            return actionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon heldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldMeleeWeapon == null) 
            {
                Debug.LogWarning($"Melee weapon is null, yet {unit.name} has a {name} available to them...");
                return "";
            }

            if (heldMeleeWeapon.currentHeldItemStance != HeldItemStance())
                return $"Grip your <b>{heldMeleeWeapon.itemData.Item.Name}</b> with both hands, <b>increasing</b> both <b>Damage (+{(damageModifier - 1f) * 100f}%)</b> and the <b>AP Cost (+{(APCostModifier - 1f) * 100f})</b> of attacks with this weapon.";
            else
                return $"Grip your <b>{heldMeleeWeapon.itemData.Item.Name}</b> with one hand, <b>decreasing</b> both <b>Damage (-{(damageModifier - 1f) * 100f}%)</b> and the <b>AP Cost ({(APCostModifier - 1f) * 100f})</b> of attacks with this weapon.";
        }

        public override string ActionName()
        {
            HeldMeleeWeapon heldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldMeleeWeapon == null)
                return "Two-Hand Weapon Stance";

            if (heldMeleeWeapon.currentHeldItemStance != HeldItemStance())
                return "Two-Hand Weapon Stance";
            else
                return "One-Hand Weapon Stance";
        }

        public override int InitialEnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;
    }
}
