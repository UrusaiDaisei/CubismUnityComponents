using UnityEngine;
using Live2D.Cubism.Framework.Physics;

[RequireComponent(typeof(CubismPhysicsController))]
public sealed class Live2DPhysicsDriver : MonoBehaviour
{
    #region Fields

    [Header("Core Settings")]
    [Tooltip("Base gravity vector in world space - this will be automatically rotated with the transform")]
    public Vector2 baseGravity = Vector2.down;
    
    [Tooltip("How quickly the physics responds to movement (0 = very smooth but delayed, 1 = immediate but sharp)")]
    [Range(0f, 1f)]
    public float responsiveness = 0.5f;

    [Header("Movement Forces")]
    [Tooltip("Enable physics response to position changes")]
    public bool enableTranslation = true;
    
    [Tooltip("Multiplier for movement-based forces (X = horizontal, Y = vertical)")]
    public Vector2 translationScale = Vector2.one;

    [Header("Rotation Forces")]
    [Tooltip("Enable physics response to rotation changes")]
    public bool enableRotation = true;
    
    [Tooltip("Multiplier for rotation-based forces")]
    public float rotationScale = 1f;

    [Header("Momentum Settings")]
    [Space(10)]
    [Tooltip("How much movement converts to lingering momentum (0 = no momentum, 1 = full momentum)")]
    [Range(0f, 1f)]
    public float linearMomentumGain = 0.5f;
    
    [Tooltip("How much rotation converts to lingering momentum (0 = no momentum, 1 = full momentum)")]
    [Range(0f, 1f)]
    public float angularMomentumGain = 0.5f;
    
    [Space(5)]
    [Tooltip("How quickly movement momentum dissipates (higher = faster decay)")]
    [Range(0f, 10f)]
    public float linearMomentumDecay = 2f;
    
    [Tooltip("How quickly rotation momentum dissipates (higher = faster decay)")]
    [Range(0f, 10f)]
    public float angularMomentumDecay = 2f;

    #endregion

    #region Runtime

    private CubismPhysicsController _physicsController;
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private Vector2 _linearMomentum;
    private float _angularMomentum;

    #endregion

    #region Unity Methods

    private void Start()
    {
        _physicsController = GetComponent<CubismPhysicsController>();
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        _linearMomentum = Vector2.zero;
        _angularMomentum = 0f;
        
        // Ensure the physics controller is initialized
        if (_physicsController.Rig == null)
        {
            Debug.LogWarning("Live2DPhysicsDriver: No physics rig found on the CubismPhysicsController.");
            return;
        }

        // Initialize the rig with base gravity
        _physicsController.Rig.Gravity = baseGravity;
    }

    private void LateUpdate()
    {
        if (_physicsController == null || _physicsController.Rig == null) return;

        float deltaTime = Time.deltaTime;

        // Calculate position and rotation deltas in world space
        Vector3 worldPositionDelta = (transform.position - _lastPosition) * responsiveness;
        Quaternion worldRotationDelta = Quaternion.Lerp(Quaternion.identity, 
            transform.rotation * Quaternion.Inverse(_lastRotation), 
            responsiveness);

        // Convert world space position delta to local space
        Vector3 localPositionDelta = transform.InverseTransformVector(worldPositionDelta);

        // Initialize combined force
        Vector2 totalForce = Vector2.zero;

        UpdateTranslationForces(ref totalForce, localPositionDelta, deltaTime);
        UpdateRotationForces(ref totalForce, worldRotationDelta, deltaTime);

        // Apply combined forces through wind
        _physicsController.Rig.Wind = totalForce;

        // Store current transform state for next frame
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }

    #endregion

    #region Auxiliary Code

    private void UpdateTranslationForces(ref Vector2 totalForce, Vector3 localPositionDelta, float deltaTime)
    {
        if (!enableTranslation) return;

        // Add new momentum from movement
        _linearMomentum += new Vector2(
            localPositionDelta.x * linearMomentumGain,
            localPositionDelta.y * linearMomentumGain
        );
        
        // Apply decay to momentum
        _linearMomentum *= Mathf.Exp(-linearMomentumDecay * deltaTime);
        
        // Add linear force to total force
        totalForce += new Vector2(
            (localPositionDelta.x + _linearMomentum.x) * translationScale.x,
            (localPositionDelta.y + _linearMomentum.y) * translationScale.y
        );
    }

    private void UpdateRotationForces(ref Vector2 totalForce, Quaternion worldRotationDelta, float deltaTime)
    {
        if (!enableRotation) return;

        // Extract the euler angle we care about (Z for 2D rotation)
        float rotationDeltaZ = worldRotationDelta.eulerAngles.z;
        if (rotationDeltaZ > 180f) rotationDeltaZ -= 360f;
        
        // Add new angular momentum
        _angularMomentum += rotationDeltaZ * angularMomentumGain;
        
        // Apply decay to angular momentum
        _angularMomentum *= Mathf.Exp(-angularMomentumDecay * deltaTime);

        // Combined rotation effect (immediate + momentum)
        float totalRotation = (rotationDeltaZ + _angularMomentum) * rotationScale;
        
        // Convert rotation to tangential force
        // The force direction is perpendicular to the position vector
        // For 2D rotation around Z, the tangential force is (-y, x) normalized
        Vector2 tangentialForce = new Vector2(
            -Mathf.Sin(totalRotation * Mathf.Deg2Rad),
            Mathf.Cos(totalRotation * Mathf.Deg2Rad)
        ) * Mathf.Abs(totalRotation);

        // Add rotational force to total force
        totalForce += tangentialForce;

        // Update gravity based on current transform
        _physicsController.Rig.Gravity = (Vector2)(transform.worldToLocalMatrix.MultiplyVector(baseGravity));
    }

    #endregion
} 