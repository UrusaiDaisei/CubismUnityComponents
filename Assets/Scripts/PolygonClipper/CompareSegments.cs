using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    /// <summary>
    /// Comparer class for line segments in the sweep line algorithm.
    /// </summary>
    public sealed class CompareSegments : IComparer<SweepEvent>
    {
        /// <summary>
        /// Singleton instance of the comparer.
        /// </summary>
        public static readonly CompareSegments Default = new CompareSegments();

        private CompareSegments() { }

        /// <summary>
        /// Compares two line segments based on their position.
        /// </summary>
        /// <param name="le1">First sweep event (left endpoint).</param>
        /// <param name="le2">Second sweep event (left endpoint).</param>
        /// <returns>
        /// Negative value if le1 is less than le2, zero if they are equal,
        /// and positive value if le1 is greater than le2.
        /// </returns>
        public int Compare(SweepEvent le1, SweepEvent le2)
        {
            if (le1 == le2) return 0;

            // Check if segments are collinear
            if (!Helper.IsCollinear(le1.point, le1.otherEvent.point, le2.point) ||
                !Helper.IsCollinear(le1.point, le1.otherEvent.point, le2.otherEvent.point))
            {
                // Segments are not collinear

                // If they share their left endpoint use the right endpoint to sort
                if (Helper.Approximately(le1.point, le2.point)) return le1.IsBelow(le2.otherEvent.point) ? -1 : 1;

                // Different left endpoint: use the left endpoint to sort
                if (Mathf.Approximately(le1.point.x, le2.point.x)) return le1.point.y < le2.point.y ? -1 : 1;

                // has the line segment associated to e1 been inserted
                // into S after the line segment associated to e2 ?
                if (CompareEvents.Default.Compare(le1, le2) == 1) return le2.IsAbove(le1.point) ? -1 : 1;

                // The line segment associated to e2 has been inserted
                // into S after the line segment associated to e1
                return le1.IsBelow(le2.point) ? -1 : 1;
            }

            if (le1.isSubject == le2.isSubject) // same polygon
            {
                Vector2 p1 = le1.point;
                Vector2 p2 = le2.point;
                if (p1.x == p2.x && p1.y == p2.y) // use exact comparison here!
                {
                    p1 = le1.otherEvent.point;
                    p2 = le2.otherEvent.point;
                    if (p1.x == p2.x && p1.y == p2.y) return 0; // use exact comparison here!
                    else return le1.contourId > le2.contourId ? 1 : -1;
                }
            }
            else
            {
                // Segments are collinear, but belong to separate polygons
                return le1.isSubject ? -1 : 1;
            }

            return CompareEvents.Default.Compare(le1, le2) == 1 ? 1 : -1;
        }
    }
}
