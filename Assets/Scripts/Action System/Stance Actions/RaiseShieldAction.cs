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

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * unit.unitMeshManager.GetHeldShield().itemData.Item.Weight * 0.5f);

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
            HeldShield heldShield = unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
                return;

            StanceStatModifier_ScriptableObject stanceStatModifier = heldShield.itemData.Item.Shield.GetStanceStatModifier(InventorySystem.HeldItemStance.RaiseShield);

            shieldRaised = true;
            heldShield.SetShouldKeepBlocking(true);

            heldShield.SetHeldItemStance(InventorySystem.HeldItemStance.RaiseShield);
            heldShield.RaiseShield();
            heldShield.anim.SetBool("keepShieldRaised", true);

            unit.stats.energyUseActions.Add(this);

            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.ApplyModifiers(unit.stats);
        }

        void LowerShield()
        {
            HeldShield heldShield = unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
                return;

            StanceStatModifier_ScriptableObject stanceStatModifier = heldShield.itemData.Item.Shield.GetStanceStatModifier(InventorySystem.HeldItemStance.RaiseShield);

            shieldRaised = false;
            heldShield.SetShouldKeepBlocking(false);

            heldShield.SetHeldItemStance(InventorySystem.HeldItemStance.Default);
            heldShield.LowerShield();
            heldShield.anim.SetBool("keepShieldRaised", false);

            unit.stats.energyUseActions.Remove(this);

            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.RemoveModifiers(unit.stats);
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
            LowerShield();
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.ShieldEquipped;

        public override Sprite ActionIcon()
        {
            HeldShield heldShield = unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
                return actionType.ActionIcon;

            if (heldShield.currentHeldItemStance != HeldItemStance())
                return actionType.ActionIcon;
            return actionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldShield heldShield = unit.unitMeshManager.GetHeldShield();
            if (heldShield == null)
            {
                Debug.LogWarning($"Held Shield is null, yet {unit.name} has a {name} available to them...");
                return "";
            }

            float speedModifier = 0f;
            StanceStatModifier_ScriptableObject stanceStatModifier = heldShield.itemData.Item.Shield.GetStanceStatModifier(InventorySystem.HeldItemStance.RaiseShield);
            if (stanceStatModifier != null)
                speedModifier = Mathf.RoundToInt(stanceStatModifier.StatModifier.PercentSpeed * 100f);
            
            if (heldShield.currentHeldItemStance != HeldItemStance())
                return $"Raise your <b>{heldShield.itemData.Item.Name}</b>, greatly <b>increasing</b> your shield's <b>Block Chance (+{blockChanceModifier * 100f}%)</b> at the detriment of <b>Speed ({speedModifier}%)</b>. Costs <b>{EnergyCostPerTurn()} Energy/Turn</b>.";
            else
                return $"Lower your <b>{heldShield.itemData.Item.Name}</b>.";
        }

        public override string ActionName()
        {
            if (unit.unitMeshManager.GetHeldShield().currentHeldItemStance != HeldItemStance())
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
