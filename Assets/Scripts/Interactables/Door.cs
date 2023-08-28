using System.Collections;
using UnityEngine;

public class Door : Interactable
{
    public bool isOpen { get; private set; }

    float rotationAmount = 90f;
    float speed = 2f;

    Vector3 closedRotation;

    public override void Awake()
    {
        base.Awake();

        closedRotation = transform.rotation.eulerAngles;
    }

    public override void Interact(Unit unit)
    {
        isOpen = !isOpen;

        if (isOpen)
            StartCoroutine(OpenDoor());
        else
            StartCoroutine(CloseDoor());
    }

    public override void UpdateGridPosition()
    {
        gridPosition = LevelGrid.GetGridPosition(transform.parent.position);
        LevelGrid.Instance.AddInteractableAtGridPosition(gridPosition, this);
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
