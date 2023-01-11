using UnityEngine;

[CreateAssetMenu]
public class TilemapBiomeData : ScriptableObject
{
    [Header("Water")]
    public RuleTile shallowWaterTiles;
    public RuleTile deepWaterTiles;

    [Header("Sand")]
    public RuleTile sandTiles;

    [Header("Grass")]
    public RuleTile shortGrassRuleTile;
    public RuleTile tallGrassTiles;

    [Header("Dirt")]
    public RuleTile dirtTiles;

    [Header("Mountains")]
    public RuleTile rockyGroundTiles;
    public RuleTile rockyMountainTiles;

    [Header("Roads")]
    public RuleTile dirtRoadTiles;
}
