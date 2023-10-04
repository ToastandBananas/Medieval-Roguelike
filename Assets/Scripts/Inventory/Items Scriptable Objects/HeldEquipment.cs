using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HeldEquipment : Equipment
{
    [Header("Transform Info")]
    [SerializeField] Vector3 idlePosition_Left;
    [SerializeField] Vector3 idlePosition_Right;
    [SerializeField] Vector3 idleRotation_Left;
    [SerializeField] Vector3 idleRotation_Right;

    public Vector3 IdlePosition_LeftHand => idlePosition_Left;

    public Vector3 IdlePosition_RightHand => idlePosition_Right;

    public Vector3 IdleRotation_LeftHand => idleRotation_Left;

    public Vector3 IdleRotation_RightHand => idleRotation_Right;
}
