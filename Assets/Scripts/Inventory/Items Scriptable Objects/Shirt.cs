using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Shirt", menuName = "Inventory/Shirt")]
    public class Shirt : VisibleArmor
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
