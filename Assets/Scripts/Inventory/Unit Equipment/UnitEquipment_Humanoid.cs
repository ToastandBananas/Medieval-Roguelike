using InteractableObjects;
using System.Collections.Generic;
using UnitSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.Actions;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using Utilities;
using ContextMenu = GeneralUI.ContextMenu;

namespace InventorySystem
{
    public enum EquipSlot { LeftHeldItem1, RightHeldItem1, LeftHeldItem2, RightHeldItem2, Helm, BodyArmor, Shirt, Gloves, Boots, Back, Quiver, Belt, LegArmor }
    public enum WeaponSet { One = 1, Two = 2 }

    public class UnitEquipment_Humanoid : UnitEquipment
    {
        [NamedArray(new string[] { "Left Held Item 1", "Right Held Item 1", "Left Held Item 2", "Right Held Item 2", "Helm", "Body Armor", "Shirt", "Gloves", "Boots", "Back", "Quiver", "Belt", "Leg Armor" })]
        [SerializeField] Item_Equipment[] startingEquipment = new Item_Equipment[13];

        public WeaponSet CurrentWeaponSet { get; private set; }

        void Awake()
        {
            Slots = new List<EquipmentSlot>();

            CurrentWeaponSet = WeaponSet.One;

            // Setup our starting equipment
            for (int i = 0; i < startingEquipment.Length; i++)
                equippedItemDatas[i].SetItem(startingEquipment[i]); 
            
            if (myUnit.IsPlayer)
                CreateSlotVisuals();
            else
                SetupItems();
        }

        public override ItemData EquippedItemData(EquipSlot equipSlot) => equippedItemDatas[(int)equipSlot];

        public override bool CanEquipItemAt(ItemData newItemData, EquipSlot targetEquipSlot)
        {
            if (newItemData.Item == null || newItemData.Item is Item_Equipment == false || newItemData.IsBroken)
                return false;

            bool isHeldItem = IsHeldItemEquipSlot(newItemData.Item.Equipment.EquipSlot);
            if (isHeldItem && !CapableOfEquippingHeldItem(newItemData, targetEquipSlot, false))
                return false;

            if ((isHeldItem && !IsHeldItemEquipSlot(targetEquipSlot))
                //|| (IsRingEquipSlot(newItemData.Item.Equipment.EquipSlot) && IsRingEquipSlot(targetEquipSlot) == false)
                || (!isHeldItem && /*IsRingEquipSlot(newItemData.Item.Equipment.EquipSlot) == false &&*/ newItemData.Item.Equipment.EquipSlot != targetEquipSlot))
                return false;

            if (targetEquipSlot == EquipSlot.Quiver && newItemData.Item is Item_Ammunition)
            {
                int availableSpace = 0;
                ItemData quiverSlotItemData = equippedItemDatas[(int)EquipSlot.Quiver];
                if (QuiverEquipped)
                {
                    // In this case we'd just be replacing the quiver with the different type of ammo (i.e. replacing an arrow quiver with crossbow bolts), so return true
                    if (quiverSlotItemData.Item.Quiver.AllowedProjectileType != newItemData.Item.Ammunition.ProjectileType)
                        return true;

                    for (int i = 0; i < myUnit.QuiverInventoryManager.ParentInventory.InventoryLayout.AmountOfSlots; i++)
                    {
                        SlotCoordinate slotCoord = myUnit.QuiverInventoryManager.ParentInventory.GetSlotCoordinate(i + 1, 1).ParentSlotCoordinate;

                        // If there's an empty slot, the ammo will fit
                        if (!slotCoord.IsFull)
                            return true;
                        else if (newItemData.IsEqual(slotCoord.ItemData))
                            availableSpace += slotCoord.ItemData.Item.MaxStackSize - slotCoord.ItemData.CurrentStackSize;
                    }
                }
                else
                {
                    // If the slot is empty, the ammo will fit
                    if (!EquipSlotHasItem(EquipSlot.Quiver))
                        return true;

                    // We would just be replacing the ammo in this case, so return true
                    if (!newItemData.IsEqual(equippedItemDatas[(int)EquipSlot.Quiver]))
                        return true;

                    availableSpace = quiverSlotItemData.Item.MaxStackSize - quiverSlotItemData.CurrentStackSize;
                }

                if (availableSpace < newItemData.CurrentStackSize)
                    return false;
            }

            return true;
        }

        public override bool CapableOfEquippingHeldItem(ItemData itemData, EquipSlot targetHeldItemEquipSlot, bool checkOppositeHeldItemSlot)
        {
            if (itemData == null || itemData.Item == null)
                return false;

            BodyPart leftArm = myUnit.HealthSystem.GetBodyPart(BodyPartType.Arm, BodyPartSide.Left);
            BodyPart rightArm = myUnit.HealthSystem.GetBodyPart(BodyPartType.Arm, BodyPartSide.Right);
            BodyPart leftHand = myUnit.HealthSystem.GetBodyPart(BodyPartType.Hand, BodyPartSide.Left);
            BodyPart rightHand = myUnit.HealthSystem.GetBodyPart(BodyPartType.Hand, BodyPartSide.Right);

            if (itemData.Item is Item_Weapon && itemData.Item.Weapon.IsTwoHanded && (leftArm.IsDisabled || rightArm.IsDisabled || leftHand.IsDisabled || rightHand.IsDisabled))
                return false;

            if ((targetHeldItemEquipSlot == EquipSlot.LeftHeldItem1 || targetHeldItemEquipSlot == EquipSlot.LeftHeldItem2) && (leftArm.IsDisabled || leftHand.IsDisabled))
            {
                if (checkOppositeHeldItemSlot)
                    return CapableOfEquippingHeldItem(itemData, GetOppositeHeldItemEquipSlot(targetHeldItemEquipSlot), false);
                return false;
            }

            if ((targetHeldItemEquipSlot == EquipSlot.RightHeldItem1 || targetHeldItemEquipSlot == EquipSlot.RightHeldItem2) && (rightArm.IsDisabled || rightHand.IsDisabled))
            {
                if (checkOppositeHeldItemSlot)
                    return CapableOfEquippingHeldItem(itemData, GetOppositeHeldItemEquipSlot(targetHeldItemEquipSlot), false);
                return false;
            }

            return true;
        }

        public override bool TryAddItemAt(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            // If the item is a two-handed weapon, assign the left held item equip slot
            if (newItemData.Item is Item_Weapon && newItemData.Item.Weapon.IsTwoHanded)
            {
                if (CurrentWeaponSet == WeaponSet.One)
                    targetEquipSlot = EquipSlot.LeftHeldItem1;
                else
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
            }

            // If trying to place ammo on a Quiver slot that has a Quiver or the same type of arrows equipped
            if (newItemData.Item is Item_Ammunition && targetEquipSlot == EquipSlot.Quiver && EquipSlotHasItem(EquipSlot.Quiver)
                && (newItemData.IsEqual(equippedItemDatas[(int)EquipSlot.Quiver]) || (equippedItemDatas[(int)EquipSlot.Quiver].Item is Item_Quiver && newItemData.Item.Ammunition.ProjectileType == equippedItemDatas[(int)EquipSlot.Quiver].Item.Quiver.AllowedProjectileType)))
                TryAddToEquippedAmmunition(newItemData);
            else
                Equip(newItemData, targetEquipSlot);

            return true;
        }

        protected override void Equip(ItemData newItemData, EquipSlot targetEquipSlot)
        {
            base.Equip(newItemData, targetEquipSlot);

            if ((CurrentWeaponSet == WeaponSet.One && (targetEquipSlot == EquipSlot.LeftHeldItem2 || targetEquipSlot == EquipSlot.RightHeldItem2))
                || (CurrentWeaponSet == WeaponSet.Two && (targetEquipSlot == EquipSlot.LeftHeldItem1 || targetEquipSlot == EquipSlot.RightHeldItem1)))
                return;

            AddActions(newItemData.Item as Item_Equipment);
        }

        protected override void UnequipFromTargetEquipSlot(EquipSlot targetEquipSlot)
        {
            if (EquipSlotHasItem(targetEquipSlot))
            {
                ItemData itemDataUnequipping = equippedItemDatas[(int)targetEquipSlot];
                InventoryManager_Container unequippedContainerInventoryManager = null;
                if (itemDataUnequipping.Item is Item_Backpack)
                    unequippedContainerInventoryManager = myUnit.BackpackInventoryManager;
                else if (itemDataUnequipping.Item is Item_Belt)
                    unequippedContainerInventoryManager = myUnit.BeltInventoryManager;
                else if (itemDataUnequipping.Item is Item_Quiver)
                    unequippedContainerInventoryManager = myUnit.QuiverInventoryManager;

                myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataUnequipping, itemDataUnequipping.CurrentStackSize, unequippedContainerInventoryManager, InventoryActionType.Unequip);
                UnequipItem(targetEquipSlot);
            }
        }

        protected override void SwapContainerInventories(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            if (InventoryUI.IsDraggingItem && InventoryUI.DraggedItem.MyUnitEquipment != null)
            {
                if (targetEquipSlot == EquipSlot.Quiver)
                {
                    ContainerEquipmentSlot quiverSlot = InventoryUI.DraggedItem.MyUnitEquipment.GetEquipmentSlot(EquipSlot.Quiver) as ContainerEquipmentSlot;
                    if (quiverSlot.GetItemData().Item is Item_Quiver)
                        myUnit.QuiverInventoryManager.SwapInventories(quiverSlot.containerInventoryManager);
                }
                else if (targetEquipSlot == EquipSlot.Back)
                {
                    ContainerEquipmentSlot backpackSlot = InventoryUI.DraggedItem.MyUnitEquipment.GetEquipmentSlot(EquipSlot.Back) as ContainerEquipmentSlot;
                    if (backpackSlot.GetItemData().Item is Item_Backpack)
                        myUnit.BackpackInventoryManager.SwapInventories(backpackSlot.containerInventoryManager);
                }
                else if (targetEquipSlot == EquipSlot.Belt)
                {
                    ContainerEquipmentSlot beltSlot = InventoryUI.DraggedItem.MyUnitEquipment.GetEquipmentSlot(EquipSlot.Belt) as ContainerEquipmentSlot;
                    myUnit.BeltInventoryManager.SwapInventories(beltSlot.containerInventoryManager);
                }
            }
            else if (ContextMenu.TargetSlot != null && ContextMenu.TargetSlot is ContainerEquipmentSlot && ContextMenu.TargetSlot.InventoryItem.MyUnitEquipment != this)
            {
                if (ContextMenu.TargetSlot.EquipmentSlot.EquipSlot == EquipSlot.Quiver)
                {
                    ContainerEquipmentSlot quiverSlot = ContextMenu.TargetSlot.InventoryItem.MyUnitEquipment.GetEquipmentSlot(EquipSlot.Quiver) as ContainerEquipmentSlot;
                    if (quiverSlot.GetItemData().Item is Item_Quiver)
                    {
                        if (quiverSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
                            InventoryUI.GetContainerUI(quiverSlot.containerInventoryManager).CloseContainerInventory();

                        UnitManager.player.QuiverInventoryManager.SwapInventories(quiverSlot.containerInventoryManager);
                    }
                }
                else if (ContextMenu.TargetSlot.EquipmentSlot.EquipSlot == EquipSlot.Back)
                {
                    ContainerEquipmentSlot backpackSlot = ContextMenu.TargetSlot.InventoryItem.MyUnitEquipment.GetEquipmentSlot(EquipSlot.Back) as ContainerEquipmentSlot;
                    if (backpackSlot.GetItemData().Item is Item_Backpack)
                    {
                        if (backpackSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
                            InventoryUI.GetContainerUI(backpackSlot.containerInventoryManager).CloseContainerInventory();

                        UnitManager.player.BackpackInventoryManager.SwapInventories(backpackSlot.containerInventoryManager);
                    }
                }
                else if (ContextMenu.TargetSlot.EquipmentSlot.EquipSlot == EquipSlot.Belt)
                {
                    ContainerEquipmentSlot beltSlot = ContextMenu.TargetSlot.InventoryItem.MyUnitEquipment.GetEquipmentSlot(EquipSlot.Belt) as ContainerEquipmentSlot;
                    if (beltSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
                        InventoryUI.GetContainerUI(beltSlot.containerInventoryManager).CloseContainerInventory();

                    UnitManager.player.BeltInventoryManager.SwapInventories(beltSlot.containerInventoryManager);
                }
            }
            else if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is Interactable_LooseContainerItem)
            {
                Interactable_LooseContainerItem looseContainerItem = ContextMenu.TargetInteractable as Interactable_LooseContainerItem;
                if (looseContainerItem.ContainerInventoryManager.ParentInventory.SlotVisualsCreated)
                    InventoryUI.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();

                if (targetEquipSlot == EquipSlot.Quiver)
                {
                    UnitManager.player.QuiverInventoryManager.SwapInventories(looseContainerItem.ContainerInventoryManager);
                    if (UnitManager.player.UnitEquipment.SlotVisualsCreated && newItemData.Item is Item_Quiver)
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
            if (EquipSlotHasItem(EquipSlot.Quiver) == false || ammoItemData.Item is Item_Ammunition == false)
                return false;

            if (ammoItemData.MyInventory != null && ammoItemData.MyInventory == myUnit.QuiverInventoryManager.ParentInventory)
            {
                if (InventoryUI.IsDraggingItem)
                    InventoryUI.ReplaceDraggedItem();

                return false;
            }

            // If there's a quiver we can add the ammo to
            if (equippedItemDatas[(int)EquipSlot.Quiver].Item is Item_Quiver && ammoItemData.Item.Ammunition.ProjectileType == equippedItemDatas[(int)EquipSlot.Quiver].Item.Quiver.AllowedProjectileType)
            {
                if (myUnit.QuiverInventoryManager.ParentInventory.TryAddItem(ammoItemData, myUnit))
                {
                    if (SlotVisualsCreated)
                        GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                    if (ammoItemData.MyInventory != null && myUnit.QuiverInventoryManager.ParentInventory != ammoItemData.MyInventory)
                        RemoveItemFromOrigin(ammoItemData);

                    if (InventoryUI.IsDraggingItem)
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

                if (SlotVisualsCreated)
                    GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();

                if (myUnit != null)
                    myUnit.Stats.UpdateCarryWeight();

                if (ammoItemData.CurrentStackSize <= 0)
                {
                    RemoveItemFromOrigin(ammoItemData);
                    if (InventoryUI.IsDraggingItem)
                        InventoryUI.DisableDraggedItem();

                    return true;
                }
                else
                {
                    // Update the stack size text for the ammo since it wasn't all equipped
                    if (ammoItemData.MyInventory != null && ammoItemData.MyInventory.SlotVisualsCreated)
                        ammoItemData.MyInventory.GetSlotFromItemData(ammoItemData).InventoryItem.UpdateStackSizeVisuals();
                    else if (InventoryUI.NpcEquipmentSlots[0].UnitEquipment != null && InventoryUI.NpcEquipmentSlots[0].UnitEquipment.SlotVisualsCreated && InventoryUI.NpcEquipmentSlots[0].UnitEquipment.ItemDataEquipped(ammoItemData))
                        InventoryUI.NpcEquipmentSlots[0].UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
                    else if (SlotVisualsCreated && ItemDataEquipped(ammoItemData))
                        GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
                }
            }

            if (InventoryUI.IsDraggingItem)
                InventoryUI.ReplaceDraggedItem();

            return false;
        }

        protected override EquipSlot GetTargetEquipSlot(ItemData newItemData)
        {
            EquipSlot targetEquipSlot = newItemData.Item.Equipment.EquipSlot;
            if (CurrentWeaponSet == WeaponSet.Two)
            {
                if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
                else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                    targetEquipSlot = EquipSlot.RightHeldItem2;
            }

            if (newItemData.Item is Item_Weapon)
            {
                if (newItemData.Item.Weapon.IsTwoHanded)
                {
                    if (CurrentWeaponSet == WeaponSet.One)
                        targetEquipSlot = EquipSlot.LeftHeldItem1;
                    else
                        targetEquipSlot = EquipSlot.LeftHeldItem2;
                }
                else if (EquipSlotIsFull(targetEquipSlot))
                {
                    EquipSlot oppositeWeaponEquipSlot = GetOppositeHeldItemEquipSlot(targetEquipSlot);
                    if (!EquipSlotIsFull(oppositeWeaponEquipSlot))
                        targetEquipSlot = oppositeWeaponEquipSlot;
                }
            }
            return targetEquipSlot;
        }

        protected override void InitializeInventories(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            if (targetEquipSlot == EquipSlot.Back && newItemData.Item is Item_Backpack)
                myUnit.BackpackInventoryManager.Initialize();
            else if (targetEquipSlot == EquipSlot.Belt)
                myUnit.BeltInventoryManager.Initialize();
            else if (targetEquipSlot == EquipSlot.Quiver)
                myUnit.QuiverInventoryManager.Initialize();
        }

        protected override void RemoveItemFromOrigin(ItemData itemDataToRemove)
        {
            // Remove the item from its original character equipment or inventory
            if (InventoryUI.IsDraggingItem)
            {
                if (InventoryUI.DraggedItem.MyUnitEquipment != null && InventoryUI.DraggedItem.MyUnitEquipment.ItemDataEquipped(itemDataToRemove))
                {
                    InventoryManager_Container itemsContainerInventoryManager = null;
                    if (itemDataToRemove.Item is Item_Backpack)
                        itemsContainerInventoryManager = InventoryUI.DraggedItem.MyUnitEquipment.MyUnit.BackpackInventoryManager;
                    else if (itemDataToRemove.Item is Item_Belt)
                        itemsContainerInventoryManager = InventoryUI.DraggedItem.MyUnitEquipment.MyUnit.BeltInventoryManager;
                    else if (itemDataToRemove.Item is Item_Quiver)
                        itemsContainerInventoryManager = InventoryUI.DraggedItem.MyUnitEquipment.MyUnit.QuiverInventoryManager;

                    // If the Player is removing an Item from a dead Unit's equipment
                    if (InventoryUI.DraggedItem.MyUnitEquipment.MyUnit.HealthSystem.IsDead)
                        myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);
                    else // If the Player is removing an Item from a living Unit's equipment, the Unit can remove the item themselves
                        InventoryUI.DraggedItem.MyUnitEquipment.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);

                    InventoryUI.DraggedItem.MyUnitEquipment.RemoveEquipment(itemDataToRemove);
                }
                else if (InventoryUI.DraggedItem.MyInventory != null && InventoryUI.DraggedItem.MyInventory.ItemDatas.Contains(itemDataToRemove))
                {
                    if (InventoryUI.DraggedItem.MyInventory.MyUnit != null)
                    {
                        // If the Player is removing an Item from a dead Unit's inventory
                        if (InventoryUI.DraggedItem.MyInventory.MyUnit.HealthSystem.IsDead)
                            myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                        else // If the Player is removing an Item from a living Unit's inventory, the Unit can remove the item themselves
                            InventoryUI.DraggedItem.MyInventory.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                    }

                    InventoryUI.DraggedItem.MyInventory.RemoveItem(itemDataToRemove, true);
                }
            }
            else if (ContextMenu.TargetSlot != null)
            {
                InventoryItem targetInventoryItem = ContextMenu.TargetSlot.InventoryItem;

                if (targetInventoryItem.MyUnitEquipment != null && targetInventoryItem.MyUnitEquipment.ItemDataEquipped(itemDataToRemove))
                {
                    InventoryManager_Container itemsContainerInventoryManager = null;
                    if (itemDataToRemove.Item is Item_Backpack)
                        itemsContainerInventoryManager = targetInventoryItem.MyUnitEquipment.MyUnit.BackpackInventoryManager;
                    else if (itemDataToRemove.Item is Item_Belt)
                        itemsContainerInventoryManager = targetInventoryItem.MyUnitEquipment.MyUnit.BeltInventoryManager;
                    else if (itemDataToRemove.Item is Item_Quiver)
                        itemsContainerInventoryManager = targetInventoryItem.MyUnitEquipment.MyUnit.QuiverInventoryManager;

                    // If the Player is removing an Item from a dead Unit's equipment
                    if (targetInventoryItem.MyUnitEquipment.MyUnit.HealthSystem.IsDead)
                        myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);
                    else // If the Player is removing an Item from a living Unit's equipment, the Unit can remove the item themselves
                        targetInventoryItem.MyUnitEquipment.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);

                    targetInventoryItem.MyUnitEquipment.RemoveEquipment(ContextMenu.TargetSlot.InventoryItem.ItemData);
                }
                else if (targetInventoryItem.MyInventory != null && targetInventoryItem.MyInventory.ItemDatas.Contains(itemDataToRemove))
                {
                    if (targetInventoryItem.MyInventory.MyUnit != null)
                    {
                        // If the Player is removing an Item from a dead Unit's inventory
                        if (targetInventoryItem.MyInventory.MyUnit.HealthSystem.IsDead)
                            myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                        else // If the Player is removing an Item from a living Unit's inventory, the Unit can remove the item themselves
                            targetInventoryItem.MyInventory.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                    }

                    targetInventoryItem.MyInventory.RemoveItem(itemDataToRemove, true);
                }
            }
            else if (itemDataToRemove.InventorySlotCoordinate != null && itemDataToRemove.InventorySlotCoordinate.MyInventory.ContainsItemData(itemDataToRemove))
            {
                if (itemDataToRemove.InventorySlotCoordinate.MyInventory.MyUnit != null)
                {
                    // If the Player is removing an Item from a dead Unit's inventory
                    if (itemDataToRemove.InventorySlotCoordinate.MyInventory.MyUnit.HealthSystem.IsDead)
                        myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                    else // If the Player is removing an Item from a living Unit's inventory, the Unit can remove the item themselves
                        itemDataToRemove.InventorySlotCoordinate.MyInventory.MyUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemDataToRemove, itemDataToRemove.CurrentStackSize, null);
                }

                itemDataToRemove.InventorySlotCoordinate.MyInventory.RemoveItem(itemDataToRemove, true);
            }
        }

        public override void UnequipItem(EquipSlot equipSlot)
        {
            if (!EquipSlotHasItem(equipSlot))
                return;

            Item_Equipment equipment = equippedItemDatas[(int)equipSlot].Item as Item_Equipment;
            RemoveActions(equipment, equipSlot);

            if (GetEquipmentSlot(equipSlot) != InventoryUI.ParentSlotDraggedFrom)
            {
                // If this is the Unit's equipped backpack
                if (equipSlot == EquipSlot.Back && equipment is Item_Backpack)
                {
                    if (myUnit.BackpackInventoryManager.ParentInventory.SlotVisualsCreated)
                        InventoryUI.GetContainerUI(myUnit.BackpackInventoryManager).CloseContainerInventory();

                    if (myUnit.BackpackInventoryManager.ContainsAnyItems())
                        DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
                }
                else if (equipSlot == EquipSlot.Belt)
                {
                    if (myUnit.BeltInventoryManager.ParentInventory.SlotVisualsCreated)
                        InventoryUI.GetContainerUI(myUnit.BeltInventoryManager).CloseContainerInventory();

                    if (myUnit.BeltInventoryManager.ContainsAnyItems())
                        DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
                }
                else if (equipSlot == EquipSlot.Quiver && equipment is Item_Quiver)
                {
                    if (myUnit.QuiverInventoryManager.ParentInventory.SlotVisualsCreated)
                        InventoryUI.GetContainerUI(myUnit.QuiverInventoryManager).CloseContainerInventory();

                    if (myUnit.QuiverInventoryManager.ContainsAnyItems())
                        DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
                }
            }

            if (myUnit.UnitInventoryManager.TryAddItemToInventories(equippedItemDatas[(int)equipSlot]))
            {
                if (SlotVisualsCreated)
                    GetEquipmentSlot(equipSlot).ClearItem();

                RemoveEquipmentMesh(equipSlot);
            }
            else // Else, drop the item
                DropItemManager.DropItem(this, equipSlot);

            if (SlotVisualsCreated && equipSlot == EquipSlot.Quiver && equipment is Item_Quiver)
                GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.HideQuiverSprites();

            equippedItemDatas[(int)equipSlot] = null;

            myUnit.Stats.UpdateCarryWeight();

            // Set the size of the opportunity attack trigger
            if (IsHeldItemEquipSlot(equipSlot))
                myUnit.OpportunityAttackTrigger.UpdateColliderRadius();
        }

        public override void CreateSlotVisuals()
        {
            if (SlotVisualsCreated)
            {
                Debug.LogWarning($"Slot visuals for {name}, owned by {myUnit.name}, has already been created...");
                return;
            }

            if (myUnit.IsPlayer)
                Slots = InventoryUI.PlayerEquipmentSlots;
            else
                Slots = InventoryUI.NpcEquipmentSlots;

            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].SetMyCharacterEquipment(this);
                Slots[i].InventoryItem.SetMyUnitEquipment(this);

                if (Slots[i] is ContainerEquipmentSlot)
                {
                    ContainerEquipmentSlot containerEquipmentSlot = Slots[i] as ContainerEquipmentSlot;
                    if (containerEquipmentSlot.EquipSlot == EquipSlot.Back)
                        containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.UnitEquipment.MyUnit.BackpackInventoryManager);
                    else if (containerEquipmentSlot.EquipSlot == EquipSlot.Belt)
                        containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.UnitEquipment.MyUnit.BeltInventoryManager);
                    else if (containerEquipmentSlot.EquipSlot == EquipSlot.Quiver)
                        containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.UnitEquipment.MyUnit.QuiverInventoryManager);
                }
            }

            if (CurrentWeaponSet == WeaponSet.One)
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

            SlotVisualsCreated = true;

            SetupItems();
        }

        protected override void SetupItems()
        {
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (equippedItemDatas[i] == null || equippedItemDatas[i].Item == null)
                    continue;

                EquipSlot targetEquipSlot = (EquipSlot)i;
                equippedItemDatas[i].RandomizeData();

                if (IsHeldItemEquipSlot(targetEquipSlot)
                    && ((equippedItemDatas[i].Item is Item_Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded && equippedItemDatas[(int)GetOppositeHeldItemEquipSlot(targetEquipSlot)] != null && equippedItemDatas[(int)GetOppositeHeldItemEquipSlot(targetEquipSlot)].Item != null)
                    || (EquipSlotHasItem(GetOppositeHeldItemEquipSlot(targetEquipSlot)) && equippedItemDatas[(int)GetOppositeHeldItemEquipSlot(targetEquipSlot)].Item is Item_Weapon && equippedItemDatas[(int)GetOppositeHeldItemEquipSlot(targetEquipSlot)].Item.Weapon.IsTwoHanded)))
                {
                    Debug.LogWarning($"{myUnit} has 2 two-handed weapons equipped, or a two-handed weapon and a one-handed weapon equipped. That's too many weapons!");
                    RemoveEquipment(equippedItemDatas[i]);
                    continue;
                }
                else if (i == (int)EquipSlot.RightHeldItem1 && equippedItemDatas[i].Item is Item_Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded)
                {
                    targetEquipSlot = EquipSlot.LeftHeldItem1;
                    equippedItemDatas[(int)targetEquipSlot] = equippedItemDatas[i];
                    equippedItemDatas[i] = null;
                }
                else if (i == (int)EquipSlot.RightHeldItem2 && equippedItemDatas[i].Item is Item_Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded)
                {
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
                    equippedItemDatas[(int)targetEquipSlot] = equippedItemDatas[i];
                    equippedItemDatas[i] = null;
                }

                SetupNewItemIcon(GetEquipmentSlot(targetEquipSlot), equippedItemDatas[(int)targetEquipSlot]);
                SetupEquipmentMesh(targetEquipSlot, equippedItemDatas[(int)targetEquipSlot]);

                if ((CurrentWeaponSet == WeaponSet.One && (targetEquipSlot == EquipSlot.LeftHeldItem2 || targetEquipSlot == EquipSlot.RightHeldItem2))
                || (CurrentWeaponSet == WeaponSet.Two && (targetEquipSlot == EquipSlot.LeftHeldItem1 || targetEquipSlot == EquipSlot.RightHeldItem1)))
                    continue;

                AddActions(equippedItemDatas[(int)targetEquipSlot].Item as Item_Equipment);
            }

            myUnit.OpportunityAttackTrigger.UpdateColliderRadius();
        }

        public override void RemoveActions(Item_Equipment equipment, EquipSlot equipSlot)
        {
            for (int i = 0; i < equipment.ActionTypes.Length; i++)
            {
                if (!myUnit.UnitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]))
                    continue;

                if (IsHeldItemEquipSlot(equipSlot))
                {
                    EquipSlot oppositeEquipSlot = GetOppositeHeldItemEquipSlot(equipSlot);
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

                Pool_Actions.ReturnToPool(equipment.ActionTypes[i].GetAction(myUnit));
                myUnit.UnitActionHandler.AvailableActionTypes.Remove(equipment.ActionTypes[i]);
            }

            if (myUnit.IsPlayer)
                ActionSystemUI.UpdateActionVisuals();
        }

        public override bool EquipSlotIsFull(EquipSlot equipSlot)
        {
            if (EquipSlotHasItem(equipSlot))
                return true;

            if (equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2)
            {
                EquipSlot oppositeEquipSlot = GetOppositeHeldItemEquipSlot(equipSlot);
                if (EquipSlotHasItem(oppositeEquipSlot) && equippedItemDatas[(int)oppositeEquipSlot].Item is Item_Weapon && equippedItemDatas[(int)oppositeEquipSlot].Item.Weapon.IsTwoHanded)
                    return true;
            }

            return false;
        }

        public EquipSlot GetOppositeHeldItemEquipSlot(EquipSlot weaponEquipSlot)
        {
            if (!IsHeldItemEquipSlot(weaponEquipSlot))
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

        protected override void SetupNewItemIcon(EquipmentSlot targetSlot, ItemData newItemData)
        {
            if (!SlotVisualsCreated)
                return;

            base.SetupNewItemIcon(targetSlot, newItemData);

            newItemData.SetInventorySlotCoordinate(null);
            targetSlot.InventoryItem.SetItemData(newItemData);
            targetSlot.SetFullSlotSprite();
            targetSlot.ShowSlotImage();
            targetSlot.InventoryItem.UpdateStackSizeVisuals();

            if (targetSlot.IsHeldItemSlot && targetSlot.InventoryItem.ItemData.Item is Item_Weapon && targetSlot.InventoryItem.ItemData.Item.Weapon.IsTwoHanded)
            {
                EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();
                oppositeWeaponSlot.SetFullSlotSprite();
                oppositeWeaponSlot.PlaceholderImage.enabled = false;
            }
            else if (targetSlot.EquipSlot == EquipSlot.Quiver && newItemData.Item is Item_Quiver)
                targetSlot.InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

            if ((CurrentWeaponSet == WeaponSet.Two && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem1 || targetSlot.EquipSlot == EquipSlot.RightHeldItem1))
                || (CurrentWeaponSet == WeaponSet.One && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem2 || targetSlot.EquipSlot == EquipSlot.RightHeldItem2)))
            {
                targetSlot.DisableSlotImage();
                targetSlot.InventoryItem.DisableIconImage();
                targetSlot.PlaceholderImage.enabled = false;

                EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();
                oppositeWeaponSlot.DisableSlotImage();
                oppositeWeaponSlot.InventoryItem.DisableIconImage();
                oppositeWeaponSlot.PlaceholderImage.enabled = false;
            }
            else if (CurrentWeaponSet == WeaponSet.One && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem1 || targetSlot.EquipSlot == EquipSlot.RightHeldItem1))
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
            else if (CurrentWeaponSet == WeaponSet.Two && (targetSlot.EquipSlot == EquipSlot.LeftHeldItem2 || targetSlot.EquipSlot == EquipSlot.RightHeldItem2))
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

        protected override void SetupEquipmentMesh(EquipSlot equipSlot, ItemData itemData)
        {
            if (myUnit.HealthSystem.IsDead)
                return;

            // We only show meshes for these types of equipment:
            if (!IsHeldItemEquipSlot(equipSlot) && itemData.Item is Item_VisibleArmor == false)
                return;

            if (!EquipSlotIsFull(equipSlot) || itemData == null || itemData.Item == null)
                return;

            if ((CurrentWeaponSet == WeaponSet.One && (equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2))
                || (CurrentWeaponSet == WeaponSet.Two && (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1)))
                return;

            if (IsHeldItemEquipSlot(equipSlot))
            {
                HeldItem heldItem = null;
                if (itemData.Item is Item_MeleeWeapon)
                    heldItem = Pool_HeldItemBase.Instance.GetMeleeWeaponBaseFromPool();
                else if (itemData.Item is Item_RangedWeapon)
                    heldItem = Pool_HeldItemBase.Instance.GetRangedWeaponBaseFromPool();
                else if (itemData.Item is Item_Shield)
                    heldItem = Pool_HeldItemBase.Instance.GetShieldBaseFromPool();

                heldItem.SetupHeldItem(itemData, myUnit, equipSlot);

                if (myUnit.IsPlayer)
                    myUnit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
            }
            else
                myUnit.UnitMeshManager.SetupWearableMesh(equipSlot, (Item_VisibleArmor)itemData.Item);
        }

        public override void RemoveEquipmentMesh(EquipSlot equipSlot)
        {
            if (!EquipSlotIsFull(equipSlot))
                return;

            // We only show meshes for these types of equipment:
            if (!IsHeldItemEquipSlot(equipSlot) && equipSlot != EquipSlot.Helm && equipSlot != EquipSlot.BodyArmor && equipSlot != EquipSlot.Shirt)
                return;

            if (IsHeldItemEquipSlot(equipSlot))
            {
                // If the right held item equipSlot was passed in and it's empty, check if the left held item slot has a two hander. If so, that's the item we need to drop.
                if ((equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2) && !EquipSlotHasItem(equipSlot))
                {
                    EquipSlot oppositeEquipSlot = GetOppositeHeldItemEquipSlot(equipSlot);
                    if (EquipSlotHasItem(oppositeEquipSlot) == false)
                    {
                        Debug.LogWarning("Opposite Equip Slot has no Item...");
                        return;
                    }

                    if (equippedItemDatas[(int)oppositeEquipSlot].Item is Item_Weapon && equippedItemDatas[(int)oppositeEquipSlot].Item.Weapon.IsTwoHanded)
                        equipSlot = oppositeEquipSlot;
                }

                myUnit.UnitMeshManager.HumanoidMeshManager.ReturnHeldItemToPool(equipSlot);

                if (myUnit.IsPlayer)
                    myUnit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
            }
            else
                myUnit.UnitMeshManager.RemoveMesh(equipSlot);
        }

        public void SwapWeaponSet()
        {
            if (myUnit.IsPlayer && InventoryUI.IsDraggingItem)
                InventoryUI.ReplaceDraggedItem();

            myUnit.UnitActionHandler.ClearActionQueue(false);

            if (CurrentWeaponSet == WeaponSet.One)
            {
                CurrentWeaponSet = WeaponSet.Two;

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

                if (SlotVisualsCreated)
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
                CurrentWeaponSet = WeaponSet.One;

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

                if (SlotVisualsCreated)
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

            myUnit.OpportunityAttackTrigger.UpdateColliderRadius();
            ActionSystemUI.UpdateActionVisuals();
        }

        public ItemData GetRangedWeaponFromOtherWeaponSet()
        {
            if (CurrentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is Item_RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.LeftHeldItem2];

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is Item_RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.RightHeldItem2];
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is Item_RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.LeftHeldItem1];

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is Item_RangedWeapon)
                    return equippedItemDatas[(int)EquipSlot.RightHeldItem1];
            }
            return null;
        }

        public bool OtherWeaponSet_IsEmpty()
        {
            if (CurrentWeaponSet == WeaponSet.One)
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
            if (CurrentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is Item_MeleeWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is Item_MeleeWeapon)
                    return true;
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is Item_MeleeWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is Item_MeleeWeapon)
                    return true;
            }
            return false;
        }

        public bool OtherWeaponSet_IsRanged()
        {
            if (CurrentWeaponSet == WeaponSet.One)
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is Item_RangedWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is Item_RangedWeapon)
                    return true;
            }
            else
            {
                if (EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is Item_RangedWeapon)
                    return true;

                if (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is Item_RangedWeapon)
                    return true;
            }
            return false;
        }

        public void GetEquippedWeapons(out Item_Weapon primaryWeapon, out Item_Weapon secondaryWeapon)
        {
            HeldMeleeWeapon primaryHeldMeleeWeapon = myUnit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
            HeldMeleeWeapon secondaryHeldMeleeWeapon = myUnit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            if (primaryHeldMeleeWeapon != null && secondaryHeldMeleeWeapon != null)
            {
                primaryWeapon = primaryHeldMeleeWeapon.ItemData.Item.Weapon;
                secondaryWeapon = secondaryHeldMeleeWeapon.ItemData.Item.Weapon;
                return;
            }
            else if (primaryHeldMeleeWeapon != null)
            {
                primaryWeapon = primaryHeldMeleeWeapon.ItemData.Item.Weapon;
                secondaryWeapon = null;
                return;
            }
            else if (RangedWeaponEquipped)
            {
                HeldRangedWeapon heldRangedWeapon = myUnit.UnitMeshManager.GetHeldRangedWeapon();
                if (heldRangedWeapon != null)
                    primaryWeapon = heldRangedWeapon.ItemData.Item.Weapon;
                else
                    primaryWeapon = null;

                secondaryWeapon = null;
                return;
            }

            primaryWeapon = null;
            secondaryWeapon = null;
        }

        public bool InVersatileStance => MeleeWeaponEquipped && myUnit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().CurrentHeldItemStance == HeldItemStance.Versatile;

        public override bool IsDualWielding => myUnit.UnitMeshManager.GetLeftHeldMeleeWeapon() != null && myUnit.UnitMeshManager.GetRightHeldMeleeWeapon() != null;

        public override bool MeleeWeaponEquipped => myUnit.UnitMeshManager.GetPrimaryHeldMeleeWeapon() != null;

        public override bool RangedWeaponEquipped => myUnit.UnitMeshManager.GetHeldRangedWeapon() != null;

        public override bool ShieldEquipped => myUnit.UnitMeshManager.GetHeldShield() != null;

        public override bool IsUnarmed => !MeleeWeaponEquipped && (!RangedWeaponEquipped || !HasValidAmmunitionEquipped());

        public EquipSlot LeftHeldItemEquipSlot => CurrentWeaponSet == WeaponSet.One ? EquipSlot.LeftHeldItem1 : EquipSlot.LeftHeldItem2;
        public EquipSlot RightHeldItemEquipSlot => CurrentWeaponSet == WeaponSet.One ? EquipSlot.RightHeldItem1 : EquipSlot.RightHeldItem2;

        public bool BackpackEquipped => EquipSlotHasItem(EquipSlot.Back) && equippedItemDatas[(int)EquipSlot.Back].Item is Item_Backpack;

        public bool BeltBagEquipped => EquipSlotHasItem(EquipSlot.Belt) && equippedItemDatas[(int)EquipSlot.Belt].Item.Belt.HasAnInventory();

        public bool QuiverEquipped => EquipSlotHasItem(EquipSlot.Quiver) && equippedItemDatas[(int)EquipSlot.Quiver].Item is Item_Quiver;

        public bool HasValidAmmunitionEquipped() => HasValidAmmunitionEquipped(RangedWeaponEquipped ? myUnit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.RangedWeapon : null);

        public bool HasValidAmmunitionEquipped(Item_RangedWeapon rangedWeapon)
        {
            if (rangedWeapon == null)
                return false;

            if (RangedWeaponEquipped && myUnit.UnitMeshManager.GetHeldRangedWeapon().IsLoaded)
                return true;

            if (QuiverEquipped)
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
            if (QuiverEquipped)
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
                if (QuiverEquipped && myUnit.QuiverInventoryManager.ParentInventory.SlotVisualsCreated)
                    myUnit.QuiverInventoryManager.ParentInventory.GetSlotFromItemData(itemData).InventoryItem.UpdateStackSizeVisuals();
                else if (EquipSlotHasItem(EquipSlot.Quiver) && SlotVisualsCreated)
                    GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
                return;
            }

            // If there's now zero in the stack
            if (QuiverEquipped)
                myUnit.QuiverInventoryManager.ParentInventory.RemoveItem(itemData, true);
            else if (EquipSlotHasItem(EquipSlot.Quiver))
                RemoveEquipment(itemData);
        }
    }
}
