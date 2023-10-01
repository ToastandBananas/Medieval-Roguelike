using UnityEngine;

[System.Serializable]
public class ItemChangeThreshold
{
    [SerializeField][Range(0f, 100f)] float thresholdPercentage = 50f;
    [SerializeField] Item newItem;
    [SerializeField] Sprite newSprite;

    public float ThresholdPercentage => thresholdPercentage;
    public Item NewItem => newItem;
    public Sprite NewSprite => newSprite;

    public static ItemChangeThreshold GetCurrentItemChangeThreshold(ItemData itemData, ItemChangeThreshold[] itemChangeThresholds)
    {
        if (itemChangeThresholds.Length == 0)
            return null;

        ItemChangeThreshold itemChangeThreshold = null;
        float percentRemaining = 100;

        if (itemData.Item.MaxUses > 1)
        {
            int count = itemData.RemainingUses;
            percentRemaining = Mathf.RoundToInt(((float)count / itemData.Item.MaxUses) * 100f);
        }
        else if (itemData.Item.MaxStackSize > 1)
        {
            int count = itemData.CurrentStackSize;
            percentRemaining = Mathf.RoundToInt(((float)count / itemData.Item.MaxStackSize) * 100f);
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

    public static bool ThresholdReached(ItemData itemData, bool usedSome, ItemChangeThreshold currentThreshold, ItemChangeThreshold[] itemChangeThresholds, out ItemChangeThreshold newThreshold)
    {
        newThreshold = null;
        if (itemChangeThresholds.Length == 0)
            return false;

        float percentRemaining = 100;

        if (itemData.Item.MaxUses > 1)
        {
            int count = itemData.RemainingUses;
            percentRemaining = Mathf.RoundToInt(((float)count / itemData.Item.MaxUses) * 100f);
        }
        else if (itemData.Item.MaxStackSize > 1)
        {
            int count = itemData.CurrentStackSize;
            percentRemaining = Mathf.RoundToInt(((float)count / itemData.Item.MaxStackSize) * 100f);
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
}
