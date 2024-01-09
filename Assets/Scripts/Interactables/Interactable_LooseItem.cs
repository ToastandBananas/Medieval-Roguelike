using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem;
using Pathfinding.Util;
using System.Collections.Generic;
using UnitSystem.ActionSystem.Actions;

namespace InteractableObjects
{
    public class Interactable_LooseItem : Interactable
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

        void Start()
        {
            if (itemData.Item == null)
                Debug.LogWarning($"{name}'s Item is null...");
        }

        public override void Interact(Unit unitPickingUpItem)
        {
            if (itemData == null || itemData.Item == null)
                return;

            if (unitPickingUpItem.UnitActionHandler.TurnAction.IsFacingTarget(gridPosition) == false)
                unitPickingUpItem.UnitActionHandler.TurnAction.RotateTowardsPosition(gridPosition.WorldPosition, false, unitPickingUpItem.UnitActionHandler.TurnAction.DefaultRotateSpeed * 2f);

            List<Interactable_LooseItem> looseProjectiles = ListPool<Interactable_LooseItem>.Claim();
            if (itemData.Item is Item_Shield && transform.childCount > 1)
            {
                for (int i = transform.childCount - 1; i > 0; i--)
                {
                    if (transform.GetChild(i).CompareTag("Loose Item") == false)
                        continue;

                    looseProjectiles.Add(transform.GetChild(i).GetComponent<Interactable_LooseItem>());
                }
            }

            // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
            if (TryEquipOnPickup(unitPickingUpItem) || unitPickingUpItem.UnitInventoryManager.TryAddItemToInventories(itemData))
            {
                TryTakeStuckProjectiles(unitPickingUpItem, looseProjectiles);
                unitPickingUpItem.Vision.RemoveVisibleLooseItem(this);
                Pool_LooseItems.ReturnToPool(this);
            }
            else
                JiggleItem();

            ListPool<Interactable_LooseItem>.Release(looseProjectiles);
        }

        void TryTakeStuckProjectiles(Unit unitPickingUpItem, List<Interactable_LooseItem> looseProjectiles)
        {
            for (int i = looseProjectiles.Count - 1; i > 0; i--)
            {
                if (unitPickingUpItem.UnitInventoryManager.TryAddItemToInventories(looseProjectiles[i].itemData))
                    Pool_LooseItems.ReturnToPool(looseProjectiles[i]);
                else
                {
                    looseProjectiles[i].transform.SetParent(Pool_LooseItems.Instance.LooseItemParent);
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
            if (itemData.IsBroken || unitPickingUpItem.UnitEquipment == null)
                return false;

            if (itemData.Item is Item_Equipment)
            {
                // Don't equip a second shield
                if (itemData.Item is Item_Shield && unitPickingUpItem.UnitEquipment.ShieldEquipped)
                    return false;

                if (itemData.Item is Item_Weapon && itemData.Item.Weapon.IsTwoHanded && (unitPickingUpItem.UnitEquipment.MeleeWeaponEquipped || unitPickingUpItem.UnitEquipment.ShieldEquipped))
                    return false;

                // Don't equip a second weapon or a shield if the character is in Versatile Stance
                if (unitPickingUpItem.UnitEquipment.HumanoidEquipment.InVersatileStance)
                    return false;

                EquipSlot targetEquipSlot = itemData.Item.Equipment.EquipSlot;
                if (itemData.Item is Item_HeldEquipment)
                {
                    if (unitPickingUpItem.UnitEquipment.HumanoidEquipment.CurrentWeaponSet == WeaponSet.Two)
                    {
                        if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                            targetEquipSlot = EquipSlot.LeftHeldItem2;
                        else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                            targetEquipSlot = EquipSlot.RightHeldItem2;
                    }

                    if (!unitPickingUpItem.UnitEquipment.CapableOfEquippingHeldItem(itemData, targetEquipSlot, true))
                        return false;

                    if (unitPickingUpItem.UnitEquipment.EquipSlotIsFull(targetEquipSlot) || !unitPickingUpItem.UnitEquipment.CapableOfEquippingHeldItem(itemData, targetEquipSlot, false))
                    {
                        EquipSlot oppositeEquipSlot = unitPickingUpItem.UnitEquipment.HumanoidEquipment.GetOppositeHeldItemEquipSlot(targetEquipSlot);
                        if ((itemData.Item is Item_Weapon == false || !itemData.Item.Weapon.IsTwoHanded) && !unitPickingUpItem.UnitEquipment.EquipSlotIsFull(oppositeEquipSlot) && unitPickingUpItem.UnitEquipment.CapableOfEquippingHeldItem(itemData, oppositeEquipSlot, false))
                        {
                            equipped = true;
                            unitPickingUpItem.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemData, itemData.CurrentStackSize, null);
                            unitPickingUpItem.UnitActionHandler.GetAction<Action_Equip>().TakeActionImmediately(itemData, oppositeEquipSlot, null);
                        }
                    }
                    else if (unitPickingUpItem.UnitEquipment.CapableOfEquippingHeldItem(itemData, targetEquipSlot, false))
                    {
                        equipped = true;
                        unitPickingUpItem.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemData, itemData.CurrentStackSize, null);
                        unitPickingUpItem.UnitActionHandler.GetAction<Action_Equip>().TakeActionImmediately(itemData, targetEquipSlot, null);
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
                else if (!unitPickingUpItem.UnitEquipment.EquipSlotIsFull(targetEquipSlot) || (itemData.Item is Item_Ammunition && itemData.IsEqual(unitPickingUpItem.UnitEquipment.EquippedItemData(EquipSlot.Quiver))))
                {
                    equipped = unitPickingUpItem.UnitEquipment.CanEquipItem(itemData);
                    if (equipped)
                    {
                        unitPickingUpItem.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(itemData, itemData.CurrentStackSize, this is Interactable_LooseContainerItem ? LooseContainerItem.ContainerInventoryManager : null, InventoryActionType.Equip);
                        unitPickingUpItem.UnitActionHandler.GetAction<Action_Equip>().TakeActionImmediately(itemData, targetEquipSlot, this is Interactable_LooseContainerItem ? LooseContainerItem.ContainerInventoryManager : null);
                    }
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

        public Interactable_LooseContainerItem LooseContainerItem => this as Interactable_LooseContainerItem;
        public Interactable_LooseQuiverItem LooseQuiverItem => this as Interactable_LooseQuiverItem;

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