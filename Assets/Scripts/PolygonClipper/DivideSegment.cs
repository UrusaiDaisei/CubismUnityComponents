using UnityEngine;

namespace Martinez
{

    public partial class MartinezClipper
    {
        // /** @brief Divide the segment associated to left event e, updating pq and (implicitly) the status line */
        void DivideSegment(SweepEvent se, Vector2 p, ref MinHeap<SweepEvent> queue)
        {
            if (p.x == se.point.x && p.y < se.point.y)
                p.x = p.x + Mathf.Epsilon;

            // "Left event" of the "right line segment" resulting from dividing e (the line segment associated to e)
            SweepEvent r = new SweepEvent(p, false, se, se.isSubject);
            SweepEvent l = new SweepEvent(p, true, se.otherEvent, se.isSubject);

            r.contourId = l.contourId = se.contourId;

            // avoid a rounding error. The left event would be processed after the right event
            if (CompareEvents.Default.Compare(l, se.otherEvent) > 0)
            {
                se.otherEvent.left = true;
                l.left = false;
            }
            se.otherEvent.otherEvent = l;
            se.otherEvent = r;

            queue.Push(l);
            queue.Push(r);
        }
    }
}

