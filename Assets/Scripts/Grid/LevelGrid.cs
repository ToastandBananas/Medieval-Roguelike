using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Random = UnityEngine.Random;
using Pathfinding.Util;
using InteractableObjects;
using UnitSystem;
using Utilities;

namespace GridSystem
{
    public class LevelGrid : MonoBehaviour
    {
        public static LevelGrid Instance { get; private set; }

        // public event EventHandler OnAnyUnitMovedGridPosition;

        [SerializeField] BlockManager blockManager;
        static List<SingleNodeBlocker> unitSingleNodeBlockers = new List<SingleNodeBlocker>();
        static BlockManager.TraversalProvider defaultTraversalProvider;

        [SerializeField] LayerMask groundMask;

        static Dictionary<GridPosition, Unit> units = new Dictionary<GridPosition, Unit>();
        static Dictionary<GridPosition, Interactable> interactableObjects = new Dictionary<GridPosition, Interactable>();

        static List<GridPosition> gridPositionsList = new List<GridPosition>();
        static List<GridPosition> validGridPositionsList = new List<GridPosition>();

        static Vector3 collisionCheckOffset = new Vector3(0f, 0.025f, 0f);
        static Collider[] collisionsCheckArray;

        public static readonly float diaganolDistance = 1.4142f;
        public static readonly int gridSize = 1;

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
            defaultTraversalProvider = new BlockManager.TraversalProvider(blockManager, Pathfinding.BlockManager.BlockMode.OnlySelector, unitSingleNodeBlockers);
        }

        public static Vector3 SnapPosition(Vector3 position)
        {
            position.Set(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y * 100f) / 100f, Mathf.RoundToInt(position.z));
            return position;
        }

        public static void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
        {
            if (units.ContainsKey(gridPosition) && units.TryGetValue(gridPosition, out Unit unitValue) == unit)
                return;
            else
                units.Add(gridPosition, unit);
            // Debug.Log(unit.name + " added to position: " + gridPosition.ToString());
        }

        public static Unit GetUnitAtGridPosition(GridPosition gridPosition)
        {
            units.TryGetValue(gridPosition, out Unit unit);
            return unit;
        }

        public static Unit GetUnitAtPosition(Vector3 position)
        {
            position = SnapPosition(position);
            foreach (KeyValuePair<GridPosition, Unit> unit in units)
            {
                if (unit.Key == position)
                    return GetUnitAtGridPosition(unit.Key);
            }
            return null;
        }

        public static void RemoveUnitAtGridPosition(GridPosition gridPosition)
        {
            // units.TryGetValue(gridPosition, out Unit unit);
            // Debug.Log(unit.name + " removed from position: " + gridPosition.ToString());
            if (units.ContainsKey(gridPosition))
                units.Remove(gridPosition);
        }

        public static void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
        {
            RemoveUnitAtGridPosition(fromGridPosition);

            AddUnitAtGridPosition(toGridPosition, unit);

            // OnAnyUnitMovedGridPosition?.Invoke(this, EventArgs.Empty);
        }

        public static void AddInteractableAtGridPosition(GridPosition gridPosition, Interactable interactable)
        {
            interactableObjects.Add(gridPosition, interactable);
        }

        public static Interactable GetInteractableAtGridPosition(GridPosition gridPosition)
        {
            interactableObjects.TryGetValue(gridPosition, out Interactable interactable);
            return interactable;
        }

        public static void RemoveInteractableAtGridPosition(GridPosition gridPosition)
        {
            interactableObjects.Remove(gridPosition);
        }

        public static Interactable GetInteractableFromTransform(Transform transform)
        {
            foreach (KeyValuePair<GridPosition, Interactable> interactable in interactableObjects)
            {
                if (interactable.Value.transform == transform)
                    return interactable.Value;
            }
            return null;
        }

        public static GridPosition GetGridPosition(Vector3 worldPosition) => new GridPosition(worldPosition);

        public static Vector3 GetWorldPosition(GridPosition gridPosition) => new Vector3(gridPosition.x, gridPosition.y, gridPosition.z);

        public static bool IsValidGridPosition(GridPosition gridPosition)
        {
            LayerGridGraph layeredGridGraph = AstarPath.active.data.layerGridGraph;

            return gridPosition.x >= layeredGridGraph.center.x - (layeredGridGraph.width / 2) &&
                   gridPosition.z >= layeredGridGraph.center.z - (layeredGridGraph.depth / 2) &&
                   gridPosition.x < (layeredGridGraph.width / 2) + layeredGridGraph.center.x &&
                   gridPosition.z < (layeredGridGraph.depth / 2) + layeredGridGraph.center.z;
        }

        public static GridPosition FindNearestValidGridPosition(GridPosition startingGridPosition, Unit unit, float rangeToSearch)
        {
            GridPosition newGridPosition;
            validGridPositionsList.Clear();
            float boundsDimension = (rangeToSearch * 2) + 0.1f;

            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startingGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition gridPosition = new GridPosition((Vector3)nodes[i].position);

                if (gridPosition == startingGridPosition)
                    continue;

                if (GridPositionObstructed(gridPosition)) // Grid Position already occupied by another Unit
                    continue;

                collisionsCheckArray = Physics.OverlapSphere(gridPosition.WorldPosition + collisionCheckOffset, 0.01f, unit.unitActionHandler.MoveObstacleMask);
                if (collisionsCheckArray.Length > 0)
                    continue;

                validGridPositionsList.Add(gridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            newGridPosition = GetClosestGridPositionFromList(startingGridPosition, validGridPositionsList);
            return newGridPosition;
        }

        public static GridPosition GetClosestGridPositionFromList(GridPosition startingGridPosition, List<GridPosition> gridPositions)
        {
            GridPosition nearestGridPosition = startingGridPosition;
            float nearestGridPositionDist = 1000000;
            for (int i = 0; i < gridPositions.Count; i++)
            {
                float dist = Vector3.Distance(startingGridPosition.WorldPosition, gridPositions[i].WorldPosition);
                if (dist < nearestGridPositionDist)
                {
                    nearestGridPosition = gridPositions[i];
                    nearestGridPositionDist = dist;
                }
            }

            return nearestGridPosition;
        }

        public List<GridPosition> GetGridPositionsInRange(GridPosition startingGridPosition, Unit unit, float minRange, float maxRange, bool checkForObstacles = false)
        {
            validGridPositionsList.Clear();
            float boundsDimension = (maxRange * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startingGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (GridPositionObstructed(nodeGridPosition)) // Grid Position already occupied by another Unit or is otherwise unwalkable
                    continue;

                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startingGridPosition, nodeGridPosition);
                if (distance > maxRange || distance < minRange)
                    continue;

                if (checkForObstacles)
                {
                    float sphereCastRadius = 0.1f;
                    Vector3 shootDir = ((nodeGridPosition.WorldPosition + Vector3.up) - (startingGridPosition.WorldPosition + Vector3.up)).normalized;
                    if (Physics.SphereCast(startingGridPosition.WorldPosition + Vector3.up, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition + Vector3.up, startingGridPosition.WorldPosition + Vector3.up), unit.unitActionHandler.AttackObstacleMask))
                        continue; // Blocked by an obstacle
                }

                collisionsCheckArray = Physics.OverlapSphere(nodeGridPosition.WorldPosition + collisionCheckOffset, 0.01f, unit.unitActionHandler.MoveObstacleMask);
                if (collisionsCheckArray.Length > 0)
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            // GridSystemVisual.Instance.ShowGridPositionList(validGridPositionList, GridSystemVisual.GridVisualType.White);

            ListPool<GraphNode>.Release(nodes);
            if (validGridPositionsList.Count == 0)
                validGridPositionsList.Add(unit.GridPosition);
            return validGridPositionsList;
        }

        public GridPosition GetRandomGridPositionInRange(GridPosition startingGridPosition, Unit unit, float minRange, float maxRange, bool checkForObstacles = false)
        {
            gridPositionsList = GetGridPositionsInRange(startingGridPosition, unit, minRange, maxRange, checkForObstacles);
            if (gridPositionsList.Count == 0)
                return unit.GridPosition;
            return gridPositionsList[Random.Range(0, gridPositionsList.Count - 1)];
        }

        public GridPosition GetRandomFleeGridPosition(Unit unit, Unit enemyUnit, int minFleeDistance, int maxFleeDistance)
        {
            Vector3 unitWorldPosition = unit.WorldPosition;
            Vector3 enemyWorldPosition = enemyUnit.WorldPosition;
            float boundsDimension = (maxFleeDistance * 2) + 0.1f;

            validGridPositionsList.Clear();
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(enemyWorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                Vector3 dirToNode = (nodeGridPosition.WorldPosition - enemyWorldPosition).normalized;
                Vector3 dirToUnit = (unitWorldPosition - enemyWorldPosition).normalized;

                if (Mathf.Abs(dirToNode.x - dirToUnit.x) > 0.25f || Mathf.Abs(dirToNode.z - dirToUnit.z) > 0.25f)
                    continue;

                float distanceToEnemy = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(enemyUnit.GridPosition, nodeGridPosition);
                if (distanceToEnemy > maxFleeDistance || distanceToEnemy < minFleeDistance)
                    continue;

                if (GridPositionObstructed(nodeGridPosition)) // Grid Position already occupied by another Unit
                    continue;

                collisionsCheckArray = Physics.OverlapSphere(nodeGridPosition.WorldPosition + collisionCheckOffset, 0.01f, unit.unitActionHandler.MoveObstacleMask);
                if (collisionsCheckArray.Length > 0)
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            // GridSystemVisual.Instance.ShowGridPositionList(validGridPositionList, GridSystemVisual.GridVisualType.White);

            ListPool<GraphNode>.Release(nodes);
            if (validGridPositionsList.Count == 0)
                return unit.GridPosition;
            return validGridPositionsList[Random.Range(0, validGridPositionsList.Count - 1)];
        }

        public static List<GridPosition> GetSurroundingGridPositions(GridPosition startingGridPosition, float range, bool obstructedGridPositionsValid, bool startingGridPositionValid)
        {
            gridPositionsList.Clear();
            float boundsDimension = (range * 2f) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startingGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension)));
            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = GetGridPosition((Vector3)nodes[i].position);
                if (startingGridPositionValid == false && nodeGridPosition == startingGridPosition)
                    continue;

                if (obstructedGridPositionsValid == false && GridPositionObstructed(nodeGridPosition))
                    continue;

                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(startingGridPosition, nodeGridPosition) > range)
                    continue;

                gridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return gridPositionsList;
        }

        public static GridPosition GetNearestSurroundingGridPosition(GridPosition targetGridPosition, GridPosition unitGridPosition, float range, bool targetGridPositionValid)
        {
            validGridPositionsList = GetSurroundingGridPositions(targetGridPosition, range, false, targetGridPositionValid);
            GridPosition nearestGridPosition = targetGridPosition;
            float nearestDist = 1000000;
            for (int i = 0; i < validGridPositionsList.Count; i++)
            {
                float dist = Vector3.Distance(unitGridPosition.WorldPosition, validGridPositionsList[i].WorldPosition);
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
            if (Mathf.RoundToInt(startPosition.x) != Mathf.RoundToInt(endPosition.x) && Mathf.RoundToInt(startPosition.z) != Mathf.RoundToInt(endPosition.z))
                return true;
            return false;
        }

        public static bool GridPositionObstructed(GridPosition gridPosition)
        {
            GraphNode node = AstarPath.active.GetNearest(gridPosition.WorldPosition).node;
            if (IsValidGridPosition(gridPosition) == false || (HasAnyUnitOnGridPosition(gridPosition, out Unit unit) && unit.health.IsDead() == false) || UnitManager.player.singleNodeBlocker.manager.NodeContainsAnyOf(node, unitSingleNodeBlockers) || node.Walkable == false)
                return true;
            return false;
        }

        public static bool HasAnyUnitOnGridPosition(GridPosition gridPosition, out Unit unit) => units.TryGetValue(gridPosition, out unit);

        public static bool HasAnyInteractableOnGridPosition(GridPosition gridPosition) => interactableObjects.TryGetValue(gridPosition, out Interactable interactable);

        public static BlockManager BlockManager => Instance.blockManager;

        public static BlockManager.TraversalProvider DefaultTraversalProvider => defaultTraversalProvider;

        public static List<SingleNodeBlocker> UnitSingleNodeBlockerList => unitSingleNodeBlockers;

        public static void AddSingleNodeBlockerToList(SingleNodeBlocker singleNodeBlocker, List<SingleNodeBlocker> singleNodeBlockerList) => singleNodeBlockerList.Add(singleNodeBlocker);

        public static void RemoveSingleNodeBlockerFromList(SingleNodeBlocker singleNodeBlocker, List<SingleNodeBlocker> singleNodeBlockerList) => singleNodeBlockerList.Remove(singleNodeBlocker);

        public static LayerMask GroundMask => Instance.groundMask;
    }
}