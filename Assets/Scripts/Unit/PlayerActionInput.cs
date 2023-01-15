using UnityEngine;

public class PlayerActionInput : MonoBehaviour
{
    Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();
        unit.SetIsMyTurn(true);
    }

    void Update()
    {
        if (unit.isMyTurn)
        {
            if (unit.unitActionHandler.GetAction<MoveAction>().isMoving == false /*&& selectedAction != null && selectedAction is MoveAction*/)
                StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
            //else if (selectedAction != null && selectedAction is TurnAction)
                //ActionLineRenderer.Instance.DrawTurnArrow(unit.UnitActionHandler().GetAction<TurnAction>().targetPosition);
            else
                ActionLineRenderer.Instance.HideLineRenderers();

            if (GameControls.gamePlayActions.leftMouseClick.WasPressed)
            {
                GridPosition mouseGridPosition = GetMouseGridPosition();

                unit.unitActionHandler.SetTargetGridPosition(mouseGridPosition);
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<MoveAction>(), 25);
            }
        }
    }

    GridPosition GetMouseGridPosition()
    {
        // Debug.Log("Mouse Grid Position: " + LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition()));
        return LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
    }
}
