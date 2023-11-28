using UnityEngine;
using UnitSystem;
using InteractableObjects;

namespace InventorySystem
{
    public enum ItemSize { ExtraSmall, VerySmall, Small, Medium, Large, VeryLarge, ExtraLarge }

    public enum ItemType
    {
        BasicItem = 0, MeleeWeapon = 1, ThrowingDagger = 10, ThrowingAxe = 11, ThrowingStar = 12, ThrowingClub = 13, Shield = 20, Bow = 30, Crossbow = 40, Bomb = 45, Arrow = 50, CrossbowBolt = 60, Quiver = 70, Clothing = 80, BodyArmor = 90, LegArmor = 95, Helm = 100, Boots = 110, Gloves = 120, Belt = 130, BeltPouch = 131, Backpack = 140, Cape = 150, PortableContainer = 160,
        MedicalSupply = 170, Herb = 180, Food = 190, Drink = 200, Potion = 210, Ingredient = 220, Seed = 230, Jewelry = 240, Readable = 250, Key = 260, QuestItem = 270
    }

    public enum ItemMaterial
    {
        Liquid, ViscousLiquid, Meat, Bone, Food, Fat, Bug, Leaf, Charcoal, Wood, Bark, Paper, Hair, Linen, QuiltedLinen, Cotton, Wool, QuiltedWool, Silk, Hemp, Fur,
        UncuredHide, Rawhide, SoftLeather, HardLeather, Keratin, Chitin, Glass, Obsidian, Stone, Gemstone, Silver, Gold, Copper, Bronze, Iron, Brass, Steel, Mithril, Dragonscale
    }

    public abstract class Item : ScriptableObject
    {
        [Header("General Item Info")]
        [SerializeField] new string name = "New Item";
        [SerializeField] string pluralName;
        [SerializeField, TextArea(1, 5)] string description;
        [SerializeField] protected ItemType itemType;
        [SerializeField] ProjectileType thrownProjectileType = ProjectileType.BluntObject;
        [SerializeField] ItemMaterial mainMaterial;
        [SerializeField] ItemSize itemSize;

        [Header("Inventory")]
        [SerializeField] protected int width = 1;
        [SerializeField] protected int height = 1;
        [SerializeField] float weight = 0.1f;
        [SerializeField] protected int maxStackSize = 1;
        [SerializeField] Sprite inventorySprite;
        [SerializeField] Sprite hotbarSprite;

        [Header("Multiple Uses?")]
        [SerializeField] protected int maxUses = 1;
        [SerializeField] bool isUsable = true;

        [Header("Value")]
        [SerializeField] Vector2Int valueRange;

        [Header("Pickup Mesh")]
        [SerializeField] Mesh pickupMesh;
        [SerializeField] Material[] pickupMeshRendererMaterials;

        public string Name => name;
        public string PluralName => pluralName;
        public string Description => description;
        public ItemType ItemType => itemType;
        public ProjectileType ThrownProjectileType => thrownProjectileType;
        public ItemMaterial MainMaterial => mainMaterial;
        public ItemSize ItemSize => itemSize;

        public int Width => width;
        public int Height => height;
        public float Weight => weight;
        public int MaxStackSize => maxStackSize;
        public virtual Sprite InventorySprite(ItemData itemData = null) => inventorySprite;
        public Sprite HotbarSprite(ItemData itemData) => hotbarSprite == null ? InventorySprite(itemData) : hotbarSprite;

        public Vector2Int ValueRange => valueRange;

        public Mesh PickupMesh => pickupMesh;
        public Material[] PickupMeshRendererMaterials => pickupMeshRendererMaterials;

        public int MaxUses => maxUses;
        public bool IsUsable => isUsable;

        protected bool initialized;

        public virtual bool Use(Unit unit, ItemData itemData, Slot slotUsingFrom, LooseItem looseItemUsing, int amountToUse = 1)
        {
            return true;
        }

        public ItemChangeThreshold[] GetItemChangeThresholds()
        {
            if (this is Ammunition)
                return Ammunition.ItemChangeThresholds;
            else if (this is Consumable)
                return Consumable.ItemChangeThresholds;
            return null;
        }

        /// <summary>Used to determine how much a character can carry in their hands, based off of ItemSize.</summary>
        public float GetSizeFactor()
        {
            switch (itemSize)
            {
                case ItemSize.ExtraSmall:
                    return 0.05f;
                case ItemSize.VerySmall:
                    return 0.1f;
                case ItemSize.Small:
                    return 0.25f;
                case ItemSize.Medium:
                    return 0.5f;
                case ItemSize.Large:
                    return 1f;
                case ItemSize.VeryLarge:
                    return 1.5f;
                case ItemSize.ExtraLarge:
                    return 2f;
                default:
                    return 1f;
            }
        }

        public BasicItem BasicItem => this as BasicItem;

        public Backpack Backpack => this as Backpack;

        public Consumable Consumable => this as Consumable;

        public Equipment Equipment => this as Equipment;

        public HeldEquipment HeldEquipment => this as HeldEquipment;

        public Ammunition Ammunition => this as Ammunition;

        public Shield Shield => this as Shield;

        public Weapon Weapon => this as Weapon;

        public Armor Armor => this as Armor;

        public BodyArmor BodyArmor => this as BodyArmor;

        public Boots Boots => this as Boots;

        public Shirt Shirt => this as Shirt;

        public Helm Helm => this as Helm;

        public MeleeWeapon MeleeWeapon => this as MeleeWeapon;

        public RangedWeapon RangedWeapon => this as RangedWeapon;

        public Wearable Wearable => this as Wearable;

        public VisibleArmor VisibleArmor => this as VisibleArmor;

        public WearableContainer WearableContainer => this as WearableContainer;

        public Quiver Quiver => this as Quiver;

        public Belt Belt => this as Belt;
    }
}
