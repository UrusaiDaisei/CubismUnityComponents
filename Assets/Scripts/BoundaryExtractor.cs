using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class for extracting boundary vertices from mesh data.
/// </summary>
public class BoundaryExtractor
{
    /// <summary>
    /// Extracts boundary vertices from a mesh defined by vertices and triangle indices.
    /// A boundary edge is one that belongs to only one triangle.
    /// </summary>
    public static List<Vector2> ExtractBoundaryVertices(Vector2[] vertices, int[] indices)
    {
        // Dictionary to track edges and how many triangles use them
        Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

        // Process all triangles to count edges
        for (int i = 0; i < indices.Length - 2; i += 3)
        {
            int v1 = indices[i];
            int v2 = indices[i + 1];
            int v3 = indices[i + 2];

            // Create the three edges of this triangle
            Edge edge1 = new Edge(v1, v2);
            Edge edge2 = new Edge(v2, v3);
            Edge edge3 = new Edge(v3, v1);

            // Count occurrences of each edge
            IncrementEdgeCount(edgeCount, edge1);
            IncrementEdgeCount(edgeCount, edge2);
            IncrementEdgeCount(edgeCount, edge3);
        }

        // Find boundary edges (those that appear exactly once)
        HashSet<int> boundaryVertexIndices = new HashSet<int>();
        foreach (var edge in edgeCount.Keys)
        {
            if (edgeCount[edge] != 1)
                continue;

            boundaryVertexIndices.Add(edge.v1);
            boundaryVertexIndices.Add(edge.v2);
        }

        // Create list of boundary vertices
        var boundaryVertexList = new List<Vector2>();
        foreach (int index in boundaryVertexIndices)
            boundaryVertexList.Add(vertices[index]);

        return OrderBoundaryVertices(boundaryVertexList);
    }

    /// <summary>
    /// Orders boundary vertices to form a continuous path around the boundary in clockwise direction.
    /// Works with both convex and non-convex boundaries.
    /// </summary>
    private static List<Vector2> OrderBoundaryVertices(List<Vector2> unorderedVertices)
    {
        if (unorderedVertices.Count <= 2)
            return unorderedVertices;

        // Step 1: Create a coherent path by connecting nearest neighbors
        List<Vector2> pathVertices = new List<Vector2>();
        List<Vector2> remainingVertices = new List<Vector2>(unorderedVertices);

        // Start with the rightmost vertex (helps establish clockwise orientation)
        Vector2 current = remainingVertices[0];
        foreach (var vertex in remainingVertices)
        {
            if (vertex.x > current.x)
                current = vertex;
        }

        pathVertices.Add(current);
        remainingVertices.Remove(current);

        // Connect points by nearest neighbor until we use all vertices
        while (remainingVertices.Count > 0)
        {
            float minDistance = float.MaxValue;
            Vector2 nearest = Vector2.zero;

            foreach (var candidate in remainingVertices)
            {
                float distance = Vector2.Distance(current, candidate);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = candidate;
                }
            }

            pathVertices.Add(nearest);
            current = nearest;
            remainingVertices.Remove(nearest);
        }

        // Step 2: Ensure the path is in clockwise order by calculating the area
        // If area is positive, the path is counter-clockwise and needs reversal
        float area = 0;
        for (int i = 0; i < pathVertices.Count; i++)
        {
            Vector2 current2 = pathVertices[i];
            Vector2 next = pathVertices[(i + 1) % pathVertices.Count];
            area += (next.x - current2.x) * (next.y + current2.y);
        }

        // If area is positive, the path is counter-clockwise, so we reverse it
        if (area > 0)
        {
            pathVertices.Reverse();
        }

        return pathVertices;
    }

    /// <summary>
    /// Helper method to increment the count for an edge in the dictionary.
    /// </summary>
    private static void IncrementEdgeCount(Dictionary<Edge, int> edgeCount, Edge edge)
    {
        if (edgeCount.ContainsKey(edge))
        {
            edgeCount[edge]++;
        }
        else
        {
            edgeCount[edge] = 1;
        }
    }

    /// <summary>
    /// Struct to represent an edge between two vertices.
    /// The order of vertices is normalized to ensure edge equality works correctly.
    /// </summary>
    public struct Edge
    {
        public readonly int v1;
        public readonly int v2;

        public Edge(int vertex1, int vertex2)
        {
            // Store vertices in ascending order to ensure edge equivalence
            if (vertex1 < vertex2)
            {
                v1 = vertex1;
                v2 = vertex2;
            }
            else
            {
                v1 = vertex2;
                v2 = vertex1;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;
            Edge other = (Edge)obj;
            return v1 == other.v1 && v2 == other.v2;
        }

        public override int GetHashCode()
            => HashCode.Combine(v1, v2);
    }
}