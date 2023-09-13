using UnityEngine;

[System.Serializable]
public class ItemData
{
    [SerializeField] string name;

    [SerializeField] Item item;
    [SerializeField] int currentStackSize = 1;

    [SerializeField] int damage;
    [SerializeField] float accuracyModifier;

    [SerializeField] int blockPower;

    [SerializeField] bool shouldRandomize = true;

    SlotCoordinate inventorySlotCoordinate;

    public ItemData() { }

    public ItemData(Item item)
    {
        this.item = item;
        RandomizeData();
    }

    public void RandomizeData(bool forceRandomization = false)
    {
        if (item != null)
        {
            name = item.name;
            if (forceRandomization || shouldRandomize)
            {
                shouldRandomize = true;

                if (item.maxStackSize > 1)
                    currentStackSize = Random.Range(1, item.maxStackSize + 1);
                else
                    currentStackSize = 1;

                if (item.IsWeapon())
                {
                    Weapon weapon = item as Weapon;
                    damage = Random.Range(weapon.minDamage, weapon.maxDamage + 1);
                    accuracyModifier = Random.Range(weapon.minAccuracyModifier, weapon.maxAccuracyModifier);
                }
                else if (item.IsShield())
                {
                    Shield shield = item as Shield;
                    blockPower = Random.Range(shield.minBlockPower, shield.maxBlockPower + 1);
                }
            }
        }
    }

    public void TransferData(ItemData itemDataToCopy)
    {
        item = itemDataToCopy.item;
        name = item.name;
        currentStackSize = itemDataToCopy.currentStackSize;
        damage = itemDataToCopy.damage;
        accuracyModifier = itemDataToCopy.accuracyModifier;
        blockPower = itemDataToCopy.blockPower;
        shouldRandomize = true;
    }

    public void SwapData(ItemData otherItemData)
    {
        ItemData temp = new ItemData();
        temp.item = item;
        temp.name = item.name;
        temp.currentStackSize = currentStackSize;
        temp.damage = damage;
        temp.accuracyModifier = accuracyModifier;
        temp.blockPower = blockPower;

        item = otherItemData.item;
        name = item.name;
        currentStackSize = otherItemData.currentStackSize;
        damage = otherItemData.damage;
        accuracyModifier = otherItemData.accuracyModifier;
        blockPower = otherItemData.blockPower;
        shouldRandomize = true;

        otherItemData.item = temp.item;
        otherItemData.name = otherItemData.item.name;
        otherItemData.currentStackSize = temp.currentStackSize;
        otherItemData.damage = temp.damage;
        otherItemData.accuracyModifier = temp.accuracyModifier;
        otherItemData.blockPower = temp.blockPower;
        otherItemData.shouldRandomize = true;
    }

    public void ClearItemData()
    {
        shouldRandomize = false;
        item = null;
        name = "";
        inventorySlotCoordinate = null;
    }

    public void SetCurrentStackSize(int stackSize)
    {
        currentStackSize = stackSize;
        if (currentStackSize > item.maxStackSize)
            currentStackSize = item.maxStackSize;
    }

    public void AdjustCurrentStackSize(int adjustmentAmount)
    {
        currentStackSize += adjustmentAmount;
        if (currentStackSize > item.maxStackSize)
            currentStackSize = item.maxStackSize;
        else if (currentStackSize < 0)
            currentStackSize = 0;
    }

    public void SetItem(Item newItem) => item = newItem;

    public Item Item() => item;

    public int CurrentStackSize() => currentStackSize;

    public int Damage() => damage;

    public float AccuracyModifier() => accuracyModifier;

    public int BlockPower() => blockPower;

    public SlotCoordinate InventorySlotCoordinate() => inventorySlotCoordinate;

    public void SetInventorySlotCoordinate(SlotCoordinate slotCoordinate) => inventorySlotCoordinate = slotCoordinate;

    public bool ShouldRandomize => shouldRandomize;
}
