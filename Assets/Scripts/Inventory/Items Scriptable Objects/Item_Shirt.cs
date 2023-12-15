using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Shirt", menuName = "Inventory/Shirt")]
    public class Item_Shirt : Item_VisibleArmor
    {
        [Header("Body Armor Info")]
        [SerializeField] bool protectsArms;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Shirt;
                initialized = true;
            }
        }

        public bool ProtectsArms => protectsArms;
    }
}
