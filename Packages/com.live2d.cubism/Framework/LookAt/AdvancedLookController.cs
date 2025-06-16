using Live2D.Cubism.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Live2D.Cubism.Framework.LookAt
{
    /// <summary>
    /// Advanced look controller that handles parameters for a specific look index.
    /// </summary>
    public sealed class AdvancedLookController : MonoBehaviour, ICubismUpdatable
    {
        /// <summary>
        /// The look index this controller is responsible for.
        /// </summary>
        [SerializeField]
        public int LookIndex;

        /// <summary>
        /// Blend mode for all parameters.
        /// </summary>
        [SerializeField]
        public CubismParameterBlendMode BlendMode = CubismParameterBlendMode.Additive;

        /// <summary>
        /// Global blend factor.
        /// </summary>
        [SerializeField]
        public float GlobalBlendFactor = 1.0f;

        /// <summary>
        /// Parameters that can influence the blend factor.
        /// </summary>
        [SerializeField]
        public CubismParameter[] BlendFactorParameters;

        /// <summary>
        /// Whether to invert each blend factor parameter's influence.
        /// </summary>
        [SerializeField]
        public bool[] InvertBlendFactors;

        /// <summary>
        /// Damping applied to movement.
        /// </summary>
        [SerializeField]
        public float Damping = 0.15f;

        /// <summary>
        /// Target object implementing IAdvancedLookTarget.
        /// </summary>
        [SerializeField]
        private Object _target;

        /// <summary>
        /// Target accessor with interface check.
        /// </summary>
        public Object Target
        {
            get { return _target; }
            set { _target = value.ToNullUnlessImplementsInterface<IAdvancedLookTarget>(); }
        }

        private IAdvancedLookTarget _targetInterface;
        private IAdvancedLookTarget TargetInterface
        {
            get
            {
                if (_targetInterface == null)
                {
                    _targetInterface = Target.GetInterface<IAdvancedLookTarget>();
                }
                return _targetInterface;
            }
        }

        /// <summary>
        /// Center transform for look calculations.
        /// </summary>
        public Transform Center;

        /// <summary>
        /// Forward direction for look calculations.
        /// </summary>
        public Vector3 ForwardDirection = Vector3.forward;

        /// <summary>
        /// Up direction for look calculations.
        /// </summary>
        public Vector3 UpDirection = Vector3.up;

        // Internal state
        private List<AdvancedLookParameter> _parameters;
        private Vector3 _lastPosition;
        private Vector3 _goalPosition;
        private Vector3 _velocityBuffer;

        [HideInInspector]
        public bool HasUpdateController { get; set; }

        /// <summary>
        /// Called by cubism update controller. Order to invoke OnLateUpdate.
        /// </summary>
        public int ExecutionOrder
        {
            get { return CubismUpdateExecutionOrder.CubismLookController; }
        }

        /// <summary>
        /// Called by cubism update controller. Needs to invoke OnLateUpdate on Editing.
        /// </summary>
        public bool NeedsUpdateOnEditing
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes the controller.
        /// </summary>
        private void Start()
        {
            if (Center == null)
            {
                Center = transform;
            }

            // Initialize blend factor arrays if needed
            if (BlendFactorParameters != null && InvertBlendFactors == null)
            {
                InvertBlendFactors = new bool[BlendFactorParameters.Length];
            }

            Refresh();
        }

        /// <summary>
        /// Refreshes the controller's parameters.
        /// </summary>
        public void Refresh()
        {
            var model = this.FindCubismModel();

            // Get all parameters for this look index
            _parameters = model.Parameters
                .GetComponentsMany<AdvancedLookParameter>()
                .Where(p => p.LookIndex == LookIndex)
                .ToList();

            HasUpdateController = (GetComponent<CubismUpdateController>() != null);
        }

        /// <summary>
        /// Calculates the current blend factor based on parameter values.
        /// </summary>
        private float CalculateBlendFactor()
        {
            if (BlendFactorParameters == null || BlendFactorParameters.Length == 0)
            {
                return GlobalBlendFactor;
            }

            float factor = GlobalBlendFactor;
            for (int i = 0; i < BlendFactorParameters.Length; i++)
            {
                var param = BlendFactorParameters[i];
                if (param == null) continue;

                var value = Mathf.InverseLerp(param.MinimumValue, param.MaximumValue, param.Value);
                if (InvertBlendFactors[i])
                {
                    value = 1 - value;
                }
                factor *= value;
            }

            return factor;
        }

        /// <summary>
        /// Updates look parameters.
        /// </summary>
        public void OnLateUpdate()
        {
            if (!enabled || _parameters == null || _parameters.Count == 0)
            {
                return;
            }

            var target = TargetInterface;
            if (target == null || !target.IsActive())
            {
                return;
            }

            // Check if this look index is available
            if (!target.GetAvailableLookIndices().Contains(LookIndex))
            {
                return;
            }

            // Get target position and transform to local space
            var targetPosition = target.GetLookTargetPosition(LookIndex);
            _goalPosition = transform.InverseTransformPoint(targetPosition) - Center.localPosition;

            // Apply damping
            var currentPosition = Vector3.SmoothDamp(
                _lastPosition,
                _goalPosition,
                ref _velocityBuffer,
                Damping
            );

            // Calculate current blend factor
            var blendFactor = CalculateBlendFactor();

            // Update parameters
            foreach (var parameter in _parameters)
            {
                var value = parameter.Evaluate(currentPosition, ForwardDirection, UpDirection);
                value *= blendFactor;
                parameter.Parameter?.BlendToValue(BlendMode, value);
            }

            _lastPosition = currentPosition;
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void LateUpdate()
        {
            if (!HasUpdateController)
            {
                OnLateUpdate();
            }
        }
    }
}