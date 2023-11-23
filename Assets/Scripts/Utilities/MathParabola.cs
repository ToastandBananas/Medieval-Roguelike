using UnityEngine;
using System;
using GridSystem;

namespace Utilities
{
    public class MathParabola
    {
        public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float time)
        {
            Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

            var mid = Vector3.Lerp(start, end, time);

            return new Vector3(mid.x, f(time) + Mathf.Lerp(start.y, end.y, time), mid.z);
        }

        public static Vector2 Parabola(Vector2 start, Vector2 end, float height, float time)
        {
            Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

            var mid = Vector2.Lerp(start, end, time);

            return new Vector2(mid.x, f(time) + Mathf.Lerp(start.y, end.y, time));
        }

        public static float CalculateParabolaArcHeight(GridPosition startGridPosition, GridPosition targetGridPosition) => CalculateParabolaArcHeight(startGridPosition.WorldPosition, targetGridPosition.WorldPosition);

        public static float CalculateParabolaArcHeight(Vector3 startPosition, Vector3 targetPosition)
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            float arcHeightFactor = 0.1f;
            float arcHeight = distance * arcHeightFactor;

            float maxArcHeight = 3f;
            arcHeight = Mathf.Clamp(arcHeight, 0f, maxArcHeight);

            // Debug.Log("Arc Height: " + arcHeight);
            return arcHeight;
        }
    }
}
