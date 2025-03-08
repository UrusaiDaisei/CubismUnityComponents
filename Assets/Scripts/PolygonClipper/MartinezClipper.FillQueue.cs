using System;
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
        void processPolygon(List<Vector2> contourOrHole, bool isSubject, int depth, ref MinHeap<SweepEvent> Q, ref Rect bbox)
        {
            int i, len;
            Vector2 s1, s2;
            SweepEvent e1, e2;
            for (i = 0, len = contourOrHole.Count - 1; i < len; i++)
            {
                s1 = contourOrHole[i];
                s2 = contourOrHole[i + 1];
                e1 = new SweepEvent(s1, false, null, isSubject);
                e2 = new SweepEvent(s2, false, e1, isSubject);
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
        MinHeap<SweepEvent> fillQueue(List<Polygon> subject, List<Polygon> clipping, ref Rect sbbox, ref Rect cbbox, ClipType operation)
        {
            int contourId = 0;
            MinHeap<SweepEvent> eventQueue = new MinHeap<SweepEvent>(CompareEvents.Default);
            Polygon polygonSet;
            bool isExteriorRing;
            int i, ii, j, jj, k;

            // Process subject polygons
            for (i = 0, ii = subject.Count; i < ii; i++)
            {
                polygonSet = subject[i];
                for (j = 0, jj = polygonSet.startIDs.Count - 1; j < jj; j++)
                {
                    isExteriorRing = j == 0;
                    if (isExteriorRing) contourId++;
                    int start = polygonSet.startIDs[j];
                    int end = polygonSet.startIDs[j + 1];
                    List<Vector2> component = new List<Vector2>();
                    for (k = start; k < end; k++)
                        component.Add(polygonSet.nodes[k]);
                    // Close the ring so intersection between end and start are detected
                    component.Add(polygonSet.nodes[start]);
                    processPolygon(component, true, contourId, ref eventQueue, ref sbbox);
                }
            }

            // Process clipping polygons
            for (i = 0, ii = clipping.Count; i < ii; i++)
            {
                polygonSet = clipping[i];
                for (j = 0, jj = polygonSet.startIDs.Count - 1; j < jj; j++)
                {
                    isExteriorRing = j == 0;
                    if (operation == ClipType.Difference) isExteriorRing = false;
                    if (isExteriorRing) contourId++;
                    int start = polygonSet.startIDs[j];
                    int end = polygonSet.startIDs[j + 1];
                    List<Vector2> component = new List<Vector2>();
                    for (k = start; k < end; k++)
                        component.Add(polygonSet.nodes[k]);
                    // Close the ring so intersection between end and start are detected
                    component.Add(polygonSet.nodes[start]);
                    processPolygon(component, false, contourId, ref eventQueue, ref cbbox);
                }
            }
            return eventQueue;
        }
    }
}

