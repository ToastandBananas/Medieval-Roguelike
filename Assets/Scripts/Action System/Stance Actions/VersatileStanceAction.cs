using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem 
{
    public class VersatileStanceAction : BaseStanceAction
    {
        public static float damageModifier = 1.25f;
        public static float APCostModifier = 1.35f;

        public bool inVersatileStance { get; private set; }

        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.Versatile;

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weight * 0.5f);

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
                if (primaryHeldMeleeWeapon.ItemData.Item.Weapon.IsTwoHanded)
                {
                    Debug.LogWarning($"{primaryHeldMeleeWeapon.ItemData.Item.Name} is Two-Handed, yet it has a Versatile Stance Action available to it...");
                    return;
                }

                if (inVersatileStance)
                {
                    primaryHeldMeleeWeapon.HeldMeleeWeapon.SetDefaultWeaponStance();
                    inVersatileStance = false;
                }
                else
                {
                    primaryHeldMeleeWeapon.HeldMeleeWeapon.SetVersatileWeaponStance();
                    inVersatileStance = true;
                }
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
            if (primaryHeldMeleeWeapon != null && inVersatileStance)
                primaryHeldMeleeWeapon.HeldMeleeWeapon.SetDefaultWeaponStance();

            if (actionBarSlot != null)
                actionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction()
        {
            if (unit != null && unit.UnitEquipment.IsDualWielding == false && unit.UnitEquipment.MeleeWeaponEquipped && unit.UnitEquipment.ShieldEquipped == false)
            {
                HeldMeleeWeapon primaryHeldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (primaryHeldMeleeWeapon.CurrentHeldItemStance != InventorySystem.HeldItemStance.Default && primaryHeldMeleeWeapon.CurrentHeldItemStance != InventorySystem.HeldItemStance.Versatile)
                    return false;
                else
                    return true;
            }
            return false;
        }

        public override Sprite ActionIcon()
        {
            HeldMeleeWeapon primaryHeldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon == null)
                return actionType.ActionIcon;

            if (!inVersatileStance)
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

            if (!inVersatileStance)
                return $"Grip your <b>{heldMeleeWeapon.ItemData.Item.Name}</b> with both hands, <b>increasing</b> both <b>Damage (+{(damageModifier - 1f) * 100f}%)</b> and the <b>AP Cost (+{(APCostModifier - 1f) * 100f})</b> of attacks with this weapon.";
            else
                return $"Grip your <b>{heldMeleeWeapon.ItemData.Item.Name}</b> with one hand, <b>decreasing</b> both <b>Damage (-{(damageModifier - 1f) * 100f}%)</b> and the <b>AP Cost ({(APCostModifier - 1f) * 100f})</b> of attacks with this weapon.";
        }

        public override string ActionName()
        {
            HeldMeleeWeapon heldMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldMeleeWeapon == null)
                return "Two-Hand Weapon Stance";

            if (!inVersatileStance)
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
