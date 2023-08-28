using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public virtual void Awake()
    {
        UpdateGridPosition();
    }

    public virtual void UpdateGridPosition()
    {
        gridPosition = LevelGrid.GetGridPosition(transform.position);
        LevelGrid.Instance.AddInteractableAtGridPosition(gridPosition, this);
    }

    public GridPosition gridPosition { get; protected set; }

    public abstract void Interact(Unit unit);

    public abstract bool CanInteractAtMyGridPosition();
}
