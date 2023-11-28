using UnityEngine;

namespace InventorySystem
{
    public abstract class InventoryManager : MonoBehaviour
    {
        public UnitInventoryManager UnitInventoryManager => this as UnitInventoryManager;

        public abstract bool ContainsItemData(ItemData itemData);

        public abstract bool AllowedItemTypeContains(ItemType[] itemTypes);

        public ContainerInventoryManager ContainerInventoryManager => this as ContainerInventoryManager;
    }
}
