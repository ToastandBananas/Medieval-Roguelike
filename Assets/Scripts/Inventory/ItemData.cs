using UnityEngine;

namespace InventorySystem
{
    [System.Serializable]
    public class ItemData
    {
        [SerializeField] string name;

        [SerializeField] Item item;
        [SerializeField] int currentStackSize = 1;
        [SerializeField] int remainingUses = 1;

        [SerializeField] int damage;
        [SerializeField] float accuracyModifier;

        [SerializeField] int blockPower;
        [SerializeField] float blockChanceAddOn;
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

        public ItemData(ItemData itemDataToCopy)
        {
            TransferData(itemDataToCopy);
        }

        public void RandomizeData(bool forceRandomization = false)
        {
            if (item != null)
            {
                name = item.Name;
                if (forceRandomization || hasBeenRandomized == false)
                {
                    hasBeenRandomized = true;

                    if (item.MaxStackSize > 1)
                    {
                        if (item.GetItemChangeThresholds().Length > 0)
                        {
                            currentStackSize = Random.Range(1, item.MaxStackSize + 1);
                            if (ItemChangeThreshold.ThresholdReached(this, true, item.GetItemChangeThresholds()[0], item.GetItemChangeThresholds(), out ItemChangeThreshold newThreshold))
                            {
                                if (item != newThreshold.NewItem)
                                {
                                    item = newThreshold.NewItem;
                                    name = item.Name;
                                }
                            }
                        }
                        else
                            currentStackSize = Random.Range(1, item.MaxStackSize + 1);
                    }
                    else
                        currentStackSize = 1;

                    if (item.MaxUses > 1 && item.MaxStackSize == 1)
                    {
                        if (item.GetItemChangeThresholds().Length > 0)
                        {
                            remainingUses = Random.Range(1, Mathf.RoundToInt(item.GetItemChangeThresholds()[0].ThresholdPercentage / 100f * item.MaxUses) + 1);
                            if (ItemChangeThreshold.ThresholdReached(this, true, item.GetItemChangeThresholds()[0], item.GetItemChangeThresholds(), out ItemChangeThreshold newThreshold))
                            {
                                if (item != newThreshold.NewItem)
                                {
                                    item = newThreshold.NewItem;
                                    name = item.Name;
                                }
                            }
                        }
                        else
                            remainingUses = Random.Range(1, item.MaxUses);
                    }
                    else
                        remainingUses = 1;

                    if (item is Weapon)
                    {
                        Weapon weapon = item as Weapon;
                        accuracyModifier = Mathf.RoundToInt(Random.Range(weapon.MinAccuracyModifier, weapon.MaxAccuracyModifier) * 1000f) / 1000f;
                        blockChanceAddOn = Mathf.RoundToInt(Random.Range(weapon.MinBlockChanceAddOn, weapon.MaxBlockChanceAddOn) * 1000f) / 1000f;
                        damage = Random.Range(weapon.MinDamage, weapon.MaxDamage + 1);
                    }
                    else if (item is Shield)
                    {
                        Shield shield = item as Shield;
                        blockChanceAddOn = Mathf.RoundToInt(Random.Range(shield.MinBlockChanceAddOn, shield.MaxBlockChanceAddOn) * 1000f) / 1000f;
                        blockPower = Random.Range(shield.MinBlockPower, shield.MaxBlockPower + 1);
                        damage = Random.Range(shield.MinDamage, shield.MaxDamage + 1);
                    }
                    else if (item is Armor)
                    {
                        Armor armor = item as Armor;
                        defense = Random.Range(armor.MinDefense, armor.MaxDefense + 1);
                    }

                    if (item.ValueRange.x == item.ValueRange.y)
                        value = item.ValueRange.x;
                    else 
                        CalculateValue();
                }
            }
        }

        /// <summary>Value = min possible value * the difference between the min/max possible value * how good the item's stats are vs. what its max values could be if the item rolled perfect stats.</summary>
        public void CalculateValue()
        {
            float percent = CalculatePercentPointValue();
            if (percent <= 0f)
                value = item.ValueRange.x;
            else
                value = Mathf.RoundToInt(item.ValueRange.x + ((item.ValueRange.y - item.ValueRange.x) * percent));

            if (value < item.ValueRange.x)
                value = item.ValueRange.x;
            else if (value > item.ValueRange.y)
                value = item.ValueRange.y;
        }

        float CalculatePercentPointValue()
        {
            // Calculate the percentage of points that were added to the item's stats when randomized (compared to the total possible points)
            float pointIncrease = 0f; // Amount the stats have been increased by in relation to its base stat values, in total

            if (item is Equipment)
            {
                //if (item.Equipment.maxBaseDurability > 0)
                    //pointIncrease += (maxDurability - equipment.minBaseDurability);

                if (item is Armor)
                {
                    Armor armor = item as Armor;
                    pointIncrease += (defense - armor.MinDefense) * 2f;
                }
                else if (item is Shield)
                {
                    Shield shield = item as Shield;
                    pointIncrease += (blockChanceAddOn - shield.MinBlockChanceAddOn) * 200f;
                    pointIncrease += (blockPower - shield.MinBlockPower) * 2f;
                    pointIncrease += damage - shield.MinDamage;
                }
                else if (item is Weapon)
                {
                    Weapon weapon = item as Weapon;
                    pointIncrease += (accuracyModifier - weapon.MinAccuracyModifier) * 200f;
                    pointIncrease += (blockChanceAddOn - weapon.MinBlockChanceAddOn) * 100f;
                    pointIncrease += (damage - weapon.MinDamage) * 3f;
                }
            }
            else if (item is Consumable)
            {
                Consumable consumable = item as Consumable;
                //if (item.Consumable.ItemType == ItemType.Food)
                    //pointIncrease += (freshness - consumable.minBaseFreshness) * 2f;
            }

            if (item.MaxUses > 1)
                pointIncrease += remainingUses;

            return pointIncrease / GetTotalPointValue(); // Return the percent of possible stat increases this item has
        }

        float GetTotalPointValue()
        {
            // Add up all the possible points that can be added to our stats when randomized (damage, defense, etc)
            float totalPointsPossible = 0f;

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
                    totalPointsPossible += (shield.MaxBlockChanceAddOn - shield.MinBlockChanceAddOn) * 200f;
                    totalPointsPossible += (shield.MaxBlockPower - shield.MinBlockPower) * 2f;
                    totalPointsPossible += shield.MaxDamage - shield.MinDamage;
                }
                else if (item is Weapon)
                {
                    Weapon weapon = item as Weapon;
                    totalPointsPossible += (weapon.MaxAccuracyModifier - weapon.MinAccuracyModifier) * 200f;
                    totalPointsPossible += (weapon.MaxBlockChanceAddOn - weapon.MinBlockChanceAddOn) * 100f;
                    totalPointsPossible += (weapon.MaxDamage - weapon.MinDamage) * 3f;
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

        public virtual bool IsBetterThan(ItemData itemDataToCompare)
        {
            float thisItemDatasPoints = 0f;
            float itemDataToComparesPoints = 0f;

            if (item.GetType() != itemDataToCompare.item.GetType())
            {
                Debug.LogWarning($"{item.Name} and {itemDataToCompare.item.Name} are not the same type of Item and should not be compared...");
                return false;
            }

            if (item is Armor)
            {
                thisItemDatasPoints += defense * 2;
                itemDataToComparesPoints += defense * 2;
            }
            else if (item is Shield)
            {
                thisItemDatasPoints += blockChanceAddOn * 200f;
                itemDataToComparesPoints += itemDataToCompare.blockChanceAddOn * 200f;

                thisItemDatasPoints += blockPower * 2;
                itemDataToComparesPoints += itemDataToCompare.blockPower * 2;

                thisItemDatasPoints += damage;
                itemDataToComparesPoints += itemDataToCompare.damage;
            }
            else if (item is Weapon)
            {
                thisItemDatasPoints += accuracyModifier * 200f;
                itemDataToComparesPoints += itemDataToCompare.accuracyModifier * 200f;

                thisItemDatasPoints += blockChanceAddOn * 100f;
                itemDataToComparesPoints += itemDataToCompare.blockChanceAddOn * 100f;

                thisItemDatasPoints += blockPower;
                itemDataToComparesPoints += itemDataToCompare.blockPower;

                thisItemDatasPoints += damage * 3;
                itemDataToComparesPoints += itemDataToCompare.damage * 3;
            }

            if (itemDataToComparesPoints > thisItemDatasPoints)
                return false;
            return true;
        }

        public bool IsEqual(ItemData otherItemData)
        {
            if (otherItemData == null)
                return false;

            if (item == otherItemData.item 
                && accuracyModifier == otherItemData.accuracyModifier 
                && blockPower == otherItemData.blockPower 
                && blockChanceAddOn == otherItemData.blockChanceAddOn
                && damage == otherItemData.damage
                && defense == otherItemData.defense)
                return true;
            return false;
        }

        public void TransferData(ItemData itemDataToCopy)
        {
            item = itemDataToCopy.item;
            name = item.Name;
            hasBeenRandomized = itemDataToCopy.hasBeenRandomized;

            currentStackSize = itemDataToCopy.currentStackSize;
            remainingUses = itemDataToCopy.remainingUses;

            damage = itemDataToCopy.damage;
            accuracyModifier = itemDataToCopy.accuracyModifier;
            blockPower = itemDataToCopy.blockPower;

            defense = itemDataToCopy.defense;

            value = itemDataToCopy.value;
        }

        public void SwapData(ItemData otherItemData)
        {
            ItemData temp = new ItemData();
            temp.item = item;
            temp.name = item.Name;
            temp.currentStackSize = currentStackSize;
            temp.remainingUses = remainingUses;
            temp.damage = damage;
            temp.accuracyModifier = accuracyModifier;
            temp.blockPower = blockPower;
            temp.blockChanceAddOn = blockChanceAddOn;
            temp.defense = defense;
            temp.value = value;
            temp.hasBeenRandomized = hasBeenRandomized;

            item = otherItemData.item;
            name = item.Name;
            currentStackSize = otherItemData.currentStackSize;
            remainingUses = otherItemData.remainingUses;
            damage = otherItemData.damage;
            accuracyModifier = otherItemData.accuracyModifier;
            blockPower = otherItemData.blockPower;
            blockChanceAddOn = otherItemData.blockChanceAddOn;
            defense = otherItemData.defense;
            value = otherItemData.value;
            hasBeenRandomized = otherItemData.hasBeenRandomized;

            otherItemData.item = temp.item;
            otherItemData.name = otherItemData.item.Name;
            otherItemData.currentStackSize = temp.currentStackSize;
            otherItemData.remainingUses = temp.remainingUses;
            otherItemData.damage = temp.damage;
            otherItemData.accuracyModifier = temp.accuracyModifier;
            otherItemData.blockPower = temp.blockPower;
            otherItemData.blockChanceAddOn = temp.blockChanceAddOn;
            otherItemData.defense = temp.defense;
            otherItemData.value = temp.value;
            otherItemData.hasBeenRandomized = temp.hasBeenRandomized;
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

        public float AccuracyModifier => accuracyModifier;
        public int Damage => damage;
        public int Defense => defense;

        public int BlockPower => blockPower;
        public float BlockChanceAddOn => blockChanceAddOn;

        public int Value => value;

        public bool ShouldRandomize => hasBeenRandomized;

        public SlotCoordinate InventorySlotCoordinate => inventorySlotCoordinate;

        public void SetInventorySlotCoordinate(SlotCoordinate slotCoordinate) => inventorySlotCoordinate = slotCoordinate;

        public Inventory MyInventory => inventorySlotCoordinate != null && inventorySlotCoordinate.myInventory != null ? inventorySlotCoordinate.myInventory : null;
    }
}
