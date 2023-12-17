using UnityEngine;

namespace InventorySystem
{
    public enum ProjectileType
    {
        Arrow = 0,
        Spear = 1,
        ThrowingDagger = 2,
        Axe = 3,
        Bolt = 10,
        BluntObject = 20,
        Explosive = 30,
    };

    [CreateAssetMenu(fileName = "New Ammunition", menuName = "Inventory/Ammunition")]
    public class Item_Ammunition : Item_Equipment
    {
        [Header("Sprite Change Thresholds")]
        [SerializeField] ItemChangeThreshold[] itemChangeThresholds;

        [Header("Quiver Sprites")]
        [SerializeField] Sprite[] quiverSprites;

        [Header("Inside Loose Quiver Mesh")]
        [SerializeField] Mesh insideLooseQuiverMesh;
        [SerializeField] Material insideLooseQuiverMaterial;

        [Header("Projectile Info")]
        [SerializeField] ProjectileType projectileType;
        [SerializeField] ProjectileAnimationType projectileAnimationType;
        [SerializeField] float speedMultiplier = 1f;

        [Tooltip("Amount the arc height will be multiplied by. (0 = no arc)")]
        [SerializeField] float arcMultiplier = 1f;

        [Header("Effectiveness Against Armor")]
        [SerializeField, Range(0f, 1f)] float armorPierce;
        [SerializeField, Range(0f, 5f)] float armorEffectiveness;

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
                itemSize = ItemSize.Medium;
                initialized = true;
            }
        }

        public ItemChangeThreshold[] ItemChangeThresholds => itemChangeThresholds;
        public Sprite[] QuiverSprites => quiverSprites;

        public Mesh InsideLooseQuiverMesh => insideLooseQuiverMesh;
        public Material InsideLooseQuiverMaterial => insideLooseQuiverMaterial;

        public ProjectileType ProjectileType => projectileType;
        public ProjectileAnimationType ProjectileAnimationType => projectileAnimationType;
        public float SpeedMultiplier => speedMultiplier;
        public float ArcMultiplier => arcMultiplier;

        public float ArmorPierce => armorPierce;
        public float ArmorEffectiveness => armorEffectiveness;
    }
}
