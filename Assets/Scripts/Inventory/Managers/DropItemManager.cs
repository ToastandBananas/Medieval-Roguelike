using UnityEngine;

public class DropItemManager : MonoBehaviour
{
    public static void DropItem(Unit unit, Inventory inventory, ItemData itemDataToDrop)
    {
        if (inventory.ItemDatas.Contains(itemDataToDrop) == false)
            return;

        if (itemDataToDrop.Item == null)
        {
            Debug.LogWarning("Item you're trying to drop from inventory is null...");
            inventory.RemoveItem(itemDataToDrop);
            return;
        }

        // The only time Unit will ever be null is when the Player is dropping an item from a container's inventory
        if (unit == null)
            unit = UnitManager.Instance.player;

        LooseItem looseItem;
        if (itemDataToDrop.Item.IsBag() || itemDataToDrop.Item.IsPortableContainer())
            looseItem = LooseItemPool.Instance.GetLooseContainerItemFromPool();
        else
            looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

        Vector3 dropDirection = GetDropDirection(unit);

        SetupItemDrop(looseItem, itemDataToDrop, unit, dropDirection);

        float randomForceMagnitude = Random.Range(50f, 300f);

        // Apply force to the dropped item
        looseItem.RigidBody.AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

        inventory.RemoveItem(itemDataToDrop);

        if (itemDataToDrop == InventoryUI.Instance.DraggedItem.itemData)
        {
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            InventoryUI.Instance.DisableDraggedItem();
        }
        else if (inventory.SlotVisualsCreated)
            inventory.GetSlotFromItemData(itemDataToDrop).ClearItem();
    }

    public static void DropItem(CharacterEquipment characterEquipment, EquipSlot equipSlot)
    {
        if (characterEquipment.EquipSlotIsFull(equipSlot) == false)
            return;

        if (InventoryUI.Instance.isDraggingItem && InventoryUI.Instance.DraggedItem.itemData == characterEquipment.EquippedItemDatas[(int)equipSlot] && (characterEquipment.EquippedItemDatas[(int)equipSlot] == null || characterEquipment.EquippedItemDatas[(int)equipSlot].Item == null))
        {
            Debug.LogWarning($"Item you're trying to drop from {characterEquipment.MyUnit.name}'s equipment is null...");
            return;
        }

        LooseItem looseItem;
        if (characterEquipment.EquippedItemDatas[(int)equipSlot].Item.IsBag() || characterEquipment.EquippedItemDatas[(int)equipSlot].Item.IsPortableContainer())
            looseItem = LooseItemPool.Instance.GetLooseContainerItemFromPool();
        else
            looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

        Vector3 dropDirection = GetDropDirection(characterEquipment.MyUnit);

        if (characterEquipment.EquippedItemDatas[(int)equipSlot].Item.IsWeapon() || characterEquipment.EquippedItemDatas[(int)equipSlot].Item.IsShield())
            SetupHeldItemDrop(characterEquipment.MyUnit.unitMeshManager.GetHeldItemFromItemData(characterEquipment.EquippedItemDatas[(int)equipSlot]).transform, looseItem, characterEquipment.EquippedItemDatas[(int)equipSlot]);
        else if (characterEquipment.EquippedItemDatas[(int)equipSlot].Item.IsBag())
            SetupContainerItemDrop(characterEquipment, equipSlot, looseItem, characterEquipment.EquippedItemDatas[(int)equipSlot], characterEquipment.MyUnit, dropDirection);
        else
            SetupItemDrop(looseItem, characterEquipment.EquippedItemDatas[(int)equipSlot], characterEquipment.MyUnit, dropDirection);

        characterEquipment.RemoveEquipmentMesh(equipSlot);

        float randomForceMagnitude = Random.Range(50f, 300f);

        // Apply force to the dropped item
        looseItem.RigidBody.AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

        if (characterEquipment.EquippedItemDatas[(int)equipSlot] == InventoryUI.Instance.DraggedItem.itemData)
        {
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            InventoryUI.Instance.DisableDraggedItem();
        }
        else if (characterEquipment.SlotVisualsCreated)
            characterEquipment.GetEquipmentSlot(equipSlot).ClearItem();

        characterEquipment.EquippedItemDatas[(int)equipSlot] = null;
    }

    public static void DropHeldItemOnDeath(HeldItem heldItem, Unit unit, Transform attackerTransform, bool diedForward)
    {
        LooseItem looseWeapon = LooseItemPool.Instance.GetLooseItemFromPool();

        float randomForceMagnitude = Random.Range(100f, 600f);
        float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees

        // Get the attacker's position and the character's position
        Vector3 attackerPosition = attackerTransform.position;
        Vector3 unitPosition = unit.transform.parent.position;

        // Calculate the force direction (depending on whether they fall forward or backward)
        Vector3 forceDirection;
        if (diedForward)
            forceDirection = (attackerPosition - unitPosition).normalized;
        else
            forceDirection = (unitPosition - attackerPosition).normalized;

        // Add some randomness to the force direction
        Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
        forceDirection = randomRotation * forceDirection;

        SetupHeldItemDrop(heldItem.transform, looseWeapon, heldItem.ItemData);

        if (heldItem is HeldRangedWeapon)
        {
            HeldRangedWeapon heldRangedWeapon = heldItem as HeldRangedWeapon;
            if (heldRangedWeapon.isLoaded)
            {
                LooseItem looseProjectile = LooseItemPool.Instance.GetLooseItemFromPool();
                SetupHeldItemDrop(heldRangedWeapon.loadedProjectile.transform, looseProjectile, heldRangedWeapon.loadedProjectile.ItemData);
                heldRangedWeapon.loadedProjectile.Disable();

                looseProjectile.RigidBody.AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);
                if (unit != UnitManager.Instance.player && UnitManager.Instance.player.vision.IsVisible(unit) == false)
                    looseProjectile.HideMeshRenderer();
            }
        }

        // Get the Rigidbody component(s) and apply force
        looseWeapon.RigidBody.AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);

        if (unit != UnitManager.Instance.player && UnitManager.Instance.player.vision.IsVisible(unit) == false)
            looseWeapon.HideMeshRenderer();

        // Get rid of the HeldItem
        if (heldItem == unit.unitMeshManager.rightHeldItem || (heldItem.itemData.Item.IsWeapon() && heldItem.itemData.Item.Weapon().isTwoHanded))
        {
            if (unit.CharacterEquipment.currentWeaponSet == WeaponSet.One)
            {
                unit.CharacterEquipment.RemoveEquipmentMesh(EquipSlot.RightHeldItem1);
                unit.CharacterEquipment.RemoveEquipment(EquipSlot.RightHeldItem1);
            }
            else
            {
                unit.CharacterEquipment.RemoveEquipmentMesh(EquipSlot.RightHeldItem2);
                unit.CharacterEquipment.RemoveEquipment(EquipSlot.RightHeldItem2);
            }
        }
        else
        {
            if (unit.CharacterEquipment.currentWeaponSet == WeaponSet.One)
            {
                unit.CharacterEquipment.RemoveEquipmentMesh(EquipSlot.LeftHeldItem1);
                unit.CharacterEquipment.RemoveEquipment(EquipSlot.LeftHeldItem1);
            }
            else
            {
                unit.CharacterEquipment.RemoveEquipmentMesh(EquipSlot.LeftHeldItem2);
                unit.CharacterEquipment.RemoveEquipment(EquipSlot.LeftHeldItem2);
            }
        }
    }

    static void SetupItemDrop(LooseItem looseItem, ItemData itemData, Unit unit, Vector3 dropDirection)
    {
        SetupLooseItem(looseItem, itemData);

        // Set the LooseItem's position to be slightly in front of the Unit dropping the item
        looseItem.transform.position = unit.transform.position + new Vector3(0, unit.ShoulderHeight(), 0) + (dropDirection / 2);

        // Randomize the rotation and set active
        looseItem.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)));
        looseItem.gameObject.SetActive(true);
    }

    static void SetupContainerItemDrop(CharacterEquipment characterEquipment, EquipSlot equipSlot, LooseItem looseItem, ItemData itemData, Unit unit, Vector3 dropDirection)
    {
        SetupItemDrop(looseItem, itemData, unit, dropDirection);

        if (equipSlot == EquipSlot.Back && itemData.Item.IsBag())
        {
            if (InventoryUI.Instance.GetContainerUI(characterEquipment.MyUnit.BackpackInventoryManager) != null)
                InventoryUI.Instance.GetContainerUI(characterEquipment.MyUnit.BackpackInventoryManager).CloseContainerInventory();

            LooseContainerItem looseContainerItem = looseItem as LooseContainerItem;
            looseContainerItem.TransferInventory(characterEquipment.MyUnit.BackpackInventoryManager);
        }
        else if ((equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2) && itemData.Item is Quiver)
        {
            if (InventoryUI.Instance.GetContainerUI(characterEquipment.MyUnit.QuiverInventoryManager) != null)
                InventoryUI.Instance.GetContainerUI(characterEquipment.MyUnit.QuiverInventoryManager).CloseContainerInventory();

            LooseContainerItem looseContainerItem = looseItem as LooseContainerItem;
            looseContainerItem.TransferInventory(characterEquipment.MyUnit.QuiverInventoryManager);
        }
    }

    static void SetupHeldItemDrop(Transform itemDropTransform, LooseItem looseItem, ItemData itemData)
    {
        SetupLooseItem(looseItem, itemData);

        // Set the LooseItem's position to match the HeldItem before we add force
        looseItem.transform.position = itemDropTransform.position;
        looseItem.transform.rotation = itemDropTransform.rotation;
        looseItem.gameObject.SetActive(true);
    }

    static void SetupLooseItem(LooseItem looseItem, ItemData itemData)
    {
        if (itemData.Item.pickupMesh != null)
            looseItem.SetupMesh(itemData.Item.pickupMesh, itemData.Item.pickupMeshRendererMaterial);
        else if (itemData.Item.meshes[0] != null)
            looseItem.SetupMesh(itemData.Item.meshes[0], itemData.Item.meshRendererMaterials[0]);
        else
            Debug.LogWarning($"Mesh info has not been set on the ScriptableObject for: {itemData.Item.name}");

        looseItem.SetItemData(itemData);
        looseItem.name = itemData.Item.name;
        itemData.SetInventorySlotCoordinate(null);
    }

    static Vector3 GetDropDirection(Unit unit)
    {
        Vector3 forceDirection = unit.transform.forward; // In front of Unit
        float raycastDistance = 1.2f;
        if (Physics.Raycast(unit.transform.position, forceDirection, out RaycastHit hit, raycastDistance, unit.unitActionHandler.AttackObstacleMask()))
        {
            forceDirection = -unit.transform.forward; // Behind Unit
            if (Physics.Raycast(unit.transform.position, forceDirection, raycastDistance, unit.unitActionHandler.AttackObstacleMask()))
            {
                forceDirection = unit.transform.right; // Right of Unit
                if (Physics.Raycast(unit.transform.position, forceDirection, raycastDistance, unit.unitActionHandler.AttackObstacleMask()))
                {
                    forceDirection = -unit.transform.right; // Left of Unit
                    if (Physics.Raycast(unit.transform.position, forceDirection, raycastDistance, unit.unitActionHandler.AttackObstacleMask()))
                        forceDirection = unit.transform.up; // Above Unit
                }
            }
        }

        // Add some randomness to the force direction
        float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees
        Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
        forceDirection = randomRotation * forceDirection;

        return forceDirection;
    }
}
