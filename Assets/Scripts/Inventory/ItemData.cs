using UnityEngine;

[System.Serializable]
public class ItemData
{
    [SerializeField] Item item;
    [SerializeField] int currentStackSize = 1;

    [SerializeField] int damage;
    [SerializeField] float accuracyModifier;

    [SerializeField] int blockPower;

    [SerializeField] bool hasBeenRandomized;

    Vector2 inventorySlotCoordinate = Vector2.zero;
    bool inventorySlotCoordinateAssigned;

    public void RandomizeData()
    {
        if (item != null && hasBeenRandomized == false)
        {
            hasBeenRandomized = true;

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

    public void TransferData(ItemData itemDataToCopy)
    {
        item = itemDataToCopy.item;
        currentStackSize = itemDataToCopy.currentStackSize;
        damage = itemDataToCopy.damage;
        accuracyModifier = itemDataToCopy.accuracyModifier;
        blockPower = itemDataToCopy.blockPower;
        hasBeenRandomized = true;
    }

    public void SwapData(ItemData otherItemData)
    {
        ItemData temp = new ItemData();
        temp.item = item;
        temp.currentStackSize = currentStackSize;
        temp.damage = damage;
        temp.accuracyModifier = accuracyModifier;
        temp.blockPower = blockPower;

        item = otherItemData.item;
        currentStackSize = otherItemData.currentStackSize;
        damage = otherItemData.damage;
        accuracyModifier = otherItemData.accuracyModifier;
        blockPower = otherItemData.blockPower;
        hasBeenRandomized = true;

        otherItemData.item = temp.item;
        otherItemData.currentStackSize = temp.currentStackSize;
        otherItemData.damage = temp.damage;
        otherItemData.accuracyModifier = temp.accuracyModifier;
        otherItemData.blockPower = temp.blockPower;
        otherItemData.hasBeenRandomized = true;
    }

    public void ClearItemData()
    {
        hasBeenRandomized = false;
        item = null;
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

    public Item Item() => item;

    public int CurrentStackSize() => currentStackSize;

    public int Damage() => damage;

    public float AccuracyModifier() => accuracyModifier;

    public int BlockPower() => blockPower;

    public bool HasBeenInitialized() => hasBeenRandomized;

    public Vector2 InventorySlotCoordinate() => inventorySlotCoordinate;

    public bool InventorySlotCoordinateAssigned() => inventorySlotCoordinateAssigned;
}
