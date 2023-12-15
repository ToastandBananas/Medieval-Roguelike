using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Body Armor", menuName = "Inventory/Body Armor")]
    public class Item_BodyArmor : Item_VisibleArmor
    {
        [Header("Body Armor Info")]
        [SerializeField] bool protectsArms;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.BodyArmor;
                initialized = true;
            }
        }

        public bool ProtectsArms => protectsArms;
    }
}
