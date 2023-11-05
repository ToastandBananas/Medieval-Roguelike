using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Backpack", menuName = "Inventory/Backpack")]
    public class Backpack : WearableContainer
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
