using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public sealed class CompareEvents : IComparer<SweepEvent>
    {
        public static readonly CompareEvents Default = new CompareEvents();
        private CompareEvents() { }
        public int Compare(SweepEvent e1, SweepEvent e2)
        {
            Vector2 p1 = e1.point;
            Vector2 p2 = e2.point;

            // Different x-coordinate
            if (p1.x > p2.x) return 1;
            if (p1.x < p2.x) return -1;

            // Different points, but same x-coordinate
            // Event with lower y-coordinate is processed first
            if (!(p1.y == p2.y)) return p1.y > p2.y ? 1 : -1; //exact equality test is needed here!
            return specialCases(e1, e2, p1, p2);
        }

        private static int specialCases(SweepEvent e1, SweepEvent e2, Vector2 p1, Vector2 p2)
        {
            // Same coordinates, but one is a left endpoint and the other is
            // a right endpoint. The right endpoint is processed first
            if (e1.left != e2.left)
                return e1.left ? 1 : -1;

            // const p2 = e1.otherEvent.point, p3 = e2.otherEvent.point;
            // const sa = (p1[0] - p3[0]) * (p2[1] - p3[1]) - (p2[0] - p3[0]) * (p1[1] - p3[1])
            // Same coordinates, both events
            // are left endpoints or right endpoints.
            // Check if the segments are collinear
            if (!Helper.IsCollinear(p1, e1.otherEvent.point, e2.otherEvent.point))
            {
                // Segments are not collinear
                // The event associated with the bottom segment is processed first
                return (!e1.IsBelow(e2.otherEvent.point)) ? 1 : -1;
            }
            if (e1.isSubject != e2.isSubject)
                return (!e1.isSubject && e2.isSubject) ? 1 : -1;
            return 0;
        }
    }
}
