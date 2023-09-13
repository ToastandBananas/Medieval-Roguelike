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

    public ContainerInventory ParentInventory => parentInventory;

    public ContainerInventory[] SubInventories => subInventories;
}
