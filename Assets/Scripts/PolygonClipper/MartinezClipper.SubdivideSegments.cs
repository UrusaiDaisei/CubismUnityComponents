using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{

    public partial class MartinezClipper
    {
        List<SweepEvent> subdivideSegments(MinHeap<SweepEvent> eventQueue, Rect sbbox, Rect cbbox, ClipType operation)
        {
            AVLTree<SweepEvent> sweepLine = new AVLTree<SweepEvent>(CompareSegments.Default);
            List<SweepEvent> sortedEvents = new List<SweepEvent>();

            float rightbound = Mathf.Min(sbbox.xMax, cbbox.xMax);

            AVLNode<SweepEvent> prev, next, begin = null;

            while (eventQueue.Length != 0)
            {
                var m_event = eventQueue.Pop();


                sortedEvents.Add(m_event);

                // optimization by bboxes for intersection and difference goes here
                if ((operation == ClipType.Intersection && m_event.point.x > rightbound) ||
                    (operation == ClipType.Difference && m_event.point.x > sbbox.xMax))
                {
                    break;
                }

                if (m_event.left)
                {
                    m_event.positionInSweepLine = sweepLine.Insert(m_event);
                    //Console.WriteLine(sweepLine);
                    next = prev = m_event.positionInSweepLine;
                    begin = sweepLine.GetMinNode();

                    if (prev != begin) prev = prev.GetPredecessor();
                    else prev = null;

                    next = next.GetSuccessor();

                    SweepEvent prevEvent = prev != null ? prev.Value : null;
                    SweepEvent prevprevEvent;
                    ComputeFields(m_event, prevEvent, operation);
                    if (next != null)
                    {
                        if (possibleIntersection(m_event, next.Value, eventQueue) == 2)
                        {
                            ComputeFields(m_event, prevEvent, operation);
                            ComputeFields(m_event, next.Value, operation);
                        }
                    }

                    if (prev != null)
                    {
                        if (possibleIntersection(prev.Value, m_event, eventQueue) == 2)
                        {
                            AVLNode<SweepEvent> prevprev = prev;
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
                    m_event = m_event.otherEvent;
                    next = prev = sweepLine.Find(m_event);

                    if (prev != null && next != null)
                    {
                        if (prev != begin) prev = prev.GetPredecessor();
                        else prev = null;

                        next = next.GetSuccessor();
                        sweepLine.Remove(m_event);
                        //Console.WriteLine(sweepLine);

                        if (next != null && prev != null)
                            possibleIntersection(prev.Value, next.Value, eventQueue);
                    }
                }
            }
            return sortedEvents;
        }
    }
}

