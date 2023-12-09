using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_RaiseShield : Action_BaseStance
    {
        public static readonly float blockChanceModifier = 0.5f;

        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.RaiseShield;

        bool shieldRaised;

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * Unit.UnitMeshManager.GetHeldShield().ItemData.Item.Weight * 0.5f);

        public override float NPCChanceToSwitchStance()
        {
            if (Unit.UnitActionHandler.TargetEnemyUnit == null)
            {
                // No point in having shield raised if there's no target
                if (shieldRaised)
                    return 1f;
                else
                    return 0f;
            }

            float distanceToTargetEnemy = Vector3.Distance(Unit.WorldPosition, Unit.UnitActionHandler.TargetEnemyUnit.WorldPosition);
            if (distanceToTargetEnemy >= 2.9f)
            {
                // No point in having shield raised if target is far enough away
                if (shieldRaised)
                    return 1f;
                else
                    return 0f;
            }

            if (shieldRaised)
                return 0.1f;
            return 0.35f;
        }

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
            HeldShield heldShield = Unit.UnitMeshManager.GetHeldShield();
            if (heldShield == null)
                return;

            shieldRaised = true;
            heldShield.SetShouldKeepBlocking(true);

            heldShield.SetHeldItemStance(InventorySystem.HeldItemStance.RaiseShield);
            heldShield.RaiseShield();
            heldShield.Anim.SetBool("keepShieldRaised", true);

            Unit.Stats.EnergyUseActions.Add(this);

            ApplyStanceStatModifiers(heldShield.ItemData.Item.HeldEquipment);
        }

        void LowerShield()
        {
            HeldShield heldShield = Unit.UnitMeshManager.GetHeldShield();
            shieldRaised = false;

            if (heldShield != null)
            {
                heldShield.SetShouldKeepBlocking(false);

                heldShield.SetHeldItemStance(InventorySystem.HeldItemStance.Default);
                heldShield.LowerShield();
                heldShield.Anim.SetBool("keepShieldRaised", false);

                RemoveStanceStatModifiers(heldShield.ItemData.Item.HeldEquipment);
            }

            Unit.Stats.EnergyUseActions.Remove(this);
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
            LowerShield();
            if (ActionBarSlot != null)
                ActionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment.ShieldEquipped;

        public override Sprite ActionIcon()
        {
            HeldShield heldShield = Unit.UnitMeshManager.GetHeldShield();
            if (heldShield == null)
                return ActionType.ActionIcon;

            if (heldShield.CurrentHeldItemStance != HeldItemStance())
                return ActionType.ActionIcon;
            return ActionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldShield heldShield = Unit.UnitMeshManager.GetHeldShield();
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
            HeldShield heldShield = Unit.UnitMeshManager.GetHeldShield();
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
