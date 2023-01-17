using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveAction : BaseAction
{
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
        if (positionList.Count == 0)
        {
            CompleteAction();
            unit.unitActionHandler.FinishAction();
            yield break;
        }
        
        Vector3 firstPointOnPath = positionList[1];
        Vector3 nextPosition = unit.transform.localPosition;
        float stoppingDistance = 0.0125f;
        
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
            unit.unitAnimator.StartMovingForward();
            ActionLineRenderer.Instance.HideLineRenderers();

            Vector3 unitPosition = unit.transform.localPosition;
            Vector3 targetPosition = unitPosition;

            if (Mathf.Abs(Mathf.Abs(firstPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
            {
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
                    targetPosition = new Vector3(firstPointOnPath.x, unitPosition.y, firstPointOnPath.z);
                    nextPosition = new Vector3(nextPosition.x, firstPointOnPath.y, nextPosition.z);
                }
            }
            else
                targetPosition = nextPosition;

            Vector3 moveDirection = (targetPosition - unitPosition).normalized;

            RotateTowardsTargetPosition(firstPointOnPath);

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

    public override string GetActionName() => "Move";

    public override bool IsValidAction()
    {
        // TODO: Test if the unit is immobile for whatever reason (broken legs, some sort of spell effect, etc.)
        return true;
    }

    public override int GetActionPointsCost()
    {
        // TODO: Cost 25 per square (or more depending on terrain type)
        return 25;
    }

    public override bool ActionIsUsedInstantly() => false;
}
