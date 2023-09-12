using UnityEngine;

public class Bag : Wearable
{
    [SerializeField] InventoryLayout[] inventorySections = new InventoryLayout[1];

    public InventoryLayout[] InventorySections => inventorySections;

    public override bool IsBag() => true;
}
