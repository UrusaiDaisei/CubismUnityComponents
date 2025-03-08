using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public partial class MartinezClipper
    {
        List<Polygon> trivialOperation(List<Polygon> subject, List<Polygon> clipping, ClipType operation)
        {
            List<Polygon> result = null;
            if (subject.Count * clipping.Count == 0)
            {
                if (operation == ClipType.Intersection)
                    result = new List<Polygon>();
                else if (operation == ClipType.Difference)
                    result = subject;
                else if (operation == ClipType.Union || operation == ClipType.Xor)
                    result = (subject.Count == 0) ? clipping : subject;
            }
            return result;
        }
        List<Polygon> compareBBoxes(List<Polygon> subject, List<Polygon> clipping, Rect sbbox, Rect cbbox, ClipType operation)
        {
            List<Polygon> result = null;
            if (!sbbox.Overlaps(cbbox))
            {
                if (operation == ClipType.Intersection)
                    result = new List<Polygon>();
                else if (operation == ClipType.Difference)
                    result = subject;
                else if (operation == ClipType.Union || operation == ClipType.Xor)
                {
                    result = subject;
                    result.AddRange(clipping);
                }
            }
            return result;
        }
        public List<Polygon> Compute(List<Polygon> subject, List<Polygon> clipping, ClipType operation)
        {
            List<Polygon> trivial = trivialOperation(subject, clipping, operation);
            if (trivial != null)
                return trivial.Count == 0 ? null : trivial;

            Rect sbbox = Rect.MinMaxRect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            Rect cbbox = Rect.MinMaxRect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

            MinHeap<SweepEvent> eventQueue = fillQueue(subject, clipping, ref sbbox, ref cbbox, operation);

            trivial = compareBBoxes(subject, clipping, sbbox, cbbox, operation);
            if (trivial != null)
                return trivial.Count == 0 ? null : trivial;

            List<SweepEvent> sortedEvents = subdivideSegments(eventQueue, sbbox, cbbox, operation);
            List<Contour> contours = connectEdges(sortedEvents);

            // Convert contours to polygons
            List<Polygon> polygons = new List<Polygon>(); //outer List: List of PolygonSets
            for (int i = 0; i < contours.Count; i++)
            {
                Contour contour = contours[i];
                if (contour.isExterior && contour.points.Count > 0)
                {
                    // The exterior ring goes first, ensure it is CCW (Counter Clockwise) 
                    Polygon rings = new Polygon(contour.points.Count);
                    rings.AddComponent();
                    if (contour.isClockwise)
                    {
                        for (int k = contour.points.Count - 1; k >= 0; k--)
                            rings.nodes.Add(contour.points[k]);
                    }
                    else
                    {
                        for (int k = 0, length = contour.points.Count; k < length; k++)
                            rings.nodes.Add(contour.points[k]);
                    }

                    // Followed by holes if any, ensure they are CW (Clockwise) 
                    for (int j = 0; j < contour.holeIds.Count; j++)
                    {
                        rings.AddComponent();
                        int holeId = contour.holeIds[j];
                        Contour hole = contours[holeId];
                        if (hole.isClockwise)
                        {
                            for (int k = 0, length = hole.points.Count; k < length; k++)
                                rings.nodes.Add(hole.points[k]);
                        }
                        else
                        {
                            for (int k = hole.points.Count - 1; k >= 0; k--)
                                rings.nodes.Add(hole.points[k]);
                        }
                    }
                    rings.AddComponent();//abuse last StartID to store end of last component
                    polygons.Add(rings);
                }
            }
            return polygons;
        }

    }
}