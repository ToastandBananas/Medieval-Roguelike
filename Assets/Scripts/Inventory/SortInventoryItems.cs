using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class SortInventoryItems : MonoBehaviour
    {
        [SerializeField] InventoryManager inventoryManager;

        List<ItemData> itemsToSort = new List<ItemData>();

        void Start()
        {
            if (inventoryManager == null)
                Debug.LogWarning($"Inventory Manager for {name} is not set...");
        }

        public void SortItems()
        {
            if (inventoryManager is InventoryManager_Unit)
            {
                Sort(inventoryManager.UnitInventoryManager.MainInventory);
            }
            else if (inventoryManager is InventoryManager_Container)
            {
                Sort(inventoryManager.ContainerInventoryManager.ParentInventory);

                for (int i = 0; i < inventoryManager.ContainerInventoryManager.SubInventories.Length; i++)
                {
                    Sort(inventoryManager.ContainerInventoryManager.SubInventories[i]);
                }
            }
        }

        void Sort(Inventory inventory)
        {
            if (inventory == null || inventory.ItemDatas.Count == 0)
            {
                Debug.Log("No items");
                itemsToSort.Clear();
                return;
            }

            InventoryUI.SetLastInventoryInteractedWith(inventory); // We do this to prevent any InventoryActions being called while sorting

            for (int i = 0; i < inventory.ItemDatas.Count; i++)
            {
                itemsToSort.Add(inventory.ItemDatas[i]);
            }

            itemsToSort.Sort((item1, item2) =>
            {
                // Compare based on criteria:
                // 1. Tallest and widest items come first.
                // 2. Then taller items.
                // 3. Then shorter and wider items.
                // 4. Then short and non-wide items.

                int heightComparison = item2.Item.Height.CompareTo(item1.Item.Height);
                int widthComparison = item2.Item.Width.CompareTo(item1.Item.Width);

                if (heightComparison != 0)
                    return heightComparison;
                else if (widthComparison != 0)
                    return widthComparison;
                else
                    return 0; // Items have the same size, no need to change their order.
            });

            for (int i = 0; i < itemsToSort.Count; i++)
            {
                inventory.RemoveItem(itemsToSort[i], true);
            }

            for (int i = 0; i < itemsToSort.Count; i++)
            {
                if (inventory.TryAddItem(itemsToSort[i], inventory.MyUnit, true) == false)
                    DropItemManager.DropItem(null, inventory.MyUnit, itemsToSort[i]);
            }

            InventoryUI.SetLastInventoryInteractedWith(null);
            itemsToSort.Clear();
        }
    }
}
