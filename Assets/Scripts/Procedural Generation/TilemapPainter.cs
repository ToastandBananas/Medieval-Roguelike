using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapPainter
{
    public static void PaintTilemap(Tilemap tilemap, float[,] heightMap, TilemapBiomeData tilemapBiomeData, TilemapType tilemapType)
    {
        MapGenerator mapGenerator = MapGenerator.instance;
        EndlessTerrain endlessTerrain = EndlessTerrain.instance;
        Vector3Int tileCoord;
        
        for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
        {
            for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
            {
                tileCoord = new Vector3Int(x, y, 0);
                tilemap.SetTile(tileCoord, DetermineTile(tilemap, heightMap, tileCoord, tilemapBiomeData, mapGenerator, tilemapType));
            }
        }
    }

    static RuleTile DetermineTile(Tilemap tilemap, float[,] heightMap, Vector3Int tileCoord, TilemapBiomeData tilemapBiomeData, MapGenerator mapGenerator, TilemapType tilemapType)
    {
        float heightMapValue = heightMap[tileCoord.x, tileCoord.y];

        Tile newTile = (Tile)tilemap.GetTile(tileCoord);
        Vector3 worldPos = tilemap.GetCellCenterWorld(tileCoord);

        if (tilemapType == TilemapType.Water)
        {
            if (heightMapValue <= mapGenerator.regions[1].height) // Deep Water
            {
                if (tilemapBiomeData.deepWaterTiles != null)
                {
                    GameTiles.instance.deepWaterTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.deepWaterTiles;
                }
            }
        }

        if (tilemapType == TilemapType.Ground)
        {
            if (heightMapValue > mapGenerator.regions[1].height && heightMapValue <= mapGenerator.regions[2].height) // Shallow water
            {
                if (tilemapBiomeData.shallowWaterTiles != null)
                {
                    GameTiles.instance.shallowWaterTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.shallowWaterTiles;
                }
            }

            if (heightMapValue > mapGenerator.regions[2].height && heightMapValue <= mapGenerator.regions[3].height) // Sand
            {
                if (tilemapBiomeData.sandTiles != null)
                {
                    GameTiles.instance.sandTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.sandTiles;
                }
            }

            if (heightMapValue > mapGenerator.regions[3].height && heightMapValue <= mapGenerator.regions[4].height) // Grass 1
            {
                if (tilemapBiomeData.shortGrassRuleTile != null)
                {
                    GameTiles.instance.shortGrassTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.shortGrassRuleTile;
                }
            }

            if (heightMapValue > mapGenerator.regions[4].height && heightMapValue <= mapGenerator.regions[5].height) // Grass 2
            {
                if (tilemapBiomeData.tallGrassTiles != null)
                {
                    GameTiles.instance.tallGrassTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.tallGrassTiles;
                }
            }

            if (heightMapValue > mapGenerator.regions[5].height && heightMapValue <= mapGenerator.regions[6].height) // Rock 1
            {
                if (tilemapBiomeData.rockyGroundTiles != null)
                {
                    GameTiles.instance.rockyGroundTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.rockyGroundTiles;
                }
            }
        }

        if (tilemapType == TilemapType.Wall)
        {
            if (heightMapValue > mapGenerator.regions[6].height)
            {
                if (tilemapBiomeData.rockyMountainTiles != null)
                {
                    GameTiles.instance.rockyMountainTiles.Add(worldPos, newTile);
                    return tilemapBiomeData.rockyMountainTiles; // Rock 2
                }
            }

        }

        return null;
    }

    static Tile DetermineTileFromPallette(float[,] heightMap, Vector3Int tileCoord, Tilemap tilemap, Tile[] tilePallette)
    {
        // Get heightmap value of tile we are checking
        float tileHeight = heightMap[tileCoord.x, tileCoord.y];
        // Determine region based on heightmap value

        // Get heightmap values of each neighboring tile
        // Determine regions for each based on heighmap values

        // Compare original tile's heightmap value to each neighboring tile's heightmap value

        // Set bools for each neighbor tile

        // Compare bools to determine tile (i.e. If has similar left and top tiles, but not right and bottom, use a bottom right corner tile

        /*bool hasSimilarLeftNeighbor, hasSimilarRightNeighbor, hasSimilarTopNeighbor, hasSimilarBelowNeighbor;
        Tile[] neighbors = TileUtilities.GetTileNeighbors(tilemap, tileCoord);

        if (neighbors[0]) // Left neighbor*/

        return null;
    }
}
