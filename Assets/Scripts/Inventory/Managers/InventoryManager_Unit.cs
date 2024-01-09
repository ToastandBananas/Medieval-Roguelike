using UnitSystem;
using UnityEngine;

namespace InventorySystem
{
    public class InventoryManager_Unit : InventoryManager
    {
        [SerializeField] protected Unit unit;
        [SerializeField] protected Inventory mainInventory;

        void Awake()
        {
            mainInventory.Initialize();
        }

        public Inventory MainInventory => mainInventory;

        public virtual bool TryAddItemToInventories(ItemData itemData)
        {
            if (itemData == null || itemData.Item == null)
                return false;
            return mainInventory.TryAddItem(itemData, unit);
        }

        public override bool ContainsItemData(ItemData itemData) => mainInventory.ItemDatas.Contains(itemData);

        public virtual bool ContainsItemDataInAnyInventory(ItemData itemData) => mainInventory.ItemDatas.Contains(itemData);

        public override float GetTotalInventoryWeight()
        {
            float weight = 0f;
            for (int i = 0; i < mainInventory.ItemDatas.Count; i++)
                weight += mainInventory.ItemDatas[i].Weight();
            return weight;
        }

        public virtual InventoryManager_Container GetContainerInventoryManager(EquipSlot equipSlot) => null;

        public override bool AllowedItemTypeContains(ItemType[] itemTypes) => mainInventory.AllowedItemTypeContains(itemTypes);

        public InventoryManager_Humanoid HumanoidInventoryManager => this as InventoryManager_Humanoid;
    }
}
