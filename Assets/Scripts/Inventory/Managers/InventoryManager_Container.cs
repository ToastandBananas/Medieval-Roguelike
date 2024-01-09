using UnityEngine;
using InteractableObjects;
using UnitSystem;

namespace InventorySystem
{
    public class InventoryManager_Container : InventoryManager
    {
        [SerializeField] ContainerInventory parentInventory;
        [SerializeField] ContainerInventory[] subInventories;

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

        public override float GetTotalInventoryWeight()
        {
            float weight = 0f;
            if (parentInventory != null)
            {
                for (int i = 0; i < parentInventory.ItemDatas.Count; i++)
                    weight += parentInventory.ItemDatas[i].Weight();
            }

            for (int subInvIndex = 0; subInvIndex < subInventories.Length; subInvIndex++)
            {
                for (int i = 0; i < subInventories[subInvIndex].ItemDatas.Count; i++)
                    weight += subInventories[subInvIndex].ItemDatas[i].Weight();
            }

            return weight;
        }

        public void IncreaseSubInventoriesArraySize(int newSize)
        {
            ContainerInventory[] newArray = new ContainerInventory[newSize];

            for (int i = 0; i < subInventories.Length; i++)
                newArray[i] = subInventories[i];

            subInventories = newArray;

            for (int i = 0; i < subInventories.Length; i++)
            {
                if (subInventories[i] == null)
                    subInventories[i] = new ContainerInventory(parentInventory.MyUnit, parentInventory.LooseItem, this);
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

        public bool Contains(ItemData itemData)
        {
            if (parentInventory.ItemDatas.Contains(itemData))
                return true;

            for (int i = 0; i < subInventories.Length; i++)
            {
                if (subInventories[i].ItemDatas.Contains(itemData))
                    return true;
            }
            return false;
        }

        public bool TryAddItem(ItemData itemData, Unit unitAdding)
        {
            if (parentInventory.InventoryLayout.AmountOfSlots > 0 && parentInventory.TryAddItem(itemData, unitAdding))
                return true;

            for (int i = 0; i < subInventories.Length; i++)
            {
                if (subInventories[i].InventoryLayout.AmountOfSlots > 0 && subInventories[i].TryAddItem(itemData, unitAdding))
                    return true;
            }
            return false;
        }

        public void SwapInventories(InventoryManager_Container containerInventoryManagerToCopy)
        {
            parentInventory.RemoveSlots();
            for (int i = 0; i < subInventories.Length; i++)
                subInventories[i].RemoveSlots();

            ContainerInventory currentParentInventory = parentInventory;
            ContainerInventory[] currentSubInventories = subInventories;
            Interactable_LooseItem currentLooseItem = parentInventory.LooseItem;
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
            if (!parentInventory.HasBeenInitialized)
                Initialize();
            else
            {
                // Set ContainerInventoryManager references
                parentInventory.SetContainerInventoryManager(this);
                for (int i = 0; i < subInventories.Length; i++)
                    subInventories[i].SetContainerInventoryManager(this);

                // Setup the items (sets up slot coordinates and item datas
                parentInventory.SetupItems();
                for (int i = 0; i < subInventories.Length; i++)
                    subInventories[i].SetupItems();
            }

            // Initialize if needed
            if (!containerInventoryManagerToCopy.ParentInventory.HasBeenInitialized)
                containerInventoryManagerToCopy.Initialize();
            else
            {
                // Set ContainerInventoryManager references
                containerInventoryManagerToCopy.ParentInventory.SetContainerInventoryManager(containerInventoryManagerToCopy);
                for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
                    containerInventoryManagerToCopy.SubInventories[i].SetContainerInventoryManager(containerInventoryManagerToCopy);

                // Setup the items (sets up slot coordinates and item datas)
                containerInventoryManagerToCopy.ParentInventory.SetupItems();
                for (int i = 0; i < containerInventoryManagerToCopy.SubInventories.Length; i++)
                    containerInventoryManagerToCopy.SubInventories[i].SetupItems();
            }
        }

        public override bool AllowedItemTypeContains(ItemType[] itemTypes)
        {
            if (parentInventory != null && parentInventory.AllowedItemTypeContains(itemTypes))
                return true;

            for (int i = 0; i < subInventories.Length; i++)
            {
                if (subInventories[i].AllowedItemTypeContains(itemTypes))
                    return true;
            }
            return false;
        }

        public override bool ContainsItemData(ItemData itemData)
        {
            if (parentInventory != null && parentInventory.ContainsItemData(itemData))
                return true;

            for (int i = 0; i < subInventories.Length; i++)
            {
                if (subInventories[i].ContainsItemData(itemData))
                    return true;
            }
            return false;
        }

        public void SetLooseItem(Interactable_LooseItem looseItem)
        {
            parentInventory.SetLooseItem(looseItem);
            for (int i = 0; i < subInventories.Length; i++)
            {
                subInventories[i].SetLooseItem(looseItem);
            }
        }

        public bool IsEquippedByPlayer() => this == UnitManager.player.BackpackInventoryManager || this == UnitManager.player.QuiverInventoryManager;

        public void SetParentInventory(ContainerInventory newParentInventory) => parentInventory = newParentInventory;

        public void SetSubInventories(ContainerInventory[] newSubInventories) => subInventories = newSubInventories;

        public ContainerInventory ParentInventory => parentInventory;

        public ContainerInventory[] SubInventories => subInventories;
    }
}