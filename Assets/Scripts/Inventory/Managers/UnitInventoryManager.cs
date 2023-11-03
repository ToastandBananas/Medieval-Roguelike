using InteractableObjects;
using UnitSystem;
using UnityEngine;

namespace InventorySystem
{
    public class UnitInventoryManager : InventoryManager
    {
        [SerializeField] Unit unit;
        [SerializeField] Inventory mainInventory;
        [SerializeField] ContainerInventoryManager backpackInventoryManager;
        [SerializeField] ContainerInventoryManager beltInventoryManager;
        [SerializeField] ContainerInventoryManager quiverInventoryManager;

        void Awake()
        {
            mainInventory.Initialize();
        }

        public Inventory MainInventory => mainInventory;

        public bool TryAddItemToInventories(ItemData itemData)
        {
            if (itemData == null || itemData.Item == null)
                return false;

            if (unit.UnitEquipment != null)
            {
                if (itemData.Item is Ammunition && quiverInventoryManager != null && unit.UnitEquipment.QuiverEquipped() && quiverInventoryManager.TryAddItem(itemData, unit))
                {
                    if (unit.UnitEquipment.slotVisualsCreated)
                        unit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                    if (itemData.MyInventory != null && itemData.MyInventory is ContainerInventory && itemData.MyInventory.ContainerInventory.LooseItem != null && itemData.MyInventory.ContainerInventory.LooseItem is LooseQuiverItem)
                        itemData.MyInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();

                    return true;
                }
            }

            if (mainInventory.TryAddItem(itemData, unit))
                return true;

            if (unit.UnitEquipment != null)
            {
                if (beltInventoryManager != null && unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Belt] != itemData && beltInventoryManager.TryAddItem(itemData, unit))
                    return true;

                if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped() && unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back] != itemData && backpackInventoryManager.TryAddItem(itemData, unit))
                    return true;
            }

            return false;
        }

        public bool ContainsItemDataInAnyInventory(ItemData itemData)
        {
            if (mainInventory.ItemDatas.Contains(itemData))
                return true;

            if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped())
            {
                if (backpackInventoryManager.ParentInventory.ItemDatas.Contains(itemData))
                    return true;

                for (int i = 0; i < backpackInventoryManager.SubInventories.Length; i++)
                {
                    if (backpackInventoryManager.SubInventories[i].ItemDatas.Contains(itemData))
                        return true;
                }
            }

            if (quiverInventoryManager != null && unit.UnitEquipment.QuiverEquipped())
            {
                if (quiverInventoryManager.ParentInventory.ItemDatas.Contains(itemData))
                    return true;

                for (int i = 0; i < quiverInventoryManager.SubInventories.Length; i++)
                {
                    if (quiverInventoryManager.SubInventories[i].ItemDatas.Contains(itemData))
                        return true;
                }
            }

            return false;
        }

        public bool ContainsMeleeWeaponInAnyInventory(out ItemData bestWeaponItemData)
        {
            bestWeaponItemData = null;
            for (int i = 0; i < mainInventory.ItemDatas.Count; i++)
            {
                if (mainInventory.ItemDatas[i].Item is MeleeWeapon)
                {
                    if (bestWeaponItemData == null || mainInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                        bestWeaponItemData = mainInventory.ItemDatas[i];
                }
            }

            if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped())
            {
                for (int i = 0; i < backpackInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (backpackInventoryManager.ParentInventory.ItemDatas[i].Item is MeleeWeapon)
                    {
                        if (bestWeaponItemData == null || backpackInventoryManager.ParentInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                            bestWeaponItemData = backpackInventoryManager.ParentInventory.ItemDatas[i];
                    }
                }

                for (int subInvIndex = 0; subInvIndex < backpackInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < backpackInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        if (backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Item is MeleeWeapon)
                        {
                            if (bestWeaponItemData == null || backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].IsBetterThan(bestWeaponItemData))
                                bestWeaponItemData = backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i];
                        }
                    }
                }
            }

            if (bestWeaponItemData != null)
                return true;
            return false;
        }

        public ContainerInventoryManager BackpackInventoryManager => backpackInventoryManager;
        public ContainerInventoryManager BeltInventoryManager => beltInventoryManager;
        public ContainerInventoryManager QuiverInventoryManager => quiverInventoryManager;
    }
}
