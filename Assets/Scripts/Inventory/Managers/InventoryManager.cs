using UnityEngine;

namespace InventorySystem
{
    public abstract class InventoryManager : MonoBehaviour
    {
        public InventoryManager_Unit UnitInventoryManager => this as InventoryManager_Unit;

        public abstract bool ContainsItemData(ItemData itemData);

        public abstract bool AllowedItemTypeContains(ItemType[] itemTypes);

        public abstract float GetTotalInventoryWeight();

        public InventoryManager_Container ContainerInventoryManager => this as InventoryManager_Container;
    }
}
