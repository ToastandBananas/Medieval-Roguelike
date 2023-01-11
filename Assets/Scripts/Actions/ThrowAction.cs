using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ThrowAction : BaseAction
{
    [SerializeField] int minThrowDistance = 2;
    [SerializeField] int maxThrowDistance = 6;

    [SerializeField] LayerMask obstaclesMask;

    void Update()
    {
        if (isActive == false)
            return;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GridPosition();

        ConstantPath path = ConstantPath.Construct(unit.transform.position, 100000 + 1);

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

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unitGridPosition, nodeGridPosition);
            if (distance > maxThrowDistance || distance < minThrowDistance)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir = ((unitGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight())) - (nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight()))).normalized;
            if (Physics.SphereCast(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight()), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unitGridPosition.WorldPosition() + Vector3.up * unit.ShoulderHeight(), nodeGridPosition.WorldPosition() + Vector3.up * unit.ShoulderHeight()), obstaclesMask))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public override bool IsValidAction()
    {
        // TODO: Check if Unit has a bomb in their inventory
        return true;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        Projectile bomb = ProjectilePool.Instance.GetProjectileFromPool();
        bomb.Setup(ProjectilePool.Instance.Bomb_SO(), unit, ProjectilePool.Instance.transform, CompleteAction);
        bomb.transform.localPosition = unit.WorldPosition() + (Vector3.up * unit.ShoulderHeight());

        StartCoroutine(bomb.ShootProjectile_AtGridPosition(gridPosition, unit));

        StartAction(onActionComplete);
    }

    public override int GetActionPointsCost()
    {
        // TODO: Maybe have this differ based on bomb type
        return 50;
    }

    public int MinThrowDistance() => minThrowDistance;

    public int MaxThrowDistance() => maxThrowDistance;

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName()
    {
        // TODO: Change name based on type of bomb
        return "Throw Bomb";
    }
}
