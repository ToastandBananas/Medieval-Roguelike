using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Bag", menuName = "Inventory/Backpack")]
    public class Backpack : Wearable
    {
        [Header("Inventory Layout")]
        [SerializeField] InventoryLayout[] inventorySections = new InventoryLayout[6];

        public InventoryLayout[] InventorySections => inventorySections;
    }
}
