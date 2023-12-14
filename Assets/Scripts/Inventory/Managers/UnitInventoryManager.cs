using InteractableObjects;
using UnitSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.Actions;
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

            if (itemData.Item is Item_Ammunition && quiverInventoryManager != null && unit.UnitEquipment.QuiverEquipped() && quiverInventoryManager.TryAddItem(itemData, unit))
            {
                if (unit.UnitEquipment.SlotVisualsCreated)
                    unit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                if (itemData.MyInventory != null && itemData.MyInventory is ContainerInventory && itemData.MyInventory.ContainerInventory.LooseItem != null && itemData.MyInventory.ContainerInventory.LooseItem is Interactable_LooseQuiverItem)
                    itemData.MyInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();

                return true;
            }
                
            if (Action_Throw.IsThrowingWeapon(itemData.Item.ItemType) && beltInventoryManager != null && unit.UnitEquipment.BeltBagEquipped())
            {
                // We need to check each belt bag inventory's allowed item types separately, to ensure that they are specifically throwing weapon inventories, before trying to add the throwing weapon
                if (beltInventoryManager.ParentInventory != null && beltInventoryManager.ParentInventory.AllowedItemTypeContains(Action_Throw.throwingWeaponItemTypes) && beltInventoryManager.ParentInventory.ItemTypeAllowed(itemData.Item.ItemType) && beltInventoryManager.ParentInventory.TryAddItem(itemData, unit))
                    return true;

                for (int i = 0; i < beltInventoryManager.SubInventories.Length; i++)
                {
                    if (beltInventoryManager.SubInventories[i].AllowedItemTypeContains(Action_Throw.throwingWeaponItemTypes) && beltInventoryManager.SubInventories[i].ItemTypeAllowed(itemData.Item.ItemType) && beltInventoryManager.SubInventories[i].TryAddItem(itemData, unit))
                        return true;
                }
            }

            if (mainInventory.TryAddItem(itemData, unit))
                return true;
            
            // Try putting the item in belt bags first so that smaller items favor belt bags
            if (beltInventoryManager != null && unit.UnitEquipment.BeltBagEquipped() && unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Belt] != itemData && beltInventoryManager.TryAddItem(itemData, unit))
                    return true;

            if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped() && unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back] != itemData && backpackInventoryManager.TryAddItem(itemData, unit))
                    return true;

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

            if (beltInventoryManager != null && unit.UnitEquipment.BeltBagEquipped())
            {
                if (beltInventoryManager.ParentInventory.ItemDatas.Contains(itemData))
                    return true;

                for (int i = 0; i < beltInventoryManager.SubInventories.Length; i++)
                {
                    if (beltInventoryManager.SubInventories[i].ItemDatas.Contains(itemData))
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
                if (mainInventory.ItemDatas[i].Item is Item_MeleeWeapon)
                {
                    if (bestWeaponItemData == null || mainInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                        bestWeaponItemData = mainInventory.ItemDatas[i];
                }
            }

            if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped())
            {
                for (int i = 0; i < backpackInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (backpackInventoryManager.ParentInventory.ItemDatas[i].Item is Item_MeleeWeapon)
                    {
                        if (bestWeaponItemData == null || backpackInventoryManager.ParentInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                            bestWeaponItemData = backpackInventoryManager.ParentInventory.ItemDatas[i];
                    }
                }

                for (int subInvIndex = 0; subInvIndex < backpackInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < backpackInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        if (backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Item is Item_MeleeWeapon)
                        {
                            if (bestWeaponItemData == null || backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].IsBetterThan(bestWeaponItemData))
                                bestWeaponItemData = backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i];
                        }
                    }
                }
            }

            if (beltInventoryManager != null && unit.UnitEquipment.BeltBagEquipped())
            {
                for (int i = 0; i < beltInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (beltInventoryManager.ParentInventory.ItemDatas[i].Item is Item_MeleeWeapon)
                    {
                        if (bestWeaponItemData == null || beltInventoryManager.ParentInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                            bestWeaponItemData = beltInventoryManager.ParentInventory.ItemDatas[i];
                    }
                }

                for (int subInvIndex = 0; subInvIndex < beltInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < beltInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        if (beltInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Item is Item_MeleeWeapon)
                        {
                            if (bestWeaponItemData == null || beltInventoryManager.SubInventories[subInvIndex].ItemDatas[i].IsBetterThan(bestWeaponItemData))
                                bestWeaponItemData = beltInventoryManager.SubInventories[subInvIndex].ItemDatas[i];
                        }
                    }
                }
            }

            if (bestWeaponItemData != null)
                return true;
            return false;
        }

        public float GetTotalInventoryWeight()
        {
            float weight = 0f;
            for (int i = 0; i < mainInventory.ItemDatas.Count; i++)
            {
                weight += mainInventory.ItemDatas[i].Weight();
            }

            if (backpackInventoryManager != null && unit.UnitEquipment.BackpackEquipped())
            {
                float backpackItemsWeight = 0f;
                for (int i = 0; i < backpackInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    backpackItemsWeight += backpackInventoryManager.ParentInventory.ItemDatas[i].Weight();
                }

                for (int subInvIndex = 0; subInvIndex < backpackInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < backpackInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        backpackItemsWeight += backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Weight();
                    }
                }

                backpackItemsWeight *= UnitEquipment.equippedWeightFactor;
                weight += backpackItemsWeight;
            }

            if (beltInventoryManager != null && unit.UnitEquipment.BeltBagEquipped())
            {
                float beltItemsWeight = 0f;
                for (int i = 0; i < beltInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    beltItemsWeight += beltInventoryManager.ParentInventory.ItemDatas[i].Weight();
                }

                for (int subInvIndex = 0; subInvIndex < beltInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < beltInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        beltItemsWeight += beltInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Weight();
                    }
                }

                beltItemsWeight *= UnitEquipment.equippedWeightFactor;
                weight += beltItemsWeight;
            }

            if (quiverInventoryManager != null && unit.UnitEquipment.QuiverEquipped())
            {
                float quiverItemsWeight = 0f;
                for (int i = 0; i < quiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    quiverItemsWeight += quiverInventoryManager.ParentInventory.ItemDatas[i].Weight();
                }

                for (int subInvIndex = 0; subInvIndex < quiverInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < quiverInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        quiverItemsWeight += quiverInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Weight();
                    }
                }

                quiverItemsWeight *= UnitEquipment.equippedWeightFactor;
                weight += quiverItemsWeight;
            }

            return weight;
        }

        public ContainerInventoryManager GetContainerInventoryManager(EquipSlot equipSlot)
        {
            if (equipSlot == EquipSlot.Back)
                return backpackInventoryManager;
            else if (equipSlot == EquipSlot.Belt)
                return beltInventoryManager;
            else if (equipSlot == EquipSlot.Quiver)
                return quiverInventoryManager;
            else
            {
                Debug.LogWarning($"{equipSlot} is not a wearable container equip slot");
                return null;
            }    
        }

        public override bool ContainsItemData(ItemData itemData) => mainInventory.ContainsItemData(itemData);

        public override bool AllowedItemTypeContains(ItemType[] itemTypes) => mainInventory.AllowedItemTypeContains(itemTypes);

        public ContainerInventoryManager BackpackInventoryManager => backpackInventoryManager;
        public ContainerInventoryManager BeltInventoryManager => beltInventoryManager;
        public ContainerInventoryManager QuiverInventoryManager => quiverInventoryManager;
    }
}
