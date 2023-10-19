using GridSystem;
using InventorySystem;
using System.Collections;
using System.Collections.Generic;
using UnitSystem;
using UnityEngine;
using Utilities;

namespace ActionSystem
{
    public abstract class BaseAttackAction : BaseAction
    {
        protected Unit targetEnemyUnit;

        public virtual void QueueAction(Unit targetEnemyUnit)
        {
            this.targetEnemyUnit = targetEnemyUnit;
            targetGridPosition = targetEnemyUnit.GridPosition;
            QueueAction();
        }

        protected void MoveToTargetInstead()
        {
            CompleteAction();
            unit.unitActionHandler.SetIsAttacking(false);
            unit.unitActionHandler.moveAction.QueueAction(GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));
            if (unit.IsPlayer)
                unit.unitActionHandler.TakeTurn();
        }

        protected override void StartAction()
        {
            base.StartAction();
            if (unit.IsPlayer && targetEnemyUnit != null)
                targetEnemyUnit.unitActionHandler.NPCActionHandler.SetStartChaseGridPosition(targetEnemyUnit.GridPosition);
        }

        protected void SetTargetEnemyUnit()
        {
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition))
            {
                Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);
                unit.unitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);
                targetEnemyUnit = unitAtGridPosition;
            }
            else if (unit.unitActionHandler.targetEnemyUnit != null)
                targetEnemyUnit = unit.unitActionHandler.targetEnemyUnit;
        }

        public virtual void DamageTarget(Unit targetUnit, HeldItem heldWeaponAttackingWith, HeldItem heldItemBlockedWith, bool headShot)
        {
            if (targetUnit != null && targetUnit.health.IsDead() == false)
            {
                int damageAmount = 0;
                if (heldWeaponAttackingWith != null)
                {
                    damageAmount = heldWeaponAttackingWith.itemData.Damage;
                    if (unit.UnitEquipment.IsDualWielding())
                    {
                        if (heldWeaponAttackingWith == unit.unitMeshManager.GetPrimaryMeleeWeapon())
                            damageAmount = Mathf.RoundToInt(damageAmount * GameManager.dualWieldPrimaryEfficiency);
                        else
                            damageAmount = Mathf.RoundToInt(damageAmount * GameManager.dualWieldSecondaryEfficiency);
                    }
                }
                else
                    damageAmount = UnarmedAttackDamage();

                // TODO: Calculate armor's damage absorbtion
                int armorAbsorbAmount = 0;

                // If the attack was blocked
                if (heldItemBlockedWith != null)
                {
                    int blockAmount = 0;
                    if (heldItemBlockedWith is HeldShield)
                    {
                        HeldShield shield = heldItemBlockedWith as HeldShield;
                        shield.LowerShield();

                        blockAmount = targetUnit.stats.ShieldBlockPower(shield);
                    }
                    else if (heldItemBlockedWith is HeldMeleeWeapon)
                    {
                        HeldMeleeWeapon meleeWeapon = heldItemBlockedWith as HeldMeleeWeapon;
                        meleeWeapon.LowerWeapon();

                        blockAmount = targetUnit.stats.WeaponBlockPower(meleeWeapon);

                        if (unit.UnitEquipment.IsDualWielding())
                        {
                            if (meleeWeapon == unit.unitMeshManager.GetRightHeldMeleeWeapon())
                                blockAmount = Mathf.RoundToInt(blockAmount * GameManager.dualWieldPrimaryEfficiency);
                            else
                                blockAmount = Mathf.RoundToInt(blockAmount * GameManager.dualWieldSecondaryEfficiency);
                        }
                    }

                    targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount - blockAmount, unit);
                }
                else
                    targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount, unit);
            }
        }

        public virtual void DamageTargets(HeldItem heldWeaponAttackingWith, bool headShot)
        {
            foreach (KeyValuePair<Unit, HeldItem> target in unit.unitActionHandler.targetUnits)
            {
                Unit targetUnit = target.Key;
                HeldItem itemBlockedWith = target.Value;
                DamageTarget(targetUnit, heldWeaponAttackingWith, itemBlockedWith, headShot);
            }

            unit.unitActionHandler.targetUnits.Clear();
        }

        public IEnumerator WaitToDamageTargets(HeldItem heldWeaponAttackingWith, bool headShot)
        {
            if (heldWeaponAttackingWith != null)
                yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(heldWeaponAttackingWith.itemData.Item as Weapon) / 2f);
            else
                yield return new WaitForSeconds(AnimationTimes.Instance.UnarmedAttackTime() / 2f);

            DamageTargets(heldWeaponAttackingWith, headShot);
        }

        public virtual int UnarmedAttackDamage()
        {
            return unit.stats.BaseUnarmedDamage;
        }

        protected virtual IEnumerator DoAttack()
        {
            // Target Units become aware of their attacker, if they weren't already
            foreach (Unit targetUnit in unit.unitActionHandler.targetUnits.Keys)
            {
                // The unit being attacked becomes aware of this unit
                if (targetUnit.unitActionHandler.targetUnits.Count == 1)
                    BecomeVisibleEnemyOfTarget(targetUnit);
                else
                {
                    if (targetUnit.alliance.IsEnemy(unit))
                        BecomeVisibleEnemyOfTarget(targetUnit);
                }
            }

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen())
            {
                if (targetEnemyUnit != null && targetEnemyUnit.unitActionHandler.isMoving)
                {
                    while (targetEnemyUnit.unitActionHandler.isMoving)
                        yield return null;

                    // If the target Unit moved out of range, queue a movement instead
                    if (IsInAttackRange(targetEnemyUnit) == false)
                    {
                        MoveToTargetInstead();
                        yield break;
                    }
                }

                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetGridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowardsPosition(targetGridPosition.WorldPosition(), false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.isRotating)
                    yield return null; 
                
                foreach (GridPosition gridPosition in GetActionAreaGridPositions(targetGridPosition))
                {
                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition) == false)
                        continue;

                    Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

                    // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
                    bool attackBlocked = targetUnit.unitActionHandler.TryBlockMeleeAttack(unit);
                    unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out HeldItem itemBlockedWith);

                    // If the target is successfully blocking the attack
                    if (attackBlocked)
                        itemBlockedWith.BlockAttack(unit);
                }

                // TODO: Come up with a method determining headshots
                bool headShot = false;

                unit.StartCoroutine(WaitToDamageTargets(unit.unitMeshManager.GetPrimaryMeleeWeapon(), headShot));

                // Play the attack animations and handle blocking for each target
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetGridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowardsPosition(targetGridPosition.WorldPosition(), true);

                // Loop through the grid positions in the attack area
                foreach (GridPosition gridPosition in GetActionAreaGridPositions(targetGridPosition))
                {
                    // Skip this position if there's no unit here
                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition) == false)
                        continue;

                    // Get the unit at this grid position
                    Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
                    bool headShot = false;

                    // The targetUnit tries to block the attack and if they do, they face their attacker
                    if (targetUnit.unitActionHandler.TryBlockMeleeAttack(unit))
                        targetUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                    // Damage this unit
                    DamageTargets(unit.unitMeshManager.GetPrimaryMeleeWeapon(), headShot);

                    unit.unitActionHandler.SetIsAttacking(false);
                }
            }

            // Wait until the attack lands before completing the action
            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompletAction early within MoveToTargetInstead
        }

        public abstract void PlayAttackAnimation();

        public abstract bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition);

        public abstract bool IsInAttackRange(Unit targetUnit);

        public abstract GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit);

        public abstract bool IsMeleeAttackAction();

        public abstract bool IsRangedAttackAction();
    }
}
