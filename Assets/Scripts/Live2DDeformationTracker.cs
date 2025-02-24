using UnityEngine;
using Live2D.Cubism.Core;

/// <summary>
/// Tracks deformations in a Live2D model by using barycentric coordinates of specific points.
/// </summary>
[RequireComponent(typeof(CubismModel))]
public class Live2DDeformationTracker : MonoBehaviour
{
    #region Data Structures

    [System.Serializable]
    public class TrackedPoint
    {
        public Vector3 basePosition;
        [HideInInspector] public BarycentricData trackingData;
    }

    [System.Serializable]
    public class BarycentricData
    {
        public int[] vertexIndices; // The three vertices forming the triangle
        public Vector3 weights; // Barycentric coordinates
    }

    #endregion

    #region Fields

    [Header("Target Settings")]
    [Tooltip("The drawable to track")]
    public CubismDrawable targetDrawable;

    [Header("Tracked Points")]
    public TrackedPoint[] trackedPoints = { };

    private static readonly Color debugVertexColor = new Color(0f, 1f, 0f, 0.5f);
    private const float debugVertexSize = 0.01f;

    #endregion

    #region Internal State

    private CubismModel _model;
    private Vector3[] _currentPositions;

    #endregion

    #region Unity Methods

    private void Start()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        UpdateTrackedPoints();
    }

    private void OnDrawGizmosSelected()
    {
        if (targetDrawable == null)
            return;

        DrawDebugGizmos(targetDrawable.VertexPositions);
    }

    #endregion

    #region Context Menu Methods

    [ContextMenu("Initialize Tracking Data")]
    public void InitializeTrackingData()
    {
        if (!Application.isPlaying)
        {
            if (targetDrawable == null)
            {
                Debug.LogError("No target drawable assigned!");
                return;
            }

            foreach (var point in trackedPoints)
            {
                point.trackingData = CalculateBarycentricData(point.basePosition);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    #endregion

    #region Private Methods

    private void Initialize()
    {
        _model = GetComponent<CubismModel>();

        if (targetDrawable == null)
        {
            Debug.LogError("No target drawable assigned!");
            return;
        }

        // Initialize positions array
        _currentPositions = new Vector3[trackedPoints.Length];
        for (int i = 0; i < trackedPoints.Length; i++)
        {
            _currentPositions[i] = trackedPoints[i].basePosition;
        }
    }

    private void UpdateTrackedPoints()
    {
        if (targetDrawable == null) return;

        var currentVertices = targetDrawable.VertexPositions;

        for (int i = 0; i < trackedPoints.Length; i++)
        {
            var point = trackedPoints[i];
            if (point.trackingData == null) continue;

            var indices = point.trackingData.vertexIndices;
            var weights = point.trackingData.weights;

            // Calculate new position using barycentric coordinates
            Vector3 newPosition = Vector3.zero;
            for (int j = 0; j < 3; j++)
            {
                newPosition += currentVertices[indices[j]] * weights[j];
            }

            _currentPositions[i] = newPosition;
        }
    }

    private BarycentricData CalculateBarycentricData(Vector3 point)
    {
        float minDistance = float.MaxValue;
        BarycentricData bestData = null;

        var vertices = targetDrawable.VertexPositions;
        var indices = targetDrawable.Indices;

        // Process each triangle in the mesh
        for (int i = 0; i < indices.Length; i += 3)
        {
            int i1 = indices[i];
            int i2 = indices[i + 1];
            int i3 = indices[i + 2];

            var barycentricCoords = CalculateBarycentricCoordinates(
                point, vertices[i1], vertices[i2], vertices[i3]);

            // Check if point is inside triangle
            bool isInside = barycentricCoords.x >= 0 && barycentricCoords.y >= 0 &&
                           barycentricCoords.z >= 0 &&
                           (barycentricCoords.x + barycentricCoords.y + barycentricCoords.z <= 1.001f);

            if (isInside)
            {
                return new BarycentricData
                {
                    vertexIndices = new int[] { i1, i2, i3 },
                    weights = barycentricCoords
                };
            }

            // If not inside, store the closest triangle
            var testPoint = vertices[i1] * barycentricCoords.x +
                           vertices[i2] * barycentricCoords.y +
                           vertices[i3] * barycentricCoords.z;
            float distance = Vector3.Distance(point, testPoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestData = new BarycentricData
                {
                    vertexIndices = new int[] { i1, i2, i3 },
                    weights = barycentricCoords
                };
            }
        }

        return bestData;
    }

    private Vector3 CalculateBarycentricCoordinates(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a;
        Vector3 v1 = c - a;
        Vector3 v2 = point - a;

        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }

    private void DrawDebugGizmos(Vector3[] vertices)
    {
        // Store current gizmo color and matrix
        Color oldColor = Gizmos.color;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Set transform matrix
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw vertices
        Gizmos.color = debugVertexColor;
        foreach (var vertex in vertices)
        {
            Gizmos.DrawLine(
                vertex - Vector3.right * debugVertexSize,
                vertex + Vector3.right * debugVertexSize
            );
            Gizmos.DrawLine(
                vertex - Vector3.up * debugVertexSize,
                vertex + Vector3.up * debugVertexSize
            );
        }

        // Draw tracked points
        for (int i = 0; i < trackedPoints.Length; i++)
        {
            var point = trackedPoints[i];
            Vector3 basePosition = point.basePosition;
            Vector3 deformedPosition;

            // Get deformed position either from runtime or calculate it
            if (Application.isPlaying && _currentPositions != null)
            {
                deformedPosition = _currentPositions[i];
            }
            else if (point.trackingData != null)
            {
                // Calculate position using barycentric coordinates
                deformedPosition = Vector3.zero;
                var indices = point.trackingData.vertexIndices;
                var weights = point.trackingData.weights;

                for (int j = 0; j < 3; j++)
                {
                    deformedPosition += vertices[indices[j]] * weights[j];
                }
            }
            else
            {
                deformedPosition = basePosition;
            }

            // Draw base point in blue
            Gizmos.color = Color.blue;
            DrawPointCross(basePosition);

            // Draw deformed point in yellow
            Gizmos.color = Color.yellow;
            DrawPointCross(deformedPosition);

            // Draw connection line in white
            if (point.trackingData != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(basePosition, deformedPosition);
            }
        }

        // Restore gizmo color and matrix
        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }

    private void DrawPointCross(Vector3 position)
    {
        Gizmos.DrawLine(
            position - Vector3.right * debugVertexSize,
            position + Vector3.right * debugVertexSize
        );
        Gizmos.DrawLine(
            position - Vector3.up * debugVertexSize,
            position + Vector3.up * debugVertexSize
        );
    }

    #endregion
}