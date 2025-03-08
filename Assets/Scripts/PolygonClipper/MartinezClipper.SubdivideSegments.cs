using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{

    public partial class MartinezClipper
    {
        /// <summary>
        /// Subdivides segments at intersection points using a sweep line algorithm.
        /// </summary>
        /// <param name="eventQueue">The priority queue of sweep events.</param>
        /// <param name="sbbox">Bounding box of the subject polygons.</param>
        /// <param name="cbbox">Bounding box of the clipping polygons.</param>
        /// <param name="operation">The type of Boolean operation to perform.</param>
        /// <returns>List of sorted sweep events after subdivision.</returns>
        List<SweepEvent> SubdivideSegments(MinHeap<SweepEvent> eventQueue, Rect sbbox, Rect cbbox, ClipType operation)
        {
            AVLTree<SweepEvent> sweepLine = new AVLTree<SweepEvent>(CompareSegments.Default);
            List<SweepEvent> sortedEvents = new List<SweepEvent>();

            float rightbound = Mathf.Min(sbbox.xMax, cbbox.xMax);

            AVLTree<SweepEvent>.Node prev, next, begin = null;

            while (eventQueue.Length != 0)
            {
                var m_event = eventQueue.Pop();
                sortedEvents.Add(m_event);

                // Optimization by bounding boxes for intersection and difference operations
                if ((operation == ClipType.Intersection && m_event.point.x > rightbound) ||
                    (operation == ClipType.Difference && m_event.point.x > sbbox.xMax))
                {
                    break;
                }

                if (m_event.left)
                {
                    // Left endpoint of segment enters the sweep line
                    m_event.positionInSweepLine = sweepLine.Insert(m_event);
                    next = prev = m_event.positionInSweepLine;
                    begin = sweepLine.GetMinNode();

                    if (prev != begin) prev = prev.GetPredecessor();
                    else prev = null;

                    next = next.GetSuccessor();

                    SweepEvent prevEvent = prev != null ? prev.Value : null;
                    SweepEvent prevprevEvent;

                    // Compute fields for the current event based on the previous event
                    ComputeFields(m_event, prevEvent, operation);

                    // Check for possible intersection with the next segment
                    if (next != null)
                    {
                        if (possibleIntersection(m_event, next.Value, eventQueue) == 2)
                        {
                            ComputeFields(m_event, prevEvent, operation);
                            ComputeFields(m_event, next.Value, operation);
                        }
                    }

                    // Check for possible intersection with the previous segment
                    if (prev != null)
                    {
                        if (possibleIntersection(prev.Value, m_event, eventQueue) == 2)
                        {
                            AVLTree<SweepEvent>.Node prevprev = prev;
                            if (prevprev != begin) prevprev = prevprev.GetPredecessor();
                            else prevprev = null;

                            prevprevEvent = prevprev != null ? prevprev.Value : null;
                            ComputeFields(prevEvent, prevprevEvent, operation);
                            ComputeFields(m_event, prevEvent, operation);
                        }
                    }
                }
                else
                {
                    // Right endpoint of segment exits the sweep line
                    m_event = m_event.otherEvent;
                    next = prev = sweepLine.Find(m_event);

                    if (prev != null && next != null)
                    {
                        if (prev != begin) prev = prev.GetPredecessor();
                        else prev = null;

                        next = next.GetSuccessor();
                        sweepLine.Remove(m_event);

                        // Check for possible intersection between the segments that become adjacent
                        if (next != null && prev != null)
                            possibleIntersection(prev.Value, next.Value, eventQueue);
                    }
                }
            }
            return sortedEvents;
        }
    }
}

