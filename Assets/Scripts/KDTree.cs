using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using Live2D.Cubism.Framework.Utils;

/// <summary>
/// KD-Tree implementation for efficient nearest neighbor searches in 2D space (optimized for Live2D)
/// </summary>
public sealed class KDTree
{
    // Array to store all nodes - value structs for better cache locality
    private KDNode[] nodes;
    private IReadOnlyList<Vector2> points;

    public KDTree(IReadOnlyList<Vector2> vertices)
    {
        this.points = vertices;

        if (vertices.Count == 0)
            return;

        // We know exactly how many nodes we need - one per vertex
        nodes = new KDNode[vertices.Count];

        // Rent an array from the shared pool for the indices
        int[] indices = ArrayPool<int>.Shared.Rent(vertices.Count);

        try
        {
            // Fill the array with indices
            for (int i = 0; i < vertices.Count; i++)
                indices[i] = i;

            // Build the tree with a span of the entire array, root will be at index 0
            BuildTree(new Span<int>(indices, 0, vertices.Count), 0, 0);
        }
        finally
        {
            // Return the array to the pool when done
            ArrayPool<int>.Shared.Return(indices);
        }
    }

    /// <summary>
    /// Recursively builds the KD-Tree
    /// </summary>
    /// <param name="indices">The indices to build the tree from</param>
    /// <param name="depth">Current depth in the tree</param>
    /// <param name="nodeIndex">The index to store this node at</param>
    /// <returns>The index of the created node in the nodes array</returns>
    private int BuildTree(Span<int> indices, int depth, int nodeIndex)
    {
        if (indices.Length == 0)
            return -1; // -1 indicates no node (null)

        // Select axis based on depth (alternating between x and y for 2D)
        int axis = depth % 2;

        // Sort the span using the SpanExtensions utility
        indices.Sort((a, b) => GetAxisValue(points[a], axis).CompareTo(GetAxisValue(points[b], axis)));

        // Get median index
        int medianIdx = indices.Length / 2;

        // Setup the current node
        nodes[nodeIndex].pointIndex = indices[medianIdx];

        // Build left subtree if we have points
        if (medianIdx > 0)
        {
            // Find the next available node index
            int leftNodeIndex = nodeIndex + 1;
            nodes[nodeIndex].leftChildIndex = leftNodeIndex;

            // Build the left subtree and get the last used index
            int lastLeftIndex = BuildTree(indices.Slice(0, medianIdx), depth + 1, leftNodeIndex);

            // Build right subtree if we have points
            if (medianIdx < indices.Length - 1)
            {
                // Next available index is after all left nodes
                int rightNodeIndex = lastLeftIndex + 1;
                nodes[nodeIndex].rightChildIndex = rightNodeIndex;

                // Build the right subtree
                return BuildTree(indices.Slice(medianIdx + 1, indices.Length - medianIdx - 1), depth + 1, rightNodeIndex);
            }
            else
            {
                // No right subtree
                nodes[nodeIndex].rightChildIndex = -1;
                return lastLeftIndex;
            }
        }
        else
        {
            // No left subtree
            nodes[nodeIndex].leftChildIndex = -1;

            // Check if we have a right subtree
            if (medianIdx < indices.Length - 1)
            {
                // Next available index is nodeIndex + 1
                int rightNodeIndex = nodeIndex + 1;
                nodes[nodeIndex].rightChildIndex = rightNodeIndex;

                // Build the right subtree
                return BuildTree(indices.Slice(medianIdx + 1, indices.Length - medianIdx - 1), depth + 1, rightNodeIndex);
            }
            else
            {
                // No right subtree
                nodes[nodeIndex].rightChildIndex = -1;
                return nodeIndex;
            }
        }
    }

    /// <summary>
    /// Helper to get the value of a point along a specific axis
    /// </summary>
    private float GetAxisValue(Vector2 point, int axis)
    {
        if (axis == 0) return point.x;
        return point.y;
    }

    /// <summary>
    /// Get indices of all points within maxDistance of the specified point
    /// </summary>
    public List<int> GetNearbyPoints(int pointIndex, float maxDistance)
    {
        if (nodes == null || nodes.Length == 0)
            return new List<int>();

        Vector2 queryPoint = points[pointIndex];
        List<int> results = new List<int>();

        // Don't add the point itself
        SearchNeighbors(0, queryPoint, maxDistance, results, 0, pointIndex);

        return results;
    }

    /// <summary>
    /// Get indices of all points within maxDistance of the specified query point
    /// </summary>
    public List<int> GetPointsInRange(Vector2 queryPoint, float maxDistance)
    {
        if (nodes == null || nodes.Length == 0)
            return new List<int>();

        List<int> results = new List<int>();
        SearchNeighbors(0, queryPoint, maxDistance, results, 0, -1);
        return results;
    }

    /// <summary>
    /// Recursively search the KD-Tree for points within maxDistance
    /// </summary>
    private void SearchNeighbors(int nodeIndex, Vector2 queryPoint, float maxDistance,
                                List<int> results, int depth, int excludeIndex)
    {
        if (nodeIndex == -1)
            return;

        ref KDNode node = ref nodes[nodeIndex];

        // Calculate axis based on the current depth
        int axis = depth % 2;

        // Get the current point being examined
        Vector2 nodePoint = points[node.pointIndex];

        // Check if this point is within our search radius and not the query point itself
        if (node.pointIndex != excludeIndex &&
            Vector2.Distance(nodePoint, queryPoint) <= maxDistance)
        {
            results.Add(node.pointIndex);
        }

        // Get value of query point and current node's point along the current axis
        float queryValue = GetAxisValue(queryPoint, axis);
        float nodeValue = GetAxisValue(nodePoint, axis);

        // Determine which child to search first (the one likely to contain closer points)
        int firstChild = queryValue < nodeValue ? node.leftChildIndex : node.rightChildIndex;
        int secondChild = queryValue < nodeValue ? node.rightChildIndex : node.leftChildIndex;

        // First, search the child that's most likely to contain close points
        SearchNeighbors(firstChild, queryPoint, maxDistance, results, depth + 1, excludeIndex);

        // Check if we need to search the other child as well
        // If the distance from the query point to the splitting plane is less than maxDistance,
        // there could be relevant points on the other side of the plane
        if (Mathf.Abs(queryValue - nodeValue) <= maxDistance)
        {
            SearchNeighbors(secondChild, queryPoint, maxDistance, results, depth + 1, excludeIndex);
        }
    }

    /// <summary>
    /// Node structure for the KD-Tree as a struct for better memory efficiency
    /// </summary>
    private struct KDNode
    {
        public int pointIndex;       // Index of the point in the points array
        public int leftChildIndex;   // Index of the left child in the nodes array (-1 if none)
        public int rightChildIndex;  // Index of the right child in the nodes array (-1 if none)
    }
}