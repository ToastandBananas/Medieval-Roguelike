using UnityEngine;

public class LooseContainerItem : LooseItem
{
    [Header("Container Info")]
    [SerializeField] ContainerInventoryManager containerInventoryManager;

    public override void Interact(Unit unitPickingUpItem)
    {
        if (containerInventoryManager.HasAnyItems())
            InventoryUI.Instance.ShowContainerUI(containerInventoryManager, itemData.Item);
        else
        {
            if (InventoryUI.Instance.GetContainerUI(containerInventoryManager) != null)
                InventoryUI.Instance.GetContainerUI(containerInventoryManager).CloseContainerInventory();
            
            if (unitPickingUpItem.TryAddItemToInventories(itemData))
                gameObject.SetActive(false);
        }
    }

    public void TransferInventory(ContainerInventoryManager containerInventoryManagerToCopy)
    {
        containerInventoryManager.ParentInventory.RemoveSlots();
        containerInventoryManagerToCopy.ParentInventory.RemoveSlots();

        ContainerInventory currentParentInventory = containerInventoryManager.ParentInventory;
        ContainerInventory[] currentSubInventories = containerInventoryManager.SubInventories;
        LooseItem currentLooseItem = containerInventoryManager.ParentInventory.LooseItem;
        Unit currentUnit = containerInventoryManager.ParentInventory.MyUnit;

        // Set Unit & LooseItem references
        containerInventoryManager.ParentInventory.SetUnit(containerInventoryManagerToCopy.ParentInventory.MyUnit);
        containerInventoryManager.ParentInventory.SetLooseItem(containerInventoryManagerToCopy.ParentInventory.LooseItem);
        for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
        {
            containerInventoryManager.SubInventories[i].SetUnit(containerInventoryManagerToCopy.ParentInventory.MyUnit);
            containerInventoryManager.SubInventories[i].SetLooseItem(containerInventoryManagerToCopy.ParentInventory.LooseItem);
        }

        containerInventoryManagerToCopy.ParentInventory.SetUnit(currentUnit);
        containerInventoryManagerToCopy.ParentInventory.SetLooseItem(currentLooseItem);
        for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
        {
            containerInventoryManagerToCopy.SubInventories[i].SetUnit(currentUnit);
            containerInventoryManagerToCopy.SubInventories[i].SetLooseItem(currentLooseItem);
        }

        // Set ContainerInventory references
        containerInventoryManager.SetParentInventory(containerInventoryManagerToCopy.ParentInventory);
        containerInventoryManager.SetSubInventories(containerInventoryManagerToCopy.SubInventories);

        containerInventoryManagerToCopy.SetParentInventory(currentParentInventory);
        containerInventoryManagerToCopy.SetSubInventories(currentSubInventories);
        
        // Initialize if needed
        if (containerInventoryManager.ParentInventory.HasBeenInitialized == false)
            containerInventoryManager.Initialize();
        else
        {
            // Set ContainerInventoryManager references
            containerInventoryManager.ParentInventory.SetContainerInventoryManager(containerInventoryManager);
            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                containerInventoryManager.SubInventories[i].SetContainerInventoryManager(containerInventoryManager);
            }

            // Setup the items (sets up slot coordinates and item datas
            containerInventoryManager.ParentInventory.SetupItems();
            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                containerInventoryManager.SubInventories[i].SetupItems();
            }
        }

        // Initialize if needed
        if (containerInventoryManagerToCopy.ParentInventory.HasBeenInitialized == false)
            containerInventoryManagerToCopy.Initialize();
        else
        {
            // Set ContainerInventoryManager references
            containerInventoryManagerToCopy.ParentInventory.SetContainerInventoryManager(containerInventoryManagerToCopy);
            for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
            {
                containerInventoryManagerToCopy.SubInventories[i].SetContainerInventoryManager(containerInventoryManagerToCopy);
            }

            // Setup the items (sets up slot coordinates and item datas
            containerInventoryManagerToCopy.ParentInventory.SetupItems();
            for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
            {
                containerInventoryManagerToCopy.SubInventories[i].SetupItems();
            }
        }
    }

    public ContainerInventoryManager ContainerInventoryManager => containerInventoryManager;

    public void SetContainerInventoryManager(ContainerInventoryManager newContainerInventoryManager) => containerInventoryManager = newContainerInventoryManager;
}
