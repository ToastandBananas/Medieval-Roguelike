using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipSlot { LeftHeldItem1, RightHeldItem1, LeftHeldItem2, RightHeldItem2, Helm, BodyArmor, Shirt, Gloves, Boots, Back }
public enum WeaponSet { One = 1, Two = 2 }

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField] Unit myUnit;

    [SerializeField] ItemData[] equippedItemDatas = new ItemData[Enum.GetValues(typeof(EquipSlot)).Length];

    [NamedArray(new string[] { "Left Held Item 1", "Right Held Item 1", "Left Held Item 2", "Right Held Item 2", "Helm", "Body Armor", "Shirt", "Gloves", "Boots", "Back" })]
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

        if (myUnit.IsPlayer())
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
        EquipSlot targetEquipSlot = newItemData.Item.Equipment().EquipSlot();
        if (newItemData.Item.IsWeapon())
        {
            if (newItemData.Item.Weapon().isTwoHanded)
                targetEquipSlot = EquipSlot.LeftHeldItem1;
            else if (EquipSlotIsFull(targetEquipSlot))
            {
                EquipSlot oppositeWeaponEquipSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);
                if (EquipSlotIsFull(oppositeWeaponEquipSlot) == false)
                    targetEquipSlot = oppositeWeaponEquipSlot;
            }
        }

        if (TryAddItemAt(targetEquipSlot, newItemData))
        {
            SetupNewItemIcon(GetEquipmentSlot(targetEquipSlot), newItemData);
            SetupEquipmentMesh(targetEquipSlot, newItemData);
            return true;
        }
        return false;
    }

    public bool TryAddItemAt(EquipSlot targetEquipSlot, ItemData newItemData)
    {
        // Check if the position is invalid
        if (InventoryUI.Instance.isDraggingItem && InventoryUI.Instance.validDragPosition == false)
            return false;

        int targetEquipSlotIndex = (int)targetEquipSlot;
        if (newItemData.Item.IsWeapon() && newItemData.Item.Weapon().isTwoHanded)
        {
            if (currentWeaponSet == WeaponSet.One)
                targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem1;
            else
                targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem2;
        }

        // If trying to place the item back into the slot it came from
        if (InventoryUI.Instance.isDraggingItem && GetEquipmentSlot((EquipSlot)targetEquipSlotIndex) == InventoryUI.Instance.parentSlotDraggedFrom)
        {
            // Place the dragged item back to where it came from
            InventoryUI.Instance.ReplaceDraggedItem();
        }
        // If there's an item already in the target slot
        else if ((InventoryUI.Instance.isDraggingItem && InventoryUI.Instance.draggedItemOverlapCount == 1) || EquipSlotIsFull(targetEquipSlot))
        {
            // Clear out the dragged item
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            if (IsHeldItemEquipSlot(targetEquipSlot))
            {
                EquipSlot oppositeWeaponSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);

                // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                if (EquipSlotHasItem(oppositeWeaponSlot) && ((newItemData.Item.IsWeapon() && newItemData.Item.Weapon().isTwoHanded) || (equippedItemDatas[(int)oppositeWeaponSlot].Item.IsWeapon() && equippedItemDatas[(int)oppositeWeaponSlot].Item.Weapon().isTwoHanded)))
                    UnequipItem(oppositeWeaponSlot);
            }

            // Unequip any item already in the target equip slot
            UnequipItem(targetEquipSlot);

            // Assign the data
            equippedItemDatas[targetEquipSlotIndex] = newItemData;

            if (targetEquipSlot == EquipSlot.Back && newItemData.Item.IsBag())
                myUnit.BackpackInventoryManager.Initialize();
            else if (newItemData.Item is Quiver)
                myUnit.QuiverInventoryManager.Initialize();

            if (targetEquipSlot == EquipSlot.RightHeldItem1 && newItemData.Item.IsWeapon() && newItemData.Item.Weapon().isTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem1] = null;
            else if (targetEquipSlot == EquipSlot.RightHeldItem2 && newItemData.Item.IsWeapon() && newItemData.Item.Weapon().isTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem2] = null;

            // Setup the target slot's item data/sprites & the mesh if necessary
            SetupNewItemIcon(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newItemData);
            SetupEquipmentMesh(targetEquipSlot, newItemData);

            // Remove the item from its original character equipment or inventory
            if (InventoryUI.Instance.isDraggingItem)
            {
                if (InventoryUI.Instance.DraggedItem.myCharacterEquipment != null)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    if (equipmentSlotDraggedFrom.IsHeldItemSlot())
                        equipmentSlotDraggedFrom.CharacterEquipment.RemoveEquipmentMesh(equipmentSlotDraggedFrom.EquipSlot);

                    InventoryUI.Instance.DraggedItem.myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot] = null;
                }
                else if (InventoryUI.Instance.DraggedItem.myInventory != null)
                    InventoryUI.Instance.DraggedItem.myInventory.ItemDatas.Remove(newItemData);
            }
            else if (newItemData.InventorySlotCoordinate() != null && newItemData.InventorySlotCoordinate().myInventory.ContainsItemData(newItemData))
            {
                newItemData.InventorySlotCoordinate().myInventory.GetSlotFromCoordinate(newItemData.InventorySlotCoordinate()).ClearItem();
                newItemData.InventorySlotCoordinate().myInventory.RemoveItem(newItemData);
            }

            // Hide the dragged item
            if (InventoryUI.Instance.isDraggingItem)
                InventoryUI.Instance.DisableDraggedItem();
        }
        else // If there's no items in the way
        {
            // Clear out the dragged item's original slot
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            if (IsHeldItemEquipSlot(targetEquipSlot))
            {
                EquipSlot oppositeWeaponSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);

                // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                if (EquipSlotHasItem(oppositeWeaponSlot) && ((newItemData.Item.IsWeapon() && newItemData.Item.Weapon().isTwoHanded) || (equippedItemDatas[(int)oppositeWeaponSlot].Item.IsWeapon() && equippedItemDatas[(int)oppositeWeaponSlot].Item.Weapon().isTwoHanded)))
                    UnequipItem(oppositeWeaponSlot);
            }

            // Assign the data
            equippedItemDatas[targetEquipSlotIndex] = newItemData;

            if (targetEquipSlot == EquipSlot.Back && newItemData.Item.IsBag())
                myUnit.BackpackInventoryManager.Initialize();
            else if (newItemData.Item is Quiver)
                myUnit.QuiverInventoryManager.Initialize();

            // Setup the target slot's item data/sprites and mesh if necessary
            SetupNewItemIcon(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newItemData);
            SetupEquipmentMesh(targetEquipSlot, newItemData);

            // Remove the item from its original character equipment or inventory
            if (InventoryUI.Instance.isDraggingItem)
            {
                if (InventoryUI.Instance.DraggedItem.myCharacterEquipment != null)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem.myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot] = null;
                }
                else if (InventoryUI.Instance.DraggedItem.myInventory != null)
                    InventoryUI.Instance.DraggedItem.myInventory.ItemDatas.Remove(newItemData);
            }
            else if (newItemData.InventorySlotCoordinate() != null && newItemData.InventorySlotCoordinate().myInventory.ContainsItemData(newItemData))
            {
                newItemData.InventorySlotCoordinate().myInventory.GetSlotFromCoordinate(newItemData.InventorySlotCoordinate()).ClearItem();
                newItemData.InventorySlotCoordinate().myInventory.RemoveItem(newItemData);
            }

            // Hide the dragged item
            if (InventoryUI.Instance.isDraggingItem)
                InventoryUI.Instance.DisableDraggedItem();
        }

        return true;
    }

    public void UnequipItem(EquipSlot equipSlot)
    {
        if (EquipSlotHasItem(equipSlot) == false)
            return;
        
        if (GetEquipmentSlot(equipSlot) != InventoryUI.Instance.parentSlotDraggedFrom)
        {
            // If this is the Unit's equipped backpack
            if (equipSlot == EquipSlot.Back && equippedItemDatas[(int)equipSlot].Item.IsBag() && myUnit.BackpackInventoryManager.HasAnyItems())
            {
                if (InventoryUI.Instance.GetContainerUI(myUnit.BackpackInventoryManager) != null)
                    InventoryUI.Instance.GetContainerUI(myUnit.BackpackInventoryManager).CloseContainerInventory();

                DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
            }
            else if (equippedItemDatas[(int)equipSlot].Item is Quiver && myUnit.QuiverInventoryManager.HasAnyItems())
            {
                if (InventoryUI.Instance.GetContainerUI(myUnit.QuiverInventoryManager) != null)
                    InventoryUI.Instance.GetContainerUI(myUnit.QuiverInventoryManager).CloseContainerInventory();

                DropItemManager.DropItem(this, equipSlot); // We can't add a bag with any items to an inventory, so just drop it
            }
        }

        // Try adding the item to the character's inventory
        if (myUnit.TryAddItemToInventories(equippedItemDatas[(int)equipSlot]))
        {
            if (slotVisualsCreated)
                GetEquipmentSlot(equipSlot).ClearItem();

            RemoveEquipmentMesh(equipSlot);
        }
        else // Else, drop the item
            DropItemManager.DropItem(this, equipSlot);

        equippedItemDatas[(int)equipSlot] = null;
    }

    void CreateSlotVisuals()
    {
        if (slotVisualsCreated)
        {
            Debug.LogWarning($"Slot visuals for {name}, owned by {myUnit.name}, has already been created...");
            return;
        }

        if (slots.Count > 0)
        {
            // Clear out any slots already in the list, so we can start from scratch
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].RemoveSlotHighlights();
                slots[i].ClearItem();
            }

            slots.Clear();
        }

        if (myUnit.IsPlayer())
            slots = InventoryUI.Instance.playerEquipmentSlots;
        else
            slots = InventoryUI.Instance.npcEquipmentSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetMyCharacterEquipment(this);
            if (slots[i] is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerEquipmentSlot = slots[i] as ContainerEquipmentSlot;
                if (containerEquipmentSlot.EquipSlot == EquipSlot.Back)
                    containerEquipmentSlot.SetContainerInventoryManager(containerEquipmentSlot.CharacterEquipment.MyUnit.BackpackInventoryManager);
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
                && ((equippedItemDatas[i].Item.IsWeapon() && equippedItemDatas[i].Item.Weapon().isTwoHanded && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)] != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item != null)
                || (EquipSlotHasItem((int)GetOppositeWeaponEquipSlot((EquipSlot)i)) && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item.IsWeapon() && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item.Weapon().isTwoHanded)))
            {
                Debug.LogError($"{myUnit} has 2 two-handed weapons equipped, or a two-handed weapon and a one-handed weapon equipped. That's too many weapons!");
            }
            else if (i == (int)EquipSlot.RightHeldItem1 && equippedItemDatas[i].Item.IsWeapon() && equippedItemDatas[i].Item.Weapon().isTwoHanded)
            {
                equippedItemDatas[(int)EquipSlot.LeftHeldItem1] = equippedItemDatas[i];
                equippedItemDatas[i] = null;

                SetupNewItemIcon(GetEquipmentSlotFromIndex((int)EquipSlot.LeftHeldItem1), equippedItemDatas[(int)EquipSlot.LeftHeldItem1]);
                SetupEquipmentMesh(EquipSlot.LeftHeldItem1, equippedItemDatas[i]);
            }
            else if (i == (int)EquipSlot.RightHeldItem2 && equippedItemDatas[i].Item.IsWeapon() && equippedItemDatas[i].Item.Weapon().isTwoHanded)
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
        }
    }

    public bool EquipSlotIsFull(EquipSlot equipSlot)
    {
        if (equippedItemDatas[(int)equipSlot] != null && equippedItemDatas[(int)equipSlot].Item != null)
            return true;

        if ((equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2) && EquipSlotHasItem(GetOppositeWeaponEquipSlot(equipSlot)) && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item.IsWeapon() && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item.Weapon().isTwoHanded)
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
        targetSlot.InventoryItem.UpdateStackSizeText();

        if (targetSlot.IsHeldItemSlot() && targetSlot.InventoryItem.itemData.Item.IsWeapon() && targetSlot.InventoryItem.itemData.Item.Weapon().isTwoHanded)
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

        if (currentWeaponSet == WeaponSet.One && (equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2))
            return;

        if (currentWeaponSet == WeaponSet.Two && (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1))
            return;

        if (IsHeldItemEquipSlot(equipSlot))
        {
            HeldItem heldItem = null;
            if (itemData.Item.IsMeleeWeapon())
                heldItem = HeldItemBasePool.Instance.GetMeleeWeaponBaseFromPool();
            else if (itemData.Item.IsRangedWeapon())
                heldItem = HeldItemBasePool.Instance.GetRangedWeaponBaseFromPool();
            else if (itemData.Item.IsShield())
                heldItem = HeldItemBasePool.Instance.GetShieldBaseFromPool();

            heldItem.SetupHeldItem(itemData, myUnit, equipSlot);

            ActionSystemUI.Instance.UpdateActionVisuals();
        }
        else if (equipSlot == EquipSlot.Helm || equipSlot == EquipSlot.BodyArmor)
        {
            myUnit.unitMeshManager.SetupMesh(equipSlot, (Equipment)itemData.Item);
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
            /*if (equipSlot == EquipSlot.LeftHeldItem1)
            {
                EquipSlot oppositeWeaponEquipSlot = GetOppositeWeaponEquipSlot(equipSlot);
                if (equippedItemDatas[(int)equipSlot].Item.IsWeapon() && equippedItemDatas[(int)equipSlot].Item.Weapon().isTwoHanded)
                    equipSlot = oppositeWeaponEquipSlot;
            }*/

            myUnit.unitMeshManager.ReturnHeldItemToPool(equipSlot);
        }
        else
            myUnit.unitMeshManager.RemoveMesh(equipSlot);
    }

    public void SwapWeaponSet()
    {
        if (currentWeaponSet == WeaponSet.One)
        {
            currentWeaponSet = WeaponSet.Two;

            // Remove held item bases for current held items
            myUnit.unitMeshManager.ReturnHeldItemToPool(EquipSlot.LeftHeldItem1);
            myUnit.unitMeshManager.ReturnHeldItemToPool(EquipSlot.RightHeldItem1);

            // Create held item bases for the other weapon set
            SetupEquipmentMesh(EquipSlot.LeftHeldItem2, equippedItemDatas[(int)EquipSlot.LeftHeldItem2]);
            SetupEquipmentMesh(EquipSlot.RightHeldItem2, equippedItemDatas[(int)EquipSlot.RightHeldItem2]);

            if (slotVisualsCreated)
            {
                // Hide current held item icons
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).DisableSlotImage();
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).InventoryItem.DisableIconImage();
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).DisableSlotImage();
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).InventoryItem.DisableIconImage();

                // Show held item icons for the other weapon set
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).EnableSlotImage();
                if (myUnit.CharacterEquipment.EquipSlotIsFull(EquipSlot.LeftHeldItem2))
                    myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).InventoryItem.EnableIconImage();

                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).EnableSlotImage();
                if (myUnit.CharacterEquipment.EquipSlotIsFull(EquipSlot.RightHeldItem2))
                    myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).InventoryItem.EnableIconImage();
            }
        }
        else
        {
            currentWeaponSet = WeaponSet.One;

            // Remove held item bases for current held items
            myUnit.unitMeshManager.ReturnHeldItemToPool(EquipSlot.LeftHeldItem2);
            myUnit.unitMeshManager.ReturnHeldItemToPool(EquipSlot.RightHeldItem2);

            // Create held item bases for the other weapon set
            SetupEquipmentMesh(EquipSlot.LeftHeldItem1, equippedItemDatas[(int)EquipSlot.LeftHeldItem1]);
            SetupEquipmentMesh(EquipSlot.RightHeldItem1, equippedItemDatas[(int)EquipSlot.RightHeldItem1]);

            if (slotVisualsCreated)
            {
                // Hide current held item icons
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).DisableSlotImage();
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2).InventoryItem.DisableIconImage();
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).DisableSlotImage();
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2).InventoryItem.DisableIconImage();

                // Show held item icons for the other weapon set
                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).EnableSlotImage();
                if (myUnit.CharacterEquipment.EquipSlotIsFull(EquipSlot.LeftHeldItem1))
                    myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1).InventoryItem.EnableIconImage();

                myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).EnableSlotImage();
                if (myUnit.CharacterEquipment.EquipSlotIsFull(EquipSlot.RightHeldItem1))
                    myUnit.CharacterEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1).InventoryItem.EnableIconImage();
            }
        }

        ActionSystemUI.Instance.UpdateActionVisuals();
    }

    public bool IsDualWielding() => 
        (currentWeaponSet == WeaponSet.One && EquipSlotHasItem(EquipSlot.LeftHeldItem1) && EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.IsMeleeWeapon() && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.IsMeleeWeapon())
        || (currentWeaponSet == WeaponSet.Two && EquipSlotHasItem(EquipSlot.LeftHeldItem2) && EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.IsMeleeWeapon() && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.IsMeleeWeapon());

    public bool MeleeWeaponEquipped() => 
        (currentWeaponSet == WeaponSet.One && ((EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.IsMeleeWeapon()) || (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.IsMeleeWeapon())))
        || (currentWeaponSet == WeaponSet.Two && ((EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.IsMeleeWeapon()) || (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.IsMeleeWeapon())));

    public bool RangedWeaponEquipped() => 
        (currentWeaponSet == WeaponSet.One && ((EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.IsRangedWeapon()) || (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.IsRangedWeapon())))
        || (currentWeaponSet == WeaponSet.Two && ((EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.IsRangedWeapon()) || (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.IsRangedWeapon())));

    public bool ShieldEquipped() =>
        (currentWeaponSet == WeaponSet.One && ((EquipSlotHasItem(EquipSlot.LeftHeldItem1) && equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.IsShield()) || (EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.IsShield())))
        || (currentWeaponSet == WeaponSet.Two && ((EquipSlotHasItem(EquipSlot.LeftHeldItem2) && equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.IsShield()) || (EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.IsShield())));

    public bool IsUnarmed() => 
        (currentWeaponSet == WeaponSet.One && (EquipSlotHasItem(EquipSlot.LeftHeldItem1) == false || equippedItemDatas[(int)EquipSlot.LeftHeldItem1].Item.IsWeapon() == false) && (EquipSlotHasItem(EquipSlot.RightHeldItem1) == false || equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.IsWeapon() == false))
        || (currentWeaponSet == WeaponSet.Two && (EquipSlotHasItem(EquipSlot.LeftHeldItem2) == false || equippedItemDatas[(int)EquipSlot.LeftHeldItem2].Item.IsWeapon() == false) && (EquipSlotHasItem(EquipSlot.RightHeldItem2) == false || equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.IsWeapon() == false));

    public bool IsHeldItemEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2;

    public bool BackpackEquipped() => EquipSlotHasItem(EquipSlot.Back) && equippedItemDatas[(int)EquipSlot.Back].Item.IsBag();

    public bool QuiverEquipped() => (currentWeaponSet == WeaponSet.One && EquipSlotHasItem(EquipSlot.RightHeldItem1) && equippedItemDatas[(int)EquipSlot.RightHeldItem1].Item.IsPortableContainer()) 
        || (currentWeaponSet == WeaponSet.Two && EquipSlotHasItem(EquipSlot.RightHeldItem2) && equippedItemDatas[(int)EquipSlot.RightHeldItem2].Item.IsPortableContainer());

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

    public void RemoveEquipment(EquipSlot equipSlot) => equippedItemDatas[(int)equipSlot] = null;

    public ItemData[] EquippedItemDatas => equippedItemDatas;

    public Unit MyUnit => myUnit;

    public bool SlotVisualsCreated => slotVisualsCreated;
}
