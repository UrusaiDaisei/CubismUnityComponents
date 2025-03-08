using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public partial class MartinezClipper
    {
        /// <summary>
        /// Processes a possible intersection between the line segments associated with two sweep events.
        /// </summary>
        /// <param name="se1">First sweep event.</param>
        /// <param name="se2">Second sweep event.</param>
        /// <param name="queue">The priority queue for managing sweep events.</param>
        /// <returns>
        /// 0 if no intersection is found.
        /// 1 if an intersection is found at a point that is not an endpoint.
        /// 2 if the segments are equal or share the left endpoint.
        /// 3 if the segments share the right endpoint or one segment includes the other.
        /// </returns>
        int possibleIntersection(SweepEvent se1, SweepEvent se2, MinHeap<SweepEvent> queue)
        {
            // This code originally disallowed self-intersecting polygons,
            // but it cost half a day of debugging, so it was left out of respect
            // if (se1.isSubject === se2.isSubject) return;

            List<Vector2> inter = intersection(se1.point, se1.otherEvent.point, se2.point, se2.otherEvent.point, false);

            int nintersections = inter != null ? inter.Count : 0;
            if (nintersections == 0) return 0; // no intersection

            // The line segments intersect at an endpoint of both line segments
            if ((nintersections == 1) &&
                (Helper.Approximately(se1.point, se2.point) ||
                 Helper.Approximately(se1.otherEvent.point, se2.otherEvent.point)))
            {
                return 0;
            }

            if (nintersections == 2 && se1.isSubject == se2.isSubject)
            {
                // Edges of the same polygon might overlap in some cases
                // (e.g., "Edges of the same polygon overlap", involving se1.point, se1.otherEvent.point, 
                // se2.point, se2.otherEvent.point)
                return 0;
            }

            // The line segments associated to se1 and se2 intersect
            if (nintersections == 1)
            {
                // If the intersection point is not an endpoint of se1
                if (!Helper.Approximately(se1.point, (inter[0])) && !Helper.Approximately(se1.otherEvent.point, (inter[0])))
                    DivideSegment(se1, inter[0], ref queue);

                // If the intersection point is not an endpoint of se2
                if (!Helper.Approximately(se2.point, (inter[0])) && !Helper.Approximately(se2.otherEvent.point, (inter[0])))
                    DivideSegment(se2, inter[0], ref queue);
                return 1;
            }

            // The line segments associated to se1 and se2 overlap
            List<SweepEvent> events = new List<SweepEvent>();
            bool leftCoincide = false;
            bool rightCoincide = false;

            if (Helper.Approximately(se1.point, se2.point))
                leftCoincide = true; // linked
            else if (CompareEvents.Default.Compare(se1, se2) == 1)
            {
                events.Add(se2);
                events.Add(se1);
            }
            else
            {
                events.Add(se1);
                events.Add(se2);
            }

            if (Helper.Approximately(se1.otherEvent.point, se2.otherEvent.point))
                rightCoincide = true;
            else if (CompareEvents.Default.Compare(se1.otherEvent, se2.otherEvent) == 1)
            {
                events.Add(se2.otherEvent);
                events.Add(se1.otherEvent);
            }
            else
            {
                events.Add(se1.otherEvent);
                events.Add(se2.otherEvent);
            }

            if ((leftCoincide && rightCoincide) || leftCoincide)
            {
                // Both line segments are equal or share the left endpoint
                se2.type = EdgeType.NonContributing;
                se1.type = (se2.inOut == se1.inOut) ? EdgeType.SameTransition : EdgeType.DifferentTransition;

                if (leftCoincide && !rightCoincide)
                {
                    // This fixes the overlapping self-intersecting polygons issue
                    // (changing events selection from [2, 1] to [0, 1])
                    DivideSegment(events[1].otherEvent, events[0].point, ref queue);
                }
                return 2;
            }

            // The line segments share the right endpoint
            if (rightCoincide)
            {
                DivideSegment(events[0], events[1].point, ref queue);
                return 3;
            }

            // No line segment includes totally the other one
            if (events[0] != events[3].otherEvent)
            {
                DivideSegment(events[0], events[1].point, ref queue);
                DivideSegment(events[1], events[2].point, ref queue);
                return 3;
            }

            // One line segment includes the other one
            DivideSegment(events[0], events[1].point, ref queue);
            DivideSegment(events[3].otherEvent, events[2].point, ref queue);
            return 3;
        }
    }
}

