using UnitSystem;
using UnitSystem.UI;
using UnityEngine;

namespace InventorySystem
{
    [System.Serializable]
    public class ItemData
    {
        [SerializeField] Item item;
        [SerializeField] int currentStackSize = 1;
        [SerializeField] int remainingUses = 1;

        [SerializeField] int maxDurability;
        [SerializeField] float currentDurability;

        [Header("Weapons & Unarmed")]
        [SerializeField] int minDamage;
        [SerializeField] int maxDamage;
        [SerializeField] float throwingDamageMultiplier;
        [SerializeField] float unarmedDamageMultiplier;

        [SerializeField] float effectivenessAgainstArmor;
        [SerializeField] float armorPierce;

        [SerializeField] float accuracyModifier;
        [SerializeField] float fumbleChanceModifier;
        [SerializeField] float attackKnockbackChanceModifier;
        [SerializeField] float knockbackChanceModifier;

        [Header("Defensive")]
        [SerializeField] int blockPower;
        [SerializeField] float blockChanceModifier;
        [SerializeField] int defense;
        [SerializeField] float protection;

        [Header("Other")]
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
                if (forceRandomization || !hasBeenRandomized)
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

                    if (item is Item_Equipment)
                    {
                        if (item is Item_Ammunition)
                        {
                            Item_Ammunition ammo = item as Item_Ammunition;
                            armorPierce = Mathf.RoundToInt(ammo.ArmorPierce * 100f) / 100f;
                            effectivenessAgainstArmor = Mathf.RoundToInt(ammo.ArmorEffectiveness * 100f) / 100f;
                        }
                        else if (item is Item_Armor)
                        {
                            Item_Armor armor = item as Item_Armor;
                            defense = Random.Range(armor.MinDefense, armor.MaxDefense + 1);
                            maxDurability = Random.Range(armor.MinDurability, armor.MaxDurability + 1);
                            protection = Mathf.RoundToInt(Random.Range(armor.MinProtection, armor.MaxProtection) * 100f) / 100f;

                            if (item is Item_BodyArmor)
                            {
                                Item_BodyArmor bodyArmor = item as Item_BodyArmor;
                                knockbackChanceModifier = Mathf.RoundToInt(Random.Range(bodyArmor.MinKnockbackChanceModifier, bodyArmor.MaxKnockbackChanceModifier) * 100f) / 100f;
                            }
                            else if (item is Item_LegArmor)
                            {
                                Item_LegArmor legArmor = item as Item_LegArmor;
                                knockbackChanceModifier = Mathf.RoundToInt(Random.Range(legArmor.MinKnockbackChanceModifier, legArmor.MaxKnockbackChanceModifier) * 100f) / 100f;
                            }
                            else if (item is Item_Boots)
                            {
                                Item_Boots boots = item as Item_Boots;
                                knockbackChanceModifier = Mathf.RoundToInt(Random.Range(boots.MinKnockbackChanceModifier, boots.MaxKnockbackChanceModifier) * 100f) / 100f;
                            }
                            else if (item is Item_Gloves)
                            {
                                Item_Gloves gloves = item as Item_Gloves;
                                accuracyModifier = Mathf.RoundToInt(Random.Range(gloves.MinAccuracyModifier, gloves.MaxAccuracyModifier) * 100f) / 100f;
                                fumbleChanceModifier = Mathf.RoundToInt(Random.Range(gloves.MinFumbleChanceModifier, gloves.MaxFumbleChanceModifier) * 100f) / 100f;
                                unarmedDamageMultiplier = Mathf.RoundToInt(Random.Range(gloves.MinUnarmedDamageMultiplier, gloves.MaxUnarmedDamageMultiplier) * 100f) / 100f;
                            }
                        }
                        else if (item is Item_Shield)
                        {
                            Item_Shield shield = item as Item_Shield;
                            blockChanceModifier = Mathf.RoundToInt(Random.Range(shield.MinBlockChanceModifier, shield.MaxBlockChanceModifier) * 100f) / 100f;
                            blockPower = Random.Range(shield.MinBlockPower, shield.MaxBlockPower + 1);
                            maxDurability = Random.Range(shield.MinDurability, shield.MaxDurability + 1);
                            minDamage = Random.Range(shield.MinMinimumDamage, shield.MaxMinimumDamage + 1);
                            maxDamage = Random.Range(shield.MinMaximumDamage, shield.MaxMaximumDamage + 1);
                            fumbleChanceModifier = Mathf.RoundToInt(Random.Range(shield.MinFumbleChanceModifier, shield.MaxFumbleChanceModifier) * 100f) / 100f;
                            knockbackChanceModifier = Mathf.RoundToInt(Random.Range(shield.MinKnockbackChanceModifier, shield.MaxKnockbackChanceModifier) * 100f) / 100f;
                        }
                        else if (item is Item_Weapon)
                        {
                            Item_Weapon weapon = item as Item_Weapon;
                            accuracyModifier = Mathf.RoundToInt(Random.Range(weapon.MinAccuracyModifier, weapon.MaxAccuracyModifier) * 100f) / 100f;
                            effectivenessAgainstArmor = Mathf.RoundToInt(Random.Range(weapon.MinArmorEffectiveness, weapon.MaxArmorEffectiveness) * 100f) / 100f;
                            armorPierce = Mathf.RoundToInt(Random.Range(weapon.MinArmorPierce, weapon.MaxArmorPierce) * 100f) / 100f;
                            blockChanceModifier = Mathf.RoundToInt(Random.Range(weapon.MinBlockChanceModifier, weapon.MaxBlockChanceModifier) * 100f) / 100f;
                            maxDurability = Random.Range(weapon.MinDurability, weapon.MaxDurability + 1);
                            minDamage = Random.Range(weapon.MinMinimumDamage, weapon.MaxMinimumDamage + 1);
                            maxDamage = Random.Range(weapon.MinMaximumDamage, weapon.MaxMaximumDamage + 1);
                            fumbleChanceModifier = Mathf.RoundToInt(Random.Range(weapon.MinFumbleChanceModifier, weapon.MaxFumbleChanceModifier) * 100f) / 100f;
                            attackKnockbackChanceModifier = Mathf.RoundToInt(Random.Range(weapon.MinKnockbackModifier, weapon.MaxKnockbackModifier) * 100f) / 100f;
                        }

                        currentDurability = Random.Range(0.5f * maxDurability, maxDurability);
                        if (currentDurability < 0f) currentDurability = 0f;
                    }
                    else if (item is Item_Consumable)
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

            if (item is Item_Equipment)
            {
                if (item is Item_Ammunition) // Ammo will always have a static value
                    currentPoints = 1f;
                else if (item is Item_Armor)
                {
                    Item_Armor armor = item as Item_Armor;
                    currentPoints += (defense - armor.MinDefense) * 2f;
                    currentPoints += (maxDurability - armor.MinDurability) * 0.5f;
                    currentPoints += (protection - armor.MinProtection) * 0.5f;

                    if (item is Item_BodyArmor)
                    {
                        Item_BodyArmor bodyArmor = item as Item_BodyArmor;
                        currentPoints += (knockbackChanceModifier - bodyArmor.MinKnockbackChanceModifier) * 50f;
                    }
                    else if (item is Item_LegArmor)
                    {
                        Item_LegArmor legArmor = item as Item_LegArmor;
                        currentPoints += (knockbackChanceModifier - legArmor.MinKnockbackChanceModifier) * 50f;
                    }
                    else if (item is Item_Boots)
                    {
                        Item_Boots boots = item as Item_Boots;
                        currentPoints += (knockbackChanceModifier - boots.MinKnockbackChanceModifier) * 50f;
                    }
                    else if (item is Item_Gloves)
                    {
                        Item_Gloves gloves = item as Item_Gloves;
                        currentPoints += (accuracyModifier - gloves.MinAccuracyModifier) * 50f;
                        currentPoints += (fumbleChanceModifier - gloves.MinFumbleChanceModifier) * 50f;
                        currentPoints += (unarmedDamageMultiplier - gloves.MinUnarmedDamageMultiplier) * 50f;
                    }
                }
                else if (item is Item_Shield)
                {
                    Item_Shield shield = item as Item_Shield;
                    currentPoints += (blockChanceModifier - shield.MinBlockChanceModifier) * 200f;
                    currentPoints += (blockPower - shield.MinBlockPower) * 2f;
                    currentPoints += (maxDurability - shield.MinDurability) * 0.5f;
                    currentPoints += minDamage - shield.MinMinimumDamage * 0.5f;
                    currentPoints += maxDamage - shield.MinMaximumDamage * 0.5f;
                    currentPoints += (fumbleChanceModifier - shield.MinFumbleChanceModifier) * 50f;
                    currentPoints += (knockbackChanceModifier - shield.MinKnockbackChanceModifier) * 50f;
                }
                else if (item is Item_Weapon)
                {
                    Item_Weapon weapon = item as Item_Weapon;
                    currentPoints += (accuracyModifier - weapon.MinAccuracyModifier) * 50f;
                    currentPoints += (effectivenessAgainstArmor = weapon.MinArmorEffectiveness) * 50f;
                    currentPoints += (armorPierce - weapon.MinArmorPierce) * 50f;
                    currentPoints += (blockChanceModifier - weapon.MinBlockChanceModifier) * 50f;
                    currentPoints += (maxDurability - weapon.MinDurability) * 0.5f;
                    currentPoints += (minDamage - weapon.MinMinimumDamage) * 1.5f;
                    currentPoints += (maxDamage - weapon.MinMaximumDamage) * 1.5f;
                    currentPoints += (fumbleChanceModifier - weapon.MinFumbleChanceModifier) * 50f;
                    currentPoints += (attackKnockbackChanceModifier - weapon.MinKnockbackModifier) * 50f;
                }
            }
            else if (item is Item_Consumable)
            {
                Item_Consumable consumable = item as Item_Consumable;
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

            // Throwing
            totalPointsPossible += (item.MaxThrowingDamageMultiplier - item.MinThrowingDamageMultiplier) * 100f;

            if (item is Item_Equipment)
            {
                if (item is Item_Ammunition) // Ammo will always have a static value
                    totalPointsPossible = 1f;
                else if (item is Item_Armor)
                {
                    Item_Armor armor = item as Item_Armor;
                    totalPointsPossible += (armor.MaxDefense - armor.MinDefense) * 2f;
                    totalPointsPossible += (armor.MaxDurability - armor.MinDurability) * 0.5f;
                    totalPointsPossible += (armor.MaxProtection - armor.MinProtection) * 0.5f;

                    if (item is Item_BodyArmor)
                    {
                        Item_BodyArmor bodyArmor = item as Item_BodyArmor;
                        totalPointsPossible += (bodyArmor.MaxKnockbackChanceModifier - bodyArmor.MinKnockbackChanceModifier) * 50f;
                    }
                    else if (item is Item_LegArmor)
                    {
                        Item_LegArmor legArmor = item as Item_LegArmor;
                        totalPointsPossible += (legArmor.MaxKnockbackChanceModifier - legArmor.MinKnockbackChanceModifier) * 50f;
                    }
                    else if (item is Item_Boots)
                    {
                        Item_Boots boots = item as Item_Boots;
                        totalPointsPossible += (boots.MaxKnockbackChanceModifier - boots.MinKnockbackChanceModifier) * 50f;
                    }
                    else if (item is Item_Gloves)
                    {
                        Item_Gloves gloves = item as Item_Gloves;
                        totalPointsPossible += (gloves.MaxAccuracyModifier - gloves.MinAccuracyModifier) * 50f;
                        totalPointsPossible += (gloves.MaxFumbleChanceModifier - gloves.MinFumbleChanceModifier) * 50f;
                        totalPointsPossible += (gloves.MaxUnarmedDamageMultiplier - gloves.MinUnarmedDamageMultiplier) * 50f;
                    }
                }
                else if (item is Item_Shield)
                {
                    Item_Shield shield = item as Item_Shield;
                    totalPointsPossible += (shield.MaxBlockChanceModifier - shield.MinBlockChanceModifier) * 200f;
                    totalPointsPossible += (shield.MaxBlockPower - shield.MinBlockPower) * 2f;
                    totalPointsPossible += (shield.MaxDurability - shield.MinDurability) * 0.5f;
                    totalPointsPossible += (shield.MaxMinimumDamage - shield.MinMinimumDamage) * 0.5f;
                    totalPointsPossible += (shield.MaxMaximumDamage - shield.MinMaximumDamage) * 0.5f;
                    totalPointsPossible += (shield.MaxFumbleChanceModifier - shield.MinFumbleChanceModifier) * 50f;
                    totalPointsPossible += (shield.MaxKnockbackChanceModifier - shield.MinKnockbackChanceModifier) * 50f;
                }
                else if (item is Item_Weapon)
                {
                    Item_Weapon weapon = item as Item_Weapon;
                    totalPointsPossible += (weapon.MaxAccuracyModifier - weapon.MinAccuracyModifier) * 50f;
                    totalPointsPossible += (weapon.MaxArmorEffectiveness - weapon.MinArmorEffectiveness) * 50f;
                    totalPointsPossible += (weapon.MaxArmorPierce - weapon.MinArmorPierce) * 50f;
                    totalPointsPossible += (weapon.MaxBlockChanceModifier - weapon.MinBlockChanceModifier) * 50f;
                    totalPointsPossible += (weapon.MaxDurability - weapon.MinDurability) *0.5f;
                    totalPointsPossible += (weapon.MaxMinimumDamage - weapon.MinMinimumDamage) * 1.5f;
                    totalPointsPossible += (weapon.MaxMaximumDamage - weapon.MinMaximumDamage) * 1.5f;
                    totalPointsPossible += (weapon.MaxFumbleChanceModifier - weapon.MinFumbleChanceModifier) * 50f;
                    totalPointsPossible += (weapon.MaxKnockbackModifier - weapon.MinKnockbackModifier) * 50f;
                }
            }
            else if (item is Item_Consumable)
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
            if (otherItemData == null)
            {
                Debug.LogWarning("Other Item Data is null...");
                return false;
            }

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

            return GetCurrentPointValue() > otherItemData.GetCurrentPointValue();
        }

        public bool IsEqual(ItemData otherItemData)
        {
            if (otherItemData == null)
            {
                Debug.LogWarning("Other Item Data is null...");
                return false;
            }

            return item == otherItemData.item
                && accuracyModifier == otherItemData.accuracyModifier
                && fumbleChanceModifier == otherItemData.fumbleChanceModifier
                && effectivenessAgainstArmor == otherItemData.effectivenessAgainstArmor
                && armorPierce == otherItemData.armorPierce
                && blockPower == otherItemData.blockPower
                && blockChanceModifier == otherItemData.blockChanceModifier
                && maxDurability == otherItemData.maxDurability
                && minDamage == otherItemData.minDamage
                && maxDamage == otherItemData.maxDamage
                && defense == otherItemData.defense
                && protection == otherItemData.protection
                && attackKnockbackChanceModifier == otherItemData.attackKnockbackChanceModifier
                && knockbackChanceModifier == otherItemData.knockbackChanceModifier
                && throwingDamageMultiplier == otherItemData.throwingDamageMultiplier
                && unarmedDamageMultiplier == otherItemData.unarmedDamageMultiplier;
        }

        public void TransferData(ItemData itemDataToCopy)
        {
            item = itemDataToCopy.item;
            hasBeenRandomized = itemDataToCopy.hasBeenRandomized;

            currentStackSize = itemDataToCopy.currentStackSize;
            remainingUses = itemDataToCopy.remainingUses;

            maxDurability = itemDataToCopy.maxDurability;
            currentDurability = itemDataToCopy.currentDurability;

            minDamage = itemDataToCopy.minDamage;
            maxDamage = itemDataToCopy.maxDamage;
            throwingDamageMultiplier = itemDataToCopy.throwingDamageMultiplier;
            unarmedDamageMultiplier = itemDataToCopy.unarmedDamageMultiplier;

            effectivenessAgainstArmor = itemDataToCopy.effectivenessAgainstArmor;
            armorPierce = itemDataToCopy.armorPierce;

            accuracyModifier = itemDataToCopy.accuracyModifier;
            fumbleChanceModifier = itemDataToCopy.fumbleChanceModifier;
            attackKnockbackChanceModifier = itemDataToCopy.attackKnockbackChanceModifier;
            knockbackChanceModifier = itemDataToCopy.knockbackChanceModifier;

            blockPower = itemDataToCopy.blockPower;
            blockChanceModifier = itemDataToCopy.blockChanceModifier;
            defense = itemDataToCopy.defense;
            protection = itemDataToCopy.protection;

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

        public void RepairDurability(Unit unit, float amount)
        {
            if (maxDurability <= 0f || amount == 0f)
                return;

            if (amount < 0f)
            {
                Debug.LogWarning($"Durability repair amount to {item.Name} is less than 0...");
                amount *= -1;
            }

            unit.ShowFloatingStatBars();

            float startNormalizedDurability = CurrentDurabilityNormalized;
            currentDurability += amount;
            currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);

            if (item is Item_Armor)
            {
                if (unit.IsPlayer)
                    StatBarManager_Player.UpdateArmorBar(item.Armor.EquipSlot, startNormalizedDurability);
                else if (unit.StatBarManager != null)
                    unit.StatBarManager.UpdateArmorBar(item.Armor.EquipSlot, startNormalizedDurability);
            }
        }

        public void DamageDurability(Unit unit, float amount)
        {
            if (maxDurability <= 0f || amount == 0f)
                return;

            if (amount < 0f)
            {
                Debug.LogWarning($"Durability damage to {item.Name} is less than 0...");
                amount *= -1f;
            }

            unit.ShowFloatingStatBars();

            // Debug.Log($"Damaging {item.Name} for {amount} durability");
            float startNormalizedDurability = CurrentDurabilityNormalized;
            currentDurability -= amount;
            currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);

            if (item is Item_Armor)
            {
                if (unit.IsPlayer)
                    StatBarManager_Player.UpdateArmorBar(item.Armor.EquipSlot, startNormalizedDurability);
                else if (unit.StatBarManager != null)
                    unit.StatBarManager.UpdateArmorBar(item.Armor.EquipSlot, startNormalizedDurability);
            }

            if (currentDurability == 0f)
            {
                Debug.Log(item.Name + " broke");
                if (!unit.UnitEquipment.ItemDataEquipped(this))
                    return;

                if ((unit.UnitMeshManager.leftHeldItem != null && this == unit.UnitMeshManager.leftHeldItem.ItemData) || (unit.UnitMeshManager.rightHeldItem != null && this == unit.UnitMeshManager.rightHeldItem.ItemData) 
                    || (this == unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Helm] && item.Helm.FallOffOnDeathChance > 0f))
                    DropItemManager.DropItem(unit.UnitEquipment, unit.UnitEquipment.GetEquipSlotFromItemData(this));
                else if (unit.IsPlayer && unit.UnitEquipment.SlotVisualsCreated)
                    unit.UnitEquipment.GetEquipmentSlot(unit.UnitEquipment.GetEquipSlotFromItemData(this)).InventoryItem.SetupBrokenIconImage();
            }
        }

        public void Use(int uses) => remainingUses -= uses;
        public void AddToUses(int uses) => remainingUses += uses;
        public void ReplenishUses() => remainingUses = item.MaxUses;

        public void SetItem(Item newItem) => item = newItem;

        public Item Item => item;
        public int CurrentStackSize => currentStackSize;
        public int RemainingUses => remainingUses;

        public int MaxDurability => maxDurability;
        public float CurrentDurability => currentDurability;
        public float CurrentDurabilityNormalized => maxDurability <= 0 ? 0 : currentDurability / MaxDurability;
        public bool IsBroken => maxDurability > 0f && currentDurability <= 0f;

        public int Damage => Random.Range(minDamage, maxDamage + 1);
        public int MinDamage => minDamage;
        public int MaxDamage => maxDamage;
        public float ThrowingDamageMultiplier => throwingDamageMultiplier;
        public float UnarmedDamageMultiplier => unarmedDamageMultiplier;

        public float EffectivenessAgainstArmor => effectivenessAgainstArmor;
        public float ArmorPierce => armorPierce;

        public float AccuracyModifier => accuracyModifier;
        public float FumbleChanceModifier => fumbleChanceModifier;
        public float AttackKnockbackChanceModifier => attackKnockbackChanceModifier;
        public float KnockbackChanceModifier => knockbackChanceModifier;

        public int BlockPower => blockPower;
        public float BlockChanceModifier => blockChanceModifier;
        public int Defense => defense;
        public float Protection => protection;

        public int Value => value;
        public bool ShouldRandomize => !hasBeenRandomized;

        public Inventory MyInventory => inventorySlotCoordinate != null && inventorySlotCoordinate.myInventory != null ? inventorySlotCoordinate.myInventory : null;
        public SlotCoordinate InventorySlotCoordinate => inventorySlotCoordinate;
        public void SetInventorySlotCoordinate(SlotCoordinate slotCoordinate) => inventorySlotCoordinate = slotCoordinate;

    }
}
