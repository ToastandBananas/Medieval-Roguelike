using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Belt", menuName = "Inventory/Belt")]
    public class Belt : WearableContainer
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Belt;
                initialized = true;
            }
        }
    }
}
