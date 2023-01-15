using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction { North, East, South, West, NorthWest, NorthEast, SouthWest, SouthEast }

public class PlayerActionInput : MonoBehaviour
{
    Unit unit;

    Direction directionFacing = Direction.North;

    void Start()
    {
        unit = GetComponent<Unit>();
        unit.SetIsMyTurn(true);
    }

    void Update()
    {
        if (unit.IsMyTurn())
        {
            if (unit.UnitActionHandler().GetAction<MoveAction>().IsMoving() == false /*&& selectedAction != null && selectedAction is MoveAction*/)
                StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
            //else if (selectedAction != null && selectedAction is TurnAction)
                //ActionLineRenderer.Instance.DrawTurnArrow(unit.UnitActionHandler().GetAction<TurnAction>().targetPosition);
            else
                ActionLineRenderer.Instance.HideLineRenderers();

            if (GameControls.gamePlayActions.leftMouseClick.WasPressed)
            {
                GridPosition mouseGridPosition = GetMouseGridPosition();

                unit.UnitActionHandler().SetTargetGridPosition(mouseGridPosition);
                unit.UnitActionHandler().QueueAction(unit.UnitActionHandler().GetAction<MoveAction>(), 25);
            }
        }
    }

    GridPosition GetMouseGridPosition()
    {
        Debug.Log("Mouse Grid Position: " + LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition()));
        return LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
    }
}
