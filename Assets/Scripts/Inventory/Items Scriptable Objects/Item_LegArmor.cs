using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Leg Armor", menuName = "Inventory/Leg Armor")]
    public class Item_LegArmor : Item_Armor
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Legs;
                initialized = true;
            }
        }
    }
}
