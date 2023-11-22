using InventorySystem;
using System.Collections;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public class SpearWallAction : BaseStanceAction
    {
        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.SpearWall;

        bool spearRaised;
        public static readonly float knockbackChanceModifier = 1f;
        readonly int energyUsedOnAttack = 10;

        public override int ActionPointsCost()
        {
            if (spearRaised)
                return 0;
            return Mathf.RoundToInt(baseAPCost * unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weight * 0.5f);
        }

        public override void TakeAction()
        {
            StartAction();
            SwitchStance();
        }

        public override void SwitchStance()
        {
            if (spearRaised)
                LowerSpear();
            else
                RaiseSpear();

            CompleteAction();
        }

        void RaiseSpear()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
                return;

            spearRaised = true;
            heldSpear.SetHeldItemStance(InventorySystem.HeldItemStance.SpearWall);
            heldSpear.RaiseSpearWall();

            unit.stats.energyUseActions.Add(this);

            ApplyStanceStatModifiers(heldSpear.itemData.Item.HeldEquipment);

            unit.unitActionHandler.moveAction.OnMove += OnMove;
            unit.opportunityAttackTrigger.OnEnemyEnterTrigger += AttackEnemy;
        }

        void LowerSpear()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
                return;

            spearRaised = false;
            heldSpear.SetHeldItemStance(InventorySystem.HeldItemStance.Default);
            heldSpear.LowerSpearWall();

            unit.stats.energyUseActions.Remove(this);

            RemoveStanceStatModifiers(heldSpear.itemData.Item.HeldEquipment);

            unit.unitActionHandler.moveAction.OnMove -= OnMove;
            unit.opportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
        }

        public void OnKnockback()
        {
            unit.stats.UseEnergy(energyUsedOnAttack);
            if (unit.stats.currentEnergy == 0)
                CancelAction();
        }

        public void OnFailedKnockback()
        {
            unit.stats.UseEnergy(energyUsedOnAttack);
            CancelAction();
        }

        void AttackEnemy(Unit enemyUnit)
        {
            Debug.Log(enemyUnit);
            if (enemyUnit.health.IsDead)
                return;

            if (unit.alliance.IsEnemy(enemyUnit) == false)
                return;

            // The Unit must be at least somewhat facing the enemyUnit
            if (unit.vision.IsDirectlyVisible(enemyUnit) == false || unit.vision.TargetInOpportunityAttackViewAngle(enemyUnit.transform) == false)
                return;

            // Check if the enemyUnit is within the Unit's attack range
            MeleeAction meleeAction = unit.unitActionHandler.GetAction<MeleeAction>();
            if (meleeAction.IsInAttackRange(enemyUnit) == false)
                return;

            meleeAction.DoOpportunityAttack(enemyUnit);
        }

        void OnMove() => CancelAction();

        void OnDestroy()
        {
            unit.opportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
            unit.unitActionHandler.moveAction.OnMove -= OnMove;
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
            unit.StartCoroutine(CancelAction_Coroutine());
        }

        IEnumerator CancelAction_Coroutine()
        {
            while (unit.unitActionHandler.isAttacking)
                yield return null;

            LowerSpear();
            ActionSystemUI.UpdateActionVisuals();
            if (actionBarSlot != null)
                actionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.MeleeWeaponEquipped;

        public override Sprite ActionIcon()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
                return actionType.ActionIcon;

            if (!spearRaised)
                return actionType.ActionIcon;
            return actionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
            {
                Debug.LogWarning($"Held Spear is null, yet {unit.name} has a {name} available to them...");
                return "";
            }

            if (!spearRaised)
                return $"Dig in your feet and raise your <b>{heldSpear.itemData.Item.Name}</b>. <b>Automatically attack</b> any enemies that come within attack range for <b>0 AP</b>, potentially <b>knocking them back (+{knockbackChanceModifier * 100f} chance)</b>. If the enemy successfully moves to their target position, you will be forced to lower your spear. Costs <b>{EnergyCostPerTurn()} Energy/Turn</b>.";
            else
                return $"Lower your <b>{heldSpear.itemData.Item.Name}</b>.";
        }

        public override string ActionName()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
                return "Spear Wall";

            if (!spearRaised)
                return "Spear Wall";
            else
                return "Lower Spear Wall";
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
