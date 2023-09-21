using UnityEngine;

public class ContainerInventoryManager : InventoryManager
{
    [SerializeField] ContainerInventory parentInventory;
    [SerializeField] ContainerInventory[] subInventories = new ContainerInventory[5];

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        parentInventory.SetContainerInventoryManager(this);
        parentInventory.Initialize();
        for (int i = 0; i < subInventories.Length; i++)
        {
            subInventories[i].SetContainerInventoryManager(this);
            subInventories[i].Initialize();
        }
    }

    public void IncreaseSubInventoriesArraySize(int newSize)
    {
        ContainerInventory[] newArray = new ContainerInventory[newSize];

        for (int i = 0; i < subInventories.Length; i++)
        {
            newArray[i] = subInventories[i];
        }

        subInventories = newArray;
        
        for (int i = 0; i < subInventories.Length; i++)
        {
            if (subInventories[i] == null)
            {
                subInventories[i] = new ContainerInventory(parentInventory.MyUnit, parentInventory.LooseItem, this);
            }
        }
    }

    public bool ContainsAnyItems()
    {
        if (parentInventory.ItemDatas.Count > 0)
            return true;

        for (int i = 0; i < subInventories.Length; i++)
        {
            if (subInventories[i].ItemDatas.Count > 0)
                return true;
        }
        return false;
    }

    public void TransferInventory(ContainerInventoryManager containerInventoryManagerToCopy)
    {
        parentInventory.RemoveSlots();
        parentInventory.RemoveSlots();

        ContainerInventory currentParentInventory = parentInventory;
        ContainerInventory[] currentSubInventories = subInventories;
        LooseItem currentLooseItem = parentInventory.LooseItem;
        Unit currentUnit = parentInventory.MyUnit;

        // Set Unit & LooseItem references
        parentInventory.SetUnit(containerInventoryManagerToCopy.ParentInventory.MyUnit);
        parentInventory.SetLooseItem(containerInventoryManagerToCopy.ParentInventory.LooseItem);
        for (int i = 0; i < subInventories.Length; i++)
        {
            subInventories[i].SetUnit(containerInventoryManagerToCopy.ParentInventory.MyUnit);
            subInventories[i].SetLooseItem(containerInventoryManagerToCopy.ParentInventory.LooseItem);
        }

        containerInventoryManagerToCopy.ParentInventory.SetUnit(currentUnit);
        containerInventoryManagerToCopy.ParentInventory.SetLooseItem(currentLooseItem);
        for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
        {
            containerInventoryManagerToCopy.SubInventories[i].SetUnit(currentUnit);
            containerInventoryManagerToCopy.SubInventories[i].SetLooseItem(currentLooseItem);
        }

        // Set ContainerInventory references
        SetParentInventory(containerInventoryManagerToCopy.ParentInventory);
        SetSubInventories(containerInventoryManagerToCopy.SubInventories);

        containerInventoryManagerToCopy.SetParentInventory(currentParentInventory);
        containerInventoryManagerToCopy.SetSubInventories(currentSubInventories);

        // Initialize if needed
        if (parentInventory.HasBeenInitialized == false)
            Initialize();
        else
        {
            // Set ContainerInventoryManager references
            parentInventory.SetContainerInventoryManager(this);
            for (int i = 0; i < subInventories.Length; i++)
            {
                subInventories[i].SetContainerInventoryManager(this);
            }

            // Setup the items (sets up slot coordinates and item datas
            parentInventory.SetupItems();
            for (int i = 0; i < subInventories.Length; i++)
            {
                subInventories[i].SetupItems();
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

    public void SetParentInventory(ContainerInventory newParentInventory) => parentInventory = newParentInventory;

    public void SetSubInventories(ContainerInventory[] newSubInventories) => subInventories = newSubInventories;

    public ContainerInventory ParentInventory => parentInventory;

    public ContainerInventory[] SubInventories => subInventories;
}