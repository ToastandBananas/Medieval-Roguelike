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
                Inventory itemDatasInventory = itemData.MyInventory();
                if (itemData.Item is Ammunition && unit.UnitEquipment != null && quiverInventoryManager != null && unit.UnitEquipment.QuiverEquipped() && quiverInventoryManager.TryAddItem(itemData, unit))
                {
                    if (unit.UnitEquipment.slotVisualsCreated)
                        unit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                    if (itemDatasInventory != null && itemDatasInventory is ContainerInventory && itemDatasInventory.ContainerInventory.LooseItem != null && itemDatasInventory.ContainerInventory.LooseItem is LooseQuiverItem)
                        itemDatasInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();

                    return true;
                }
            }

            if (mainInventory.TryAddItem(itemData, unit))
                return true;

            if (unit.UnitEquipment != null)
            {
                if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped() && unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back] != itemData && backpackInventoryManager.TryAddItem(itemData, unit))
                    return true;
            }

            return false;
        }

        public bool HasMeleeWeaponInAnyInventory(out ItemData bestWeaponItemData)
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

        public ContainerInventoryManager QuiverInventoryManager => quiverInventoryManager;
    }
}
