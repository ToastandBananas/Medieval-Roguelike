namespace InventorySystem
{
    public class ContainerEquipmentSlot : EquipmentSlot
    {
        public ContainerInventoryManager containerInventoryManager { get; private set; }

        public void SetContainerInventoryManager(ContainerInventoryManager containerInventoryManager) => this.containerInventoryManager = containerInventoryManager;
    }
}
