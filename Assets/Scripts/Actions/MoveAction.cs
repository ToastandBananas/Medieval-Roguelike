using System.Collections.Generic;
using System;
using UnityEngine;
using Pathfinding;
using System.Collections;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    [SerializeField] float moveCostMultiplier = 1f;

    Seeker seeker;

    List<Vector3> positionList;

    protected override void Awake()
    {
        base.Awake();

        seeker = GetComponent<Seeker>();
    }

    IEnumerator Move()
    {
        Vector3 finalTargetPosition = positionList[positionList.Count - 1];
        bool increaseCurrentPositionIndex = false;
        float stoppingDistance = 0.0125f;
        int currentPositionIndex = 0;

        while (Vector3.Distance(unit.transform.localPosition, finalTargetPosition) > stoppingDistance)
        {
            Vector3 unitPosition = unit.transform.localPosition;
            Vector3 targetPosition = unitPosition;

            if (Mathf.Abs(positionList[currentPositionIndex].y) - Mathf.Abs(unitPosition.y) > stoppingDistance)
            {
                if (positionList[currentPositionIndex].y - unitPosition.y > 0f) // If the next path position is above the unit's current position
                    targetPosition = new Vector3(unitPosition.x, positionList[currentPositionIndex].y, unitPosition.z);
                else if (positionList[currentPositionIndex].y - unitPosition.y < 0f && positionList[currentPositionIndex].x != unitPosition.x && positionList[currentPositionIndex].z != unitPosition.z) // If the next path position is below the unit's current position
                    targetPosition = new Vector3(positionList[currentPositionIndex].x, unitPosition.y, positionList[currentPositionIndex].z);
            }
            else
            {
                targetPosition = positionList[currentPositionIndex];
                increaseCurrentPositionIndex = true;
            }

            Vector3 moveDirection = (targetPosition - unitPosition).normalized;

            RotateTowardsTargetPosition(positionList[currentPositionIndex]);

            float distanceToTargetPosition = Vector3.Distance(unitPosition, targetPosition);
            if (distanceToTargetPosition > stoppingDistance)
            {
                float distanceToFinalTargetPosition = Vector3.Distance(unitPosition, finalTargetPosition);
                float distanceToTriggerStopAnimation = 1f;
                if (distanceToFinalTargetPosition <= distanceToTriggerStopAnimation)
                    OnStopMoving?.Invoke(this, EventArgs.Empty);

                float moveSpeed = 4f;
                unit.transform.localPosition += moveDirection * moveSpeed * Time.deltaTime;
            }
            else if (increaseCurrentPositionIndex && currentPositionIndex < positionList.Count - 1)
                currentPositionIndex++;

            yield return null;
        }

        StartCoroutine(RotateTowardsFinalPosition(finalTargetPosition));

        transform.position = finalTargetPosition;
        unit.UpdateGridPosition();

        CompleteAction();

        if (unit.IsPlayer())
            GridSystemVisual.Instance.UpdateGridVisual();
    }

    void RotateTowardsTargetPosition(Vector3 targetPosition)
    {
        float rotateSpeed = 10f;
        Vector3 lookPos = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    IEnumerator RotateTowardsFinalPosition(Vector3 finalTargetPosition)
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        float headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;

        float rotateSpeed = 10f;
        Vector3 lookPos = (new Vector3(finalTargetPosition.x, transform.position.y, finalTargetPosition.z) - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        
        while (Mathf.Abs(targetRotation.eulerAngles.y) - Mathf.Abs(headingAngle) > 0.25f || Mathf.Abs(targetRotation.eulerAngles.y) - Mathf.Abs(headingAngle) < -0.25f)
        {
            forward = transform.forward;
            forward.y = 0;
            headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        unit.GetAction<TurnAction>().SetCurrentDirection();
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (this is MoveAction && unit.IsPlayer() == false)
            unit.UnblockCurrentPosition();

        ABPath path = ABPath.Construct(unit.transform.position, LevelGrid.Instance.GetWorldPosition(gridPosition));
        path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

        // Schedule the path for calculation
        seeker.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        positionList = new List<Vector3>();

        for (int i = 0; i < path.vectorPath.Count; i++)
        {
            positionList.Add(path.vectorPath[i]);
        }

        OnStartMoving?.Invoke(this, EventArgs.Empty);

        if (unit.IsPlayer())
            GridSystemVisual.Instance.HideAllGridPositions();

        StartAction(onActionComplete);

        StartCoroutine(Move());
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        ConstantPath path = ConstantPath.Construct(unit.transform.position, Mathf.RoundToInt(unit.ActionPoints() / (float)GetActionPointsCost() * 1000f) + 100);
        path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

        // Schedule the path for calculation
        seeker.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition gridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (LevelGrid.Instance.IsValidGridPosition(gridPosition) == false)
                continue;

            Collider[] collisions = Physics.OverlapSphere(gridPosition.WorldPosition() + new Vector3(0f, 0.025f, 0f), 0.01f, UnitActionSystem.Instance.ActionObstaclesMask());
            if (collisions.Length > 0)
                continue;

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition)) // Grid Position already occupied by another Unit
                continue;

            // Debug.Log(gridPosition);
            validGridPositionList.Add(gridPosition);
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        ShootAction shootAction = unit.GetAction<ShootAction>();
        int targetCountAtGridPosition = shootAction.GetTargetCountAtPosition(gridPosition);
        int finalActionValue = 1 + (targetCountAtGridPosition * 10);

        if (unit.IsRangedUnit())
        {
            Unit nearestEnemyUnitFromGridPosition = null;

            float nearestEnemyDistanceFromGridPosition = float.MaxValue;
            foreach (Unit testUnit in UnitManager.Instance.UnitsList())
            {
                if (testUnit.IsEnemy(unit.GetCurrentFaction()))
                {
                    float distanceToEnemyFromGridPosition = Vector3.Distance(LevelGrid.Instance.GetWorldPosition(gridPosition), LevelGrid.Instance.GetWorldPosition(testUnit.GridPosition()) / LevelGrid.Instance.GridSize());
                    if (distanceToEnemyFromGridPosition < nearestEnemyDistanceFromGridPosition)
                    {
                        nearestEnemyDistanceFromGridPosition = distanceToEnemyFromGridPosition;
                        nearestEnemyUnitFromGridPosition = testUnit;
                    }
                }
            }

            float nearestEnemyDistanceFromThisUnit = float.MaxValue;
            foreach (Unit testUnit in UnitManager.Instance.UnitsList())
            {
                if (testUnit.IsEnemy(unit.GetCurrentFaction()))
                {
                    float distanceToEnemyFromThisUnit = Vector3.Distance(LevelGrid.Instance.GetWorldPosition(unit.GridPosition()), LevelGrid.Instance.GetWorldPosition(testUnit.GridPosition()) / LevelGrid.Instance.GridSize());
                    if (distanceToEnemyFromThisUnit < nearestEnemyDistanceFromThisUnit)
                        nearestEnemyDistanceFromThisUnit = distanceToEnemyFromThisUnit;
                }
            }

            float gridPositionHeightDifferenceFromNearestEnemy = 0;
            if (nearestEnemyUnitFromGridPosition != null)
                gridPositionHeightDifferenceFromNearestEnemy = gridPosition.y - nearestEnemyUnitFromGridPosition.GridPosition().y;

            finalActionValue += Mathf.RoundToInt(10 * (1 + gridPositionHeightDifferenceFromNearestEnemy));

            if (nearestEnemyDistanceFromThisUnit < shootAction.MinShootDistance()) // If there's an enemy Unit too close to this Unit
                finalActionValue += Mathf.RoundToInt(finalActionValue * nearestEnemyDistanceFromGridPosition);
            else if (nearestEnemyDistanceFromGridPosition < shootAction.MinShootDistance()) // If there's an enemy Unit too close to this Grid Position
                finalActionValue = 0;
            else if (shootAction.GetTargetCountAtPosition(unit.GridPosition()) > 0) // If the Unit already has a target visible within the proper range
                finalActionValue = 0;
            else if (targetCountAtGridPosition > 0)
                finalActionValue += (shootAction.PreferredShootDistance() * 10) - Mathf.RoundToInt(Mathf.Abs(nearestEnemyDistanceFromGridPosition - shootAction.PreferredShootDistance()) * 10f);
        }

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = finalActionValue
        };
    }

    public Seeker Seeker() => seeker;

    public override void CompleteAction()
    {
        base.CompleteAction();

        if (unit.IsPlayer() == false)
            unit.BlockCurrentPosition();
    }

    public override string GetActionName() => "Move";

    public override int GetActionPointsCost()
    {
        // TODO: Cost 10 per square (or more depending on terrain type)
        return 10;
    }

    public int GetActionPointsCost(float moveDistance)
    {
        return Mathf.RoundToInt(GetActionPointsCost() * moveCostMultiplier * moveDistance);
    }

    public override bool ActionIsUsedInstantly() => false;

    public override bool IsValidAction()
    {
        // TODO: Test if the unit is immobile for whatever reason (broken legs, some sort of spell effect, etc.)
        return true;
    }
}
