using UnityEngine;

[System.Serializable]
public class ItemChangeThreshold
{
    [SerializeField][Range(0f, 100f)] float thresholdPercentage = 50f;
    [SerializeField] Item newItem;

    public float ThresholdPercentage => thresholdPercentage;

    public Item NewItem => newItem;
}
