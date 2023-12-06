using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public class VersatileStanceAction : BaseStanceAction
    {
        public static float damageModifier = 1.25f;
        public static float APCostModifier = 1.35f;

        public bool inVersatileStance { get; private set; }

        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.Versatile;

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weight * 0.5f);

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
            if (Unit.UnitEquipment.IsDualWielding || Unit.UnitEquipment.MeleeWeaponEquipped == false)
                return;

            HeldMeleeWeapon primaryHeldMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
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

            Unit.UnitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override void CancelAction()
        {
            base.CancelAction();
            HeldMeleeWeapon primaryHeldMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon != null && inVersatileStance)
                primaryHeldMeleeWeapon.HeldMeleeWeapon.SetDefaultWeaponStance();

            if (ActionBarSlot != null)
                ActionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction()
        {
            if (Unit != null && Unit.UnitEquipment.IsDualWielding == false && Unit.UnitEquipment.MeleeWeaponEquipped && Unit.UnitEquipment.ShieldEquipped == false)
            {
                HeldMeleeWeapon primaryHeldMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (primaryHeldMeleeWeapon.CurrentHeldItemStance != InventorySystem.HeldItemStance.Default && primaryHeldMeleeWeapon.CurrentHeldItemStance != InventorySystem.HeldItemStance.Versatile)
                    return false;
                else
                    return true;
            }
            return false;
        }

        public override Sprite ActionIcon()
        {
            HeldMeleeWeapon primaryHeldMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon == null)
                return ActionType.ActionIcon;

            if (!inVersatileStance)
                return ActionType.ActionIcon;
            return ActionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon heldMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldMeleeWeapon == null) 
            {
                Debug.LogWarning($"Melee weapon is null, yet {Unit.name} has a {name} available to them...");
                return "";
            }

            if (!inVersatileStance)
                return $"Grip your <b>{heldMeleeWeapon.ItemData.Item.Name}</b> with both hands, <b>increasing</b> both <b>Damage (+{(damageModifier - 1f) * 100f}%)</b> and the <b>AP Cost (+{(APCostModifier - 1f) * 100f})</b> of attacks with this weapon.";
            else
                return $"Grip your <b>{heldMeleeWeapon.ItemData.Item.Name}</b> with one hand, <b>decreasing</b> both <b>Damage (-{(damageModifier - 1f) * 100f}%)</b> and the <b>AP Cost ({(APCostModifier - 1f) * 100f})</b> of attacks with this weapon.";
        }

        public override string ActionName()
        {
            HeldMeleeWeapon heldMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
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
