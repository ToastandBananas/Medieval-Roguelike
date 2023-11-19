using UnityEngine;
using Pathfinding;
using GridSystem;

namespace Utilities
{
    public abstract class TacticsUtilities
    {
        const float MOVE_STRAIGHT_COST = 1f;
        const float MOVE_DIAGONAL_COST = 1.4f;

        public readonly static float diaganolDistance = LevelGrid.diaganolDistance;

        public static float CalculateHeightDifferenceToTarget(GridPosition startGridPosition, GridPosition targetGridPosition) => startGridPosition.y - targetGridPosition.y;

        public static float CalculateDistance_XZ(GridPosition gridPositionA, GridPosition gridPositionB)
        {
            GridPosition gridPositionDistance = gridPositionA - gridPositionB;
            int xDistance = Mathf.Abs(gridPositionDistance.x);
            int zDistance = Mathf.Abs(gridPositionDistance.z);
            int remaining = Mathf.Abs(xDistance - zDistance);
            return (MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remaining);
        }

        public static float CalculateDistance_XYZ(GridPosition gridPositionA, GridPosition gridPositionB)
        {
            GridPosition gridPositionDistance = gridPositionA - gridPositionB;
            int xDistance = Mathf.Abs(gridPositionDistance.x);
            float yDistance = Mathf.Abs(gridPositionDistance.y);
            int zDistance = Mathf.Abs(gridPositionDistance.z);
            int remainingXZ = Mathf.Abs(xDistance - zDistance);

            // Debug.Log($"Distance Between {gridPositionA} & {gridPositionB}: {(MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remainingXZ) + yDistance}");
            return (MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance)) + (MOVE_STRAIGHT_COST * remainingXZ) + yDistance;
        }

        public static float CalculateMoveDistanceFromPath_XZ(ABPath path)
        {
            float distance = 0f;
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

        public static float CalculateDistanceY(GridPosition gridPositionA, GridPosition gridPositionB) => Mathf.Abs(gridPositionA.y - gridPositionB.y); 
        
        public static float CalculateParabolaArcHeight(GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            float distanceXZ = CalculateDistance_XZ(startGridPosition, targetGridPosition);
            float distanceY = startGridPosition.y - targetGridPosition.y;
            float arcHeightFactor = 0.1f;

            float arcHeight = distanceXZ * arcHeightFactor;
            arcHeight += distanceY * arcHeightFactor;

            float maxArcHeight = 3f;
            arcHeight = Mathf.Clamp(arcHeight, 0f, maxArcHeight);

            // Debug.Log("Arc Height: " + arcHeight);
            return arcHeight;
        }

        public static float CalculateParabolaArcHeight(Vector3 startPosition, Vector3 targetPosition)
        {
            float distanceXYZ = Vector3.Distance(startPosition,targetPosition);
            float arcHeightFactor = 0.1f;
            float arcHeight = distanceXYZ * arcHeightFactor;

            float maxArcHeight = 3f;
            arcHeight = Mathf.Clamp(arcHeight, 0f, maxArcHeight);

            // Debug.Log("Arc Height: " + arcHeight);
            return arcHeight;
        }
    }
}
