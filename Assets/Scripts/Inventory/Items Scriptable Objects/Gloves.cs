using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Gloves", menuName = "Inventory/Gloves")]
    public class Gloves : Wearable
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Gloves;
                initialized = true;
            }
        }
    }
}
