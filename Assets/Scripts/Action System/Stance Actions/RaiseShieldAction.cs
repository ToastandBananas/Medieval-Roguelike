using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public class RaiseShieldAction : BaseStanceAction
    {
        public static readonly float blockChanceModifier = 0.5f;

        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.RaiseShield;

        bool shieldRaised;

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * Unit.unitMeshManager.GetHeldShield().ItemData.Item.Weight * 0.5f);

        public override void TakeAction()
        {
            StartAction();
            SwitchStance();
        }

        public override void SwitchStance()
        {
            if (shieldRaised)
                LowerShield();
            else
                RaiseShield();

            CompleteAction();
        }

        void RaiseShield()
        {
            HeldShield heldShield = Unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
                return;

            shieldRaised = true;
            heldShield.SetShouldKeepBlocking(true);

            heldShield.SetHeldItemStance(InventorySystem.HeldItemStance.RaiseShield);
            heldShield.RaiseShield();
            heldShield.Anim.SetBool("keepShieldRaised", true);

            Unit.stats.energyUseActions.Add(this);

            ApplyStanceStatModifiers(heldShield.ItemData.Item.HeldEquipment);
        }

        void LowerShield()
        {
            HeldShield heldShield = Unit.unitMeshManager.GetHeldShield();
            shieldRaised = false;

            if (heldShield != null)
            {
                heldShield.SetShouldKeepBlocking(false);

                heldShield.SetHeldItemStance(InventorySystem.HeldItemStance.Default);
                heldShield.LowerShield();
                heldShield.Anim.SetBool("keepShieldRaised", false);

                RemoveStanceStatModifiers(heldShield.ItemData.Item.HeldEquipment);
            }

            Unit.stats.energyUseActions.Remove(this);
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            Unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override void CancelAction()
        {
            base.CancelAction();
            LowerShield();
            if (ActionBarSlot != null)
                ActionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment.ShieldEquipped;

        public override Sprite ActionIcon()
        {
            HeldShield heldShield = Unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
                return ActionType.ActionIcon;

            if (heldShield.CurrentHeldItemStance != HeldItemStance())
                return ActionType.ActionIcon;
            return ActionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldShield heldShield = Unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
            {
                Debug.LogWarning($"Held Shield is null, yet {Unit.name} has a {name} available to them...");
                return "";
            }

            float speedModifier = 0f;
            StanceStatModifier_ScriptableObject stanceStatModifier = heldShield.ItemData.Item.Shield.GetStanceStatModifier(InventorySystem.HeldItemStance.RaiseShield);
            if (stanceStatModifier != null)
                speedModifier = Mathf.RoundToInt(stanceStatModifier.StatModifier.PercentSpeed * 100f);
            
            if (heldShield.CurrentHeldItemStance != HeldItemStance())
                return $"Raise your <b>{heldShield.ItemData.Item.Name}</b>, greatly <b>increasing</b> your shield's <b>Block Chance (+{blockChanceModifier * 100f}%)</b> at the detriment of <b>Speed ({speedModifier}%)</b>. Costs <b>{EnergyCostPerTurn()} Energy/Turn</b>.";
            else
                return $"Lower your <b>{heldShield.ItemData.Item.Name}</b>.";
        }

        public override string ActionName()
        {
            HeldShield heldShield = Unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
                return "Raise Shield";

            if (heldShield.CurrentHeldItemStance != HeldItemStance())
                return "Raise Shield";
            else
                return "Lower Shield";
        }

        public override int InitialEnergyCost() => 5;

        public override float EnergyCostPerTurn() => 3f;

        public override bool IsInterruptable() => false;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;
    }
}
