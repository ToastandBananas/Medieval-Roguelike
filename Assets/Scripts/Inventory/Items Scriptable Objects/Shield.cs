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
}
