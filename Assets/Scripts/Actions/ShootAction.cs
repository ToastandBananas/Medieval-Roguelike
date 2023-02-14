using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShootAction : BaseAction
{
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
            unit.leftHeldItem.DoDefaultAttack(AttackBlocked(targetUnit));

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
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
        }
    }

    public void DamageTarget(Unit targetUnit, HeldRangedWeapon heldRangedWeapon, bool attackBlocked)
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

        if (unit.IsPlayer() && PlayerActionInput.Instance.autoAttack == false)
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
        if (targetUnit.ShieldEquipped() && targetUnit.unitActionHandler.GetAction<TurnAction>().IsFacingUnit(unit))
        {
            float random = Random.Range(1f, 100f);
            if (random <= targetUnit.stats.ShieldBlockChance(targetUnit.GetShield().itemData))
                return true;
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
        if (unit.IsPlayer() && PlayerActionInput.Instance.autoAttack == false)
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
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        ConstantPath path = ConstantPath.Construct(unit.WorldPosition(), 100100);

        // Schedule the path for calculation
        AstarPath.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (LevelGrid.Instance.IsValidGridPosition(nodeGridPosition) == false)
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
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public override List<GridPosition> GetValidActionGridPositionList_Secondary(GridPosition startGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        ConstantPath path = ConstantPath.Construct(unit.WorldPosition(), 100100);

        // Schedule the path for calculation
        AstarPath.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (LevelGrid.Instance.IsValidGridPosition(nodeGridPosition) == false)
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
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public List<GridPosition> GetValidGridPositionsInRange(GridPosition targetGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);

        ConstantPath path = ConstantPath.Construct(unit.WorldPosition(), 100100);

        // Schedule the path for calculation
        AstarPath.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (LevelGrid.Instance.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            // Grid Position has a Unit there already
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition)) 
                continue;

            // If target is out of attack range
            if (IsInAttackRange(null, nodeGridPosition, targetGridPosition) == false)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f)) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f)), unit.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public GridPosition GetNearestShootPosition(GridPosition startGridPosition, GridPosition targetGridPosition)
    {
        List<GridPosition> validGridPositionsList = GetValidGridPositionsInRange(targetGridPosition);
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();
        float nearestDistance = 10000000f;

        // First find the nearest valid Grid Positions to the Player
        for (int i = 0; i < validGridPositionsList.Count; i++)
        {
            float distance = Vector3.Distance(validGridPositionsList[i].WorldPosition(), startGridPosition.WorldPosition());
            if (distance < nearestDistance)
            {
                nearestGridPositionsList.Clear();
                nearestGridPositionsList.Add(validGridPositionsList[i]);
                nearestDistance = distance;
            }
            else if (Mathf.Approximately(distance, nearestDistance))
                nearestGridPositionsList.Add(validGridPositionsList[i]);
        }

        GridPosition nearestGridPosition = startGridPosition;
        float nearestDistanceToTarget = 10000000f;
        for (int i = 0; i < nearestGridPositionsList.Count; i++)
        {
            // Get the Grid Position that is closest to the target Grid Position
            float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition(), targetGridPosition.WorldPosition());
            if (distance < nearestDistanceToTarget)
            {
                nearestDistanceToTarget = distance;
                nearestGridPosition = nearestGridPositionsList[i];
            }
        }

        return nearestGridPosition;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        float finalActionValue = 0f;
        Unit targetUnit = null;

        if (IsValidAction())
        {
            targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            if (targetUnit != null)
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

        return new EnemyAIAction
        {
            unit = targetUnit,
            gridPosition = gridPosition,
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
