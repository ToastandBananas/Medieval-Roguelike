using UnityEngine;

namespace InventorySystem
{
    [System.Serializable]
    public class ItemData
    {
        [SerializeField] Item item;
        [SerializeField] int currentStackSize = 1;
        [SerializeField] int remainingUses = 1;

        [SerializeField] int minDamage;
        [SerializeField] int maxDamage;
        [SerializeField] float throwingDamageMultiplier;

        [SerializeField] float armorEffectiveness;
        [SerializeField] float armorPierce;

        [SerializeField] float accuracyModifier;
        [SerializeField] float knockbackChanceModifier;

        [SerializeField] int blockPower;
        [SerializeField] float blockChanceModifier;
        [SerializeField] int defense;

        [SerializeField] int value;

        [SerializeField] bool hasBeenRandomized;

        SlotCoordinate inventorySlotCoordinate;

        public ItemData() { }

        public ItemData(Item item)
        {
            this.item = item;
            RandomizeData();
        }

        public ItemData(ItemData itemDataToCopy) => TransferData(itemDataToCopy);

        public void RandomizeData(bool forceRandomization = false)
        {
            if (item != null)
            {
                if (forceRandomization || hasBeenRandomized == false)
                {
                    hasBeenRandomized = true;

                    // Stack size
                    if (item.MaxStackSize > 1)
                    {
                        if (item.GetItemChangeThresholds().Length > 0)
                        {
                            currentStackSize = Random.Range(1, item.MaxStackSize + 1);
                            if (ItemChangeThreshold.ThresholdReached(this, true, item.GetItemChangeThresholds()[0], item.GetItemChangeThresholds(), out ItemChangeThreshold newThreshold))
                            {
                                if (item != newThreshold.NewItem)
                                    item = newThreshold.NewItem;
                            }
                        }
                        else
                            currentStackSize = Random.Range(1, item.MaxStackSize + 1);
                    }
                    else
                        currentStackSize = 1;

                    // Uses
                    if (item.MaxUses > 1 && item.MaxStackSize == 1)
                    {
                        if (item.GetItemChangeThresholds().Length > 0)
                        {
                            remainingUses = Random.Range(1, Mathf.RoundToInt(item.GetItemChangeThresholds()[0].ThresholdPercentage / 100f * item.MaxUses) + 1);
                            if (ItemChangeThreshold.ThresholdReached(this, true, item.GetItemChangeThresholds()[0], item.GetItemChangeThresholds(), out ItemChangeThreshold newThreshold))
                            {
                                if (item != newThreshold.NewItem)
                                    item = newThreshold.NewItem;
                            }
                        }
                        else
                            remainingUses = Random.Range(1, item.MaxUses);
                    }
                    else
                        remainingUses = 1;

                    throwingDamageMultiplier = Mathf.RoundToInt(Random.Range(item.MinThrowingDamageMultiplier, item.MaxThrowingDamageMultiplier) * 100f) / 100f;

                    if (item is Equipment)
                    {
                        if (item is Armor)
                        {
                            Armor armor = item as Armor;
                            defense = Random.Range(armor.MinDefense, armor.MaxDefense + 1);
                        }
                        else if (item is Shield)
                        {
                            Shield shield = item as Shield;
                            blockChanceModifier = Mathf.RoundToInt(Random.Range(shield.MinBlockChanceModifier, shield.MaxBlockChanceModifier) * 100f) / 100f;
                            blockPower = Random.Range(shield.MinBlockPower, shield.MaxBlockPower + 1);
                            minDamage = Random.Range(shield.MinMinimumDamage, shield.MaxMinimumDamage + 1);
                            maxDamage = Random.Range(shield.MinMaximumDamage, shield.MaxMaximumDamage + 1);
                        }
                        else if (item is Weapon)
                        {
                            Weapon weapon = item as Weapon;
                            accuracyModifier = Mathf.RoundToInt(Random.Range(weapon.MinAccuracyModifier, weapon.MaxAccuracyModifier) * 100f) / 100f;
                            armorEffectiveness = Mathf.RoundToInt(Random.Range(weapon.MinArmorEffectiveness, weapon.MaxArmorEffectiveness) * 100f) / 100f;
                            armorPierce = Mathf.RoundToInt(Random.Range(weapon.MinArmorPierce, weapon.MaxArmorPierce) * 100f) / 100f;
                            blockChanceModifier = Mathf.RoundToInt(Random.Range(weapon.MinBlockChanceModifier, weapon.MaxBlockChanceModifier) * 100f) / 100f;
                            minDamage = Random.Range(weapon.MinMinimumDamage, weapon.MaxMinimumDamage + 1);
                            maxDamage = Random.Range(weapon.MinMaximumDamage, weapon.MaxMaximumDamage + 1);
                            knockbackChanceModifier = Mathf.RoundToInt(Random.Range(weapon.MinKnockbackModifier, weapon.MaxKnockbackModifier) * 100f) / 100f;
                        }
                    }
                    else if (item is Consumable)
                    {

                    }

                    CalculateValue();
                }
            }
        }

        /// <summary>Value = min possible value * the difference between the min/max possible value * how good the item's stats are vs. what its max values could be if the item rolled perfect stats.</summary>
        public void CalculateValue()
        {
            if (item.ValueRange.x == item.ValueRange.y)
                value = item.ValueRange.x;
            else
            {
                float percentValue = PercentPointValue;
                if (percentValue <= 0f)
                    value = item.ValueRange.x;
                else
                    value = Mathf.RoundToInt(item.ValueRange.x + ((item.ValueRange.y - item.ValueRange.x) * percentValue));

                if (value < item.ValueRange.x)
                    value = item.ValueRange.x;
                else if (value > item.ValueRange.y)
                    value = item.ValueRange.y;
            }
        }

        /// <summary>Calculate the percentage of points that were added to the item's stats when randomized (compared to the total possible points)</summary>
        /// <returns>The percent of possible stat values this item has.</returns>
        float PercentPointValue => GetCurrentPointValue() / GetTotalPointValue();

        float GetCurrentPointValue()
        {
            float currentPoints = 0f; // Amount the stats have been increased by in relation to its base stat values, in total

            // Throwing
            currentPoints += (throwingDamageMultiplier - item.MinThrowingDamageMultiplier) * 100f;

            if (item is Equipment)
            {
                //if (item.Equipment.maxBaseDurability > 0)
                //pointIncrease += (maxDurability - equipment.minBaseDurability);

                if (item is Armor)
                {
                    Armor armor = item as Armor;
                    currentPoints += (defense - armor.MinDefense) * 2f;
                }
                else if (item is Shield)
                {
                    Shield shield = item as Shield;
                    currentPoints += (blockChanceModifier - shield.MinBlockChanceModifier) * 200f;
                    currentPoints += (blockPower - shield.MinBlockPower) * 2f;
                    currentPoints += minDamage - shield.MinMinimumDamage * 0.5f;
                    currentPoints += maxDamage - shield.MinMaximumDamage * 0.5f;
                }
                else if (item is Weapon)
                {
                    Weapon weapon = item as Weapon;
                    currentPoints += (accuracyModifier - weapon.MinAccuracyModifier) * 50f;
                    currentPoints += (armorEffectiveness = weapon.MinArmorEffectiveness) * 50f;
                    currentPoints += (armorPierce - weapon.MinArmorPierce) * 50f;
                    currentPoints += (blockChanceModifier - weapon.MinBlockChanceModifier) * 50f;
                    currentPoints += (minDamage - weapon.MinMinimumDamage) * 1.5f;
                    currentPoints += (maxDamage - weapon.MinMaximumDamage) * 1.5f;
                    currentPoints += (knockbackChanceModifier - weapon.MinKnockbackModifier) * 50f;
                }
            }
            else if (item is Consumable)
            {
                Consumable consumable = item as Consumable;
                //if (item.Consumable.ItemType == ItemType.Food)
                //pointIncrease += (freshness - consumable.minBaseFreshness) * 2f;
            }

            if (item.MaxUses > 1)
                currentPoints += remainingUses;

            return currentPoints;
        }

        float GetTotalPointValue()
        {
            // Add up all the possible points that can be added to our stats when randomized (damage, defense, etc)
            float totalPointsPossible = 0f;

            totalPointsPossible += (item.MaxThrowingDamageMultiplier - item.MinThrowingDamageMultiplier) * 100f;

            if (item is Equipment)
            {
                //if (equipment.maxBaseDurability > 0)
                    //totalPointsPossible += (equipment.maxBaseDurability - equipment.minBaseDurability);

                if (item is Armor)
                {
                    Armor armor = item as Armor;
                    totalPointsPossible += (armor.MaxDefense - armor.MinDefense) * 2f;
                }
                else if (item is Shield)
                {
                    Shield shield = item as Shield;
                    totalPointsPossible += (shield.MaxBlockChanceModifier - shield.MinBlockChanceModifier) * 200f;
                    totalPointsPossible += (shield.MaxBlockPower - shield.MinBlockPower) * 2f;
                    totalPointsPossible += (shield.MaxMinimumDamage - shield.MinMinimumDamage) * 0.5f;
                    totalPointsPossible += (shield.MaxMaximumDamage - shield.MinMaximumDamage) * 0.5f;
                }
                else if (item is Weapon)
                {
                    Weapon weapon = item as Weapon;
                    totalPointsPossible += (weapon.MaxAccuracyModifier - weapon.MinAccuracyModifier) * 50f;
                    totalPointsPossible += (weapon.MaxArmorEffectiveness - weapon.MinArmorEffectiveness) * 50f;
                    totalPointsPossible += (weapon.MaxArmorPierce - weapon.MinArmorPierce) * 50f;
                    totalPointsPossible += (weapon.MaxBlockChanceModifier - weapon.MinBlockChanceModifier) * 50f;
                    totalPointsPossible += (weapon.MaxMinimumDamage - weapon.MinMinimumDamage) * 1.5f;
                    totalPointsPossible += (weapon.MaxMaximumDamage - weapon.MinMaximumDamage) * 1.5f;
                    totalPointsPossible += (weapon.MaxKnockbackModifier - weapon.MinKnockbackModifier) * 50f;
                }
            }
            else if (item is Consumable)
            {
                //if (item.Consumable.ItemType == ItemType.Food)
                    //totalPointsPossible += (consumable.maxBaseFreshness - consumable.minBaseFreshness) * 2f;
            }

            if (item.MaxUses > 1)
                totalPointsPossible += item.MaxUses;

            return totalPointsPossible;
        }

        public bool IsBetterThan(ItemData otherItemData)
        {
            if (item == null || otherItemData.item == null)
            {
                Debug.LogWarning($"Item is null for one of the ItemDatas being compared... | This Item: {item} | Other Item: {otherItemData.item}");
                return false;
            }

            if (item.GetType() != otherItemData.item.GetType())
            {
                Debug.LogWarning($"{item.Name} and {otherItemData.item.Name} are not the same type of Item and should not be compared...");
                return false;
            }

            if (GetCurrentPointValue() > otherItemData.GetCurrentPointValue())
                return true;
            return false;
        }

        public bool IsEqual(ItemData otherItemData)
        {
            if (otherItemData == null)
                return false;

            return item == otherItemData.item
                && accuracyModifier == otherItemData.accuracyModifier
                && armorEffectiveness == otherItemData.armorEffectiveness
                && armorPierce == otherItemData.armorPierce
                && blockPower == otherItemData.blockPower
                && blockChanceModifier == otherItemData.blockChanceModifier
                && minDamage == otherItemData.minDamage
                && maxDamage == otherItemData.maxDamage
                && defense == otherItemData.defense
                && knockbackChanceModifier == otherItemData.knockbackChanceModifier
                && throwingDamageMultiplier == otherItemData.throwingDamageMultiplier;
        }

        public void TransferData(ItemData itemDataToCopy)
        {
            item = itemDataToCopy.item;
            hasBeenRandomized = itemDataToCopy.hasBeenRandomized;

            currentStackSize = itemDataToCopy.currentStackSize;
            remainingUses = itemDataToCopy.remainingUses;

            minDamage = itemDataToCopy.minDamage;
            maxDamage = itemDataToCopy.maxDamage;
            throwingDamageMultiplier = itemDataToCopy.throwingDamageMultiplier;

            armorEffectiveness = itemDataToCopy.armorEffectiveness;
            armorPierce = itemDataToCopy.armorPierce;

            accuracyModifier = itemDataToCopy.accuracyModifier;
            knockbackChanceModifier = itemDataToCopy.knockbackChanceModifier;

            blockPower = itemDataToCopy.blockPower;
            blockChanceModifier = itemDataToCopy.blockChanceModifier;
            defense = itemDataToCopy.defense;

            value = itemDataToCopy.value;
        }

        public void ClearItemData()
        {
            hasBeenRandomized = false;
            item = null;
            inventorySlotCoordinate = null;
        }

        public void SetCurrentStackSize(int stackSize)
        {
            currentStackSize = stackSize;
            if (currentStackSize > item.MaxStackSize)
                currentStackSize = item.MaxStackSize;
        }

        public void AdjustCurrentStackSize(int adjustmentAmount)
        {
            currentStackSize += adjustmentAmount;
            if (currentStackSize > item.MaxStackSize)
                currentStackSize = item.MaxStackSize;
            else if (currentStackSize < 0)
                currentStackSize = 0;
        }

        public string Name()
        {
            if (item == null)
                return "";

            if (currentStackSize > 1)
            {
                if (item.PluralName != "")
                    return item.PluralName;
                return $"{item.Name}s";
            }

            return item.Name;
        }

        public float Weight()
        {
            if (item.MaxUses > 1)
                return item.Weight * (remainingUses / item.MaxUses);
            else
                return item.Weight * currentStackSize;
        }

        public void Use(int uses) => remainingUses -= uses;

        public void AddToUses(int uses) => remainingUses += uses;

        public void ReplenishUses() => remainingUses = item.MaxUses;

        public void SetItem(Item newItem) => item = newItem;

        public Item Item => item;
        public int CurrentStackSize => currentStackSize;
        public int RemainingUses => remainingUses;

        public int Damage => Random.Range(minDamage, maxDamage + 1);
        public int MinDamage => minDamage;
        public int MaxDamage => maxDamage;
        public float ThrowingDamageMultiplier => throwingDamageMultiplier;

        public float AccuracyModifier => accuracyModifier;
        public float KnockbackChanceModifier => knockbackChanceModifier;

        public int BlockPower => blockPower;
        public float BlockChanceModifier => blockChanceModifier;
        public int Defense => defense;

        public int Value => value;

        public bool ShouldRandomize => hasBeenRandomized;

        public SlotCoordinate InventorySlotCoordinate => inventorySlotCoordinate;

        public void SetInventorySlotCoordinate(SlotCoordinate slotCoordinate) => inventorySlotCoordinate = slotCoordinate;

        public Inventory MyInventory => inventorySlotCoordinate != null && inventorySlotCoordinate.myInventory != null ? inventorySlotCoordinate.myInventory : null;
    }
}
