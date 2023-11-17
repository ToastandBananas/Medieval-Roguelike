using GridSystem;
using InventorySystem;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace UnitSystem.ActionSystem
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
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        protected override void StartAction()
        {
            base.StartAction();
            if (unit.IsPlayer && targetEnemyUnit != null)
                targetEnemyUnit.unitActionHandler.NPCActionHandler.SetStartChaseGridPosition(targetEnemyUnit.GridPosition);
        }

        protected void SetTargetEnemyUnit()
        {
            if (LevelGrid.HasAnyUnitOnGridPosition(targetGridPosition, out Unit unitAtGridPosition))
            {
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
                targetUnit.unitActionHandler.InterruptActions();

                float damageAmount;
                if (heldWeaponAttackingWith != null)
                {
                    damageAmount = heldWeaponAttackingWith.itemData.Damage;
                    if (unit.UnitEquipment.IsDualWielding)
                    {
                        if (heldWeaponAttackingWith == unit.unitMeshManager.GetPrimaryHeldMeleeWeapon())
                            damageAmount *= Weapon.dualWieldPrimaryEfficiency;
                        else
                            damageAmount *= Weapon.dualWieldSecondaryEfficiency;
                    }
                    else if (unit.UnitEquipment.InVersatileStance)
                        damageAmount *= VersatileStanceAction.damageModifier;
                }
                else
                    damageAmount = UnarmedAttackDamage();

                // TODO: Calculate armor's damage absorbtion
                float armorAbsorbAmount = 0f;

                // If the attack was blocked
                if (heldItemBlockedWith != null)
                {
                    float blockAmount = 0f;
                    if (heldItemBlockedWith is HeldShield)
                    {
                        HeldShield shield = heldItemBlockedWith as HeldShield;
                        if (shield.currentHeldItemStance != HeldItemStance.RaiseShield)
                            shield.LowerShield();

                        blockAmount = targetUnit.stats.BlockPower(shield);

                        shield.TryFumbleHeldItem();
                    }
                    else if (heldItemBlockedWith is HeldMeleeWeapon)
                    {
                        HeldMeleeWeapon meleeWeapon = heldItemBlockedWith as HeldMeleeWeapon;
                        meleeWeapon.LowerWeapon();

                        blockAmount = targetUnit.stats.BlockPower(meleeWeapon);

                        if (unit.UnitEquipment.IsDualWielding)
                        {
                            if (meleeWeapon == unit.unitMeshManager.GetRightHeldMeleeWeapon())
                                blockAmount *= Weapon.dualWieldPrimaryEfficiency;
                            else
                                blockAmount *= Weapon.dualWieldSecondaryEfficiency;
                        }

                        meleeWeapon.TryFumbleHeldItem();
                    }

                    targetUnit.health.TakeDamage(Mathf.RoundToInt(damageAmount - armorAbsorbAmount - blockAmount), unit);
                }
                else
                    targetUnit.health.TakeDamage(Mathf.RoundToInt(damageAmount - armorAbsorbAmount), unit);
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

            if (heldWeaponAttackingWith != null)
                heldWeaponAttackingWith.TryFumbleHeldItem();
        }

        public virtual IEnumerator WaitToDamageTargets(HeldItem heldWeaponAttackingWith)
        {
            // TODO: Come up with a headshot method
            bool headShot = false;

            if (heldWeaponAttackingWith != null)
                yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(heldWeaponAttackingWith.itemData.Item as Weapon));
            else
                yield return new WaitForSeconds(AnimationTimes.Instance.UnarmedAttackTime() * 0.5f);

            DamageTargets(heldWeaponAttackingWith, headShot);
        }

        public virtual int UnarmedAttackDamage()
        {
            return unit.stats.BaseUnarmedDamage;
        }

        protected virtual IEnumerator DoAttack()
        {
            // Get the Units within the attack position
            List<Unit> targetUnits = ListPool<Unit>.Claim(); 
            foreach (GridPosition gridPosition in GetActionAreaGridPositions(targetGridPosition))
            {
                if (LevelGrid.HasAnyUnitOnGridPosition(gridPosition, out Unit targetUnit) == false)
                    continue;

                targetUnits.Add(targetUnit);

                // The unit being attacked becomes aware of this unit
                unit.vision.BecomeVisibleUnitOfTarget(targetUnit, targetUnit.unitActionHandler.targetUnits.Count == 1);
            }

            // We need to skip a frame in case the target Unit's meshes are being enabled due to becoming visible
            yield return null; 
            
            HeldMeleeWeapon primaryMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            ItemData weaponItemData = null;
            if (primaryMeleeWeapon != null)
                weaponItemData = primaryMeleeWeapon.itemData;

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (unit.IsPlayer || (targetEnemyUnit != null && targetEnemyUnit.IsPlayer) || unit.unitMeshManager.IsVisibleOnScreen || (targetEnemyUnit != null && targetEnemyUnit.unitMeshManager.IsVisibleOnScreen))
            {
                if (targetEnemyUnit != null)
                {
                    while (targetEnemyUnit.unitActionHandler.moveAction.isMoving)
                        yield return null;

                    // If the target Unit moved out of range, queue a movement instead
                    if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false)
                    {
                        MoveToTargetInstead();
                        yield break;
                    }
                }

                // Rotate towards the target
                if (targetUnits.Count == 1) // If there's only 1 target Unit in the attack area, they mind as well just face that target
                {
                    if (unit.unitActionHandler.turnAction.IsFacingTarget(targetUnits[0].GridPosition) == false)
                        unit.unitActionHandler.turnAction.RotateTowardsPosition(targetUnits[0].transform.position, false);
                }
                else
                {
                    if (unit.unitActionHandler.turnAction.IsFacingTarget(targetGridPosition) == false)
                        unit.unitActionHandler.turnAction.RotateTowardsPosition(targetGridPosition.WorldPosition, false);
                }

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.turnAction.isRotating)
                    yield return null;

                for (int i = 0; i < targetUnits.Count; i++)
                {
                    // The targetUnit tries to dodge, and if they fail that, they try to block instead
                    if (targetUnits[i].unitActionHandler.TryDodgeAttack(unit, weaponItemData, this, false))
                        targetUnits[i].unitAnimator.DoDodge(unit, null);
                    else
                    {
                        // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
                        bool attackBlocked = targetUnits[i].unitActionHandler.TryBlockMeleeAttack(unit, weaponItemData, false);
                        unit.unitActionHandler.targetUnits.TryGetValue(targetUnits[i], out HeldItem itemBlockedWith);

                        // If the target is successfully blocking the attack
                        if (attackBlocked && itemBlockedWith != null)
                            itemBlockedWith.BlockAttack(unit);
                    }
                }

                unit.StartCoroutine(WaitToDamageTargets(primaryMeleeWeapon));

                // Play the attack animations and handle blocking for each target
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetGridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowardsPosition(targetGridPosition.WorldPosition, true);

                for (int i = 0; i < targetUnits.Count; i++)
                {
                    // The targetUnit tries to dodge, and if they fail that, they try to block instead
                    if (targetUnits[i].unitActionHandler.TryDodgeAttack(unit, weaponItemData, this, false) == false)
                    {
                        bool headShot = false;

                        // The targetUnit tries to block the attack and if they do, they face their attacker
                        if (targetUnits[i].unitActionHandler.TryBlockMeleeAttack(unit, weaponItemData, false))
                            targetUnits[i].unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                        // Damage this unit
                        DamageTargets(primaryMeleeWeapon, headShot);
                    }
                }

                unit.unitActionHandler.SetIsAttacking(false);
            }

            ListPool<Unit>.Release(targetUnits);

            // Wait until the attack lands before completing the action
            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompletAction early within MoveToTargetInstead
        }

        public bool OtherUnitInTheWay(Unit unit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            Unit targetUnit = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (targetUnit == null)
                return false;

            // Check if there's a Unit in the way of the attack
            float raycastDistance = Vector3.Distance(startGridPosition.WorldPosition, targetGridPosition.WorldPosition);
            Vector3 attackDir = (targetGridPosition.WorldPosition - startGridPosition.WorldPosition).normalized;
            if (Physics.Raycast(startGridPosition.WorldPosition, attackDir, out RaycastHit hit, raycastDistance, unit.vision.UnitsMask))
            {
                if (hit.collider.gameObject != unit.gameObject && hit.collider.gameObject != targetUnit.gameObject && unit.vision.IsVisible(hit.collider.gameObject))
                    return true;
            }
            return false;
        }

        protected float ActionPointCostModifier_WeaponType(Weapon weapon)
        {
            if (weapon == null) // Unarmed
                return 0.5f;

            switch (weapon.WeaponType)
            {
                case WeaponType.Bow:
                    return 1f;
                case WeaponType.Crossbow:
                    return 0.2f;
                case WeaponType.Throwing:
                    return 0.6f;
                case WeaponType.Dagger:
                    return 0.55f;
                case WeaponType.Sword:
                    return 1f;
                case WeaponType.Axe:
                    return 1.35f;
                case WeaponType.Mace:
                    return 1.3f;
                case WeaponType.WarHammer:
                    return 1.5f;
                case WeaponType.Spear:
                    return 0.85f;
                case WeaponType.Polearm:
                    return 1.25f;
                default:
                    Debug.LogError(weapon.WeaponType.ToString() + " has not been implemented in this method. Fix me!");
                    return 1f;
            }
        }

        public abstract void PlayAttackAnimation();

        public abstract bool IsInAttackRange(Unit targetUnit, GridPosition attackGridPosition, GridPosition targetGridPosition);

        public virtual bool IsInAttackRange(Unit targetUnit)
        {
            if (targetUnit == null)
                return IsInAttackRange(null, unit.GridPosition, targetGridPosition);
            else
                return IsInAttackRange(targetUnit, unit.GridPosition, targetUnit.GridPosition);
        }

        public abstract GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit);

        public abstract float AccuracyModifier();

        public abstract bool IsMeleeAttackAction();

        public abstract bool IsRangedAttackAction();
    }
}
