using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Shirt", menuName = "Inventory/Shirt")]
    public class Item_Shirt : Item_VisibleArmor
    {
        [Header("Shirt Info")]
        [SerializeField, Range(-5f, 5f)] float moveNoiseModifier;
        [SerializeField] bool protectsArms;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Shirt;
                initialized = true;
            }
        }

        public float MoveNoiseModifier => moveNoiseModifier;

        public bool ProtectsArms => protectsArms;
    }
}
