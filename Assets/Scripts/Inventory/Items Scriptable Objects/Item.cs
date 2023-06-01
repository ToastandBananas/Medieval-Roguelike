using UnityEngine;

public enum ItemSize { ExtraSmall, VerySmall, Small, Medium, Large, VeryLarge, ExtraLarge }
public enum PartialAmount { Whole, Half, Quarter, Tenth }
public enum ItemType { BasicItem, MeleeWeapon, RangedWeapon, Ammo, Clothing, Armor, Shield, Medical, Food, Drink, Ingredient, Seed, Readable, Key, QuestItem, Bag, Container }
public enum ItemMaterial
{
    Liquid, ViscousLiquid, Meat, Bone, Food, Fat, Bug, Leaf, Charcoal, Wood, Bark, Paper, Hair, Linen, QuiltedLinen, Cotton, Wool, QuiltedWool, Silk, Hemp, Fur,
    UncuredHide, Rawhide, SoftLeather, HardLeather, Keratin, Chitin, Glass, Obsidian, Stone, Gemstone, Silver, Gold, Copper, Bronze, Iron, Brass, Steel, Mithril, Dragonscale
}

public abstract class Item : ScriptableObject
{
    [Header("General Item Info")]
    new public string name = "New Item";
    public string pluralName;
    public ItemType itemType;
    public ItemMaterial mainMaterial;
    public ItemSize itemSize;
    public string description;
    public bool isUsable = true;
    public bool canUsePartial;

    [Header("Inventory")]
    public Sprite inventorySprite;
    public int width = 1;
    public int height = 1;
    public int maxStackSize = 1;
    public float weight = 0.1f;
    public float volume = 0.1f;

    [Header("Value")]
    public Vector2Int value;
    public int staticValue = 1;

    [Header("Equipped Mesh")]
    public Mesh[] meshes;
    public Material[] meshRendererMaterials;

    [Header("Pickup Mesh")]
    public Mesh pickupMesh;
    public Material pickupMeshRendererMaterial;

    public virtual void Use(Unit unit, Inventory inventory, InventoryItem invItem, ItemData itemData, int itemCount, EquipmentSlot equipSlot, PartialAmount partialAmountToUse = PartialAmount.Whole)
    {
        
    }

    /// <summary>Returns partialAmount as a whole number percent.</summary>
    public int GetPartialAmountsPercentage(PartialAmount partialAmount)
    {
        switch (partialAmount)
        {
            case PartialAmount.Whole:
                return 100;
            case PartialAmount.Half:
                return 50;
            case PartialAmount.Quarter:
                return 25;
            case PartialAmount.Tenth:
                return 10;
            default:
                return 100;
        }
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

    public BasicItem BasicItem() => this as BasicItem;

    public Consumable Consumable() => this as Consumable;

    public Equipment Equipment() => this as Equipment;

    public Ammunition Ammunition() => this as Ammunition;

    public Shield Shield() => this as Shield;

    public Weapon Weapon() => this as Weapon;

    public Wearable Wearable() => this as Wearable;

    public abstract bool IsEquipment();

    public abstract bool IsWeapon();

    public abstract bool IsMeleeWeapon();

    public abstract bool IsRangedWeapon();

    public abstract bool IsAmmunition();

    public abstract bool IsWearable();

    public abstract bool IsShield();

    public abstract bool IsBag();

    public abstract bool IsPortableContainer();

    public abstract bool IsConsumable();

    public abstract bool IsMedicalSupply();

    public abstract bool IsKey();
}
