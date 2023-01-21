using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveAction : BaseAction
{
    public Seeker seeker { get; private set; }
    public GridPosition targetGridPosition { get; private set; }

    public bool isMoving { get; private set; }
    bool moveQueued;

    [SerializeField] LayerMask moveObstaclesMask;

    List<Vector3> positionList;

    readonly int defaultTileMoveCost = 200;
    int lastMoveCost;

    public override void Awake()
    {
        base.Awake();

        seeker = GetComponent<Seeker>();

        positionList = new List<Vector3>();
    }

    public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete)
    {
          ////////////////////////////////////////////////////////////////
         // Path is also calculated in the GetActionsPointsCost method //
        ////////////////////////////////////////////////////////////////
        //Debug.Log(Time.frameCount);

        GetPathToTargetPosition(targetGridPosition);

        //OnStartMoving?.Invoke(this, EventArgs.Empty);

        if (unit.IsPlayer())
            GridSystemVisual.Instance.HideAllGridPositions();

        StartAction(onActionComplete);

        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        if (positionList.Count == 0)
        {
            if (unit.IsPlayer()) Debug.Log("Position List length is 0");

            CompleteAction();
            unit.unitActionHandler.FinishAction();

            yield break;
        }
        
        Vector3 firstPointOnPath = positionList[1];
        Vector3 nextPathPosition = unit.transform.localPosition;
        float stoppingDistance = 0.0125f;
        Direction directionToNextPosition = Direction.Center;

        if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);
            directionToNextPosition = Direction.North;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);
            directionToNextPosition = Direction.South;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z);
            directionToNextPosition = Direction.East;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z);
            directionToNextPosition = Direction.West;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);
            directionToNextPosition = Direction.NorthEast;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);
            directionToNextPosition = Direction.SouthWest;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);
            directionToNextPosition = Direction.SouthEast;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            nextPathPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);
            directionToNextPosition = Direction.NorthWest;
        }

        // Check if the next position is now blocked, before moving the Unit there
        Vector3 nextTargetPosition = GetNextTargetPosition();
        if (nextTargetPosition == unit.WorldPosition() || LevelGrid.Instance.HasAnyUnitOnGridPosition(LevelGrid.Instance.GetGridPosition(nextTargetPosition)))
        {
            if (unit.IsPlayer()) Debug.Log(unit.name + "'s next position is not walkable...");

            unit.stats.AddToCurrentAP(lastMoveCost);
            CompleteAction();
            unit.unitActionHandler.FinishAction();

            yield break;
        }

        // Block the Next Position so that NPCs who are also currently looking for a path don't try to use the Next Position's tile
        unit.BlockAtPosition(nextTargetPosition);

        // Remove the Unit from the Units list
        LevelGrid.Instance.RemoveUnitAtGridPosition(unit.gridPosition);

        // Add the Unit to it's next position
        LevelGrid.Instance.AddUnitAtGridPosition(LevelGrid.Instance.GetGridPosition(nextTargetPosition), unit);

        if (unit.IsNPC())
            StartCoroutine(TurnManager.Instance.StartNextNPCsAction(unit));

        ActionLineRenderer.Instance.HideLineRenderers();

        while (Vector3.Distance(unit.transform.localPosition, nextPathPosition) > stoppingDistance)
        {
            isMoving = true;
            unit.unitAnimator.StartMovingForward();

            Vector3 unitPosition = unit.transform.localPosition;
            Vector3 targetPosition = unitPosition;

            if (Mathf.Abs(Mathf.Abs(firstPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
            {
                // If the next path position is above the unit's current position
                if (firstPointOnPath.y - unitPosition.y > 0f)
                {
                    targetPosition = new Vector3(unitPosition.x, firstPointOnPath.y, unitPosition.z);
                    nextPathPosition = new Vector3(nextPathPosition.x, firstPointOnPath.y, nextPathPosition.z);
                }
                else if (firstPointOnPath.y - unitPosition.y < 0f && Mathf.Abs(nextPathPosition.x - unitPosition.x) < stoppingDistance && Mathf.Abs(nextPathPosition.z - unitPosition.z) < stoppingDistance)
                {
                    targetPosition = nextPathPosition;
                }
                // If the next path position is below the unit's current position
                else if (firstPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPathPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPathPosition.z, unitPosition.z) == false))
                {
                    targetPosition = new Vector3(firstPointOnPath.x, unitPosition.y, firstPointOnPath.z);
                    nextPathPosition = new Vector3(nextPathPosition.x, firstPointOnPath.y, nextPathPosition.z);
                }
            }
            else
                targetPosition = nextPathPosition;

            RotateTowardsTargetPosition(nextPathPosition);

            Vector3 moveDirection = (targetPosition - unitPosition).normalized;
            float distanceToTargetPosition = Vector3.Distance(unitPosition, targetPosition);
            if (distanceToTargetPosition > stoppingDistance)
            {
                float distanceToNextPosition = Vector3.Distance(unitPosition, LevelGrid.Instance.GetWorldPosition(targetGridPosition));
                float distanceToTriggerStopAnimation = 1f;
                if (distanceToNextPosition <= distanceToTriggerStopAnimation)
                    unit.unitAnimator.StopMovingForward();

                float moveSpeed = 4f;
                unit.transform.localPosition += moveDirection * moveSpeed * Time.deltaTime;
            }

            yield return null;
        }

        unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(directionToNextPosition);

        unit.transform.localPosition = nextPathPosition;

        if (nextPathPosition != nextTargetPosition && unit.IsNPC())
            Debug.Log("Target and Next Target positions are not equal..." + nextPathPosition + " / " + nextTargetPosition);

        CompleteAction();
        unit.unitActionHandler.FinishAction();

        if (unit.IsPlayer()) 
        {
            GridSystemVisual.Instance.UpdateGridVisual();

            if (unit.gridPosition != targetGridPosition)
                unit.unitActionHandler.QueueAction(this, GetActionPointsCost(targetGridPosition));
        }
    }

    void GetPathToTargetPosition(GridPosition targetGridPosition)
    {
        this.targetGridPosition = targetGridPosition;

        unit.UnblockCurrentPosition();

        ABPath path = ABPath.Construct(unit.transform.position, LevelGrid.Instance.GetWorldPosition(targetGridPosition));
        path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

        // Schedule the path for calculation
        seeker.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        if (unit.IsNPC() && path.vectorPath.Count == 0)
        {
            NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
            if (unit.stateController.CurrentState() == State.Patrol)
            {
                GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(npcActionHandler.PatrolPoints()[npcActionHandler.currentPatrolPointIndex]);
                npcActionHandler.IncreasePatrolPointIndex();
                npcActionHandler.SetTargetGridPosition(patrolPointGridPosition);
                SetTargetGridPosition(patrolPointGridPosition);
            }

            unit.unitActionHandler.FinishAction();
            return;
        }

        positionList.Clear();

        for (int i = 0; i < path.vectorPath.Count; i++)
        {
            positionList.Add(path.vectorPath[i]);
        }
    }

    void RotateTowardsTargetPosition(Vector3 targetPosition)
    {
        float rotateSpeed = 10f;
        Vector3 lookPos = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public override string GetActionName() => "Move";

    public override bool IsValidAction()
    {
        // TODO: Test if the unit is immobile for whatever reason (broken legs, some sort of spell effect, etc.)
        return true;
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        // TODO: Cost 600 (6 seconds) per square (or more depending on terrain type)
        int cost = defaultTileMoveCost;
        float floatCost = cost;

        GetPathToTargetPosition(targetGridPosition);

        if (positionList.Count == 0)
            return cost;

        Vector3 nextTargetPosition = GetNextTargetPosition();

        float tileCostMultiplier = GetTileMoveCostMultiplier(nextTargetPosition);
        // if (unit.IsPlayer()) Debug.Log(tileCostMultiplier);

        floatCost += floatCost * tileCostMultiplier;

        if (LevelGrid.IsDiagonal(unit.WorldPosition(), nextTargetPosition))
            floatCost *= 1.4f;

        cost = Mathf.RoundToInt(floatCost);

        if (nextTargetPosition == transform.position)
        {
            unit.unitActionHandler.SetTargetGridPosition(unit.gridPosition);

            if (unit.IsNPC())
            {
                if (unit.stateController.CurrentState() == State.Patrol)
                {
                    NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                    npcActionHandler.AssignNextPatrolTargetPosition();
                }
            }

            unit.stats.AddToCurrentAP(cost);
            CompleteAction();
            unit.unitActionHandler.FinishAction();
        }

        unit.BlockCurrentPosition();

        // if (unit.IsPlayer()) Debug.Log("Move Cost (" + nextPosition + "): " + cost);

        lastMoveCost = cost;
        return cost;
    }

    Vector3 GetNextTargetPosition()
    {
        Vector3 nextPosition;
        Vector3 firstPointOnPath = positionList[1];
        if (Mathf.Approximately(firstPointOnPath.y, unit.transform.position.y) == false)
        {
            nextPosition = firstPointOnPath;
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // North
            nextPosition = new Vector3(unit.transform.localPosition.x, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // South
            nextPosition = new Vector3(unit.transform.localPosition.x, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // East
            nextPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // West
            nextPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // NorthEast
            nextPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // SouthWest
            nextPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // SouthEast
            nextPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);
        }
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.localPosition.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.localPosition.z))
        {
            // NorthWest
            nextPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);
        }
        else
        {
            Debug.LogWarning("Next Position is " + unit.name + "'s current position...");
            nextPosition = transform.position;
        }

        return nextPosition;
    }

    float GetTileMoveCostMultiplier(Vector3 tilePosition)
    {
        GraphNode node = AstarPath.active.GetNearest(tilePosition).node;
        // if (unit.IsPlayer()) Debug.Log("Tag #" + node.Tag + " penalty is: ");

        for (int i = 0; i < seeker.tagPenalties.Length; i++)
        {
            if (node.Tag == i)
            {
                if (seeker.tagPenalties[i] == 0)
                    return 0f;
                else
                    return seeker.tagPenalties[i] / 1000f;
            }
        }

        return 1f;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();

        // Unblock the Unit's postion, in case it's still their turn after this action ( so that the ActionLineRenderer will work). If not, it will be blocked again in the TurnManager's finish turn methods
        if (unit.IsPlayer())
            unit.UnblockCurrentPosition();
        else
            unit.BlockCurrentPosition();

        unit.UpdateGridPosition();

        isMoving = false;
        positionList.Clear();
    }

    public override bool ActionIsUsedInstantly() => false;

    public LayerMask MoveObstaclesMask() => moveObstaclesMask;
}
