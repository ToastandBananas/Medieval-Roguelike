using UnityEngine;

public class ContainerInventoryManager : InventoryManager
{
    [SerializeField] ContainerInventory parentInventory;
    [SerializeField] ContainerInventory[] subInventories;

    void Awake()
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
                subInventories[i] = new ContainerInventory(parentInventory.MyUnit(), this);
            }
        }
    }

    public ContainerInventory ParentInventory => parentInventory;

    public ContainerInventory[] SubInventories => subInventories;
}
