using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public class MeleeAction : BaseAttackAction
    {
        readonly int baseAPCost = 300;

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);
            SetTargetEnemyUnit();
        }

        public override int ActionPointsCost()
        {
            float cost;
            if (unit.UnitEquipment != null)
            {
                unit.UnitEquipment.GetEquippedWeapons(out Weapon primaryWeapon, out Weapon secondaryWeapon);
                if (primaryWeapon != null)
                {
                    if (secondaryWeapon != null)
                    {
                        cost = baseAPCost / 2f * ActionPointCostModifier_WeaponType(primaryWeapon);
                        cost += baseAPCost / 2f * ActionPointCostModifier_WeaponType(secondaryWeapon);
                    }
                    else
                        cost = baseAPCost * ActionPointCostModifier_WeaponType(primaryWeapon);
                }
                else
                    cost = baseAPCost * ActionPointCostModifier_WeaponType(null);

                if (unit.UnitEquipment.InVersatileStance)
                    cost *= VersatileStanceAction.APCostModifier;
            }
            else
                cost = baseAPCost * ActionPointCostModifier_WeaponType(null);

            // If not facing the target position, add the cost of turning towards that position
            unit.unitActionHandler.turnAction.DetermineTargetTurnDirection(targetGridPosition);
            cost += unit.unitActionHandler.turnAction.ActionPointsCost();
            return Mathf.RoundToInt(cost);
        }

        public override void TakeAction()
        {
            if (unit.unitActionHandler.isAttacking) return;

            if (targetEnemyUnit == null)
                SetTargetEnemyUnit();

            if (targetEnemyUnit == null || targetEnemyUnit.health.IsDead)
            {
                targetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.FinishAction();
                return;
            }

            if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition))
            {
                StartAction();
                unit.StartCoroutine(DoAttack());
            }
            else
                MoveToTargetInstead();
        }

        public void DoOpportunityAttack(Unit targetEnemyUnit)
        {
            // Unit's can't opportunity attack while moving
            if (unit.unitActionHandler.moveAction.isMoving)
                return;

            this.targetEnemyUnit = targetEnemyUnit;
            unit.unitActionHandler.SetIsAttacking(true);
            unit.StartCoroutine(Attack());
        }
        
        void AttackTarget(HeldItem heldWeaponAttackingWith, bool isUsingOffhandWeapon)
        {
            bool attackDodged = targetEnemyUnit.unitActionHandler.TryDodgeAttack(unit, heldWeaponAttackingWith, this, isUsingOffhandWeapon);
            if (attackDodged)
                targetEnemyUnit.unitAnimator.DoDodge(unit, heldWeaponAttackingWith, null);
            else
            {
                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                bool attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit, heldWeaponAttackingWith, isUsingOffhandWeapon);
                unit.unitActionHandler.targetUnits.TryGetValue(targetEnemyUnit, out HeldItem itemBlockedWith);

                if (attackBlocked && itemBlockedWith != null)
                    itemBlockedWith.BlockAttack(unit);
            }

            unit.StartCoroutine(WaitToDamageTargets(heldWeaponAttackingWith));
        }

        protected override IEnumerator DoAttack()
        {
            while (targetEnemyUnit.unitActionHandler.moveAction.isMoving || targetEnemyUnit.unitAnimator.beingKnockedBack)
                yield return null;

            // If the target Unit moved out of range, queue a movement instead
            if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false)
            {
                MoveToTargetInstead();
                yield break;
            }

            StartCoroutine(Attack());

            // Wait until the attack lands before completing the action
            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        IEnumerator Attack()
        {
            // The unit being attacked becomes aware of this unit
            unit.vision.BecomeVisibleUnitOfTarget(targetEnemyUnit, true);

            // We need to skip a frame in case the target Unit's meshes are being enabled
            yield return null;
            
            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (unit.IsPlayer || targetEnemyUnit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen || targetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.turnAction.isRotating)
                    yield return null;
                
                // Play the attack animations and handle blocking for the target
                if (unit.UnitEquipment.IsUnarmed || unit.UnitEquipment.RangedWeaponEquipped)
                {
                    unit.unitAnimator.DoDefaultUnarmedAttack();
                    AttackTarget(null, false);
                }
                else if (unit.UnitEquipment.IsDualWielding)
                {
                    float distanceToTarget = Vector3.Distance(unit.WorldPosition, targetEnemyUnit.WorldPosition);
                    HeldMeleeWeapon primaryHeldWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                    HeldMeleeWeapon secondaryHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    bool primaryWeaponInRange = primaryHeldWeapon.itemData.Item.Weapon.MaxRange >= distanceToTarget && primaryHeldWeapon.itemData.Item.Weapon.MinRange <= distanceToTarget;
                    bool secondaryWeaponInRange = secondaryHeldWeapon.itemData.Item.Weapon.MaxRange >= distanceToTarget && secondaryHeldWeapon.itemData.Item.Weapon.MinRange <= distanceToTarget;

                    if (primaryWeaponInRange && secondaryWeaponInRange) // Dual wield attack
                    {
                        unit.unitAnimator.StartDualMeleeAttack();
                        primaryHeldWeapon.DoDefaultAttack(targetGridPosition);
                        AttackTarget(primaryHeldWeapon, false);

                        yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(primaryHeldWeapon.itemData.Item as Weapon) / 2f) + 0.05f);

                        secondaryHeldWeapon.DoDefaultAttack(targetGridPosition);
                        AttackTarget(secondaryHeldWeapon, true);
                    }
                    else
                    {
                        unit.unitAnimator.StartMeleeAttack();

                        if (primaryWeaponInRange) // Primary weapon only attack
                        {
                            primaryHeldWeapon.DoDefaultAttack(targetGridPosition);
                            AttackTarget(primaryHeldWeapon, false);
                        }
                        else // Secondary weapon only attack
                        {
                            secondaryHeldWeapon.DoDefaultAttack(targetGridPosition);
                            AttackTarget(secondaryHeldWeapon, false);
                        }
                    }
                }
                else
                {
                    // Primary weapon attack
                    unit.unitAnimator.StartMeleeAttack();
                    if (unit.UnitEquipment.MeleeWeaponEquipped)
                    {
                        unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().DoDefaultAttack(targetGridPosition);
                        AttackTarget(unit.unitMeshManager.GetPrimaryHeldMeleeWeapon(), false);
                    }
                    else // Fallback to unarmed attack
                    {
                        unit.unitAnimator.DoDefaultUnarmedAttack();
                        AttackTarget(null, false);
                    }
                }
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Try to dodge or block the attack
                bool attackDodged = false;
                bool attackBlocked = false;
                bool headShot = false;
                if (unit.UnitEquipment.IsUnarmed || unit.UnitEquipment.RangedWeaponEquipped) // Unarmed or has a ranged weapon equipped, but no ammo
                {
                    attackDodged = targetEnemyUnit.unitActionHandler.TryDodgeAttack(unit, null, this, false);
                    if (attackDodged == false)
                    {
                        attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit, null, false);
                        DamageTargets(null, headShot);
                    }
                }
                else if (unit.UnitEquipment.IsDualWielding) // Dual wield attack
                {
                    float distanceToTarget = Vector3.Distance(unit.WorldPosition, targetEnemyUnit.WorldPosition);
                    HeldMeleeWeapon primaryHeldWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                    HeldMeleeWeapon secondaryHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    bool primaryWeaponInRange = primaryHeldWeapon.itemData.Item.Weapon.MaxRange >= distanceToTarget && primaryHeldWeapon.itemData.Item.Weapon.MinRange <= distanceToTarget;
                    bool secondaryWeaponInRange = secondaryHeldWeapon.itemData.Item.Weapon.MaxRange >= distanceToTarget && secondaryHeldWeapon.itemData.Item.Weapon.MinRange <= distanceToTarget;
                    
                    bool mainAttackDodged = false;
                    bool mainAttackBlocked = false;
                    if (primaryWeaponInRange)
                    {
                        mainAttackDodged = targetEnemyUnit.unitActionHandler.TryDodgeAttack(unit, unit.unitMeshManager.rightHeldItem, this, false);
                        if (mainAttackDodged == false)
                        {
                            mainAttackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit, unit.unitMeshManager.rightHeldItem, false);
                            DamageTargets(unit.unitMeshManager.rightHeldItem as HeldMeleeWeapon, headShot);
                        }
                    }

                    bool offhandAttackDodged = false;
                    bool offhandAttackBlocked = false;
                    if (secondaryWeaponInRange)
                    {
                        offhandAttackDodged = targetEnemyUnit.unitActionHandler.TryDodgeAttack(unit, unit.unitMeshManager.leftHeldItem, this, true);
                        if (offhandAttackDodged == false)
                        {
                            offhandAttackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit, unit.unitMeshManager.leftHeldItem, true);
                            DamageTargets(unit.unitMeshManager.leftHeldItem as HeldMeleeWeapon, headShot);
                        }
                    }

                    if (mainAttackDodged || offhandAttackDodged)
                        attackDodged = true;

                    if (mainAttackBlocked || offhandAttackBlocked)
                        attackBlocked = true;
                }
                else
                {
                    HeldMeleeWeapon primaryMeleeWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                    attackDodged = targetEnemyUnit.unitActionHandler.TryDodgeAttack(unit, primaryMeleeWeapon, this, false);
                    if (attackDodged == false)
                    {
                        attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit, primaryMeleeWeapon, false);
                        if (primaryMeleeWeapon != null)
                            DamageTargets(primaryMeleeWeapon, headShot); // Right hand weapon attack
                        else
                            DamageTargets(null, headShot); // Fallback to unarmed damage
                    }
                }

                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // If the attack was dodged or blocked and the defending unit isn't facing their attacker, turn to face the attacker
                if (attackDodged || attackBlocked)
                    targetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                unit.unitActionHandler.SetIsAttacking(false);
            }
        }

        public override void PlayAttackAnimation() { }

        public override void CompleteAction()
        {
            base.CompleteAction();
            
            if (unit.IsPlayer)
            {
                unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                if (PlayerInput.Instance.autoAttack == false)
                {
                    targetEnemyUnit = null;
                    unit.unitActionHandler.SetTargetEnemyUnit(null);
                }
            }

            unit.unitActionHandler.FinishAction();
        }

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized * 100f);
                float distance = Vector3.Distance(unit.WorldPosition, targetUnit.WorldPosition);
                float minAttackRange = 1f;
                if (unit.UnitEquipment.IsDualWielding)
                    minAttackRange = Mathf.Min(unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weapon.MinRange, unit.unitMeshManager.GetLeftHeldMeleeWeapon().itemData.Item.Weapon.MinRange);
                else if (unit.UnitEquipment.MeleeWeaponEquipped)
                    minAttackRange = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weapon.MinRange;

                if (distance < minAttackRange)
                    finalActionValue = 0f;
                else
                    finalActionValue -= distance * 10f;

                return new NPCAIAction
                {
                    baseAction = this,
                    actionGridPosition = targetUnit.GridPosition,
                    actionValue = Mathf.RoundToInt(finalActionValue)
                };
            }

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = unit.GridPosition,
                actionValue = -1
            };
        }

        public override NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
        {
            float finalActionValue = 0f;

            // Make sure there's a Unit at this grid position
            if (LevelGrid.HasUnitAtGridPosition(actionGridPosition, out Unit unitAtGridPosition))
            {
                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                if (unitAtGridPosition.health.IsDead == false && unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized * 70f);

                    // Favor the targetEnemyUnit
                    if (unit.unitActionHandler.targetEnemyUnit != null && unitAtGridPosition == unit.unitActionHandler.targetEnemyUnit)
                        finalActionValue += 15f;
                }
                else
                    finalActionValue = -1f;

                return new NPCAIAction
                {
                    baseAction = this,
                    actionGridPosition = actionGridPosition,
                    actionValue = Mathf.RoundToInt(finalActionValue)
                };
            }

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = unit.GridPosition,
                actionValue = -1
            };
        }

        /*public override GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            nearestGridPositionsList.Clear();
            List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
            gridPositions = GetValidGridPositions_InRangeOfTarget(targetUnit);
            float nearestDistance = 10000f;

            // First, find the nearest valid Grid Positions to the Player
            for (int i = 0; i < gridPositions.Count; i++)
            {
                float distance = Vector3.Distance(gridPositions[i].WorldPosition, startGridPosition.WorldPosition);
                if (distance < nearestDistance)
                {
                    nearestGridPositionsList.Clear();
                    nearestGridPositionsList.Add(gridPositions[i]);
                    nearestDistance = distance;
                }
                else if (Mathf.Approximately(distance, nearestDistance))
                    nearestGridPositionsList.Add(gridPositions[i]);
            }

            GridPosition nearestGridPosition = startGridPosition;
            float nearestDistanceToTarget = 10000f;
            for (int i = 0; i < nearestGridPositionsList.Count; i++)
            {
                // Get the Grid Position that is closest to the target Grid Position
                float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition, targetUnit.transform.position);
                if (distance < nearestDistanceToTarget)
                {
                    nearestDistanceToTarget = distance;
                    nearestGridPosition = nearestGridPositionsList[i];
                }
            }

            ListPool<GridPosition>.Release(gridPositions);
            return nearestGridPosition;
        }*/

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && !unitAtGridPosition.health.IsDead && !unit.alliance.IsAlly(unitAtGridPosition) && unit.vision.IsDirectlyVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (unit != null && (unit.UnitEquipment.MeleeWeaponEquipped || unit.stats.CanFightUnarmed))
                return true;
            return false;
        }

        public override string TooltipDescription()
        {
            if (unit.UnitEquipment.IsUnarmed || unit.UnitEquipment.RangedWeaponEquipped)
                return "Engage in <b>hand-to-hand</b> combat, delivering a swift and powerful strike to your target.";
            else if (unit.UnitEquipment.IsDualWielding)
                return $"Deliver coordinated strikes with your <b>{unit.unitMeshManager.rightHeldItem.itemData.Item.Name}</b> and <b>{unit.unitMeshManager.leftHeldItem.itemData.Item.Name}</b>.";
            else
                return $"Deliver a decisive strike to your target using your <b>{unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Name}</b>.";
        }

        public override float AccuracyModifier() => 1f;

        public override int InitialEnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Special;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanAttackThroughUnits() => false;

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;

        public override float MinAttackRange()
        {
            if (unit.UnitEquipment == null || unit.UnitEquipment.IsUnarmed || unit.UnitEquipment.RangedWeaponEquipped)
                return 1f;
            else
            {
                HeldMeleeWeapon primaryHeldWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (unit.UnitEquipment.IsDualWielding)
                {
                    HeldMeleeWeapon secondaryHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    return Mathf.Min(primaryHeldWeapon.itemData.Item.Weapon.MinRange, secondaryHeldWeapon.itemData.Item.Weapon.MinRange);
                }
                else if (primaryHeldWeapon != null)
                    return primaryHeldWeapon.itemData.Item.Weapon.MinRange;
                else
                    return 1f;
            }
        }

        public override float MaxAttackRange()
        {
            if (unit.UnitEquipment == null || unit.UnitEquipment.IsUnarmed || unit.UnitEquipment.RangedWeaponEquipped)
                return unit.stats.UnarmedAttackRange;
            else
            {
                HeldMeleeWeapon primaryHeldWeapon = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (unit.UnitEquipment.IsDualWielding)
                {
                    HeldMeleeWeapon secondaryHeldWeapon = unit.unitMeshManager.GetLeftHeldMeleeWeapon();
                    return Mathf.Max(primaryHeldWeapon.itemData.Item.Weapon.MaxRange, secondaryHeldWeapon.itemData.Item.Weapon.MaxRange);
                }
                else if (primaryHeldWeapon != null)
                    return primaryHeldWeapon.itemData.Item.Weapon.MaxRange;
                else
                    return unit.stats.UnarmedAttackRange;
            }
        }
    }
}
