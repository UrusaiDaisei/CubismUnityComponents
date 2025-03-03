using UnityEngine;
using Live2D.Cubism.Core;
using System;

namespace Live2D.Cubism.Framework
{
    /// <summary>
    /// Tracks deformations in a Live2D model by using direct vertex references.
    /// </summary>
    public sealed class PointDeformationTracker : MonoBehaviour
    {
        /// <summary>
        /// Maximum number of vertex references per point.
        /// </summary>
        internal const int MAX_TOTAL_VERTICES = 6;

        #region Data Structures

        [Serializable]
        internal struct VertexReference
        {
            public int drawableIndex;          // Index into includedDrawables list
            public int vertexIndex;            // Index of vertex within the drawable
            public float weight;               // Influence weight for this vertex
        }

        [Serializable]
        internal struct TrackedPoint
        {
            public int vertexReferencesStartIndex;  // Start index for this point's vertex references in the vertexReferences array
            public float radius;                    // Influence radius for this point
        }

        #endregion

        #region Fields

        [SerializeField]
        internal CubismDrawable[] includedDrawables = new CubismDrawable[0];

        [SerializeField]
        internal TrackedPoint[] trackedPoints = { };

        [SerializeField]
        internal VertexReference[] vertexReferences = { };

        #endregion

        #region Internal State

        private CubismModel _model;
        internal CubismModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = GetComponentInParent<CubismModel>();
                    if (_model == null)
                        throw new Exception("Could not find CubismModel in this object or its parents!");
                }
                return _model;
            }
        }

        private Transform _cachedTransform;
        private Transform CachedTransform
        {
            get
            {
                return _cachedTransform ??= transform;
            }
        }

        #endregion

        /// <summary>
        /// Gets the current calculated position for a tracked point.
        /// </summary>
        public Vector3 GetLocalTrackedPosition(int pointIndex)
        {
            // Bounds check - will be stripped in build
            Debug.AssertFormat(pointIndex >= 0 && pointIndex < trackedPoints.Length,
                             "PointIndex {0} is out of bounds. Valid range: 0-{1}",
                             pointIndex, trackedPoints.Length - 1);

            var point = trackedPoints[pointIndex];

            // Create a span over the vertex references needed for this point
            ReadOnlySpan<VertexReference> vertexRefs = vertexReferences
                .AsSpan(point.vertexReferencesStartIndex, MAX_TOTAL_VERTICES);

            // Calculate from vertex references
            Vector3 result = Vector3.zero;
            float totalWeight = 0;

            // Iterate through the span
            foreach (var vertexRef in vertexRefs)
            {
                var drawable = includedDrawables[vertexRef.drawableIndex];
                var position = drawable.VertexPositions[vertexRef.vertexIndex];
                result += position * vertexRef.weight;
                totalWeight += vertexRef.weight;
            }

            // Due to the way the weights are calculated,
            // their sum will always be not zero
            return result / totalWeight;
        }

        public Vector3 GetWorldTrackedPosition(int pointIndex)
        {
            return CachedTransform.TransformPoint(GetLocalTrackedPosition(pointIndex));
        }
    }
}