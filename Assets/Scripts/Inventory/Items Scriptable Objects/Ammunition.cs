using UnityEngine;

namespace InventorySystem
{
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

        [Header("Inside Loose Quiver Mesh")]
        [SerializeField] Mesh insideLooseQuiverMesh;
        [SerializeField] Material insideLooseQuiverMaterial;

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

        public override Sprite InventorySprite(ItemData itemData = null)
        {
            if (itemData == null)
                return base.InventorySprite();

            ItemChangeThreshold itemChangeThreshold = ItemChangeThreshold.GetCurrentItemChangeThreshold(itemData, itemChangeThresholds);
            if (itemChangeThreshold != null && itemChangeThreshold.NewSprite != null)
                return itemChangeThreshold.NewSprite;
            return base.InventorySprite();
        }

        void OnEnable()
        {
            if (initialized == false)
            {
                equipSlot = EquipSlot.Quiver;
                initialized = true;
            }
        }

        public ItemChangeThreshold[] ItemChangeThresholds => itemChangeThresholds;
        public Sprite[] QuiverSprites => quiverSprites;

        public Mesh InsideLooseQuiverMesh => insideLooseQuiverMesh;
        public Material InsideLooseQuiverMaterial => insideLooseQuiverMaterial;

        public Vector3 CapsuleColliderCenter => capsuleColliderCenter;
        public float CapsuleColliderRadius => capsuleColliderRadius;
        public float CapsuleColliderHeight => capsuleColliderHeight;
        public int CapsuleColliderDirection => capsuleColliderDirection;

        public ProjectileType ProjectileType => projectileType;
        public int Speed => speed;
        public float ArcMultiplier => arcMultiplier;
    }
}
