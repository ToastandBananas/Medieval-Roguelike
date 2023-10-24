using Pathfinding;
using Pathfinding.Util;
using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using UnitSystem;
using Utilities;

namespace ActionSystem
{
    public class SwipeAction : BaseAttackAction
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

        public override void TakeAction()
        {
            if (unit == null || unit.unitActionHandler.AvailableActions.Contains(this) == false || unit.unitActionHandler.isAttacking) return;

            if (IsValidUnitInActionArea(targetGridPosition) == false || unit.stats.HasEnoughEnergy(GetEnergyCost()) == false)
            {
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.ClearQueuedAttack();
                unit.unitActionHandler.FinishAction();
                return;
            }

            StartAction();

            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition))
                unit.StartCoroutine(DoAttack());
            else
            {
                CompleteAction();
                if (unit.IsPlayer)
                    unit.unitActionHandler.TakeTurn();
                return;
            }
        }

        public override void PlayAttackAnimation()
        {
            unit.unitAnimator.StartMeleeAttack();
            unit.unitMeshManager.GetPrimaryMeleeWeapon().DoSwipeAttack(targetGridPosition);
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition) => unit.unitActionHandler.GetAction<MeleeAction>().GetActionGridPositionsInRange(startGridPosition);

        public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float maxAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().itemData.Item.Weapon.MaxRange;

            float boundsDimension = (maxAttackRange * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.GridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                // If Grid Position has a Unit there already
                if (LevelGrid.HasAnyUnitOnGridPosition(nodeGridPosition))
                    continue;

                // If target is out of attack range from this Grid Position
                if (IsInAttackRange(null, nodeGridPosition, targetUnit.GridPosition) == false)
                    continue;

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                Vector3 attackDir = ((nodeGridPosition.WorldPosition + (Vector3.up * unit.ShoulderHeight * 2f)) - (targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f))).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition + (Vector3.up * unit.ShoulderHeight * 2f), targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f)), unit.unitActionHandler.AttackObstacleMask))
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public override List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
        {
            validGridPositionsList.Clear();

            // Get the closest cardinal or intermediate direction
            Vector3 directionToTarget = (targetGridPosition.WorldPosition - unit.WorldPosition).normalized;
            Vector3 generalDirection = GetGeneralDirection(directionToTarget);

            if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
                return validGridPositionsList;

            // Exclude attacker's position
            if (targetGridPosition == unit.GridPosition)
                return validGridPositionsList;

            // Check if the target position is within the max attack range
            //if (Vector3.Distance(unit.WorldPosition, targetGridPosition.WorldPosition) > maxAttackRange)
            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition) == false)
                return validGridPositionsList;

            // Check for obstacles
            float sphereCastRadius = 0.1f;
            Vector3 heightOffset = Vector3.up * unit.ShoulderHeight * 2f;
            if (Physics.SphereCast(unit.WorldPosition + heightOffset, sphereCastRadius, directionToTarget, out RaycastHit hit, Vector3.Distance(targetGridPosition.WorldPosition + heightOffset, unit.WorldPosition + heightOffset), unit.unitActionHandler.AttackObstacleMask))
            {
                // Debug.Log(targetGridPosition.WorldPosition + " (the target position) is blocked by " + hit.collider.name);
                return validGridPositionsList;
            }

            float maxAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().itemData.Item.Weapon.MaxRange;
            float boundsDimension = (maxAttackRange * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            // Validate the potential attack positions based on the requirements
            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                // Exclude attacker's position
                if (nodeGridPosition == unit.GridPosition)
                    continue;

                // Check if the node is in the general direction of the attack
                Vector3 yDifference = Vector3.up * (nodeGridPosition.WorldPosition.y - unit.WorldPosition.y);
                Vector3 directionToNode = (nodeGridPosition.WorldPosition - (unit.WorldPosition + yDifference)).normalized;
                float angleBetweenDirections = Vector3.Angle(generalDirection, directionToNode);
                if (angleBetweenDirections > 45f)
                    continue;

                // Check if the node is within the max attack range
                //if (Vector3.Distance(unit.WorldPosition, nodeGridPosition.WorldPosition) > maxAttackRange)
                if (IsInAttackRange(null, unit.GridPosition, nodeGridPosition) == false)
                    continue;

                // Make sure the node isn't too much lower or higher than the target grid position (this is a swipe attack, so think of it basically swiping across in a horizontal line)
                if (Mathf.Abs(nodeGridPosition.y - targetGridPosition.y) > 0.5f)
                    continue;

                // Check for obstacles
                Vector3 directionToAttackPosition = (nodeGridPosition.WorldPosition + heightOffset - (unit.WorldPosition + heightOffset)).normalized;
                if (Physics.SphereCast(unit.WorldPosition + heightOffset, sphereCastRadius, directionToAttackPosition, out hit, Vector3.Distance(nodeGridPosition.WorldPosition + heightOffset, unit.WorldPosition + heightOffset), unit.unitActionHandler.AttackObstacleMask))
                {
                    // Debug.Log(nodeGridPosition.WorldPosition + " is blocked by " + hit.collider.name);
                    continue;
                }
                
                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        Vector3 GetGeneralDirection(Vector3 directionToTarget)
        {
            // Add the cardinal directions, each represented by a Vector3, to an array
            Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

            float maxDot = float.MinValue;
            Vector3 generalDirection = Vector3.zero;

            foreach (Vector3 dir in directions)
            {
                float dot = Vector3.Dot(directionToTarget, dir);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    generalDirection = dir;
                }
            }

            return generalDirection;
        }

        public override GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            nearestGridPositionsList.Clear();
            List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
            gridPositions = GetValidGridPositionsInRange(targetUnit);
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
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            List<GridPosition> attackGridPositions = ListPool<GridPosition>.Claim();
            attackGridPositions = GetActionAreaGridPositions(targetGridPosition);
            for (int i = 0; i < attackGridPositions.Count; i++)
            {
                if (LevelGrid.HasAnyUnitOnGridPosition(attackGridPositions[i]) == false)
                    continue;

                Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(attackGridPositions[i]);
                if (unitAtGridPosition.health.IsDead())
                    continue;

                if (unit.alliance.IsAlly(unitAtGridPosition))
                    continue;

                if (unit.vision.IsVisible(unitAtGridPosition) == false)
                    continue;

                // If the loop makes it to this point, then it found a valid unit
                ListPool<GridPosition>.Release(attackGridPositions);
                return true;
            }
            
            ListPool<GridPosition>.Release(attackGridPositions);
            return false;
        }

        public override bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition) => unit.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(targetUnit, startGridPosition, targetGridPosition);

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, targetUnit.GridPosition);
                float minAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().itemData.Item.Weapon.MinRange;

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
            List<GridPosition> actionAreaGridPositions = ListPool<GridPosition>.Claim();
            actionAreaGridPositions = GetActionAreaGridPositions(actionGridPosition);

            // Loop through each grid position within the action area (for example, each grid position within a Swipe attack)
            for (int i = 0; i < actionAreaGridPositions.Count; i++)
            {
                // Make sure there's a Unit at this grid position
                if (LevelGrid.HasAnyUnitOnGridPosition(actionAreaGridPositions[i]) == false)
                    continue;

                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(actionAreaGridPositions[i]);

                // Skip this unit if they're dead
                if (unitAtGridPosition.health.IsDead())
                    continue;

                if (unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 50f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 50f - (unitAtGridPosition.health.CurrentHealthNormalized() * 50f);
                }
                else if (unit.alliance.IsAlly(unitAtGridPosition))
                {
                    // Allies in the action area decrease this action's value
                    finalActionValue -= 35f;

                    // Lower ally health gives this action less value
                    finalActionValue -= 35f - (unitAtGridPosition.health.CurrentHealthNormalized() * 35f);

                    // Provide some padding in case the ally is the Player (we don't want their allied followers hitting them)
                    if (unitAtGridPosition.IsPlayer)
                        finalActionValue -= 100f;
                }
                else // If IsNeutral
                {
                    // Neutral units in the action area decrease this action's value, but to a lesser extent than allies
                    finalActionValue -= 15f;

                    // Lower neutral unit health gives this action less value
                    finalActionValue -= 15f - (unitAtGridPosition.health.CurrentHealthNormalized() * 15f);

                    // Provide some padding in case the neutral unit is the Player (we don't want neutral units to hit the Player unless it's a very desireable position to attack)
                    if (unitAtGridPosition.IsPlayer)
                        finalActionValue -= 50f;
                }
            }

            ListPool<GridPosition>.Release(actionAreaGridPositions);

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = actionGridPosition,
                actionValue = Mathf.RoundToInt(finalActionValue)
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

        protected override void StartAction()
        {
            base.StartAction();
            unit.unitActionHandler.SetIsAttacking(true);
            unit.stats.UseEnergy(GetEnergyCost());
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            if (unit.IsPlayer)
                unit.unitActionHandler.SetTargetEnemyUnit(null);

            unit.unitActionHandler.FinishAction();
        }

        public override int GetEnergyCost() => 25;

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.MeleeWeaponEquipped();

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => ActionSystem.ActionBarSection.Default;

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;
    }
}
