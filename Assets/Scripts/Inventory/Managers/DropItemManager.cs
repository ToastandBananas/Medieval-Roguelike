using UnityEngine;
using InteractableObjects;
using ActionSystem;
using UnitSystem;
using ContextMenu = GeneralUI.ContextMenu;
using GeneralUI;

namespace InventorySystem
{
    public class DropItemManager : MonoBehaviour
    {
        public static void DropItem(Inventory inventory, Unit unit, ItemData itemDataToDrop)
        {
            if (itemDataToDrop == null || itemDataToDrop.Item == null)
            {
                Debug.LogWarning("Item you're trying to drop from inventory is null...");
                if (inventory != null && itemDataToDrop != null)
                    inventory.RemoveItem(itemDataToDrop, true);
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

            unit.vision.AddVisibleLooseItem(looseItem);

            if (unit != UnitManager.player)
            {
                if (UnitManager.player.vision.IsVisible(unit) == false)
                    looseItem.HideMeshRenderer();
                else
                    UnitManager.player.vision.AddVisibleLooseItem(looseItem);
            }

            if (inventory != null)
                inventory.RemoveItem(itemDataToDrop, true);

            if (itemDataToDrop == InventoryUI.DraggedItem.itemData)
                InventoryUI.DisableDraggedItem();

            if (inventory.MyUnit != null)
                inventory.MyUnit.stats.UpdateCarryWeight();

            // In this case, the Player is dropping an item from a dead Unit's inventory
            if (unit.health.IsDead())
            {
                UnitManager.player.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, null);
                UnitManager.player.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, null, InventoryActionType.Drop);
            }
            else
            {
                unit.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, null);
                unit.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, null, InventoryActionType.Drop);
            }

            TooltipManager.UpdateLooseItemTooltips();
        }

        public static void DropItem(UnitEquipment unitEquipment, EquipSlot equipSlot)
        {
            if (unitEquipment.EquipSlotIsFull(equipSlot) == false)
                return;

            LooseItem looseItem;
            if (unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Quiver)
                looseItem = LooseItemPool.Instance.GetLooseQuiverItemFromPool();
            else if (unitEquipment.EquippedItemDatas[(int)equipSlot].Item is WearableContainer)
                looseItem = LooseItemPool.Instance.GetLooseContainerItemFromPool();
            else
                looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

            ContainerInventoryManager itemsContainerInventoryManager = null;
            Vector3 dropDirection = GetDropDirection(unitEquipment.MyUnit);

            if (unitEquipment.MyUnit.health.IsDead() == false && (unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Weapon || unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Shield))
                SetupHeldItemDrop(unitEquipment.MyUnit.unitMeshManager.GetHeldItemFromItemData(unitEquipment.EquippedItemDatas[(int)equipSlot]), looseItem);
            else if (equipSlot == EquipSlot.Helm)
                SetupHelmItemDrop(looseItem, unitEquipment.EquippedItemDatas[(int)equipSlot], unitEquipment.MyUnit);
            else if ((equipSlot == EquipSlot.Back && unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Backpack) || (equipSlot == EquipSlot.Quiver && unitEquipment.EquippedItemDatas[(int)equipSlot].Item is Quiver) || (equipSlot == EquipSlot.Belt))
            {
                SetupContainerItemDrop(unitEquipment, equipSlot, looseItem, unitEquipment.EquippedItemDatas[(int)equipSlot], unitEquipment.MyUnit, dropDirection);
                itemsContainerInventoryManager = looseItem.LooseContainerItem.ContainerInventoryManager;
            }
            else
                SetupItemDrop(looseItem, unitEquipment.EquippedItemDatas[(int)equipSlot], unitEquipment.MyUnit, dropDirection);

            // We queue each action twice to account for unequipping the item before dropping it
            if (unitEquipment.MyUnit.health.IsDead()) // In this case, the player is dropping an item from a dead Unit's equipment
            {
                if (ContextMenu.targetSlot == null || InventoryUI.isDraggingItem)
                    UnitManager.player.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);

                UnitManager.player.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Drop);
            }
            else
            {
                // The context menu already accounts for unequipping AP cost by calling an UnequipAction
                if (ContextMenu.targetSlot == null || InventoryUI.isDraggingItem)
                    unitEquipment.MyUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Unequip);

                unitEquipment.MyUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(looseItem.ItemData, looseItem.ItemData.CurrentStackSize, itemsContainerInventoryManager, InventoryActionType.Drop);
            }

            unitEquipment.RemoveActions(unitEquipment.EquippedItemDatas[(int)equipSlot].Item as Equipment);
            unitEquipment.RemoveEquipmentMesh(equipSlot);

            float randomForceMagnitude = Random.Range(looseItem.RigidBody.mass * 0.8f, looseItem.RigidBody.mass * 3f);

            // Apply force to the dropped item
            looseItem.RigidBody.AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

            unitEquipment.MyUnit.vision.AddVisibleLooseItem(looseItem);

            if (unitEquipment.MyUnit != UnitManager.player)
            {
                if (UnitManager.player.vision.IsVisible(unitEquipment.MyUnit) == false)
                    looseItem.HideMeshRenderer();
                else
                    UnitManager.player.vision.AddVisibleLooseItem(looseItem);
            }

            if (unitEquipment.EquippedItemDatas[(int)equipSlot] == InventoryUI.DraggedItem.itemData)
            {
                if (InventoryUI.parentSlotDraggedFrom != null)
                    InventoryUI.parentSlotDraggedFrom.ClearItem();

                InventoryUI.DisableDraggedItem();
            }
            else if (unitEquipment.slotVisualsCreated)
                unitEquipment.GetEquipmentSlot(equipSlot).ClearItem();

            unitEquipment.EquippedItemDatas[(int)equipSlot] = null;

            if (unitEquipment.MyUnit != null)
                unitEquipment.MyUnit.stats.UpdateCarryWeight();

            if (UnitEquipment.IsHeldItemEquipSlot(equipSlot))
                unitEquipment.MyUnit.opportunityAttackTrigger.UpdateColliderRadius();

            ActionSystemUI.UpdateActionVisuals();
            TooltipManager.UpdateLooseItemTooltips();
        }

        public static void DropHelmOnDeath(ItemData itemData, Unit unit, Transform attackerTransform, bool diedForward)
        {
            LooseItem looseHelm = LooseItemPool.Instance.GetLooseItemFromPool();

            float randomForceMagnitude = Random.Range(looseHelm.RigidBody.mass, looseHelm.RigidBody.mass * 6f);
            float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees

            // Get the attacker's position and the character's position
            Vector3 attackerPosition = attackerTransform.position;
            Vector3 unitPosition = unit.transform.position;

            // Calculate the force direction (depending on whether they fall forward or backward)
            Vector3 forceDirection;
            if (diedForward)
                forceDirection = (attackerPosition - unitPosition).normalized;
            else
                forceDirection = (unitPosition - attackerPosition).normalized;

            // Add some randomness to the force direction
            Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
            forceDirection = randomRotation * forceDirection;

            SetupHelmItemDrop(looseHelm, itemData, unit);

            // Get the Rigidbody component(s) and apply force
            looseHelm.RigidBody.AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);

            if (unit != UnitManager.player)
            {
                if (UnitManager.player.vision.IsVisible(unit) == false)
                    looseHelm.HideMeshRenderer();
                else
                    UnitManager.player.vision.AddVisibleLooseItem(looseHelm);
            }

            unit.UnitEquipment.RemoveActions(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Helm].Item as Equipment);
            unit.UnitEquipment.RemoveEquipment(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Helm]);

            if (unit.IsNPC)
                TooltipManager.UpdateLooseItemTooltips();
        }

        public static void DropHeldItemOnDeath(HeldItem heldItem, Unit unit, Transform attackerTransform, bool diedForward)
        {
            LooseItem looseWeapon = LooseItemPool.Instance.GetLooseItemFromPool();

            float randomForceMagnitude = Random.Range(looseWeapon.RigidBody.mass, looseWeapon.RigidBody.mass * 6f);
            float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees

            // Get the attacker's position and the character's position
            Vector3 attackerPosition = attackerTransform.position;
            Vector3 unitPosition = unit.transform.position;

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

                    if (unit != UnitManager.player)
                    {
                        if (UnitManager.player.vision.IsVisible(unit) == false)
                            looseProjectile.HideMeshRenderer();
                        else
                            UnitManager.player.vision.AddVisibleLooseItem(looseProjectile);
                    }
                }
            }

            // Get the Rigidbody component(s) and apply force
            looseWeapon.RigidBody.AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);

            if (unit != UnitManager.player)
            {
                if (UnitManager.player.vision.IsVisible(unit) == false)
                    looseWeapon.HideMeshRenderer();
                else
                    UnitManager.player.vision.AddVisibleLooseItem(looseWeapon);
            }

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

            unit.opportunityAttackTrigger.UpdateColliderRadius();

            if (unit.IsNPC)
                TooltipManager.UpdateLooseItemTooltips();
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

                looseItem.LooseContainerItem.ContainerInventoryManager.SwapInventories(unitEquipment.MyUnit.BackpackInventoryManager);
            }
            else if (equipSlot == EquipSlot.Belt)
            {
                if (unitEquipment.MyUnit.BeltInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(unitEquipment.MyUnit.BeltInventoryManager).CloseContainerInventory();

                looseItem.LooseContainerItem.ContainerInventoryManager.SwapInventories(unitEquipment.MyUnit.BeltInventoryManager);
            }
            else if (equipSlot == EquipSlot.Quiver)
            {
                if (unitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(unitEquipment.MyUnit.QuiverInventoryManager).CloseContainerInventory();

                LooseQuiverItem looseQuiverItem = looseItem as LooseQuiverItem;
                looseQuiverItem.ContainerInventoryManager.SwapInventories(unitEquipment.MyUnit.QuiverInventoryManager);
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

                    SetupStuckLooseProjectile(heldItem.transform.GetChild(i), looseItem, yOffset);
                }
            }

            // Set the LooseItem's position to match the HeldItem before we add force
            looseItem.transform.position = heldItem.transform.position;
            looseItem.transform.rotation = heldItem.transform.rotation;
        }

        static void SetupHelmItemDrop(LooseItem looseItem, ItemData itemData, Unit unit)
        {
            SetupLooseItem(looseItem, itemData);

            // Set the LooseItem's position to match the worn Helm before we add force
            looseItem.transform.position = unit.unitMeshManager.HelmMeshRenderer.transform.position + new Vector3(0f, itemData.Item.PickupMesh.bounds.size.y, 0f);
            looseItem.transform.rotation = unit.unitMeshManager.HelmMeshRenderer.transform.rotation;
        }

        static float FindHeightDifference(MeshCollider meshCollider1, MeshCollider meshCollider2) => Mathf.Abs(meshCollider1.bounds.center.y - meshCollider2.bounds.center.y) * 2f;

        static void SetupLooseItem(LooseItem looseItem, ItemData itemData)
        {
            looseItem.SetItemData(itemData);
            looseItem.SetupMesh();
            looseItem.name = itemData.Item.Name;
            itemData.SetInventorySlotCoordinate(null);
            looseItem.gameObject.SetActive(true);
        }

        static void SetupStuckLooseProjectile(Transform looseProjectileTransform, LooseItem looseItem, Vector3 yOffset)
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
