using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

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

    public GridPosition FindNearestValidGridPosition(GridPosition startingGridPosition, Unit unit)
    {
        GridPosition newGridPosition = startingGridPosition;
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        ConstantPath path = ConstantPath.Construct(startingGridPosition.WorldPosition(), 1001);
        path.traversalProvider = DefaultTraversalProvider();

        // Schedule the path for calculation
        unit.unitActionHandler.GetAction<MoveAction>().seeker.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition gridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (IsValidGridPosition(gridPosition) == false)
                continue;

            Collider[] collisions = Physics.OverlapSphere(gridPosition.WorldPosition() + new Vector3(0f, 0.025f, 0f), 0.01f, unit.unitActionHandler.actionsObstacleMask);
            if (collisions.Length > 0)
                continue;

            if (HasAnyUnitOnGridPosition(gridPosition)) // Grid Position already occupied by another Unit
                continue;

            // Debug.Log(gridPosition);
            validGridPositionList.Add(gridPosition);
        }

        if (validGridPositionList.Count > 0)
            newGridPosition = validGridPositionList[0];

        Debug.Log(newGridPosition);

        return newGridPosition;
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
