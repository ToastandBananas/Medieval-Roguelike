using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem;

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
        }

        public override void Interact(Unit unitPickingUpItem)
        {
            // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
            if (TryEquipOnPickup(unitPickingUpItem) || unitPickingUpItem.TryAddItemToInventories(itemData))
            {
                TryTakeStuckProjectiles(unitPickingUpItem);
                LooseItemPool.ReturnToPool(this);
            }
            else
                FumbleItem();
        }

        void TryTakeStuckProjectiles(Unit unitPickingUpItem)
        {
            if (itemData.Item is Shield && transform.childCount > 1)
            {
                for (int i = transform.childCount - 1; i > 0; i--)
                {
                    if (transform.GetChild(i).CompareTag("Loose Item") == false)
                        continue;

                    LooseItem looseProjectile = transform.GetChild(i).GetComponent<LooseItem>();
                    if (unitPickingUpItem.TryAddItemToInventories(looseProjectile.itemData))
                        LooseItemPool.ReturnToPool(looseProjectile);
                    else
                    {
                        looseProjectile.transform.SetParent(LooseItemPool.Instance.LooseItemParent);
                        looseProjectile.meshCollider.enabled = true;
                        looseProjectile.rigidBody.useGravity = true;
                        looseProjectile.rigidBody.isKinematic = false;
                        looseProjectile.FumbleItem();
                    }
                }
            }
        }

        protected bool TryEquipOnPickup(Unit unitPickingUpItem)
        {
            bool equipped = false;
            if (itemData.Item is Equipment)
            {
                if (itemData.Item.Equipment is Shield && unitPickingUpItem.CharacterEquipment.ShieldEquipped())
                    return false;

                EquipSlot targetEquipSlot = itemData.Item.Equipment.EquipSlot;
                if (unitPickingUpItem.CharacterEquipment.IsHeldItemEquipSlot(targetEquipSlot))
                {
                    if (unitPickingUpItem.CharacterEquipment.currentWeaponSet == WeaponSet.Two)
                    {
                        if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                            targetEquipSlot = EquipSlot.LeftHeldItem2;
                        else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                            targetEquipSlot = EquipSlot.RightHeldItem2;
                    }

                    if (unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(targetEquipSlot))
                    {
                        EquipSlot oppositeEquipSlot = unitPickingUpItem.CharacterEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot);

                        if ((itemData.Item is Weapon == false || itemData.Item.Weapon.IsTwoHanded == false) && unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(unitPickingUpItem.CharacterEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot)) == false)
                            equipped = unitPickingUpItem.CharacterEquipment.TryAddItemAt(oppositeEquipSlot, itemData);
                    }
                    else
                        equipped = unitPickingUpItem.CharacterEquipment.TryAddItemAt(targetEquipSlot, itemData);
                }
                else if (unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(targetEquipSlot) == false)
                    equipped = unitPickingUpItem.CharacterEquipment.TryEquipItem(itemData);
                else if (itemData.Item is Ammunition && itemData.IsEqual(unitPickingUpItem.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver]))
                    equipped = unitPickingUpItem.CharacterEquipment.TryAddToEquippedAmmunition(itemData);

                // Transfer inventory from loose container item if applicable
                /*if (equipped && this is LooseContainerItem)
                {
                    LooseContainerItem looseContainerItem = this as LooseContainerItem;
                    if (targetEquipSlot == EquipSlot.Back)
                        unitPickingUpItem.BackpackInventoryManager.TransferInventory(looseContainerItem.ContainerInventoryManager);
                    else if (itemData.Item is Quiver)
                        unitPickingUpItem.QuiverInventoryManager.TransferInventory(looseContainerItem.ContainerInventoryManager);
                }*/
            }

            return equipped;
        }

        public void FumbleItem()
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
            if (Physics.Raycast(meshCollider.bounds.center, Vector3.down, out RaycastHit hit, 100f, LevelGrid.Instance.GroundMask))
                gridPosition = LevelGrid.GetGridPosition(hit.point);
            else
                gridPosition = LevelGrid.GetGridPosition(meshCollider.bounds.center);
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
                        materials[i] = null;
                    else
                        materials[i] = itemData.Item.PickupMeshRendererMaterials[i];
                }

                meshRenderer.materials = materials;
            }
            else if (itemData.Item.Meshes[0] != null)
            {
                meshFilter.mesh = itemData.Item.Meshes[0];
                meshCollider.sharedMesh = itemData.Item.Meshes[0];

                Material[] materials = meshRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (i > itemData.Item.MeshRendererMaterials.Length - 1)
                        materials[i] = null;
                    else
                        materials[i] = itemData.Item.MeshRendererMaterials[i];
                }

                meshRenderer.materials = materials;
            }
            else
                Debug.LogWarning($"Mesh info has not been set on the ScriptableObject for: {itemData.Item.name}");
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

            if (itemData.Item.PickupMesh != null)
                meshFilter.mesh = itemData.Item.PickupMesh;
            else
                meshFilter.mesh = itemData.Item.Meshes[0];
        }

        public void HideMeshRenderer() => meshFilter.mesh = null;

        public bool CanSeeMeshRenderer() => meshFilter.mesh != null;

        public override bool CanInteractAtMyGridPosition() => true;
    }
}