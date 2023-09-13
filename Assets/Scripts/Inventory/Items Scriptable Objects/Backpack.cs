using UnityEngine;

[CreateAssetMenu(fileName = "New Bag", menuName = "Inventory/Item/Backpack")]
public class Backpack : Wearable
{
    [SerializeField] InventoryLayout[] inventorySections = new InventoryLayout[1];

    public InventoryLayout[] InventorySections => inventorySections;

    public override bool IsBackpack() => true;
}
