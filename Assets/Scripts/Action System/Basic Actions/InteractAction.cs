using System.Collections;
using GridSystem;
using InteractableObjects;
using UnitSystem;

namespace ActionSystem
{
    public class InteractAction : BaseAction
    {
        public Interactable targetInteractable { get; private set; }

        public void QueueAction(Interactable targetInteractable)
        {
            this.targetInteractable = targetInteractable;
            unit.unitActionHandler.QueueAction(this);
        }

        public void QueueActionImmediately(Interactable targetInteractable)
        {
            this.targetInteractable = targetInteractable;
            unit.unitActionHandler.QueueAction(this, true);
        }

        public override void TakeAction()
        {
            StartAction();

            StartCoroutine(Interact());
        }

        IEnumerator Interact()
        {
            TurnAction turnAction = unit.unitActionHandler.GetAction<TurnAction>();
            if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen())
            {
                if (turnAction.IsFacingTarget(targetInteractable.GridPosition()) == false)
                    turnAction.RotateTowardsPosition(targetInteractable.GridPosition().WorldPosition(), false, turnAction.DefaultRotateSpeed() * 2f);

                while (unit.unitActionHandler.isRotating)
                    yield return null;
            }
            else
                turnAction.RotateTowardsPosition(targetInteractable.GridPosition().WorldPosition(), true);

            // Perform the interaction
            if (targetInteractable.CanInteractAtMyGridPosition() || LevelGrid.Instance.HasAnyUnitOnGridPosition(targetInteractable.GridPosition()) == false)
                targetInteractable.Interact(unit);

            CompleteAction();
            targetInteractable = null;
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override int GetActionPointsCost()
        {
            if (targetInteractable is Door)
                return 150;
            return 100;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();
            unit.unitActionHandler.FinishAction();
        }

        public override bool IsHotbarAction() => false;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override int GetEnergyCost() => 0;
    }
}
