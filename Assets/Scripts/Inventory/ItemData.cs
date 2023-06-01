using UnityEngine;

[System.Serializable]
public class ItemData
{
    [SerializeField] Item item;

    [SerializeField] int damage;
    [SerializeField] float accuracyModifier;

    [SerializeField] int blockPower;

    [SerializeField] bool hasBeenInitialized;

    public void InitializeData()
    {
        if (item != null && hasBeenInitialized == false)
        {
            hasBeenInitialized = true;

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

    public void SetItemData(ItemData itemDataToCopy)
    {
        item = itemDataToCopy.item;
        damage = itemDataToCopy.damage;
        accuracyModifier = itemDataToCopy.accuracyModifier;
        blockPower = itemDataToCopy.blockPower;
        hasBeenInitialized = true;
    }

    public void SwapItemData(ItemData otherItemData)
    {
        ItemData temp = new ItemData();
        temp.item = item;
        temp.damage = damage;
        temp.accuracyModifier = accuracyModifier;
        temp.blockPower = blockPower;

        item = otherItemData.item;
        damage = otherItemData.damage;
        accuracyModifier = otherItemData.accuracyModifier;
        blockPower = otherItemData.blockPower;
        hasBeenInitialized = true;

        otherItemData.item = temp.item;
        otherItemData.damage = temp.damage;
        otherItemData.accuracyModifier = temp.accuracyModifier;
        otherItemData.blockPower = temp.blockPower;
        otherItemData.hasBeenInitialized = true;
    }

    public void ClearItemData()
    {
        hasBeenInitialized = false;
        item = null;
    }

    public Item Item() => item;

    public int Damage() => damage;

    public float AccuracyModifier() => accuracyModifier;

    public int BlockPower() => blockPower;

    public bool HasBeenInitialized() => hasBeenInitialized;
}
