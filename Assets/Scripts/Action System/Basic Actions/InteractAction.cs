using System.Collections;
using GridSystem;
using InteractableObjects;
using UnitSystem;
using UnityEngine;

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

            if (targetInteractable == null || targetInteractable.gameObject.activeSelf == false)
            {
                CompleteAction();
                return;
            }

            StartCoroutine(Interact());
        }

        IEnumerator Interact()
        {
            TurnAction turnAction = unit.unitActionHandler.turnAction;
            if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen)
            {
                if (turnAction.IsFacingTarget(targetInteractable.GridPosition()) == false)
                    turnAction.RotateTowardsPosition(targetInteractable.GridPosition().WorldPosition, false, turnAction.DefaultRotateSpeed * 2f);

                while (unit.unitActionHandler.isRotating)
                    yield return null;
            }
            else
                turnAction.RotateTowardsPosition(targetInteractable.GridPosition().WorldPosition, true);

            // Perform the interaction
            if (targetInteractable.CanInteractAtMyGridPosition() || LevelGrid.HasAnyUnitOnGridPosition(targetInteractable.GridPosition()) == false)
                targetInteractable.Interact(unit);

            CompleteAction();
        }

        public void SetTargetInteractable(Interactable interactable) => targetInteractable = interactable;

        public override int GetActionPointsCost()
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
            unit.unitActionHandler.FinishAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public override string TooltipDescription() => "";

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => ActionSystem.ActionBarSection.None;

        public override bool IsValidAction() => true;

        public override bool ActionIsUsedInstantly() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override int GetEnergyCost() => 0;
    }
}
