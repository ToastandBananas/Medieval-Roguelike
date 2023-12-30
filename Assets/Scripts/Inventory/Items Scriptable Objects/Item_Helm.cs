using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Helm", menuName = "Inventory/Helm")]
    public class Item_Helm : Item_VisibleArmor
    {
        [Header("Helm Info")]
        [SerializeField, Range(0f, 1f)] float fallOffOnDeathChance;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Helm;
                initialized = true;
            }
        }

        public float FallOffOnDeathChance => fallOffOnDeathChance;
    }
}