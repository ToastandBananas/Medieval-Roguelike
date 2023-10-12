using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using GridSystem;
using InventorySystem;
using UnitSystem;
using Utilities;

namespace ActionSystem
{
    public class ShootAction : BaseAttackAction
    {
        List<GridPosition> validGridPositionsList = new List<GridPosition>();
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition))
                targetEnemyUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        }

        public override void TakeAction()
        {
            if (unit.unitActionHandler.isAttacking) return;

            if (targetEnemyUnit == null)
            {
                if (unit.unitActionHandler.targetEnemyUnit != null)
                    targetEnemyUnit = unit.unitActionHandler.targetEnemyUnit;
                else if (LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition))
                    targetEnemyUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);
            }

            if (targetEnemyUnit == null || targetEnemyUnit.health.IsDead())
            {
                targetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.FinishAction();
                return;
            }

            StartAction();

            if (RangedWeaponIsLoaded() == false)
            {
                CompleteAction();
                unit.unitActionHandler.SetTargetEnemyUnit(targetEnemyUnit);
                unit.unitActionHandler.GetAction<ReloadAction>().QueueAction();
                return;
            }
            else if (IsInAttackRange(targetEnemyUnit))
                unit.StartCoroutine(Shoot());
            else
            {
                CompleteAction();
                if (unit.IsPlayer)
                    unit.unitActionHandler.TakeTurn();
                return;
            }
        }

        IEnumerator Shoot()
        {
            TurnAction turnAction = unit.unitActionHandler.GetAction<TurnAction>();

            // The unit being attacked becomes aware of this unit
            BecomeVisibleEnemyOfTarget(targetEnemyUnit);

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen())
            {
                if (targetEnemyUnit.unitActionHandler.isMoving)
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
                if (turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    turnAction.RotateTowards_Unit(targetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (unit.unitActionHandler.isRotating)
                    yield return null;

                // Rotate towards the target and do the shoot animation
                unit.StartCoroutine(RotateTowardsTarget());
                unit.unitMeshManager.GetHeldRangedWeapon().DoDefaultAttack();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                bool missedTarget = MissedTarget();
                bool attackBlocked = targetEnemyUnit.unitActionHandler.TryBlockRangedAttack(unit);
                if (missedTarget == false)
                    DamageTargets(unit.unitMeshManager.GetHeldRangedWeapon());

                // Rotate towards the target
                if (turnAction.IsFacingTarget(targetEnemyUnit.GridPosition) == false)
                    turnAction.RotateTowards_Unit(targetEnemyUnit, true);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    targetEnemyUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Unit(unit, true);
            }

            while (unit.unitActionHandler.isAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override void DamageTargets(HeldItem heldWeapon)
        {
            HeldRangedWeapon heldRangedWeapon = heldWeapon as HeldRangedWeapon;
            foreach (KeyValuePair<Unit, HeldItem> target in unit.unitActionHandler.targetUnits)
            {
                Unit targetUnit = target.Key;
                HeldItem itemBlockedWith = target.Value;
                if (targetUnit != null && targetUnit.health.IsDead() == false)
                {
                    int damageAmount = heldRangedWeapon.ItemData.Damage;
                    int armorAbsorbAmount = 0;

                    // If the attack was blocked
                    if (itemBlockedWith != null)
                    {
                        int blockAmount = 0;
                        if (targetUnit.UnitEquipment.ShieldEquipped())
                            blockAmount = targetUnit.stats.ShieldBlockPower(targetUnit.unitMeshManager.GetHeldShield());

                        targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount - blockAmount, unit);

                        if (targetUnit.UnitEquipment.ShieldEquipped())
                            targetUnit.unitMeshManager.GetHeldShield().LowerShield();
                    }
                    else
                        targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount, unit);
                }
            }

            unit.unitActionHandler.targetUnits.Clear();

            if (unit.IsPlayer && PlayerInput.Instance.autoAttack == false)
            {
                targetEnemyUnit = null;
                unit.unitActionHandler.SetTargetEnemyUnit(null);
            }
        }

        public bool MissedTarget()
        {
            float random = Random.Range(0f, 100f);
            float rangedAccuracy = unit.stats.RangedAccuracy(unit.unitMeshManager.GetHeldRangedWeapon().ItemData);
            if (random > rangedAccuracy)
                return true;
            return false;
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the projectile hits something, rather than in this method as is with non-ranged actions
            base.CompleteAction();
            if (unit.IsPlayer)
            {
                unit.unitActionHandler.SetDefaultSelectedAction();
                if (PlayerInput.Instance.autoAttack == false)
                    unit.unitActionHandler.SetTargetEnemyUnit(null);
            }

            unit.unitActionHandler.SetIsAttacking(false);
            unit.unitActionHandler.FinishAction();
        }

        IEnumerator WaitToCompleteAction()
        {
            if (unit.unitMeshManager.GetHeldRangedWeapon() != null)
                yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item as Weapon));
            else
                yield return new WaitForSeconds(1f);

            CompleteAction();
        }

        IEnumerator RotateTowardsTarget()
        {
            Vector3 targetPos = unit.unitActionHandler.targetEnemyUnit.WorldPosition;
            while (unit.unitActionHandler.isAttacking)
            {
                float rotateSpeed = 10f;
                Vector3 lookPos = (new Vector3(targetPos.x, transform.position.y, targetPos.z) - unit.WorldPosition).normalized;
                Quaternion rotation = Quaternion.LookRotation(lookPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
                yield return null;
            }

            // After this Unit is done shooting, rotate back towards their TurnAction's currentDirection
            unit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Direction(unit.unitActionHandler.GetAction<TurnAction>().currentDirection, false);
        }

        public override bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            if (targetUnit != null && unit.vision.IsInLineOfSight_SphereCast(targetUnit) == false)
                return false;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, targetGridPosition);
            Weapon rangedWeapon = unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon;
            float maxRangeToTargetPosition = rangedWeapon.MaxRange + (startGridPosition.y - targetGridPosition.y);
            if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;

            if (distance > maxRangeToTargetPosition || distance < rangedWeapon.MinRange)
                return false;
            return true;
        }

        public override bool IsInAttackRange(Unit targetUnit) => IsInAttackRange(targetUnit, unit.GridPosition, targetUnit.GridPosition);

        protected override void StartAction()
        {
            base.StartAction();
            unit.unitActionHandler.SetIsAttacking(true);
        }

        public override int GetActionPointsCost()
        {
            int cost = 300;

            // If not facing the target position, add the cost of turning towards that position
            unit.unitActionHandler.GetAction<TurnAction>().DetermineTargetTurnDirection(targetEnemyUnit.GridPosition);
            cost += unit.unitActionHandler.GetAction<TurnAction>().GetActionPointsCost();
            return cost;
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            float minRange = unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange;
            float maxRange = unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MaxRange;
            float boundsDimension = ((startGridPosition.y + maxRange) * 2) + 0.1f;

            validGridPositionsList.Clear();
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                float maxRangeToNodePosition = maxRange + (startGridPosition.y - nodeGridPosition.y);
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, nodeGridPosition);
                if (distance > maxRangeToNodePosition || distance < minRange)
                    continue;

                float sphereCastRadius = 0.1f;
                Vector3 offset = Vector3.up * unit.ShoulderHeight * 2f;
                Vector3 shootDir = ((nodeGridPosition.WorldPosition() + offset) - (startGridPosition.WorldPosition() + offset)).normalized;
                if (Physics.SphereCast(startGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition + offset, nodeGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask))
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
            Vector3 offset = Vector3.up * unit.ShoulderHeight * 2f;
            Vector3 shootDir = ((unit.WorldPosition + offset) - (targetGridPosition.WorldPosition() + offset)).normalized;
            if (Physics.SphereCast(targetGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition + offset, targetGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask))
                return validGridPositionsList; // Blocked by an obstacle

            validGridPositionsList.Add(targetGridPosition);
            return validGridPositionsList;
        }

        public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float maxAttackRange = unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MaxRange;
            float boundsDimension = ((targetUnit.GridPosition.y + maxAttackRange) * 2) + 0.1f;

            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.transform.position, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                // Grid Position has a Unit there already
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition))
                    continue;

                // If target is out of attack range
                if (IsInAttackRange(null, nodeGridPosition, targetUnit.GridPosition) == false)
                    continue;

                float sphereCastRadius = 0.1f;
                Vector3 shootDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight * 2f)) - (targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f))).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight * 2f), targetUnit.WorldPosition + (Vector3.up * targetUnit.ShoulderHeight * 2f)), unit.unitActionHandler.AttackObstacleMask))
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
            float nearestDistanceToTarget = 10000000f;
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

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, targetUnit.GridPosition);
                if (distance < unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
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

                    float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, actionGridPosition);
                    if (distance < unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
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
            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && unit.alliance.IsAlly(unitAtGridPosition) == false && unit.vision.IsVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (unit != null && unit.UnitEquipment.RangedWeaponEquipped() && unit.UnitEquipment.HasValidAmmunitionEquipped())
                return true;
            return false;
        }

        public override int GetEnergyCost() => 0;

        public override bool CanQueueMultiple() => false;

        public override bool IsHotbarAction() => true;

        public bool RangedWeaponIsLoaded() => unit.unitMeshManager.GetHeldRangedWeapon().isLoaded;

        public override bool ActionIsUsedInstantly() => false;

        public override bool IsMeleeAttackAction() => false;

        public override bool IsRangedAttackAction() => true;
    }
}
