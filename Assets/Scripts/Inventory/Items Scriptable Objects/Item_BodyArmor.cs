using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Body Armor", menuName = "Inventory/Body Armor")]
    public class Item_BodyArmor : Item_VisibleArmor
    {
        [Header("Body Armor Stats")]
        [SerializeField, Range(-5f, 5f)] float moveNoiseModifier;

        [Header("Secondary Protection")]
        [SerializeField] bool protectsArms;
        [SerializeField] bool protectsLegs;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.BodyArmor;
                initialized = true;
            }
        }

        public float MoveNoiseModifier => moveNoiseModifier;

        public bool ProtectsArms => protectsArms;
        public bool ProtectsLegs => protectsLegs;
    }
}
