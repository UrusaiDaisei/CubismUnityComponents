using System;
using Live2D.Cubism.Core;
using UnityEngine;

namespace Live2D.Cubism.Framework.LookAt
{
    /// <summary>
    /// Advanced look parameter that provides control over look behavior.
    /// </summary>
    [RequireComponent(typeof(CubismParameter))]
    public sealed class AdvancedLookParameter : MonoBehaviour
    {
        /// <summary>
        /// The look index this parameter is associated with.
        /// </summary>
        [SerializeField]
        public int LookIndex;

        /// <summary>
        /// Look axis.
        /// </summary>
        [SerializeField]
        public CubismLookAxis Axis;

        /// <summary>
        /// Minimum projection distance before this parameter starts responding.
        /// </summary>
        [SerializeField]
        public float MinDistance = -1.0f;

        /// <summary>
        /// Maximum projection distance where this parameter reaches its maximum value.
        /// </summary>
        [SerializeField]
        public float MaxDistance = 1.0f;

        /// <summary>
        /// Whether to invert the parameter mapping (min distance maps to max value and vice versa).
        /// </summary>
        [SerializeField]
        public bool Invert = false;

        /// <summary>
        /// The center point for look calculations in local coordinates relative to the model transform.
        /// </summary>
        [SerializeField]
        public Vector2 CenterPoint = Vector2.zero;

        private CubismParameter _parameter;
        /// <summary>
        /// The parameter this component controls.
        /// </summary>
        public CubismParameter Parameter
        {
            get
            {
                if (_parameter == null)
                {
                    _parameter = GetComponent<CubismParameter>();
                }
                return _parameter;
            }
        }

        private CubismModel _model;
        /// <summary>
        /// The model this parameter belongs to.
        /// </summary>
        public CubismModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = GetComponentInParent<CubismModel>();
                }
                return _model;
            }
        }

        /// <summary>
        /// Evaluates the parameter value based on target position and settings.
        /// </summary>
        public float Evaluate(Vector3 targetPosition, Vector3 forward, Vector3 up)
        {
            // Convert target position to local space
            var localTargetPosition = Model.transform.InverseTransformPoint(targetPosition);
            var directionToTarget = (Vector3)CenterPoint - localTargetPosition;

            // Get the axis direction based on the provided vectors
            var axisDirection = Axis switch
            {
                CubismLookAxis.X => Vector3.Cross(forward, up).normalized,
                CubismLookAxis.Y => up,
                CubismLookAxis.Z => forward,
                _ => Vector3.zero
            };

            // Calculate projection onto the axis
            float projection = Vector3.Dot(directionToTarget, axisDirection);

            // Clamp distance to our range
            float clampedDistance = Mathf.Clamp(projection, MinDistance, MaxDistance);

            // Normalize distance to 0-1 range and apply sign
            float normalizedDistance = Mathf.InverseLerp(MinDistance, MaxDistance, clampedDistance);

            // Apply inversion if needed
            if (Invert)
            {
                normalizedDistance = 1f - normalizedDistance;
            }

            // Remap to parameter's actual range
            return Mathf.Lerp(Parameter.MinimumValue, Parameter.MaximumValue, normalizedDistance);
        }
    }
}