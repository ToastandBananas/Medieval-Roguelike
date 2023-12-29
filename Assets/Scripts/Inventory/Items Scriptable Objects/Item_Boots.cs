using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Boots", menuName = "Inventory/Boots")]
    public class Item_Boots : Item_Armor
    {
        [Header("Boots Stats")]
        [SerializeField, Range(-5f, 5f)] float moveNoiseModifier;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Boots;
                initialized = true;
            }
        }

        public float MoveNoiseModifier => moveNoiseModifier;
    }
}
