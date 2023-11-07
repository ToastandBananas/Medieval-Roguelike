using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Accessory", menuName = "Inventory/Accessory")]
    public class Accessory : Wearable
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Accessory;
                initialized = true;
            }
        }
    }
}
