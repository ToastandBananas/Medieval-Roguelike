using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    public Seeker seeker { get; private set; }
    public GridPosition finalTargetGridPosition { get; private set; }
    public GridPosition nextTargetGridPosition { get; private set; }

    public bool isMoving { get; private set; }
    // bool moveQueued { get; private set; }

    [SerializeField] LayerMask moveObstaclesMask;

    List<Vector3> positionList;

    readonly float defaultMoveSpeed = 5f;
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
        // Debug.Log(Time.frameCount); // We can use this to make sure multiple moves are never ran in the same frame, to avoid Units trying to go on the same space

        GetPathToFinalTargetPosition(targetGridPosition);

        //OnStartMoving?.Invoke(this, EventArgs.Empty);

        if (unit.IsPlayer()) GridSystemVisual.Instance.HideAllGridPositions();

        StartAction(onActionComplete);

        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        if (positionList.Count == 0)
        {
            if (unit.IsPlayer()) Debug.Log("Player's position List length is 0");

            CompleteAction();
            unit.unitActionHandler.FinishAction();

            yield break;
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

        nextTargetGridPosition = LevelGrid.Instance.GetGridPosition(nextTargetPosition);

        // Block the Next Position so that NPCs who are also currently looking for a path don't try to use the Next Position's tile
        unit.BlockAtPosition(nextTargetPosition);

        // Remove the Unit from the Units list
        LevelGrid.Instance.RemoveUnitAtGridPosition(unit.gridPosition);

        // Add the Unit to its next position
        LevelGrid.Instance.AddUnitAtGridPosition(LevelGrid.Instance.GetGridPosition(nextTargetPosition), unit);

        // Start the next NPCs action before moving, that way their actions play out at the same time as this Unit's
        if (unit.IsNPC()) TurnManager.Instance.StartNextNPCsAction(unit);

        ActionLineRenderer.Instance.HideLineRenderers();

        Vector3 firstPointOnPath = positionList[1];
        Vector3 nextPathPosition = unit.transform.position;
        Direction directionToNextPosition;

        if (unit.IsPlayer() || unit.unitBaseMeshRenderer.isVisible)
        {
            directionToNextPosition = GetDirectionToNextTargetPosition(firstPointOnPath);

            if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1);

            float stoppingDistance = 0.0125f;
            while (Vector3.Distance(unit.transform.position, nextPathPosition) > stoppingDistance)
            {
                isMoving = true;
                unit.unitAnimator.StartMovingForward(); // Move animation

                Vector3 unitPosition = unit.transform.position;
                Vector3 targetPosition = unitPosition;

                // If the first point on the path is above or below the Unit
                if (Mathf.Abs(Mathf.Abs(firstPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
                {
                    // If the next path position is above the unit's current position
                    if (firstPointOnPath.y - unitPosition.y > 0f)
                    {
                        targetPosition = new Vector3(unitPosition.x, firstPointOnPath.y, unitPosition.z);
                        nextPathPosition = new Vector3(nextPathPosition.x, firstPointOnPath.y, nextPathPosition.z);
                    }
                    // If the Unit is directly above the next path position
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
                    float distanceToNextPosition = Vector3.Distance(unitPosition, LevelGrid.Instance.GetWorldPosition(finalTargetGridPosition));
                    float distanceToTriggerStopAnimation = 1f;
                    if (distanceToNextPosition <= distanceToTriggerStopAnimation)
                        unit.unitAnimator.StopMovingForward();

                    unit.transform.position += moveDirection * defaultMoveSpeed * Time.deltaTime;
                }

                yield return null;
            }

            unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(directionToNextPosition, unit.transform.position, false);
        }
        else // Move and rotate instantly while NPC is offscreen
        {
            directionToNextPosition = GetDirectionToNextTargetPosition(firstPointOnPath);
            unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(directionToNextPosition, unit.transform.position, true);

            nextPathPosition = nextTargetPosition;
            unit.UpdateGridPosition();

            if (unit.IsNPC()) TurnManager.Instance.StartNextNPCsAction(unit);
        }

        unit.transform.position = nextPathPosition;

        if (nextPathPosition != nextTargetPosition && unit.IsNPC())
            Debug.LogWarning("Target and Next Target positions are not equal..." + nextPathPosition + " / " + nextTargetPosition);

        CompleteAction();
        unit.unitActionHandler.FinishAction();

        if (unit.IsPlayer()) 
        {
            GridSystemVisual.Instance.UpdateGridVisual();

            if (unit.gridPosition != finalTargetGridPosition)
                unit.unitActionHandler.QueueAction(this, GetActionPointsCost(finalTargetGridPosition));
        }
    }

    void GetPathToFinalTargetPosition(GridPosition finalTargetGridPosition)
    {
        SetFinalTargetGridPosition(finalTargetGridPosition);

        unit.UnblockCurrentPosition();

        ABPath path = ABPath.Construct(unit.transform.position, LevelGrid.Instance.GetWorldPosition(finalTargetGridPosition));
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
                SetFinalTargetGridPosition(patrolPointGridPosition);
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

    public void SetFinalTargetGridPosition(GridPosition finalTargetGridPosition) => this.finalTargetGridPosition = finalTargetGridPosition;

    public void SetNextTargetGridPosition(GridPosition nextTargetGridPosition) => this.nextTargetGridPosition = nextTargetGridPosition;

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

        GetPathToFinalTargetPosition(targetGridPosition);

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

        // if (unit.IsNPC()) Debug.Log("Move Cost (" + nextTargetPosition + "): " + cost);

        lastMoveCost = cost;
        return cost;
    }

    Vector3 GetNextTargetPosition()
    {
        Vector3 firstPointOnPath = positionList[1];
        if (Mathf.Approximately(firstPointOnPath.y, unit.transform.position.y) == false) 
            return firstPointOnPath; 
        else if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // North
            return new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // South
            return new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z)) // East
            return new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z)) // West
            return new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // NorthEast
            return new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // SouthWest
            return new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // SouthEast
            return new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1); 
        else if (Mathf.RoundToInt(firstPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(firstPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // NorthWest
            return new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1); 
        else // Debug.LogWarning("Next Position is " + unit.name + "'s current position...");
            return transform.position;
    }

    Direction GetDirectionToNextTargetPosition(Vector3 targetPosition)
    {
        if (Mathf.RoundToInt(targetPosition.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(unit.transform.position.z))
            return Direction.North;
        else if (Mathf.RoundToInt(targetPosition.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(unit.transform.position.z))
            return Direction.South;
        else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) == Mathf.RoundToInt(unit.transform.position.z))
            return Direction.East;
        else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) == Mathf.RoundToInt(unit.transform.position.z))
            return Direction.West;
        else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(unit.transform.position.z))
            return Direction.NorthEast;
        else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(unit.transform.position.z))
            return Direction.SouthWest;
        else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(unit.transform.position.z))
            return Direction.SouthEast;
        else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(unit.transform.position.z))
            return Direction.NorthWest;
        else
            return Direction.Center;
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
