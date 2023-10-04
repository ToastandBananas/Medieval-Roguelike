using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipSlot { LeftHeldItem1, RightHeldItem1, LeftHeldItem2, RightHeldItem2, Helm, BodyArmor, Shirt, Gloves, Boots, Back, Quiver }
public enum WeaponSet { One = 1, Two = 2 }

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField] Unit unit;

    [SerializeField] ItemData[] equippedItemDatas = new ItemData[Enum.GetValues(typeof(EquipSlot)).Length];

    [NamedArray(new string[] { "Left Held Item 1", "Right Held Item 1", "Left Held Item 2", "Right Held Item 2", "Helm", "Body Armor", "Shirt", "Gloves", "Boots", "Back", "Quiver" })]
    [SerializeField] Equipment[] startingEquipment = new Equipment[Enum.GetValues(typeof(EquipSlot)).Length];

    public List<EquipmentSlot> slots { get; private set; }

    public WeaponSet currentWeaponSet { get; private set; }

    bool slotVisualsCreated;

    void Awake()
    {
        slots = new List<EquipmentSlot>();

        currentWeaponSet = WeaponSet.One;

        // Setup our starting equipment
        for (int i = 0; i < startingEquipment.Length; i++)
        {
            equippedItemDatas[i].SetItem(startingEquipment[i]);
        }

        if (unit.IsPlayer())
            CreateSlotVisuals();
        else
            SetupItems();
    }

    // For testing:
    /*void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ItemData newItemData = new ItemData(equippedItemDatas[(int)EquipSlot.RightHeldItem].Item);
            TryEquipItem(newItemData);
        }
    }*/

    public bool TryEquipItem(ItemData newItemData)
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

        return TryAddItemAt(targetEquipSlot, newItemData);
    }

    public bool TryAddItemAt(EquipSlot targetEquipSlot, ItemData newItemData)
    {
        if ((newItemData.Item is Equipment == false || newItemData.Item.Equipment.EquipSlot != EquipSlot.Back) && targetEquipSlot == EquipSlot.Back && EquipSlotHasItem(EquipSlot.Back) && equippedItemDatas[(int)targetEquipSlot].Item is Backpack)
        { 
            if (unit.BackpackInventoryManager.ParentInventory.TryAddItem(newItemData))
            {
                if (ItemDataEquipped(newItemData))
                    RemoveEquipment(GetEquipmentFromItemData(newItemData));

                if (InventoryUI.Instance.isDraggingItem)
                    InventoryUI.Instance.DisableDraggedItem();
                return true;
            }
            else
            {
                if (InventoryUI.Instance.isDraggingItem)
                    InventoryUI.Instance.ReplaceDraggedItem();
                return false;
            }
        }

        // Check if the position is invalid
        if (InventoryUI.Instance.isDraggingItem && InventoryUI.Instance.validDragPosition == false)
        {
            InventoryUI.Instance.ReplaceDraggedItem();
            return false;
        }

        // If the item is a two-handed weapon, assign the left held item equip slot
        if (newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded)
        {
            if (currentWeaponSet == WeaponSet.One)
                targetEquipSlot = EquipSlot.LeftHeldItem1;
            else
                targetEquipSlot = EquipSlot.LeftHeldItem2;
        }

        // If trying to place the item back into the slot it came from, place the dragged item back to where it came from
        if (InventoryUI.Instance.isDraggingItem && GetEquipmentSlot(targetEquipSlot) == InventoryUI.Instance.parentSlotDraggedFrom)
            InventoryUI.Instance.ReplaceDraggedItem();
        // If trying to place ammo on a Quiver slot that has a Quiver equipped
        else if (newItemData.Item is Ammunition && targetEquipSlot == EquipSlot.Quiver && EquipSlotHasItem(EquipSlot.Quiver) && (newItemData.IsEqual(equippedItemDatas[(int)EquipSlot.Quiver]) || (equippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver && newItemData.Item.Ammunition.ProjectileType == equippedItemDatas[(int)EquipSlot.Quiver].Item.Quiver.AllowedProjectileType)))
            TryAddToEquippedAmmunition(newItemData);
        else
            Equip(newItemData, targetEquipSlot);

        return true;
    }

    void Equip(ItemData newItemData, EquipSlot targetEquipSlot)
    {
        // Clear out the item from it's original slot
        RemoveItemFromOrigin(newItemData);

        if (IsHeldItemEquipSlot(targetEquipSlot))
        {
            EquipSlot oppositeWeaponSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);

            // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
            if (EquipSlotHasItem(oppositeWeaponSlot) && ((newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded) || (equippedItemDatas[(int)oppositeWeaponSlot].Item is Weapon && equippedItemDatas[(int)oppositeWeaponSlot].Item.Weapon.IsTwoHanded)))
                UnequipItem(oppositeWeaponSlot);
        }

        // Unequip any item already in the target equip slot
        UnequipItem(targetEquipSlot);

        // Assign the data
        equippedItemDatas[(int)targetEquipSlot] = newItemData;

        InitializeInventories(targetEquipSlot, newItemData);

        if (targetEquipSlot == EquipSlot.RightHeldItem1 && newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded)
            equippedItemDatas[(int)EquipSlot.RightHeldItem1] = null;
        else if (targetEquipSlot == EquipSlot.RightHeldItem2 && newItemData.Item is Weapon && newItemData.Item.Weapon.IsTwoHanded)
            equippedItemDatas[(int)EquipSlot.RightHeldItem2] = null;

        // Setup the target slot's item data/sprites and mesh if necessary
        SetupNewItemIcon(GetEquipmentSlot(targetEquipSlot), newItemData);
        SetupEquipmentMesh(targetEquipSlot, newItemData);

        // Hide the dragged item
        if (InventoryUI.Instance.isDraggingItem)
            InventoryUI.Instance.DisableDraggedItem();

        AddActions(newItemData.Item as Equipment);
    }

    public bool TryAddToEquippedAmmunition(ItemData ammoItemData)
    {
        if (EquipSlotHasItem(EquipSlot.Quiver) == false || ammoItemData.Item is Ammunition == false)
            return false;
        
        if (ammoItemData.MyInventory() != null && ammoItemData.MyInventory() == unit.QuiverInventoryManager.ParentInventory)
        {
            if (InventoryUI.Instance.isDraggingItem)
                InventoryUI.Instance.ReplaceDraggedItem();

            return false;
        }

        // If there's a quiver we can add the ammo to
        if (equippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver && ammoItemData.Item.Ammunition.ProjectileType == equippedItemDatas[(int)EquipSlot.Quiver].Item.Quiver.AllowedProjectileType)
        {
            if (unit.QuiverInventoryManager.ParentInventory.TryAddItem(ammoItemData))
            {
                if (unit.QuiverInventoryManager.ParentInventory.SlotVisualsCreated)
                    GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                if (InventoryUI.Instance.isDraggingItem)
                    InventoryUI.Instance.DisableDraggedItem();

                return true;
            }
        }
        // If trying to add to a stack of ammo
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

            if (ammoItemData.CurrentStackSize <= 0)
            {
                RemoveItemFromOrigin(ammoItemData);
                if (InventoryUI.Instance.isDraggingItem)
                    InventoryUI.Instance.DisableDraggedItem();

                return true;
            }
            else
            {
                // Update the stack size text for the ammo since it wasn't all equipped
                if (ammoItemData.MyInventory() != null && ammoItemData.MyInventory().SlotVisualsCreated)
                {
                    InventorySlot slot = ammoItemData.MyInventory().GetSlotFromItemData(ammoItemData);
                    slot.InventoryItem.UpdateStackSizeVisuals();
                }
            }
        }

        if (InventoryUI.Instance.isDraggingItem)
            InventoryUI.Instance.ReplaceDraggedItem();

        return false;
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

            RemoveEquipmentMesh((EquipSlot)targetEquipSlotIndex);
            equippedItemDatas[targetEquipSlotIndex] = null;

            ActionSystemUI.UpdateActionVisuals();
        }
    }

    void InitializeInventories(EquipSlot targetEquipSlot, ItemData newItemData)
    {
        if (targetEquipSlot == EquipSlot.Back && newItemData.Item is Backpack)
            unit.BackpackInventoryManager.Initialize();
        else if (targetEquipSlot == EquipSlot.Quiver)
            unit.QuiverInventoryManager.Initialize();
    }

    void RemoveItemFromOrigin(ItemData itemDataToRemove)
    {
        // Remove the item from its original character equipment or inventory
        if (InventoryUI.Instance.isDraggingItem)
        {
            if (InventoryUI.Instance.DraggedItem.myCharacterEquipment != null)
            {
                EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                equipmentSlotDraggedFrom.CharacterEquipment.RemoveEquipmentMesh(equipmentSlotDraggedFrom.EquipSlot);

                InventoryUI.Instance.DraggedItem.myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot] = null;

                ActionSystemUI.UpdateActionVisuals();
            }
            else if (InventoryUI.Instance.DraggedItem.myInventory != null)
                InventoryUI.Instance.DraggedItem.myInventory.ItemDatas.Remove(itemDataToRemove);
        }
        else if (itemDataToRemove.InventorySlotCoordinate() != null && itemDataToRemove.InventorySlotCoordinate().myInventory.ContainsItemData(itemDataToRemove))
        {
            itemDataToRemove.InventorySlotCoordinate().myInventory.RemoveItem(itemDataToRemove);
        }
        else if (ContextMenu.Instance.TargetSlot != null)
        {
            InventoryItem targetInventoryItem = ContextMenu.Instance.TargetSlot.InventoryItem;
            if (targetInventoryItem.myCharacterEquipment != null)
            {
                EquipmentSlot equipmentSlotTakenFrom = ContextMenu.Instance.TargetSlot.ParentSlot() as EquipmentSlot;
                equipmentSlotTakenFrom.CharacterEquipment.RemoveEquipmentMesh(equipmentSlotTakenFrom.EquipSlot);

                targetInventoryItem.myCharacterEquipment.equippedItemDatas[(int)equipmentSlotTakenFrom.EquipSlot] = null;
                ContextMenu.Instance.TargetSlot.ParentSlot().ClearItem();

                ActionSystemUI.UpdateActionVisuals();
            }
            else if (targetInventoryItem.myInventory != null)
            {
                targetInventoryItem.myInventory.ItemDatas.Remove(itemDataToRemove);
                ContextMenu.Instance.TargetSlot.ParentSlot().ClearItem();
            }
        }

        // Clear out the dragged item's original slot
        if (InventoryUI.Instance.parentSlotDraggedFrom != null)
            InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();
    }

    public void UnequipItem(EquipSlot equipSlot)
    {
        if (EquipSlotHasItem(equipSlot) == false)
            return;

        Equipment equipment = equippedItemDatas[(int)equipSlot].Item as Equipment;
        if (GetEquipmentSlot(equipSlot) != InventoryUI.Instance.parentSlotDraggedFrom)
        {
            // If this is the Unit's equipped backpack
            if (equipSlot == EquipSlot.Back && equipment is Backpack)
            {
                if (InventoryUI.Instance.GetContainerUI(unit.BackpackInventoryManager) != null)
                    InventoryUI.Instance.GetContainerUI(unit.BackpackInventoryManager).CloseContainerInventory();

                if (unit.BackpackInventoryManager.ContainsAnyItems())
                    DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
            }
            else if (equipSlot == EquipSlot.Quiver && equipment is Quiver)
            {
                if (InventoryUI.Instance.GetContainerUI(unit.QuiverInventoryManager) != null)
                    InventoryUI.Instance.GetContainerUI(unit.QuiverInventoryManager).CloseContainerInventory();

                if (unit.QuiverInventoryManager.ContainsAnyItems())
                    DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
            }
        }

        if (unit.TryAddItemToInventories(equippedItemDatas[(int)equipSlot]))
        {
            if (slotVisualsCreated)
                GetEquipmentSlot(equipSlot).ClearItem();

            RemoveEquipmentMesh(equipSlot);
        }
        else // Else, drop the item
            DropItemManager.DropItem(this, equipSlot);

        equippedItemDatas[(int)equipSlot] = null;
        RemoveActions(equipment);
    }

    void CreateSlotVisuals()
    {
        if (slotVisualsCreated)
        {
            Debug.LogWarning($"Slot visuals for {name}, owned by {unit.name}, has already been created...");
            return;
        }

        if (slots.Count > 0)
        {
            // Clear out any slots already in the list, so we can start from scratch
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].RemoveSlotHighlights();
                slots[i].ClearItem();
                slots[i].SetMyCharacterEquipment(null);
            }

            slots.Clear();
        }

        if (unit.IsPlayer())
            slots = InventoryUI.Instance.playerEquipmentSlots;
        else
            slots = InventoryUI.Instance.npcEquipmentSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetMyCharacterEquipment(this);
            slots[i].InventoryItem.SetMyCharacterEquipment(this);

            if (slots[i] is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerEquipmentSlot = slots[i] as ContainerEquipmentSlot;
                if (containerEquipmentSlot.EquipSlot == EquipSlot.Back)
                    containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.CharacterEquipment.Unit.BackpackInventoryManager);
                else if (containerEquipmentSlot.EquipSlot == EquipSlot.Quiver)
                    containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.CharacterEquipment.Unit.QuiverInventoryManager);
            }
        }

        slotVisualsCreated = true;

        SetupItems();
    }

    public void SetupItems()
    {
        for (int i = 0; i < equippedItemDatas.Length; i++)
        {
            if (EquipSlotHasItem(i) == false)
                continue;

            equippedItemDatas[i].RandomizeData();

            if (IsHeldItemEquipSlot((EquipSlot)i)
                && ((equippedItemDatas[i].Item is Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)] != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item != null)
                || (EquipSlotHasItem((int)GetOppositeWeaponEquipSlot((EquipSlot)i)) && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item is Weapon && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item.Weapon.IsTwoHanded)))
            {
                Debug.LogError($"{unit} has 2 two-handed weapons equipped, or a two-handed weapon and a one-handed weapon equipped. That's too many weapons!");
            }
            else if (i == (int)EquipSlot.RightHeldItem1 && equippedItemDatas[i].Item is Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded)
            {
                equippedItemDatas[(int)EquipSlot.LeftHeldItem1] = equippedItemDatas[i];
                equippedItemDatas[i] = null;

                SetupNewItemIcon(GetEquipmentSlotFromIndex((int)EquipSlot.LeftHeldItem1), equippedItemDatas[(int)EquipSlot.LeftHeldItem1]);
                SetupEquipmentMesh(EquipSlot.LeftHeldItem1, equippedItemDatas[i]);
            }
            else if (i == (int)EquipSlot.RightHeldItem2 && equippedItemDatas[i].Item is Weapon && equippedItemDatas[i].Item.Weapon.IsTwoHanded)
            {
                equippedItemDatas[(int)EquipSlot.LeftHeldItem2] = equippedItemDatas[i];
                equippedItemDatas[i] = null;

                SetupNewItemIcon(GetEquipmentSlotFromIndex((int)EquipSlot.LeftHeldItem2), equippedItemDatas[(int)EquipSlot.LeftHeldItem2]);
                SetupEquipmentMesh(EquipSlot.LeftHeldItem2, equippedItemDatas[i]);
            }
            else
            {
                SetupNewItemIcon(GetEquipmentSlotFromIndex(i), equippedItemDatas[i]);
                SetupEquipmentMesh((EquipSlot)i, equippedItemDatas[i]);
            }

            AddActions(equippedItemDatas[i].Item as Equipment);
        }
    }

    void AddActions(Equipment equipment)
    {
        for (int i = 0; i < equipment.ActionTypes.Length; i++)
        {
            if (unit.unitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]))
                continue;

            unit.unitActionHandler.AvailableActionTypes.Add(equipment.ActionTypes[i]);
            equipment.ActionTypes[i].GetAction(unit);
        }

        ActionSystemUI.UpdateActionVisuals();
    }

    public void RemoveActions(Equipment equipment)
    {
        for (int i = 0; i < equipment.ActionTypes.Length; i++)
        {
            if (unit.unitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]) == false || (equipment.ActionTypes[i].GetAction(unit) is MeleeAction && unit.stats.CanFightUnarmed)) // Don't remove the basic MeleeAction if this Unit can fight unarmed
                continue;

            ActionSystem.ReturnToPool(equipment.ActionTypes[i].GetAction(unit));
            unit.unitActionHandler.AvailableActionTypes.Remove(equipment.ActionTypes[i]);
        }

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

    public EquipSlot GetOppositeWeaponEquipSlot(EquipSlot equipSlot)
    {
        if (equipSlot != EquipSlot.LeftHeldItem1 && equipSlot != EquipSlot.RightHeldItem1 && equipSlot != EquipSlot.LeftHeldItem2 && equipSlot != EquipSlot.RightHeldItem2)
        {
            Debug.LogWarning($"{equipSlot} is not a weapon slot...");
            return equipSlot;
        }

        if (equipSlot == EquipSlot.LeftHeldItem1)
            return EquipSlot.RightHeldItem1;
        else if (equipSlot == EquipSlot.RightHeldItem1)
            return EquipSlot.LeftHeldItem1;
        else if (equipSlot == EquipSlot.LeftHeldItem2)
            return EquipSlot.RightHeldItem2;
        else
            return EquipSlot.LeftHeldItem2;
    }

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
        }
    }

    void SetupEquipmentMesh(EquipSlot equipSlot, ItemData itemData)
    {
        // We only show meshes for these types of equipment:
        if (IsHeldItemEquipSlot(equipSlot) == false && equipSlot != EquipSlot.Helm && equipSlot != EquipSlot.BodyArmor)
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

            heldItem.SetupHeldItem(itemData, unit, equipSlot);

            if (unit.IsPlayer())
                unit.unitActionHandler.SetSelectedActionType(unit.unitActionHandler.FindActionTypeByName("MoveAction"));
        }
        else if (equipSlot == EquipSlot.Helm || equipSlot == EquipSlot.BodyArmor)
        {
            unit.unitMeshManager.SetupMesh(equipSlot, (Equipment)itemData.Item);
        }
    }

    public void RemoveEquipmentMesh(EquipSlot equipSlot)
    {
        if (EquipSlotIsFull(equipSlot) == false)
            return;

        // We only show meshes for these types of equipment:
        if (IsHeldItemEquipSlot(equipSlot) == false && equipSlot != EquipSlot.Helm && equipSlot != EquipSlot.BodyArmor)
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

            unit.unitMeshManager.ReturnHeldItemToPool(equipSlot);

            if (unit.IsPlayer())
                unit.unitActionHandler.SetSelectedActionType(unit.unitActionHandler.FindActionTypeByName("MoveAction"));
        }
        else
            unit.unitMeshManager.RemoveMesh(equipSlot);
    }

    public void SwapWeaponSet()
    {
        if (InventoryUI.Instance.isDraggingItem)
            InventoryUI.Instance.ReplaceDraggedItem();

        if (currentWeaponSet == WeaponSet.One)
        {
            currentWeaponSet = WeaponSet.Two;

            // Remove held item bases for current held items
            if (EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                RemoveEquipmentMesh(EquipSlot.LeftHeldItem1);

            if (EquipSlotHasItem(EquipSlot.RightHeldItem1))
                RemoveEquipmentMesh(EquipSlot.RightHeldItem1);

            // Create held item bases for the other weapon set
            SetupEquipmentMesh(EquipSlot.LeftHeldItem2, equippedItemDatas[(int)EquipSlot.LeftHeldItem2]);
            SetupEquipmentMesh(EquipSlot.RightHeldItem2, equippedItemDatas[(int)EquipSlot.RightHeldItem2]);

            if (slotVisualsCreated)
            {
                // Hide current held item icons
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).DisableSlotImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).InventoryItem.DisableIconImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = false;

                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).DisableSlotImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).InventoryItem.DisableIconImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = false;

                // Show held item icons for the other weapon set
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).EnableSlotImage();
                if (unit.CharacterEquipment.EquipSlotIsFull(EquipSlot.LeftHeldItem2))
                {
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).InventoryItem.EnableIconImage();
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = false;
                }
                else
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = true;

                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).EnableSlotImage();
                if (unit.CharacterEquipment.EquipSlotIsFull(EquipSlot.RightHeldItem2))
                {
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).InventoryItem.EnableIconImage();
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = false;
                }
                else
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = true;
            }
        }
        else
        {
            currentWeaponSet = WeaponSet.One;

            // Remove held item bases for current held items
            if (EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                RemoveEquipmentMesh(EquipSlot.LeftHeldItem2);

            if (EquipSlotHasItem(EquipSlot.RightHeldItem2))
                RemoveEquipmentMesh(EquipSlot.RightHeldItem2);

            // Create held item bases for the other weapon set
            SetupEquipmentMesh(EquipSlot.LeftHeldItem1, equippedItemDatas[(int)EquipSlot.LeftHeldItem1]);
            SetupEquipmentMesh(EquipSlot.RightHeldItem1, equippedItemDatas[(int)EquipSlot.RightHeldItem1]);

            if (slotVisualsCreated)
            {
                // Hide current held item icons
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).DisableSlotImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).InventoryItem.DisableIconImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).PlaceholderImage.enabled = false;

                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).DisableSlotImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).InventoryItem.DisableIconImage();
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).PlaceholderImage.enabled = false;

                // Show held item icons for the other weapon set
                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).EnableSlotImage();
                if (unit.CharacterEquipment.EquipSlotIsFull(EquipSlot.LeftHeldItem1))
                {
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).InventoryItem.EnableIconImage();
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = false;
                }
                else
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).PlaceholderImage.enabled = true;

                unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).EnableSlotImage();
                if (unit.CharacterEquipment.EquipSlotIsFull(EquipSlot.RightHeldItem1))
                {
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).InventoryItem.EnableIconImage();
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = false;
                }
                else
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).PlaceholderImage.enabled = true;
            }
        }

        ActionSystemUI.UpdateActionVisuals();
    }

    public bool IsDualWielding() => 
        (currentWeaponSet == WeaponSet.One && EquipSlotHasItem(EquipSlot.LeftHeldItem1) && EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is MeleeWeapon && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is MeleeWeapon)
        || (currentWeaponSet == WeaponSet.Two && EquipSlotHasItem(EquipSlot.LeftHeldItem2) && EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is MeleeWeapon && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is MeleeWeapon);

    public bool MeleeWeaponEquipped() => 
        (currentWeaponSet == WeaponSet.One && ((EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is MeleeWeapon) || (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is MeleeWeapon)))
        || (currentWeaponSet == WeaponSet.Two && ((EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is MeleeWeapon) || (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is MeleeWeapon)));

    public bool RangedWeaponEquipped() => 
        (currentWeaponSet == WeaponSet.One && ((EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is RangedWeapon) || (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is RangedWeapon)))
        || (currentWeaponSet == WeaponSet.Two && ((EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is RangedWeapon) || (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is RangedWeapon)));

    public bool ShieldEquipped() =>
        (currentWeaponSet == WeaponSet.One && ((EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is Shield) || (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is Shield)))
        || (currentWeaponSet == WeaponSet.Two && ((EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is Shield) || (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is Shield)));

    public bool IsUnarmed() => 
        (currentWeaponSet == WeaponSet.One && (EquipSlotHasItem(EquipSlot.LeftHeldItem1) == false || equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item is Weapon == false) && (EquipSlotHasItem(EquipSlot.RightHeldItem1) == false || equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item is Weapon == false))
        || (currentWeaponSet == WeaponSet.Two && (EquipSlotHasItem(EquipSlot.LeftHeldItem2) == false || equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item is Weapon == false) && (EquipSlotHasItem(EquipSlot.RightHeldItem2) == false || equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item is Weapon == false));

    public bool IsHeldItemEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2;

    public bool BackpackEquipped() => EquipSlotHasItem(EquipSlot.Back) && equippedItemDatas[(int)EquipSlot.Back].Item is Backpack;

    public bool QuiverEquipped() => EquipSlotHasItem(EquipSlot.Quiver) && equippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver;

    public bool HasValidAmmunitionEquipped()
    {
        if (RangedWeaponEquipped() == false)
            return false;

        if (QuiverEquipped())
        {
            for (int i = 0; i < unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
            {
                if (unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.ProjectileType == unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ProjectileType)
                    return true;
            }
        }
        else if (EquipSlotHasItem(EquipSlot.Quiver))
        {
            if (equippedItemDatas[(int)EquipSlot.Quiver].Item.Ammunition.ProjectileType == unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ProjectileType)
                return true;
        }

        return false;
    }

    public ItemData GetEquippedProjectile(ProjectileType projectileType)
    {
        if (QuiverEquipped())
        {
            for (int i = 0; i < unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
            {
                if (unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.ProjectileType == projectileType)
                    return unit.QuiverInventoryManager.ParentInventory.ItemDatas[i];
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
            if (QuiverEquipped() && unit.QuiverInventoryManager.ParentInventory.SlotVisualsCreated)
                unit.QuiverInventoryManager.ParentInventory.GetSlotFromItemData(itemData).InventoryItem.UpdateStackSizeVisuals();
            else if (EquipSlotHasItem(EquipSlot.Quiver) && slotVisualsCreated)
                GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.UpdateStackSizeVisuals();
            return;
        }

        // If there's now zero in the stack
        if (QuiverEquipped())
            unit.QuiverInventoryManager.ParentInventory.RemoveItem(itemData);
        else if (EquipSlotHasItem(EquipSlot.Quiver))
            RemoveEquipment(itemData);
    }

    public EquipmentSlot GetEquipmentSlot(EquipSlot equipSlot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].EquipSlot == equipSlot)
            {
                if (slots[i] is ContainerEquipmentSlot)
                    return slots[i] as ContainerEquipmentSlot;
                return slots[i];
            }
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
    
    public ItemData GetEquipmentFromItemData(ItemData itemData)
    {
        for (int i = 0; i < equippedItemDatas.Length; i++)
        {
            if (equippedItemDatas[i] == itemData)
                return equippedItemDatas[i];
        }
        return null;
    }

    public ItemData[] EquippedItemDatas => equippedItemDatas;

    public Unit Unit => unit;

    public bool SlotVisualsCreated => slotVisualsCreated;
}
