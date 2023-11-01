using UnityEngine;

namespace InventorySystem
{
    public abstract class WearableContainer : Wearable
    {
        [Header("Inventory Layout")]
        [SerializeField] InventoryLayout[] inventorySections = new InventoryLayout[1];

        public InventoryLayout[] InventorySections => inventorySections;
    }
}
