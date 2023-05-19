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

    public bool isRotating { get; private set; }

    void Start()
    {
        SetCurrentDirection();
    }

    public override void TakeAction(GridPosition gridPosition)
    {
        if (targetDirection == Direction.Center)
            return;

        SetTargetPosition(targetDirection);
        StartAction();

        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
            Turn(false);
        else
            Turn(true);
    }

    void Turn(bool rotateInstantly)
    {
        RotateTowards_CurrentTargetPosition(rotateInstantly);
        currentDirection = targetDirection;

        CompleteAction();

        if (unit.IsNPC())
            unit.unitActionHandler.TakeTurn();
        else
            TurnManager.Instance.StartNextUnitsTurn(unit);
    }

    public void RotateTowards_CurrentTargetPosition(bool rotateInstantly)
    {
        StartCoroutine(Rotate(targetPosition, rotateInstantly));
    }

    public void RotateTowards_Direction(Direction direction, bool rotateInstantly)
    {
        SetTargetPosition(direction);
        RotateTowards_CurrentTargetPosition(rotateInstantly);
    }

    public void RotateTowardsPosition(Vector3 targetPos, bool rotateInstantly, float rotateSpeed = 10f)
    {
        StartCoroutine(Rotate(targetPos, rotateInstantly, rotateSpeed));
    }

    IEnumerator Rotate(Vector3 targetPos, bool rotateInstantly, float rotateSpeed = 10f)
    {
        Vector3 lookPos = (new Vector3(targetPos.x, transform.position.y, targetPos.z) - transform.position).normalized;
        if (lookPos == Vector3.zero)
            yield break;

        targetPosition = targetPos;
        Vector3 rotateTargetPosition = targetPos;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);

        if (rotateInstantly == false)
        {
            while (true)
            {
                if (rotateTargetPosition != targetPosition)
                    break;

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
                    break;

                yield return null;
            }
        }

        isRotating = false;
        transform.rotation = targetRotation;
        SetCurrentDirection();

        unit.vision.FindVisibleUnitsAndObjects();
    }

    public void RotateTowards_Unit(Unit targetUnit, bool rotateInstantly)
    {
        RotateTowardsPosition(targetUnit.gridPosition.WorldPosition(), rotateInstantly, defaultRotateSpeed * 2f);
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

    public GridPosition GetGridPositionBehindUnit()
    {
        GridPosition gridPositionBehindUnit;
        switch (currentDirection)
        {
            case Direction.North:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(0, 0, -1));
                break;
            case Direction.East:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(-1, 0, 0));
                break;
            case Direction.South:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(0, 0, 1));
                break;
            case Direction.West:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(1, 0, 0));
                break;
            case Direction.NorthWest:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(1, 0, -1));
                break;
            case Direction.NorthEast:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(-1, 0, -1));
                break;
            case Direction.SouthWest:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(1, 0, 1));
                break;
            case Direction.SouthEast:
                gridPositionBehindUnit = LevelGrid.GetGridPosition(unit.gridPosition.WorldPosition() + new Vector3(-1, 0, 1));
                break;
            default:
                return unit.gridPosition;
        }

        // Get the Y position for the Grid Position using a raycast
        Physics.Raycast(gridPositionBehindUnit.WorldPosition() + new Vector3(0, 1, 0), -Vector3.up, out RaycastHit hit, 1000f, WorldMouse.Instance.MousePlaneLayerMask());
        if (hit.collider != null)
            gridPositionBehindUnit = new GridPosition(gridPositionBehindUnit.x, hit.point.y, gridPositionBehindUnit.z);

        if (LevelGrid.Instance.GridPositionObstructed(gridPositionBehindUnit))
            gridPositionBehindUnit = LevelGrid.Instance.FindNearestValidGridPosition(unit.gridPosition, unit, 1.4f);
        return gridPositionBehindUnit;
    }

    public bool AttackerInFrontOfUnit(Unit attackingUnit)
    {
        if (attackingUnit == null) return false;

        Vector3 unitPos = transform.position;
        Vector3 attackerPos = attackingUnit.transform.position;

        switch (attackingUnit.unitActionHandler.GetAction<TurnAction>().currentDirection) // Direction the attacking Unit is facing
        {
            case Direction.North:
                if ((currentDirection == Direction.South || currentDirection == Direction.SouthWest || currentDirection == Direction.SouthEast) && unitPos.z > attackerPos.z)
                    return true;
                break;
            case Direction.East:
                if ((currentDirection == Direction.West || currentDirection == Direction.SouthWest || currentDirection == Direction.NorthWest) && unitPos.x > attackerPos.x)
                    return true;
                break;
            case Direction.South:
                if ((currentDirection == Direction.North || currentDirection == Direction.NorthWest || currentDirection == Direction.NorthEast) && unitPos.z < attackerPos.z)
                    return true;
                break;
            case Direction.West:
                if ((currentDirection == Direction.East || currentDirection == Direction.SouthEast || currentDirection == Direction.NorthEast) && unitPos.x < attackerPos.x)
                    return true;
                break;
            case Direction.NorthWest:
                if ((currentDirection == Direction.South || currentDirection == Direction.SouthEast || currentDirection == Direction.East) && unitPos.x < attackerPos.x && unitPos.z > attackerPos.z)
                    return true;
                break;
            case Direction.NorthEast:
                if ((currentDirection == Direction.South || currentDirection == Direction.SouthWest || currentDirection == Direction.West) && unitPos.x > attackerPos.x && unitPos.z > attackerPos.z)
                    return true;
                break;
            case Direction.SouthWest:
                if ((currentDirection == Direction.North || currentDirection == Direction.NorthEast || currentDirection == Direction.East) && unitPos.x < attackerPos.x && unitPos.z < attackerPos.z)
                    return true;
                break;
            case Direction.SouthEast:
                if ((currentDirection == Direction.North || currentDirection == Direction.NorthWest || currentDirection == Direction.West) && unitPos.x > attackerPos.x && unitPos.z < attackerPos.z)
                    return true;
                break;
        }
        return false;
    }

    public bool AttackerBesideUnit(Unit attackingUnit)
    {
        if (attackingUnit == null) return false;

        Vector3 unitPos = transform.position;
        Vector3 attackerPos = attackingUnit.transform.position;

        switch (attackingUnit.unitActionHandler.GetAction<TurnAction>().currentDirection) // Direction the attacking Unit is facing
        {
            case Direction.North:
                if ((currentDirection == Direction.West || currentDirection == Direction.East) && unitPos.x == attackerPos.x && unitPos.z > attackerPos.z)
                    return true;
                break;
            case Direction.East:
                if ((currentDirection == Direction.North || currentDirection == Direction.South) && unitPos.x > attackerPos.x && unitPos.z == attackerPos.z)
                    return true;
                break;
            case Direction.South:
                if ((currentDirection == Direction.West || currentDirection == Direction.East) && unitPos.x == attackerPos.x && unitPos.z < attackerPos.z)
                    return true;
                break;
            case Direction.West:
                if ((currentDirection == Direction.North || currentDirection == Direction.South) && unitPos.x < attackerPos.x && unitPos.z == attackerPos.z)
                    return true;
                break;
            case Direction.NorthWest:
                if ((currentDirection == Direction.NorthEast || currentDirection == Direction.SouthWest) && unitPos.x < attackerPos.x && unitPos.z > attackerPos.z)
                    return true;
                break;
            case Direction.NorthEast:
                if ((currentDirection == Direction.NorthWest || currentDirection == Direction.SouthEast) && unitPos.x > attackerPos.x && unitPos.z > attackerPos.z)
                    return true;
                break;
            case Direction.SouthWest:
                if ((currentDirection == Direction.NorthWest || currentDirection == Direction.SouthEast) && unitPos.x < attackerPos.x && unitPos.z < attackerPos.z)
                    return true;
                break;
            case Direction.SouthEast:
                if ((currentDirection == Direction.NorthEast || currentDirection == Direction.SouthWest) && unitPos.x > attackerPos.x && unitPos.z < attackerPos.z)
                    return true;
                break;
        }
        return false;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();
        unit.unitActionHandler.FinishAction();
    }

    public bool IsFacingTarget(GridPosition targetGridPosition) => DetermineTargetTurnDirection(targetGridPosition) == currentDirection;

    public GridPosition GetTargetGridPosition() => LevelGrid.GetGridPosition(targetPosition);

    public override string GetActionName() => "Turn";

    public override bool IsValidAction() => true;

    public override bool IsAttackAction() => false;

    public override bool IsMeleeAttackAction() => false;

    public override bool IsRangedAttackAction() => false;

    public override int GetActionPointsCost() => singleTurnSegmentAPCost * GetRotationsSegmentCount();

    public override int GetEnergyCost() => 0;

    public override bool ActionIsUsedInstantly() => false;

    public float DefaultRotateSpeed() => defaultRotateSpeed;
}
