using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameTiles : MonoBehaviour
{
    public Dictionary<Vector3, Tile> deepWaterTiles = new Dictionary<Vector3, Tile>();
    public Dictionary<Vector3, Tile> shallowWaterTiles = new Dictionary<Vector3, Tile>();
    public Dictionary<Vector3, Tile> sandTiles = new Dictionary<Vector3, Tile>();
    public Dictionary<Vector3, Tile> shortGrassTiles = new Dictionary<Vector3, Tile>();
    public Dictionary<Vector3, Tile> tallGrassTiles = new Dictionary<Vector3, Tile>();
    public Dictionary<Vector3, Tile> rockyGroundTiles = new Dictionary<Vector3, Tile>();
    public Dictionary<Vector3, Tile> rockyMountainTiles = new Dictionary<Vector3, Tile>();

    #region Singleton
    public static GameTiles instance;
    void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
                Destroy(gameObject);
        }
        else
            instance = this;
    }
    #endregion
}
