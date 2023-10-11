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
    public class SwipeAction : BaseAttackAction
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

        public override void TakeAction()
        {
            if (unit.unitActionHandler.isAttacking) return;

            if (IsValidUnitInActionArea(targetGridPosition) == false || unit.stats.HasEnoughEnergy(GetEnergyCost()) == false)
            {
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.ClearQueuedAttack();
                unit.unitActionHandler.FinishAction();
                return;
            }

            StartAction();

            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition))
                StartCoroutine(Attack());
            else
            {
                CompleteAction();
                if (unit.IsPlayer)
                    unit.unitActionHandler.TakeTurn();
                return;
            }
        }

        IEnumerator Attack()
        {
            TurnAction turnAction = unit.unitActionHandler.GetAction<TurnAction>();

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
                        unit.unitActionHandler.GetAction<MoveAction>().QueueAction(GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));

                        CompleteAction();
                        if (unit.IsPlayer)
                            unit.unitActionHandler.TakeTurn();

                        yield break;
                    }
                }

                // Rotate towards the target
                if (turnAction.IsFacingTarget(targetGridPosition) == false)
                    turnAction.RotateTowardsPosition(targetGridPosition.WorldPosition(), false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.isRotating)
                    yield return null;

                // Play the attack animations and handle blocking for each target
                unit.unitAnimator.StartMeleeAttack();
                unit.unitMeshManager.GetPrimaryMeleeWeapon().DoSwipeAttack();

                // Wait until the attack lands before completing the action
                StartCoroutine(WaitToCompleteAction());
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Rotate towards the target
                if (turnAction.IsFacingTarget(targetGridPosition) == false)
                    turnAction.RotateTowardsPosition(targetGridPosition.WorldPosition(), true);

                // Loop through the grid positions in the attack area
                foreach (GridPosition gridPosition in GetActionAreaGridPositions(targetGridPosition))
                {
                    // Skip this position if there's no unit here
                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition) == false)
                        continue;

                    // Get the unit at this grid position
                    Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

                    // The targetUnit tries to block the attack and if they do, they face their attacker
                    if (targetUnit.unitActionHandler.TryBlockMeleeAttack(unit))
                        targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Unit(unit, true);

                    // Damage this unit
                    DamageTargets(unit.unitMeshManager.GetPrimaryMeleeWeapon());
                }

                CompleteAction();
            }
        }

        public override void DamageTargets(HeldItem heldWeapon)
        {
            HeldMeleeWeapon heldMeleeWeapon = heldWeapon as HeldMeleeWeapon;
            foreach (KeyValuePair<Unit, HeldItem> target in unit.unitActionHandler.targetUnits)
            {
                Unit targetUnit = target.Key;
                HeldItem itemBlockedWith = target.Value;

                if (targetUnit != null && targetUnit.health.IsDead() == false)
                {
                    // The unit being attacked becomes aware of this unit
                    BecomeVisibleEnemyOfTarget(targetUnit);

                    int armorAbsorbAmount = 0;
                    int damageAmount = heldMeleeWeapon.DamageAmount();

                    // If the attack was blocked
                    if (itemBlockedWith != null)
                    {
                        int blockAmount = 0;
                        if (targetUnit.UnitEquipment.ShieldEquipped() && itemBlockedWith == targetUnit.unitMeshManager.GetHeldShield()) // If blocked with shield
                            blockAmount = targetUnit.stats.ShieldBlockPower(targetUnit.unitMeshManager.GetHeldShield());
                        else if (targetUnit.UnitEquipment.MeleeWeaponEquipped()) // If blocked with melee weapon
                        {
                            if (itemBlockedWith == targetUnit.unitMeshManager.GetPrimaryMeleeWeapon()) // If blocked with primary weapon (only weapon, or dual wield right hand weapon)
                            {
                                blockAmount = targetUnit.stats.WeaponBlockPower(targetUnit.unitMeshManager.GetPrimaryMeleeWeapon());
                                if (unit.UnitEquipment.IsDualWielding())
                                {
                                    if (itemBlockedWith == unit.unitMeshManager.GetRightHeldMeleeWeapon())
                                        blockAmount = Mathf.RoundToInt(blockAmount * GameManager.dualWieldPrimaryEfficiency);
                                }
                            }
                            else // If blocked with dual wield left hand weapon
                                blockAmount = Mathf.RoundToInt(targetUnit.stats.WeaponBlockPower(targetUnit.unitMeshManager.GetLeftHeldMeleeWeapon()) * GameManager.dualWieldSecondaryEfficiency);
                        }

                        targetUnit.health.TakeDamage(damageAmount - blockAmount - armorAbsorbAmount, unit);

                        if (itemBlockedWith is HeldShield)
                            targetUnit.unitMeshManager.GetHeldShield().LowerShield();
                        else
                        {
                            HeldMeleeWeapon blockingWeapon = itemBlockedWith as HeldMeleeWeapon;
                            blockingWeapon.LowerWeapon();
                        }
                    }
                    else
                        targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount, unit);
                }
            }

            unit.unitActionHandler.targetUnits.Clear();
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition) => unit.unitActionHandler.GetAction<MeleeAction>().GetActionGridPositionsInRange(startGridPosition);

        public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float maxAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().ItemData.Item.Weapon.MaxRange;

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

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                Vector3 attackDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight * 2f)) - (targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f))).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight * 2f), targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f)), unit.unitActionHandler.AttackObstacleMask))
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
            Vector3 directionToTarget = (targetGridPosition.WorldPosition() - unit.WorldPosition).normalized;
            Vector3 generalDirection = GetGeneralDirection(directionToTarget);

            if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
                return validGridPositionsList;

            // Exclude attacker's position
            if (targetGridPosition == unit.GridPosition)
                return validGridPositionsList;

            // Check if the target position is within the max attack range
            //if (Vector3.Distance(unit.WorldPosition, targetGridPosition.WorldPosition()) > maxAttackRange)
            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition) == false)
                return validGridPositionsList;

            // Check for obstacles
            float sphereCastRadius = 0.1f;
            Vector3 heightOffset = Vector3.up * unit.ShoulderHeight * 2f;
            if (Physics.SphereCast(unit.WorldPosition + heightOffset, sphereCastRadius, directionToTarget, out RaycastHit hit, Vector3.Distance(targetGridPosition.WorldPosition() + heightOffset, unit.WorldPosition + heightOffset), unit.unitActionHandler.AttackObstacleMask))
            {
                // Debug.Log(targetGridPosition.WorldPosition() + " (the target position) is blocked by " + hit.collider.name);
                return validGridPositionsList;
            }

            float maxAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().ItemData.Item.Weapon.MaxRange;
            float boundsDimension = (maxAttackRange * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

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
                Vector3 yDifference = Vector3.up * (nodeGridPosition.WorldPosition().y - unit.WorldPosition.y);
                Vector3 directionToNode = (nodeGridPosition.WorldPosition() - (unit.WorldPosition + yDifference)).normalized;
                float angleBetweenDirections = Vector3.Angle(generalDirection, directionToNode);
                if (angleBetweenDirections > 45f)
                    continue;

                // Check if the node is within the max attack range
                //if (Vector3.Distance(unit.WorldPosition, nodeGridPosition.WorldPosition()) > maxAttackRange)
                if (IsInAttackRange(null, unit.GridPosition, nodeGridPosition) == false)
                    continue;

                // Make sure the node isn't too much lower or higher than the target grid position (this is a swipe attack, so think of it basically swiping across in a horizontal line)
                if (Mathf.Abs(nodeGridPosition.WorldPosition().y - targetGridPosition.WorldPosition().y) > 0.5f)
                    continue;

                // Check for obstacles
                Vector3 directionToAttackPosition = ((nodeGridPosition.WorldPosition() + heightOffset) - (unit.WorldPosition + heightOffset)).normalized;
                if (Physics.SphereCast(unit.WorldPosition + heightOffset, sphereCastRadius, directionToAttackPosition, out hit, Vector3.Distance(nodeGridPosition.WorldPosition() + heightOffset, unit.WorldPosition + heightOffset), unit.unitActionHandler.AttackObstacleMask))
                {
                    // Debug.Log(nodeGridPosition.WorldPosition() + " is blocked by " + hit.collider.name);
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
            List<GridPosition> attackGridPositions = ListPool<GridPosition>.Claim();
            attackGridPositions = GetActionAreaGridPositions(targetGridPosition);
            for (int i = 0; i < attackGridPositions.Count; i++)
            {
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(attackGridPositions[i]) == false)
                    continue;

                Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(attackGridPositions[i]);
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

        public override bool IsInAttackRange(Unit targetUnit) => unit.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(targetUnit);

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, targetUnit.GridPosition);
                float minAttackRange = unit.unitMeshManager.GetPrimaryMeleeWeapon().ItemData.Item.Weapon.MinRange;

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
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(actionAreaGridPositions[i]) == false)
                    continue;

                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(actionAreaGridPositions[i]);

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
            unit.unitActionHandler.GetAction<TurnAction>().DetermineTargetTurnDirection(targetGridPosition);
            cost += unit.unitActionHandler.GetAction<TurnAction>().GetActionPointsCost();
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

            unit.unitActionHandler.SetIsAttacking(false);
            if (unit.IsPlayer)
                unit.unitActionHandler.SetTargetEnemyUnit(null);

            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        IEnumerator WaitToCompleteAction()
        {
            yield return new WaitForSeconds(AnimationTimes.Instance.SwipeAttackTime());

            CompleteAction();
        }

        public override int GetEnergyCost() => 25;

        public override bool IsHotbarAction() => true;

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.MeleeWeaponEquipped();

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;
    }
}
