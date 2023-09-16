using UnityEngine;

public class LooseContainerItem : LooseItem
{
    [Header("Container Info")]
    [SerializeField] ContainerInventoryManager containerInventoryManager;

    public override void Interact(Unit unitPickingUpItem)
    {
        if (containerInventoryManager.HasAnyItems())
            InventoryUI.Instance.ShowContainerUI(containerInventoryManager, itemData.Item);
        else if (unitPickingUpItem.TryAddItemToInventories(itemData))
            gameObject.SetActive(false);
    }

    public void TransferInventory(ContainerInventoryManager containerInventoryManagerToCopy)
    {
        ContainerInventory currentParentInventory = containerInventoryManager.ParentInventory;
        ContainerInventory[] currentSubInventories = containerInventoryManager.SubInventories;

        // Set the ContainerInventory references
        containerInventoryManager.SetParentInventory(containerInventoryManagerToCopy.ParentInventory);
        containerInventoryManager.SetSubInventories(containerInventoryManagerToCopy.SubInventories);

        containerInventoryManagerToCopy.SetParentInventory(currentParentInventory);
        containerInventoryManagerToCopy.SetSubInventories(currentSubInventories);

        // Set the ContainerInventoryManager references for each ContainerInventory
        containerInventoryManager.ParentInventory.SetContainerInventoryManager(containerInventoryManager);
        for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
        {
            containerInventoryManager.SubInventories[i].SetContainerInventoryManager(containerInventoryManager);
        }

        containerInventoryManagerToCopy.ParentInventory.SetContainerInventoryManager(containerInventoryManagerToCopy);
        for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
        {
            containerInventoryManagerToCopy.SubInventories[i].SetContainerInventoryManager(containerInventoryManagerToCopy);
        }

        if (containerInventoryManager.ParentInventory.HasBeenInitialized == false)
            containerInventoryManager.Initialize();

        if (containerInventoryManagerToCopy.ParentInventory.HasBeenInitialized == false)
            containerInventoryManagerToCopy.Initialize();
    }

    public ContainerInventoryManager ContainerInventoryManager => containerInventoryManager;

    public void SetContainerInventoryManager(ContainerInventoryManager newContainerInventoryManager) => containerInventoryManager = newContainerInventoryManager;
}
