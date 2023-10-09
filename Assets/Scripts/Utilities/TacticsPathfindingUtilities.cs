using UnityEngine;
using Pathfinding;
using GridSystem;

namespace Utilities
{
    public abstract class TacticsPathfindingUtilities
    {
        const int MOVE_STRAIGHT_COST = 10;
        const int MOVE_DIAGONAL_COST = 14;

        public static int CalculateDistance_XZ(GridPosition gridPositionA, GridPosition gridPositionB)
        {
            GridPosition gridPositionDistance = gridPositionA - gridPositionB;
            int xDistance = Mathf.Abs(gridPositionDistance.x);
            int zDistance = Mathf.Abs(gridPositionDistance.z);
            int remaining = Mathf.Abs(xDistance - zDistance);
            return (MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remaining);
        }

        public static float CalculateWorldSpaceDistance_XZ(GridPosition gridPositionA, GridPosition gridPositionB)
        {
            GridPosition gridPositionDistance = gridPositionA - gridPositionB;
            int xDistance = Mathf.Abs(gridPositionDistance.x);
            int zDistance = Mathf.Abs(gridPositionDistance.z);
            int remaining = Mathf.Abs(xDistance - zDistance);
            return ((MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remaining)) / 10f;
        }

        public static float CalculateWorldSpaceDistance_XYZ(GridPosition gridPositionA, GridPosition gridPositionB)
        {
            GridPosition gridPositionDistance = gridPositionA - gridPositionB;
            int xDistance = Mathf.Abs(gridPositionDistance.x);
            int zDistance = Mathf.Abs(gridPositionDistance.z);
            int remaining = Mathf.Abs(xDistance - zDistance);

            float yDistance = Mathf.Abs(gridPositionDistance.y);

            // Debug.Log("Distance: " + (((MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remaining)) + yDistance) / 10f);
            return ((MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remaining) + yDistance) / 10f;
        }

        public static int CalculateMoveDistanceFromPath_XZ(ABPath path)
        {
            int distance = 0;
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                if (Mathf.Approximately(path.vectorPath[i].x, path.vectorPath[i + 1].x) == false && Mathf.Approximately(path.vectorPath[i].z, path.vectorPath[i + 1].z) == false) // Diagonal movement
                    distance += MOVE_DIAGONAL_COST;
                else
                    distance += MOVE_STRAIGHT_COST;
            }

            // Debug.Log("Path Distance: " + distance);
            return distance;
        }

        public static float CalculateWorldSpaceMoveDistanceFromPath_XZ(ABPath path)
        {
            int distance = 0;
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                if (Mathf.Approximately(path.vectorPath[i].x, path.vectorPath[i + 1].x) == false && Mathf.Approximately(path.vectorPath[i].z, path.vectorPath[i + 1].z) == false) // Diagonal movement
                    distance += MOVE_DIAGONAL_COST;
                else
                    distance += MOVE_STRAIGHT_COST;
            }

            // Debug.Log("Path Distance: " + distance);
            return distance / 10f;
        }

        public static float CalculateWorldSpaceDistanceY(GridPosition gridPositionA, GridPosition gridPositionB) => Mathf.Abs(gridPositionA.y - gridPositionB.y);
    }
}
