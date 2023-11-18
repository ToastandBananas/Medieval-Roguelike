using InteractableObjects;
using System;
using System.Collections.Generic;
using UnitSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using Utilities;
using ContextMenu = GeneralUI.ContextMenu;

namespace InventorySystem
{
    public enum EquipSlot { LeftHeldItem1, RightHeldItem1, LeftHeldItem2, RightHeldItem2, Helm, BodyArmor, Shirt, Gloves, Boots, Back, Quiver, Belt, Legs }
    public enum WeaponSet { One = 1, Two = 2 }

    public class UnitEquipment : MonoBehaviour
    {
        [SerializeField] Unit myUnit;

        [SerializeField] ItemData[] equippedItemDatas = new ItemData[Enum.GetValues(typeof(EquipSlot)).Length];

        [NamedArray(new string[] { "Left Held Item 1", "Right Held Item 1", "Left Held Item 2", "Right Held Item 2", "Helm", "Body Armor", "Shirt", "Gloves", "Boots", "Back", "Quiver", "Belt", "Amulet", "Ring 1", "Ring 2" })]
        [SerializeField] Equipment[] startingEquipment = new Equipment[Enum.GetValues(typeof(EquipSlot)).Length];

        public List<EquipmentSlot> slots { get; private set; }

        public WeaponSet currentWeaponSet { get; private set; }

        public bool slotVisualsCreated { get; private set; }

        public static readonly float equippedWeightFactor = 0.5f; // The weight reduction of an item when it's equipped or inside a bag that's equipped

        void Awake()
        {
            slots = new List<EquipmentSlot>();

            currentWeaponSet = WeaponSet.One;

            // Setup our starting equipment
            for (int i = 0; i < startingEquipment.Length; i++)
            {
                equippedItemDatas[i].SetItem(startingEquipment[i]);
            }

            if (myUnit.IsPlayer)
                CreateSlotVisuals();
            else
                SetupItems();
        }

        public ItemData GetEquippedItemData(EquipSlot equipSlot) => equippedItemDatas[(int)equipSlot];

        public bool CanEquipItem(ItemData newItemData) => CanEquipItemAt(newItemData, GetTargetEquipSlot(newItemData));

        public bool CanEquipItemAt(ItemData newItemData, EquipSlot targetEquipSlot)
        {
            if (newItemData.Item is Equipment == false)
                return false;

            if ((IsHeldItemEquipSlot(newItemData.Item.Equipment.EquipSlot) && IsHeldItemEquipSlot(targetEquipSlot) == false)
                //|| (IsRingEquipSlot(newItemData.Item.Equipment.EquipSlot) && IsRingEquipSlot(targetEquipSlot) == false)
                || (IsHeldItemEquipSlot(newItemData.Item.Equipment.EquipSlot) == false && /*IsRingEquipSlot(newItemData.Item.Equipment.EquipSlot) == false &&*/ newItemData.Item.Equipment.EquipSlot != targetEquipSlot))
                return false;

            if (targetEquipSlot == EquipSlot.Quiver && newItemData.Item is Ammunition)
            {
                int availableSpace = 0;
                ItemData quiverSlotItemData = equippedItemDatas[(int)EquipSlot.Quiver];
                if (QuiverEquipped())
                {
                    // In this case we'd just be replacing the quiver with the different type of ammo (i.e. replacing an arrow quiver with crossbow bolts), so return true
                    if (quiverSlotItemData.Item.Quiver.AllowedProjectileType != newItemData.Item.Ammunition.ProjectileType)
                        return true;

                    for (int i = 0; i < myUnit.QuiverInventoryManager.ParentInventory.InventoryLayout.AmountOfSlots; i++)
                    {
                        SlotCoordinate slotCoord = myUnit.QuiverInventoryManager.ParentInventory.GetSlotCoordinate(i + 1, 1).parentSlotCoordinate;

                        // If there's an empty slot, the ammo will fit
                        if (slotCoord.isFull == false) 
                            return true;
                        else if (newItemData.IsEqual(slotCoord.itemData))
                            availableSpace += slotCoord.itemData.Item.MaxStackSize - slotCoord.itemData.CurrentStackSize;
                    }
                }
                else
                {
                    // If the slot is empty, the ammo will fit
                    if (EquipSlotHasItem(EquipSlot.Quiver) == false)
                        return true;

                    // We would just be replacing the ammo in this case, so return true
                    if (newItemData.IsEqual(equippedItemDatas[(int)EquipSlot.Quiver]) == false)
                        return true;

                    availableSpace = quiverSlotItemData.Item.MaxStackSize - quiverSlotItemData.CurrentStackSize;
                }

                if (availableSpace < newItemData.CurrentStackSize)
                    return false;
            }

            return true;
        }

        public bool TryEquipItem(ItemData newItemData) => TryAddItemAt(GetTargetEquipSlot(newItemData), newItemData);

        public bool TryAddItemAt(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            // If the item is a two-handed weapon, assign the left held item equip slot
            if (newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded)
            {
                if (currentWeaponSet == WeaponSet.One)
                    targetEquipSlot = EquipSlot.LeftHeldItem1;
                else
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
            }

            // If trying to place ammo on a Quiver slot that has a Quiver or the same type of arrows equipped
            if (newItemData.Item is Ammunition && targetEquipSlot == EquipSlot.Quiver && EquipSlotHasItem(EquipSlot.Quiver) && (newItemData.IsEqual(equippedItemDatas[(int)EquipSlot.Quiver]) || (equippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver && newItemData.Item.Ammunition.ProjectileType == equippedItemDatas[(int)EquipSlot.Quiver].Item.Quiver.AllowedProjectileType)))
                TryAddToEquippedAmmunition(newItemData);
            else
                Equip(newItemData, targetEquipSlot);

            return true;
        }

        void Equip(ItemData newItemData, EquipSlot targetEquipSlot)
        {
            if (IsHeldItemEquipSlot(targetEquipSlot))
            {
                EquipSlot oppositeWeaponSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);

                // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                if (EquipSlotHasItem(oppositeWeaponSlot) && ((newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded) || (equippedItemDatas[(int)oppositeWeaponSlot].Item is Weapon && equippedItemDatas[(int)oppositeWeaponSlot].Item.Weapon.IsTwoHanded)))
                {
                    myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(equippedItemDatas[(int)oppositeWeaponSlot], equippedItemDatas[(int)oppositeWeaponSlot].CurrentStackSize, null, InventoryActionType.Unequip);
                    UnequipItem(oppositeWeaponSlot);
                }
            }

            // Unequip any item already in the target equip slot
            if (EquipSlotHasItem(targetEquipSlot))
            {
                ItemData itemDataUnequipping = equippedItemDatas[(int)targetEquipSlot];
                ContainerInventoryManager unequippedContainerInventoryManager = null;
                if (itemDataUnequipping.Item is Backpack)
                    unequippedContainerInventoryManager = myUnit.BackpackInventoryManager;
                else if (itemDataUnequipping.Item is Belt)
                    unequippedContainerInventoryManager = myUnit.BeltInventoryManager;
                else if (itemDataUnequipping.Item is Quiver)
                    unequippedContainerInventoryManager = myUnit.QuiverInventoryManager;

                myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataUnequipping, itemDataUnequipping.CurrentStackSize, unequippedContainerInventoryManager, InventoryActionType.Unequip);
                UnequipItem(targetEquipSlot);
            }
            
            // If the new item is coming from an NPC's Container Equipment Slot or a Loose Container Item, transfer the inventories
            if (IsWearableContainerEquipSlot(targetEquipSlot))
                SwapContainerInventories(targetEquipSlot, newItemData);

            // Clear out the item from it's original slot
            RemoveItemFromOrigin(newItemData);
            newItemData.SetInventorySlotCoordinate(null);

            // Assign the data
            equippedItemDatas[(int)targetEquipSlot] = newItemData;

            if (targetEquipSlot == EquipSlot.RightHeldItem1 && newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem1] = null;
            else if (targetEquipSlot == EquipSlot.RightHeldItem2 && newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem2] = null;

            if (IsWearableContainerEquipSlot(targetEquipSlot))
                InitializeInventories(targetEquipSlot, newItemData);

            // Setup the target slot's item data/sprites and mesh if necessary
            SetupNewItemIcon(GetEquipmentSlot(targetEquipSlot), newItemData);
            SetupEquipmentMesh(targetEquipSlot, newItemData);

            // Set the size of the opportunity attack trigger
            if (IsHeldItemEquipSlot(targetEquipSlot))
                myUnit.opportunityAttackTrigger.UpdateColliderRadius();

            if (myUnit != null)
                myUnit.stats.UpdateCarryWeight();

            if ((currentWeaponSet == WeaponSet.One && (targetEquipSlot == EquipSlot.LeftHeldItem2 || targetEquipSlot == EquipSlot.RightHeldItem2))
                || (currentWeaponSet == WeaponSet.Two && (targetEquipSlot == EquipSlot.LeftHeldItem1 || targetEquipSlot == EquipSlot.RightHeldItem1)))
                return;

            AddActions(newItemData.Item as Equipment);
        }

        void SwapContainerInventories(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            if (InventoryUI.isDraggingItem && InventoryUI.DraggedItem.myUnitEquipment != null)
            {
                if (targetEquipSlot == EquipSlot.Quiver)
                {
                    ContainerEquipmentSlot quiverSlot = InventoryUI.DraggedItem.myUnitEquipment.GetEquipmentSlot(EquipSlot.Quiver) as ContainerEquipmentSlot;
                    if (quiverSlot.GetItemData().Item is Quiver)
                        myUnit.QuiverInventoryManager.SwapInventories(quiverSlot.containerInventoryManager);
                }
                else if (targetEquipSlot == EquipSlot.Back)
                {
                    ContainerEquipmentSlot backpackSlot = InventoryUI.DraggedItem.myUnitEquipment.GetEquipmentSlot(EquipSlot.Back) as ContainerEquipmentSlot;
                    if (backpackSlot.GetItemData().Item is Backpack)
                        myUnit.BackpackInventoryManager.SwapInventories(backpackSlot.containerInventoryManager);
                }
                else if (targetEquipSlot == EquipSlot.Belt)
                {
                    ContainerEquipmentSlot beltSlot = InventoryUI.DraggedItem.myUnitEquipment.GetEquipmentSlot(EquipSlot.Belt) as ContainerEquipmentSlot;
                    myUnit.BeltInventoryManager.SwapInventories(beltSlot.containerInventoryManager);
                }
            }
            else if (ContextMenu.targetSlot != null && ContextMenu.targetSlot is ContainerEquipmentSlot && ContextMenu.targetSlot.InventoryItem.myUnitEquipment != this)
            {
                if (ContextMenu.targetSlot.EquipmentSlot.EquipSlot == EquipSlot.Quiver)
                {
                    ContainerEquipmentSlot quiverSlot = ContextMenu.targetSlot.InventoryItem.myUnitEquipment.GetEquipmentSlot(EquipSlot.Quiver) as ContainerEquipmentSlot;
                    if (quiverSlot.GetItemData().Item is Quiver)
                    {
                        if (quiverSlot.containerInventoryManager.ParentInventory.slotVisualsCreated)
                            InventoryUI.GetContainerUI(quiverSlot.containerInventoryManager).CloseContainerInventory();

                        UnitManager.player.QuiverInventoryManager.SwapInventories(quiverSlot.containerInventoryManager);
                    }
                }
                else if (ContextMenu.targetSlot.EquipmentSlot.EquipSlot == EquipSlot.Back)
                {
                    ContainerEquipmentSlot backpackSlot = ContextMenu.targetSlot.InventoryItem.myUnitEquipment.GetEquipmentSlot(EquipSlot.Back) as ContainerEquipmentSlot;
                    if (backpackSlot.GetItemData().Item is Backpack)
                    {
                        if (backpackSlot.containerInventoryManager.ParentInventory.slotVisualsCreated)
                            InventoryUI.GetContainerUI(backpackSlot.containerInventoryManager).CloseContainerInventory();

                        UnitManager.player.BackpackInventoryManager.SwapInventories(backpackSlot.containerInventoryManager);
                    }
                }
                else if (ContextMenu.targetSlot.EquipmentSlot.EquipSlot == EquipSlot.Belt)
                {
                    ContainerEquipmentSlot beltSlot = ContextMenu.targetSlot.InventoryItem.myUnitEquipment.GetEquipmentSlot(EquipSlot.Belt) as ContainerEquipmentSlot;
                    if (beltSlot.containerInventoryManager.ParentInventory.slotVisualsCreated)
                        InventoryUI.GetContainerUI(beltSlot.containerInventoryManager).CloseContainerInventory();

                    UnitManager.player.BeltInventoryManager.SwapInventories(beltSlot.containerInventoryManager);
                }
            }
            else if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseContainerItem)
            {
                LooseContainerItem looseContainerItem = ContextMenu.targetInteractable as LooseContainerItem;
                if (looseContainerItem.ContainerInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();

                if (targetEquipSlot == EquipSlot.Quiver)
                {
                    UnitManager.player.QuiverInventoryManager.SwapInventories(looseContainerItem.ContainerInventoryManager);
                    if (UnitManager.player.UnitEquipment.slotVisualsCreated && newItemData.Item is Quiver)
                        UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                }
                else if (targetEquipSlot == EquipSlot.Back)
                    UnitManager.player.BackpackInventoryManager.SwapInventories(looseContainerItem.ContainerInventoryManager);
                else if (targetEquipSlot == EquipSlot.Belt)
                    UnitManager.player.BeltInventoryManager.SwapInventories(looseContainerItem.ContainerInventoryManager);
            }
        }

        public bool TryAddToEquippedAmmunition(ItemData ammoItemData)
        {
            if (EquipSlotHasItem(EquipSlot.Quiver) == false || ammoItemData.Item is Ammunition == false)
                return false;

            if (ammoItemData.MyInventory != null && ammoItemData.MyInventory == myUnit.QuiverInventoryManager.ParentInventory)
            {
                if (InventoryUI.isDraggingItem)
                    InventoryUI.ReplaceDraggedItem();

                return false;
            }

            // If there's a quiver we can add the ammo to
            if (equippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver && ammoItemData.Item.Ammunition.ProjectileType == equippedItemDatas[(int)EquipSlot.Quiver].Item.Quiver.AllowedProjectileType)
            {
                if (myUnit.QuiverInventoryManager.ParentInventory.TryAddItem(ammoItemData, myUnit))
                {
                    if (slotVisualsCreated)
                        GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                    if (ammoItemData.MyInventory != null && myUnit.QuiverInventoryManager.ParentInventory != ammoItemData.MyInventory)
                        RemoveItemFromOrigin(ammoItemData);

                    if (InventoryUI.isDraggingItem)
                        InventoryUI.DisableDraggedItem();

                    return true;
                }
            }
            // If trying to add to an equipped stack of ammo
            else if (ammoItemData.IsEqual(equippedItemDatas[(int)EquipSlot.Quiver]))
            {
                int roomInStack = equippedItemDatas[(int)EquipSlot.Quiver].Item.MaxStackSize - equippedItemDatas[(int)EquipSlot.Quiver].CurrentStackSize;

                // If there's more room in the stack than the new item's current stack size, add it all to the stack
                if (ammoItemData.CurrentStackSize <= roomInStack)
                {
                    equippedItemDatas[(int)EquipSlot.Quiver].AdjustCurrentStackSize(ammoItemData.CurrentStackSize);
                    ammoItemData.SetCurrentStackSize(0);
                }
                else // If the new item's stack size is greater than the amount of room in the other item's stack, add what we can
                {
                    equippedItemDatas[(int)EquipSlot.Quiver].AdjustCurrentStackSize(roomInStack);
                    ammoItemData.AdjustCurrentStackSize(-roomInStack);
                }

                GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();

                if (myUnit != null)
                    myUnit.stats.UpdateCarryWeight();

                if (ammoItemData.CurrentStackSize <= 0)
                {
                    RemoveItemFromOrigin(ammoItemData);
                    if (InventoryUI.isDraggingItem)
                        InventoryUI.DisableDraggedItem();

                    return true;
                }
                else
                {
                    // Update the stack size text for the ammo since it wasn't all equipped
                    if (ammoItemData.MyInventory != null && ammoItemData.MyInventory.slotVisualsCreated)
                        ammoItemData.MyInventory.GetSlotFromItemData(ammoItemData).InventoryItem.UpdateStackSizeVisuals();
                    else if (InventoryUI.npcEquipmentSlots[0].UnitEquipment != null && InventoryUI.npcEquipmentSlots[0].UnitEquipment.slotVisualsCreated && InventoryUI.npcEquipmentSlots[0].UnitEquipment.ItemDataEquipped(ammoItemData))
                        InventoryUI.npcEquipmentSlots[0].UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
                    else if (slotVisualsCreated && ItemDataEquipped(ammoItemData))
                        GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
                }
            }

            if (InventoryUI.isDraggingItem)
                InventoryUI.ReplaceDraggedItem();

            return false;
        }

        EquipSlot GetTargetEquipSlot(ItemData newItemData)
        {
            EquipSlot targetEquipSlot = newItemData.Item.Equipment.EquipSlot;
            if (currentWeaponSet == WeaponSet.Two)
            {
                if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
                else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                    targetEquipSlot = EquipSlot.RightHeldItem2;
            }

            if (newItemData.Item is Weapon)
            {
                if (newItemData.Item.Weapon.IsTwoHanded)
                {
                    if (currentWeaponSet == WeaponSet.One)
                        targetEquipSlot = EquipSlot.LeftHeldItem1;
                    else
                        targetEquipSlot = EquipSlot.LeftHeldItem2;
                }
                else if (EquipSlotIsFull(targetEquipSlot))
                {
                    EquipSlot oppositeWeaponEquipSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);
                    if (EquipSlotIsFull(oppositeWeaponEquipSlot) == false)
                        targetEquipSlot = oppositeWeaponEquipSlot;
                }
            }
            return targetEquipSlot;
        }

        public void RemoveEquipment(ItemData itemData)
        {
            int targetEquipSlotIndex = -1;
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (equippedItemDatas[i] == itemData)
                {
                    targetEquipSlotIndex = i;
                    break;
                }
            }

            if (targetEquipSlotIndex != -1)
            {
                if (slotVisualsCreated)
                    GetEquipmentSlotFromIndex(targetEquipSlotIndex).ClearItem();

                RemoveActions(itemData.Item.Equipment, (EquipSlot)targetEquipSlotIndex);
                RemoveEquipmentMesh((EquipSlot)targetEquipSlotIndex);
                equippedItemDatas[targetEquipSlotIndex] = null;

                if (myUnit != null)
                    myUnit.stats.UpdateCarryWeight();

                ActionSystemUI.UpdateActionVisuals();
            }
        }

        void InitializeInventories(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            if (targetEquipSlot == EquipSlot.Back && newItemData.Item is Backpack)
                myUnit.BackpackInventoryManager.Initialize();
            else if (targetEquipSlot == EquipSlot.Belt)
                myUnit.BeltInventoryManager.Initialize();
            else if (targetEquipSlot == EquipSlot.Quiver)
                myUnit.QuiverInventoryManager.Initialize();
        }

        void RemoveItemFromOrigin(ItemData itemDataToRemove)
        {
            // Remove the item from its original character equipment or inventory
            if (InventoryUI.isDraggingItem)
            {
                if (InventoryUI.DraggedItem.myUnitEquipment != null && InventoryUI.DraggedItem.myUnitEquipment.ItemDataEquipped(itemDataToRemove))
                {
                    ContainerInventoryManager itemsContainerInventoryManager = null;
                    if (itemDataToRemove.Item is Backpack)
                        itemsContainerInventoryManager = InventoryUI.DraggedItem.myUnitEquipment.myUnit.BackpackInventoryManager;
                    else if (itemDataToRemove.Item is Belt)
                        itemsContainerInventoryManager = InventoryUI.DraggedItem.myUnitEquipment.myUnit.BeltInventoryManager;
                    else if (itemDataToRemove.Item is Quiver)
                        itemsContainerInventoryManager = InventoryUI.DraggedItem.myUnitEquipment.myUnit.QuiverInventoryManager;

                    // If the Player is removing an Item from a dead Unit's equipment
                    if (InventoryUI.DraggedItem.myUnitEquipment.myUnit.health.IsDead())
                        myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);
                    else // If the Player is removing an Item from a living Unit's equipment, the Unit can remove the item themselves
                        InventoryUI.DraggedItem.myUnitEquipment.myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);

                    InventoryUI.DraggedItem.myUnitEquipment.RemoveEquipment(itemDataToRemove);
                }
                else if (InventoryUI.DraggedItem.myInventory != null && InventoryUI.DraggedItem.myInventory.ItemDatas.Contains(itemDataToRemove))
                {
                    if (InventoryUI.DraggedItem.myInventory.MyUnit != null)
                    {
                        // If the Player is removing an Item from a dead Unit's inventory
                        if (InventoryUI.DraggedItem.myInventory.MyUnit.health.IsDead())
                            myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                        else // If the Player is removing an Item from a living Unit's inventory, the Unit can remove the item themselves
                            InventoryUI.DraggedItem.myInventory.MyUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                    }

                    InventoryUI.DraggedItem.myInventory.RemoveItem(itemDataToRemove, true);
                }
            }
            else if (ContextMenu.targetSlot != null)
            {
                InventoryItem targetInventoryItem = ContextMenu.targetSlot.InventoryItem;

                if (targetInventoryItem.myUnitEquipment != null && targetInventoryItem.myUnitEquipment.ItemDataEquipped(itemDataToRemove))
                {
                    ContainerInventoryManager itemsContainerInventoryManager = null;
                    if (itemDataToRemove.Item is Backpack)
                        itemsContainerInventoryManager = targetInventoryItem.myUnitEquipment.myUnit.BackpackInventoryManager;
                    else if (itemDataToRemove.Item is Belt)
                        itemsContainerInventoryManager = targetInventoryItem.myUnitEquipment.myUnit.BeltInventoryManager;
                    else if (itemDataToRemove.Item is Quiver)
                        itemsContainerInventoryManager = targetInventoryItem.myUnitEquipment.myUnit.QuiverInventoryManager;

                    // If the Player is removing an Item from a dead Unit's equipment
                    if (targetInventoryItem.myUnitEquipment.myUnit.health.IsDead())
                        myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);
                    else // If the Player is removing an Item from a living Unit's equipment, the Unit can remove the item themselves
                        targetInventoryItem.myUnitEquipment.myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);

                    targetInventoryItem.myUnitEquipment.RemoveEquipment(ContextMenu.targetSlot.InventoryItem.itemData);
                }
                else if (targetInventoryItem.myInventory != null && targetInventoryItem.myInventory.ItemDatas.Contains(itemDataToRemove))
                {
                    if (targetInventoryItem.myInventory.MyUnit != null)
                    {
                        // If the Player is removing an Item from a dead Unit's inventory
                        if (targetInventoryItem.myInventory.MyUnit.health.IsDead())
                            myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                        else // If the Player is removing an Item from a living Unit's inventory, the Unit can remove the item themselves
                            targetInventoryItem.myInventory.MyUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                    }

                    targetInventoryItem.myInventory.RemoveItem(itemDataToRemove, true);
                }
            }
            else if (itemDataToRemove.InventorySlotCoordinate != null && itemDataToRemove.InventorySlotCoordinate.myInventory.ContainsItemData(itemDataToRemove))
            {
                if (itemDataToRemove.InventorySlotCoordinate.myInventory.MyUnit != null)
                {
                    // If the Player is removing an Item from a dead Unit's inventory
                    if (itemDataToRemove.InventorySlotCoordinate.myInventory.MyUnit.health.IsDead())
                        myUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                    else // If the Player is removing an Item from a living Unit's inventory, the Unit can remove the item themselves
                        itemDataToRemove.InventorySlotCoordinate.myInventory.MyUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                }

                itemDataToRemove.InventorySlotCoordinate.myInventory.RemoveItem(itemDataToRemove, true);
            }
        }

        public void UnequipItem(EquipSlot equipSlot)
        {
            if (EquipSlotHasItem(equipSlot) == false)
                return;

            Equipment equipment = equippedItemDatas[(int)equipSlot].Item as Equipment;
            RemoveActions(equipment, equipSlot);

            if (GetEquipmentSlot(equipSlot) != InventoryUI.parentSlotDraggedFrom)
            {
                // If this is the Unit's equipped backpack
                if (equipSlot == EquipSlot.Back && equipment is Backpack)
                {
                    if (myUnit.BackpackInventoryManager.ParentInventory.slotVisualsCreated)
                        InventoryUI.GetContainerUI(myUnit.BackpackInventoryManager).CloseContainerInventory();

                    if (myUnit.BackpackInventoryManager.ContainsAnyItems())
                        DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
                }
                else if (equipSlot == EquipSlot.Belt)
                {
                    if (myUnit.BeltInventoryManager.ParentInventory.slotVisualsCreated)
                        InventoryUI.GetContainerUI(myUnit.BeltInventoryManager).CloseContainerInventory();

                    if (myUnit.BeltInventoryManager.ContainsAnyItems())
                        DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
                }
                else if (equipSlot == EquipSlot.Quiver && equipment is Quiver)
                {
                    if (myUnit.QuiverInventoryManager.ParentInventory.slotVisualsCreated)
                        InventoryUI.GetContainerUI(myUnit.QuiverInventoryManager).CloseContainerInventory();

                    if (myUnit.QuiverInventoryManager.ContainsAnyItems())
                        DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
                }
            }

            if (myUnit.UnitInventoryManager.TryAddItemToInventories(equippedItemDatas[(int)equipSlot]))
            {
                if (slotVisualsCreated)
                    GetEquipmentSlot(equipSlot).ClearItem();

                RemoveEquipmentMesh(equipSlot);
            }
            else // Else, drop the item
                DropItemManager.DropItem(this, equipSlot);

            if (slotVisualsCreated && equipSlot == EquipSlot.Quiver && equipment is Quiver)
                GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.HideQuiverSprites();

            equippedItemDatas[(int)equipSlot] = null;

            if (myUnit != null)
                myUnit.stats.UpdateCarryWeight();

            // Set the size of the opportunity attack trigger
            if (IsHeldItemEquipSlot(equipSlot))
                myUnit.opportunityAttackTrigger.UpdateColliderRadius();
        }

        public void CreateSlotVisuals()
        {
            if (slotVisualsCreated)
            {
                Debug.LogWarning($"Slot visuals for {name}, owned by {myUnit.name}, has already been created...");
                return;
            }

            if (myUnit.IsPlayer)
                slots = InventoryUI.playerEquipmentSlots;
            else
                slots = InventoryUI.npcEquipmentSlots;

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetMyCharacterEquipment(this);
                slots[i].InventoryItem.SetMyUnitEquipment(this);

                if (slots[i] is ContainerEquipmentSlot)
                {
                    ContainerEquipmentSlot containerEquipmentSlot = slots[i] as ContainerEquipmentSlot;
                    if (containerEquipmentSlot.EquipSlot == EquipSlot.Back)
                        containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.UnitEquipment.MyUnit.BackpackInventoryManager);
                    else if (containerEquipmentSlot.EquipSlot == EquipSlot.Belt)
                        containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.UnitEquipment.MyUnit.BeltInventoryManager);
                    else if (containerEquipmentSlot.EquipSlot == EquipSlot.Quiver)
                        containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.UnitEquipment.MyUnit.QuiverInventoryManager);
                }
            }

            if (currentWeaponSet == WeaponSet.One)
            {
                GetEquipmentSlot(EquipSlot.RightHeldItem2).HideItemIcon();
                GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = false;

                GetEquipmentSlot(EquipSlot.LeftHeldItem2).HideItemIcon();
                GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = false;

                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) == false)
                {
                    GetEquipmentSlot(EquipSlot.LeftHeldItem1).EnableSlotImage();
                    GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = true;
                }

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) == false)
                {
                    GetEquipmentSlot(EquipSlot.RightHeldItem1).EnableSlotImage();
                    GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = true;
                }
            }
            else
            {
                GetEquipmentSlot(EquipSlot.RightHeldItem1).HideItemIcon();
                GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = false;

                GetEquipmentSlot(EquipSlot.LeftHeldItem1).HideItemIcon();
                GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = false;

                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) == false)
                {
                    GetEquipmentSlot(EquipSlot.LeftHeldItem2).EnableSlotImage();
                    GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = true;
                }

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) == false)
                {
                    GetEquipmentSlot(EquipSlot.RightHeldItem2).EnableSlotImage();
                    GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = true;
                }
            }

            slotVisualsCreated = true;

            SetupItems();
        }

        void SetupItems()
        {
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (EquipSlotHasItem(i) == false)
                    continue;

                EquipSlot targetEquipSlot = (EquipSlot)i;
                equippedItemDatas[i].RandomizeData();

                if (IsHeldItemEquipSlot(targetEquipSlot)
                    && ((equippedItemDatas[i].Item is Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(targetEquipSlot)] != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(targetEquipSlot)].Item != null)
                    || (EquipSlotHasItem((int)GetOppositeWeaponEquipSlot(targetEquipSlot)) && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(targetEquipSlot)].Item is Weapon && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(targetEquipSlot)].Item.Weapon.IsTwoHanded)))
                {
                    Debug.LogWarning($"{myUnit} has 2 two-handed weapons equipped, or a two-handed weapon and a one-handed weapon equipped. That's too many weapons!");
                    RemoveEquipment(equippedItemDatas[i]);
                    continue;
                }
                else if (i == (int)EquipSlot.RightHeldItem1 && equippedItemDatas[i].Item is Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded)
                {
                    targetEquipSlot = EquipSlot.LeftHeldItem1;
                    equippedItemDatas[(int)targetEquipSlot] = equippedItemDatas[i];
                    equippedItemDatas[i] = null;
                }
                else if (i == (int)EquipSlot.RightHeldItem2 && equippedItemDatas[i].Item is Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded)
                {
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
                    equippedItemDatas[(int)targetEquipSlot] = equippedItemDatas[i];
                    equippedItemDatas[i] = null;
                }

                SetupNewItemIcon(GetEquipmentSlot(targetEquipSlot), equippedItemDatas[(int)targetEquipSlot]);
                SetupEquipmentMesh(targetEquipSlot, equippedItemDatas[(int)targetEquipSlot]);

                if ((currentWeaponSet == WeaponSet.One && (targetEquipSlot == EquipSlot.LeftHeldItem2 || targetEquipSlot == EquipSlot.RightHeldItem2))
                || (currentWeaponSet == WeaponSet.Two && (targetEquipSlot == EquipSlot.LeftHeldItem1 || targetEquipSlot == EquipSlot.RightHeldItem1)))
                    continue;

                AddActions(equippedItemDatas[(int)targetEquipSlot].Item as Equipment);
            }

            myUnit.opportunityAttackTrigger.UpdateColliderRadius();
        }

        void AddActions(Equipment equipment)
        {
            if (equipment.ActionTypes.Length == 0)
                return;

            for (int i = 0; i < equipment.ActionTypes.Length; i++)
            {
                if (myUnit.unitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]))
                    continue;

                myUnit.unitActionHandler.AvailableActionTypes.Add(equipment.ActionTypes[i]);
                equipment.ActionTypes[i].GetAction(myUnit);

                if (myUnit.IsPlayer)
                    ActionSystemUI.AddButton(equipment.ActionTypes[i]);
            }

            if (myUnit.IsPlayer)
                ActionSystemUI.UpdateActionVisuals();
        }

        public void RemoveActions(Equipment equipment, EquipSlot equipSlot)
        {
            for (int i = 0; i < equipment.ActionTypes.Length; i++)
            {
                BaseAction action = equipment.ActionTypes[i].GetAction(myUnit);
                if (myUnit.unitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]) == false || (action is MeleeAction && myUnit.stats.CanFightUnarmed)) // Don't remove the basic MeleeAction if this Unit can fight unarmed
                    continue;

                if (IsHeldItemEquipSlot(equipSlot))
                {
                    EquipSlot oppositeEquipSlot = GetOppositeWeaponEquipSlot(equipSlot);
                    if (EquipSlotHasItem(oppositeEquipSlot))
                    {
                        // Don't remove the action if the opposite equipped item has that action
                        bool shouldSkip = false;
                        ItemData oppositeHeldItem = GetEquippedItemData(oppositeEquipSlot);
                        for (int j = 0; j < oppositeHeldItem.Item.Equipment.ActionTypes.Length; j++)
                        {
                            if (oppositeHeldItem.Item.Equipment.ActionTypes[j] == equipment.ActionTypes[i])
                            {
                                shouldSkip = true;
                                break;
                            }
                        }

                        if (shouldSkip)
                            continue;
                    }
                }

                if (myUnit.IsPlayer)
                    ActionSystemUI.RemoveButton(equipment.ActionTypes[i]);

                ActionsPool.ReturnToPool(equipment.ActionTypes[i].GetAction(myUnit));
                myUnit.unitActionHandler.AvailableActionTypes.Remove(equipment.ActionTypes[i]);
            }

            if (myUnit.IsPlayer)
                ActionSystemUI.UpdateActionVisuals();
        }

        public bool EquipSlotIsFull(EquipSlot equipSlot)
        {
            if (EquipSlotHasItem(equipSlot))
                return true;

            if ((equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2) && EquipSlotHasItem(GetOppositeWeaponEquipSlot(equipSlot)) && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item is Weapon && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item.Weapon.IsTwoHanded)
                return true;
            return false;
        }

        public bool EquipSlotHasItem(int equipSlotIndex)
        {
            if (equippedItemDatas[equipSlotIndex] != null && equippedItemDatas[equipSlotIndex].Item != null)
                return true;
            return false;
        }

        public bool EquipSlotHasItem(EquipSlot equipSlot) => EquipSlotHasItem((int)equipSlot);

        public EquipSlot GetOppositeWeaponEquipSlot(EquipSlot weaponEquipSlot)
        {
            if (weaponEquipSlot != EquipSlot.LeftHeldItem1 && weaponEquipSlot != EquipSlot.RightHeldItem1 && weaponEquipSlot != EquipSlot.LeftHeldItem2 && weaponEquipSlot != EquipSlot.RightHeldItem2)
            {
                Debug.LogWarning($"{weaponEquipSlot} is not a weapon slot...");
                return weaponEquipSlot;
            }

            if (weaponEquipSlot == EquipSlot.LeftHeldItem1)
                return EquipSlot.RightHeldItem1;
            else if (weaponEquipSlot == EquipSlot.RightHeldItem1)
                return EquipSlot.LeftHeldItem1;
            else if (weaponEquipSlot == EquipSlot.LeftHeldItem2)
                return EquipSlot.RightHeldItem2;
            else
                return EquipSlot.LeftHeldItem2;
        }

        /*public EquipSlot GetOppositeRingEquipSlot(EquipSlot ringEquipSlot)
        {
            if (ringEquipSlot != EquipSlot.Ring1 && ringEquipSlot != EquipSlot.Ring2)
            {
                Debug.LogWarning($"{ringEquipSlot} is not a ring slot...");
                return ringEquipSlot;
            }

            if (ringEquipSlot == EquipSlot.Ring1)
                return EquipSlot.Ring2;
            else
                return EquipSlot.Ring1;
        }*/

        EquipmentSlot GetEquipmentSlotFromIndex(int index)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (index == (int)slots[i].EquipSlot)
                    return slots[i];
            }
            return null;
        }

        /// <summary>Setup the target slot's item data and sprites.</summary>
        void SetupNewItemIcon(EquipmentSlot targetSlot, ItemData newItemData)
        {
            if (slotVisualsCreated == false)
                return;

            newItemData.SetInventorySlotCoordinate(null);
            targetSlot.InventoryItem.SetItemData(newItemData);
            targetSlot.SetFullSlotSprite();
            targetSlot.ShowSlotImage();
            targetSlot.InventoryItem.UpdateStackSizeVisuals();

            if (targetSlot.IsHeldItemSlot() && targetSlot.InventoryItem.itemData.Item is Weapon && targetSlot.InventoryItem.itemData.Item.Weapon.IsTwoHanded)
            {
                EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();
                oppositeWeaponSlot.SetFullSlotSprite();
                oppositeWeaponSlot.PlaceholderImage.enabled = false;
            }
            else if (targetSlot.EquipSlot == EquipSlot.Quiver && newItemData.Item is Quiver)
                targetSlot.InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

            if ((currentWeaponSet == WeaponSet.Two && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem1 || targetSlot.EquipSlot == EquipSlot.RightHeldItem1))
                || (currentWeaponSet == WeaponSet.One && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem2 || targetSlot.EquipSlot == EquipSlot.RightHeldItem2)))
            {
                targetSlot.DisableSlotImage();
                targetSlot.InventoryItem.DisableIconImage();
                targetSlot.PlaceholderImage.enabled = false;

                EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();
                oppositeWeaponSlot.DisableSlotImage();
                oppositeWeaponSlot.InventoryItem.DisableIconImage();
                oppositeWeaponSlot.PlaceholderImage.enabled = false;
            }
            else if (currentWeaponSet == WeaponSet.One && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem1 || targetSlot.EquipSlot == EquipSlot.RightHeldItem1))
            {
                EquipmentSlot leftHeldItemSlot2 = GetEquipmentSlot(EquipSlot.LeftHeldItem2);
                leftHeldItemSlot2.DisableSlotImage();
                leftHeldItemSlot2.InventoryItem.DisableIconImage();
                leftHeldItemSlot2.PlaceholderImage.enabled = false;

                EquipmentSlot rightHeldItemSlot2 = GetEquipmentSlot(EquipSlot.RightHeldItem2);
                rightHeldItemSlot2.DisableSlotImage();
                rightHeldItemSlot2.InventoryItem.DisableIconImage();
                rightHeldItemSlot2.PlaceholderImage.enabled = false;
            }
            else if (currentWeaponSet == WeaponSet.Two && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem2 || targetSlot.EquipSlot == EquipSlot.RightHeldItem2))
            {
                EquipmentSlot leftHeldItemSlot1 = GetEquipmentSlot(EquipSlot.LeftHeldItem1);
                leftHeldItemSlot1.DisableSlotImage();
                leftHeldItemSlot1.InventoryItem.DisableIconImage();
                leftHeldItemSlot1.PlaceholderImage.enabled = false;

                EquipmentSlot rightHeldItemSlot1 = GetEquipmentSlot(EquipSlot.RightHeldItem1);
                rightHeldItemSlot1.DisableSlotImage();
                rightHeldItemSlot1.InventoryItem.DisableIconImage();
                rightHeldItemSlot1.PlaceholderImage.enabled = false;
            }
        }

        void SetupEquipmentMesh(EquipSlot equipSlot, ItemData itemData)
        {
            if (myUnit.health.IsDead())
                return;

            // We only show meshes for these types of equipment:
            if (IsHeldItemEquipSlot(equipSlot) == false && itemData.Item is VisibleArmor == false)
                return;

            if (EquipSlotIsFull(equipSlot) == false || itemData == null || itemData.Item == null)
                return;

            if ((currentWeaponSet == WeaponSet.One && (equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2))
                || (currentWeaponSet == WeaponSet.Two && (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1)))
                return;

            if (IsHeldItemEquipSlot(equipSlot))
            {
                HeldItem heldItem = null;
                if (itemData.Item is MeleeWeapon)
                    heldItem = HeldItemBasePool.Instance.GetMeleeWeaponBaseFromPool();
                else if (itemData.Item is RangedWeapon)
                    heldItem = HeldItemBasePool.Instance.GetRangedWeaponBaseFromPool();
                else if (itemData.Item is Shield)
                    heldItem = HeldItemBasePool.Instance.GetShieldBaseFromPool();

                heldItem.SetupHeldItem(itemData, myUnit, equipSlot);

                if (myUnit.IsPlayer)
                    myUnit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
            }
            else
                myUnit.unitMeshManager.SetupWearableMesh(equipSlot, (VisibleArmor)itemData.Item);
        }

        public void RemoveEquipmentMesh(EquipSlot equipSlot)
        {
            if (EquipSlotIsFull(equipSlot) == false)
                return;

            // We only show meshes for these types of equipment:
            if (IsHeldItemEquipSlot(equipSlot) == false && equipSlot != EquipSlot.Helm && equipSlot != EquipSlot.BodyArmor && equipSlot != EquipSlot.Shirt)
                return;

            if (IsHeldItemEquipSlot(equipSlot))
            {
                // If the right held item equipSlot was passed in and it's empty, check if the left held item slot has a two hander. If so, that's the item we need to drop.
                if ((equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2) && EquipSlotHasItem(equipSlot) == false)
                {
                    EquipSlot oppositeEquipSlot = GetOppositeWeaponEquipSlot(equipSlot);
                    if (EquipSlotHasItem(oppositeEquipSlot) == false)
                    {
                        Debug.LogWarning("Opposite Equip Slot has no Item...");
                        return;
                    }

                    if (equippedItemDatas[(int)oppositeEquipSlot].Item is Weapon && equippedItemDatas[(int)oppositeEquipSlot].Item.Weapon.IsTwoHanded)
                        equipSlot = oppositeEquipSlot;
                }

                myUnit.unitMeshManager.ReturnHeldItemToPool(equipSlot);

                if (myUnit.IsPlayer)
                    myUnit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
            }
            else
                myUnit.unitMeshManager.RemoveMesh(equipSlot);
        }

        public void SwapWeaponSet()
        {
            if (myUnit.IsPlayer && InventoryUI.isDraggingItem)
                InventoryUI.ReplaceDraggedItem();

            myUnit.unitActionHandler.ClearActionQueue(false);

            if (currentWeaponSet == WeaponSet.One)
            {
                currentWeaponSet = WeaponSet.Two;

                // Remove held item bases for current held items
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                {
                    RemoveActions(equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.Equipment, EquipSlot.LeftHeldItem1);
                    RemoveEquipmentMesh(EquipSlot.LeftHeldItem1);
                }

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1))
                {
                    RemoveActions(equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.Equipment, EquipSlot.RightHeldItem1);
                    RemoveEquipmentMesh(EquipSlot.RightHeldItem1);
                }

                // Create held item bases for the other weapon set
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                {
                    SetupEquipmentMesh(EquipSlot.LeftHeldItem2, equippedItemDatas[(int)EquipSlot.LeftHeldItem2]);
                    AddActions(equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.Equipment);
                }

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2))
                {
                    SetupEquipmentMesh(EquipSlot.RightHeldItem2, equippedItemDatas[(int)EquipSlot.RightHeldItem2]);
                    AddActions(equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.Equipment);
                }

                if (slotVisualsCreated)
                {
                    // Hide current held item icons
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).DisableSlotImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).InventoryItem.DisableIconImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = false;

                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).DisableSlotImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).InventoryItem.DisableIconImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = false;

                    // Show held item icons for the other weapon set
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).EnableSlotImage();
                    if (myUnit.UnitEquipment.EquipSlotIsFull(EquipSlot.LeftHeldItem2))
                    {
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).InventoryItem.EnableIconImage();
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = false;
                    }
                    else
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = true;

                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).EnableSlotImage();
                    if (myUnit.UnitEquipment.EquipSlotIsFull(EquipSlot.RightHeldItem2))
                    {
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).InventoryItem.EnableIconImage();
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = false;
                    }
                    else
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = true;
                }
            }
            else
            {
                currentWeaponSet = WeaponSet.One;

                // Remove held item bases for current held items
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                {
                    RemoveActions(equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.Equipment, EquipSlot.LeftHeldItem2);
                    RemoveEquipmentMesh(EquipSlot.LeftHeldItem2);
                }

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2))
                {
                    RemoveActions(equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.Equipment, EquipSlot.RightHeldItem2);
                    RemoveEquipmentMesh(EquipSlot.RightHeldItem2);
                }

                // Create held item bases for the other weapon set
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                {
                    SetupEquipmentMesh(EquipSlot.LeftHeldItem1, equippedItemDatas[(int)EquipSlot.LeftHeldItem1]);
                    AddActions(equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.Equipment);
                }

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1))
                {
                    SetupEquipmentMesh(EquipSlot.RightHeldItem1, equippedItemDatas[(int)EquipSlot.RightHeldItem1]);
                    AddActions(equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.Equipment);
                }

                if (slotVisualsCreated)
                {
                    // Hide current held item icons
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).DisableSlotImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).InventoryItem.DisableIconImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = false;

                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).DisableSlotImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).InventoryItem.DisableIconImage();
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = false;

                    // Show held item icons for the other weapon set
                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).EnableSlotImage();
                    if (myUnit.UnitEquipment.EquipSlotIsFull(EquipSlot.LeftHeldItem1))
                    {
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).InventoryItem.EnableIconImage();
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = false;
                    }
                    else
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = true;

                    myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).EnableSlotImage();
                    if (myUnit.UnitEquipment.EquipSlotIsFull(EquipSlot.RightHeldItem1))
                    {
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).InventoryItem.EnableIconImage();
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = false;
                    }
                    else
                        myUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = true;
                }
            }

            myUnit.opportunityAttackTrigger.UpdateColliderRadius();
            ActionSystemUI.UpdateActionVisuals();
        }

        public bool InVersatileStance => myUnit.UnitEquipment.MeleeWeaponEquipped && myUnit.unitMeshManager.GetPrimaryHeldMeleeWeapon().currentHeldItemStance == HeldItemStance.Versatile;

        public ItemData GetRangedWeaponFromOtherWeaponSet()
        {
            if (currentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.LeftHeldItem2];

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.RightHeldItem2];
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.LeftHeldItem1];

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.RightHeldItem1];
            }
            return null;
        }

        public bool OtherWeaponSet_IsEmpty()
        {
            if (currentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) == false && EquipSlotHasItem(EquipSlot.RightHeldItem2) == false)
                    return true;
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) == false && EquipSlotHasItem(EquipSlot.RightHeldItem1) == false)
                    return true;
            }
            return false;
        }

        public bool OtherWeaponSet_IsMelee()
        {
            if (currentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is MeleeWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is MeleeWeapon)
                    return true;
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is MeleeWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is MeleeWeapon)
                    return true;
            }
            return false;
        }

        public bool OtherWeaponSet_IsRanged()
        {
            if (currentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is RangedWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is RangedWeapon)
                    return true;
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is RangedWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is RangedWeapon)
                    return true;
            }
            return false;
        }

        public void GetEquippedWeapons(out Weapon primaryWeapon, out Weapon secondaryWeapon)
        {
            HeldMeleeWeapon primaryHeldMeleeWeapon = myUnit.unitMeshManager.GetPrimaryHeldMeleeWeapon();
            HeldMeleeWeapon secondaryHeldMeleeWeapon = myUnit.unitMeshManager.GetLeftHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon != null && secondaryHeldMeleeWeapon != null)
            {
                primaryWeapon = primaryHeldMeleeWeapon.itemData.Item.Weapon;
                secondaryWeapon = secondaryHeldMeleeWeapon.itemData.Item.Weapon;
                return;
            }
            else if (primaryHeldMeleeWeapon != null)
            {
                primaryWeapon = primaryHeldMeleeWeapon.itemData.Item.Weapon;
                secondaryWeapon = null;
                return;
            }
            else if (RangedWeaponEquipped)
            {
                HeldRangedWeapon heldRangedWeapon = myUnit.unitMeshManager.GetHeldRangedWeapon();
                if (heldRangedWeapon != null)
                    primaryWeapon = heldRangedWeapon.itemData.Item.Weapon;
                else
                    primaryWeapon = null;

                secondaryWeapon = null;
                return;
            }

            primaryWeapon = null;
            secondaryWeapon = null;
        }

        public EquipSlot GetEquipSlotFromItemData(ItemData itemData)
        {
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (itemData == equippedItemDatas[i])
                    return (EquipSlot)i;
            }
            return EquipSlot.LeftHeldItem1;
        }

        public bool IsDualWielding => myUnit.unitMeshManager.GetLeftHeldMeleeWeapon() != null && myUnit.unitMeshManager.GetRightHeldMeleeWeapon() != null;

        public bool MeleeWeaponEquipped => myUnit.unitMeshManager.GetPrimaryHeldMeleeWeapon() != null;

        public bool RangedWeaponEquipped => myUnit.unitMeshManager.GetHeldRangedWeapon() != null;

        public bool ShieldEquipped => myUnit.unitMeshManager.GetHeldShield() != null;

        public bool IsUnarmed => (MeleeWeaponEquipped == false && RangedWeaponEquipped == false) || (RangedWeaponEquipped && HasValidAmmunitionEquipped() == false);

        public static bool IsHeldItemEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2;

        public static bool IsWearableContainerEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.Back || equipSlot == EquipSlot.Belt || equipSlot == EquipSlot.Quiver;

        //public static bool IsRingEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.Ring1 || equipSlot == EquipSlot.Ring2;

        public bool BackpackEquipped() => EquipSlotHasItem(EquipSlot.Back) && equippedItemDatas[(int)EquipSlot.Back].Item is Backpack;

        public bool BeltBagEquipped() => EquipSlotHasItem(EquipSlot.Belt) && equippedItemDatas[(int)EquipSlot.Belt].Item.Belt.HasAnInventory();

        public bool QuiverEquipped() => EquipSlotHasItem(EquipSlot.Quiver) && equippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver;

        public bool HasValidAmmunitionEquipped() => HasValidAmmunitionEquipped(RangedWeaponEquipped ? myUnit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon : null);

        public bool HasValidAmmunitionEquipped(RangedWeapon rangedWeapon)
        {
            if (rangedWeapon == null)
                return false;

            if (RangedWeaponEquipped && myUnit.unitMeshManager.GetHeldRangedWeapon().isLoaded)
                return true;

            if (QuiverEquipped())
            {
                for (int i = 0; i < myUnit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (myUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.ProjectileType == rangedWeapon.ProjectileType)
                        return true;
                }
            }
            else if (EquipSlotHasItem(EquipSlot.Quiver))
            {
                if (equippedItemDatas[(int)EquipSlot.Quiver].Item.Ammunition.ProjectileType == rangedWeapon.ProjectileType)
                    return true;
            }

            return false;
        }

        public ItemData GetEquippedProjectile(ProjectileType projectileType)
        {
            if (QuiverEquipped())
            {
                for (int i = 0; i < myUnit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
                {
                    if (myUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.ProjectileType == projectileType)
                        return myUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i];
                }
            }
            else if (EquipSlotHasItem(EquipSlot.Quiver))
            {
                if (equippedItemDatas[(int)EquipSlot.Quiver].Item.Ammunition.ProjectileType == projectileType)
                    return equippedItemDatas[(int)EquipSlot.Quiver];
            }
            return null;
        }

        public void OnReloadProjectile(ItemData itemData)
        {
            itemData.AdjustCurrentStackSize(-1);

            // If there's still some in the stack
            if (itemData.CurrentStackSize > 0)
            {
                // Update stack size visuals
                if (QuiverEquipped() && myUnit.QuiverInventoryManager.ParentInventory.slotVisualsCreated)
                    myUnit.QuiverInventoryManager.ParentInventory.GetSlotFromItemData(itemData).InventoryItem.UpdateStackSizeVisuals();
                else if (EquipSlotHasItem(EquipSlot.Quiver) && slotVisualsCreated)
                    GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
                return;
            }

            // If there's now zero in the stack
            if (QuiverEquipped())
                myUnit.QuiverInventoryManager.ParentInventory.RemoveItem(itemData, true);
            else if (EquipSlotHasItem(EquipSlot.Quiver))
                RemoveEquipment(itemData);
        }

        public float GetTotalEquipmentWeight()
        {
            float weight = 0f;
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (EquipSlotHasItem(i))
                    weight += equippedItemDatas[i].Weight();
            }

            return weight * equippedWeightFactor;
        }

        public EquipmentSlot GetEquipmentSlot(EquipSlot equipSlot)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].EquipSlot == equipSlot)
                    return slots[i];
            }
            return null;
        }

        public bool ItemDataEquipped(ItemData itemData)
        {
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (equippedItemDatas[i] == itemData)
                    return true;
            }
            return false;
        }

        public ItemData[] EquippedItemDatas => equippedItemDatas;

        public Unit MyUnit => myUnit;

        public void OnCloseNPCInventory()
        {
            slotVisualsCreated = false;

            if (slots.Count > 0)
            {
                // Clear out any slots already in the list, so we can start from scratch
                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].RemoveSlotHighlights();
                    slots[i].ClearItem();
                    slots[i].SetMyCharacterEquipment(null);
                    slots[i].InventoryItem.SetMyUnitEquipment(null);
                }
            }
        }
    }
}
