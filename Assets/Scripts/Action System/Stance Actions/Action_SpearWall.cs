using GridSystem;
using InventorySystem;
using System.Collections;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_SpearWall : Action_BaseStance
    {
        public override HeldItemStance HeldItemStance() => InventorySystem.HeldItemStance.SpearWall;

        bool spearRaised;
        public static readonly float knockbackChanceModifier = 3.5f;
        readonly int energyUsedOnAttack = 10;

        public override float NPCChanceToSwitchStance()
        {
            if (spearRaised) // Less chance to lower spear wall
                return 0.1f;

            // Having enemies in range means there's no chance to raise spear wall
            if (LevelGrid.HasAnyUnitWithinRange(Unit, Unit.GridPosition, Unit.GetAttackRange(), false, true, true))
                return 0f;
            return 0.25f;
        }

        public override int ActionPointsCost()
        {
            if (spearRaised)
                return 0; 
            
            HeldMeleeWeapon primaryHeldWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
            HeldMeleeWeapon secondaryHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            bool primaryWeaponValid = IsValidWeapon(primaryHeldWeapon);
            bool secondaryWeaponValid = IsValidWeapon(secondaryHeldWeapon);
            float cost = 0f;
            if (primaryWeaponValid && secondaryHeldWeapon)
                cost += ((baseAPCost * primaryHeldWeapon.ItemData.Item.Weight * 0.5f) + (cost += baseAPCost * secondaryHeldWeapon.ItemData.Item.Weight * 0.5f)) / 2f;
            else if (primaryWeaponValid)
                cost += baseAPCost * primaryHeldWeapon.ItemData.Item.Weight * 0.5f;
            else if (secondaryWeaponValid)
                cost += cost += baseAPCost * secondaryHeldWeapon.ItemData.Item.Weight * 0.5f;
            return Mathf.RoundToInt(cost);
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
            HeldMeleeWeapon rightHeldWeapon = Unit.UnitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!rightWeaponValid && !leftWeaponValid)
                return;

            spearRaised = true;

            if (rightWeaponValid)
                rightHeldWeapon.RaiseSpearWall();

            if (leftWeaponValid)
                leftHeldWeapon.RaiseSpearWall();

            Unit.Stats.EnergyUseActions.Add(this);

            if (rightWeaponValid)
                ApplyStanceStatModifiers(rightHeldWeapon.ItemData.Item.HeldEquipment);
            else
                ApplyStanceStatModifiers(leftHeldWeapon.ItemData.Item.HeldEquipment);

            Unit.UnitActionHandler.MoveAction.OnMove += CancelAction;
            Unit.Health.OnTakeDamageFromMeleeAttack += CancelAction;

            Unit.OpportunityAttackTrigger.OnEnemyEnterTrigger += AttackEnemy;
            Unit.OpportunityAttackTrigger.OnEnemyMoved += OnUnitInRangeMoved;

            Unit.Stats.OnKnockbackTarget += OnKnockback;
            Unit.Stats.OnFailedToKnockbackTarget += OnFailedKnockback;
        }

        void LowerSpear()
        {
            HeldMeleeWeapon rightHeldWeapon = Unit.UnitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (rightWeaponValid)
                rightHeldWeapon.LowerSpearWall();

            if (leftWeaponValid)
                leftHeldWeapon.LowerSpearWall();

            spearRaised = false;
            if (rightWeaponValid)
                RemoveStanceStatModifiers(rightHeldWeapon.ItemData.Item.HeldEquipment);
            else if (leftWeaponValid)
                RemoveStanceStatModifiers(leftHeldWeapon.ItemData.Item.HeldEquipment);

            Unit.Stats.EnergyUseActions.Remove(this);

            Unit.UnitActionHandler.MoveAction.OnMove -= CancelAction;
            Unit.Health.OnTakeDamageFromMeleeAttack -= CancelAction;

            Unit.OpportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
            Unit.OpportunityAttackTrigger.OnEnemyMoved -= OnUnitInRangeMoved;

            Unit.Stats.OnKnockbackTarget -= OnKnockback;
            Unit.Stats.OnFailedToKnockbackTarget -= OnFailedKnockback;
        }

        bool IsValidWeapon(HeldMeleeWeapon heldMeleeWeapon) => heldMeleeWeapon != null && heldMeleeWeapon.ItemData.Item.Weapon.HasAccessToAction(ActionType);

        public void OnKnockback()
        {
            Unit.Stats.UseEnergy(energyUsedOnAttack);
            if (Unit.Stats.CurrentEnergy == 0)
                CancelAction();
        }

        public void OnFailedKnockback(GridPosition targetUnitGridPosition)
        {
            Unit.Stats.UseEnergy(energyUsedOnAttack); 
            if (Vector3.Distance(Unit.WorldPosition, targetUnitGridPosition.WorldPosition) <= LevelGrid.diaganolDistance)
                CancelAction();
        }

        public void OnUnitInRangeMoved(Unit targetUnit, GridPosition enemyGridPosition) => AttackEnemy(targetUnit, enemyGridPosition);

        void AttackEnemy(Unit enemyUnit, GridPosition enemyGridPosition)
        {
            if (enemyUnit == null || enemyUnit.Health.IsDead || Unit.UnitActionHandler.MoveAction.IsMoving)
                return;

            if (Unit.Alliance.IsEnemy(enemyUnit) == false)
                return;

            // The Unit must be at least somewhat facing the enemyUnit
            if (Unit.Vision.IsDirectlyVisible(enemyUnit) == false || Unit.Vision.TargetInOpportunityAttackViewAngle(enemyUnit.transform) == false)
                return;

            // Check if the enemyUnit is within the Unit's attack range
            HeldMeleeWeapon rightHeldWeapon = Unit.UnitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!leftWeaponValid && !rightWeaponValid)
                return;

            float distanceToEnemy = Vector3.Distance(Unit.WorldPosition, enemyGridPosition.WorldPosition);
            
            bool inAttackRangeOfRightWeapon = false;
            if (rightWeaponValid)
                inAttackRangeOfRightWeapon = rightHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToEnemy && rightHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToEnemy;

            bool inAttackRangeOfLeftWeapon = false;
            if (leftWeaponValid)
                inAttackRangeOfLeftWeapon = leftHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToEnemy && leftHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToEnemy;
            
            if (!inAttackRangeOfLeftWeapon && !inAttackRangeOfRightWeapon)
                return;

            Unit.UnitActionHandler.GetAction<Action_Melee>().DoOpportunityAttack(enemyUnit);
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
            Unit.StartCoroutine(CancelAction_Coroutine());
        }

        IEnumerator CancelAction_Coroutine()
        {
            while (Unit.UnitActionHandler.IsAttacking)
                yield return null;

            LowerSpear();
            ActionSystemUI.UpdateActionVisuals();
            if (ActionBarSlot != null)
                ActionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment.MeleeWeaponEquipped;

        public override Sprite ActionIcon()
        {
            if (!IsValidWeapon(Unit.UnitMeshManager.GetRightHeldMeleeWeapon()) && !IsValidWeapon(Unit.UnitMeshManager.GetLeftHeldMeleeWeapon()))
                return ActionType.ActionIcon;

            if (!spearRaised)
                return ActionType.ActionIcon;
            return ActionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon rightHeldWeapon = Unit.UnitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!rightWeaponValid && !leftWeaponValid)
            {
                Debug.LogWarning($"Held Spear is null, yet {Unit.name} has a {name} available to them...");
                return "";
            }

            string weaponName;
            if (rightWeaponValid && leftWeaponValid)
                weaponName = "<b>both of your spears</b>";
            else if (rightWeaponValid)
                weaponName = $"your <b>{rightHeldWeapon.ItemData.Item.Name}</b>";
            else
                weaponName = $"your <b>{leftHeldWeapon.ItemData.Item.Name}</b>";

            if (!spearRaised)
                return $"Dig in your feet and raise {weaponName}. <b>Automatically attack</b> any enemies that come within attack range for <b>0 AP</b>, with a much larger <b>knockback</b> chance <b>({knockbackChanceModifier}x more likely)</b>. " +
                    $"If you fail to knock them back or if you take damage from a melee attack, you will be forced to lower your <b>Spear Wall</b>. Costs <b>{EnergyCostPerTurn()} Energy/Turn</b>.";
            else
                return $"Lower {weaponName}.";
        }

        public override string ActionName()
        {
            if (!IsValidWeapon(Unit.UnitMeshManager.GetRightHeldMeleeWeapon()) && !IsValidWeapon(Unit.UnitMeshManager.GetLeftHeldMeleeWeapon()))
                return "Spear Wall";

            if (!spearRaised)
                return "Spear Wall";
            else
                return "Lower Spear Wall";
        }

        public override int EnergyCost() => 5;

        public override float EnergyCostPerTurn()
        {
            float cost = 0f;
            if (IsValidWeapon(Unit.UnitMeshManager.GetRightHeldMeleeWeapon()))
                cost += 3f;

            if (IsValidWeapon(Unit.UnitMeshManager.GetLeftHeldMeleeWeapon()))
                cost += 3f;
            return cost;
        }

        public override bool IsInterruptable() => false;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;
    }
}
