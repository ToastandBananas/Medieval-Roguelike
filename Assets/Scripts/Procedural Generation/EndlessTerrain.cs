using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDist;

    public Transform viewer;

    public static Vector3 viewerPosition;
    Vector3 viewerPositionOld;
    static MapGenerator mapGenerator;

    static Grid grid;
    public TilemapBiomeData tilemapBiomeData;

    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector3, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector3, TerrainChunk>();
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    #region Singleton
    public static EndlessTerrain instance;
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

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;

        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDist / chunkSize);

        grid = FindObjectOfType<Grid>();

        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector3(viewer.localPosition.x - (chunkSize / 2), 0, viewer.localPosition.z - (chunkSize / 2)) / mapGenerator.terrainData.uniformScale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector3> alreadyUpdatedChunkCoords = new HashSet<Vector3>();

        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.z / chunkSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector3 viewedChunkCoord = new Vector3(currentChunkCoordX + xOffset, 0, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    else
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels));
                }
            }
        }
    }

    public class TerrainChunk
    {
        public Vector3 coord;
        
        Vector3 position;
        Bounds bounds;

        LODInfo[] detailLevels;

        GameObject tilemapChunkParent;

        GameObject groundTilemapChunk;
        Tilemap groundTilemap;

        GameObject waterTilemapChunk;
        Tilemap waterTilemap;
        Rigidbody2D waterRigidBody;
        TilemapCollider2D waterTilemapCollider;

        GameObject wallTilemapChunk;
        Tilemap wallTilemap;
        Rigidbody2D wallRigidBody;
        TilemapCollider2D wallTilemapCollider;

        MapData mapData;
        bool mapDataReceived;

        public TerrainChunk(Vector3 coord, int size, LODInfo[] detailLevels)
        {
            this.coord = coord;
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, position.y, position.z);

            // Parent
            tilemapChunkParent = new GameObject("Tilemap Chunk Parent - " + coord);
            tilemapChunkParent.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            tilemapChunkParent.transform.parent = grid.transform;

            // Ground
            groundTilemapChunk = new GameObject("Ground");
            groundTilemap = groundTilemapChunk.AddComponent<Tilemap>();
            groundTilemap.size = new Vector3Int(size, size, 0);
            groundTilemapChunk.AddComponent<TilemapRenderer>();

            groundTilemapChunk.transform.position = tilemapChunkParent.transform.position;
            groundTilemapChunk.transform.rotation = Quaternion.Euler(90, 0, 0);
            groundTilemapChunk.transform.parent = tilemapChunkParent.transform;
            groundTilemapChunk.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            // Water
            waterTilemapChunk = new GameObject("Water");
            waterTilemap = waterTilemapChunk.AddComponent<Tilemap>();
            waterTilemap.size = new Vector3Int(size, size, 0);

            waterTilemapChunk.AddComponent<TilemapRenderer>();

            waterRigidBody = waterTilemapChunk.AddComponent<Rigidbody2D>();
            waterRigidBody.bodyType = RigidbodyType2D.Kinematic;

            waterTilemapCollider = waterTilemapChunk.AddComponent<TilemapCollider2D>();
            waterTilemapCollider.usedByComposite = true;

            waterTilemapChunk.AddComponent<CompositeCollider2D>();

            waterTilemapChunk.transform.position = tilemapChunkParent.transform.position;
            waterTilemapChunk.transform.rotation = Quaternion.Euler(90, 0, 0);
            waterTilemapChunk.transform.parent = tilemapChunkParent.transform;
            waterTilemapChunk.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            // Walls
            wallTilemapChunk = new GameObject("Walls");
            wallTilemap = wallTilemapChunk.AddComponent<Tilemap>();
            wallTilemap.size = new Vector3Int(size, size, 0);

            wallTilemapChunk.AddComponent<TilemapRenderer>();

            wallRigidBody = wallTilemapChunk.AddComponent<Rigidbody2D>();
            wallRigidBody.bodyType = RigidbodyType2D.Kinematic;

            waterTilemapCollider = wallTilemapChunk.AddComponent<TilemapCollider2D>();
            waterTilemapCollider.usedByComposite = true;

            wallTilemapChunk.AddComponent<CompositeCollider2D>();

            wallTilemapChunk.transform.position = tilemapChunkParent.transform.position;
            wallTilemapChunk.transform.rotation = Quaternion.Euler(90, 0, 0);
            wallTilemapChunk.transform.parent = tilemapChunkParent.transform;
            wallTilemapChunk.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            SetVisible(false);

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            
            TilemapPainter.PaintTilemap(groundTilemap, mapData.heightMap, instance.tilemapBiomeData, TilemapType.Ground);
            TilemapPainter.PaintTilemap(waterTilemap, mapData.heightMap, instance.tilemapBiomeData, TilemapType.Water);
            TilemapPainter.PaintTilemap(wallTilemap, mapData.heightMap, instance.tilemapBiomeData, TilemapType.Wall);

            UpdateTerrainChunk();
        }

        // Shows or hides chunks of the map
        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool wasVisible = IsVisible();
                bool visible = viewerDistFromNearestEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold)
                            lodIndex = i + 1;
                        else
                            break; // viewerDistFromNearestEdge will be greater than maxViewDist at this point, so we mind as well break out of the loop
                    }
                }

                if (wasVisible != visible)
                {
                    if (visible)
                        visibleTerrainChunks.Add(this);
                    else
                        visibleTerrainChunks.Remove(this);

                    SetVisible(visible);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            tilemapChunkParent.SetActive(visible);
        }

        public bool IsVisible()
        {
            return tilemapChunkParent.activeSelf;
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        [Range(0, MeshGenerator.numSupportedLODs - 1)]
        public int lod;
        public float visibleDistThreshold;
    }
}