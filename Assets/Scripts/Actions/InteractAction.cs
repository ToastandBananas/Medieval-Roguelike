using System.Collections;

public class InteractAction : BaseAction
{
    Interactable targetInteractable;

    public override void TakeAction(GridPosition gridPosition)
    {
        StartAction();

        StartCoroutine(Interact());
    }

    IEnumerator Interact()
    {
        TurnAction turnAction = unit.unitActionHandler.GetAction<TurnAction>();

        if (unit.IsPlayer() || unit.unitMeshManager.IsVisibleOnScreen())
        {
            if (turnAction.IsFacingTarget(targetInteractable.gridPosition) == false)
                turnAction.RotateTowardsPosition(targetInteractable.gridPosition.WorldPosition(), false, turnAction.DefaultRotateSpeed() * 2f);

            while (turnAction.isRotating)
                yield return null;
        }
        else
            turnAction.RotateTowardsPosition(targetInteractable.gridPosition.WorldPosition(), true);

        // Do the interaction
        targetInteractable.Interact(unit);

        CompleteAction();
        unit.unitActionHandler.SetTargetInteractable(null);
        TurnManager.Instance.StartNextUnitsTurn(unit);
    }

    public override int GetActionPointsCost()
    {
        Interactable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(unit.unitActionHandler.targetGridPosition);
        if (interactable is Door)
            return 150;
        return 100;
    }

    public void SetTargetInteractable(Interactable target) => targetInteractable = target;

    public override void CompleteAction()
    {
        base.CompleteAction();
        unit.unitActionHandler.FinishAction();
    }

    public override bool IsValidAction() => false; // Setting this to false prevents it from showing up as an action in the action bar

    public override bool IsAttackAction() => false;

    public override bool IsMeleeAttackAction() => false;

    public override bool IsRangedAttackAction() => false;

    public override bool ActionIsUsedInstantly() => true;

    public override int GetEnergyCost() => 0;

    public override string GetActionName() => "Interact";
}
