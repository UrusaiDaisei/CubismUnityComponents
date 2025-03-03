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
            public VertexReference[] vertexReferences;  // Array of vertex references
            public float radius;                        // Influence radius for this point
        }

        #endregion

        #region Fields

        [SerializeField]
        internal CubismDrawable[] includedDrawables = new CubismDrawable[0];

        [SerializeField]
        internal TrackedPoint[] trackedPoints = { };

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

        #endregion

        /// <summary>
        /// Gets the current calculated position for a tracked point.
        /// </summary>
        public Vector3 GetTrackedPosition(int pointIndex)
        {
            var point = trackedPoints[pointIndex];

            // Calculate from vertex references
            Vector3 result = Vector3.zero;
            float totalWeight = 0;

            for (int i = 0; i < point.vertexReferences.Length; i++)
            {
                var vertexRef = point.vertexReferences[i];
                var drawable = includedDrawables[vertexRef.drawableIndex];
                var position = drawable.VertexPositions[vertexRef.vertexIndex];

                result += position * vertexRef.weight;
                totalWeight += vertexRef.weight;
            }

            // Return the weighted average or zero if no valid references
            return totalWeight > 0 ? result / totalWeight : Vector3.zero;
        }
    }
}