using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    /// <summary>
    /// Represents a polygon with multiple components (exterior and holes).
    /// </summary>
    public struct Polygon
    {
        /// <summary>
        /// List of points (vertices) that make up the polygon.
        /// </summary>
        public List<Vector2> nodes;

        /// <summary>
        /// List of indices that mark the start of each component (exterior and holes).
        /// </summary>
        public List<int> startIDs;

        /// <summary>
        /// Initializes a new polygon with a specified initial capacity.
        /// </summary>
        /// <param name="size">Initial capacity for the nodes list.</param>
        public Polygon(int size)
        {
            nodes = new List<Vector2>(size);
            startIDs = new List<int>();
        }

        /// <summary>
        /// Initializes a new polygon with specified capacities for nodes and components.
        /// </summary>
        /// <param name="NodeSize">Initial capacity for the nodes list.</param>
        /// <param name="Components">Initial capacity for the components list.</param>
        public Polygon(int NodeSize, int Components)
        {
            nodes = new List<Vector2>(NodeSize);
            startIDs = new List<int>(Components);
        }

        /// <summary>
        /// Adds a new component to the polygon by marking the current node count as a start index.
        /// </summary>
        public void AddComponent()
        {
            startIDs.Add(this.nodes.Count);
        }

        /// <summary>
        /// Clears all nodes and component markers from the polygon.
        /// </summary>
        public void Clear()
        {
            nodes.Clear();
            startIDs.Clear();
        }
    }
}

