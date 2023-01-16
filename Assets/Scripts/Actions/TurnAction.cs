using System;
using System.Collections;
using UnityEngine;

public enum Direction { North, East, South, West, NorthWest, NorthEast, SouthWest, SouthEast, Center }

public class TurnAction : BaseAction
{
    public Direction currentDirection { get; private set; }
    public Direction targetDirection { get; private set; }
    public Vector3 targetPosition { get; private set; }

    Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();

        SetCurrentDirection();
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (targetPosition == Vector3.zero)
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

    public Direction DetermineTargetTurnDirection()
    {
        GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
        GridPosition unitGridPosition = unit.gridPosition;

        if (mouseGridPosition.x == unitGridPosition.x && mouseGridPosition.z > unitGridPosition.z)
            targetDirection = Direction.North;
        else if (mouseGridPosition.x > unitGridPosition.x && mouseGridPosition.z == unitGridPosition.z)
            targetDirection = Direction.East;
        else if (mouseGridPosition.x == unitGridPosition.x && mouseGridPosition.z < unitGridPosition.z)
            targetDirection = Direction.South;
        else if (mouseGridPosition.x < unitGridPosition.x && mouseGridPosition.z == unitGridPosition.z)
            targetDirection = Direction.West;
        else if (mouseGridPosition.x < unitGridPosition.x && mouseGridPosition.z > unitGridPosition.z)
            targetDirection = Direction.NorthWest;
        else if (mouseGridPosition.x > unitGridPosition.x && mouseGridPosition.z > unitGridPosition.z)
            targetDirection = Direction.NorthEast;
        else if (mouseGridPosition.x < unitGridPosition.x && mouseGridPosition.z < unitGridPosition.z)
            targetDirection = Direction.SouthWest;
        else if (mouseGridPosition.x > unitGridPosition.x && mouseGridPosition.z < unitGridPosition.z)
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

    public override string GetActionName() => "Turn";

    public override bool IsValidAction() => true;

    public override int GetActionPointsCost() => 10;

    public override bool ActionIsUsedInstantly() => false;
}
