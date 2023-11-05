using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Body Armor", menuName = "Inventory/Body Armor")]
    public class BodyArmor : Armor
    {
        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.BodyArmor;
                initialized = true;
            }
        }
    }
}
