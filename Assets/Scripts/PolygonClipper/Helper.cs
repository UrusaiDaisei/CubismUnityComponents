using System.Runtime.CompilerServices;
using UnityEngine;

namespace Martinez
{
    /// <summary>
    /// Provides utility methods for geometric operations.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Calculates the cross product of two 2D vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>The cross product value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float crossProduct(Vector2 a, Vector2 b)
        {
            return (a.x * b.y) - (a.y * b.x);
        }

        /// <summary>
        /// Checks if two points are approximately equal.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>True if the points are approximately equal, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately((a - b).sqrMagnitude, 0);
        }

        /// <summary>
        /// Tests if three points are collinear (lie on the same straight line).
        /// </summary>
        /// <param name="p0">First point.</param>
        /// <param name="p1">Second point.</param>
        /// <param name="p2">Third point.</param>
        /// <returns>True if the points are collinear, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollinear(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            // Points are collinear if the cross product is zero (or very close to zero)
            return Mathf.Approximately(crossProduct(p0 - p2, p1 - p2), 0f);
        }
    }
}
