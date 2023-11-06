using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Shirt", menuName = "Inventory/Shirt")]
    public class Shirt : VisibleWearable
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Shirt;
                initialized = true;
            }
        }
    }
}
