using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField] Unit myUnit;
    [SerializeField] ItemData[] equippedItemDatas = new ItemData[Enum.GetValues(typeof(EquipSlot)).Length];

    public List<EquipmentSlot> slots { get; private set; }

    bool equipmentVisualsCreated;

    void Awake()
    {
        slots = new List<EquipmentSlot>();
    }

    public bool TryEquipItem(ItemData newItemData)
    {
        if (newItemData.Item() != null)
        {
            EquipSlot targetEquipSlot = 0;
            if (newItemData.Item().IsWeapon())
            {
                if (newItemData.Item().Weapon().isTwoHanded)
                    targetEquipSlot = EquipSlot.LeftHeldItem;
                else if (GetEquipmentSlot(EquipSlot.RightHeldItem).IsFull() == false || GetEquipmentSlot(EquipSlot.LeftHeldItem).IsFull())
                    targetEquipSlot = EquipSlot.RightHeldItem;
            }
            else
                targetEquipSlot = newItemData.Item().Equipment().EquipSlot();

            // If the target equip slot is full
            if (equippedItemDatas[(int)targetEquipSlot].Item() != null)
            {
                // Get the already equipped item(s)

                // Check if the new item is a weapon and if it will be taking the space of a two-hander or dual wielded weapons
                if (newItemData.Item().IsWeapon())
                {
                    EquipSlot oppositeWeaponSlot;
                    if (targetEquipSlot == EquipSlot.LeftHeldItem)
                        oppositeWeaponSlot = EquipSlot.RightHeldItem;
                    else
                        oppositeWeaponSlot = EquipSlot.LeftHeldItem;

                    // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                    if (equippedItemDatas[(int)oppositeWeaponSlot].Item() != null && (newItemData.Item().Weapon().isTwoHanded || equippedItemDatas[(int)oppositeWeaponSlot].Item().Weapon().isTwoHanded))
                    {
                        // Try adding the item to the character's inventory
                        if (myUnit.TryAddItemToInventories(equippedItemDatas[(int)oppositeWeaponSlot]) == false)
                            Debug.Log("Dropping " + equippedItemDatas[(int)oppositeWeaponSlot].Item());
                            //DropItem(oppositeWeaponSlot); // Else, drop the item
                        //else
                            //oppositeWeaponSlot.ClearItem();

                        equippedItemDatas[(int)oppositeWeaponSlot] = null;
                    }
                }

                // Try to add the already equipped item(s) to the Unit's inventory

                // If they don't fit, drop the already equipped item

            }
            else
            {

            }

            // If the equipment came from an inventory slot, clear it out
            if (newItemData.InventorySlotCoordinate() != null && newItemData.InventorySlotCoordinate().myInventory.ContainsItemData(newItemData))
            {
                newItemData.InventorySlotCoordinate().myInventory.GetSlotFromCoordinate(newItemData.InventorySlotCoordinate()).ClearItem();
                newItemData.InventorySlotCoordinate().myInventory.RemoveItem(newItemData);
            }

            // Assign the equipment to the proper slot
            equippedItemDatas[(int)targetEquipSlot] = newItemData;

            if (newItemData.Item().IsWeapon() || newItemData.Item().IsShield() || targetEquipSlot == EquipSlot.BodyArmor || targetEquipSlot == EquipSlot.Helm)
                SetupEquipmentMesh();

            if (equipmentVisualsCreated)
                ShowItemInEquipment();

            return true;
        }
        return false;
    }

    void ShowItemInEquipment()
    {

    }

    void SetupEquipmentMesh()
    {

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
            int targetEquipSlotIndex = (int)targetSlot.EquipSlot();
            if (newItemData.Item().IsWeapon() && newItemData.Item().Weapon().isTwoHanded)
                targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem;

            // Clear out the dragged item
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            if (targetSlot.IsWeaponSlot())
            {
                EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();

                // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                if (oppositeWeaponSlot.IsFull() && (newItemData.Item().Weapon().isTwoHanded || oppositeWeaponSlot.GetItemData().Item().Weapon().isTwoHanded))
                {
                    // Try adding the item to the character's inventory
                    if (myUnit.TryAddItemToInventories(oppositeWeaponSlot.GetItemData()) == false)
                        oppositeWeaponSlot.InventoryItem().DropItem(); // Else, drop the item
                    else
                        oppositeWeaponSlot.ClearItem();

                    equippedItemDatas[(int)oppositeWeaponSlot.EquipSlot()] = null;
                }
            }

            // Try adding the target slot's item to the character's inventory
            if (myUnit.TryAddItemToInventories(targetSlot.GetItemData()) == false)
            {
                // Else, drop the item
                targetSlot.InventoryItem().DropItem();
            }
            else
                targetSlot.ClearItem();

            // Assign the data
            equippedItemDatas[targetEquipSlotIndex] = newItemData;

            // Setup the target slot's item data and sprites
            SetupNewItem(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newItemData);

            // Remove the item from its original character equipment
            if (InventoryUI.Instance.DraggedItem().myCharacterEquipment != null)
            {
                EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                InventoryUI.Instance.DraggedItem().myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot()] = null;
            }
            // Or remove the item from its original inventory
            else if (InventoryUI.Instance.DraggedItem().myInventory != null)
                InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);

            // Hide the dragged item
            InventoryUI.Instance.DisableDraggedItem();
        }
        else // If there's no items in the way
        {
            int targetEquipSlotIndex = (int)targetSlot.EquipSlot();
            if (newItemData.Item().IsWeapon() && newItemData.Item().Weapon().isTwoHanded)
                targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem;

            // Clear out the dragged item's original slot
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            if (targetSlot.IsWeaponSlot())
            {
                EquipmentSlot oppositeWeaponSlot = targetSlot.GetOppositeWeaponSlot();

                // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                if (oppositeWeaponSlot.IsFull() && (newItemData.Item().Weapon().isTwoHanded || oppositeWeaponSlot.GetItemData().Item().Weapon().isTwoHanded))
                {
                    // Try adding the item to the character's inventory
                    if (myUnit.TryAddItemToInventories(oppositeWeaponSlot.GetItemData()) == false)
                        oppositeWeaponSlot.InventoryItem().DropItem(); // Else, drop the item
                    else
                        oppositeWeaponSlot.ClearItem();

                    equippedItemDatas[(int)oppositeWeaponSlot.EquipSlot()] = null;
                }
            }

            // Assign the data
            equippedItemDatas[targetEquipSlotIndex] = newItemData;

            // Setup the target slot's item data and sprites
            SetupNewItem(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newItemData);

            // Remove the item from its original character equipment
            if (InventoryUI.Instance.DraggedItem().myCharacterEquipment != null)
            {
                EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                InventoryUI.Instance.DraggedItem().myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot()] = null;
            }
            // Or remove the item from its original inventory
            else if (InventoryUI.Instance.DraggedItem().myInventory != null)
                InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);

            // Hide the dragged item
            InventoryUI.Instance.DisableDraggedItem();
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
        newItemData.SetInventorySlotCoordinate(null);
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

    public bool ItemDataEquipped(ItemData itemData)
    {
        for (int i = 0; i < equippedItemDatas.Length; i++)
        {
            if (equippedItemDatas[i] == itemData)
                return true;
        }
        return false;
    }

    public ItemData[] EquippedItemDatas() => equippedItemDatas;

    public Unit MyUnit() => myUnit;
}
