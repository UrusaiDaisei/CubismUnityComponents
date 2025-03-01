using UnityEngine;
using Live2D.Cubism.Core;
using System.Runtime.CompilerServices;
using System;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

/// <summary>
/// Tracks deformations in a Live2D model by using barycentric coordinates of specific points.
/// </summary>
public class Live2DDeformationTracker : MonoBehaviour
{
    #region Data Structures

    [System.Serializable]
    internal struct TrackedPoint
    {
        [HideInInspector] public BarycentricData trackingData;
    }

    [System.Serializable]
    internal unsafe struct BarycentricData
    {
        public fixed int vertexIndices[3]; // The three vertices forming the triangle
        public Vector3 weights; // Barycentric coordinates

        public BarycentricData(Vector3 weights, int v1, int v2, int v3)
        {
            this.weights = weights;
            this.vertexIndices[0] = v1;
            this.vertexIndices[1] = v2;
            this.vertexIndices[2] = v3;
        }
    }

    #endregion

    #region Fields

    [Header("Target Settings")]
    [Tooltip("The drawable to track")]
    public CubismDrawable targetDrawable;

    [Header("Tracked Points")]
    [SerializeField]
    internal TrackedPoint[] trackedPoints = { };

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

    #endregion



    #region Private Methods

    private void Initialize()
    {
        // Find model in this object or parents
        _model = GetComponentInParent<CubismModel>();

        if (_model == null)
        {
            Debug.LogError("Could not find CubismModel in this object or its parents!");
            return;
        }

        if (targetDrawable == null)
        {
            Debug.LogError("No target drawable assigned!");
            return;
        }

        // Initialize positions array
        _currentPositions = new Vector3[trackedPoints.Length];

        // Subscribe to drawable updates
        targetDrawable.VertexPositionsDidChange.AddListener(OnVertexPositionsChanged);
    }

    private void OnDestroy()
    {
        if (targetDrawable != null)
        {
            targetDrawable.VertexPositionsDidChange.RemoveListener(OnVertexPositionsChanged);
        }
    }

    private void OnVertexPositionsChanged(CubismDrawable drawable)
    {
        UpdateTrackedPoints();
    }

    private void UpdateTrackedPoints()
    {
        if (targetDrawable == null) return;

        var currentVertices = targetDrawable.VertexPositions.AsSpan();

        for (int i = 0; i < trackedPoints.Length; i++)
        {
            _currentPositions[i] = CalculateWeightedPosition(i, currentVertices);
        }
    }

    internal Vector3 CalculateWeightedPosition(int pointIndex)
        => CalculateWeightedPosition(pointIndex, targetDrawable.VertexPositions);

    private unsafe Vector3 CalculateWeightedPosition(int pointIndex, Span<Vector3> vertices)
    {
        Vector3 position = Vector3.zero;
        var point = trackedPoints[pointIndex];
        var indices = point.trackingData.vertexIndices;
        var weights = point.trackingData.weights;

        for (int j = 0; j < 3; j++)
        {
            position += vertices[indices[j]] * weights[j];
        }

        return position;
    }

    public Vector3 GetCurrentPosition(int index)
    {
        if (index < 0 || index >= _currentPositions.Length)
            return Vector3.zero;
        return _currentPositions[index];
    }

    #endregion
}