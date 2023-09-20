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

    public void SetParentInventory(ContainerInventory newParentInventory) => parentInventory = newParentInventory;

    public void SetSubInventories(ContainerInventory[] newSubInventories) => subInventories = newSubInventories;

    public ContainerInventory ParentInventory => parentInventory;

    public ContainerInventory[] SubInventories => subInventories;
}
