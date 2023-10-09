using UnityEngine;

namespace InventorySystem
{
    public class UnitInventoryManager : InventoryManager
    {
        [SerializeField] Inventory mainInventory;

        void Awake()
        {
            mainInventory.Initialize();
        }

        public Inventory MainInventory => mainInventory;
    }
}
