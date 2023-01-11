using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractAction : BaseAction
{
    [SerializeField] float maxInteractDistance = 1.4f;

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        Collider[] colliderArray = Physics.OverlapSphere(unit.transform.position, maxInteractDistance);

        foreach (Collider collider in colliderArray)
        {
            if (collider.transform.parent.TryGetComponent(out Interactable interactable))
            {
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(interactable.GridPosition(), unit.GridPosition()) <= maxInteractDistance)
                    validGridPositionList.Add(interactable.GridPosition());
            }
        }

        return validGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        Interactable interactableObject = LevelGrid.Instance.GetInteractableAtGridPosition(gridPosition);
        interactableObject.Interact(CompleteAction);

        StartAction(onActionComplete);
    }

    public override void CompleteAction()
    {
        base.CompleteAction();

        UnitActionSystem.Instance.ClearSelectedInteractable();
    }

    public override int GetActionPointsCost() => 10;

    public int GetActionPointsCost(Interactable interactable) => Mathf.RoundToInt(GetActionPointsCost() * interactable.ActionPointCostMultiplier());

    public override bool IsValidAction()
    {
        // TODO: Check if there's an Interactable in range
        return true;
    }

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Interact";
}
