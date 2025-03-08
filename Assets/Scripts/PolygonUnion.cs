using UnityEngine;
using System.Collections.Generic;
using Martinez;

public static class PolygonUnion
{
    public static Vector2[] UnionPolygons(Vector2[] polygon1, Vector2[] polygon2)
    {
        // Convert Unity Vector2 arrays to Martinez Polygon objects
        List<Polygon> subject = new List<Polygon> { ConvertToMartinezPolygon(polygon1) };
        List<Polygon> clipping = new List<Polygon> { ConvertToMartinezPolygon(polygon2) };

        // Create the Martinez clipper and perform union operation
        MartinezClipper clipper = new MartinezClipper();
        List<Polygon> result = clipper.Compute(subject, clipping, ClipType.Union);

        // If no result, return empty array
        if (result == null || result.Count == 0)
            return new Vector2[0];

        // Convert the result back to Unity Vector2 array
        // For simplicity, we're just taking the first polygon from the result
        // This should be expanded if handling multiple polygons or polygons with holes is needed
        return ConvertToVector2Array(result[0]);
    }

    // Helper method to convert Unity Vector2 array to Martinez Polygon
    private static Polygon ConvertToMartinezPolygon(Vector2[] points)
    {
        Polygon polygon = new Polygon(points.Length);
        polygon.AddComponent(); // Add exterior ring

        // Add points to the polygon
        for (int i = 0; i < points.Length; i++)
        {
            polygon.nodes.Add(points[i]);
        }

        polygon.AddComponent(); // Mark end of the component
        return polygon;
    }

    // Helper method to convert Martinez Polygon back to Unity Vector2 array
    private static Vector2[] ConvertToVector2Array(Polygon polygon)
    {
        // For simplicity, we're assuming the polygon has only one component (exterior ring)
        List<Vector2> result = new List<Vector2>();

        // If there are multiple components, we'd need to handle them appropriately
        // but for now, we'll just take all points
        for (int i = 0; i < polygon.nodes.Count; i++)
        {
            var point = polygon.nodes[i];
            result.Add(point);
        }

        return result.ToArray();
    }
}