using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public class ShootAction : BaseAttackAction
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

        readonly int baseAPCost = 300;

        public override void QueueAction()
        {
            if (RangedWeaponIsLoaded() == false)
            {
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
                return;
            }

            base.QueueAction();
        }

        public override void QueueAction(GridPosition targetGridPosition)
        {
            if (RangedWeaponIsLoaded() == false)
            {
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
                return;
            }

            base.QueueAction(targetGridPosition);
        }

        public override void QueueAction(Unit targetEnemyUnit)
        {
            if (RangedWeaponIsLoaded() == false)
            {
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
                return;
            }

            base.QueueAction(targetEnemyUnit);
        }

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);
            SetTargetEnemyUnit();
        }

        public override void TakeAction()
        {
            if (unit.unitActionHandler.isAttacking) return;

            if (targetEnemyUnit == null)
            {
                if (unit.unitActionHandler.targetEnemyUnit != null)
                    targetEnemyUnit = unit.unitActionHandler.targetEnemyUnit;
                else if (LevelGrid.HasAnyUnitOnGridPosition(targetGridPosition, out Unit targetUnit))
                    targetEnemyUnit = targetUnit;
            }

            if (targetEnemyUnit == null || targetEnemyUnit.health.IsDead())
            {
                targetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }

            if (RangedWeaponIsLoaded() == false)
            {
                CompleteAction();
                unit.unitActionHandler.SetTargetEnemyUnit(targetEnemyUnit);

                // ReloadAction can become null if the Unit drops their weapon or switches their loadout
                ReloadAction reloadAction = unit.unitActionHandler.GetAction<ReloadAction>();
                if (reloadAction != null)
                    reloadAction.QueueAction();
                else
                    TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }
            else if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition))
            {
                StartAction();
                unit.StartCoroutine(DoAttack());
            }
            else
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                return;
            }
        }

        protected override IEnumerator DoAttack()
        {
            while (targetEnemyUnit.unitActionHandler.moveAction.isMoving)
                yield return null;

            // If the target Unit moved out of range, queue a movement instead
            if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false)
            {
                MoveToTargetInstead();
                yield break;
            }

            // The unit being attacked becomes aware of this unit
            unit.vision.BecomeVisibleUnitOfTarget(targetEnemyUnit, true);

            // We need to skip a frame in case the target Unit's meshes are being enabled
            yield return null;

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            HeldRangedWeapon heldRangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon();
            if (unit.IsPlayer || targetEnemyUnit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen || targetEnemyUnit.unitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.turnAction.isRotating)
                    yield return null;
                
                // If the target Unit moved out of range, queue a movement instead
                if (IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetGridPosition) == false)
                {
                    MoveToTargetInstead();
                    yield break;
                }

                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                if (targetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, heldRangedWeapon.itemData, false))
                {
                    // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
                    targetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, false);
                    unit.unitActionHandler.targetUnits.TryGetValue(targetEnemyUnit, out HeldItem itemBlockedWith);
                    if (itemBlockedWith != null)
                        itemBlockedWith.BlockAttack(unit);
                }

                // Rotate towards the target and do the shoot animation
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                bool missedTarget = TryHitTarget(targetEnemyUnit.GridPosition);
                bool attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit, heldRangedWeapon.itemData, false);
                bool headShot = false;
                if (missedTarget == false)
                    DamageTargets(heldRangedWeapon, headShot);

                // Rotate towards the target
                if (unit.unitActionHandler.turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    unit.unitActionHandler.turnAction.RotateTowards_Unit(targetEnemyUnit, true);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    targetEnemyUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, true);

                heldRangedWeapon.RemoveProjectile();
                unit.unitActionHandler.SetIsAttacking(false);
            }

            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        public override void PlayAttackAnimation()
        {
            unit.unitActionHandler.turnAction.RotateTowardsAttackPosition(targetEnemyUnit.WorldPosition);
            unit.unitMeshManager.GetHeldRangedWeapon().DoDefaultAttack(targetGridPosition);
        }

        public bool TryHitTarget(GridPosition targetGridPosition)
        {
            float random = Random.Range(0f, 1f);
            float rangedAccuracy = unit.stats.RangedAccuracy(unit.unitMeshManager.GetHeldRangedWeapon().itemData, targetGridPosition, this);
            if (random <= rangedAccuracy)
                return true;
            return false;
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the projectile hits something, rather than in this method as is with non-ranged actions
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

        public override bool IsInAttackRange(Unit targetUnit, GridPosition shootGridPosition, GridPosition targetGridPosition)
        {
            if (unit.unitMeshManager.GetHeldRangedWeapon() == null)
                return false;

            if (targetUnit != null && unit.vision.IsInLineOfSight_SphereCast(targetUnit) == false)
                return false;

            float distance;
            if (targetUnit != null)
                distance = TacticsUtilities.CalculateDistance_XZ(shootGridPosition, targetUnit.GridPosition);
            else
                distance = TacticsUtilities.CalculateDistance_XZ(shootGridPosition, targetGridPosition);

            Weapon rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon;
            float maxRangeToTargetPosition = rangedWeapon.MaxRange + (shootGridPosition.y - targetGridPosition.y); // Take into account grid position y differences
            if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;

            if (unit != null && unit.IsPlayer && targetUnit != null)
                Debug.Log("Distance to " + targetUnit + ": " + distance);
            if (distance > maxRangeToTargetPosition || distance < rangedWeapon.MinRange)
                return false;
            return true;
        }

        protected override void StartAction()
        {
            base.StartAction();
            unit.unitActionHandler.SetIsAttacking(true);
        }

        public override int GetActionPointsCost()
        {
            float cost = baseAPCost * ActionPointCostModifier_WeaponType(unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon);

            // If not facing the target position, add the cost of turning towards that position
            unit.unitActionHandler.turnAction.DetermineTargetTurnDirection(targetGridPosition);
            cost += unit.unitActionHandler.turnAction.GetActionPointsCost();
            return Mathf.RoundToInt(cost);
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            float minRange = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange;
            float maxRange = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MaxRange;
            float boundsDimension = ((startGridPosition.y + maxRange) * 2) + 0.1f;

            validGridPositionsList.Clear();
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                float maxRangeToNodePosition = maxRange + (startGridPosition.y - nodeGridPosition.y);
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

                float distance = TacticsUtilities.CalculateDistance_XZ(startGridPosition, nodeGridPosition);
                if (distance > maxRangeToNodePosition || distance < minRange)
                    continue;

                float sphereCastRadius = 0.1f;
                Vector3 offset = 2f * unit.ShoulderHeight * Vector3.up;
                Vector3 shootDir = (nodeGridPosition.WorldPosition + offset - (startGridPosition.WorldPosition + offset)).normalized;
                if (Physics.SphereCast(startGridPosition.WorldPosition + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition + offset, nodeGridPosition.WorldPosition + offset), unit.unitActionHandler.AttackObstacleMask))
                    continue;

                // Debug.Log(gridPosition);
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
            Vector3 offset = 2f * unit.ShoulderHeight * Vector3.up;
            Vector3 shootDir = (unit.WorldPosition + offset - (targetGridPosition.WorldPosition + offset)).normalized;
            if (Physics.SphereCast(targetGridPosition.WorldPosition + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition + offset, targetGridPosition.WorldPosition + offset), unit.unitActionHandler.AttackObstacleMask))
                return validGridPositionsList; // Blocked by an obstacle

            validGridPositionsList.Add(targetGridPosition);
            return validGridPositionsList;
        }

        public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float maxAttackRange = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MaxRange;
            float boundsDimension = ((targetUnit.GridPosition.y + maxAttackRange) * 2) + 0.1f;

            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.transform.position, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                // Grid Position has a Unit there already
                if (LevelGrid.HasAnyUnitOnGridPosition(nodeGridPosition, out _))
                    continue;

                // If target is out of attack range
                if (IsInAttackRange(null, nodeGridPosition, targetUnit.GridPosition) == false)
                    continue;

                float sphereCastRadius = 0.1f;
                Vector3 unitOffset = 2f * unit.ShoulderHeight * Vector3.up;
                Vector3 targetUnitOffset = 2f * targetUnit.ShoulderHeight * Vector3.up;
                Vector3 shootDir = (nodeGridPosition.WorldPosition + unitOffset - (targetUnit.WorldPosition + targetUnitOffset)).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + targetUnitOffset, sphereCastRadius, shootDir, out _, Vector3.Distance(nodeGridPosition.WorldPosition + unitOffset, targetUnit.WorldPosition + targetUnitOffset), unit.unitActionHandler.AttackObstacleMask))
                    continue; // Blocked by an obstacle

                // Debug.Log(gridPosition);
                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public override GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            nearestGridPositionsList.Clear();
            List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
            gridPositions = GetValidGridPositionsInRange(targetUnit);
            float nearestDistance = 10000000f;

            // First find the nearest valid Grid Positions to the Player
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
            float nearestDistanceToTarget = 10000000f;
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
        }

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsUtilities.CalculateDistance_XYZ(unit.GridPosition, targetUnit.GridPosition);
                if (distance < unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange)
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
            if (LevelGrid.HasAnyUnitOnGridPosition(actionGridPosition, out Unit unitAtGridPosition))
            {
                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                if (unitAtGridPosition.health.IsDead() == false && unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized() * 70f);

                    // Favor the targetEnemyUnit
                    if (unit.unitActionHandler.targetEnemyUnit != null && unitAtGridPosition == unit.unitActionHandler.targetEnemyUnit)
                        finalActionValue += 15f;

                    float distance = TacticsUtilities.CalculateDistance_XYZ(unit.GridPosition, actionGridPosition);
                    if (distance < unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange)
                        finalActionValue = -1f;
                    else
                        finalActionValue -= distance * 1.5f;
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

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && unit.alliance.IsAlly(unitAtGridPosition) == false && unit.vision.IsDirectlyVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (unit != null && unit.UnitEquipment.RangedWeaponEquipped && unit.UnitEquipment.HasValidAmmunitionEquipped())
                return true;
            return false;
        }

        public override string TooltipDescription()
        {
            RangedWeapon rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon;
            if (rangedWeapon.WeaponType == WeaponType.Bow)
                return $"Draw your <b>{rangedWeapon.Name}'s</b> bowstring taut and release a deadly arrow towards your target.";
            else if (rangedWeapon.WeaponType == WeaponType.Crossbow)
                return $"Ready your <b>{rangedWeapon.Name}</b> and release a deadly bolt towards your target.";
            return "";
        }

        public bool RangedWeaponIsLoaded() => unit.UnitEquipment.RangedWeaponEquipped && unit.unitMeshManager.GetHeldRangedWeapon().isLoaded;

        public override float AccuracyModifier() => 1f;

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Special;

        public override bool IsInterruptable() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool ActionIsUsedInstantly() => false;

        public override bool IsMeleeAttackAction() => false;

        public override bool IsRangedAttackAction() => true;
    }
}
