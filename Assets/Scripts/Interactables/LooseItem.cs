using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem;
using UnitSystem.ActionSystem;
using Pathfinding.Util;
using System.Collections.Generic;

namespace InteractableObjects
{
    public class LooseItem : Interactable
    {
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] MeshCollider meshCollider;
        [SerializeField] Rigidbody rigidBody;

        [SerializeField] protected ItemData itemData;

        public override void Awake()
        {
            UpdateGridPosition();

            Item startingItem = itemData.Item;
            itemData.RandomizeData();
            if (itemData.Item != startingItem) // If an Item Change Threshold was reached and the Item changed, we will need to update the mesh
                SetupMesh();

            HideMeshRenderer();
        }

        public override void Interact(Unit unitPickingUpItem)
        {
            if (unitPickingUpItem.unitActionHandler.turnAction.IsFacingTarget(gridPosition) == false)
                unitPickingUpItem.unitActionHandler.turnAction.RotateTowardsPosition(gridPosition.WorldPosition, false, unitPickingUpItem.unitActionHandler.turnAction.DefaultRotateSpeed * 2f);

            List<LooseItem> looseProjectiles = ListPool<LooseItem>.Claim();
            if (itemData.Item is Shield && transform.childCount > 1)
            {
                for (int i = transform.childCount - 1; i > 0; i--)
                {
                    if (transform.GetChild(i).CompareTag("Loose Item") == false)
                        continue;

                    looseProjectiles.Add(transform.GetChild(i).GetComponent<LooseItem>());
                }
            }

            // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
            if (TryEquipOnPickup(unitPickingUpItem) || unitPickingUpItem.UnitInventoryManager.TryAddItemToInventories(itemData))
            {
                TryTakeStuckProjectiles(unitPickingUpItem, looseProjectiles);
                unitPickingUpItem.vision.RemoveVisibleLooseItem(this);
                LooseItemPool.ReturnToPool(this);
            }
            else
                JiggleItem();

            ListPool<LooseItem>.Release(looseProjectiles);
        }

        void TryTakeStuckProjectiles(Unit unitPickingUpItem, List<LooseItem> looseProjectiles)
        {
            for (int i = looseProjectiles.Count - 1; i > 0; i--)
            {
                if (unitPickingUpItem.UnitInventoryManager.TryAddItemToInventories(looseProjectiles[i].itemData))
                    LooseItemPool.ReturnToPool(looseProjectiles[i]);
                else
                {
                    looseProjectiles[i].transform.SetParent(LooseItemPool.Instance.LooseItemParent);
                    looseProjectiles[i].meshCollider.enabled = true;
                    looseProjectiles[i].rigidBody.useGravity = true;
                    looseProjectiles[i].rigidBody.isKinematic = false;
                    looseProjectiles[i].JiggleItem();
                }
            }
        }

        protected bool TryEquipOnPickup(Unit unitPickingUpItem)
        {
            bool equipped = false;
            if (itemData.Item is Equipment)
            {
                // Don't equip a second shield
                if (itemData.Item is Shield && unitPickingUpItem.UnitEquipment.ShieldEquipped)
                    return false;

                if (itemData.Item is Weapon && itemData.Item.Weapon.IsTwoHanded && unitPickingUpItem.UnitEquipment.MeleeWeaponEquipped)
                    return false;

                // Don't equip a second weapon or a shield if the character is in Versatile Stance
                if (unitPickingUpItem.UnitEquipment.InVersatileStance)
                    return false;

                EquipSlot targetEquipSlot = itemData.Item.Equipment.EquipSlot;
                if (UnitEquipment.IsHeldItemEquipSlot(targetEquipSlot))
                {
                    if (unitPickingUpItem.UnitEquipment.currentWeaponSet == WeaponSet.Two)
                    {
                        if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                            targetEquipSlot = EquipSlot.LeftHeldItem2;
                        else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                            targetEquipSlot = EquipSlot.RightHeldItem2;
                    }

                    if (unitPickingUpItem.UnitEquipment.EquipSlotIsFull(targetEquipSlot))
                    {
                        EquipSlot oppositeEquipSlot = unitPickingUpItem.UnitEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot);
                        if ((itemData.Item is Weapon == false || itemData.Item.Weapon.IsTwoHanded == false) && unitPickingUpItem.UnitEquipment.EquipSlotIsFull(oppositeEquipSlot) == false)
                        {
                            equipped = true;
                            unitPickingUpItem.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemData, itemData.CurrentStackSize, null);
                            unitPickingUpItem.unitActionHandler.GetAction<EquipAction>().TakeActionImmediately(itemData, oppositeEquipSlot, null);
                        }
                    }
                    else
                    {
                        equipped = true;
                        unitPickingUpItem.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemData, itemData.CurrentStackSize, null);
                        unitPickingUpItem.unitActionHandler.GetAction<EquipAction>().TakeActionImmediately(itemData, targetEquipSlot, null);
                    }
                }
                /*else if (UnitEquipment.IsRingEquipSlot(targetEquipSlot))
                {
                    if (unitPickingUpItem.UnitEquipment.EquipSlotIsFull(targetEquipSlot))
                    {
                        EquipSlot oppositeEquipSlot = unitPickingUpItem.UnitEquipment.GetOppositeRingEquipSlot(targetEquipSlot);
                        if (unitPickingUpItem.UnitEquipment.EquipSlotIsFull(oppositeEquipSlot) == false)
                        {
                            equipped = true;
                            unitPickingUpItem.unitActionHandler.GetAction<EquipAction>().QueueAction(itemData, oppositeEquipSlot, null);
                        }
                    }
                    else
                    {
                        equipped = true;
                        unitPickingUpItem.unitActionHandler.GetAction<EquipAction>().QueueAction(itemData, targetEquipSlot, null);
                    }
                }*/
                else if (unitPickingUpItem.UnitEquipment.EquipSlotIsFull(targetEquipSlot) == false || (itemData.Item is Ammunition && itemData.IsEqual(unitPickingUpItem.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Quiver])))
                {
                    equipped = unitPickingUpItem.UnitEquipment.CanEquipItem(itemData);
                    unitPickingUpItem.unitActionHandler.GetAction<InventoryAction>().QueueAction(itemData, itemData.CurrentStackSize, this is LooseContainerItem ? LooseContainerItem.ContainerInventoryManager : null, InventoryActionType.Equip);
                    unitPickingUpItem.unitActionHandler.GetAction<EquipAction>().TakeActionImmediately(itemData, targetEquipSlot, this is LooseContainerItem ? LooseContainerItem.ContainerInventoryManager : null);
                }
            }
            
            return equipped;
        }

        public void JiggleItem()
        {
            float fumbleForceMin = rigidBody.mass * 0.8f;   // Minimum force magnitude
            float fumbleForceMax = rigidBody.mass * 2.25f;  // Maximum force magnitude
            float fumbleAngleMax = 65f;                     // Maximum angle for randomization in degrees
            float fumbleAngleAwayFromPlayer = 30f;          // Maximum angle away from the player in degrees
            float fumbleTorqueMin = rigidBody.mass * 0.25f; // Minimum torque magnitude
            float fumbleTorqueMax = rigidBody.mass * 1.25f; // Maximum torque magnitude

            // Generate a random force magnitude
            float fumbleForceMagnitude = Random.Range(fumbleForceMin, fumbleForceMax);

            // Generate a random rotation angle
            float randomAngle = Random.Range(0f, fumbleAngleMax);

            // Generate a random angle away from the player
            float randomAngleAwayFromPlayer = Random.Range(-fumbleAngleAwayFromPlayer, fumbleAngleAwayFromPlayer);

            // Calculate a random direction within the cone defined by the angle
            Vector3 randomDirection = Quaternion.Euler(randomAngle, Random.Range(0f, 360f), randomAngleAwayFromPlayer) * Vector3.up;

            // Apply the random force in the random direction
            Vector3 randomForce = randomDirection.normalized * fumbleForceMagnitude;
            rigidBody.AddForce(randomForce, ForceMode.Impulse);

            // Generate a random torque magnitude
            float fumbleTorqueMagnitude = Random.Range(fumbleTorqueMin, fumbleTorqueMax);

            // Generate a random torque axis
            Vector3 randomTorqueAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

            // Apply the random torque in the random axis
            Vector3 randomTorque = randomTorqueAxis.normalized * fumbleTorqueMagnitude;
            rigidBody.AddTorque(randomTorque, ForceMode.Impulse);
        }

        public override void UpdateGridPosition()
        {
            if (Physics.Raycast(transform.TransformPoint(meshCollider.sharedMesh.bounds.center), Vector3.down, out RaycastHit hit, 50f, LevelGrid.GroundMask))
                gridPosition.Set(hit.point);
            else
                gridPosition.Set(transform.TransformPoint(meshCollider.sharedMesh.bounds.center));
        }

        public override GridPosition GridPosition()
        {
            UpdateGridPosition();
            return gridPosition;
        }

        public void SetupMesh()
        {
            if (itemData.Item.PickupMesh != null)
            {
                meshFilter.mesh = itemData.Item.PickupMesh;
                meshCollider.sharedMesh = itemData.Item.PickupMesh;

                Material[] materials = meshRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (i > itemData.Item.PickupMeshRendererMaterials.Length - 1)
                        materials[i] = itemData.Item.PickupMeshRendererMaterials[itemData.Item.PickupMeshRendererMaterials.Length - 1];
                    else
                        materials[i] = itemData.Item.PickupMeshRendererMaterials[i];
                }

                meshRenderer.materials = materials;
            }
            else
                Debug.LogWarning($"Pickup Mesh info has not been set on the ScriptableObject for: {itemData.Item.name}");
        }

        public LooseContainerItem LooseContainerItem => this as LooseContainerItem;
        public LooseQuiverItem LooseQuiverItem => this as LooseQuiverItem;

        public ItemData ItemData => itemData;

        public void SetItemData(ItemData newItemData) => itemData = newItemData;

        public Rigidbody RigidBody => rigidBody;
        public MeshCollider MeshCollider => meshCollider;
        public MeshFilter MeshFilter => meshFilter;

        public void ShowMeshRenderer()
        {
            if (itemData == null || itemData.Item == null)
                return;

            // Debug.Log("Show: " + name);
            meshFilter.mesh = itemData.Item.PickupMesh;
        }

        public void HideMeshRenderer()
        {
            // Debug.Log("Hide: " + name);
            meshFilter.mesh = null;
        }

        public bool CanSeeMeshRenderer => meshFilter.mesh.vertexCount > 0;

        public override bool CanInteractAtMyGridPosition() => true;
    }
}