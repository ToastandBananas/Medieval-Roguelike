using System.Collections;
using UnityEngine;

public class Door : Interactable
{
    public bool isOpen { get; private set; }

    float rotationAmount = 90f;
    float speed = 2f;

    Vector3 closedRotation;
    Transform doorHinge;

    public override void Awake()
    {
        base.Awake();

        doorHinge = transform.GetChild(0);
        closedRotation = doorHinge.rotation.eulerAngles;
    }

    public override void Interact(Unit unit)
    {
        isOpen = !isOpen;

        if (isOpen)
            StartCoroutine(OpenDoor());
        else
            StartCoroutine(CloseDoor());
    }

    IEnumerator OpenDoor()
    {
        Quaternion startRotation = doorHinge.rotation;
        Quaternion endRotation;

        endRotation = Quaternion.Euler(new Vector3(0f, startRotation.y + rotationAmount, 0f));

        float time = 0f;
        while (time < 1f)
        {
            doorHinge.rotation = Quaternion.Slerp(startRotation, endRotation, time);
            yield return null;
            time += Time.deltaTime * speed;
        }
    }

    IEnumerator CloseDoor()
    {
        Quaternion startRotation = doorHinge.rotation;
        Quaternion endRotation = Quaternion.Euler(closedRotation);

        float time = 0f;
        while (time < 1f)
        {
            doorHinge.rotation = Quaternion.Slerp(startRotation, endRotation, time);
            yield return null;
            time += Time.deltaTime * speed;
        }
    }
}
