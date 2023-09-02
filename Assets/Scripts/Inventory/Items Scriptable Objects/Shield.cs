using UnityEngine;

[CreateAssetMenu(fileName = "New Shield", menuName = "Inventory/Item/Shield")]
public class Shield : Equipment
{
    [Header("Block Power")]
    public int minBlockPower = 1;
    public int maxBlockPower = 5;

    [Header("Modifiers")]
    public float blockChanceAddOn = 10f;

    [Header("Transform Info")]
    [SerializeField] Vector3 idlePosition_Left;
    [SerializeField] Vector3 idlePosition_Right;
    [SerializeField] Vector3 idleRotation_Left;
    [SerializeField] Vector3 idleRotation_Right;

    public Vector3 IdlePosition_LeftHand => idlePosition_Left;

    public Vector3 IdlePosition_RightHand => idlePosition_Right;

    public Vector3 IdleRotation_LeftHand => idleRotation_Left;

    public Vector3 IdleRotation_RightHand => idleRotation_Right;

    public override bool IsBag() => false;

    public override bool IsConsumable() => false;

    public override bool IsEquipment() => true;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => false;

    public override bool IsPortableContainer() => false;

    public override bool IsRangedWeapon() => false;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => true;

    public override bool IsWeapon() => false;

    public override bool IsWearable() => false;
}
