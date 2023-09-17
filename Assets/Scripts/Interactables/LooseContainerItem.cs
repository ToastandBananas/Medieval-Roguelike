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
        ContainerInventory currentParentInventory = containerInventoryManager.ParentInventory;
        ContainerInventory[] currentSubInventories = containerInventoryManager.SubInventories;
        Unit currentUnit = containerInventoryManager.ParentInventory.MyUnit;
        Unit otherContainerUnit = containerInventoryManagerToCopy.ParentInventory.MyUnit;
        LooseItem currentLooseItem = containerInventoryManager.ParentInventory.LooseItem;
        LooseItem otherLooseItem = containerInventoryManagerToCopy.ParentInventory.LooseItem;

        // Set Unit & LooseItem references
        containerInventoryManager.ParentInventory.SetUnit(otherContainerUnit);
        containerInventoryManager.ParentInventory.SetLooseItem(otherLooseItem);
        for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
        {
            containerInventoryManager.SubInventories[i].SetUnit(otherContainerUnit);
            containerInventoryManager.SubInventories[i].SetLooseItem(otherLooseItem);
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

        // Set ContainerInventoryManager references for each ContainerInventory
        /*containerInventoryManager.ParentInventory.SetContainerInventoryManager(containerInventoryManager);
        for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
        {
            containerInventoryManager.SubInventories[i].SetContainerInventoryManager(containerInventoryManager);
        }

        containerInventoryManagerToCopy.ParentInventory.SetContainerInventoryManager(containerInventoryManagerToCopy);
        for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
        {
            containerInventoryManagerToCopy.SubInventories[i].SetContainerInventoryManager(containerInventoryManagerToCopy);
        }*/

        //if (containerInventoryManager.ParentInventory.HasBeenInitialized == false)
            containerInventoryManager.Initialize();

        //if (containerInventoryManagerToCopy.ParentInventory.HasBeenInitialized == false)
            containerInventoryManagerToCopy.Initialize();
    }

    public ContainerInventoryManager ContainerInventoryManager => containerInventoryManager;

    public void SetContainerInventoryManager(ContainerInventoryManager newContainerInventoryManager) => containerInventoryManager = newContainerInventoryManager;
}
