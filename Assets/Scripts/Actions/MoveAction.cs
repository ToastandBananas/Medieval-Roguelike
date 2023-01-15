using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    [Header("Flee State Variables")]
    public LayerMask fleeObstacleMask;
    public float fleeDistance = 20f;
    public bool shouldAlwaysFleeCombat;

    [Header("Follow State Variables")]
    public float startFollowingDistance = 3f;
    public float slowDownDistance = 4f;
    public bool shouldFollowLeader;

    [Header("Patrol State Variables")]
    public Vector2[] patrolPoints;

    [Header("Pursue State Variables")]
    public float maxChaseDistance = 15f;

    [Header("Wandering State Variables")]
    public Vector2 defaultPosition;
    public float minRoamDistance = 5f;
    public float maxRoamDistance = 20f;

    Unit unit;
    Seeker seeker;
    
    int currentPatrolPointIndex;
    bool initialPatrolPointSet;

    GridPosition targetGridPosition;
    Vector2 roamPosition;
    Vector3 fleeDestination;
    float distToFleeDestination = 0;

    bool isMoving, moveQueued;
    bool roamPositionSet;
    bool needsFleeDestination = true;

    Quaternion targetRotation;

    List<Vector3> positionList;

    void Awake()
    {
        unit = GetComponent<Unit>();
        seeker = GetComponent<Seeker>();
    }

    public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete)
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

        positionList = new List<Vector3>();

        for (int i = 0; i < path.vectorPath.Count; i++)
        {
            positionList.Add(path.vectorPath[i]);
        }

        //OnStartMoving?.Invoke(this, EventArgs.Empty);

        if (unit.IsPlayer())
            GridSystemVisual.Instance.HideAllGridPositions();

        StartAction(onActionComplete);

        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        Vector3 finalTargetPosition = positionList[positionList.Count - 1];
        Vector3 firstPointOnPath = positionList[1];
        Vector3 nextPosition = unit.transform.localPosition;
        bool increaseCurrentPositionIndex = false;
        float stoppingDistance = 0.0125f;
        int currentPositionIndex = 0;

        Debug.Log("First Point on Path: " + firstPointOnPath);
        Debug.Log("Unit Position: " + unit.transform.localPosition);
        if ((int)firstPointOnPath.x == (int)unit.transform.localPosition.x && (int)firstPointOnPath.z > (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);

        else if ((int)firstPointOnPath.x == (int)unit.transform.localPosition.x && (int)firstPointOnPath.z < (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);

        else if ((int)firstPointOnPath.x > (int)unit.transform.localPosition.x && (int)firstPointOnPath.z == (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z);

        else if ((int)firstPointOnPath.x < (int)unit.transform.localPosition.x && (int)firstPointOnPath.z == (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z);

        else if ((int)firstPointOnPath.x > (int)unit.transform.localPosition.x && (int)firstPointOnPath.z > (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);

        else if ((int)firstPointOnPath.x < (int)unit.transform.localPosition.x && (int)firstPointOnPath.z < (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);

        else if ((int)firstPointOnPath.x > (int)unit.transform.localPosition.x && (int)firstPointOnPath.z < (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x + 1, unit.transform.localPosition.y, unit.transform.localPosition.z - 1);

        else if ((int)firstPointOnPath.x < (int)unit.transform.localPosition.x && (int)firstPointOnPath.z > (int)unit.transform.localPosition.z)
            nextPosition = new Vector3(unit.transform.localPosition.x - 1, unit.transform.localPosition.y, unit.transform.localPosition.z + 1);

        Debug.Log("Next Position: " + nextPosition);

        while (Vector3.Distance(unit.transform.localPosition, nextPosition) > stoppingDistance)
        {
            isMoving = true;
            ActionLineRenderer.Instance.HideLineRenderers();

            Vector3 unitPosition = unit.transform.localPosition;
            Vector3 targetPosition = unitPosition;

            if (Mathf.Abs(positionList[currentPositionIndex].y) - Mathf.Abs(unitPosition.y) > stoppingDistance)
            {
                if (positionList[currentPositionIndex].y - unitPosition.y > 0f) // If the next path position is above the unit's current position
                {
                    targetPosition = new Vector3(unitPosition.x, positionList[currentPositionIndex].y, unitPosition.z);
                    nextPosition = new Vector3(nextPosition.x, nextPosition.y + 1, nextPosition.z);
                }
                else if (positionList[currentPositionIndex].y - unitPosition.y < 0f && positionList[currentPositionIndex].x != unitPosition.x && positionList[currentPositionIndex].z != unitPosition.z) // If the next path position is below the unit's current position
                {
                    targetPosition = new Vector3(positionList[currentPositionIndex].x, unitPosition.y, positionList[currentPositionIndex].z);
                    nextPosition = new Vector3(nextPosition.x, nextPosition.y - 1, nextPosition.z);
                }
            }
            else
            {
                targetPosition = nextPosition;
                increaseCurrentPositionIndex = true;
            }

            Vector3 moveDirection = (targetPosition - unitPosition).normalized;

            RotateTowardsTargetPosition(nextPosition);

            float distanceToTargetPosition = Vector3.Distance(unitPosition, targetPosition);
            if (distanceToTargetPosition > stoppingDistance)
            {
                float distanceToNextPosition = Vector3.Distance(unitPosition, targetPosition);
                float distanceToTriggerStopAnimation = 1f;
                //if (distanceToNextPosition <= distanceToTriggerStopAnimation)
                //OnStopMoving?.Invoke(this, EventArgs.Empty);

                float moveSpeed = 4f;
                unit.transform.localPosition += moveDirection * moveSpeed * Time.deltaTime;
            }
            else if (increaseCurrentPositionIndex && currentPositionIndex < positionList.Count - 1)
                currentPositionIndex++;

            yield return null;
        }

        SetTargetRotation(nextPosition);
        StartCoroutine(RotateTowardsFinalPosition());

        unit.transform.localPosition = nextPosition;
        unit.UpdateGridPosition();

        isMoving = false;
        CompleteAction();
        unit.UnitActionHandler().FinishAction();

        if (unit.IsPlayer())
            GridSystemVisual.Instance.UpdateGridVisual();

        if (unit.GridPosition() != targetGridPosition)
            unit.UnitActionHandler().QueueAction(this, 25);
    }

    void RotateTowardsTargetPosition(Vector3 targetPosition)
    {
        float rotateSpeed = 10f;
        Vector3 lookPos = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    void SetTargetRotation(Vector3 finalTargetPosition)
    {
        Vector3 lookPos = (new Vector3(finalTargetPosition.x, transform.position.y, finalTargetPosition.z) - transform.position).normalized;
        targetRotation = Quaternion.LookRotation(lookPos);
    }

    IEnumerator RotateTowardsFinalPosition()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        float headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;

        float rotateSpeed = 10f;
        Quaternion currentTargetRotation = targetRotation;

        while (currentTargetRotation == targetRotation && (Mathf.Abs(targetRotation.eulerAngles.y) - Mathf.Abs(headingAngle) > 0.25f || Mathf.Abs(targetRotation.eulerAngles.y) - Mathf.Abs(headingAngle) < -0.25f))
        {
            forward = transform.forward;
            forward.y = 0;
            headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        //unit.GetAction<TurnAction>().SetCurrentDirection();
    }

    public bool IsMoving() => isMoving;

    public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

    public GridPosition TargetGridPosition() => targetGridPosition;

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition; 
    
    public void ResetToDefaults()
    {
        roamPositionSet = false;
        needsFleeDestination = true;
        initialPatrolPointSet = false;

        fleeDestination = Vector3.zero;
        distToFleeDestination = 0;
    }

    public Seeker Seeker() => seeker;
}
