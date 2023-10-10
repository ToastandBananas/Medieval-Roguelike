using UnityEngine;
using InteractableObjects;
using ActionSystem;
using UnitSystem;

namespace InventorySystem
{
    public class DropItemManager : MonoBehaviour
    {
        public static void DropItem(Unit unit, Inventory inventory, ItemData itemDataToDrop)
        {
            if (itemDataToDrop.Item == null)
            {
                Debug.LogWarning("Item you're trying to drop from inventory is null...");
                if (inventory != null)
                    inventory.RemoveItem(itemDataToDrop);
                return;
            }

            // The only time Unit will ever be null is when the Player is dropping an item from a container's inventory
            if (unit == null)
                unit = UnitManager.player;

            LooseItem looseItem;
            if (itemDataToDrop.Item is Backpack)
                looseItem = LooseItemPool.Instance.GetLooseContainerItemFromPool();
            else if (itemDataToDrop.Item is Quiver)
                looseItem = LooseItemPool.Instance.GetLooseQuiverItemFromPool();
            else
                looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

            Vector3 dropDirection = GetDropDirection(unit);

            SetupItemDrop(looseItem, itemDataToDrop, unit, dropDirection);

            float randomForceMagnitude = Random.Range(looseItem.RigidBody.mass * 0.8f, looseItem.RigidBody.mass * 3f);

            // Apply force to the dropped item
            looseItem.RigidBody.AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

            if (inventory != null)
                inventory.RemoveItem(itemDataToDrop);

            if (itemDataToDrop == InventoryUI.DraggedItem.itemData)
                InventoryUI.DisableDraggedItem();
        }

        public static void DropItem(UnitEquipment unitEquipment, EquipSlot equipSlot)
        {
            if (unitEquipment.EquipSlotIsFull(equipSlot) == false)
                return;

            if (InventoryUI.isDraggingItem && InventoryUI.DraggedItem.itemData == unitEquipment.EquippedItemDatas[(int)equipSlot] && (unitEquipment.EquippedItemDatas[(int)equipSlot] == null || unitEquipment.EquippedItemDatas[(int)equipSlot].Item == null))
            {
                Debug.LogWarning($"Item you're trying to drop from {unitEquipment.MyUnit.name}'s equipment is null...");
                return;
            }

            LooseItem looseItem;
            if (unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Backpack)
                looseItem = LooseItemPool.Instance.GetLooseContainerItemFromPool();
            else if (unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Quiver)
                looseItem = LooseItemPool.Instance.GetLooseQuiverItemFromPool();
            else
                looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

            Vector3 dropDirection = GetDropDirection(unitEquipment.MyUnit);

            if (unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Weapon || unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Shield)
                SetupHeldItemDrop(unitEquipment.MyUnit.unitMeshManager.GetHeldItemFromItemData(unitEquipment.EquippedItemDatas[(int)equipSlot]), looseItem);
            else if ((equipSlot == EquipSlot.Back && unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Backpack) || (equipSlot == EquipSlot.Quiver && unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Quiver))
                SetupContainerItemDrop(unitEquipment, equipSlot, looseItem, unitEquipment.EquippedItemDatas[(int)equipSlot], unitEquipment.MyUnit, dropDirection);
            else
                SetupItemDrop(looseItem, unitEquipment.EquippedItemDatas[(int)equipSlot], unitEquipment.MyUnit, dropDirection);

            //if (characterEquipment.slotVisualsCreated && equipSlot == EquipSlot.Quiver && characterEquipment.EquippedItemDatas[(int)equipSlot].Item is Quiver)
            //characterEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.HideQuiverSprites();

            unitEquipment.RemoveActions(unitEquipment.EquippedItemDatas[(int)equipSlot].Item as Equipment);
            unitEquipment.RemoveEquipmentMesh(equipSlot);

            float randomForceMagnitude = Random.Range(looseItem.RigidBody.mass * 0.8f, looseItem.RigidBody.mass * 3f);

            // Apply force to the dropped item
            looseItem.RigidBody.AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

            if (unitEquipment.EquippedItemDatas[(int)equipSlot] == InventoryUI.DraggedItem.itemData)
            {
                if (InventoryUI.parentSlotDraggedFrom != null)
                    InventoryUI.parentSlotDraggedFrom.ClearItem();

                InventoryUI.DisableDraggedItem();
            }
            else if (unitEquipment.slotVisualsCreated)
                unitEquipment.GetEquipmentSlot(equipSlot).ClearItem();

            unitEquipment.EquippedItemDatas[(int)equipSlot] = null;

            ActionSystemUI.UpdateActionVisuals();
        }

        public static void DropHeldItemOnDeath(HeldItem heldItem, Unit unit, Transform attackerTransform, bool diedForward)
        {
            LooseItem looseWeapon = LooseItemPool.Instance.GetLooseItemFromPool();

            float randomForceMagnitude = Random.Range(looseWeapon.RigidBody.mass, looseWeapon.RigidBody.mass * 6f);
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

            SetupHeldItemDrop(heldItem, looseWeapon);

            if (heldItem is HeldRangedWeapon)
            {
                HeldRangedWeapon heldRangedWeapon = heldItem as HeldRangedWeapon;
                if (heldRangedWeapon.isLoaded)
                {
                    heldRangedWeapon.loadedProjectile.SetupNewLooseItem(false, out LooseItem looseProjectile);
                    looseProjectile.RigidBody.AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);
                    if (unit != UnitManager.player && UnitManager.player.vision.IsVisible(unit) == false)
                        looseProjectile.HideMeshRenderer();
                }
            }

            // Get the Rigidbody component(s) and apply force
            looseWeapon.RigidBody.AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);

            if (unit != UnitManager.player && UnitManager.player.vision.IsVisible(unit) == false)
                looseWeapon.HideMeshRenderer();

            // Get rid of the HeldItem
            EquipSlot equipSlot;
            if (heldItem == unit.unitMeshManager.rightHeldItem)
            {
                if (unit.UnitEquipment.currentWeaponSet == WeaponSet.One)
                {
                    if (heldItem.itemData.Item is Weapon && heldItem.itemData.Item.Weapon.IsTwoHanded)
                        equipSlot = EquipSlot.LeftHeldItem1;
                    else
                        equipSlot = EquipSlot.RightHeldItem1;
                }
                else
                {
                    if (heldItem.itemData.Item is Weapon && heldItem.itemData.Item.Weapon.IsTwoHanded)
                        equipSlot = EquipSlot.LeftHeldItem2;
                    else
                        equipSlot = EquipSlot.RightHeldItem2;
                }
            }
            else
            {
                if (unit.UnitEquipment.currentWeaponSet == WeaponSet.One)
                    equipSlot = EquipSlot.LeftHeldItem1;
                else
                    equipSlot = EquipSlot.LeftHeldItem2;
            }

            unit.UnitEquipment.RemoveActions(unit.UnitEquipment.EquippedItemDatas[(int)equipSlot].Item as Equipment);
            unit.UnitEquipment.RemoveEquipment(unit.UnitEquipment.EquippedItemDatas[(int)equipSlot]);
        }

        static void SetupItemDrop(LooseItem looseItem, ItemData itemData, Unit unit, Vector3 dropDirection)
        {
            SetupLooseItem(looseItem, itemData);

            // Set the LooseItem's position to be slightly in front of the Unit dropping the item
            looseItem.transform.position = unit.transform.position + new Vector3(0, unit.ShoulderHeight, 0) + (dropDirection / 2);

            // Randomize the rotation and set active
            looseItem.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)));
            looseItem.gameObject.SetActive(true);
        }

        static void SetupContainerItemDrop(UnitEquipment unitEquipment, EquipSlot equipSlot, LooseItem looseItem, ItemData itemData, Unit unit, Vector3 dropDirection)
        {
            SetupItemDrop(looseItem, itemData, unit, dropDirection);

            if (equipSlot == EquipSlot.Back)
            {
                if (unitEquipment.MyUnit.BackpackInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(unitEquipment.MyUnit.BackpackInventoryManager).CloseContainerInventory();

                LooseContainerItem looseContainerItem = looseItem as LooseContainerItem;
                looseContainerItem.ContainerInventoryManager.TransferInventory(unitEquipment.MyUnit.BackpackInventoryManager);
            }
            else if (equipSlot == EquipSlot.Quiver)
            {
                if (unitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(unitEquipment.MyUnit.QuiverInventoryManager).CloseContainerInventory();

                LooseQuiverItem looseQuiverItem = looseItem as LooseQuiverItem;
                looseQuiverItem.ContainerInventoryManager.TransferInventory(unitEquipment.MyUnit.QuiverInventoryManager);
                looseQuiverItem.UpdateArrowMeshes();
            }
        }

        static void SetupHeldItemDrop(HeldItem heldItem, LooseItem looseItem)
        {
            SetupLooseItem(looseItem, heldItem.itemData);

            if (heldItem.itemData.Item is Shield && heldItem.transform.childCount > 1)
            {
                HeldShield heldShield = heldItem as HeldShield;
                Vector3 yOffset = new Vector3(0f, FindHeightDifference(looseItem.MeshCollider, heldShield.MeshCollider), 0f);

                for (int i = heldItem.transform.childCount - 1; i > 0; i--)
                {
                    if (heldItem.transform.GetChild(i).CompareTag("Loose Item") == false)
                        continue;

                    SetupLooseProjectile(heldItem.transform.GetChild(i), looseItem, yOffset);
                }
            }

            // Set the LooseItem's position to match the HeldItem before we add force
            looseItem.transform.position = heldItem.transform.position;
            looseItem.transform.rotation = heldItem.transform.rotation;
        }

        static float FindHeightDifference(MeshCollider meshCollider1, MeshCollider meshCollider2) => Mathf.Abs(meshCollider1.bounds.center.y - meshCollider2.bounds.center.y) * 2f;

        static void SetupLooseItem(LooseItem looseItem, ItemData itemData)
        {
            looseItem.SetItemData(itemData);
            looseItem.SetupMesh();
            looseItem.name = itemData.Item.name;
            itemData.SetInventorySlotCoordinate(null);
            looseItem.gameObject.SetActive(true);
        }

        static void SetupLooseProjectile(Transform looseProjectileTransform, LooseItem looseItem, Vector3 yOffset)
        {
            Vector3 projectilePosition = looseProjectileTransform.localPosition;
            Quaternion projectileRotation = looseProjectileTransform.localRotation;
            LooseItem looseProjectile = looseProjectileTransform.GetComponent<LooseItem>();
            looseProjectile.MeshCollider.enabled = false;
            looseProjectileTransform.SetParent(looseItem.transform);
            looseProjectileTransform.transform.localPosition = projectilePosition + yOffset;
            looseProjectileTransform.transform.localRotation = projectileRotation;
        }

        static Vector3 GetDropDirection(Unit unit)
        {
            Vector3 forceDirection = unit.transform.forward; // In front of Unit
            float raycastDistance = 1.2f;
            if (Physics.Raycast(unit.transform.position, forceDirection, out RaycastHit hit, raycastDistance, unit.unitActionHandler.AttackObstacleMask))
            {
                forceDirection = -unit.transform.forward; // Behind Unit
                if (Physics.Raycast(unit.transform.position, forceDirection, raycastDistance, unit.unitActionHandler.AttackObstacleMask))
                {
                    forceDirection = unit.transform.right; // Right of Unit
                    if (Physics.Raycast(unit.transform.position, forceDirection, raycastDistance, unit.unitActionHandler.AttackObstacleMask))
                    {
                        forceDirection = -unit.transform.right; // Left of Unit
                        if (Physics.Raycast(unit.transform.position, forceDirection, raycastDistance, unit.unitActionHandler.AttackObstacleMask))
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
}
