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
        gridPosition = LevelGrid.GetGridPosition(transform.position);

        itemData.RandomizeData();
    }

    public void FixedUpdate()
    {
        if (rigidBody.velocity.magnitude >= 0.01f)
            UpdateGridPosition();
    }

    public override void Interact(Unit unitPickingUpItem)
    {
        // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
        if (TryEquipItemOnPickup(unitPickingUpItem) || unitPickingUpItem.TryAddItemToInventories(itemData))
            LooseItemPool.Instance.ReturnToPool(this);
    }

    protected bool TryEquipItemOnPickup(Unit unitPickingUpItem)
    {
        bool equipped = false;
        if (itemData.Item.IsEquipment())
        {
            EquipSlot targetEquipSlot = itemData.Item.Equipment().EquipSlot;
            if (unitPickingUpItem.CharacterEquipment.currentWeaponSet == WeaponSet.Two)
            {
                if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                    targetEquipSlot = EquipSlot.LeftHeldItem2;
                else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                    targetEquipSlot = EquipSlot.RightHeldItem2;
            }

            if (unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(targetEquipSlot))
            {
                if ((itemData.Item.IsWeapon() == false || itemData.Item.Weapon().isTwoHanded == false) && unitPickingUpItem.CharacterEquipment.EquipSlotIsFull(unitPickingUpItem.CharacterEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot)) == false)
                    equipped = unitPickingUpItem.CharacterEquipment.TryEquipItem(itemData);
            }
            else
                equipped = unitPickingUpItem.CharacterEquipment.TryEquipItem(itemData);

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
        gridPosition = LevelGrid.GetGridPosition(meshCollider.bounds.center);
    }

    public void SetupMesh(Mesh mesh, Material material)
    {
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        meshCollider.sharedMesh = mesh;
    }

    public ItemData ItemData => itemData;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public Rigidbody RigidBody => rigidBody;

    public MeshCollider MeshCollider() => meshCollider;

    public void ShowMeshRenderer() => meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

    public void HideMeshRenderer() => meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

    public bool CanSeeMeshRenderer() => meshRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;

    public override bool CanInteractAtMyGridPosition() => true;
}
