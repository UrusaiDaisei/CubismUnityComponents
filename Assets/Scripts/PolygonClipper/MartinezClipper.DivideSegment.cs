using UnityEngine;

namespace Martinez
{
    public partial class MartinezClipper
    {
        /// <summary>
        /// Divides the segment associated with the given sweep event at the specified point.
        /// This is used during the sweep line algorithm when intersections are detected.
        /// </summary>
        /// <param name="se">The sweep event whose segment should be divided.</param>
        /// <param name="p">The point at which to divide the segment.</param>
        /// <param name="queue">The priority queue to which the new events should be added.</param>
        void DivideSegment(SweepEvent se, Vector2 p, ref MinHeap<SweepEvent> queue)
        {
            // Minor adjustment to avoid numerical issues when a point lies directly below the segment endpoint
            if (p.x == se.point.x && p.y < se.point.y)
                p.x = p.x + Mathf.Epsilon;

            // Create new events for the division point:
            // "Left event" of the "right line segment" resulting from dividing the line segment
            SweepEvent r = new SweepEvent(p, false, se, se.isSubject);
            SweepEvent l = new SweepEvent(p, true, se.otherEvent, se.isSubject);

            // Maintain contour ID for both new events
            r.contourId = l.contourId = se.contourId;

            // Avoid a rounding error. The left event would be processed after the right event
            if (CompareEvents.Default.Compare(l, se.otherEvent) > 0)
            {
                se.otherEvent.left = true;
                l.left = false;
            }

            // Update the links between events
            se.otherEvent.otherEvent = l;
            se.otherEvent = r;

            // Add the new events to the queue
            queue.Push(l);
            queue.Push(r);
        }
    }
}

