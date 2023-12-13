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
        [SerializeField] ItemSize itemSize;

        [Header("Throwing")]
        [SerializeField] ProjectileType thrownProjectileType = ProjectileType.BluntObject;
        [SerializeField] ProjectileAnimationType thrownAnimationType = ProjectileAnimationType.EndOverEnd;
        [SerializeField] float throwingSpeedMultiplier = 1f;
        [SerializeField, Range(-10f, 10f)] float minThrowingDamageMultiplier;
        [SerializeField, Range(-10f, 10f)] float maxThrowingDamageMultiplier;

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
        public ItemSize ItemSize => itemSize;

        public ProjectileType ThrownProjectileType => thrownProjectileType;
        public ProjectileAnimationType ThrownAnimationType => thrownAnimationType;
        public float ThrowingSpeedMultiplier => throwingSpeedMultiplier;
        public float MinThrowingDamageMultiplier => minThrowingDamageMultiplier;
        public float MaxThrowingDamageMultiplier => maxThrowingDamageMultiplier;

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

        public virtual bool Use(Unit unit, ItemData itemData, Slot slotUsingFrom, Interactable_LooseItem looseItemUsing, int amountToUse = 1) => isUsable;

        public ItemChangeThreshold[] GetItemChangeThresholds()
        {
            if (this is Item_Ammunition)
                return Ammunition.ItemChangeThresholds;
            else if (this is Item_Consumable)
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

        public Item_Basic BasicItem => this as Item_Basic;

        public Item_Backpack Backpack => this as Item_Backpack;

        public Item_Consumable Consumable => this as Item_Consumable;

        public Item_Equipment Equipment => this as Item_Equipment;

        public Item_HeldEquipment HeldEquipment => this as Item_HeldEquipment;

        public Item_Ammunition Ammunition => this as Item_Ammunition;

        public Item_Shield Shield => this as Item_Shield;

        public Item_Weapon Weapon => this as Item_Weapon;

        public Item_Armor Armor => this as Item_Armor;

        public Item_BodyArmor BodyArmor => this as Item_BodyArmor;

        public Item_Boots Boots => this as Item_Boots;

        public Item_Shirt Shirt => this as Item_Shirt;

        public Item_Helm Helm => this as Item_Helm;

        public Item_MeleeWeapon MeleeWeapon => this as Item_MeleeWeapon;

        public Item_RangedWeapon RangedWeapon => this as Item_RangedWeapon;

        public Item_Wearable Wearable => this as Item_Wearable;

        public Item_VisibleArmor VisibleArmor => this as Item_VisibleArmor;

        public Item_WearableContainer WearableContainer => this as Item_WearableContainer;

        public Item_Quiver Quiver => this as Item_Quiver;

        public Item_Belt Belt => this as Item_Belt;
    }
}
