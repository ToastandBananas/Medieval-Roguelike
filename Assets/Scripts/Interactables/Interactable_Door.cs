using System.Collections;
using UnityEngine;
using GridSystem;
using UnitSystem;

namespace InteractableObjects
{
    public class Interactable_Door : Interactable
    {
        public bool IsOpen { get; private set; }

        readonly float rotationAmount = 90f;
        readonly float speed = 2f;

        Vector3 closedRotation;

        public override void Awake()
        {
            base.Awake();

            closedRotation = transform.rotation.eulerAngles;
        }

        public override void Interact(Unit unit)
        {
            if (unit.UnitActionHandler.TurnAction.IsFacingTarget(gridPosition) == false)
                unit.UnitActionHandler.TurnAction.RotateTowardsPosition(gridPosition.WorldPosition, false, unit.UnitActionHandler.TurnAction.DefaultRotateSpeed * 2f);

            IsOpen = !IsOpen;

            if (IsOpen)
                StartCoroutine(OpenDoor());
            else
                StartCoroutine(CloseDoor());
        }

        public override void UpdateGridPosition()
        {
            LevelGrid.RemoveInteractableAtGridPosition(gridPosition);
            gridPosition.Set(transform.parent.position);
            LevelGrid.AddInteractableAtGridPosition(gridPosition, this);
        }

        IEnumerator OpenDoor()
        {
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation;

            endRotation = Quaternion.Euler(new Vector3(0f, startRotation.y + rotationAmount, 0f));

            float time = 0f;
            while (time < 1f)
            {
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, time);
                yield return null;
                time += Time.deltaTime * speed;
            }
        }

        IEnumerator CloseDoor()
        {
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(closedRotation);

            float time = 0f;
            while (time < 1f)
            {
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, time);
                yield return null;
                time += Time.deltaTime * speed;
            }
        }

        public override bool CanInteractAtMyGridPosition() => false;
    }
}