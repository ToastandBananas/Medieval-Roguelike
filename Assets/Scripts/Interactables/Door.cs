using System;
using System.Collections;
using UnityEngine;

public class Door : Interactable
{
    [SerializeField] BoxCollider boxCollider;

    bool isOpen = false;

    public override void Interact(Action onInteractableBehaviourComplete)
    {
        StartCoroutine(ToggleDoor(onInteractableBehaviourComplete));
    }

    IEnumerator ToggleDoor(Action onInteractableBehaviourComplete)
    {
        isOpen = !isOpen;
        boxCollider.enabled = false;

        Quaternion targetRotation;
        if (isOpen)
        {
            targetRotation = Quaternion.Euler(0f, -90f, 0f);
            UnblockCurrentPosition();
        }
        else
        {
            targetRotation = Quaternion.Euler(Vector3.zero);
            BlockCurrentPosition();
        }

        float rotateSpeed = 5f;
        while (transform.localRotation != targetRotation)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        onInteractableBehaviourComplete();

        boxCollider.enabled = true;
        transform.localRotation = targetRotation;
    }
}
