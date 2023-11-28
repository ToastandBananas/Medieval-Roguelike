using System.Collections;
using GridSystem;
using InteractableObjects;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public class InteractAction : BaseAction
    {
        public Interactable targetInteractable { get; private set; }

        public void QueueAction(Interactable targetInteractable)
        {
            this.targetInteractable = targetInteractable;
            TargetGridPosition = targetInteractable.GridPosition();

            // If the Unit is too far away to Interact, move to it first
            if (Vector3.Distance(Unit.WorldPosition, TargetGridPosition.WorldPosition) > LevelGrid.diaganolDistance)
                Unit.unitActionHandler.MoveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(TargetGridPosition, Unit.GridPosition, LevelGrid.diaganolDistance, true));
            else
                Unit.unitActionHandler.QueueAction(this);
        }

        public void QueueActionImmediately(Interactable targetInteractable)
        {
            this.targetInteractable = targetInteractable;
            TargetGridPosition = targetInteractable.GridPosition();

            // If the Unit is too far away to Interact, move to it first
            if (Vector3.Distance(Unit.WorldPosition, TargetGridPosition.WorldPosition) > LevelGrid.diaganolDistance)
                Unit.unitActionHandler.MoveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(TargetGridPosition, Unit.GridPosition, LevelGrid.diaganolDistance, true));
            else
                Unit.unitActionHandler.QueueAction(this, true);
        }

        public override void TakeAction()
        {
            StartAction();

            if (targetInteractable == null || targetInteractable.gameObject.activeSelf == false)
            {
                CompleteAction();
                return;
            }

            StartCoroutine(Interact());
        }

        IEnumerator Interact()
        {
            TurnAction turnAction = Unit.unitActionHandler.TurnAction;
            if (Unit.IsPlayer || Unit.unitMeshManager.IsVisibleOnScreen)
            {
                if (turnAction.IsFacingTarget(targetInteractable.GridPosition()) == false)
                    turnAction.RotateTowardsPosition(targetInteractable.GridPosition().WorldPosition, false, turnAction.DefaultRotateSpeed * 2f);

                while (Unit.unitActionHandler.TurnAction.isRotating)
                    yield return null;
            }
            else
                turnAction.RotateTowardsPosition(targetInteractable.GridPosition().WorldPosition, true);

            if (targetInteractable == null)
            {
                CompleteAction();
                yield break;
            }

            Interactable interactable = targetInteractable;
            CompleteAction();

            // Perform the interaction
            if (interactable.CanInteractAtMyGridPosition() || LevelGrid.HasUnitAtGridPosition(interactable.GridPosition(), out _) == false)
                interactable.Interact(Unit);
        }

        public void SetTargetInteractable(Interactable interactable) => targetInteractable = interactable;

        public override int ActionPointsCost()
        {
            if (targetInteractable is Door)
                return 150;
            else if (targetInteractable is LooseItem)
            {
                if (targetInteractable is LooseContainerItem)
                {
                    LooseContainerItem looseContainerItem = targetInteractable as LooseContainerItem;
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
            targetInteractable = null;
            Unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public override string TooltipDescription() => "";

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.None;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override int InitialEnergyCost() => 0;
    }
}
