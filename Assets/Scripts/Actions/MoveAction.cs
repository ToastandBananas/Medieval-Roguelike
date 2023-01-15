using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    [Header("Flee State Variables")]
    [SerializeField] LayerMask fleeObstacleMask;
    [SerializeField] float fleeDistance = 20f;
    [SerializeField] bool shouldAlwaysFleeCombat;
    Vector3 fleeDestination;
    float distToFleeDestination = 0;
    bool needsFleeDestination = true;

    [Header("Follow State Variables")]
    [SerializeField] float startFollowingDistance = 3f;
    [SerializeField] float slowDownDistance = 4f;
    [SerializeField] public bool shouldFollowLeader { get; private set; }

    [Header("Patrol State Variables")]
    [SerializeField] Vector2[] patrolPoints;
    int currentPatrolPointIndex;
    bool initialPatrolPointSet;

    [Header("Pursue State Variables")]
    [SerializeField] float maxChaseDistance = 15f;

    [Header("Wandering State Variables")]
    [SerializeField] Vector2 defaultPosition;
    [SerializeField] public float minRoamDistance = 5f;
    [SerializeField] public float maxRoamDistance = 20f;
    Vector2 roamPosition;
    bool roamPositionSet;

    Unit unit;

    public Seeker seeker { get; private set; }
    public GridPosition targetGridPosition { get; private set; }

    public bool isMoving { get; private set; }
    bool moveQueued;

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
        Vector3 firstPointOnPath = positionList[1];
        Vector3 nextPosition = unit.transform.localPosition;
        bool increaseCurrentPositionIndex = false;
        float stoppingDistance = 0.0125f;
        int currentPositionIndex = 0;

        Debug.Log("First Point on Path: " + firstPointOnPath);
        
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

        while (Vector3.Distance(unit.transform.localPosition, nextPosition) > stoppingDistance)
        {
            isMoving = true;
            ActionLineRenderer.Instance.HideLineRenderers();

            Vector3 unitPosition = unit.transform.localPosition;
            Vector3 targetPosition = unitPosition;

            if (Mathf.Abs(Mathf.Abs(firstPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
            {
                Debug.Log("Next Position: " + nextPosition);
                Debug.Log("Unit Position: " + unitPosition);
                // If the next path position is above the unit's current position
                if (firstPointOnPath.y - unitPosition.y > 0f)
                {
                    targetPosition = new Vector3(unitPosition.x, firstPointOnPath.y, unitPosition.z);
                    nextPosition = new Vector3(nextPosition.x, firstPointOnPath.y, nextPosition.z);
                }
                else if (firstPointOnPath.y - unitPosition.y < 0f && Mathf.Abs(nextPosition.x - unitPosition.x) < stoppingDistance && Mathf.Abs(nextPosition.z - unitPosition.z) < stoppingDistance)
                {
                    targetPosition = nextPosition;
                }
                // If the next path position is below the unit's current position
                else if (firstPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPosition.z, unitPosition.z) == false))
                {
                    Debug.Log("And then Here");
                    targetPosition = new Vector3(firstPointOnPath.x, unitPosition.y, firstPointOnPath.z);
                    nextPosition = new Vector3(nextPosition.x, firstPointOnPath.y, nextPosition.z);
                }
                //else
                    //targetPosition = nextPosition;
            }
            else
            {
                Debug.Log("And Finally Here");
                targetPosition = nextPosition;
                increaseCurrentPositionIndex = true;
            }

            Vector3 moveDirection = (targetPosition - unitPosition).normalized;

            RotateTowardsTargetPosition(firstPointOnPath);

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
        unit.unitActionHandler.FinishAction();

        if (unit.IsPlayer())
            GridSystemVisual.Instance.UpdateGridVisual();

        if (unit.gridPosition != targetGridPosition)
            unit.unitActionHandler.QueueAction(this, 25);
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

        unit.unitActionHandler.GetAction<TurnAction>().SetCurrentDirection();
    }

    public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public void SetShouldFollowLeader(bool shouldFollowLeader) => this.shouldFollowLeader = shouldFollowLeader;
    
    public void ResetToDefaults()
    {
        roamPositionSet = false;
        needsFleeDestination = true;
        initialPatrolPointSet = false;

        fleeDestination = Vector3.zero;
        distToFleeDestination = 0;
    }
}
