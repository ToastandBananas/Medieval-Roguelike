using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction { North, East, South, West, NorthWest, NorthEast, SouthWest, SouthEast, Center }

public class TurnAction : BaseAction
{
    public Direction currentDirection { get; private set; }

    Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();

        SetCurrentDirection();
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        throw new NotImplementedException();
    }

    public Direction DetermineTurnDirection()
    {
        GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
        GridPosition unitGridPosition = unit.gridPosition;
        Direction turnDirection;

        if (mouseGridPosition.x == unitGridPosition.x && mouseGridPosition.z > unitGridPosition.z)
            turnDirection = Direction.North;
        else if (mouseGridPosition.x > unitGridPosition.x && mouseGridPosition.z == unitGridPosition.z)
            turnDirection = Direction.East;
        else if (mouseGridPosition.x == unitGridPosition.x && mouseGridPosition.z < unitGridPosition.z)
            turnDirection = Direction.South;
        else if (mouseGridPosition.x < unitGridPosition.x && mouseGridPosition.z == unitGridPosition.z)
            turnDirection = Direction.West;
        else if (mouseGridPosition.x < unitGridPosition.x && mouseGridPosition.z > unitGridPosition.z)
            turnDirection = Direction.NorthWest;
        else if (mouseGridPosition.x > unitGridPosition.x && mouseGridPosition.z > unitGridPosition.z)
            turnDirection = Direction.NorthEast;
        else if (mouseGridPosition.x < unitGridPosition.x && mouseGridPosition.z < unitGridPosition.z)
            turnDirection = Direction.SouthWest;
        else if (mouseGridPosition.x > unitGridPosition.x && mouseGridPosition.z < unitGridPosition.z)
            turnDirection = Direction.SouthEast;
        else
            turnDirection = Direction.Center;

        return turnDirection;
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
}
