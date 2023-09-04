using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EquipSlot { LeftHeldItem, RightHeldItem, Helm, BodyArmor, LegArmor, Gloves, Boots, Backpack }

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField] Unit myUnit;

    [SerializeField] ItemData[] equippedItemDatas = new ItemData[Enum.GetValues(typeof(EquipSlot)).Length];

    [NamedArray(new string[] { "Left Held Item", "Right Held Item", "Helm", "Body Armor", "Leg Armor", "Gloves", "Boots", "Backpack" })]
    [SerializeField] Equipment[] startingEquipment = new Equipment[Enum.GetValues(typeof(EquipSlot)).Length];

    public List<EquipmentSlot> slots { get; private set; }

    bool slotVisualsCreated;

    void Awake()
    {
        slots = new List<EquipmentSlot>();

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
            ItemData newItemData = new ItemData(equippedItemDatas[(int)EquipSlot.RightHeldItem].Item());
            TryEquipItem(newItemData);
        }
    }*/

    public bool TryEquipItem(ItemData newItemData)
    {
        EquipSlot targetEquipSlot = newItemData.Item().Equipment().EquipSlot();
        if (newItemData.Item().IsWeapon())
        {
            if (newItemData.Item().Weapon().isTwoHanded)
                targetEquipSlot = EquipSlot.LeftHeldItem;
            else 
            {
                EquipSlot oppositeWeaponEquipSlot = GetOppositeWeaponEquipSlot(targetEquipSlot);
                if (equippedItemDatas[(int)oppositeWeaponEquipSlot] == null || equippedItemDatas[(int)oppositeWeaponEquipSlot].Item() == null)
                    targetEquipSlot = oppositeWeaponEquipSlot;
            }
        }

        if (TryAddItemAt(targetEquipSlot, newItemData))
        {
            if (slotVisualsCreated)
                SetupNewItem(GetEquipmentSlot(targetEquipSlot), newItemData);
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
        if (newItemData.Item().IsWeapon() && newItemData.Item().Weapon().isTwoHanded)
            targetEquipSlotIndex = (int)EquipSlot.LeftHeldItem;

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
                if (equippedItemDatas[(int)oppositeWeaponSlot] != null && equippedItemDatas[(int)oppositeWeaponSlot].Item() != null && (newItemData.Item().Weapon().isTwoHanded || equippedItemDatas[(int)oppositeWeaponSlot].Item().Weapon().isTwoHanded))
                {
                    // Try adding the item to the character's inventory
                    if (myUnit.TryAddItemToInventories(equippedItemDatas[(int)oppositeWeaponSlot]) && slotVisualsCreated)
                        GetEquipmentSlot(oppositeWeaponSlot).ClearItem();
                    else // Else, drop the item
                        DropItem(oppositeWeaponSlot);

                    equippedItemDatas[(int)oppositeWeaponSlot] = null;
                }
            }

            // Try adding the target slot's item to the character's inventory
            if (equippedItemDatas[(int)targetEquipSlot] != null && equippedItemDatas[(int)targetEquipSlot].Item() != null)
            {
                if (myUnit.TryAddItemToInventories(equippedItemDatas[(int)targetEquipSlot]) && slotVisualsCreated)
                    GetEquipmentSlot(targetEquipSlot).ClearItem();
                else // Else, drop the item
                    DropItem(targetEquipSlot);
            }

            // Assign the data
            equippedItemDatas[targetEquipSlotIndex] = newItemData;

            if (targetEquipSlot == EquipSlot.RightHeldItem && newItemData.Item().IsWeapon() && newItemData.Item().Weapon().isTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem] = null;

            // Setup the target slot's item data and sprites
            if (slotVisualsCreated)
                SetupNewItem(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newItemData);

            // Remove the item from its original character equipment or inventory
            if (InventoryUI.Instance.isDraggingItem)
            {
                if (InventoryUI.Instance.DraggedItem().myCharacterEquipment != null)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem().myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot()] = null;
                }
                else if (InventoryUI.Instance.DraggedItem().myInventory != null)
                    InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);
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
                if (equippedItemDatas[(int)oppositeWeaponSlot] != null && equippedItemDatas[(int)oppositeWeaponSlot].Item() != null && (newItemData.Item().Weapon().isTwoHanded || equippedItemDatas[(int)oppositeWeaponSlot].Item().Weapon().isTwoHanded))
                {
                    // Try adding the item to the character's inventory
                    if (myUnit.TryAddItemToInventories(equippedItemDatas[(int)oppositeWeaponSlot]) && slotVisualsCreated)
                        GetEquipmentSlot(oppositeWeaponSlot).ClearItem();
                    else // Else, drop the item
                        DropItem(oppositeWeaponSlot);

                    equippedItemDatas[(int)oppositeWeaponSlot] = null;
                }
            }

            // Assign the data
            equippedItemDatas[targetEquipSlotIndex] = newItemData;

            // Setup the target slot's item data and sprites
            if (slotVisualsCreated)
                SetupNewItem(GetEquipmentSlotFromIndex(targetEquipSlotIndex), newItemData);

            // Remove the item from its original character equipment or inventory
            if (InventoryUI.Instance.isDraggingItem)
            {
                if (InventoryUI.Instance.DraggedItem().myCharacterEquipment != null)
                {
                    EquipmentSlot equipmentSlotDraggedFrom = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;
                    InventoryUI.Instance.DraggedItem().myCharacterEquipment.equippedItemDatas[(int)equipmentSlotDraggedFrom.EquipSlot()] = null;
                }
                else if (InventoryUI.Instance.DraggedItem().myInventory != null)
                    InventoryUI.Instance.DraggedItem().myInventory.ItemDatas().Remove(newItemData);
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
        }

        slotVisualsCreated = true;

        SetupItems();
    }

    public void SetupItems()
    {
        for (int i = 0; i < equippedItemDatas.Length; i++)
        {
            if (equippedItemDatas[i].Item() == null)
                continue;

            equippedItemDatas[i].RandomizeData();

            if ((i == (int)EquipSlot.LeftHeldItem || i == (int)EquipSlot.RightHeldItem)
                && ((equippedItemDatas[i].Item().Weapon().isTwoHanded && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)] != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item() != null)
                || (equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)] != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item() != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot((EquipSlot)i)].Item().Weapon().isTwoHanded)))
            {
                Debug.LogError($"{myUnit} has 2 two-handed weapons equipped, or a two-handed weapon and a one-handed weapon equipped. That's too many weapons!");
            }
            else if (i == (int)EquipSlot.RightHeldItem && equippedItemDatas[i].Item().IsWeapon() && equippedItemDatas[i].Item().Weapon().isTwoHanded)
            {
                equippedItemDatas[(int)EquipSlot.LeftHeldItem] = equippedItemDatas[i];
                equippedItemDatas[i] = null;

                if (slotVisualsCreated)
                    SetupNewItem(GetEquipmentSlotFromIndex((int)EquipSlot.LeftHeldItem), equippedItemDatas[(int)EquipSlot.LeftHeldItem]);
            }
            else if (slotVisualsCreated)
                SetupNewItem(GetEquipmentSlotFromIndex(i), equippedItemDatas[i]);
        }
    }

    public void DropItem(EquipSlot equipSlot)
    {
        if (EquipSlotIsFull(equipSlot) == false)
            return;

        if (InventoryUI.Instance.isDraggingItem && InventoryUI.Instance.DraggedItem().itemData == equippedItemDatas[(int)equipSlot] && (equippedItemDatas[(int)equipSlot] == null || equippedItemDatas[(int)equipSlot].Item() == null))
            return;

        LooseItem looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

        Vector3 dropDirection = GetDropDirection(myUnit);

        SetupItemDrop(looseItem, equippedItemDatas[(int)equipSlot], dropDirection);

        float randomForceMagnitude = Random.Range(50f, 300f);

        // Apply force to the dropped item
        looseItem.RigidBody().AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

        if (equippedItemDatas[(int)equipSlot] != null && equippedItemDatas[(int)equipSlot].Item() != null)
            equippedItemDatas[(int)equipSlot] = null;

        if (equippedItemDatas[(int)equipSlot] == InventoryUI.Instance.DraggedItem().itemData)
        {
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            InventoryUI.Instance.DisableDraggedItem();
        }
        else if (slotVisualsCreated)
            GetEquipmentSlot(equipSlot).ClearItem();
    }

    public virtual void SetupItemDrop(LooseItem looseItem, ItemData itemData, Vector3 dropDirection)
    {
        if (itemData.Item().pickupMesh != null)
            looseItem.SetupMesh(itemData.Item().pickupMesh, itemData.Item().pickupMeshRendererMaterial);
        else if (itemData.Item().meshes[0] != null)
            looseItem.SetupMesh(itemData.Item().meshes[0], itemData.Item().meshRendererMaterials[0]);
        else
            Debug.LogWarning("Mesh info has not been set on the ScriptableObject for: " + itemData.Item().name);

        looseItem.SetItemData(itemData);
        itemData.SetInventorySlotCoordinate(null);

        // Set the LooseItem's position to be slightly in front of the Unit dropping the item
        looseItem.transform.position = myUnit.transform.position + new Vector3(0, myUnit.ShoulderHeight(), 0) + (dropDirection / 2);

        // Randomize the rotation and set active
        looseItem.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)));
        looseItem.gameObject.SetActive(true);
    }

    Vector3 GetDropDirection(Unit myUnit)
    {
        Vector3 forceDirection = myUnit.transform.forward; // In front of myUnit
        float raycastDistance = 1.2f;
        if (Physics.Raycast(myUnit.transform.position, forceDirection, out RaycastHit hit, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
        {
            forceDirection = -myUnit.transform.forward; // Behind myUnit
            if (Physics.Raycast(myUnit.transform.position, forceDirection, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
            {
                forceDirection = myUnit.transform.right; // Right of myUnit
                if (Physics.Raycast(myUnit.transform.position, forceDirection, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
                {
                    forceDirection = -myUnit.transform.right; // Left of myUnit
                    if (Physics.Raycast(myUnit.transform.position, forceDirection, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
                        forceDirection = myUnit.transform.up; // Above myUnit
                }
            }
        }

        // Add some randomness to the force direction
        float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees
        Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
        forceDirection = randomRotation * forceDirection;

        return forceDirection;
    }

    public bool EquipSlotIsFull(EquipSlot equipSlot)
    {
        if (equippedItemDatas[(int)equipSlot] != null && equippedItemDatas[(int)equipSlot].Item() != null)
            return true;

        if (equipSlot == EquipSlot.RightHeldItem && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)] != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item() != null && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item().IsWeapon() && equippedItemDatas[(int)GetOppositeWeaponEquipSlot(equipSlot)].Item().Weapon().isTwoHanded)
            return true;
        return false;
    }

    public EquipSlot GetOppositeWeaponEquipSlot(EquipSlot equipSlot)
    {
        if (equipSlot == EquipSlot.LeftHeldItem && equipSlot == EquipSlot.RightHeldItem)
        {
            Debug.LogWarning($"{equipSlot} is not a weapon slot...");
            return equipSlot;
        }

        if (equipSlot == EquipSlot.LeftHeldItem)
            return EquipSlot.RightHeldItem;
        else
            return EquipSlot.LeftHeldItem;
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

            SetupEquipmentMesh(oppositeWeaponSlot.EquipSlot(), newItemData);
        }
        else
            SetupEquipmentMesh(targetSlot.EquipSlot(), newItemData);
    }

    void SetupEquipmentMesh(EquipSlot equipSlot, ItemData itemData)
    {
        // We only show meshes for these types of equipment:
        if (IsHeldItemEquipSlot(equipSlot) == false && equipSlot != EquipSlot.Helm && equipSlot != EquipSlot.BodyArmor)
            return;

        if (IsHeldItemEquipSlot(equipSlot))
        {
            HeldItem heldItem = null;
            if (itemData.Item().IsMeleeWeapon())
                heldItem = HeldItemBasePool.Instance.GetMeleeWeaponBaseFromPool();
            else if (itemData.Item().IsRangedWeapon())
                heldItem = HeldItemBasePool.Instance.GetRangedWeaponBaseFromPool();
            else if (itemData.Item().IsShield())
                heldItem = HeldItemBasePool.Instance.GetShieldBaseFromPool();

            heldItem.SetupHeldItem(itemData, myUnit, equipSlot);
        }
        else if (equipSlot == EquipSlot.Helm || equipSlot == EquipSlot.BodyArmor)
        {
            myUnit.unitMeshManager.SetupMesh(equipSlot, (Equipment)itemData.Item());
        }
    }

    void RemoveEquipmentMesh(EquipSlot equipSlot)
    {
        if (EquipSlotIsFull(equipSlot) == false)
            return;

        // We only show meshes for these types of equipment:
        if (IsHeldItemEquipSlot(equipSlot) == false && equipSlot != EquipSlot.Helm && equipSlot != EquipSlot.BodyArmor)
            return;
        
        if (IsHeldItemEquipSlot(equipSlot))
        {
            if (equippedItemDatas[(int)equipSlot] == null || equippedItemDatas[(int)equipSlot].Item() == null)
            {
                EquipSlot oppositeWeaponEquipSlot = GetOppositeWeaponEquipSlot(equipSlot);
                if (equippedItemDatas[(int)oppositeWeaponEquipSlot] != null && equippedItemDatas[(int)oppositeWeaponEquipSlot].Item() != null && equippedItemDatas[(int)oppositeWeaponEquipSlot].Item().IsWeapon() && equippedItemDatas[(int)oppositeWeaponEquipSlot].Item().Weapon().isTwoHanded)
                    equipSlot = oppositeWeaponEquipSlot;
            }
        }

        myUnit.unitMeshManager.HideMesh(equipSlot);
    }

    public bool IsDualWielding() => equippedItemDatas[(int)EquipSlot.LeftHeldItem] != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.RightHeldItem] != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item().IsMeleeWeapon() && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item().IsMeleeWeapon();

    public bool MeleeWeaponEquipped() => (equippedItemDatas[(int)EquipSlot.LeftHeldItem] != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item().IsMeleeWeapon()) || (equippedItemDatas[(int)EquipSlot.RightHeldItem] != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item().IsMeleeWeapon());

    public bool RangedWeaponEquipped() => (equippedItemDatas[(int)EquipSlot.RightHeldItem] != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item().IsRangedWeapon()) || (equippedItemDatas[(int)EquipSlot.LeftHeldItem] != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item().IsRangedWeapon());

    public bool ShieldEquipped() => (equippedItemDatas[(int)EquipSlot.LeftHeldItem] != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item().IsShield()) || (equippedItemDatas[(int)EquipSlot.RightHeldItem] != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item() != null && equippedItemDatas[(int)EquipSlot.RightHeldItem].Item().IsShield());

    public bool IsUnarmed() => (equippedItemDatas[(int)EquipSlot.LeftHeldItem] == null || equippedItemDatas[(int)EquipSlot.LeftHeldItem].Item() == null) && (equippedItemDatas[(int)EquipSlot.RightHeldItem] == null || equippedItemDatas[(int)EquipSlot.RightHeldItem].Item() == null);

    public bool IsHeldItemEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.LeftHeldItem || equipSlot == EquipSlot.RightHeldItem;

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
