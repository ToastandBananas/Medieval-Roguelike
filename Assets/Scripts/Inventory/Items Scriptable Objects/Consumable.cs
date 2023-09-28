using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable")]
public class Consumable : Item
{
    [SerializeField] ItemChangeThreshold[] itemChangeThresholds;

    public override bool Use(Unit unit, ItemData itemData, int amountToUse = 1)
    {
        base.Use(unit, itemData, amountToUse);

        if (ThresholdReached(itemData, true, out ItemChangeThreshold newThreshold))
        {
            if (itemData.MyInventory() != null)
            {
                SlotCoordinate originalTargetSlotCoordinate = itemData.MyInventory().GetSlotCoordinateFromItemData(itemData);
                itemData.MyInventory().RemoveItem(itemData);
                itemData.SetItem(newThreshold.NewItem);
                itemData.MyInventory().TryAddItemAt(itemData.MyInventory().GetSlotCoordinate(originalTargetSlotCoordinate.coordinate.x - width + newThreshold.NewItem.width, originalTargetSlotCoordinate.coordinate.y - height + newThreshold.NewItem.height), itemData);
            }
        }

        return true;
    }

    public bool ThresholdReached(ItemData itemData, bool usedSome, out ItemChangeThreshold newThreshold)
    {
        newThreshold = null;
        float percentRemaining = 1;

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
                if (itemChangeThresholds[i].NewItem == this)
                    continue;

                if (usedSome == false && percentRemaining >= itemChangeThresholds[i].ThresholdPercentage)
                    newThreshold = itemChangeThresholds[i];
                if (usedSome && percentRemaining <= itemChangeThresholds[i].ThresholdPercentage)
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
