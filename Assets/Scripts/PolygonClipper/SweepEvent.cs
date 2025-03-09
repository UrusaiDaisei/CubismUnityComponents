using UnityEngine;

namespace Martinez
{

    partial class MartinezClipper
    {

        /// <summary>
        /// Represents an event in the sweep line algorithm used for polygon clipping.
        /// </summary>
        private sealed class SweepEvent
        {
            /// <summary>
            /// Position of this event in the sweep line data structure.
            /// </summary>
            public AVLTree<SweepEvent>.INode positionInSweepLine;

            /// <summary>
            /// The 2D point associated with this event.
            /// </summary>
            public Vector2 point;

            /// <summary>
            /// Indicates whether this event represents the left endpoint of a segment.
            /// </summary>
            public bool left;

            /// <summary>
            /// Reference to the corresponding event at the other end of the segment.
            /// </summary>
            public SweepEvent otherEvent;

            /// <summary>
            /// Indicates whether this event belongs to the subject polygon (true) or the clipping polygon (false).
            /// </summary>
            public bool isSubject;

            /// <summary>
            /// The type of edge contribution to the result.
            /// </summary>
            public EdgeType type;

            /// <summary>
            /// In-out transition flag for the sweepline crossing the polygon.
            /// </summary>
            public bool inOut;

            /// <summary>
            /// In-out transition flag for a vertical ray from (p.x, -infinite) that crosses the edge.
            /// </summary>
            public bool otherInOut;

            /// <summary>
            /// Reference to the previous event in the result.
            /// </summary>
            public SweepEvent prevInResult;

            /// <summary>
            /// Type of result transition (0 = not in result, +1 = out-in, -1 = in-out).
            /// </summary>
            public int resultTransition;

            /// <summary>
            /// Position index of the other event.
            /// </summary>
            public int otherPos;

            /// <summary>
            /// ID of the contour this event belongs to.
            /// </summary>
            public int contourId;

            /// <summary>
            /// Initializes a new sweep event.
            /// </summary>
            /// <param name="point">The 2D point associated with this event.</param>
            /// <param name="left">Indicates whether this is a left endpoint.</param>
            /// <param name="otherEvent">Reference to the corresponding event at the other end of the segment.</param>
            /// <param name="isSubject">Indicates whether this event belongs to the subject polygon.</param>
            /// <param name="edgeType">The type of edge contribution.</param>
            public SweepEvent(Vector2 point, bool left, SweepEvent otherEvent, bool isSubject, EdgeType edgeType = EdgeType.Normal)
            {
                this.point = point;
                this.left = left;
                this.isSubject = isSubject;
                this.otherEvent = otherEvent;
                this.type = edgeType;
                this.inOut = false;
                this.otherInOut = false;
                this.prevInResult = null;
                this.resultTransition = 0;
                this.otherPos = -1;
                this.contourId = -1;
                this.positionInSweepLine = null;
            }

            /// <summary>
            /// Determines if the edge this event belongs to is below a given point.
            /// </summary>
            /// <param name="p">The reference point.</param>
            /// <returns>True if the edge is below the point; otherwise, false.</returns>
            public bool IsBelow(Vector2 p)
            {
                return left
                    ? Helper.crossProduct(point - p, otherEvent.point - p) > 0  // Direct cross product check
                    : Helper.crossProduct(otherEvent.point - p, point - p) > 0; // Direct cross product check
            }

            /// <summary>
            /// Determines if the edge this event belongs to is above a given point.
            /// </summary>
            /// <param name="p">The reference point.</param>
            /// <returns>True if the edge is above the point; otherwise, false.</returns>
            public bool IsAbove(Vector2 p)
            {
                return !IsBelow(p);
            }

            /// <summary>
            /// Determines if the segment this event belongs to is vertical.
            /// </summary>
            /// <returns>True if the segment is vertical; otherwise, false.</returns>
            public bool IsVertical()
            {
                return Mathf.Approximately(point.x, otherEvent.point.x);
            }

            /// <summary>
            /// Gets whether this event belongs to the result.
            /// </summary>
            public bool inResult
            {
                get { return resultTransition != 0; }
            }
        };

    }
}
