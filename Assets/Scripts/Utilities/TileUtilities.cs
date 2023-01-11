using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TilemapType { Ground, Wall, Water }

public static class TileUtilities
{
    public static Tile[] GetTileNeighbors(Tilemap tilemap, Vector3Int tileCoord, bool getDiagonals = false)
    {
        Tile[] neighbors;
        if (getDiagonals)
            neighbors = new Tile[8];
        else
            neighbors = new Tile[4];

        neighbors[0] = tilemap.GetTile<Tile>(tileCoord + new Vector3Int(-1, 0, 0)); // Left 
        neighbors[1] = tilemap.GetTile<Tile>(tileCoord + new Vector3Int(1, 0, 0));  // Right
        neighbors[2] = tilemap.GetTile<Tile>(tileCoord + new Vector3Int(0, 1, 0));  // Up
        neighbors[3] = tilemap.GetTile<Tile>(tileCoord + new Vector3Int(0, -1, 0)); // Down

        if (getDiagonals)
        {

        }

        return neighbors;
    }

    public static Tile GetTileFromWorldPosition(Vector3 worldPos, Dictionary<Vector3, Tile> tileDictionary)
    {
        Tile tile;
        tileDictionary.TryGetValue(worldPos, out tile);
        if (tile != null)
            return tile;

        return null;
    }
}