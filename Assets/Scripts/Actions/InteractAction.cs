public class InteractAction : BaseAction
{
    GridPosition interactableGridPosition;

    public override void TakeAction(GridPosition gridPosition)
    {
        StartAction();
        LevelGrid.Instance.GetInteractableAtGridPosition(interactableGridPosition).Interact(unit);

        CompleteAction();
        TurnManager.Instance.StartNextUnitsTurn(unit);
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        Interactable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(targetGridPosition);
        if (interactable is Door)
            return 150;
        return 100;
    }

    public void SetInteractableGridPosition(GridPosition gridPosition) => interactableGridPosition = gridPosition;

    public override void CompleteAction()
    {
        base.CompleteAction();
        unit.unitActionHandler.SetTargetInteractable(null);
        unit.unitActionHandler.FinishAction();
    }

    public override bool IsValidAction() => false; // Setting this to false prevents it from showing up as an action in the action bar

    public override bool ActionIsUsedInstantly() => true;

    public override string GetActionName() => "Interact";
}
