using UnityEngine;
using InteractableObjects;

public enum ProjectileType
{
    Arrow = 0,
    Bolt = 10,
    BluntObject = 20,
    Explosive = 30,
};

[CreateAssetMenu(fileName = "New Ammunition", menuName = "Inventory/Ammunition")]
public class Ammunition : Equipment
{
    [Header("Sprite Change Thresholds")]
    [SerializeField] ItemChangeThreshold[] itemChangeThresholds;

    [Header("Quiver Sprites")]
    [SerializeField] Sprite[] quiverSprites;

    [Header("Loose Quiver Mesh")]
    [SerializeField] Mesh looseQuiverMesh;
    [SerializeField] Material looseQuiverMaterial;

    [Header("Collider Info")]
    [SerializeField] Vector3 capsuleColliderCenter;
    [SerializeField] float capsuleColliderRadius;
    [SerializeField] float capsuleColliderHeight;

    [Tooltip("0: X-Axis, 1: Y-Axis, 2: Z-axis")]
    [SerializeField][Range(0, 2)] int capsuleColliderDirection;

    [Header("Projectile Info")]
    [SerializeField] ProjectileType projectileType;
    [SerializeField] int speed = 15;

    [Tooltip("Amount the arc height will be multiplied by. (0 = no arc)")]
    [SerializeField] float arcMultiplier = 1f;

    public override bool Use(Unit unit, ItemData itemData, int amountToUse = 1)
    {
        if (unit.CharacterEquipment.EquipSlotHasItem(EquipSlot.Quiver) && unit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver)
        {
            Quiver quiver = unit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item as Quiver;
            if (quiver.AllowedProjectileType == projectileType)
            {
                Inventory itemDatasInventory = itemData.MyInventory();

                bool itemAdded = unit.QuiverInventoryManager.ParentInventory.TryAddItem(itemData);
                if (unit.CharacterEquipment.slotVisualsCreated)
                    unit.CharacterEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                if (itemDatasInventory != null && itemDatasInventory is ContainerInventory && itemDatasInventory.ContainerInventory.LooseItem != null && itemDatasInventory.ContainerInventory.LooseItem is LooseQuiverItem)
                    itemDatasInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();

                if (itemAdded)
                {
                    if (ContextMenu.targetSlot != null && ContextMenu.targetSlot.InventoryItem.myCharacterEquipment != null)
                        ContextMenu.targetSlot.InventoryItem.myCharacterEquipment.RemoveEquipment(itemData);
                }
                else
                {
                    if (ContextMenu.targetSlot != null && ContextMenu.targetSlot.InventoryItem.myCharacterEquipment != null && ContextMenu.targetSlot.InventoryItem.myCharacterEquipment.slotVisualsCreated)
                        ContextMenu.targetSlot.InventoryItem.UpdateStackSizeVisuals();
                }

                return itemAdded;
            }
            else
                return unit.CharacterEquipment.TryEquipItem(itemData);
        }
        else
            return unit.CharacterEquipment.TryEquipItem(itemData);
    }

    public override Sprite InventorySprite(ItemData itemData = null)
    {
        if (itemData == null)
            return base.InventorySprite();

        ItemChangeThreshold itemChangeThreshold = ItemChangeThreshold.GetCurrentItemChangeThreshold(itemData, itemChangeThresholds);
        if (itemChangeThreshold != null && itemChangeThreshold.NewSprite != null)
            return itemChangeThreshold.NewSprite;
        return base.InventorySprite();
    }

    public ItemChangeThreshold[] ItemChangeThresholds => itemChangeThresholds;
    public Sprite[] QuiverSprites => quiverSprites;

    public Mesh LooseQuiverMesh => looseQuiverMesh;
    public Material LooseQuiverMaterial => looseQuiverMaterial;

    public Vector3 CapsuleColliderCenter => capsuleColliderCenter;
    public float CapsuleColliderRadius => capsuleColliderRadius;
    public float CapsuleColliderHeight => capsuleColliderHeight;
    public int CapsuleColliderDirection => capsuleColliderDirection;

    public ProjectileType ProjectileType => projectileType;
    public int Speed => speed;
    public float ArcMultiplier => arcMultiplier;
}
