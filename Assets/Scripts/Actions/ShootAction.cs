using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShootAction : BaseAction
{
    public event EventHandler OnStartShooting;
    public event EventHandler OnStopShooting;
    
    [Tooltip("If a target is any closer than this distance, the unit can't shoot the target")]
    [SerializeField] int minShootDistance = 2;
    [SerializeField] int maxShootDistance = 10;

    enum State { Aiming, Shooting, Cooloff }
    State currentState;
    float stateTimer;
    bool canShoot;
    Unit targetUnit, myUnit;

    void Start()
    {
        myUnit = GetComponent<Unit>();
    }

    void Update()
    {
        if (isActive == false)
            return;

        if (TurnSystem.Instance.IsPlayerTurn() && UnitActionSystemUI.Instance.SelectedActionValid() == false)
            return;

        stateTimer -= Time.deltaTime;
        switch (currentState)
        {
            case State.Aiming:
                float rotateSpeed = 10f;
                Vector3 lookPos = (new Vector3(targetUnit.WorldPosition().x, transform.position.y, targetUnit.WorldPosition().z) - unit.WorldPosition()).normalized;
                Quaternion rotation = Quaternion.LookRotation(lookPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
                break;
            case State.Shooting:
                if (canShoot)
                {
                    canShoot = false;
                    OnStartShooting?.Invoke(this, EventArgs.Empty);
                }
                break;
            case State.Cooloff:
                break;
        }

        if (stateTimer <= 0f)
            NextState();
    }

    void NextState()
    {
        switch (currentState)
        {
            case State.Aiming:
                currentState = State.Shooting;
                float shootingStateTime = 1.1f;
                stateTimer = shootingStateTime;
                break;
            case State.Shooting:
                currentState = State.Cooloff;
                float cooloffStateTime = 0.5f;
                stateTimer = cooloffStateTime;
                break;
            case State.Cooloff:
                OnStopShooting?.Invoke(this, EventArgs.Empty);
                CompleteAction();
                break;
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

        currentState = State.Aiming;
        float aimingStateTime = 0.25f;
        stateTimer = aimingStateTime;
        canShoot = true;

        StartAction(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GridPosition();
        return GetValidActionGridPositionList(unitGridPosition);
    }

    public List<GridPosition> GetValidActionGridPositionList(GridPosition startGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>(); 
        
        ConstantPath path = ConstantPath.Construct(unit.transform.position, 100100);

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

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, nodeGridPosition);
            if (distance > maxShootDistance || distance < minShootDistance)
                    continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If both Units are on the same team
            if (targetUnit.IsAlly(unit.GetCurrentFaction()))
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir = ((startGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight())) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight()))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight()), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(startGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight()), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight())), UnitActionSystem.Instance.ActionObstaclesMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public Unit TargetUnit() => targetUnit;

    public Unit MyUnit() => myUnit;

    public int MinShootDistance() => minShootDistance;

    public int MaxShootDistance() => maxShootDistance;

    public int PreferredShootDistance() => Mathf.RoundToInt((minShootDistance + maxShootDistance) / 2f);

    public int GetTargetCountAtPosition(GridPosition gridPosition) => GetValidActionGridPositionList(gridPosition).Count;

    public List<Unit> GetValidTargetsAtPosition(GridPosition gridPosition)
    {
        List<Unit> targets = new List<Unit>();
        List<GridPosition> gridPositionList = GetValidActionGridPositionList(gridPosition);
        for (int i = 0; i < gridPositionList.Count; i++)
        {
            targets.Add(LevelGrid.Instance.GetUnitAtGridPosition(gridPositionList[i]));
        }

        return targets;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int finalActionValue = 0;

        if (IsValidAction())
        {
            float nearestEnemyDistance = float.MaxValue;

            foreach (Unit testUnit in UnitManager.Instance.UnitsList())
            {
                if (testUnit.IsEnemy(unit.GetCurrentFaction()))
                {
                    float distanceToEnemy = Vector3.Distance(LevelGrid.Instance.GetWorldPosition(unit.GridPosition()), LevelGrid.Instance.GetWorldPosition(testUnit.GridPosition()) / LevelGrid.Instance.GridSize());
                    if (distanceToEnemy < nearestEnemyDistance)
                        nearestEnemyDistance = distanceToEnemy;
                }
            }

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
            float heightDifferenceFromEnemy = unit.GridPosition().y - targetUnit.GridPosition().y;

            // Target the Unit with the lowest health
            finalActionValue += 200 + Mathf.RoundToInt((1 - targetUnit.HealthSystem().CurrentHealthNormalized()) * 100f);
            finalActionValue += Mathf.RoundToInt(10 * (1 + heightDifferenceFromEnemy));

            // If any enemy is too close, this Unit probably won't want to shoot
            if (nearestEnemyDistance < minShootDistance)
                finalActionValue = Mathf.RoundToInt(finalActionValue / 8f);
            // If the target is too close, the Unit can't shoot them
            else if (Vector3.Distance(LevelGrid.Instance.GetWorldPosition(unit.GridPosition()), LevelGrid.Instance.GetWorldPosition(targetUnit.GridPosition())) / LevelGrid.Instance.GridSize() < minShootDistance)
                finalActionValue = 0;
        }
        
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = finalActionValue
        };
    }

    public override string GetActionName() => "Shoot";

    public override int GetActionPointsCost()
    {
        // TODO: Different types of ranged weapons cost different amounts
        return 30;
    }

    public override bool ActionIsUsedInstantly() => false;

    public override bool IsValidAction()
    {
        HeldItem leftHeldItem = unit.LeftHeldItem();
        if (leftHeldItem == null || leftHeldItem is RangedWeapon == false)
            return false;
        else
        {
            RangedWeapon rangedWeapon = (RangedWeapon)leftHeldItem;
            if (rangedWeapon.IsLoaded())
                return true;
            else
                return false;
        }
    }
}
