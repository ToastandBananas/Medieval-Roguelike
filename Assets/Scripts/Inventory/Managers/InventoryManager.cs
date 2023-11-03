using UnityEngine;

namespace InventorySystem
{
    public abstract class InventoryManager : MonoBehaviour
    {
        public UnitInventoryManager UnitInventoryManager => this as UnitInventoryManager;

        public ContainerInventoryManager ContainerInventoryManager => this as ContainerInventoryManager;
    }
}
