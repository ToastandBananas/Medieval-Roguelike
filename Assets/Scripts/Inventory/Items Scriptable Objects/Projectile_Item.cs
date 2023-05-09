using UnityEngine;

public enum ProjectileType
{
    Arrow = 0,
    Bolt = 10,
    BluntObject = 20,
    Explosive = 30,
};

[CreateAssetMenu(fileName = "New Projectile", menuName = "Held Item/Projectile")]
public class Projectile_Item : Item
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    [Header("Capsule Collider")]
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
    [SerializeField] Vector3 projectilePositionOffset;
    [SerializeField] Vector3 projectileRotation;
    [SerializeField] Vector3 projectileScale = Vector3.one;

    public Mesh ProjectileMesh() => mesh;
    public Material ProjectileMaterial() => material;

    public Vector3 CapsuleColliderCenter() => capsuleColliderCenter;
    public float CapsuleColliderRadius() => capsuleColliderRadius;
    public float CapsuleColliderHeight() => capsuleColliderHeight;
    public int CapsuleColliderDirection() => capsuleColliderDirection;

    public ProjectileType ProjectilesType() => projectileType;
    public int Speed() => speed;
    public float ArcMultiplier() => arcMultiplier;

    public Vector3 ProjectilePositionOffset() => projectilePositionOffset;
    public Vector3 ProjectileRotation() => projectileRotation;
    public Vector3 ProjectileScale() => projectileScale;

    public override bool IsEquipment() => false;

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
