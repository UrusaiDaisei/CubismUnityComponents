using UnityEngine;
using Live2D.Cubism.Core;
using System.Runtime.CompilerServices;
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
    internal struct BarycentricData
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

        var currentVertices = targetDrawable.VertexPositions;

        for (int i = 0; i < trackedPoints.Length; i++)
        {
            var point = trackedPoints[i];
            if (point.trackingData.vertexIndices == null) continue;

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

    public Vector3 GetCurrentPosition(int index)
    {
        if (index < 0 || index >= _currentPositions.Length)
            return Vector3.zero;
        return _currentPositions[index];
    }

    #endregion
}