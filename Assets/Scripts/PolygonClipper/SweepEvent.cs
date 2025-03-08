using UnityEngine;

namespace Martinez
{
    public class SweepEvent
    {
        public AVLNode<SweepEvent> positionInSweepLine;
        public Vector2 point;
        public bool left;                       // Is left endpoint?
        public SweepEvent otherEvent;           // Other edge reference
        public bool isSubject;                  //Belongs to source or clipping polygon
        public EdgeType type;               //Edge contribution type       
        public bool inOut;                      //In-out transition for the sweepline crossing polygon
        public bool otherInOut;                 // a vertical ray from (p.x, -infinite) that crosses the edge
        public SweepEvent prevInResult;         //Previous event in result?
        public int resultTransition;            //Type of result transition (0 = not in result, +1 = out-in, -1, in-out)
        public bool inside;                     // Is the edge inside of another polygon
        public int otherPos;
        public int contourId;

        /// <summary>
        /// Sweepline event
        /// </summary>
        /// <param name="point"></param>
        /// <param name="left"></param>
        /// <param name="otherEvent"></param>
        /// <param name="isSubject"></param>
        /// <param name="edgeType"></param>
        public SweepEvent(Vector2 point, bool left, SweepEvent otherEvent, bool isSubject, EdgeType edgeType = EdgeType.NORMAL)
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

        public bool IsBelow(Vector2 p)
        {
            return left
                ? Helper.crossProduct(point - p, otherEvent.point - p) > 0  // Direct cross product check
                : Helper.crossProduct(otherEvent.point - p, point - p) > 0; // Direct cross product check
        }
        public bool IsAbove(Vector2 p)
        {
            return !IsBelow(p);
        }
        public bool IsVertical()
        {
            return Mathf.Approximately(point.x, otherEvent.point.x);
        }
        // Does event belong to result?
        public bool inResult
        {
            get { return resultTransition != 0; }
        }
    };
}
