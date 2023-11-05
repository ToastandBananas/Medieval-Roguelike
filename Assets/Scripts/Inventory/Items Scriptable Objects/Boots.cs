using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Boots", menuName = "Inventory/Boots")]
    public class Boots : Wearable
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Boots;
                initialized = true;
            }
        }
    }
}
