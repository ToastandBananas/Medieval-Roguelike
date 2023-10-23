using UnityEngine;
using GridSystem;
using UnitSystem;

namespace InteractableObjects
{
    public abstract class Interactable : MonoBehaviour
    {
        protected GridPosition gridPosition;

        public virtual void Awake()
        {
            gridPosition.Set(transform.position);
            UpdateGridPosition();
        }

        public virtual void UpdateGridPosition()
        {
            LevelGrid.RemoveInteractableAtGridPosition(gridPosition);
            gridPosition = LevelGrid.GetGridPosition(transform.position);
            LevelGrid.AddInteractableAtGridPosition(gridPosition, this);
        }

        public virtual GridPosition GridPosition() => gridPosition;

        public abstract void Interact(Unit unit);

        public abstract bool CanInteractAtMyGridPosition();
    }
}