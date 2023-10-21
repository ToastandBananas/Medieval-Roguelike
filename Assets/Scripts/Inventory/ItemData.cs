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
                            remainingUses = Random.Range(1, Mathf.RoundToInt((item.GetItemChangeThresholds()[0].ThresholdPercentage / 100f) * item.MaxUses) + 1);
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
                        damage = Random.Range(weapon.MinDamage, weapon.MaxDamage + 1);
                        accuracyModifier = Random.Range(weapon.MinAccuracyModifier, weapon.MaxAccuracyModifier);
                    }
                    else if (item is Shield)
                    {
                        Shield shield = item as Shield;
                        blockPower = Random.Range(shield.MinBlockPower, shield.MaxBlockPower + 1);
                    }
                }
            }
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

            if (itemDataToCompare.item is Weapon)
            {
                thisItemDatasPoints += damage;
                itemDataToComparesPoints += itemDataToCompare.damage;

                thisItemDatasPoints += blockPower;
                itemDataToComparesPoints += itemDataToCompare.damage;

                thisItemDatasPoints += accuracyModifier;
                itemDataToComparesPoints += itemDataToCompare.accuracyModifier;
            }

            if (itemDataToComparesPoints > thisItemDatasPoints)
                return false;
            return true;
        }

        public bool IsEqual(ItemData otherItemData)
        {
            if (otherItemData == null)
                return false;

            if (item == otherItemData.item && damage == otherItemData.damage && accuracyModifier == otherItemData.accuracyModifier && blockPower == otherItemData.blockPower)
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
            temp.hasBeenRandomized = hasBeenRandomized;

            item = otherItemData.item;
            name = item.Name;
            currentStackSize = otherItemData.currentStackSize;
            remainingUses = otherItemData.remainingUses;
            damage = otherItemData.damage;
            accuracyModifier = otherItemData.accuracyModifier;
            blockPower = otherItemData.blockPower;
            hasBeenRandomized = otherItemData.hasBeenRandomized;

            otherItemData.item = temp.item;
            otherItemData.name = otherItemData.item.Name;
            otherItemData.currentStackSize = temp.currentStackSize;
            otherItemData.remainingUses = temp.remainingUses;
            otherItemData.damage = temp.damage;
            otherItemData.accuracyModifier = temp.accuracyModifier;
            otherItemData.blockPower = temp.blockPower;
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

        public void Use(int uses) => remainingUses -= uses;

        public void AddToUses(int uses) => remainingUses += uses;

        public void ReplenishUses() => remainingUses = item.MaxUses;

        public void SetItem(Item newItem) => item = newItem;

        public Item Item => item;

        public int CurrentStackSize => currentStackSize;

        public int Damage => damage;

        public float AccuracyModifier => accuracyModifier;

        public int BlockPower => blockPower;

        public int RemainingUses => remainingUses;

        public bool ShouldRandomize => hasBeenRandomized;

        public SlotCoordinate InventorySlotCoordinate() => inventorySlotCoordinate;

        public void SetInventorySlotCoordinate(SlotCoordinate slotCoordinate) => inventorySlotCoordinate = slotCoordinate;

        public Inventory MyInventory => inventorySlotCoordinate != null && inventorySlotCoordinate.myInventory != null ? inventorySlotCoordinate.myInventory : null;
    }
}
