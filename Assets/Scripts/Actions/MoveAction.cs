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
    Vector3 nextTargetPosition;

    public bool isMoving { get; private set; }

    [SerializeField] LayerMask moveObstaclesMask;

    List<Vector3> positionList;
    int positionIndex;

    readonly float defaultMoveSpeed = 4.5f;
    float moveSpeed;

    readonly int defaultTileMoveCost = 200;
    int lastMoveCost;

    public override void Awake()
    {
        base.Awake();

        seeker = GetComponent<Seeker>();

        positionList = new List<Vector3>();
    }

    void Start() => SetMoveSpeed();

    public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete)
    {
        if (isMoving) return;

        StartAction(onActionComplete);
        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        if (positionList.Count == 0)
        {
            Debug.Log(unit.name + "'s position List length is 0");

            CompleteAction();
            unit.unitActionHandler.FinishAction();

            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));

            yield break;
        }

        isMoving = true;

        if (finalTargetGridPosition == unit.gridPosition)
        {
            //if (unit.IsPlayer()) Debug.Log(unit.name + "'s next position is the same as the their current position...");

            unit.stats.AddToCurrentAP(lastMoveCost);
            CompleteAction();
            unit.unitActionHandler.FinishAction();

            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));

            yield break;
        }

        if (LevelGrid.Instance.GridPositionObstructed(nextTargetGridPosition))
        {
            // Get a new path to the target position because the previous path is obstructed
            GetPathToTargetPosition(finalTargetGridPosition);
            nextTargetPosition = GetNextTargetPosition();
            nextTargetGridPosition = LevelGrid.Instance.GetGridPosition(nextTargetPosition);
        }

        if (nextTargetPosition == unit.WorldPosition() || LevelGrid.Instance.GridPositionObstructed(LevelGrid.Instance.GetGridPosition(nextTargetPosition)))
        {
            //if (unit.IsPlayer()) Debug.Log(unit.name + "'s next position is not walkable or is the same as the their current position...");

            unit.stats.AddToCurrentAP(lastMoveCost);
            CompleteAction();
            unit.unitActionHandler.FinishAction();

            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));

            yield break;
        }

        // Block the Next Position so that NPCs who are also currently looking for a path don't try to use the Next Position's tile
        unit.BlockAtPosition(nextTargetPosition);

        // Remove the Unit from the Units list
        LevelGrid.Instance.RemoveUnitAtGridPosition(unit.gridPosition);

        // Add the Unit to its next position
        LevelGrid.Instance.AddUnitAtGridPosition(LevelGrid.Instance.GetGridPosition(nextTargetPosition), unit);

        // Start the next Unit's action before moving, that way their actions play out at the same time as this Unit's
        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));

        ActionLineRenderer.Instance.HideLineRenderers();

        Vector3 nextPointOnPath = positionList[positionIndex];
        Vector3 nextPathPosition = unit.transform.position;
        Direction directionToNextPosition;

        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);

            if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1);

            float stoppingDistance = 0.0125f;
            while (Vector3.Distance(unit.transform.position, nextPathPosition) > stoppingDistance)
            {
                unit.unitAnimator.StartMovingForward(); // Move animation

                Vector3 unitPosition = unit.transform.position;
                Vector3 targetPosition = unitPosition;

                // If the next point on the path is above or below the Unit
                if (Mathf.Abs(Mathf.Abs(nextPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
                {
                    // If the next path position is above the unit's current position
                    if (nextPointOnPath.y - unitPosition.y > 0f)
                    {
                        targetPosition = new Vector3(unitPosition.x, nextPointOnPath.y, unitPosition.z);
                        nextPathPosition = new Vector3(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
                    }
                    // If the Unit is directly above the next path position
                    else if (nextPointOnPath.y - unitPosition.y < 0f && Mathf.Abs(nextPathPosition.x - unitPosition.x) < stoppingDistance && Mathf.Abs(nextPathPosition.z - unitPosition.z) < stoppingDistance)
                    {
                        targetPosition = nextPathPosition;
                    }
                    // If the next path position is below the unit's current position
                    else if (nextPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPathPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPathPosition.z, unitPosition.z) == false))
                    {
                        targetPosition = new Vector3(nextPointOnPath.x, unitPosition.y, nextPointOnPath.z);
                        nextPathPosition = new Vector3(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
                    }
                }
                else
                    targetPosition = nextPathPosition;

                RotateTowardsTargetPosition(nextPathPosition);

                Vector3 moveDirection = (targetPosition - unitPosition).normalized;
                float distanceToTargetPosition = Vector3.Distance(unitPosition, targetPosition);
                if (distanceToTargetPosition > stoppingDistance)
                {
                    float distanceToFinalPosition = Vector3.Distance(unitPosition, LevelGrid.Instance.GetWorldPosition(finalTargetGridPosition));
                    float distanceToTriggerStopAnimation = 1f;
                    if (distanceToFinalPosition <= distanceToTriggerStopAnimation)
                        unit.unitAnimator.StopMovingForward();
                    
                    unit.transform.position += moveDirection * moveSpeed * Time.deltaTime;
                }

                yield return null;
            }

            unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(directionToNextPosition, unit.transform.position, false);
        }
        else // Move and rotate instantly while NPC is offscreen
        {
            directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);
            unit.unitActionHandler.GetAction<TurnAction>().RotateTowardsDirection(directionToNextPosition, unit.transform.position, true);

            nextPathPosition = nextTargetPosition;
            unit.UpdateGridPosition();

            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
        }

        nextPathPosition = new Vector3(Mathf.RoundToInt(nextPathPosition.x), nextPathPosition.y, Mathf.RoundToInt(nextPathPosition.z));
        unit.transform.position = nextPathPosition;

        if (unit.transform.position == positionList[positionIndex] && unit.transform.position != finalTargetGridPosition.WorldPosition())
            positionIndex++;

        //if (nextPathPosition != nextTargetPosition && unit.IsNPC())
        //    Debug.LogWarning("Target and Next Target positions are not equal..." + nextPathPosition + " / " + nextTargetPosition);

        CompleteAction();
        unit.unitActionHandler.FinishAction();

        if (unit.IsPlayer()) 
        {
            GridSystemVisual.Instance.UpdateGridVisual();

            // If the Player hasn't reached their destination, add the next move to the queue
            if (unit.gridPosition != finalTargetGridPosition)
                unit.unitActionHandler.QueueAction(this, GetActionPointsCost(finalTargetGridPosition));
        }
    }

    void GetPathToTargetPosition(GridPosition targetGridPosition)
    {
        SetFinalTargetGridPosition(targetGridPosition);

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
            if (unit.stateController.currentState == State.Patrol)
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
        positionIndex = 1;

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

        if (positionIndex >= positionList.Count)
            positionIndex = positionList.Count - 1;

        // Only calculate a new path if the Unit's target position changed or if their path becomes obstructed
        if (targetGridPosition != finalTargetGridPosition || (positionList.Count > 0 && LevelGrid.Instance.GridPositionObstructed(LevelGrid.Instance.GetGridPosition(positionList[positionIndex]))))
            GetPathToTargetPosition(targetGridPosition);

        if (positionList.Count == 0)
            return cost;

        // Get the next Move position
        nextTargetPosition = GetNextTargetPosition();
        nextTargetGridPosition = LevelGrid.Instance.GetGridPosition(nextTargetPosition);

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
                if (unit.stateController.currentState == State.Patrol)
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
        Vector3 nextPointOnPath = positionList[positionIndex];
        Vector3 nextTargetPosition;
        if (Mathf.Approximately(nextPointOnPath.y, unit.transform.position.y) == false)
            nextTargetPosition = nextPointOnPath; 
        else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // North
            nextTargetPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // South
            nextTargetPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z)) // East
            nextTargetPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z)) // West
            nextTargetPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // NorthEast
            nextTargetPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // SouthWest
            nextTargetPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // SouthEast
            nextTargetPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1); 
        else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // NorthWest
            nextTargetPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1); 
        else // Debug.LogWarning("Next Position is " + unit.name + "'s current position...");
            nextTargetPosition = transform.position;
        nextTargetPosition = new Vector3(Mathf.RoundToInt(nextTargetPosition.x), nextTargetPosition.y, Mathf.RoundToInt(nextTargetPosition.z));
        return nextTargetPosition;
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

    float GetMoveSpeed()
    {
        int unitsMaxAP = unit.stats.MaxAP();
        int playersMaxAP = UnitManager.Instance.player.stats.MaxAP();
        float moveSpeed;
        if (unit.IsPlayer() || unitsMaxAP <= playersMaxAP)
            return defaultMoveSpeed;
        else
            moveSpeed = defaultMoveSpeed + ((unitsMaxAP / playersMaxAP) - 1f); // Example: (150 / 100) = 1.5 | 1.5 - 1 = 0.5 add-on

        if (moveSpeed > 8f)
            return 8f;
        else
            return moveSpeed;
    }

    public void SetMoveSpeed() => moveSpeed = GetMoveSpeed();

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
    }

    public override bool ActionIsUsedInstantly() => false;

    public LayerMask MoveObstaclesMask() => moveObstaclesMask;
}
