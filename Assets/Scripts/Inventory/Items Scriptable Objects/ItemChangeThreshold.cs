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
}
