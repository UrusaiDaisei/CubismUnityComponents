using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    /// <summary>
    /// Represents a contour in a polygon, which can be either an exterior boundary or a hole.
    /// </summary>
    public class Contour
    {
        /// <summary>
        /// The list of points that define the contour.
        /// </summary>
        public List<Vector2> points;

        /// <summary>
        /// The list of indices for holes contained within this contour.
        /// </summary>
        public List<int> holeIds;

        /// <summary>
        /// Index of the contour that this contour is a hole of. Set to -1 if it's an exterior contour.
        /// </summary>
        public int holeOf;

        /// <summary>
        /// The nesting depth of this contour in the polygon structure.
        /// </summary>
        public int depth;

        private bool clockwise;
        private bool orientationCalculated;

        /// <summary>
        /// Initializes a new contour with empty point and hole lists.
        /// </summary>
        public Contour()
        {
            points = new List<Vector2>();
            holeIds = new List<int>();
        }

        /// <summary>
        /// Gets whether this contour is an exterior boundary (not a hole).
        /// </summary>
        public bool isExterior => this.holeOf == -1;

        /// <summary>
        /// Gets whether the contour points are arranged in clockwise order.
        /// This is calculated by computing the signed area of the contour.
        /// </summary>
        public bool isClockwise
        {
            get
            {
                if (orientationCalculated)
                    return clockwise;
                orientationCalculated = true;
                var area = SignedArea(points);
                clockwise = area < 0 ? true : false;
                return clockwise;
            }
        }

        /// <summary>
        /// Calculates the signed area of a polygon defined by a list of points.
        /// A positive result indicates counter-clockwise orientation; negative indicates clockwise.
        /// </summary>
        /// <param name="data">The list of points defining the polygon.</param>
        /// <returns>The signed area value.</returns>
        private static float SignedArea(IReadOnlyList<Vector2> data)
        {
            int count = data.Count;
            if (count < 3) return 0f;

            float area = 0f;
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                area += Helper.crossProduct(data[i], data[j]);
            }

            return area * 0.5f;
        }
    }
}