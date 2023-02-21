using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Random = UnityEngine.Random;
using Pathfinding.Util;

public class LevelGrid : MonoBehaviour
{
    public static LevelGrid Instance { get; private set; }

    public event EventHandler OnAnyUnitMovedGridPosition;

    [SerializeField] BlockManager blockManager;
    List<SingleNodeBlocker> unitSingleNodeBlockers = new List<SingleNodeBlocker>();
    BlockManager.TraversalProvider defaultTraversalProvider;

    [SerializeField] float gridSize = 1f;

    Dictionary<GridPosition, Unit> units = new Dictionary<GridPosition, Unit>();
    Dictionary<GridPosition, Unit> deadUnits = new Dictionary<GridPosition, Unit>();
    Dictionary<GridPosition, Interactable> interactableObjects = new Dictionary<GridPosition, Interactable>();
    // public static Dictionary<Vector3, List<ItemData>> itemDatas = new Dictionary<Vector3, List<ItemData>>();

    List<GridPosition> gridPositionsList = new List<GridPosition>();
    List<GridPosition> validGridPositionsList = new List<GridPosition>();

    Vector3 collisionCheckOffset = new Vector3(0f, 0.025f, 0f);
    Collider[] collisionsCheckArray;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one LevelGrid! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Create a traversal provider which says that a path should be blocked by all the SingleNodeBlockers in the unitSingleNodeBlockers array
        defaultTraversalProvider = new BlockManager.TraversalProvider(blockManager, BlockManager.BlockMode.OnlySelector, unitSingleNodeBlockers);
    }

    public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        if (units.ContainsKey(gridPosition) && units.TryGetValue(gridPosition, out Unit unitValue) == unit)
            return;
        else
            units.Add(gridPosition, unit);
        // Debug.Log(unit.name + " added to position: " + gridPosition.ToString());
    }

    public Unit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        units.TryGetValue(gridPosition, out Unit unit);
        return unit;
    }

    public void RemoveUnitAtGridPosition(GridPosition gridPosition)
    {
        // units.TryGetValue(gridPosition, out Unit unit);
        // Debug.Log(unit.name + " removed from position: " + gridPosition.ToString());
        if (units.ContainsKey(gridPosition))
            units.Remove(gridPosition);
    }
    
    public void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
    {
        RemoveUnitAtGridPosition(fromGridPosition);

        AddUnitAtGridPosition(toGridPosition, unit);

        OnAnyUnitMovedGridPosition?.Invoke(this, EventArgs.Empty);
    }

    public void AddDeadUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        deadUnits.Add(gridPosition, unit);
    }

    public Unit GetDeadUnitAtGridPosition(GridPosition gridPosition)
    {
        deadUnits.TryGetValue(gridPosition, out Unit unit);
        return unit;
    }

    public void RemoveDeadUnitAtGridPosition(GridPosition gridPosition)
    {
        deadUnits.Remove(gridPosition);
    }

    public void DeadUnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
    {
        RemoveDeadUnitAtGridPosition(fromGridPosition);

        AddDeadUnitAtGridPosition(toGridPosition, unit);
    }

    public void AddInteractableAtGridPosition(GridPosition gridPosition, Interactable interactable)
    {
        interactableObjects.Add(gridPosition, interactable);
        // Debug.Log(interactable.name + " added to position: " + gridPosition.ToString());
    }

    public Interactable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        interactableObjects.TryGetValue(gridPosition, out Interactable interactable);
        return interactable;
    }

    public void RemoveInteractableAtGridPosition(GridPosition gridPosition)
    {
        // interactableObjects.TryGetValue(gridPosition, out Interactable interactable);
        // Debug.Log(interactable.name + " removed from position: " + gridPosition.ToString());
        interactableObjects.Remove(gridPosition);
    }

    public GridPosition GetGridPosition(Vector3 worldPosition) => new GridPosition(worldPosition);

    public Vector3 GetWorldPosition(GridPosition gridPosition) => new Vector3(gridPosition.x, gridPosition.y, gridPosition.z);

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        LayerGridGraph layeredGridGraph = AstarPath.active.data.layerGridGraph;

        return gridPosition.x >= layeredGridGraph.center.x - (layeredGridGraph.width / 2) &&
               gridPosition.z >= layeredGridGraph.center.z - (layeredGridGraph.depth / 2) &&
               gridPosition.x < (layeredGridGraph.width / 2) + layeredGridGraph.center.x &&
               gridPosition.z < (layeredGridGraph.depth / 2) + layeredGridGraph.center.z;
    }

    public GridPosition FindNearestValidGridPosition(GridPosition startingGridPosition, Unit unit, float rangeToSearch)
    {
        GridPosition newGridPosition;
        validGridPositionsList.Clear();

        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startingGridPosition.WorldPosition(), new Vector3((rangeToSearch * 2) + 0.1f, (rangeToSearch * 2) + 0.1f, (rangeToSearch * 2) + 0.1f)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition gridPosition = new GridPosition((Vector3)nodes[i].position);

            if (gridPosition == startingGridPosition)
                continue;

            if (GridPositionObstructed(gridPosition)) // Grid Position already occupied by another Unit
                continue;

            collisionsCheckArray = Physics.OverlapSphere(gridPosition.WorldPosition() + collisionCheckOffset, 0.01f, unit.unitActionHandler.GetAction<MoveAction>().MoveObstaclesMask());
            if (collisionsCheckArray.Length > 0)
                continue;

            validGridPositionsList.Add(gridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        newGridPosition = GetClosestGridPositionFromList(startingGridPosition, validGridPositionsList);
        return newGridPosition;
    }

    public GridPosition GetClosestGridPositionFromList(GridPosition startingGridPosition, List<GridPosition> gridPositions)
    {
        GridPosition nearestGridPosition = startingGridPosition;
        float nearestGridPositionDist = 1000000;
        for (int i = 0; i < gridPositions.Count; i++)
        {
            float dist = Vector3.Distance(startingGridPosition.WorldPosition(), gridPositions[i].WorldPosition());
            if (dist < nearestGridPositionDist)
            {
                nearestGridPosition = gridPositions[i];
                nearestGridPositionDist = dist;
            }
        }

        return nearestGridPosition;
    }

    public List<GridPosition> GetGridPositionsInRange(GridPosition startingGridPosition, Unit unit, int minRange, int maxRange)
    {
        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startingGridPosition.WorldPosition(), new Vector3((maxRange * 2) + 0.1f, (maxRange * 2) + 0.1f, (maxRange * 2) + 0.1f)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (GridPositionObstructed(nodeGridPosition)) // Grid Position already occupied by another Unit
                continue;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startingGridPosition, nodeGridPosition);
            if (distance > maxRange || distance < minRange)
                continue;

            collisionsCheckArray = Physics.OverlapSphere(nodeGridPosition.WorldPosition() + collisionCheckOffset, 0.01f, unit.unitActionHandler.GetAction<MoveAction>().MoveObstaclesMask());
            if (collisionsCheckArray.Length > 0)
                continue;

            validGridPositionsList.Add(nodeGridPosition);
        }

        // GridSystemVisual.Instance.ShowGridPositionList(validGridPositionList, GridSystemVisual.GridVisualType.White);

        ListPool<GraphNode>.Release(nodes);
        if (validGridPositionsList.Count == 0)
            validGridPositionsList.Add(unit.gridPosition);
        return validGridPositionsList;
    }

    public GridPosition GetRandomGridPositionInRange(GridPosition startingGridPosition, Unit unit, int minRange, int maxRange)
    {
        gridPositionsList = GetGridPositionsInRange(startingGridPosition, unit, minRange, maxRange);
        if (gridPositionsList.Count == 0)
            return unit.gridPosition;
        return gridPositionsList[Random.Range(0, gridPositionsList.Count - 1)];
    }

    public GridPosition GetRandomFleeGridPosition(Unit unit, Unit enemyUnit, int minFleeDistance, int maxFleeDistance)
    {
        Vector3 unitWorldPosition = unit.WorldPosition();
        Vector3 enemyWorldPosition = enemyUnit.WorldPosition();

        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(enemyWorldPosition, new Vector3((maxFleeDistance * 2) + 0.1f, (maxFleeDistance * 2) + 0.1f, (maxFleeDistance * 2) + 0.1f)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            Vector3 dirToNode = (nodeGridPosition.WorldPosition() - enemyWorldPosition).normalized;
            Vector3 dirToUnit = (unitWorldPosition - enemyWorldPosition).normalized;

            if (Mathf.Abs(dirToNode.x - dirToUnit.x) > 0.25f || Mathf.Abs(dirToNode.z - dirToUnit.z) > 0.25f)
                continue;

            float distanceToEnemy = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(enemyUnit.gridPosition, nodeGridPosition);
            if (distanceToEnemy > maxFleeDistance || distanceToEnemy < minFleeDistance)
                continue;

            if (GridPositionObstructed(nodeGridPosition)) // Grid Position already occupied by another Unit
                continue;

            collisionsCheckArray = Physics.OverlapSphere(nodeGridPosition.WorldPosition() + collisionCheckOffset, 0.01f, unit.unitActionHandler.GetAction<MoveAction>().MoveObstaclesMask());
            if (collisionsCheckArray.Length > 0)
                continue;

            validGridPositionsList.Add(nodeGridPosition);
        }

        // GridSystemVisual.Instance.ShowGridPositionList(validGridPositionList, GridSystemVisual.GridVisualType.White);

        ListPool<GraphNode>.Release(nodes);
        if (validGridPositionsList.Count == 0)
            return unit.gridPosition;
        return validGridPositionsList[Random.Range(0, validGridPositionsList.Count - 1)];
    }

    public List<GridPosition> GetSurroundingGridPositions(GridPosition startingGridPosition)
    {
        gridPositionsList.Clear();
        gridPositionsList.Add(startingGridPosition + new GridPosition(0, 0, 1)); // North
        gridPositionsList.Add(startingGridPosition + new GridPosition(0, 0, -1)); // South
        gridPositionsList.Add(startingGridPosition + new GridPosition(1, 0, 0)); // East
        gridPositionsList.Add(startingGridPosition + new GridPosition(-1, 0, 0)); // West
        gridPositionsList.Add(startingGridPosition + new GridPosition(-1, 0, 1)); // NorthWest
        gridPositionsList.Add(startingGridPosition + new GridPosition(1, 0, 1)); // NorthEast
        gridPositionsList.Add(startingGridPosition + new GridPosition(-1, 0, -1)); // SouthWest
        gridPositionsList.Add(startingGridPosition + new GridPosition(1, 0, -1)); // SouthEast
        return gridPositionsList;
    }

    public GridPosition GetNearestSurroundingGridPosition(GridPosition targetGridPosition, GridPosition unitGridPosition)
    {
        validGridPositionsList = GetSurroundingGridPositions(targetGridPosition);
        GridPosition nearestGridPosition = targetGridPosition;
        float nearestDist = 1000000;
        for (int i = 0; i < validGridPositionsList.Count; i++)
        {
            if (GridPositionObstructed(validGridPositionsList[i]))
                continue;

            float dist = Vector3.Distance(unitGridPosition.WorldPosition(), validGridPositionsList[i].WorldPosition());
            if (dist < nearestDist)
            {
                nearestGridPosition = validGridPositionsList[i];
                nearestDist = dist;
            }
        }
        return nearestGridPosition;
    }

    public static bool IsDiagonal(GridPosition startGridPosition, GridPosition endGridPosition)
    {
        if (Mathf.RoundToInt(startGridPosition.x) != Mathf.RoundToInt(endGridPosition.x) && Mathf.RoundToInt(startGridPosition.z) != Mathf.RoundToInt(endGridPosition.z))
            return true;
        return false;
    }

    public static bool IsDiagonal(Vector3 startPosition, Vector3 endPosition)
    {
        //if (TurnManager.Instance.IsPlayerTurn())
            //Debug.Log("Start: " + startPosition + " / " + "End: " + endPosition);
        if (Mathf.RoundToInt(startPosition.x) != Mathf.RoundToInt(endPosition.x) && Mathf.RoundToInt(startPosition.z) != Mathf.RoundToInt(endPosition.z))
            return true;
        return false;
    }

    public bool GridPositionObstructed(GridPosition gridPosition)
    {
        if (IsValidGridPosition(gridPosition) == false || HasAnyUnitOnGridPosition(gridPosition) || UnitManager.Instance.player.singleNodeBlocker.manager.NodeContainsAnyOf(AstarPath.active.GetNearest(gridPosition.WorldPosition()).node, unitSingleNodeBlockers))
            return true;
        return false;
    }

    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition) => units.TryGetValue(gridPosition, out Unit unit);

    public bool HasAnyDeadUnitOnGridPosition(GridPosition gridPosition) => deadUnits.TryGetValue(gridPosition, out Unit unit);

    public bool HasAnyInteractableOnGridPosition(GridPosition gridPosition) => interactableObjects.TryGetValue(gridPosition, out Interactable interactable);

    public BlockManager GetBlockManager() => blockManager;

    public BlockManager.TraversalProvider DefaultTraversalProvider() => defaultTraversalProvider;

    public List<SingleNodeBlocker> GetUnitSingleNodeBlockerList() => unitSingleNodeBlockers;

    public void AddSingleNodeBlockerToList(SingleNodeBlocker singleNodeBlocker, List<SingleNodeBlocker> singleNodeBlockerList) => singleNodeBlockerList.Add(singleNodeBlocker);

    public void RemoveSingleNodeBlockerFromList(SingleNodeBlocker singleNodeBlocker, List<SingleNodeBlocker> singleNodeBlockerList) => singleNodeBlockerList.Remove(singleNodeBlocker);

    public float GridSize() => gridSize;
}
