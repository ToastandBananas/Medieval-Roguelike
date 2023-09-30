using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable")]
public class Consumable : Item
{
    [SerializeField] ItemChangeThreshold[] itemChangeThresholds;

    public override bool Use(Unit unit, ItemData itemData, int amountToUse = 1)
    {
        ItemChangeThreshold currentItemChangeThreshold = GetCurrentItemChangeThreshold(itemData);

        base.Use(unit, itemData, amountToUse);

        if (ThresholdReached(itemData, true, currentItemChangeThreshold, out ItemChangeThreshold newThreshold))
        {
            if (itemData.MyInventory() != null)
            {
                SlotCoordinate itemsSlotCoordinate = itemData.MyInventory().GetSlotCoordinateFromItemData(itemData);
                if (newThreshold.NewItem == currentItemChangeThreshold.NewItem)
                {
                    if (itemsSlotCoordinate.myInventory.SlotVisualsCreated)
                        itemsSlotCoordinate.myInventory.GetSlotFromCoordinate(itemsSlotCoordinate).InventoryItem.SetupIconSprite(true);
                }
                else
                {
                    itemData.MyInventory().RemoveItem(itemData);
                    itemData.SetItem(newThreshold.NewItem);
                    itemData.MyInventory().TryAddItemAt(itemData.MyInventory().GetSlotCoordinate(itemsSlotCoordinate.coordinate.x - width + newThreshold.NewItem.Width, itemsSlotCoordinate.coordinate.y - height + newThreshold.NewItem.Height), itemData);
                }
            }
        }

        return true;
    }

    public override Sprite InventorySprite(ItemData itemData = null)
    {
        if (itemData == null)
            return base.InventorySprite();

        ItemChangeThreshold itemChangeThreshold = GetCurrentItemChangeThreshold(itemData);
        if (itemChangeThreshold.NewSprite != null)
            return itemChangeThreshold.NewSprite;
        return base.InventorySprite();
    }

    public ItemChangeThreshold GetCurrentItemChangeThreshold(ItemData itemData)
    {
        ItemChangeThreshold itemChangeThreshold = null;
        float percentRemaining = 100;

        if (maxUses > 1)
        {
            int count = itemData.RemainingUses;
            percentRemaining = Mathf.RoundToInt(((float)count / maxUses) * 100f);
        }
        else if (maxStackSize > 1)
        {
            int count = itemData.CurrentStackSize;
            percentRemaining = Mathf.RoundToInt(((float)count / maxStackSize) * 100f);
        }

        for (int i = 0; i < itemChangeThresholds.Length; i++)
        {
            if (itemChangeThreshold == null)
                itemChangeThreshold = itemChangeThresholds[i];
            else if ((percentRemaining <= itemChangeThresholds[i].ThresholdPercentage || Mathf.Approximately(percentRemaining, itemChangeThresholds[i].ThresholdPercentage)) && itemChangeThresholds[i].ThresholdPercentage < itemChangeThreshold.ThresholdPercentage)
                itemChangeThreshold = itemChangeThresholds[i];
            else if ((percentRemaining >= itemChangeThresholds[i].ThresholdPercentage || Mathf.Approximately(percentRemaining, itemChangeThresholds[i].ThresholdPercentage)) && itemChangeThresholds[i].ThresholdPercentage > itemChangeThreshold.ThresholdPercentage)
                itemChangeThreshold = itemChangeThresholds[i];
        }

        return itemChangeThreshold;
    }

    public bool ThresholdReached(ItemData itemData, bool usedSome, ItemChangeThreshold currentThreshold, out ItemChangeThreshold newThreshold)
    {
        newThreshold = null;
        float percentRemaining = 100;

        if (maxUses > 1) 
        {
            int count = itemData.RemainingUses;
            percentRemaining = Mathf.RoundToInt(((float)count / maxUses) * 100f);
        }
        else if (maxStackSize > 1) 
        {
            int count = itemData.CurrentStackSize;
            percentRemaining = Mathf.RoundToInt(((float)count / maxStackSize) * 100f);
        }

        if (percentRemaining != 0)
        {
            for (int i = 0; i < itemChangeThresholds.Length; i++)
            {
                if (itemChangeThresholds[i] == currentThreshold)
                    continue;
                
                if (usedSome == false && (percentRemaining >= itemChangeThresholds[i].ThresholdPercentage || Mathf.Approximately(percentRemaining, itemChangeThresholds[i].ThresholdPercentage)))
                    newThreshold = itemChangeThresholds[i];
                else if (usedSome && (percentRemaining <= itemChangeThresholds[i].ThresholdPercentage || Mathf.Approximately(percentRemaining, itemChangeThresholds[i].ThresholdPercentage)))
                    newThreshold = itemChangeThresholds[i];
            }

            if (newThreshold != null)
                return true;
        }

        return false;
    }

    public override bool IsBag() => false;

    public override bool IsConsumable() => true;

    public override bool IsEquipment() => false;

    public override bool IsKey() => false;

    public override bool IsMedicalSupply() => false;

    public override bool IsMeleeWeapon() => false;

    public override bool IsPortableContainer() => false;

    public override bool IsRangedWeapon() => false;

    public override bool IsAmmunition() => false;

    public override bool IsShield() => false;

    public override bool IsWeapon() => false;

    public override bool IsWearable() => false;
}
