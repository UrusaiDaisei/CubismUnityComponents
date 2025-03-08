using System.Collections.Generic;
using System;
using UnityEngine;

namespace Martinez
{
    public partial class MartinezClipper
    {
        /// <summary>
        /// Calculates the intersection points between two line segments.
        /// </summary>
        /// <param name="a1">First point of the first line segment.</param>
        /// <param name="a2">Second point of the first line segment.</param>
        /// <param name="b1">First point of the second line segment.</param>
        /// <param name="b2">Second point of the second line segment.</param>
        /// <param name="noEndpointTouch">If true, endpoints that just touch are not considered intersections (connected segments).</param>
        /// <returns>
        /// If the lines intersect at a single point, returns a list with that point.
        /// If they overlap, returns a list with the two endpoints of the overlapping segment.
        /// If they don't intersect, returns null.
        /// </returns>
        List<Vector2> intersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, bool noEndpointTouch)
        {
            List<Vector2> returnList = new List<Vector2>(2);

            // The algorithm expects our lines in the form P + sd, where P is a point,
            // s is on the interval [0, 1], and d is a vector.
            // We are passed two points. P can be the first point of each pair. The
            // vector, then, could be thought of as the distance (in x and y components)
            // from the first point to the second point.

            // First, make our vectors:
            Vector2 va = a2 - a1;
            Vector2 vb = b2 - b1;

            // Define a function to convert back to regular point form:
            static Vector2 toPoint(Vector2 p, float s, Vector2 d)
            {
                return p + s * d;
            }

            // The rest is pretty much a straight port of the algorithm.
            Vector2 e = b1 - a1;
            float kross = Helper.crossProduct(va, vb);
            float sqrKross = kross * kross;
            float sqrLenA = Vector2.Dot(va, va);

            // Check for line intersection. This works because of the properties of the
            // cross product -- specifically, two vectors are parallel if and only if the
            // cross product is the 0 vector. The full calculation involves relative error
            // to account for possible very small line segments. See Schneider & Eberly
            // for details.
            if (sqrKross > 0)
            {
                // If they're not parallel, then (because these are line segments) they
                // still might not actually intersect. This code checks that the
                // intersection point of the lines is actually on both line segments.
                float s = Helper.crossProduct(e, vb) / kross;

                if (s < 0 || s > 1) // not on line segment a
                {
                    return null;
                }

                float t = Helper.crossProduct(e, va) / kross;

                if (t < 0 || t > 1) // not on line segment b
                {
                    return null;
                }

                if (Mathf.Approximately(s, 0) || Mathf.Approximately(s, 1))
                {
                    // on an endpoint of line segment a
                    if (noEndpointTouch)
                        return null;
                    else
                    {
                        returnList.Add(toPoint(a1, s, va));
                        return returnList;
                    }
                }

                if (Mathf.Approximately(t, 0) || Mathf.Approximately(t, 1))
                {
                    // on an endpoint of line segment b
                    if (noEndpointTouch)
                        return null;
                    else
                    {
                        returnList.Add(toPoint(b1, t, vb));
                        return returnList;
                    }
                }

                returnList.Add(toPoint(a1, s, va));
                return returnList;
            }

            // If we've reached this point, then the lines are either parallel or the
            // same, but the segments could overlap partially or fully, or not at all.
            // So we need to find the overlap, if any. To do that, we can use e, which is
            // the (vector) difference between the two initial points. If this is parallel
            // with the line itself, then the two lines are the same line, and there will
            // be overlap.
            kross = Helper.crossProduct(e, va);
            sqrKross = kross * kross;

            if (sqrKross > 0)
            {
                // Lines are just parallel, not the same. No overlap.
                return null;
            }

            float sa = Vector2.Dot(va, e) / sqrLenA;
            float sb = sa + Vector2.Dot(va, vb) / sqrLenA;
            float smin = Mathf.Min(sa, sb);
            float smax = Mathf.Max(sa, sb);

            // This is, essentially, the FindIntersection acting on floats from
            // Schneider & Eberly, just inlined into this function.
            if (smin <= 1 && smax >= 0)
            {
                // Overlap on an end point
                if (Mathf.Approximately(smin, 1))
                {
                    if (noEndpointTouch)
                        return null;
                    else
                    {
                        returnList.Add(toPoint(a1, smin > 0 ? smin : 0, va));
                        return returnList;
                    }
                }

                if (Mathf.Approximately(smax, 0))
                {
                    if (noEndpointTouch)
                        return null;
                    else
                    {
                        returnList.Add(toPoint(a1, smax < 1 ? smax : 1, va));
                        return returnList;
                    }
                }

                if (noEndpointTouch && Mathf.Approximately(smin, 0) && Mathf.Approximately(smax, 1))
                    return null;

                // There's overlap on a segment -- two points of intersection. Return both.
                returnList.Add(toPoint(a1, smin > 0 ? smin : 0, va));
                returnList.Add(toPoint(a1, smax < 1 ? smax : 1, va));
                return returnList;
            }
            return null;
        }
    }
}


