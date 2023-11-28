using GridSystem;
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
        public static readonly float knockbackChanceModifier = 3.5f;
        readonly int energyUsedOnAttack = 10;

        public override int ActionPointsCost()
        {
            if (spearRaised)
                return 0; 
            
            HeldMeleeWeapon primaryHeldWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            HeldMeleeWeapon secondaryHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
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
            HeldMeleeWeapon rightHeldWeapon = Unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!rightWeaponValid && !leftWeaponValid)
                return;

            spearRaised = true;

            if (rightWeaponValid)
                rightHeldWeapon.RaiseSpearWall();

            if (leftWeaponValid)
                leftHeldWeapon.RaiseSpearWall();

            Unit.stats.energyUseActions.Add(this);

            if (rightWeaponValid)
                ApplyStanceStatModifiers(rightHeldWeapon.ItemData.Item.HeldEquipment);
            else
                ApplyStanceStatModifiers(leftHeldWeapon.ItemData.Item.HeldEquipment);

            Unit.unitActionHandler.MoveAction.OnMove += CancelAction;
            Unit.health.OnTakeDamageFromMeleeAttack += CancelAction;
            Unit.opportunityAttackTrigger.OnEnemyEnterTrigger += AttackEnemy;
            Unit.opportunityAttackTrigger.OnEnemyMoved += OnUnitInRangeMoved;
        }

        void LowerSpear()
        {
            HeldMeleeWeapon rightHeldWeapon = Unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
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

            Unit.stats.energyUseActions.Remove(this);

            Unit.unitActionHandler.MoveAction.OnMove -= CancelAction;
            Unit.health.OnTakeDamageFromMeleeAttack -= CancelAction;
            Unit.opportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
            Unit.opportunityAttackTrigger.OnEnemyMoved -= OnUnitInRangeMoved;
        }

        bool IsValidWeapon(HeldMeleeWeapon heldMeleeWeapon) => heldMeleeWeapon != null && heldMeleeWeapon.ItemData.Item.Weapon.HasAccessToAction(ActionType);

        public void OnKnockback()
        {
            Unit.stats.UseEnergy(energyUsedOnAttack);
            if (Unit.stats.currentEnergy == 0)
                CancelAction();
        }

        public void OnFailedKnockback(GridPosition targetUnitGridPosition)
        {
            Unit.stats.UseEnergy(energyUsedOnAttack); 
            if (Vector3.Distance(Unit.WorldPosition, targetUnitGridPosition.WorldPosition) <= LevelGrid.diaganolDistance)
                CancelAction();
        }

        public void OnUnitInRangeMoved(Unit targetUnit, GridPosition enemyGridPosition) => AttackEnemy(targetUnit, enemyGridPosition);

        void AttackEnemy(Unit enemyUnit, GridPosition enemyGridPosition)
        {
            if (enemyUnit == null || enemyUnit.health.IsDead)
                return;

            if (Unit.alliance.IsEnemy(enemyUnit) == false)
                return;

            // The Unit must be at least somewhat facing the enemyUnit
            if (Unit.vision.IsDirectlyVisible(enemyUnit) == false || Unit.vision.TargetInOpportunityAttackViewAngle(enemyUnit.transform) == false)
                return;

            // Check if the enemyUnit is within the Unit's attack range
            HeldMeleeWeapon rightHeldWeapon = Unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
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

            Unit.unitActionHandler.GetAction<MeleeAction>().DoOpportunityAttack(enemyUnit);
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
            Unit.StartCoroutine(CancelAction_Coroutine());
        }

        IEnumerator CancelAction_Coroutine()
        {
            while (Unit.unitActionHandler.IsAttacking)
                yield return null;

            LowerSpear();
            ActionSystemUI.UpdateActionVisuals();
            if (ActionBarSlot != null)
                ActionBarSlot.UpdateIcon();
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment.MeleeWeaponEquipped;

        public override Sprite ActionIcon()
        {
            if (!IsValidWeapon(Unit.unitMeshManager.GetRightHeldMeleeWeapon()) && !IsValidWeapon(Unit.unitMeshManager.GetLeftHeldMeleeWeapon()))
                return ActionType.ActionIcon;

            if (!spearRaised)
                return ActionType.ActionIcon;
            return ActionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon rightHeldWeapon = Unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = Unit.unitMeshManager.GetLeftHeldMeleeWeapon();
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
            if (!IsValidWeapon(Unit.unitMeshManager.GetRightHeldMeleeWeapon()) && !IsValidWeapon(Unit.unitMeshManager.GetLeftHeldMeleeWeapon()))
                return "Spear Wall";

            if (!spearRaised)
                return "Spear Wall";
            else
                return "Lower Spear Wall";
        }

        public override int InitialEnergyCost() => 5;

        public override float EnergyCostPerTurn()
        {
            float cost = 0f;
            if (IsValidWeapon(Unit.unitMeshManager.GetRightHeldMeleeWeapon()))
                cost += 3f;

            if (IsValidWeapon(Unit.unitMeshManager.GetLeftHeldMeleeWeapon()))
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
