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
        readonly float knockbackChance = 0.5f;

        public override int ActionPointsCost() => Mathf.RoundToInt(baseAPCost * unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weight * 0.5f);

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

            unit.opportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
        }

        void AttackEnemy(Unit enemyUnit)
        {
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


        void OnDestroy()
        {
            unit.opportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
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
            LowerSpear();
        }

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.MeleeWeaponEquipped;

        public override Sprite ActionIcon()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
                return actionType.ActionIcon;

            if (heldSpear.currentHeldItemStance != HeldItemStance())
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

            if (heldSpear.currentHeldItemStance != HeldItemStance())
                return $"Dig in your feet and raise your <b>{heldSpear.itemData.Item.Name}</b>. <b>Automatically attack</b> any enemies that come within attack range for <b>0 AP</b>, potentially <b>knocking them back</b>. If the enemy successfully moves to their target position, you will be forced to lower your spear. Costs <b>{EnergyCostPerTurn()} Energy/Turn</b>.";
            else
                return $"Lower your <b>{heldSpear.itemData.Item.Name}</b>.";
        }

        public override string ActionName()
        {
            HeldMeleeWeapon heldSpear = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            if (heldSpear == null)
                return "Spear Wall";

            if (heldSpear.currentHeldItemStance != HeldItemStance())
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
