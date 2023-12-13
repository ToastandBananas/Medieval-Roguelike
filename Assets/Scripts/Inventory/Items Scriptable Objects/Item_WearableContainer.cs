using UnityEngine;

namespace InventorySystem
{
    public abstract class Item_WearableContainer : Item_Wearable
    {
        [Header("Inventory Layout")]
        [SerializeField] InventoryLayout[] inventorySections;

        public InventoryLayout[] InventorySections => inventorySections; 
        
        public bool HasAnInventory()
        {
            for (int i = 0; i < InventorySections.Length; i++)
            {
                if (InventorySections[i].AmountOfSlots > 0)
                    return true;
            }

            return false;
        }
    }
}
