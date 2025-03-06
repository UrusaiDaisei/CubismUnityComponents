using UnityEngine;
using System.Collections.Generic;

public static class PolygonClipper
{
    // Threshold for floating point comparisons
    private const float EPSILON = 0.00001f;

    // Internal struct to store polygon data
    private struct PolygonData
    {
        public Vector2[] Points;
        public float Area;
        public Rect Bounds;
        public int GroupIndex;  // For tracking connected components

        public PolygonData(Vector2[] points)
        {
            Points = points;
            Area = CalculateArea(points);
            Bounds = CalculateBounds(points);
            GroupIndex = -1;  // Unassigned initially
        }
    }

    public static List<Vector2[]> UnionPolygons(List<Vector2[]> polygons)
    {
        // Keep trivial cases intact
        if (polygons == null || polygons.Count == 0)
            return new List<Vector2[]>();

        if (polygons.Count == 1)
            return new List<Vector2[]> { polygons[0] };

        // Calculate area and bounds for each polygon
        List<PolygonData> polygonDataList = new List<PolygonData>();
        foreach (var polygon in polygons)
        {
            if (polygon != null && polygon.Length >= 3)
            {
                polygonDataList.Add(new PolygonData(polygon));
            }
        }

        // Return empty list if no valid polygons
        if (polygonDataList.Count == 0)
            return new List<Vector2[]>();

        // Perform broad phase collision detection and group polygons
        GroupPolygonsByOverlap(polygonDataList);

        // Find the maximum group index to know how many groups we have
        int maxGroupIndex = -1;
        foreach (var polygonData in polygonDataList)
        {
            maxGroupIndex = Mathf.Max(maxGroupIndex, polygonData.GroupIndex);
        }

        // Create a result list to hold the largest polygon from each group
        List<Vector2[]> result = new List<Vector2[]>();

        // For each group, find the polygon with the largest area
        for (int groupIndex = 0; groupIndex <= maxGroupIndex; groupIndex++)
        {
            float largestArea = 0f;
            Vector2[] largestPolygon = null;

            // Find largest polygon in this group
            foreach (var polygonData in polygonDataList)
            {
                if (polygonData.GroupIndex == groupIndex && polygonData.Area > largestArea)
                {
                    largestArea = polygonData.Area;
                    largestPolygon = polygonData.Points;
                }
            }

            // Add the largest polygon from this group to the result
            if (largestPolygon != null)
            {
                result.Add(largestPolygon);
            }
        }

        return result;
    }

    // Group polygons based on AABB overlap using a connected components algorithm
    private static void GroupPolygonsByOverlap(List<PolygonData> polygons)
    {
        // If no polygons or just one, assign to group 0 and return
        if (polygons.Count <= 1)
        {
            if (polygons.Count == 1)
            {
                polygons[0] = new PolygonData(polygons[0].Points) { GroupIndex = 0 };
            }
            return;
        }

        // Build adjacency list - for each polygon, which other polygons does it overlap with
        List<List<int>> adjacencyList = new List<List<int>>();

        for (int i = 0; i < polygons.Count; i++)
        {
            adjacencyList.Add(new List<int>());

            for (int j = 0; j < polygons.Count; j++)
            {
                if (i != j && polygons[i].Bounds.Overlaps(polygons[j].Bounds))
                {
                    adjacencyList[i].Add(j);
                }
            }
        }

        // Assign group indices using connected components (breadth-first search)
        int currentGroup = 0;
        bool[] visited = new bool[polygons.Count];

        for (int i = 0; i < polygons.Count; i++)
        {
            if (!visited[i])
            {
                // Start a new connected component
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;

                // Update the polygon's group index
                var polyData = polygons[i];
                polyData.GroupIndex = currentGroup;
                polygons[i] = polyData;

                // Process all connected polygons
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();

                    foreach (int neighbor in adjacencyList[current])
                    {
                        if (!visited[neighbor])
                        {
                            queue.Enqueue(neighbor);
                            visited[neighbor] = true;

                            // Update the neighbor's group index
                            var neighborData = polygons[neighbor];
                            neighborData.GroupIndex = currentGroup;
                            polygons[neighbor] = neighborData;
                        }
                    }
                }

                // Move to the next group
                currentGroup++;
            }
        }
    }

    // Calculate the area of a polygon using the Shoelace formula
    private static float CalculateArea(Vector2[] polygon)
    {
        float area = 0f;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            area += (polygon[j].x + polygon[i].x) * (polygon[j].y - polygon[i].y);
            j = i;
        }

        return Mathf.Abs(area / 2f);
    }

    // Calculate the AABB (Axis-Aligned Bounding Box) of a polygon
    private static Rect CalculateBounds(Vector2[] polygon)
    {
        if (polygon == null || polygon.Length == 0)
            return new Rect(0, 0, 0, 0);

        float minX = polygon[0].x;
        float minY = polygon[0].y;
        float maxX = polygon[0].x;
        float maxY = polygon[0].y;

        for (int i = 1; i < polygon.Length; i++)
        {
            minX = Mathf.Min(minX, polygon[i].x);
            minY = Mathf.Min(minY, polygon[i].y);
            maxX = Mathf.Max(maxX, polygon[i].x);
            maxY = Mathf.Max(maxY, polygon[i].y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}