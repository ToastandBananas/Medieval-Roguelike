using InteractableObjects;
using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace InventorySystem
{
    public class InventoryManager_Humanoid : InventoryManager_Unit
    {
        [SerializeField] InventoryManager_Container backpackInventoryManager;
        [SerializeField] InventoryManager_Container beltInventoryManager;
        [SerializeField] InventoryManager_Container quiverInventoryManager;

        public override bool TryAddItemToInventories(ItemData itemData)
        {
            if (itemData == null || itemData.Item == null)
                return false;

            if (itemData.Item is Item_Ammunition && quiverInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.QuiverEquipped && quiverInventoryManager.TryAddItem(itemData, unit))
            {
                if (unit.UnitEquipment.SlotVisualsCreated)
                    unit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                if (itemData.MyInventory != null && itemData.MyInventory is ContainerInventory && itemData.MyInventory.ContainerInventory.LooseItem != null && itemData.MyInventory.ContainerInventory.LooseItem is Interactable_LooseQuiverItem)
                    itemData.MyInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();

                return true;
            }

            if (Action_Throw.IsThrowingWeapon(itemData.Item.ItemType) && beltInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped)
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
            if (beltInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped && unit.UnitEquipment.EquippedItemData(EquipSlot.Belt) != itemData && beltInventoryManager.TryAddItem(itemData, unit))
                return true;

            if (backpackInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BackpackEquipped && unit.UnitEquipment.EquippedItemData(EquipSlot.Back) != itemData && backpackInventoryManager.TryAddItem(itemData, unit))
                return true;

            return false;
        }

        public override bool ContainsItemDataInAnyInventory(ItemData itemData)
        {
            if (mainInventory.ItemDatas.Contains(itemData))
                return true;

            if (backpackInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BackpackEquipped)
            {
                if (backpackInventoryManager.ParentInventory.ItemDatas.Contains(itemData))
                    return true;

                for (int i = 0; i < backpackInventoryManager.SubInventories.Length; i++)
                {
                    if (backpackInventoryManager.SubInventories[i].ItemDatas.Contains(itemData))
                        return true;
                }
            }

            if (beltInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped)
            {
                if (beltInventoryManager.ParentInventory.ItemDatas.Contains(itemData))
                    return true;

                for (int i = 0; i < beltInventoryManager.SubInventories.Length; i++)
                {
                    if (beltInventoryManager.SubInventories[i].ItemDatas.Contains(itemData))
                        return true;
                }
            }

            if (quiverInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.QuiverEquipped)
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
                if (mainInventory.ItemDatas[i].Item is Item_MeleeWeapon && !mainInventory.ItemDatas[i].IsBroken)
                {
                    if (bestWeaponItemData == null || mainInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                        bestWeaponItemData = mainInventory.ItemDatas[i];
                }
            }

            if (backpackInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BackpackEquipped)
            {
                for (int i = 0; i < backpackInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (backpackInventoryManager.ParentInventory.ItemDatas[i].Item is Item_MeleeWeapon && !backpackInventoryManager.ParentInventory.ItemDatas[i].IsBroken)
                    {
                        if (bestWeaponItemData == null || backpackInventoryManager.ParentInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                            bestWeaponItemData = backpackInventoryManager.ParentInventory.ItemDatas[i];
                    }
                }

                for (int subInvIndex = 0; subInvIndex < backpackInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < backpackInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        if (backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Item is Item_MeleeWeapon && !backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].IsBroken)
                        {
                            if (bestWeaponItemData == null || backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i].IsBetterThan(bestWeaponItemData))
                                bestWeaponItemData = backpackInventoryManager.SubInventories[subInvIndex].ItemDatas[i];
                        }
                    }
                }
            }

            if (beltInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped)
            {
                for (int i = 0; i < beltInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (beltInventoryManager.ParentInventory.ItemDatas[i].Item is Item_MeleeWeapon && !beltInventoryManager.ParentInventory.ItemDatas[i].IsBroken)
                    {
                        if (bestWeaponItemData == null || beltInventoryManager.ParentInventory.ItemDatas[i].IsBetterThan(bestWeaponItemData))
                            bestWeaponItemData = beltInventoryManager.ParentInventory.ItemDatas[i];
                    }
                }

                for (int subInvIndex = 0; subInvIndex < beltInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    for (int i = 0; i < beltInventoryManager.SubInventories[subInvIndex].ItemDatas.Count; i++)
                    {
                        if (beltInventoryManager.SubInventories[subInvIndex].ItemDatas[i].Item is Item_MeleeWeapon && !beltInventoryManager.SubInventories[subInvIndex].ItemDatas[i].IsBroken)
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

        public override float GetTotalInventoryWeight()
        {
            float weight = 0f;
            for (int i = 0; i < mainInventory.ItemDatas.Count; i++)
                weight += mainInventory.ItemDatas[i].Weight();

            if (backpackInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BackpackEquipped)
                weight += backpackInventoryManager.GetTotalInventoryWeight() * UnitEquipment.equippedWeightFactor;

            if (beltInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped)
                weight += beltInventoryManager.GetTotalInventoryWeight() * UnitEquipment.equippedWeightFactor;

            if (quiverInventoryManager != null && unit.UnitEquipment.HumanoidEquipment.QuiverEquipped)
                weight += quiverInventoryManager.GetTotalInventoryWeight() * UnitEquipment.equippedWeightFactor;
            return weight;
        }

        public override InventoryManager_Container GetContainerInventoryManager(EquipSlot equipSlot)
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

        public InventoryManager_Container BackpackInventoryManager => backpackInventoryManager;
        public InventoryManager_Container BeltInventoryManager => beltInventoryManager;
        public InventoryManager_Container QuiverInventoryManager => quiverInventoryManager;
    }
}
