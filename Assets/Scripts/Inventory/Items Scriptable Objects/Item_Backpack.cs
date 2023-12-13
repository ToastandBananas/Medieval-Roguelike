using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Backpack", menuName = "Inventory/Backpack")]
    public class Item_Backpack : Item_WearableContainer
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Back;
                initialized = true;
            }
        }
    }
}
