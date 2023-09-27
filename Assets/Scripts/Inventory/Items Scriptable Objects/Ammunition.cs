using UnityEngine;

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

    [Header("Transform")]
    [SerializeField] Vector3 ammunitionPositionOffset;
    [SerializeField] Vector3 ammunitionRotation;

    public override void Use(Unit unit, ItemData itemData, int amountToUse = 1)
    {
        if (unit.CharacterEquipment.EquipSlotHasItem(EquipSlot.Quiver) && unit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver)
        {
            Quiver quiver = unit.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item as Quiver;
            if (quiver.AllowedProjectileType == projectileType)
                unit.QuiverInventoryManager.ParentInventory.TryAddItem(itemData);
            else
                unit.CharacterEquipment.TryEquipItem(itemData);
        }
        else
            unit.CharacterEquipment.TryEquipItem(itemData);
    }

    public Mesh AmmunitionMesh => meshes[0];
    public Material AmmunitionMaterial => meshRendererMaterials[0];

    public Vector3 CapsuleColliderCenter => capsuleColliderCenter;
    public float CapsuleColliderRadius => capsuleColliderRadius;
    public float CapsuleColliderHeight => capsuleColliderHeight;
    public int CapsuleColliderDirection => capsuleColliderDirection;

    public ProjectileType ProjectileType => projectileType;
    public int Speed => speed;
    public float ArcMultiplier => arcMultiplier;

    public Vector3 AmmunitionPositionOffset => ammunitionPositionOffset;
    public Vector3 AmmunitionRotation => ammunitionRotation;

    public override bool IsEquipment() => true;

    public override bool IsWeapon() => false;

    public override bool IsMeleeWeapon() => false;

    public override bool IsRangedWeapon() => false;

    public override bool IsWearable() => false;

    public override bool IsShield() => false;

    public override bool IsBag() => false;

    public override bool IsPortableContainer() => false;

    public override bool IsConsumable() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsKey() => false;

    public override bool IsAmmunition() => true;
}
