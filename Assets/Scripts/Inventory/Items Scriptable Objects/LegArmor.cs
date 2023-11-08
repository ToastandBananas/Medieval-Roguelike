using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Leg Armor", menuName = "Inventory/Leg Armor")]
    public class LegArmor : Armor
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
