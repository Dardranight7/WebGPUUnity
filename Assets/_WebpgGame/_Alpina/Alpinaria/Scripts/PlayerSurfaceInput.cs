using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class PlayerSurfaceInput : MonoBehaviour
{
    // -----------------------------------------------------------------------
    //                             CONFIGURATION
    // -----------------------------------------------------------------------
    [SerializeField] private GameObject testSphere;
    [Header("Movement Configuration")]
    [SerializeField] private float moveSpeed = 5f; // Lateral movement speed
    [SerializeField] private float forwardSpeed = 8f; // Forward speed along the spline
    [SerializeField] private float maxSpeed = 10f; // Maximum allowed velocity
    [SerializeField] private float drag = 2f; // Linear Damping for Rigidbody

    [Header("Movement VFX")] 
    [SerializeField] private ParticleSystem rightSnowTrail;
    [SerializeField] private ParticleSystem leftSnowTrail;
    
    [Header("Spline Configuration")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float tobogganWidth = 3f; // Width of the path
    [SerializeField] private float heightOffset = 0.5f; // Height above the spline
    [SerializeField] private bool useGravity = true; // Initial gravity setting
    
    [Header("Grounding Configuration")]
    [Tooltip("La distancia que queremos mantener entre el personaje y el suelo.")]
    [SerializeField] private float desiredGroundDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastMaxDistance = 5f;
    [SerializeField] private GameObject visualModel;
    private bool drawGizmosOnHit = false;
    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothness = 10f;
    [SerializeField] private float positionSmoothness = 5f;
    
    [Header("Obstacle Interaction")]
    [SerializeField] private float stunTime = 0.5f; // Time the player is stunned
    [SerializeField] private float bounceForce = 5f; // Bounce force applied on hit
    [SerializeField] private ParticleSystem collisionFXPrefab;
    
    // -----------------------------------------------------------------------
    //                            COMPONENTS AND STATE
    // -----------------------------------------------------------------------
    
    private Rigidbody _rb;
    private ThirdPerson _inputActions; // Assuming 'ThirdPerson' is your Input Action Map name
    
    // Spline State Variables
    private float _splineProgress = 0f; // Current position on the spline (0 to 1)
    private float _lateralPosition = 0f; // Lateral position (-1 to 1)
    private float _horizontalInput = 0f; // Input value (-1 to 1)
    private float _initialLateralPosition = 0f;

    // Stun and State Variables
    [Header("State variables")]
    [SerializeField] private bool _canMove = false; // Controls if movement logic runs
    [SerializeField] private bool _isStunned = false; // Controls if input is blocked
    
    // PUBLIC PROPERTIES
    public float CurrentSplineProgress => _splineProgress;
    public bool IsFinished => _splineProgress >= 0.99f;
    public bool IsStunned => _isStunned;

    // -----------------------------------------------------------------------
    //                            UNITY LIFECYCLE
    // -----------------------------------------------------------------------

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        if (_rb == null)
        {
            Debug.LogError("Rigidbody is missing on " + gameObject.name + ". Disabling script.", this);
            enabled = false;
            return;
        }

        if (splineContainer == null)
        {
            Debug.LogError("SplineContainer is required for this lane-runner script. Disabling script.", this);
            enabled = false;
            return;
        }

        // Rigidbody setup
        _rb.freezeRotation = true; 
        _rb.linearDamping = drag;
        _rb.useGravity = useGravity;

        // Input setup
        _inputActions = new ThirdPerson();
        _inputActions.Enable();
        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;
    }

    void Start()
    {
        InitializeSplinePosition();
        OnRacePrepare(); // Set initial state (waiting for GO)
    }

    private void OnDestroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Disable();
        }
    }
    
    void FixedUpdate()
    {
        if (!_canMove)
        {
            // Keep Rigidbody stationary when waiting for state change
            if (_rb.linearVelocity != Vector3.zero) _rb.linearVelocity = Vector3.zero;
            return;
        }

        HandleSplineMovement();
    }
    
    // -----------------------------------------------------------------------
    //                                INPUT
    // -----------------------------------------------------------------------

    public void OnMove(InputAction.CallbackContext movement)
    {
        // Prevent input if stunned.
        if (_isStunned)
        {
            _horizontalInput = 0f;
            return;
        }
        Vector2 vector2 = movement.ReadValue<Vector2>();
        _horizontalInput = vector2.x;
    }

    // -----------------------------------------------------------------------
    //                            SPLINE MOVEMENT LOGIC
    // -----------------------------------------------------------------------

    void InitializeSplinePosition()
    {
        // Find the starting progress point
        _splineProgress = FindClosestProgress(transform.position);
        _initialLateralPosition = CalculateInitialLateralPosition();
        _lateralPosition = _initialLateralPosition; 

        // Teleport the player to the calculated position
        transform.position = CalculateSplinePosition();
        
        // Set the initial rotation
        FallbackRotation();
    }
    
    float CalculateInitialLateralPosition()
    {
        float3 splinePos = splineContainer.EvaluatePosition(_splineProgress);
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
    
        float3 right = math.normalize(math.cross(tangent, up));
    
        Vector3 offsetFromSpline = transform.position - (Vector3)splinePos;
        float lateralDistance = Vector3.Dot(offsetFromSpline, right);
    
        float normalizedLateralPos = lateralDistance / (tobogganWidth / 2f);
        return Mathf.Clamp(normalizedLateralPos, -1f, 1f);
    }
    void HandleSplineMovement()
    {
        // Forward Advance (Longitudinal)
        float splineLength = splineContainer.Spline.GetLength();
        float progressIncrement = (forwardSpeed * Time.fixedDeltaTime) / splineLength;
        _splineProgress += progressIncrement;
        _splineProgress = Mathf.Clamp01(_splineProgress);
        
        // Lateral Movement
        // Lateral input directly manipulates the lateral position on the track.
        _lateralPosition -= _horizontalInput * moveSpeed * Time.fixedDeltaTime;
        _lateralPosition = Mathf.Clamp(_lateralPosition, -1f, 1f);
        
        // Calculate Target Position
        Vector3 targetPosition = CalculateSplinePosition();
        
        // Move Rigidbody (Cinematic move with smoothing)
        _rb.MovePosition(Vector3.Lerp(_rb.position, targetPosition, Time.fixedDeltaTime * positionSmoothness));
        
        // Rotation & Velocity Limits
        AdjustRotation();
        LimitLateralVelocity();
        TriggerSnowTrailVfx(true);
    }
    
    Vector3 CalculateSplinePosition()
    {
        float3 splinePosition = splineContainer.EvaluatePosition(_splineProgress);
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        
        // Calculate the cross product to find the perpendicular 'right' vector
        float3 crossProduct = math.cross(tangent, up);
        float3 right;
        
        // Anti-NaN Check: Prevent division by zero if tangent and up are parallel (Fixes NaN error)
        if (math.lengthsq(crossProduct) < 0.0001f)
        {
            // Fallback to a safe vector (global right) if math fails
            right = new float3(1f, 0f, 0f); 
        }
        else
        {
            right = math.normalize(crossProduct);
        }

        // Calculate offsets
        float3 lateralOffset = right * _lateralPosition * (tobogganWidth / 2f);
        // Position in the track without height adjustment
        Vector3 trackPosition = (Vector3)splinePosition + (Vector3)lateralOffset + (Vector3)up * heightOffset;

        RaycastHit hit;
        // Raycast start point
        Vector3 rayStart = transform.position + transform.up * 2;
        // Raycast Lenght
        Vector3 rayDirection = transform.up * -1;

        // Launch raycast at needed direction(Down)
        if (Physics.Raycast(rayStart, rayDirection, out hit, float.PositiveInfinity, groundLayer))
        {
            // Actual Character position it's the y position
            Debug.DrawRay(rayStart, rayDirection, Color.forestGreen, 0.1f);
            
            testSphere.transform.position = hit.point;

            visualModel.transform.position = hit.point;

            // look at TANGENT, with UP alaingned to HIT.NORMAL.
            Quaternion targetRotation = Quaternion.LookRotation((Vector3)tangent, hit.normal);
            // Apply smooth rotation
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));
        }
        else
        {
            Debug.DrawRay(rayStart, rayDirection, Color.darkRed, 0.1f);
        }

            return trackPosition;
    }
    void FallbackRotation()
    {
        // Rotación de respaldo: solo usa el UP del spline.
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        
        Quaternion targetRotation = Quaternion.LookRotation(tangent, up);
        
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));
    }
    void AdjustRotation()
    {
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        
        Quaternion targetRotation = Quaternion.LookRotation(tangent, up);
        
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));
    }
    
    void LimitLateralVelocity()
    {
        // Limits the actual velocity component that is lateral to the track.
        Vector3 right = transform.right;
        float lateralVelocity = Vector3.Dot(_rb.linearVelocity, right);
        
        if (Mathf.Abs(lateralVelocity) > maxSpeed)
        {
            Vector3 lateralVector = right * Mathf.Sign(lateralVelocity) * maxSpeed;
            Vector3 otherVelocity = _rb.linearVelocity - (right * lateralVelocity);
            _rb.linearVelocity = lateralVector + otherVelocity;
        }
    }
    
    float FindClosestProgress(Vector3 position)
    {
        float bestDistance = float.MaxValue;
        float bestProgress = 0f;
        
        // Search the spline for the closest point
        for (float t = 0; t <= 1f; t += 0.01f)
        {
            Vector3 splinePoint = splineContainer.EvaluatePosition(t);
            float distance = Vector3.Distance(position, splinePoint);
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestProgress = t;
            }
        }
        return bestProgress;
    }
    
    // -----------------------------------------------------------------------
    //                             RACE STATE & STUN
    // -----------------------------------------------------------------------

    public void OnRacePrepare()
    {
        _canMove = false;
        _isStunned = true; // Block input while preparing
        _rb.linearVelocity = Vector3.zero;
        _rb.useGravity = false; // Prevent sliding before start
    }

    public void OnRaceStart()
    {
        _lateralPosition = _initialLateralPosition; 
        _splineProgress = 0f; // Solo reseteamos el progreso.

        // Llamada a la función base (que ahora solo usa el estado).
        StartRaceFromInitialPosition(); 
    
        // Resetear input y flags
        _horizontalInput = 0f; 
        _canMove = true;
        _isStunned = false; 
        _rb.useGravity = useGravity;
    }

    public void OnRaceStop()
    {
        _canMove = false;
        _rb.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// Applies a slowdown and a brief bounce back upon hitting an obstacle.
    /// </summary>
    public void HitObstacle(float forwardSlowdownFactor = 0.5f)
    {
        if (_isStunned) return;

        TriggerSnowTrailVfx(false);
        
        // Reduce forward speed
        forwardSpeed *= forwardSlowdownFactor;
        forwardSpeed = Mathf.Max(1f, forwardSpeed); 

        // Calculate bounce direction (opposite of spline tangent)
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        Vector3 bounceDirection = -tangent; 
        
        // 1. Stop current velocity
        _rb.linearVelocity = Vector3.zero; 
        
        // 2. Apply bounce force (Impulse mode is best for sudden impacts)
        _rb.AddForce(bounceDirection.normalized * bounceForce, ForceMode.Impulse);
        
        if (collisionFXPrefab != null)
        {
            ParticleSystem fx = Instantiate(collisionFXPrefab, transform.position, Quaternion.identity); 
            Destroy(fx.gameObject, fx.main.duration); 
        }
        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine()
    {
        _isStunned = true;
        yield return new WaitForSeconds(stunTime);
        _isStunned = false;
    }
    void StartRaceFromInitialPosition()
    {
        transform.position = CalculateSplinePosition(); 
        AdjustRotation();
    }
    public void ResetOnSpline()
    {
        _splineProgress = 0f;
        _lateralPosition = 0f;
        transform.position = CalculateSplinePosition();
        AdjustRotation();
    }

    private void TriggerSnowTrailVfx(bool play = true)
    {
        if (play)
        {
            if(rightSnowTrail.isPlaying) return;
            rightSnowTrail.Play();
            leftSnowTrail.Play();
        }
        else
        {
            rightSnowTrail.Stop();
            leftSnowTrail.Stop();
        }
    }

    // -----------------------------------------------------------------------
    //                                GIZMOS
    // -----------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (splineContainer == null || !Application.isPlaying) return;

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(CalculateSplinePosition(), 0.3f);
        
        // Draw lateral limits
        float3 splinePosition = splineContainer.EvaluatePosition(_splineProgress);
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        float3 right = math.normalize(math.cross(tangent, up));

        Gizmos.color = Color.red;
        Vector3 leftBoundary = splinePosition - right * (tobogganWidth / 2f);
        Vector3 rightBoundary = splinePosition + right * (tobogganWidth / 2f);
        
        Gizmos.DrawLine(leftBoundary, leftBoundary + (Vector3)up * 2f);
        Gizmos.DrawLine(rightBoundary, rightBoundary + (Vector3)up * 2f);
    }
}