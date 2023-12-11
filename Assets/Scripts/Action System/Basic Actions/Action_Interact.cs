using System.Collections;
using GridSystem;
using InteractableObjects;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_Interact : Action_Base
    {
        public Interactable TargetInteractable { get; private set; }

        public void QueueAction(Interactable targetInteractable)
        {
            this.TargetInteractable = targetInteractable;
            TargetGridPosition = targetInteractable.GridPosition();

            // If the Unit is too far away to Interact, move to it first
            if (Vector3.Distance(Unit.WorldPosition, TargetGridPosition.WorldPosition) > LevelGrid.diaganolDistance)
                Unit.UnitActionHandler.MoveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(TargetGridPosition, Unit.GridPosition, LevelGrid.diaganolDistance, true));
            else
                Unit.UnitActionHandler.QueueAction(this);
        }

        public void QueueActionImmediately(Interactable targetInteractable)
        {
            this.TargetInteractable = targetInteractable;
            TargetGridPosition = targetInteractable.GridPosition();

            // If the Unit is too far away to Interact, move to it first
            if (Vector3.Distance(Unit.WorldPosition, TargetGridPosition.WorldPosition) > LevelGrid.diaganolDistance)
                Unit.UnitActionHandler.MoveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(TargetGridPosition, Unit.GridPosition, LevelGrid.diaganolDistance, true));
            else
                Unit.UnitActionHandler.QueueAction(this, true);
        }

        public override void TakeAction()
        {
            StartAction();

            if (TargetInteractable == null || TargetInteractable.gameObject.activeSelf == false)
            {
                CompleteAction();
                return;
            }

            StartCoroutine(Interact());
        }

        IEnumerator Interact()
        {
            Action_Turn turnAction = Unit.UnitActionHandler.TurnAction;
            if (Unit.IsPlayer || Unit.UnitMeshManager.IsVisibleOnScreen)
            {
                if (turnAction.IsFacingTarget(TargetInteractable.GridPosition()) == false)
                    turnAction.RotateTowardsPosition(TargetInteractable.GridPosition().WorldPosition, false, turnAction.DefaultRotateSpeed * 2f);

                while (Unit.UnitActionHandler.TurnAction.isRotating)
                    yield return null;
            }
            else
                turnAction.RotateTowardsPosition(TargetInteractable.GridPosition().WorldPosition, true);

            if (TargetInteractable == null)
            {
                CompleteAction();
                yield break;
            }

            Interactable interactable = TargetInteractable;
            CompleteAction();

            // Perform the interaction
            if (interactable.CanInteractAtMyGridPosition() || LevelGrid.HasUnitAtGridPosition(interactable.GridPosition(), out _) == false)
                interactable.Interact(Unit);
        }

        public void SetTargetInteractable(Interactable interactable) => TargetInteractable = interactable;

        public override int ActionPointsCost()
        {
            if (TargetInteractable is Door)
                return 150;
            else if (TargetInteractable is LooseItem)
            {
                if (TargetInteractable is LooseContainerItem)
                {
                    LooseContainerItem looseContainerItem = TargetInteractable as LooseContainerItem;
                    if (looseContainerItem.ContainerInventoryManager.ContainsAnyItems()) // If a LooseContainerItem has any items in its inventory, then the interaction will be to open it up and look inside, costing AP
                        return 200;
                }

                return 100; // Bending down to pick it up will take some time
            }

            return 100;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();
            TargetInteractable = null;
            Unit.UnitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override string TooltipDescription() => "";

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.None;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override int EnergyCost() => 0;
    }
}
