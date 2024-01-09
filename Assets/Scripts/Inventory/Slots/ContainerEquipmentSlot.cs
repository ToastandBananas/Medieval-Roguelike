namespace InventorySystem
{
    public class ContainerEquipmentSlot : EquipmentSlot
    {
        public InventoryManager_Container containerInventoryManager { get; private set; }

        public void SetContainerInventoryManager(InventoryManager_Container containerInventoryManager) => this.containerInventoryManager = containerInventoryManager;
    }
}
