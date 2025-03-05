using System.Collections.Generic;
using UnityEngine;
using Live2D.Cubism.Core;
using System;

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

    [Serializable]
    private struct Holder
    {
        public Vector2[] vertices;
    }

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

            // Extract only the boundary vertices using the BoundaryExtractor
            var boundary = BoundaryExtractor.ExtractBoundaryVertices(worldVertices, indices);
            if (boundary.Count == 0)
                continue;

            boundaryVertices.Add(new Holder { vertices = boundary.ToArray() });
        }
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
        // Draw the boundary vertices
        int drawableIndex = 0;
        foreach (var holder in boundaryVertices)
        {
            // Get a unique color for this drawable based on its index
            Color drawableColor = GetColorForIndex(drawableIndex);
            Gizmos.color = drawableColor;

            // Draw vertices
            foreach (var vertex in holder.vertices)
            {
                Gizmos.DrawSphere(vertex, vertexSize);
            }

            // Draw lines connecting consecutive boundary vertices
            for (int i = 0; i < holder.vertices.Length; i++)
            {
                Vector3 start = holder.vertices[i];
                Vector3 end = holder.vertices[(i + 1) % holder.vertices.Length]; // Connect back to start
                Gizmos.DrawLine(start, end);
            }

            drawableIndex++;
        }
    }
}