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
            
            HeldMeleeWeapon primaryHeldWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            HeldMeleeWeapon secondaryHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
            bool primaryWeaponValid = IsValidWeapon(primaryHeldWeapon);
            bool secondaryWeaponValid = IsValidWeapon(secondaryHeldWeapon);
            float cost = 0f;
            if (primaryWeaponValid && secondaryHeldWeapon)
                cost += ((baseAPCost * primaryHeldWeapon.itemData.Item.Weight * 0.5f) + (cost += baseAPCost * secondaryHeldWeapon.itemData.Item.Weight * 0.5f)) / 2f;
            else if (primaryWeaponValid)
                cost += baseAPCost * primaryHeldWeapon.itemData.Item.Weight * 0.5f;
            else if (secondaryWeaponValid)
                cost += cost += baseAPCost * secondaryHeldWeapon.itemData.Item.Weight * 0.5f;
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
            HeldMeleeWeapon rightHeldWeapon = unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!rightWeaponValid && !leftWeaponValid)
                return;

            spearRaised = true;

            if (rightWeaponValid)
                rightHeldWeapon.RaiseSpearWall();

            if (leftWeaponValid)
                leftHeldWeapon.RaiseSpearWall();

            unit.stats.energyUseActions.Add(this);

            if (rightWeaponValid)
                ApplyStanceStatModifiers(rightHeldWeapon.itemData.Item.HeldEquipment);
            else
                ApplyStanceStatModifiers(leftHeldWeapon.itemData.Item.HeldEquipment);

            unit.unitActionHandler.moveAction.OnMove += CancelAction;
            unit.health.OnTakeDamageFromMeleeAttack += CancelAction;
            unit.opportunityAttackTrigger.OnEnemyEnterTrigger += AttackEnemy;
            unit.opportunityAttackTrigger.OnEnemyMoved += OnUnitInRangeMoved;
        }

        void LowerSpear()
        {
            HeldMeleeWeapon rightHeldWeapon = unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (rightWeaponValid)
                rightHeldWeapon.LowerSpearWall();

            if (leftWeaponValid)
                leftHeldWeapon.LowerSpearWall();

            spearRaised = false;
            if (rightWeaponValid)
                RemoveStanceStatModifiers(rightHeldWeapon.itemData.Item.HeldEquipment);
            else if (leftWeaponValid)
                RemoveStanceStatModifiers(leftHeldWeapon.itemData.Item.HeldEquipment);

            unit.stats.energyUseActions.Remove(this);

            unit.unitActionHandler.moveAction.OnMove -= CancelAction;
            unit.health.OnTakeDamageFromMeleeAttack -= CancelAction;
            unit.opportunityAttackTrigger.OnEnemyEnterTrigger -= AttackEnemy;
            unit.opportunityAttackTrigger.OnEnemyMoved -= OnUnitInRangeMoved;
        }

        bool IsValidWeapon(HeldMeleeWeapon heldMeleeWeapon) => heldMeleeWeapon != null && heldMeleeWeapon.itemData.Item.Weapon.HasAccessToAction(actionType);

        public void OnKnockback()
        {
            unit.stats.UseEnergy(energyUsedOnAttack);
            if (unit.stats.currentEnergy == 0)
                CancelAction();
        }

        public void OnFailedKnockback(GridPosition targetUnitGridPosition)
        {
            unit.stats.UseEnergy(energyUsedOnAttack); 
            if (Vector3.Distance(unit.WorldPosition, targetUnitGridPosition.WorldPosition) <= LevelGrid.diaganolDistance)
                CancelAction();
        }

        public void OnUnitInRangeMoved(Unit targetUnit, GridPosition enemyGridPosition) => AttackEnemy(targetUnit, enemyGridPosition);

        void AttackEnemy(Unit enemyUnit, GridPosition enemyGridPosition)
        {
            if (enemyUnit == null || enemyUnit.health.IsDead)
                return;

            if (unit.alliance.IsEnemy(enemyUnit) == false)
                return;

            // The Unit must be at least somewhat facing the enemyUnit
            if (unit.vision.IsDirectlyVisible(enemyUnit) == false || unit.vision.TargetInOpportunityAttackViewAngle(enemyUnit.transform) == false)
                return;

            // Check if the enemyUnit is within the Unit's attack range
            HeldMeleeWeapon rightHeldWeapon = unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!leftWeaponValid && !rightWeaponValid)
                return;

            float distanceToEnemy = Vector3.Distance(unit.WorldPosition, enemyGridPosition.WorldPosition);
            
            bool inAttackRangeOfRightWeapon = false;
            if (rightWeaponValid)
                inAttackRangeOfRightWeapon = rightHeldWeapon.itemData.Item.Weapon.MaxRange >= distanceToEnemy && rightHeldWeapon.itemData.Item.Weapon.MinRange <= distanceToEnemy;

            bool inAttackRangeOfLeftWeapon = false;
            if (leftWeaponValid)
                inAttackRangeOfLeftWeapon = leftHeldWeapon.itemData.Item.Weapon.MaxRange >= distanceToEnemy && leftHeldWeapon.itemData.Item.Weapon.MinRange <= distanceToEnemy;
            
            if (!inAttackRangeOfLeftWeapon && !inAttackRangeOfRightWeapon)
                return;

            unit.unitActionHandler.GetAction<MeleeAction>().DoOpportunityAttack(enemyUnit);
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
            if (!IsValidWeapon(unit.unitMeshManager.GetRightHeldMeleeWeapon()) && !IsValidWeapon(unit.unitMeshManager.GetLeftHeldMeleeWeapon()))
                return actionType.ActionIcon;

            if (!spearRaised)
                return actionType.ActionIcon;
            return actionType.CancelActionIcon;
        }

        public override string TooltipDescription()
        {
            HeldMeleeWeapon rightHeldWeapon = unit.unitMeshManager.GetRightHeldMeleeWeapon();
            HeldMeleeWeapon leftHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
            bool rightWeaponValid = IsValidWeapon(rightHeldWeapon);
            bool leftWeaponValid = IsValidWeapon(leftHeldWeapon);

            if (!rightWeaponValid && !leftWeaponValid)
            {
                Debug.LogWarning($"Held Spear is null, yet {unit.name} has a {name} available to them...");
                return "";
            }

            string weaponName;
            if (rightWeaponValid && leftWeaponValid)
                weaponName = "<b>both of your spears</b>";
            else if (rightWeaponValid)
                weaponName = $"your <b>{rightHeldWeapon.itemData.Item.Name}</b>";
            else
                weaponName = $"your <b>{leftHeldWeapon.itemData.Item.Name}</b>";

            if (!spearRaised)
                return $"Dig in your feet and raise {weaponName}. <b>Automatically attack</b> any enemies that come within attack range for <b>0 AP</b>, with a much larger <b>knockback</b> chance <b>({knockbackChanceModifier}x more likely)</b>. " +
                    $"If you fail to knock them back or if you take damage from a melee attack, you will be forced to lower your <b>Spear Wall</b>. Costs <b>{EnergyCostPerTurn()} Energy/Turn</b>.";
            else
                return $"Lower {weaponName}.";
        }

        public override string ActionName()
        {
            if (!IsValidWeapon(unit.unitMeshManager.GetRightHeldMeleeWeapon()) && !IsValidWeapon(unit.unitMeshManager.GetLeftHeldMeleeWeapon()))
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
            if (IsValidWeapon(unit.unitMeshManager.GetRightHeldMeleeWeapon()))
                cost += 3f;

            if (IsValidWeapon(unit.unitMeshManager.GetLeftHeldMeleeWeapon()))
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
