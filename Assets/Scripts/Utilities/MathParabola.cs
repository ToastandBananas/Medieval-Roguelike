using UnityEngine;
using System;
using GridSystem;

namespace Utilities
{
    public class MathParabola
    {
        public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
        {
            Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

            var mid = Vector3.Lerp(start, end, t);

            return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
        }

        public static Vector2 Parabola(Vector2 start, Vector2 end, float height, float t)
        {
            Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

            var mid = Vector2.Lerp(start, end, t);

            return new Vector2(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t));
        }

        public static Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
        {
            float parabolicT = t * 2 - 1;
            if (Mathf.Abs(start.y - end.y) < 0.1f)
            {
                //start and end are roughly level, pretend they are - simpler solution with less steps
                Vector3 travelDirection = end - start;
                Vector3 result = start + t * travelDirection;
                result.y += (-parabolicT * parabolicT + 1) * height;
                return result;
            }
            else
            {
                //start and end are not level, gets more complicated
                Vector3 travelDirection = end - start;
                Vector3 levelDirection = end - new Vector3(start.x, end.y, start.z);
                Vector3 right = Vector3.Cross(travelDirection, levelDirection);
                Vector3 up = Vector3.Cross(right, levelDirection);

                if (end.y > start.y) up = -up;
                Vector3 result = start + t * travelDirection;
                result += (-parabolicT * parabolicT + 1) * height * up.normalized;
                return result;
            }
        }

        public static float CalculateParabolaArcHeight(GridPosition startGridPosition, GridPosition targetGridPosition, float arcMultiplier) => CalculateParabolaArcHeight(startGridPosition.WorldPosition, targetGridPosition.WorldPosition, arcMultiplier);

        public static float CalculateParabolaArcHeight(Vector3 startPosition, Vector3 targetPosition, float arcMultiplier)
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            float arcHeightFactor = 0.1f;
            float arcHeight = distance * arcHeightFactor * arcMultiplier;

            float maxArcHeight = 3f;
            arcHeight = Mathf.Clamp(arcHeight, 0f, maxArcHeight);

            // Debug.Log("Arc Height: " + arcHeight);
            return arcHeight;
        }
    }
}
