using Pathfinding;
using System;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    protected GridPosition gridPosition;

    [SerializeField] protected SingleNodeBlocker singleNodeBlocker;
    [SerializeField] float actionPointCostMultiplier = 1f;

    void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddInteractableAtGridPosition(gridPosition, this);

        singleNodeBlocker.manager = LevelGrid.Instance.GetBlockManager();
        LevelGrid.Instance.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.Instance.GetUnitSingleNodeBlockerList());
        BlockCurrentPosition();
    }

    public GridPosition GridPosition() => gridPosition;

    public float ActionPointCostMultiplier() => actionPointCostMultiplier;

    protected void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    protected void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    public abstract void Interact(Action onInteractableBehaviourComplete);
}
