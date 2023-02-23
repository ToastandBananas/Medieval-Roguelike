using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public virtual void Awake()
    {
        gridPosition = LevelGrid.GetGridPosition(transform.position);
        LevelGrid.Instance.AddInteractableAtGridPosition(gridPosition, this);
    }

    public GridPosition gridPosition { get; protected set; }

    public abstract void Interact(Unit unit);
}
