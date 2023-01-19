using System;
using System.Collections;
using UnityEngine;

public enum Direction { North, East, South, West, NorthWest, NorthEast, SouthWest, SouthEast, Center }

public class TurnAction : BaseAction
{
    public Direction currentDirection { get; private set; }
    public Direction targetDirection { get; private set; }
    public Vector3 targetPosition { get; private set; }

    readonly float defaultRotateSpeed = 10f;
    readonly int singleTurnSegmentAPCost = 25;

    Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();

        SetCurrentDirection();
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (targetDirection == Direction.Center)
            return;

        StartAction(onActionComplete);

        StartCoroutine(RotateTowardsPosition(targetPosition));
    }

    public IEnumerator RotateTowardsPosition(Vector3 targetPosition)
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        float headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;

        float rotateSpeed = 10f;
        Vector3 lookPos = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
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
        SetCurrentDirection();

        CompleteAction();
        unit.unitActionHandler.FinishAction();
    }

    public void RotateTowardsDirection(Direction direction)
    {
        if (direction == currentDirection)
            return;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition;
        switch (direction)
        {
            case Direction.North:
                targetPosition = new Vector3(startPosition.x, startPosition.y, startPosition.z + 1);
                break;
            case Direction.East:
                targetPosition = new Vector3(startPosition.x + 1, startPosition.y, startPosition.z);
                break;
            case Direction.South:
                targetPosition = new Vector3(startPosition.x, startPosition.y, startPosition.z - 1);
                break;
            case Direction.West:
                targetPosition = new Vector3(startPosition.x - 1, startPosition.y, startPosition.z);
                break;
            case Direction.NorthWest:
                targetPosition = new Vector3(startPosition.x - 1, startPosition.y, startPosition.z + 1);
                break;
            case Direction.NorthEast:
                targetPosition = new Vector3(startPosition.x + 1, startPosition.y, startPosition.z + 1);
                break;
            case Direction.SouthWest:
                targetPosition = new Vector3(startPosition.x - 1, startPosition.y, startPosition.z - 1);
                break;
            case Direction.SouthEast:
                targetPosition = new Vector3(startPosition.x + 1, startPosition.y, startPosition.z - 1);
                break;
            case Direction.Center:
                break;
        }

        Vector3 dir = (targetPosition - startPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f) 
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, defaultRotateSpeed * Time.deltaTime);
        }

        transform.rotation = targetRotation;
        SetCurrentDirection();
    }

    public Direction DetermineTargetTurnDirection(GridPosition targetGridPosition)
    {
        GridPosition unitGridPosition = unit.gridPosition;

        if (targetGridPosition.x == unitGridPosition.x && targetGridPosition.z > unitGridPosition.z)
            targetDirection = Direction.North;
        else if (targetGridPosition.x > unitGridPosition.x && targetGridPosition.z == unitGridPosition.z)
            targetDirection = Direction.East;
        else if (targetGridPosition.x == unitGridPosition.x && targetGridPosition.z < unitGridPosition.z)
            targetDirection = Direction.South;
        else if (targetGridPosition.x < unitGridPosition.x && targetGridPosition.z == unitGridPosition.z)
            targetDirection = Direction.West;
        else if (targetGridPosition.x < unitGridPosition.x && targetGridPosition.z > unitGridPosition.z)
            targetDirection = Direction.NorthWest;
        else if (targetGridPosition.x > unitGridPosition.x && targetGridPosition.z > unitGridPosition.z)
            targetDirection = Direction.NorthEast;
        else if (targetGridPosition.x < unitGridPosition.x && targetGridPosition.z < unitGridPosition.z)
            targetDirection = Direction.SouthWest;
        else if (targetGridPosition.x > unitGridPosition.x && targetGridPosition.z < unitGridPosition.z)
            targetDirection = Direction.SouthEast;
        else
            targetDirection = Direction.Center;

        return targetDirection;
    }

    public Direction SetCurrentDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        float headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;

        if ((headingAngle >= 337.5f && headingAngle <= 360f) || (headingAngle >= 0f && headingAngle <= 22.5f))
            currentDirection = Direction.North;
        else if (headingAngle > 22.5f && headingAngle < 67.5f)
            currentDirection = Direction.NorthEast;
        else if (headingAngle >= 67.5f && headingAngle <= 112.5f)
            currentDirection = Direction.East;
        else if (headingAngle > 112.5f && headingAngle < 157.5f)
            currentDirection = Direction.SouthEast;
        else if (headingAngle >= 157.5f && headingAngle <= 202.5f)
            currentDirection = Direction.South;
        else if (headingAngle > 202.5f && headingAngle < 247.5f)
            currentDirection = Direction.SouthWest;
        else if (headingAngle >= 247.5f && headingAngle <= 292.5f)
            currentDirection = Direction.West;
        else
            currentDirection = Direction.NorthWest;

        return currentDirection;
    }

    public void SetTargetPosition(Direction targetDirection)
    {
        switch (targetDirection)
        {
            case Direction.North:
                targetPosition = transform.position + new Vector3(0, 0, 1);
                break;
            case Direction.East:
                targetPosition = transform.position + new Vector3(1, 0, 0);
                break;
            case Direction.South:
                targetPosition = transform.position + new Vector3(0, 0, -1);
                break;
            case Direction.West:
                targetPosition = transform.position + new Vector3(-1, 0, 0);
                break;
            case Direction.NorthWest:
                targetPosition = transform.position + new Vector3(-1, 0, 1);
                break;
            case Direction.NorthEast:
                targetPosition = transform.position + new Vector3(1, 0, 1);
                break;
            case Direction.SouthWest:
                targetPosition = transform.position + new Vector3(-1, 0, -1);
                break;
            case Direction.SouthEast:
                targetPosition = transform.position + new Vector3(1, 0, -1);
                break;
            case Direction.Center:
                targetPosition = Vector3.zero;
                break;
        }
    }

    int GetRotationsSegmentCount()
    {
        switch (currentDirection)
        {
            case Direction.North:
                switch (targetDirection)
                {
                    case Direction.South:
                        return 4;
                    case Direction.West:
                        return 2;
                    case Direction.East:
                        return 2;
                    case Direction.NorthWest:
                        return 1;
                    case Direction.NorthEast:
                        return 1;
                    case Direction.SouthWest:
                        return 3;
                    case Direction.SouthEast:
                        return 3;
                    default:
                        return 0;
                }
            case Direction.South:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 4;
                    case Direction.West:
                        return 2;
                    case Direction.East:
                        return 2;
                    case Direction.NorthWest:
                        return 3;
                    case Direction.NorthEast:
                        return 3;
                    case Direction.SouthWest:
                        return 1;
                    case Direction.SouthEast:
                        return 1;
                    default:
                        return 0;
                }
            case Direction.West:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 2;
                    case Direction.South:
                        return 2;
                    case Direction.East:
                        return 4;
                    case Direction.NorthWest:
                        return 1;
                    case Direction.NorthEast:
                        return 3;
                    case Direction.SouthWest:
                        return 1;
                    case Direction.SouthEast:
                        return 3;
                    default:
                        return 0;
                }
            case Direction.East:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 2;
                    case Direction.South:
                        return 2;
                    case Direction.West:
                        return 4;
                    case Direction.NorthWest:
                        return 3;
                    case Direction.NorthEast:
                        return 1;
                    case Direction.SouthWest:
                        return 3;
                    case Direction.SouthEast:
                        return 1;
                    default:
                        return 0;
                }
            case Direction.NorthWest:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 1;
                    case Direction.South:
                        return 3;
                    case Direction.West:
                        return 1;
                    case Direction.East:
                        return 3;
                    case Direction.NorthEast:
                        return 2;
                    case Direction.SouthWest:
                        return 2;
                    case Direction.SouthEast:
                        return 4;
                    default:
                        return 0;
                }
            case Direction.NorthEast:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 1;
                    case Direction.South:
                        return 3;
                    case Direction.West:
                        return 3;
                    case Direction.East:
                        return 1;
                    case Direction.NorthWest:
                        return 2;
                    case Direction.SouthWest:
                        return 4;
                    case Direction.SouthEast:
                        return 2;
                    default:
                        return 0;
                }
            case Direction.SouthWest:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 3;
                    case Direction.South:
                        return 1;
                    case Direction.West:
                        return 1;
                    case Direction.East:
                        return 3;
                    case Direction.NorthWest:
                        return 2;
                    case Direction.NorthEast:
                        return 4;
                    case Direction.SouthEast:
                        return 2;
                    default:
                        return 0;
                }
            case Direction.SouthEast:
                switch (targetDirection)
                {
                    case Direction.North:
                        return 3;
                    case Direction.South:
                        return 1;
                    case Direction.West:
                        return 3;
                    case Direction.East:
                        return 1;
                    case Direction.NorthWest:
                        return 4;
                    case Direction.NorthEast:
                        return 2;
                    case Direction.SouthWest:
                        return 2;
                    default:
                        return 0;
                }
            default:
                return 0;
        }
    }

    public GridPosition GetTargetGridPosition() => LevelGrid.Instance.GetGridPosition(targetPosition); 

    public override string GetActionName() => "Turn";

    public override bool IsValidAction() => true;

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        // Debug.Log(singleTurnSegmentAPCost * GetRotationsSegmentCount());
        return singleTurnSegmentAPCost * GetRotationsSegmentCount();
    }

    public override bool ActionIsUsedInstantly() => false;
}
