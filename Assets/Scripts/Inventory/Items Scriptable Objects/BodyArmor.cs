using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Body Armor", menuName = "Inventory/Body Armor")]
    public class BodyArmor : VisibleArmor
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
