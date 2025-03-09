using System;
using UnityEngine;

namespace Martinez
{
    /// <summary>
    /// Represents a polygon with multiple components (exterior and holes).
    /// </summary>
    public struct Polygon
    {
        public static readonly Polygon Empty = new Polygon(
            Array.Empty<Vector2>(),
            Array.Empty<int>()
        );

        /// <summary>
        /// List of points (vertices) that make up the polygon.
        /// </summary>
        private readonly Vector2[] nodes;

        /// <summary>
        /// List of indices that mark the start of each component (exterior and holes).
        /// </summary>
        private readonly int[] startIDs;

        public Polygon(Vector2[] nodes, int[] startIDs)
        {
            this.nodes = nodes;
            this.startIDs = startIDs;
        }

        public ReadOnlySpan<Vector2> this[int index]
        {
            get
            {
                if (index < 0 || index >= startIDs.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var startIndex = startIDs[index];
                var endIndex = index < startIDs.Length - 1 ? startIDs[index + 1] : nodes.Length;
                return nodes.AsSpan(startIndex, endIndex - startIndex);
            }
        }

        public int Count => startIDs.Length;

        public bool IsEmpty => (nodes?.Length ?? 0) == 0;
    }
}

