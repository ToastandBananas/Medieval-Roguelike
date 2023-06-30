using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField] Unit myUnit;
    [SerializeField] ItemData[] equippedItemDatas = new ItemData[Enum.GetValues(typeof(EquipSlot)).Length];

    public List<EquipmentSlot> slots { get; private set; }

    void Awake()
    {
        slots = new List<EquipmentSlot>();
    }

    public bool TryAddDraggedItemAt(EquipmentSlot targetSlot, ItemData newItemData)
    {
        // Check if the position is invalid
        if (InventoryUI.Instance.validDragPosition == false)
            return false;

        // If trying to place the item back into the slot it came from
        if (targetSlot == InventoryUI.Instance.parentSlotDraggedFrom)
        {
            // Place the dragged item back to where it came from
            InventoryUI.Instance.ReplaceDraggedItem();
        }
        // If there's an item already in the target slot
        else if (InventoryUI.Instance.draggedItemOverlapCount == 1)
        {
            // Get a reference to the overlapped item's data and parent slot before we clear it out
            Slot overlappedItemsParentSlot = InventoryUI.Instance.overlappedItemsParentSlot;
            ItemData overlappedItemData = overlappedItemsParentSlot.GetItemData();
            ItemData newDraggedItemData;

            Debug.Log(overlappedItemsParentSlot);
            Debug.Log(overlappedItemData.Item());

            // Remove the highlighting
            targetSlot.RemoveSlotHighlights();

            int targetEquipSlotIndex = (int)targetSlot.EquipSlot();
            if (newItemData.Item().IsWeapon() && newItemData.Item().Weapon().isTwoHanded)
                targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem;

            // If the slots are in different character equipments
            if (targetSlot.MyCharacterEquipment() != InventoryUI.Instance.DraggedItem().myCharacterEquipment)
            {
                // Create a new ItemData and assign it to the new character equipment
                newDraggedItemData = new ItemData();
                newDraggedItemData.TransferData(newItemData);
                equippedItemDatas[targetEquipSlotIndex] = newDraggedItemData;

                // Remove the item from its original character equipment
                if (InventoryUI.Instance.parentSlotDraggedFrom is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem().myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot()] = null;
                }
                // Remove the item from its original inventory
                else
                    InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);
            }
            else // Else, just get a reference to the dragged item's data before we clear it out
            {
                newDraggedItemData = newItemData;
                equippedItemDatas[targetEquipSlotIndex] = newDraggedItemData;
            }

            /*
            // If we're placing an item directly on top of the same type of item that is stackable and has more room in its stack
            if (overlappedItemsParentSlot == targetSlot && newDraggedItemData.Item() == overlappedItemsData.Item() && newDraggedItemData.Item().maxStackSize > 1 && overlappedItemsData.CurrentStackSize() < overlappedItemsData.Item().maxStackSize)
            {
                int remainingStack = newDraggedItemData.CurrentStackSize();

                // If we can't fit the entire stack, add what we can to the overlapped item's stack size
                if (overlappedItemsData.CurrentStackSize() + remainingStack > overlappedItemsData.Item().maxStackSize)
                {
                    remainingStack -= overlappedItemsData.Item().maxStackSize - overlappedItemsData.CurrentStackSize();
                    overlappedItemsData.SetCurrentStackSize(overlappedItemsData.Item().maxStackSize);
                }
                else // If we can fit the entire stack, add it to the overlapped item's stack size
                {
                    overlappedItemsData.AdjustCurrentStackSize(remainingStack);
                    remainingStack = 0;
                }

                // Update the overlapped item's stack size text
                overlappedItemsParentSlot.InventoryItem().UpdateStackSizeText();

                // If the dragged item has been depleted
                if (remainingStack == 0)
                {
                    // Clear out the parent slot the item was dragged from, if it exists
                    if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                        InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

                    // Hide the dragged item
                    InventoryUI.Instance.DisableDraggedItem();
                }
                else // If there's still some left in the dragged item's stack
                {
                    // Update the dragged item's stack size and text
                    InventoryUI.Instance.DraggedItem().itemData.SetCurrentStackSize(remainingStack);
                    InventoryUI.Instance.DraggedItem().UpdateStackSizeText();

                    // Re-enable the highlighting
                    targetSlot.HighlightSlots();
                }
            }
            else
            {*/

            // Clear out the overlapped item
            overlappedItemsParentSlot.ClearItem();

            // Clear out the dragged item
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            // Setup the target slot's item data and sprites
            SetupNewItem(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newDraggedItemData);

            // Setup the dragged item's data and sprite and start dragging the new item
            InventoryUI.Instance.SetupDraggedItem(overlappedItemData, null, this);

            // Re-enable the highlighting
            targetSlot.HighlightSlots();
        }
        else // If there's no items in the way
        {
            ItemData newEquipmentItemData;

            int targetEquipSlotIndex = (int)targetSlot.EquipSlot();
            if (newItemData.Item().IsWeapon() && newItemData.Item().Weapon().isTwoHanded)
                targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem;

            // If the slots are in different character equipments
            if (targetSlot.MyCharacterEquipment() != InventoryUI.Instance.DraggedItem().myCharacterEquipment)
            {
                // Create a new ItemData and assign it to the new character equipment
                newEquipmentItemData = new ItemData();
                newEquipmentItemData.TransferData(newItemData);
                equippedItemDatas[targetEquipSlotIndex] = newEquipmentItemData;

                // Remove the item from its original character equipment
                if (InventoryUI.Instance.parentSlotDraggedFrom != null && InventoryUI.Instance.parentSlotDraggedFrom is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem().myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot()] = newItemData;
                }
                // Remove the item from its original inventory
                else if (InventoryUI.Instance.DraggedItem().myInventory != null)
                    InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);
            }
            else // Else, just get a reference to the dragged item's data before we clear it out
            {
                newEquipmentItemData = newItemData;
                equippedItemDatas[targetEquipSlotIndex] = newEquipmentItemData;
            }

            // Clear out the dragged item's original slot
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            EquipmentSlot targetEquipmentSlot = GetEquipmentSlotFromIndex(targetEquipSlotIndex);

            // If we're equipping a two handed weapon to the left weapon slot and there's already a weapon in the right weapon slot
            if (targetEquipmentSlot.EquipSlot() == EquipSlot.LeftHeldItem && newEquipmentItemData.Item().Weapon().isTwoHanded && GetEquipmentSlot(EquipSlot.RightHeldItem).GetItemData() != null && GetEquipmentSlot(EquipSlot.RightHeldItem).GetItemData().Item() != null)
            {
                ItemData newDraggedItemData = new ItemData();
                EquipmentSlot rightWeaponSlot = GetEquipmentSlot(EquipSlot.RightHeldItem);
                newDraggedItemData.TransferData(rightWeaponSlot.GetItemData());
                rightWeaponSlot.ClearItem();

                // Setup the dragged item's data and sprite and start dragging the new item
                InventoryUI.Instance.SetupDraggedItem(newDraggedItemData, null, this);
            }
            else
                // Hide the dragged item
                InventoryUI.Instance.DisableDraggedItem();

            // Setup the target slot's item data and sprites
            SetupNewItem(targetEquipmentSlot, newEquipmentItemData);
        }

        return true;
    }

    EquipmentSlot GetEquipmentSlotFromIndex(int index)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (index == (int)slots[i].EquipSlot())
                return slots[i];
        }
        return null;
    }

    /// <summary>Setup the target slot's item data and sprites.</summary>
    void SetupNewItem(EquipmentSlot targetSlot, ItemData newItemData)
    {
        targetSlot.InventoryItem().SetItemData(newItemData);
        targetSlot.SetFullSlotSprite();
        targetSlot.ShowSlotImage();
        targetSlot.InventoryItem().UpdateStackSizeText();

        if (targetSlot.IsWeaponSlot() && targetSlot.InventoryItem().itemData.Item().Weapon().isTwoHanded)
        {
            EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();
            oppositeWeaponSlot.SetFullSlotSprite();
        }
    }

    public EquipmentSlot GetEquipmentSlot(EquipSlot equipSlot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].EquipSlot() == equipSlot)
                return slots[i];
        }
        return null;
    }

    public ItemData[] EquippedItemDatas() => equippedItemDatas;

    public Unit MyUnit() => myUnit;
}
