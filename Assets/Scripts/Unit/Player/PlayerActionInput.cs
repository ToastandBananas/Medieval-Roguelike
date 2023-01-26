using UnityEngine;

public class PlayerActionInput : MonoBehaviour
{
    Unit unit;

    float skipCooldownTime = 0.25f;
    float skipCooldownTimer;

    void Start()
    {
        unit = GetComponent<Unit>();
        unit.SetIsMyTurn(true);
    }

    void Update()
    {
        if (skipCooldownTimer < skipCooldownTime)
            skipCooldownTimer += Time.deltaTime;

        if (unit.isDead == false)
        {
            if (GameControls.gamePlayActions.turnMode.WasReleased && unit.unitActionHandler.selectedAction == unit.unitActionHandler.GetAction<TurnAction>())
            {
                ActionLineRenderer.Instance.ResetCurrentPositions();
                unit.unitActionHandler.SetSelectedAction(unit.unitActionHandler.GetAction<MoveAction>());
            }

            if (unit.unitActionHandler.queuedAction != null || unit.unitActionHandler.GetAction<MoveAction>().isMoving)
            {
                if (GameControls.gamePlayActions.skipTurn.WasPressed)
                {
                    // Debug.Log("Cancelling Action");
                    unit.unitActionHandler.CancelAction();
                    ActionLineRenderer.Instance.ResetCurrentPositions();
                }
            }
            else if (unit.isMyTurn && unit.unitActionHandler.isPerformingAction == false && unit.unitActionHandler.GetAction<MoveAction>().isMoving == false)
            {
                if (GameControls.gamePlayActions.skipTurn.IsPressed && skipCooldownTimer >= skipCooldownTime)
                {
                    Debug.Log("Skipping");
                    skipCooldownTimer = 0f;
                    TurnManager.Instance.FinishTurn(unit);
                }

                if (unit.unitActionHandler.selectedAction != null)
                {
                    if (unit.unitActionHandler.selectedAction is MoveAction)
                        StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                    else if (unit.unitActionHandler.selectedAction is TurnAction)
                        ActionLineRenderer.Instance.DrawTurnArrow(unit.unitActionHandler.GetAction<TurnAction>().targetPosition);
                }

                if (GameControls.gamePlayActions.turnMode.IsPressed)
                {
                    unit.unitActionHandler.SetSelectedAction(unit.unitActionHandler.GetAction<TurnAction>());
                    unit.unitActionHandler.GetAction<TurnAction>().SetTargetPosition(unit.unitActionHandler.GetAction<TurnAction>().DetermineTargetTurnDirection(LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition())));

                    if (GameControls.gamePlayActions.select.WasPressed && unit.unitActionHandler.GetAction<TurnAction>().targetDirection != unit.unitActionHandler.GetAction<TurnAction>().currentDirection)
                        unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>(), unit.unitActionHandler.GetAction<TurnAction>().GetActionPointsCost(unit.unitActionHandler.GetAction<TurnAction>().GetTargetGridPosition()));
                }
                else if (GameControls.gamePlayActions.select.WasPressed)
                {
                    GridPosition mouseGridPosition = GetMouseGridPosition();
                    if (mouseGridPosition != unit.gridPosition && LevelGrid.Instance.IsValidGridPosition(mouseGridPosition) && AstarPath.active.GetNearest(mouseGridPosition.WorldPosition()).node.Walkable)
                    {
                        unit.unitActionHandler.SetTargetGridPosition(mouseGridPosition);
                        unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<MoveAction>(), unit.unitActionHandler.GetAction<MoveAction>().GetActionPointsCost(mouseGridPosition));
                    }
                }
            }
            else
            {
                ActionLineRenderer.Instance.HideLineRenderers();
            }
        }
    }

    GridPosition GetMouseGridPosition()
    {
        // Debug.Log("Mouse Grid Position: " + LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition()));
        return LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
    }
}
