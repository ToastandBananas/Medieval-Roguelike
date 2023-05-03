using Pathfinding;
using Pathfinding.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GridSystemVisual;
using Random = UnityEngine.Random;

public class ShootAction : BaseAction
{
    List<GridPosition> validGridPositionsList = new List<GridPosition>();
    List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

    public bool isShooting { get; private set; }
    bool nextAttackFree;

    void Start()
    {
        unit.unitActionHandler.GetAction<MoveAction>().OnStopMoving += MoveAction_OnStopMoving;
    }

    public override void TakeAction(GridPosition gridPosition)
    {
        if (isShooting) return;

        if (unit.unitActionHandler.targetEnemyUnit == null || unit.unitActionHandler.targetEnemyUnit.health.IsDead())
        {
            unit.unitActionHandler.FinishAction();
            return;
        }

        StartAction();

        if (RangedWeaponIsLoaded() == false)
        {
            CompleteAction();
            unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<ReloadAction>());
            return;
        }
        else if (IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))
        {
            if (unit.unitActionHandler.GetAction<TurnAction>().IsFacingTarget(unit.unitActionHandler.targetEnemyUnit.gridPosition))
                StartCoroutine(Shoot());
            else
            {
                nextAttackFree = true;
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
                CompleteAction();
            }
        }
        else
        {
            CompleteAction();
            unit.unitActionHandler.TakeTurn();
            return;
        }
    }

    IEnumerator Shoot()
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;

        // The unit being attacked becomes aware of this unit
        BecomeVisibleEnemyOfTarget(targetUnit);

        // If this is the Player attacking, or if this is an NPC that's visible on screen
        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            // Wait to finish any rotations already in progress
            while (unit.unitActionHandler.GetAction<TurnAction>().isRotating)
                yield return null;

            // Rotate towards the target and do the shoot animation
            StartCoroutine(RotateTowardsTarget());
            unit.leftHeldItem.DoDefaultAttack();

            StartCoroutine(WaitToFinishAction());
        }
        else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
        {
            bool missedTarget = MissedTarget();
            bool attackBlocked = targetUnit.TryBlockRangedAttack(unit);
            if (missedTarget == false)
                DamageTargets(unit.GetRangedWeapon());

            if (attackBlocked)
                StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, true));

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }
    }

    public override void DamageTargets(HeldItem heldWeapon)
    {
        HeldRangedWeapon heldRangedWeapon = heldWeapon as HeldRangedWeapon;
        foreach (KeyValuePair<Unit, HeldItem> target in unit.unitActionHandler.targetUnits)
        {
            Unit targetUnit = target.Key;
            HeldItem itemBlockedWith = target.Value;
            if (targetUnit != null)
            {
                int damageAmount = heldRangedWeapon.itemData.damage;
                int armorAbsorbAmount = 0;

                // If the attack was blocked
                if (itemBlockedWith != null)
                {
                    int blockAmount = 0;
                    if (targetUnit.ShieldEquipped())
                        blockAmount = targetUnit.stats.ShieldBlockPower(targetUnit.GetShield());

                    targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount - blockAmount);

                    if (targetUnit.ShieldEquipped())
                        targetUnit.GetShield().LowerShield();
                }
                else
                    targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount);
            }
        }

        unit.unitActionHandler.targetUnits.Clear();

        if (unit.IsPlayer() && PlayerInput.Instance.autoAttack == false)
            unit.unitActionHandler.SetTargetEnemyUnit(null);
    }

    public bool MissedTarget()
    {
        float random = Random.Range(0f, 100f);
        float rangedAccuracy = unit.stats.RangedAccuracy(unit.GetRangedWeapon().itemData);
        if (random > rangedAccuracy)
            return true;
        return false;
    }

    IEnumerator WaitToFinishAction()
    {
        if (unit.leftHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(unit.leftHeldItem.itemData.item as Weapon));
        else
            yield return new WaitForSeconds(0.5f);

        CompleteAction();
    }

    IEnumerator RotateTowardsTarget()
    {
        Vector3 targetPos = unit.unitActionHandler.targetEnemyUnit.WorldPosition();
        while (isShooting)
        {
            float rotateSpeed = 10f;
            Vector3 lookPos = (new Vector3(targetPos.x, transform.position.y, targetPos.z) - unit.WorldPosition()).normalized;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        unit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Direction(unit.unitActionHandler.GetAction<TurnAction>().currentDirection, false);
    }

    public override bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
    {
        if (targetUnit != null && unit.vision.IsInLineOfSight(targetUnit) == false)
            return false;

        float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, targetGridPosition);
        Weapon rangedWeapon = unit.GetRangedWeapon().itemData.item.Weapon();
        float maxRangeToTargetPosition = rangedWeapon.maxRange + (startGridPosition.y - targetGridPosition.y);
        if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;

        if (distance > maxRangeToTargetPosition || distance < rangedWeapon.minRange)
            return false;
        return true;
    }

    public override bool IsInAttackRange(Unit targetUnit) => IsInAttackRange(targetUnit, unit.gridPosition, targetUnit.gridPosition);

    protected override void StartAction()
    {
        base.StartAction();
        isShooting = true;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();
        if (unit.IsPlayer() && PlayerInput.Instance.autoAttack == false)
            unit.unitActionHandler.SetTargetEnemyUnit(null);
        isShooting = false;
        unit.unitActionHandler.FinishAction();
    }

    public override int GetActionPointsCost()
    {
        if (nextAttackFree || RangedWeaponIsLoaded() == false)
        {
            nextAttackFree = false;
            return 0;
        }
        return 300;
    }

    public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
    {
        float minRange = unit.GetRangedWeapon().itemData.item.Weapon().minRange;
        float maxRange = unit.GetRangedWeapon().itemData.item.Weapon().maxRange;
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
            Vector3 offset = Vector3.up * unit.ShoulderHeight() * 2f;
            Vector3 shootDir = ((nodeGridPosition.WorldPosition() + offset) - (startGridPosition.WorldPosition() + offset)).normalized;
            if (Physics.SphereCast(startGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition() + offset, nodeGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask()))
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

        if (IsInAttackRange(null, unit.gridPosition, targetGridPosition) == false)
            return validGridPositionsList;

        float sphereCastRadius = 0.1f;
        Vector3 offset = Vector3.up * unit.ShoulderHeight() * 2f;
        Vector3 shootDir = ((unit.WorldPosition() + offset) - (targetGridPosition.WorldPosition() + offset)).normalized;
        if (Physics.SphereCast(targetGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition() + offset, targetGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask()))
            return validGridPositionsList; // Blocked by an obstacle

        validGridPositionsList.Add(targetGridPosition);
        return validGridPositionsList;
    }

    public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
    {
        validGridPositionsList.Clear();
        if (targetUnit == null)
            return validGridPositionsList;

        float maxAttackRange = unit.GetRangedWeapon().itemData.item.Weapon().maxRange;
        float boundsDimension = ((targetUnit.gridPosition.y + maxAttackRange) * 2) + 0.1f;

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
            if (IsInAttackRange(null, nodeGridPosition, targetUnit.gridPosition) == false)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f)) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f)), unit.unitActionHandler.AttackObstacleMask()))
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
            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetUnit.gridPosition);
            if (distance < unit.GetRangedWeapon().itemData.item.Weapon().minRange)
                finalActionValue = 0f;
            else
                finalActionValue -= distance * 10f;

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = targetUnit.gridPosition,
                actionValue = Mathf.RoundToInt(finalActionValue)
            };
        }

        return new NPCAIAction
        {
            baseAction = this,
            actionGridPosition = unit.gridPosition,
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
            if (unit.alliance.IsEnemy(unitAtGridPosition))
            {
                // Enemies in the action area increase this action's value
                finalActionValue += 70f;

                // Lower enemy health gives this action more value
                finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized() * 70f);

                // Favor the targetEnemyUnit
                if (unit.unitActionHandler.targetEnemyUnit != null && unitAtGridPosition == unit.unitActionHandler.targetEnemyUnit)
                    finalActionValue += 15f; 
                
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, actionGridPosition);
                if (distance < unit.GetRangedWeapon().itemData.item.Weapon().minRange)
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
            actionGridPosition = unit.gridPosition,
            actionValue = -1
        };
    }

    public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
    {
        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition) && unit.alliance.IsAlly(LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition)) == false)
            return true;
        return false;
    }

    public override bool IsValidAction()
    {
        if (unit.RangedWeaponEquipped())
            return true;
        return false;
    }

    void MoveAction_OnStopMoving(object sender, EventArgs e) => nextAttackFree = false;

    public bool RangedWeaponIsLoaded() => unit.GetRangedWeapon().isLoaded; 

    public override bool ActionIsUsedInstantly() => false;

    public override bool IsAttackAction() => true;

    public override bool IsMeleeAttackAction() => false;

    public override bool IsRangedAttackAction() => true;

    public override string GetActionName() => "Shoot";
}