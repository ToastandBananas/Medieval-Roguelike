using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem;
using Utilities;

namespace ActionSystem
{
    public class MeleeAction : BaseAttackAction
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);
            SetTargetEnemyUnit();
        }

        public override void TakeAction()
        {
            if (unit == null || unit.unitActionHandler.AvailableActions.Contains(this) == false || unit.unitActionHandler.isAttacking) return;

            if (targetEnemyUnit == null)
                SetTargetEnemyUnit();

            if (targetEnemyUnit == null || targetEnemyUnit.health.IsDead())
            {
                targetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.FinishAction();
                return;
            }

            StartAction();

            if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetGridPosition))
                unit.StartCoroutine(DoAttack());
            else
                MoveToTargetInstead();
        }

        public void DoOpportunityAttack(Unit targetEnemyUnit)
        {
            this.targetEnemyUnit = targetEnemyUnit;
            unit.unitActionHandler.SetIsAttacking(true);
            unit.StartCoroutine(Attack());
        }
        
        void AttackTarget(HeldItem heldWeaponAttackingWith)
        {
            // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
            bool attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit);
            unit.unitActionHandler.targetUnits.TryGetValue(targetEnemyUnit, out HeldItem itemBlockedWith);

            if (attackBlocked)
                itemBlockedWith.BlockAttack(unit);

            // TODO: Come up with a method determining headshots
            bool headShot = false;

            unit.StartCoroutine(WaitToDamageTargets(heldWeaponAttackingWith, headShot));
        }

        IEnumerator Attack()
        {
            // The unit being attacked becomes aware of this unit
            BecomeVisibleEnemyOfTarget(targetEnemyUnit);

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen())
            {
                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.isRotating)
                    yield return null;

                // Play the attack animations and handle blocking for the target
                if (unit.UnitEquipment.IsUnarmed())
                {
                    unit.unitAnimator.DoDefaultUnarmedAttack();
                    AttackTarget(null);
                }
                else if (unit.UnitEquipment.IsDualWielding())
                {
                    // Dual wield attack
                    unit.unitAnimator.StartDualMeleeAttack();
                    unit.unitMeshManager.rightHeldItem.DoDefaultAttack(targetGridPosition);
                    AttackTarget(unit.unitMeshManager.rightHeldItem);

                    yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(unit.unitMeshManager.rightHeldItem.itemData.Item as Weapon) / 2f) + 0.05f);

                    unit.unitMeshManager.leftHeldItem.DoDefaultAttack(targetGridPosition);
                    AttackTarget(unit.unitMeshManager.leftHeldItem);
                }
                else
                {
                    // Primary weapon attack
                    unit.unitAnimator.StartMeleeAttack();
                    if (unit.unitMeshManager.GetPrimaryMeleeWeapon() != null)
                    {
                        unit.unitMeshManager.GetPrimaryMeleeWeapon().DoDefaultAttack(targetGridPosition);
                        AttackTarget(unit.unitMeshManager.GetPrimaryMeleeWeapon());
                    }
                    else if (unit.stats.CanFightUnarmed) // Fallback to unarmed attack
                    {
                        unit.unitAnimator.DoDefaultUnarmedAttack();
                        AttackTarget(null);
                    }
                }
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Try to block the attack
                bool attackBlocked = false;
                bool headShot = false;
                if (unit.UnitEquipment.IsUnarmed())
                {
                    attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit);
                    DamageTargets(null, headShot);
                }
                else if (unit.UnitEquipment.IsDualWielding()) // Dual wield attack
                {
                    bool mainAttackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit);
                    DamageTargets(unit.unitMeshManager.rightHeldItem as HeldMeleeWeapon, headShot);

                    bool offhandAttackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit);
                    DamageTargets(unit.unitMeshManager.leftHeldItem as HeldMeleeWeapon, headShot);

                    if (mainAttackBlocked || offhandAttackBlocked)
                        attackBlocked = true;
                }
                else
                {
                    attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockMeleeAttack(unit);
                    if (unit.unitMeshManager.GetPrimaryMeleeWeapon() != null)
                        DamageTargets(unit.unitMeshManager.GetPrimaryMeleeWeapon(), headShot); // Right hand weapon attack
                    else
                        DamageTargets(null, headShot); // Fallback to unarmed damage
                }

                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    targetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                unit.unitActionHandler.SetIsAttacking(false);
            }
        }

        protected override IEnumerator DoAttack()
        {
            if (targetEnemyUnit != null && targetEnemyUnit.unitActionHandler.isMoving)
            {
                while (targetEnemyUnit.unitActionHandler.isMoving)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetGridPosition) == false)
                {
                    MoveToTargetInstead();
                    yield break;
                }
            }

            StartCoroutine(Attack());

            // Wait until the attack lands before completing the action
            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompletAction early within MoveToTargetInstead
        }

        public override void PlayAttackAnimation() { }

        public override bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            if (targetUnit != null && unit.vision.IsInLineOfSight_SphereCast(targetUnit) == false)
                return false;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, targetGridPosition);
            if (unit.UnitEquipment.MeleeWeaponEquipped())
            {
                Weapon meleeWeapon = unit.unitMeshManager.GetPrimaryMeleeWeapon().itemData.Item.Weapon;
                float maxRangeToTargetPosition = meleeWeapon.MaxRange - Mathf.Abs(targetGridPosition.y - startGridPosition.y);
                if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;
                
                if (distance > maxRangeToTargetPosition || distance < meleeWeapon.MinRange)
                    return false;
            }
            else
            {
                float maxRangeToTargetPosition = unit.stats.UnarmedAttackRange - Mathf.Abs(targetGridPosition.y - startGridPosition.y);
                if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;

                if (distance > maxRangeToTargetPosition || distance < 1f)
                    return false;
            }

            return true;
        }

        protected override void StartAction()
        {
            base.StartAction();
            unit.unitActionHandler.SetIsAttacking(true);
        }

        public override void CompleteAction()
        {
            base.CompleteAction();
            
            if (unit.IsPlayer)
            {
                unit.unitActionHandler.SetDefaultSelectedAction();
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
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, targetUnit.GridPosition);
                float minAttackRange = 1f;
                if (unit.UnitEquipment.MeleeWeaponEquipped())
                    minAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().itemData.Item.Weapon.MinRange;

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
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(actionGridPosition))
            {
                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(actionGridPosition);

                if (unit.health.IsDead() == false && unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized() * 70f);

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

        public override int GetActionPointsCost()
        {
            int cost = 300;

            // If not facing the target position, add the cost of turning towards that position
            unit.unitActionHandler.turnAction.DetermineTargetTurnDirection(targetGridPosition);
            cost += unit.unitActionHandler.turnAction.GetActionPointsCost();
            return cost;
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            float minRange;
            float maxRange;

            if (unit.UnitEquipment.IsUnarmed())
            {
                minRange = 1f;
                maxRange = UnarmedAttackRange(startGridPosition, false);
            }
            else
            {
                HeldMeleeWeapon primaryHeldMeleeWeapon = unit.unitMeshManager.GetPrimaryMeleeWeapon();
                if (primaryHeldMeleeWeapon != null)
                {
                    minRange = primaryHeldMeleeWeapon.itemData.Item.Weapon.MinRange;
                    maxRange = primaryHeldMeleeWeapon.itemData.Item.Weapon.MaxRange;
                }
                else
                {
                    minRange = 1f;
                    maxRange = UnarmedAttackRange(startGridPosition, false);
                }
            }

            float boundsDimension = (maxRange * 2) + 0.1f;

            validGridPositionsList.Clear();
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                float maxRangeToNodePosition = maxRange - Mathf.Abs(nodeGridPosition.y - startGridPosition.y);
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, nodeGridPosition);
                if (distance > maxRangeToNodePosition || distance < minRange)
                    continue;

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                Vector3 offset = Vector3.up * unit.ShoulderHeight * 2f;
                Vector3 shootDir = ((nodeGridPosition.WorldPosition() + offset) - (startGridPosition.WorldPosition() + offset)).normalized;
                if (Physics.SphereCast(startGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition + offset, nodeGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask))
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public override List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
        {
            validGridPositionsList.Clear();
            if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
                return validGridPositionsList;

            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition) == false)
                return validGridPositionsList;

            float sphereCastRadius = 0.1f;
            Vector3 offset = Vector3.up * unit.ShoulderHeight * 2f;
            Vector3 shootDir = ((unit.WorldPosition + offset) - (targetGridPosition.WorldPosition() + offset)).normalized;
            if (Physics.SphereCast(targetGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition + offset, targetGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask))
                return validGridPositionsList; // Blocked by an obstacle

            validGridPositionsList.Add(targetGridPosition);
            return validGridPositionsList;
        }

        public List<GridPosition> GetValidGridPositions_InRangeOfTarget(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float maxAttackRange;
            if (unit.UnitEquipment.MeleeWeaponEquipped())
                maxAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().itemData.Item.Weapon.MaxRange;
            else
                maxAttackRange = unit.stats.UnarmedAttackRange;

            float boundsDimension = (maxAttackRange * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.GridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                // If Grid Position has a Unit there already
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition))
                    continue;

                // If target is out of attack range from this Grid Position
                if (IsInAttackRange(null, nodeGridPosition, targetUnit.GridPosition) == false)
                    continue;

                float sphereCastRadius = 0.1f;
                Vector3 attackDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight * 2f)) - (targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f))).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight * 2f), targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f)), unit.unitActionHandler.AttackObstacleMask))
                    continue; // Blocked by an obstacle

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public override GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            nearestGridPositionsList.Clear();
            List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
            gridPositions = GetValidGridPositions_InRangeOfTarget(targetUnit);
            float nearestDistance = 10000f;

            // First, find the nearest valid Grid Positions to the Player
            for (int i = 0; i < gridPositions.Count; i++)
            {
                float distance = Vector3.Distance(gridPositions[i].WorldPosition(), startGridPosition.WorldPosition());
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
                float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition(), targetUnit.transform.position);
                if (distance < nearestDistanceToTarget)
                {
                    nearestDistanceToTarget = distance;
                    nearestGridPosition = nearestGridPositionsList[i];
                }
            }

            ListPool<GridPosition>.Release(gridPositions);
            return nearestGridPosition;
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && unit.alliance.IsAlly(unitAtGridPosition) == false && unit.vision.IsVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (unit != null && (unit.UnitEquipment.MeleeWeaponEquipped() || unit.stats.CanFightUnarmed))
                return true;
            return false;
        }

        public float UnarmedAttackRange(GridPosition enemyGridPosition, bool accountForHeight)
        {
            if (accountForHeight == false)
                return unit.stats.UnarmedAttackRange;

            float maxRange = unit.stats.UnarmedAttackRange - Mathf.Abs(enemyGridPosition.y - unit.GridPosition.y);
            if (maxRange < 0f) maxRange = 0f;
            return maxRange;
        }

        public override int GetEnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool IsHotbarAction() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;
    }
}
