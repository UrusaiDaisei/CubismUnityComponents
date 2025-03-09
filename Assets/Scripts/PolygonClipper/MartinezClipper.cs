using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Martinez
{
    /// <summary>
    /// Implementation of the Martinez-Rueda-Feito polygon clipping algorithm.
    /// This class provides methods to compute Boolean operations (union, intersection, difference, XOR)
    /// between two sets of polygons.
    /// </summary>
    public partial class MartinezClipper
    {
        /// <summary>
        /// Computes a Boolean operation between two sets of polygons.
        /// </summary>
        /// <param name="subject">List of subject polygons.</param>
        /// <param name="clipping">List of clipping polygons.</param>
        /// <param name="operation">The type of Boolean operation to perform.</param>
        /// <returns>The resulting list of polygons after applying the operation.</returns>
        public List<Polygon> Compute(List<Polygon> subject, List<Polygon> clipping, ClipType operation)
        {
            List<Polygon> trivial = TrivialOperation(subject, clipping, operation);
            if (trivial != null)
                return trivial.Count == 0 ? null : trivial;

            Rect sbbox = Rect.MinMaxRect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            Rect cbbox = Rect.MinMaxRect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            var eventQueue = FillQueue(subject, clipping, ref sbbox, ref cbbox, operation);

            trivial = CompareBBoxes(subject, clipping, sbbox, cbbox, operation);
            if (trivial != null)
                return trivial.Count == 0 ? null : trivial;

            List<SweepEvent> sortedEvents = SubdivideSegments(eventQueue, sbbox, cbbox, operation);
            List<Contour> contours = ConnectEdges(sortedEvents);

            // Convert contours to polygons
            var ringsBuilder = new PolygonBuilder();
            List<Polygon> polygons = new List<Polygon>(); // outer List: List of PolygonSets
            for (int i = 0; i < contours.Count; i++)
            {
                Contour contour = contours[i];
                if (contour.isExterior && contour.points.Count > 0)
                {
                    // The exterior ring goes first, ensure it is CCW (Counter Clockwise) 
                    ringsBuilder.Clear();
                    if (contour.isClockwise)
                    {
                        ringsBuilder.CreateComponent(contour.points.Reverse<Vector2>());
                    }
                    else
                    {
                        ringsBuilder.CreateComponent(contour.points);
                    }

                    // Followed by holes if any, ensure they are CW (Clockwise) 
                    for (int j = 0; j < contour.holeIds.Count; j++)
                    {
                        int holeId = contour.holeIds[j];
                        Contour hole = contours[holeId];
                        if (hole.isClockwise)
                        {
                            ringsBuilder.CreateComponent(hole.points);
                        }
                        else
                        {
                            ringsBuilder.CreateComponent(hole.points.Reverse<Vector2>());
                        }
                    }
                    polygons.Add(ringsBuilder.Build());
                }
            }
            return polygons;
        }

        /// <summary>
        /// Performs a trivial operation check for cases where one or both polygon lists are empty.
        /// </summary>
        /// <param name="subject">List of subject polygons.</param>
        /// <param name="clipping">List of clipping polygons.</param>
        /// <param name="operation">The type of Boolean operation to perform.</param>
        /// <returns>Result polygons if a trivial operation was performed, otherwise null.</returns>
        List<Polygon> TrivialOperation(List<Polygon> subject, List<Polygon> clipping, ClipType operation)
        {
            if (subject.Count * clipping.Count != 0)
                return null;

            if (operation == ClipType.Intersection)
                return new List<Polygon>();
            if (operation == ClipType.Difference)
                return subject;

            //operation == ClipType.Union || operation == ClipType.Xor
            return (subject.Count == 0) ? clipping : subject;
        }

        /// <summary>
        /// Compares bounding boxes of subject and clipping polygons to perform a quick check.
        /// </summary>
        /// <param name="subject">List of subject polygons.</param>
        /// <param name="clipping">List of clipping polygons.</param>
        /// <param name="sbbox">Bounding box of the subject polygons.</param>
        /// <param name="cbbox">Bounding box of the clipping polygons.</param>
        /// <param name="operation">The type of Boolean operation to perform.</param>
        /// <returns>Result polygons if determined from bounding box check, otherwise null.</returns>
        List<Polygon> CompareBBoxes(List<Polygon> subject, List<Polygon> clipping, Rect sbbox, Rect cbbox, ClipType operation)
        {
            if (sbbox.Overlaps(cbbox))
                return null;

            if (operation == ClipType.Intersection)
                return new List<Polygon>();
            if (operation == ClipType.Difference)
                return subject;

            //operation == ClipType.Union || operation == ClipType.Xor
            List<Polygon> result = subject;
            result.AddRange(clipping);
            return result;
        }
    }
}