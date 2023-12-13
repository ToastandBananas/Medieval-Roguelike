using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Gloves", menuName = "Inventory/Gloves")]
    public class Item_Gloves : Item_Wearable
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
