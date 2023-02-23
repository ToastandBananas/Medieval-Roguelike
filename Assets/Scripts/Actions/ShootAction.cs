using Pathfinding;
using Pathfinding.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
                Shoot();
            else
            {
                nextAttackFree = true;
                CompleteAction();
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
            }
        }
        else
        {
            CompleteAction();
            unit.unitActionHandler.TakeTurn();
            return;
        }
    }

    void Shoot()
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        BecomeVisibleEnemyOfTarget(targetUnit);

        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            StartCoroutine(RotateTowardsTarget());
            unit.leftHeldItem.DoDefaultAttack(AttackBlocked(targetUnit), null);

            StartCoroutine(WaitToFinishAction());
        }
        else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
        {
            bool missedTarget = MissedTarget();
            bool attackBlocked = AttackBlocked(targetUnit);
            if (missedTarget == false)
                DamageTarget(targetUnit, unit.GetRangedWeapon(), attackBlocked);

            if (attackBlocked)
                StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, true));

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }
    }

    public void DamageTarget(Unit targetUnit, HeldRangedWeapon heldRangedWeapon, bool attackBlocked)
    {
        if (targetUnit != null)
        {
            int damageAmount = heldRangedWeapon.itemData.damage;
            int armorAbsorbAmount = 0;

            if (attackBlocked)
            {
                int blockAmount = 0;
                if (targetUnit.ShieldEquipped())
                    blockAmount = targetUnit.GetShield().itemData.blockPower;

                targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount - blockAmount);

                heldRangedWeapon.ResetAttackBlocked();

                if (targetUnit.ShieldEquipped())
                    targetUnit.GetShield().LowerShield();
            }
            else
                targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount);
        }

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

    bool AttackBlocked(Unit targetUnit)
    {
        if (targetUnit.ShieldEquipped())
        {
            if (targetUnit.unitActionHandler.GetAction<TurnAction>().AttackerInFrontOfUnit(unit))
            {
                float random = Random.Range(1f, 100f);
                if (random <= targetUnit.stats.ShieldBlockChance(targetUnit.GetShield(), false))
                    return true;
            }
            else if (targetUnit.unitActionHandler.GetAction<TurnAction>().AttackerBesideUnit(unit))
            {
                float random = Random.Range(1f, 100f);
                if (random <= targetUnit.stats.ShieldBlockChance(targetUnit.GetShield(), true))
                    return true;
            }
        }
        return false;
    }

    IEnumerator WaitToFinishAction()
    {
        if (unit.leftHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetWeaponAttackAnimationTime(unit.leftHeldItem.itemData.item as Weapon));
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

    public bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
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

    public bool IsInAttackRange(Unit targetUnit) => IsInAttackRange(targetUnit, unit.gridPosition, targetUnit.gridPosition);

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

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        if (nextAttackFree || RangedWeaponIsLoaded() == false)
        {
            nextAttackFree = false;
            return 0;
        }
        return 300;
    }

    public override List<GridPosition> GetValidActionGridPositionList(GridPosition startGridPosition)
    {
        float maxAttackRange = unit.GetRangedWeapon().itemData.item.Weapon().maxRange;

        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(((startGridPosition.y + maxAttackRange) * 2) + 0.1f, ((startGridPosition.y + maxAttackRange) * 2) + 0.1f, ((startGridPosition.y + maxAttackRange) * 2) + 0.1f)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition) == false) // Grid Position is empty, no Unit to shoot
                continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If the target is dead
            if (targetUnit.health.IsDead())
                continue;

            // If the target is out of sight
            if (unit.vision.IsVisible(targetUnit) == false)
                continue;

            // If both Units are on the same team
            if (unit.alliance.IsAlly(targetUnit) || unit.alliance.IsNeutral(targetUnit))
                continue;

            // If target is out of attack range
            if (IsInAttackRange(targetUnit, startGridPosition, nodeGridPosition) == false)
                continue;

            // Debug.Log(gridPosition);
            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public override List<GridPosition> GetValidActionGridPositionList_Secondary(GridPosition startGridPosition)
    {
        float maxAttackRange = unit.GetRangedWeapon().itemData.item.Weapon().maxRange;

        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(((startGridPosition.y + maxAttackRange) * 2) + 0.1f, ((startGridPosition.y + maxAttackRange) * 2) + 0.1f, ((startGridPosition.y + maxAttackRange) * 2) + 0.1f)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition) == false) // Grid Position is empty, no Unit to shoot
                continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If the target is dead
            if (targetUnit.health.IsDead())
                continue;

            // If the target is out of sight
            if (unit.vision.IsVisible(targetUnit) == false)
                continue;

            // If both Units are on the same team
            if (unit.alliance.IsNeutral(targetUnit) == false)
                continue;

            // If target is out of attack range
            if (IsInAttackRange(targetUnit, startGridPosition, nodeGridPosition) == false)
                continue;

            // Debug.Log(gridPosition);
            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
    {
        validGridPositionsList.Clear();
        if (targetUnit == null)
            return validGridPositionsList;

        float maxAttackRange = unit.GetRangedWeapon().itemData.item.Weapon().maxRange;

        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.transform.position, new Vector3(((targetUnit.gridPosition.y + maxAttackRange) * 2) + 0.1f, ((targetUnit.gridPosition.y + maxAttackRange) * 2) + 0.1f, ((targetUnit.gridPosition.y + maxAttackRange) * 2) + 0.1f)));

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

    public GridPosition GetNearestShootPosition(GridPosition startGridPosition, Unit targetUnit)
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

    public override EnemyAIAction GetEnemyAIAction(Unit targetUnit)
    {
        float finalActionValue = 0f;
        if (targetUnit != null && IsValidAction())
        {
            targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetUnit.gridPosition);

            if (targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetUnit.gridPosition);
                if (distance < unit.GetRangedWeapon().itemData.item.Weapon().minRange)
                    finalActionValue = 0f;
                else
                    finalActionValue -= distance * 10f;
            }
        }
        
        if (targetUnit == null) 
            return new EnemyAIAction
            {
                unit = null,
                gridPosition = unit.gridPosition,
                actionValue = -1
            }; 
        
        return new EnemyAIAction
        {
            unit = targetUnit,
            gridPosition = targetUnit.gridPosition,
            actionValue = Mathf.RoundToInt(finalActionValue)
        };
    }

    void MoveAction_OnStopMoving(object sender, EventArgs e) => nextAttackFree = false;

    public bool RangedWeaponIsLoaded() => unit.GetRangedWeapon().isLoaded; 

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Shoot";

    public override bool IsValidAction()
    {
        if (unit.RangedWeaponEquipped())
            return true;
        return false;
    }
}
