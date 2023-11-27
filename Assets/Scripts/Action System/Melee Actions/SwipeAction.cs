using Pathfinding;
using Pathfinding.Util;
using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using Utilities;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using System.Collections;

namespace UnitSystem.ActionSystem
{
    public class SwipeAction : BaseAttackAction
    {
        readonly int baseAPCost = 300;

        public override int ActionPointsCost()
        {
            float cost;
            if (unit.UnitEquipment != null)
            {
                cost = baseAPCost * ActionPointCostModifier_WeaponType(unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon);

                if (unit.UnitEquipment.InVersatileStance)
                    cost *= 1.35f;
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

            if (IsValidUnitInActionArea(targetGridPosition) == false || unit.stats.HasEnoughEnergy(InitialEnergyCost()) == false)
            {
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.ClearQueuedAttack();
                unit.unitActionHandler.FinishAction();
                return;
            }

            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition))
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

        public override void PlayAttackAnimation()
        {
            unit.unitAnimator.StartMeleeAttack();
            unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().DoSwipeAttack(targetGridPosition);
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
            if (IsInAttackRange(null, unit.GridPosition, targetGridPosition) == false)
                return validGridPositionsList;

            // Check for obstacles
            float sphereCastRadius = 0.1f;
            Vector3 heightOffset = 2f * unit.ShoulderHeight * Vector3.up;
            if (Physics.SphereCast(unit.WorldPosition + heightOffset, sphereCastRadius, directionToTarget, out RaycastHit hit, Vector3.Distance(targetGridPosition.WorldPosition + heightOffset, unit.WorldPosition + heightOffset), unit.unitActionHandler.AttackObstacleMask))
            {
                // Debug.Log(targetGridPosition.WorldPosition + " (the target position) is blocked by " + hit.collider.name);
                return validGridPositionsList;
            }

            float maxAttackRange = unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MaxRange;
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
                if (Physics.SphereCast(unit.WorldPosition + heightOffset, sphereCastRadius, directionToAttackPosition, out _, Vector3.Distance(nodeGridPosition.WorldPosition + heightOffset, unit.WorldPosition + heightOffset), unit.unitActionHandler.AttackObstacleMask))
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

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized * 100f);
                float distance = Vector3.Distance(unit.WorldPosition, targetUnit.WorldPosition);

                if (distance < MinAttackRange())
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
                if (LevelGrid.HasUnitAtGridPosition(actionAreaGridPositions[i], out Unit unitAtGridPosition) == false)
                    continue;

                // Skip this unit if they're dead
                if (unitAtGridPosition.health.IsDead)
                    continue;

                if (unit.alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 50f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 50f - (unitAtGridPosition.health.CurrentHealthNormalized * 50f);
                }
                else if (unit.alliance.IsAlly(unitAtGridPosition))
                {
                    // Allies in the action area decrease this action's value
                    finalActionValue -= 100f;

                    // Lower ally health gives this action less value
                    finalActionValue -= 100f - (unitAtGridPosition.health.CurrentHealthNormalized * 100f);

                    // Provide some padding in case the ally is the Player (we don't want their allied followers hitting them)
                    if (unitAtGridPosition.IsPlayer)
                        finalActionValue -= 100f;
                }
                else // If IsNeutral
                {
                    // Neutral units in the action area decrease this action's value, but to a lesser extent than allies
                    finalActionValue -= 25f;

                    // Lower neutral unit health gives this action less value
                    finalActionValue -= 25f - (unitAtGridPosition.health.CurrentHealthNormalized * 25f);

                    // Provide some padding in case the neutral unit is the Player (we don't want neutral units to hit the Player unless it's a very desireable position to attack)
                    if (unitAtGridPosition.IsPlayer)
                        finalActionValue -= 75f;
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

        public override IEnumerator WaitToDamageTargets(HeldItem heldWeaponAttackingWith, ItemData itemDataHittingWith)
        {
            // TODO: Come up with a headshot method
            bool headShot = false;

            yield return new WaitForSeconds(AnimationTimes.Instance.SwipeAttackTime());

            DamageTargets(heldWeaponAttackingWith, itemDataHittingWith, headShot);
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            if (unit.IsPlayer)
                unit.unitActionHandler.SetTargetEnemyUnit(null);

            unit.unitActionHandler.FinishAction();
        }

        public override string TooltipDescription()
        {
            return $"Harness the might of your <b>{unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Name}</b> to execute a wide-reaching swipe, striking multiple foes in a single, devastating motion.";
        }

        public override float AccuracyModifier() => 0.75f;

        public override int InitialEnergyCost() => 25;

        public override bool IsValidAction() => unit != null && unit.UnitEquipment.MeleeWeaponEquipped;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Special;

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;

        public override bool CanAttackThroughUnits() => true;

        public override float MinAttackRange() => unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MinRange;

        public override float MaxAttackRange() => unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MaxRange;
    }
}
