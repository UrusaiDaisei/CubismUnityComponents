using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public partial class MartinezClipper
    {
        /// <summary>
        /// Processes a polygon contour or hole, creating sweep events for each edge.
        /// </summary>
        /// <param name="contourOrHole">List of points defining the contour or hole.</param>
        /// <param name="isSubject">True if this polygon is from the subject set, false if from the clipping set.</param>
        /// <param name="depth">The depth/contour ID of the polygon.</param>
        /// <param name="Q">Priority queue to store the created events.</param>
        /// <param name="bbox">Bounding box to be updated with the extents of this polygon.</param>
        static void ProcessPolygon(Span<Vector2> contourOrHole, bool isSubject, int depth, ref MinHeap<SweepEvent> Q, ref Rect bbox)
        {
            for (int i = 0, len = contourOrHole.Length - 1; i < len; i++)
            {
                var s1 = contourOrHole[i];
                var s2 = contourOrHole[i + 1];
                var e1 = new SweepEvent(s1, false, null, isSubject);
                var e2 = new SweepEvent(s2, false, e1, isSubject);
                e1.otherEvent = e2;

                // Skip collapsed edges, or it breaks
                if (Helper.Approximately(s1, s2))
                    continue;

                e1.contourId = e2.contourId = depth;

                if (CompareEvents.Default.Compare(e1, e2) > 0)
                    e2.left = true;
                else
                    e1.left = true;

                bbox.min = Vector2.Min(bbox.min, s1);
                bbox.max = Vector2.Max(bbox.max, s1);

                // Pushing events so the queue is sorted from left to right,
                // with objects on the left having the highest priority.
                Q.Push(e1);
                Q.Push(e2);
            }
        }

        /// <summary>
        /// Fills a priority queue with sweep events from both the subject and clipping polygons.
        /// </summary>
        /// <param name="subject">List of subject polygons.</param>
        /// <param name="clipping">List of clipping polygons.</param>
        /// <param name="sbbox">Output parameter for the subject polygons' bounding box.</param>
        /// <param name="cbbox">Output parameter for the clipping polygons' bounding box.</param>
        /// <param name="operation">The type of Boolean operation to perform.</param>
        /// <returns>A priority queue containing all sweep events.</returns>
        MinHeap<SweepEvent> FillQueue(List<Polygon> subject, List<Polygon> clipping, ref Rect sbbox, ref Rect cbbox, ClipType operation)
        {
            MinHeap<SweepEvent> eventQueue = new MinHeap<SweepEvent>(CompareEvents.Default);

            // Process subject polygons
            var contourId = processPolygonList(subject, true, 0, ref eventQueue, ref sbbox, operation);

            // Process clipping polygons
            processPolygonList(clipping, false, contourId, ref eventQueue, ref cbbox, operation);

            return eventQueue;

            // Local function to process polygon lists
            static int processPolygonList(List<Polygon> polygons, bool isSubject, int contourId,
                ref MinHeap<SweepEvent> eventQueue, ref Rect bbox, ClipType operation)
            {
                for (int i = 0; i < polygons.Count; i++)
                {
                    var polygonSet = polygons[i];
                    for (int j = 0; j < polygonSet.startIDs.Count - 1; j++)
                    {
                        bool isExteriorRing = j == 0;
                        if (!isSubject && operation == ClipType.Difference)
                            isExteriorRing = false;

                        if (isExteriorRing)
                            contourId++;
                        int start = polygonSet.startIDs[j];
                        int end = polygonSet.startIDs[j + 1];

                        var maxSize = end - start + 1;
                        var rentedArray = ArrayPool<Vector2>.Shared.Rent(maxSize);
                        //var component = rentedArray.AsSpan(0, maxSize);
                        polygonSet.nodes.CopyTo(start, rentedArray, 0, end - start);
                        // Close the ring so intersection between end and start are detected
                        rentedArray[maxSize - 1] = polygonSet.nodes[start];
                        var component = rentedArray.AsSpan(0, maxSize);
                        ProcessPolygon(component, isSubject, contourId, ref eventQueue, ref bbox);
                        ArrayPool<Vector2>.Shared.Return(rentedArray);
                    }
                }

                return contourId;
            }
        }
    }
}

