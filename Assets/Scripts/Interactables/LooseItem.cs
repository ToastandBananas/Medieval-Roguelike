using UnityEngine;

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

        itemData.RandomizeData();
    }

    public override void Interact(Unit unitPickingUpItem)
    {
        // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
        if (TryEquipItemOnPickup(unitPickingUpItem) || unitPickingUpItem.TryAddItemToInventories(itemData))
        {
            TryTakeStuckProjectiles(unitPickingUpItem);
            LooseItemPool.ReturnToPool(this);
        }
    }

    void TryTakeStuckProjectiles(Unit unitPickingUpItem)
    {
        if (itemData.Item.IsShield() && transform.childCount > 1)
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
                }
            }
        }
    }

    protected bool TryEquipItemOnPickup(Unit unitPickingUpItem)
    {
        bool equipped = false;
        if (itemData.Item.IsEquipment())
        {
            EquipSlot targetEquipSlot = itemData.Item.Equipment().EquipSlot;
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

                    if ((itemData.Item.IsWeapon() == false || itemData.Item.Weapon().isTwoHanded == false) && unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(unitPickingUpItem.CharacterEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot)) == false)
                        equipped = unitPickingUpItem.CharacterEquipment.TryAddItemAt(oppositeEquipSlot, itemData);
                }
                else
                    equipped = unitPickingUpItem.CharacterEquipment.TryAddItemAt(targetEquipSlot, itemData);
            }
            else if (unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(targetEquipSlot) == false)
                equipped = unitPickingUpItem.CharacterEquipment.TryEquipItem(itemData);
            else if (itemData.Item.IsAmmunition() && itemData.IsEqual(unitPickingUpItem.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver]))
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
        if (itemData.Item.pickupMesh != null)
        {
            meshFilter.mesh = itemData.Item.pickupMesh;
            meshRenderer.material = itemData.Item.pickupMeshRendererMaterial;
            meshCollider.sharedMesh = itemData.Item.pickupMesh;
        }
        else if (itemData.Item.meshes[0] != null)
        {
            meshFilter.mesh = itemData.Item.meshes[0];
            meshRenderer.material = itemData.Item.meshRendererMaterials[0];
            meshCollider.sharedMesh = itemData.Item.meshes[0];
        }
        else
            Debug.LogWarning($"Mesh info has not been set on the ScriptableObject for: {itemData.Item.name}");
    }

    public ItemData ItemData => itemData;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public Rigidbody RigidBody => rigidBody;

    public MeshCollider MeshCollider => meshCollider;

    public void ShowMeshRenderer()
    {
        if (itemData == null || itemData.Item == null)
            return;

        if (itemData.Item.pickupMesh != null)
            meshFilter.mesh = itemData.Item.pickupMesh;
        else
            meshFilter.mesh = itemData.Item.meshes[0];
    }

    public void HideMeshRenderer() => meshFilter.mesh = null;

    public bool CanSeeMeshRenderer() => meshFilter.mesh != null;

    public override bool CanInteractAtMyGridPosition() => true;
}
