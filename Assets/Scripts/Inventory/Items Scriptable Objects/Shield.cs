using UnityEngine;

[CreateAssetMenu(fileName = "New Shield", menuName = "Inventory/Shield")]
public class Shield : HeldEquipment
{
    [Header("Block Power")]
    [SerializeField] int minBlockPower = 1;
    [SerializeField] int maxBlockPower = 5;

    [Header("Modifiers")]
    [SerializeField] float blockChanceAddOn = 10f;

    public int MinBlockPower => minBlockPower;
    public int MaxBlockPower => maxBlockPower;

    public float BlockChanceAddOn => blockChanceAddOn;

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
