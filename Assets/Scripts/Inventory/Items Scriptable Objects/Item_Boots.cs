using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Boots", menuName = "Inventory/Boots")]
    public class Item_Boots : Item_Armor
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
