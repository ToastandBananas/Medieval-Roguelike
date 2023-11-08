using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Belt", menuName = "Inventory/Belt")]
    public class Belt : WearableContainer
    {
        [Header("Belt Pouches")]
        [SerializeField] string[] beltPouchNames;

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Belt;
                initialized = true;
            }
        }

        public string[] BeltPouchNames => beltPouchNames;
    }
}
