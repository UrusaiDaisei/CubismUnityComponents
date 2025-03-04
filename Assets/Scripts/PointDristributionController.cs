using UnityEngine;
using Live2D.Cubism.Core;
using System.Collections.Generic;

public class PointDistributionController : MonoBehaviour
{
    [Tooltip("The Live2D drawables to extract vertices from")]
    [SerializeField] private CubismDrawable[] drawables;

    [Tooltip("Size of each vertex point in the gizmo visualization")]
    [SerializeField] private float pointSize = 0.05f;

    [Tooltip("Maximum distance between points to be considered part of the same cluster")]
    [Range(0.00001f, float.MaxValue)]
    [SerializeField] private float maxClusterDistance = 0.1f;

    // Class to represent a cluster of points
    [System.Serializable]
    private class Cluster
    {
        public List<Vector3> points = new List<Vector3>();
        public Color color;

        public Cluster()
        {
            // Generate a random vibrant color
            color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);
        }
    }

    // Store the clusters for efficient access
    [SerializeField]
    private List<Cluster> clusters = new List<Cluster>();

    /// <summary>
    /// Called when a value is changed in the inspector
    /// </summary>
    private void OnValidate()
    {
        ExtractVerticesAndCluster();
    }

    /// <summary>
    /// Adds a "Recompute" option to the context menu
    /// </summary>
    [ContextMenu("Recompute")]
    private void Recompute()
    {
        ExtractVerticesAndCluster();
    }

    /// <summary>
    /// Extract vertices from all drawables and cluster them
    /// </summary>
    private void ExtractVerticesAndCluster()
    {
        clusters.Clear();
        List<Vector3> extractedVertices = ExtractVertices();

        // Now perform the clustering
        PerformClustering(extractedVertices);
    }

    /// <summary>
    /// Extract vertices from all drawables and return them as a list
    /// </summary>
    /// <returns>List of vertex positions in world space</returns>
    private List<Vector3> ExtractVertices()
    {
        List<Vector3> vertices = new List<Vector3>();

        if (drawables == null)
            return vertices;

        // Extract all vertices
        foreach (var drawable in drawables)
        {
            if (drawable == null)
                continue;

            // Get vertex positions from the drawable
            Vector3[] vertexPositions = drawable.VertexPositions;

            if (vertexPositions != null)
            {
                // Transform the vertices to world space and add them to our list
                foreach (var vertexPos in vertexPositions)
                {
                    // Convert to world space
                    Vector3 worldPos = drawable.transform.TransformPoint(vertexPos);
                    vertices.Add(worldPos);
                }
            }
        }

        return vertices;
    }

    /// <summary>
    /// Cluster the vertices based on distance
    /// </summary>
    private void PerformClustering(List<Vector3> vertices)
    {
        if (vertices.Count == 0)
            return;

        // Simple distance-based clustering algorithm
        foreach (var vertex in vertices)
        {
            bool addedToExistingCluster = false;

            // Try to add to an existing cluster
            foreach (var cluster in clusters)
            {
                foreach (var point in cluster.points)
                {
                    if (Vector3.Distance(vertex, point) <= maxClusterDistance)
                    {
                        cluster.points.Add(vertex);
                        addedToExistingCluster = true;
                        break;
                    }
                }

                if (addedToExistingCluster)
                    break;
            }

            // If not added to any existing cluster, create a new cluster
            if (!addedToExistingCluster)
            {
                Cluster newCluster = new Cluster();
                newCluster.points.Add(vertex);
                clusters.Add(newCluster);
            }
        }

        // Merge overlapping clusters
        MergeClusters();
    }

    /// <summary>
    /// Merge clusters that have points within maxClusterDistance of each other
    /// </summary>
    private void MergeClusters()
    {
        bool mergeOccurred;
        do
        {
            mergeOccurred = false;
            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    if (ClustersShouldMerge(clusters[i], clusters[j]))
                    {
                        // Merge clusters[j] into clusters[i]
                        clusters[i].points.AddRange(clusters[j].points);
                        clusters.RemoveAt(j);
                        mergeOccurred = true;
                        break;
                    }
                }
                if (mergeOccurred) break;
            }
        } while (mergeOccurred);
    }

    /// <summary>
    /// Check if two clusters should be merged
    /// </summary>
    private bool ClustersShouldMerge(Cluster a, Cluster b)
    {
        foreach (var pointA in a.points)
        {
            foreach (var pointB in b.points)
            {
                if (Vector3.Distance(pointA, pointB) <= maxClusterDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Draw all vertices as gizmos in the scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!enabled || clusters == null || clusters.Count == 0)
            return;

        // Draw each cluster with its own color
        foreach (var cluster in clusters)
        {
            Gizmos.color = cluster.color;

            // Draw each vertex in the cluster as a sphere
            foreach (var point in cluster.points)
            {
                Gizmos.DrawSphere(point, pointSize);
            }
        }
    }

    /// <summary>
    /// Draw the vertices when the object is selected
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // We could use a different color or size here for selected state
        // But for now we just reuse the OnDrawGizmos implementation
        OnDrawGizmos();
    }
}
