using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Ring", menuName = "Inventory/Ring")]
    public class Ring : Wearable
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Ring1;
                initialized = true;
            }
        }
    }
}
