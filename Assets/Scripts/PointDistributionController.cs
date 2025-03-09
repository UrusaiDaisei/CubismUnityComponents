using System.Collections.Generic;
using UnityEngine;
using Live2D.Cubism.Core;
using System;
using Martinez;
using System.Linq;

/// <summary>
/// Controller that extracts mesh data from Cubism drawables and visualizes them.
/// This is a first step toward implementing point distribution across drawable surfaces.
/// </summary>
[ExecuteInEditMode]
public class PointDistributionController : MonoBehaviour
{
    [Tooltip("List of Cubism drawables to extract mesh data from")]
    public List<CubismDrawable> targetDrawables = new List<CubismDrawable>();

    [Tooltip("Size of vertex gizmos")]
    public float vertexSize = 0.05f;

    // Store only boundary vertices for each drawable
    [SerializeField]
    private List<Holder> boundaryVertices = new List<Holder>();

    // Store the merged boundaries after union operations
    [SerializeField]
    private List<Holder> mergedBoundaries = new List<Holder>();

    [Serializable]
    private struct Holder
    {
        public Vector2[] vertices;
        public int[] indices;
    }

    public bool Toogle = true;

    /// <summary>
    /// Updates the mesh data and extracts boundary vertices from the target drawables.
    /// </summary>
    [ContextMenu("Extract Mesh Data")]
    public void UpdateMeshData()
    {
        boundaryVertices.Clear();

        foreach (var drawable in targetDrawables)
        {
            if (drawable == null) continue;

            // Get vertex positions and indices from the drawable
            var vertices = drawable.VertexPositions;
            var indices = drawable.Indices;

            if (vertices == null || indices == null)
                continue;

            // Transform vertices from drawable's local space to world space
            var worldVertices = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                worldVertices[i] = drawable.transform.TransformPoint(vertices[i]);
            }

            // Store both vertices and indices
            boundaryVertices.Add(new Holder
            {
                vertices = worldVertices,
                indices = indices
            });
        }
    }

    /// <summary>
    /// Performs a boolean union operation on all boundary polygons that intersect.
    /// </summary>
    [ContextMenu("Merge Intersecting Polygons")]
    public void MergeIntersectingPolygons()
    {
        // Make sure we have boundary data
        if (boundaryVertices.Count == 0)
        {
            Debug.LogWarning("No boundary data available. Run 'Extract Mesh Data' first.");
            return;
        }

        // Convert our mesh data to the format expected by the Martinez clipper
        List<Martinez.Polygon> polygons = new List<Martinez.Polygon>();
        foreach (var holder in boundaryVertices)
        {
            // Create a polygon from mesh data (vertices and indices)
            Polygon polygon = MeshToPolygon(holder.vertices, holder.indices);
            if (!polygon.IsEmpty)
                polygons.Add(polygon);
        }

        // Create a Martinez clipper
        Martinez.MartinezClipper clipper = new Martinez.MartinezClipper();

        // Perform the union operation (if there's only one polygon, just use it)
        List<Martinez.Polygon> result;
        if (polygons.Count <= 1)
        {
            result = polygons;
        }
        else
        {
            // Start with the first polygon
            List<Martinez.Polygon> current = new List<Martinez.Polygon> { polygons[0] };

            // Union each additional polygon one by one
            for (int i = 1; i < polygons.Count; i++)
            {
                List<Martinez.Polygon> next = new List<Martinez.Polygon> { polygons[i] };
                current = clipper.Compute(current, next, Martinez.ClipType.Union);

                // If there's no result (completely separate polygons), add both to the result
                if (current == null)
                {
                    current = new List<Martinez.Polygon>();
                    current.Add(polygons[i - 1]);
                    current.Add(polygons[i]);
                }
            }

            result = current;
        }

        // Convert the result back to Vector2 arrays
        mergedBoundaries.Clear();
        foreach (var polygon in result)
        {
            // For each polygon, convert all its points to Vector2
            // Process all contours (both exterior and holes)
            for (int i = 0; i < polygon.Count; i++)
            {
                mergedBoundaries.Add(new Holder
                {
                    vertices = polygon[i].ToArray()
                });
            }
        }

        Debug.Log($"Merged {boundaryVertices.Count} polygons into {mergedBoundaries.Count} contours.");
    }

    /// <summary>
    /// Gets a color based on the index, with full saturation and value.
    /// </summary>
    private Color GetColorForIndex(int index)
    {
        // Calculate hue by dividing index by total count to distribute colors evenly
        float hue = index / Mathf.Max(1.0f, targetDrawables.Count);
        return Color.HSVToRGB(hue, 1f, 1f);
    }

    /// <summary>
    /// Draws the extracted boundary vertices as gizmos in the scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Toogle)
        {
            // Draw the original boundary vertices with transparency
            int drawableIndex = 0;
            foreach (var holder in boundaryVertices)
            {
                // Get a unique color for this drawable based on its index
                Color drawableColor = GetColorForIndex(drawableIndex);
                Gizmos.color = drawableColor;

                // Draw vertices with reduced size
                foreach (var vertex in holder.vertices)
                {
                    Gizmos.DrawSphere(vertex, vertexSize * 0.5f);
                }

                // If we have indices, draw the triangles
                if (holder.indices != null && holder.indices.Length > 0)
                {
                    for (int i = 0; i < holder.indices.Length; i += 3)
                    {
                        if (i + 2 < holder.indices.Length)
                        {
                            Vector3 v1 = holder.vertices[holder.indices[i]];
                            Vector3 v2 = holder.vertices[holder.indices[i + 1]];
                            Vector3 v3 = holder.vertices[holder.indices[i + 2]];

                            Gizmos.DrawLine(v1, v2);
                            Gizmos.DrawLine(v2, v3);
                            Gizmos.DrawLine(v3, v1);
                        }
                    }
                }
                // If no indices, draw lines connecting consecutive vertices
                else if (holder.vertices.Length > 0)
                {
                    for (int i = 0; i < holder.vertices.Length; i++)
                    {
                        Vector3 start = holder.vertices[i];
                        Vector3 end = holder.vertices[(i + 1) % holder.vertices.Length]; // Connect back to start
                        Gizmos.DrawLine(start, end);
                    }
                }

                drawableIndex++;
            }
        }
        else
        {
            // Draw the merged boundaries with full opacity
            for (int i = 0; i < mergedBoundaries.Count; i++)
            {
                // Use a different color scheme for merged boundaries
                Color mergedColor = Color.HSVToRGB((float)i / Mathf.Max(1, mergedBoundaries.Count), 0.8f, 1f);
                Gizmos.color = mergedColor;

                var holder = mergedBoundaries[i];

                // Draw vertices
                foreach (var vertex in holder.vertices)
                {
                    Gizmos.DrawSphere(vertex, vertexSize);
                }

                // Draw lines connecting consecutive boundary vertices
                for (int j = 0; j < holder.vertices.Length; j++)
                {
                    Vector3 start = holder.vertices[j];
                    Vector3 end = holder.vertices[(j + 1) % holder.vertices.Length]; // Connect back to start
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }

    /// <summary>
    /// Converts mesh data (vertices and indices) to a Martinez.Polygon.
    /// This method extracts the boundary edges (exterior and holes) from the mesh.
    /// </summary>
    private Martinez.Polygon MeshToPolygon(Vector2[] vertices, int[] indices)
    {
        // Extract boundary edges (edges that appear in exactly one triangle)
        Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

        // Process all triangles to count edges
        for (int i = 0; i < indices.Length; i += 3)
        {
            if (i + 2 >= indices.Length) continue;

            int v1 = indices[i];
            int v2 = indices[i + 1];
            int v3 = indices[i + 2];

            // Skip degenerate triangles
            if (v1 >= vertices.Length || v2 >= vertices.Length || v3 >= vertices.Length)
                continue;

            if (v1 == v2 || v2 == v3 || v3 == v1)
                continue;

            // Create the three edges of this triangle
            Edge edge1 = new Edge(v1, v2);
            Edge edge2 = new Edge(v2, v3);
            Edge edge3 = new Edge(v3, v1);

            // Count occurrences of each edge
            if (edgeCount.ContainsKey(edge1))
                edgeCount[edge1]++;
            else
                edgeCount[edge1] = 1;

            if (edgeCount.ContainsKey(edge2))
                edgeCount[edge2]++;
            else
                edgeCount[edge2] = 1;

            if (edgeCount.ContainsKey(edge3))
                edgeCount[edge3]++;
            else
                edgeCount[edge3] = 1;
        }

        // Extract boundary edges (those that appear exactly once)
        List<Edge> boundaryEdges = new List<Edge>();
        foreach (var entry in edgeCount)
        {
            if (entry.Value == 1)
                boundaryEdges.Add(entry.Key);
        }

        // No boundary edges found
        if (boundaryEdges.Count == 0)
            return Polygon.Empty;

        // Create edge chains by connecting edges
        List<List<int>> contours = new List<List<int>>();

        // While we still have boundary edges, form contours
        while (boundaryEdges.Count > 0)
        {
            List<int> contour = new List<int>();

            // Start with the first edge
            Edge currentEdge = boundaryEdges[0];
            boundaryEdges.RemoveAt(0);

            // Add first vertex
            contour.Add(currentEdge.v1);
            int currentVertex = currentEdge.v2;

            // Keep adding vertices until we complete the loop
            while (currentVertex != contour[0] && boundaryEdges.Count > 0)
            {
                contour.Add(currentVertex);

                // Find the next edge that connects with the current vertex
                bool foundNext = false;
                for (int i = 0; i < boundaryEdges.Count; i++)
                {
                    Edge edge = boundaryEdges[i];

                    if (edge.v1 == currentVertex)
                    {
                        currentVertex = edge.v2;
                        boundaryEdges.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                    else if (edge.v2 == currentVertex)
                    {
                        currentVertex = edge.v1;
                        boundaryEdges.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                }

                if (!foundNext)
                    break; // Chain is broken, will form a new contour
            }

            // Only add contours with at least 3 vertices (a valid polygon)
            if (contour.Count >= 3)
                contours.Add(contour);
        }

        // No valid contours found
        if (contours.Count == 0)
            return Polygon.Empty;

        var builder = new PolygonBuilder();

        // Sort contours by area (largest first - this will be our exterior contour)
        contours.Sort((a, b) => CalculateContourArea(vertices, b).CompareTo(CalculateContourArea(vertices, a)));

        // Add each contour as a component
        foreach (var contour in contours)
            builder.CreateComponent(contour.Select(i => vertices[i]));

        return builder.Build();
    }

    /// <summary>
    /// Calculates the signed area of a contour.
    /// Positive area means counter-clockwise winding order, negative means clockwise.
    /// </summary>
    private float CalculateContourArea(Vector2[] vertices, List<int> contour)
    {
        float area = 0;
        for (int i = 0; i < contour.Count; i++)
        {
            int j = (i + 1) % contour.Count;
            if (contour[i] < vertices.Length && contour[j] < vertices.Length) // Safety check
            {
                area += vertices[contour[i]].x * vertices[contour[j]].y;
                area -= vertices[contour[j]].x * vertices[contour[i]].y;
            }
        }
        return area / 2f;
    }

    /// <summary>
    /// Edge struct for boundary detection.
    /// </summary>
    private struct Edge
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
        {
            return (v1 * 31) + v2;
        }
    }
}