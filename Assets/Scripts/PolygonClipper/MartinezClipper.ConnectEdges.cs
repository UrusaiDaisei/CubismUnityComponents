using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public partial class MartinezClipper
    {
        /// <summary>
        /// Orders the sweep events for edge connection, keeping only those that are part of the result.
        /// </summary>
        /// <param name="sortedEvents">List of sorted sweep events.</param>
        /// <returns>List of result events ordered for edge connection.</returns>
        List<SweepEvent> orderEvents(List<SweepEvent> sortedEvents)
        {
            SweepEvent m_event, tmp;
            int i, len, tmp2;
            List<SweepEvent> resultEvents = new List<SweepEvent>();

            // Filter events that are part of the result
            for (i = 0, len = sortedEvents.Count; i < len; i++)
            {
                m_event = sortedEvents[i];
                if ((m_event.left && m_event.inResult) ||
                    (!m_event.left && m_event.otherEvent.inResult))
                    resultEvents.Add(m_event);
            }

            // Due to overlapping edges the resultEvents array can be not wholly sorted
            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                for (i = 0, len = resultEvents.Count; i < len; i++)
                {
                    if ((i + 1) < len &&
                        CompareEvents.Default.Compare(resultEvents[i], resultEvents[i + 1]) == 1)
                    {
                        tmp = resultEvents[i];
                        resultEvents[i] = resultEvents[i + 1];
                        resultEvents[i + 1] = tmp;
                        sorted = false;
                    }
                }
            }

            // Set the position index for each event
            for (i = 0, len = resultEvents.Count; i < len; i++)
            {
                m_event = resultEvents[i];
                m_event.otherPos = i;
            }

            // Ensure right events point to their left counterparts correctly
            // Imagine the right event is found in the beginning of the queue,
            // when its left counterpart is not marked yet
            for (i = 0, len = resultEvents.Count; i < len; i++)
            {
                m_event = resultEvents[i];
                if (!m_event.left)
                {
                    tmp2 = m_event.otherPos;
                    m_event.otherPos = m_event.otherEvent.otherPos;
                    m_event.otherEvent.otherPos = tmp2;
                }
            }
            return resultEvents;
        }

        /// <summary>
        /// Finds the next position in the result events list that hasn't been processed yet.
        /// </summary>
        /// <param name="pos">Current position.</param>
        /// <param name="resultEvents">List of result events.</param>
        /// <param name="processed">Array indicating which events have been processed.</param>
        /// <param name="origPos">Original starting position.</param>
        /// <returns>Next position to process.</returns>
        int nextPos(int pos, List<SweepEvent> resultEvents, bool[] processed, int origPos)
        {
            int newPos = pos + 1;
            Vector2 p = resultEvents[pos].point;
            Vector2 p1 = default;
            int length = resultEvents.Count;

            if (newPos < length)
                p1 = resultEvents[newPos].point;

            // Try to find a next event with the same point that hasn't been processed
            while (newPos < length && Helper.Approximately(p1, p))
            {
                if (!processed[newPos])
                    return newPos;
                else
                    newPos++;
                if (newPos < length)
                    p1 = resultEvents[newPos].point;
            }

            // If no suitable event found moving forward, try moving backward
            newPos = pos - 1;
            while (processed[newPos] && newPos > origPos)
                newPos--;

            return newPos;
        }

        /// <summary>
        /// Initializes a new contour based on the context of the current event.
        /// Determines if it's an exterior contour or a hole, and sets appropriate depth.
        /// </summary>
        /// <param name="m_event">The sweep event to initialize the contour from.</param>
        /// <param name="contours">List of existing contours.</param>
        /// <param name="contourId">ID to assign to the new contour.</param>
        /// <returns>A new initialized contour.</returns>
        Contour initializeContourFromContext(SweepEvent m_event, List<Contour> contours, int contourId)
        {
            Contour contour = new Contour();

            if (m_event.prevInResult != null)
            {
                SweepEvent prevInResult = m_event.prevInResult;
                // Note that it is valid to query the "previous in result" for its output contour id,
                // because we must have already processed it (i.e., assigned an output contour id)
                // in an earlier iteration, otherwise it wouldn't be possible that it is "previous in
                // result".
                int lowerContourId = prevInResult.contourId;
                int lowerResultTransition = prevInResult.resultTransition;

                if (lowerResultTransition > 0)
                {
                    // We are inside. Now we have to check if the thing below us is another hole or
                    // an exterior contour.
                    Contour lowerContour = contours[lowerContourId];

                    if (lowerContour.holeOf != -1)
                    {
                        // The lower contour is a hole => Connect the new contour as a hole to its parent,
                        // and use same depth.
                        int parentContourId = lowerContour.holeOf;
                        contours[parentContourId].holeIds.Add(contourId);
                        contour.holeOf = parentContourId;
                        contour.depth = contours[lowerContourId].depth;
                    }
                    else
                    {
                        // The lower contour is an exterior contour => Connect the new contour as a hole,
                        // and increment depth.
                        contours[lowerContourId].holeIds.Add(contourId);
                        contour.holeOf = lowerContourId;
                        contour.depth = contours[lowerContourId].depth + 1;
                    }
                }
                else
                {
                    // We are outside => this contour is an exterior contour of same depth.
                    contour.holeOf = -1;
                    contour.depth = contours[lowerContourId].depth;
                }
            }
            else
            {
                // There is no lower/previous contour => this contour is an exterior contour of depth 0.
                contour.holeOf = -1;
                contour.depth = 0;
            }
            return contour;
        }

        /// <summary>
        /// Connects edges to form contours (polygons and holes) from the sorted sweep events.
        /// </summary>
        /// <param name="sortedEvents">List of sorted sweep events.</param>
        /// <returns>List of contours representing the result polygons.</returns>
        List<Contour> connectEdges(List<SweepEvent> sortedEvents)
        {
            int i, len;
            List<SweepEvent> resultEvents = orderEvents(sortedEvents);
            len = resultEvents.Count;

            // Array to track which events have been processed
            bool[] processed = new bool[len];
            List<Contour> contours = new List<Contour>(len);

            for (i = 0; i < len; i++)
            {
                if (processed[i])
                    continue;

                int contourId = contours.Count;
                Contour contour = initializeContourFromContext(resultEvents[i], contours, contourId);

                // Helper function that combines marking an event as processed with assigning its output contour ID
                Action<int> markAsProcessed = (pos) =>
                {
                    processed[pos] = true;
                    resultEvents[pos].contourId = contourId;
                };

                int pos = i;
                int origPos = i;

                Vector2 initial = resultEvents[i].point;
                contour.points.Add(initial);

                // Build the contour by connecting edges
                while (true)
                {
                    markAsProcessed(pos);

                    pos = resultEvents[pos].otherPos;

                    markAsProcessed(pos);
                    contour.points.Add(resultEvents[pos].point);

                    pos = nextPos(pos, resultEvents, processed, origPos);

                    if (pos == origPos)
                        break;
                }
                contours.Add(contour);
            }
            return contours;
        }
    }
}