public class InteractAction : BaseAction
{
    GridPosition targetInteractableGridPosition;

    public override void TakeAction(GridPosition gridPosition)
    {
        StartAction();
        if (unit.unitActionHandler.GetAction<TurnAction>().IsFacingTarget(targetInteractableGridPosition))
        {
            LevelGrid.Instance.GetInteractableAtGridPosition(targetInteractableGridPosition).Interact(unit);

            CompleteAction();
            unit.unitActionHandler.SetTargetInteractable(null);
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }
        else
        {
            CompleteAction();
            unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
        }
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        Interactable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(targetGridPosition);
        if (interactable is Door)
            return 150;
        return 100;
    }

    public void SetTargetInteractableGridPosition(GridPosition gridPosition) => targetInteractableGridPosition = gridPosition;

    public override void CompleteAction()
    {
        base.CompleteAction();
        unit.unitActionHandler.FinishAction();
    }

    public override bool IsValidAction() => false; // Setting this to false prevents it from showing up as an action in the action bar

    public override bool ActionIsUsedInstantly() => true;

    public override string GetActionName() => "Interact";
}
