using UnityEngine;
using Live2D.Cubism.Core;
using System.Collections.Generic;
using System.Linq;

public class PointDistributionController : MonoBehaviour
{
    [Tooltip("The Live2D drawables to extract vertices from")]
    [SerializeField] private CubismDrawable[] drawables;

    [Tooltip("Size of each vertex point in the gizmo visualization")]
    [SerializeField] private float pointSize = 0.05f;

    [Tooltip("Maximum distance between points to be considered part of the same cluster")]
    [Range(0.00001f, float.MaxValue)]
    [SerializeField] private float maxClusterDistance = 0.1f;

    [Tooltip("Whether to draw the point gizmos in the scene view")]
    [SerializeField] private bool showClusters = true;

    // Class to represent a cluster of points
    [System.Serializable]
    private class Cluster
    {
        public List<Vector3> points = new List<Vector3>();
        public Bounds bounds;

        public Cluster()
        {
            bounds = new Bounds();
        }

        public void AddPoint(Vector3 point)
        {
            if (points.Count == 0)
            {
                bounds = new Bounds(point, Vector3.zero);
            }
            else
            {
                bounds.Encapsulate(point);
            }
            points.Add(point);
        }

        public bool MightContain(Vector3 point, float maxDistance)
        {
            // Early rejection using AABB
            return points.Count == 0 || bounds.SqrDistance(point) <= maxDistance * maxDistance;
        }
    }

    // Store the clusters for efficient access
    [SerializeField]
    private List<Cluster> clusters = new List<Cluster>();

    /// <summary>
    /// Adds a "Recompute" option to the context menu
    /// </summary>
    [ContextMenu("Recompute Clusters")]
    private void Recompute()
    {
        ExtractVerticesAndCluster();
    }

    /// <summary>
    /// Extract vertices from all drawables and cluster them
    /// </summary>
    private void ExtractVerticesAndCluster()
    {
        var extractedVertices = ExtractVertices().ToList();
        PerformClustering(extractedVertices);
    }

    /// <summary>
    /// Extract vertices from all drawables and return them as a list
    /// </summary>
    /// <returns>List of vertex positions in world space</returns>
    private IEnumerable<Vector2> ExtractVertices()
    {
        var result = Enumerable.Empty<Vector2>();

        if (drawables == null)
            return result;

        // Extract all vertices
        foreach (var drawable in drawables)
        {
            if (drawable == null)
                continue;

            result = result.Concat(
                drawable.VertexPositions.Select(
                    v => (Vector2)drawable.transform.TransformPoint(v)
                )
            );
        }

        return result;
    }

    /// <summary>
    /// Cluster the vertices based on distance
    /// </summary>
    private void PerformClustering(List<Vector2> vertices)
    {
        clusters.Clear();

        if (vertices.Count == 0)
            return;

        PerformDBSCANClustering(vertices);
    }

    /// <summary>
    /// Performs DBSCAN clustering algorithm
    /// </summary>
    private void PerformDBSCANClustering(List<Vector2> vertices)
    {
        // Build a kd-tree for faster neighbor lookups
        KDTree kdTree = new KDTree(vertices);

        // Track which points have been visited and their cluster assignment
        bool[] visited = new bool[vertices.Count];
        int[] clusterAssignment = new int[vertices.Count];
        for (int i = 0; i < clusterAssignment.Length; i++)
        {
            clusterAssignment[i] = -1; // -1 = unassigned
        }

        int clusterIndex = 0;

        // Process each point
        for (int i = 0; i < vertices.Count; i++)
        {
            // Skip if already visited
            if (visited[i])
                continue;

            visited[i] = true;

            // Get neighbors within maxClusterDistance
            List<int> neighbors = kdTree.GetNearbyPoints(i, maxClusterDistance);

            // If this point doesn't have enough neighbors, mark as noise
            if (neighbors.Count < 1)
                continue;

            // Start a new cluster
            clusterAssignment[i] = clusterIndex;
            clusters.Add(new Cluster());
            // Convert back to Vector3 with z=0 for existing code compatibility
            clusters[clusterIndex].AddPoint(new Vector3(vertices[i].x, vertices[i].y, 0));

            // Add all neighbors to a queue for processing
            Queue<int> queue = new Queue<int>(neighbors);

            // Process all reachable points
            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();

                // Skip if already visited
                if (visited[currentIndex])
                    continue;

                visited[currentIndex] = true;

                // Get neighbors of this point
                List<int> currentNeighbors = kdTree.GetNearbyPoints(currentIndex, maxClusterDistance);

                // If this is a core point, add its unvisited neighbors to the queue
                if (currentNeighbors.Count >= 1)
                {
                    foreach (int neighborIndex in currentNeighbors)
                    {
                        if (!visited[neighborIndex])
                        {
                            queue.Enqueue(neighborIndex);
                        }
                    }
                }

                // Add this point to the current cluster if not already assigned
                if (clusterAssignment[currentIndex] == -1)
                {
                    clusterAssignment[currentIndex] = clusterIndex;
                    // Convert back to Vector3 with z=0 for existing code compatibility
                    clusters[clusterIndex].AddPoint(new Vector3(vertices[currentIndex].x, vertices[currentIndex].y, 0));
                }
            }

            clusterIndex++;
        }

        // Remove any empty clusters (shouldn't happen, but just in case)
        clusters.RemoveAll(c => c.points.Count == 0);

        // Perform a final merge for any clusters that should be combined
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
        // First do a quick AABB check
        if (!a.bounds.Intersects(b.bounds))
            return false;

        // Now do the detailed check
        foreach (var pointA in a.points)
        {
            if (!b.MightContain(pointA, maxClusterDistance))
                continue;

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
        if (!enabled || clusters == null || clusters.Count == 0 || !showClusters)
            return;

        // Draw each cluster with its own color based on index
        for (int i = 0; i < clusters.Count; i++)
        {
            // Compute color based on cluster index
            // Hue is determined by cluster index, saturation and value are max (1.0)
            Color clusterColor = Color.HSVToRGB(
                (float)i / Mathf.Max(1, clusters.Count),  // Scale hue by index
                1.0f,  // Maximum saturation
                1.0f   // Maximum value
            );

            Gizmos.color = clusterColor;

            // Draw each vertex in the cluster as a sphere
            foreach (var point in clusters[i].points)
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
