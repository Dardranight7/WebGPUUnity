using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class BotAlpinaria : MonoBehaviour
{
    // -----------------------------------------------------------------------
    //                             ENUMERATION
    // -----------------------------------------------------------------------
    public enum DifficultyType
    {
        Easy,
        Normal,
        Hard,
        Expert
    }
    
    // -----------------------------------------------------------------------
    //                             CONFIGURATION
    // -----------------------------------------------------------------------
    
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private RaceManager raceManager;
  
    [Header("Speed Configuration")]
    [SerializeField] private float baseSpeed = 7f;
    [SerializeField] private float minSpeed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float speedVariation = 2f;
    
    [Header("Lateral Movement Config")]
    [SerializeField] private float lateralSpeed = 3f;
    [SerializeField] private float trackWidth = 3f;
    [SerializeField] private float heightOffset = 0.5f;
    
    [Header("Bot Intelligence")]
    [SerializeField] private float agility = 5f; 
    [SerializeField] private float accuracy = 0.8f;
    [SerializeField] private float distancePrediction = 5f;
    [SerializeField] private DifficultyType difficulty = DifficultyType.Normal;
    
    [Header("Behavior Toggles")]
    [SerializeField] private bool edgeAvoidance = true;
    [SerializeField] private bool findOptimalLine = true;
    [SerializeField] private bool makeMistakes = true;
    [SerializeField] private bool avoidObstacles = true;
    
    [Header("Obstacle Detection")]
    [SerializeField] private float sphereCastRadius = 0.5f;
    [SerializeField] private float detectionDistance = 10f;
    [SerializeField] private LayerMask obstacleLayer;
    
    
    [Header("Obstacle Interaction")]
    [SerializeField] private float stunTime = 0.5f; 
    [SerializeField] private float bounceForce = 5f; 
    
    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothness = 10f;
    [SerializeField] private float positionSmoothness = 8f;
    
    [Header("Grounding Configuration (Raycast)")]
    [Tooltip("La distancia que queremos mantener entre el bot y el suelo.")]
    [SerializeField] private float desiredGroundDistance = 0.5f; 
    [SerializeField] private LayerMask groundLayer; 
    [SerializeField] private float raycastMaxDistance = 5f;
    
    // -----------------------------------------------------------------------
    //                             INTERNAL STATE
    // -----------------------------------------------------------------------
    
    private float _splineProgress = 0f;
    private float _lateralPosition = 0f;
    private float _currentSpeed;
    private float _nextErrorTime = 0f;
    private float _currentError = 0f;
    private float _personalityFactor;
    private float _lateralBias = 0f; 
    
    private float _preStunBaseSpeed; 
    
    private bool _canMove = false;
    private bool _isStunned = false; 
    private bool _isInvulnerable = false;
    // PUBLIC PROPERTIES
    public float GetProgress() => _splineProgress;
    public bool HasFinished() => _splineProgress >= 0.999f;
    public bool IsStunned => _isStunned;

    // -----------------------------------------------------------------------
    //                            UNITY LIFECYCLE
    // -----------------------------------------------------------------------

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        if (splineContainer == null || rb == null)
        {
            Debug.LogError($"Bot {gameObject.name} is missing a critical reference. Disabling script.");
            enabled = false;
            return;
        }
        
        rb.freezeRotation = true;
        rb.useGravity = false;

        InitializeBot();
        
        _canMove = false; 
    }

    void FixedUpdate()
    {
        // El bot no toma decisiones ni se mueve si no está en carrera o aturdido.
        if (!_canMove || _isStunned) return;
        
        MakeDecisions();
        AdvanceInSpline();
        UpdatePositionAndRotation();
    }

    // -----------------------------------------------------------------------
    //                            INITIALIZATION
    // -----------------------------------------------------------------------

    void InitializeBot()
    {
        _personalityFactor = Random.Range(0.7f, 1.3f);
        ConfigureDifficulty();
        _currentSpeed = Random.Range(minSpeed, maxSpeed); 
        
        InitializeSplinePosition();
    }

    void InitializeSplinePosition()
    {
        _splineProgress = FindNearestProgress(transform.position);
        _lateralPosition = CalculateInitialLateralPosition();
        
        transform.position = CalculateSplinePosition();
        
        AdjustRotation();
    }

    float CalculateInitialLateralPosition()
    {
        float3 splinePos = splineContainer.EvaluatePosition(_splineProgress);
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        
        float3 right = math.normalize(math.cross(tangent, up));
        
        Vector3 offsetFromSpline = transform.position - (Vector3)splinePos;
        float lateralDistance = Vector3.Dot(offsetFromSpline, right);
        
        float normalizedLateralPos = lateralDistance / (trackWidth / 2f);
        return Mathf.Clamp(normalizedLateralPos, -1f, 1f);
    }

    void ConfigureDifficulty()
    {
        switch (difficulty)
        {
            case DifficultyType.Easy:
                baseSpeed *= 0.8f;
                agility = 3f;
                accuracy = 0.6f;
                makeMistakes = true;
                break;
                
            case DifficultyType.Normal:
                baseSpeed *= 0.95f;
                agility = 5f;
                accuracy = 0.8f;
                makeMistakes = true;
                break;
                
            case DifficultyType.Hard:
                baseSpeed *= 1.1f;
                agility = 7f;
                accuracy = 0.9f;
                makeMistakes = false;
                break;
                
            case DifficultyType.Expert:
                baseSpeed *= 1.2f;
                agility = 10f;
                accuracy = 0.95f;
                makeMistakes = false;
                break;
        }
    }

    // -----------------------------------------------------------------------
    //                                AI LOGIC
    // -----------------------------------------------------------------------
    
    /// <summary>
    /// Make decisions of where the ebot needs to move
    /// </summary>
    void MakeDecisions()
    {
        float desiredMovement = 0f;
        
        if (avoidObstacles)
        {
            float avoidance = AvoidObstacles();
            if (avoidance != 0f)
            {
                desiredMovement = avoidance;
            }
        }
        // If needs to avoid obstacle, it prevents other movements
        if (desiredMovement == 0f)
        {
            if (findOptimalLine)
            {
                desiredMovement += CalculateOptimalLineMovement();
            }
            
            if (edgeAvoidance)
            {
                desiredMovement += AvoidEdgeMovement();
            }
        }
        
        if (makeMistakes)
        {
            desiredMovement += SimulateErrors();
        }
        
        desiredMovement += _lateralBias * 0.5f; 
        
        float finalMovement = desiredMovement * accuracy;
        
        _lateralPosition += finalMovement * lateralSpeed * Time.fixedDeltaTime;
        _lateralPosition = Mathf.Clamp(_lateralPosition, -1f, 1f);
    }

    /// <summary>
    /// Uses a SphereCast to detect obstacles in his way and avoid them.
    /// </summary>
    float AvoidObstacles()
    {
        Vector3 forward = transform.forward; 
        
        // Searchs for obstacle with a SphereCastRadius
        if (Physics.SphereCast(transform.position, sphereCastRadius, forward, out RaycastHit hit, detectionDistance, obstacleLayer, QueryTriggerInteraction.Collide))
        {
            float distancia = Vector3.Dot(transform.right, (hit.point - transform.position));
            
            if (distancia > 0)
            {
                return -2f;
            }
            return 2f;
        }
        // If there is no obstacles it sends 0
        return 0f;
    }
    /// <summary>
    /// Calculate optimal position to move around spline
    /// </summary>
    /// <returns></returns>
    float CalculateOptimalLineMovement()
    {
        float futureProgress = _splineProgress + (distancePrediction / splineContainer.Spline.GetLength());
        futureProgress = Mathf.Clamp01(futureProgress);
        
        float3 currentTangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 futureTangent = splineContainer.EvaluateTangent(futureProgress);
        
        Vector3 cross = Vector3.Cross(currentTangent, futureTangent);
        float curveIntensity = cross.magnitude;
        
        if (curveIntensity > 0.1f)
        {
            bool curveToTheRight = cross.y > 0;
            
            if (curveToTheRight)
            {
                return _lateralPosition > -0.5f ? -1f : 0f;
            }
            else
            {
                return _lateralPosition < 0.5f ? 1f : 0f;
            }
        }
        
        if (Mathf.Abs(_lateralPosition) > 0.2f)
        {
            return -Mathf.Sign(_lateralPosition) * 0.5f;
        }
        
        return 0f;
    }
    /// <summary>
    /// avoid to move always on the edge of the ramp
    /// </summary>
    /// <returns></returns>
    float AvoidEdgeMovement()
    {
        if (_lateralPosition > 0.8f)
        {
            return -2f;
        }
        else if (_lateralPosition < -0.8f)
        {
            return 2f;
        }
        
        return 0f;
    }
    /// <summary>
    /// Simulate Error, to make the both be more "human"
    /// </summary>
    /// <returns></returns>
    float SimulateErrors()
    {
        if (Time.time > _nextErrorTime)
        {
            _currentError = Random.Range(-1f, 1f) * (1f - accuracy); 
            _nextErrorTime = Time.time + Random.Range(1f, 3f);
        }
        
        return _currentError * 0.3f; 
    }

    // -----------------------------------------------------------------------
    //                         SPLINE MOVEMENT EXECUTION
    // -----------------------------------------------------------------------

    void AdvanceInSpline()
    {
        float splineLength = splineContainer.Spline.GetLength();
        
        float targetSpeed = baseSpeed * _personalityFactor;
        
        _currentSpeed = Mathf.Lerp(
            _currentSpeed, 
            targetSpeed + Random.Range(-speedVariation, speedVariation),
            Time.fixedDeltaTime
        );
        _currentSpeed = Mathf.Clamp(_currentSpeed, minSpeed, maxSpeed);
        
        float progressIncrement = (_currentSpeed * Time.fixedDeltaTime) / splineLength;
        _splineProgress += progressIncrement;
        _splineProgress = Mathf.Clamp01(_splineProgress);
    }

    void UpdatePositionAndRotation()
    {
        Vector3 targetPosition = CalculateSplinePosition();
        
        rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * positionSmoothness));
        
        if (rb.rotation.x == 0 && rb.rotation.y == 0 && rb.rotation.z == 0)
        {
            AdjustRotation();
        }
    }
    void AdjustRotation()
    {
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        
        Quaternion targetRotation = Quaternion.LookRotation(tangent, up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));
    }
    Vector3 CalculateSplinePosition()
    {
        float3 splinePos = splineContainer.EvaluatePosition(_splineProgress);
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
        float3 lateralOffset = right * _lateralPosition * (trackWidth / 2f);
        // Position in the track without height adjustment
        Vector3 trackPosition = (Vector3)splinePos + (Vector3)lateralOffset;
        
        RaycastHit hit;
        // Raycast start point
        Vector3 rayStart = trackPosition + transform.up;
        // Raycast Lenght
        float maxDist = raycastMaxDistance * 2f;
        Vector3 rayDirection = transform.up * -1 + transform.position;
        
        // Launch raycast at needed direction(Down)
        if (Physics.Raycast(rayStart, rayDirection, out hit, maxDist, groundLayer))
        {
            // Actual Character position it's the y position
            
            // New Height for the character.
            float targetY = hit.point.y + desiredGroundDistance;
            
            // We use a lerp to make it the movement smooth
            float smoothedY = Mathf.Lerp(
                transform.position.y, // Actual height
                targetY,              // New Height
                Time.fixedDeltaTime * positionSmoothness * 2f // Smooth Factor
            );
            
            // Set the new Height
            trackPosition.y = smoothedY;

            // look at TANGENT, with UP alaingned to HIT.NORMAL.
            Quaternion targetRotation = Quaternion.LookRotation((Vector3)tangent, hit.normal);
            
            // Apply smooth rotation
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));
        }
        else
        {
            AdjustRotation();
            trackPosition.y -= Time.fixedDeltaTime * 10f;
        }
       
        return trackPosition;
    }
    
    float FindNearestProgress(Vector3 position)
    {
        float bestDistance = float.MaxValue;
        float bestProgress = 0f;
        
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
    //                         RACE MANAGER CALLBACKS / HIT LOGIC
    // -----------------------------------------------------------------------
    
    public void OnRacePrepare()
    {
        _canMove = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
    
    public void OnRaceStart()
    {
        ResetBot();
        _canMove = true;
    }

    public void OnRaceStop()
    {
        _canMove = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    public void HitObstacle(float forwardSlowdownFactor = 0.5f)
    {
        // Previene el choque si está aturdido O es invulnerable
        if (_isStunned || _isInvulnerable) return; 

        _preStunBaseSpeed = baseSpeed; 
        
        // Activar inmediatamente la invulnerabilidad para prevenir choques anidados
        _isInvulnerable = true; 
        
        // Slowdown: Reduce la base speed del bot
        baseSpeed *= forwardSlowdownFactor;
        baseSpeed = Mathf.Max(minSpeed, baseSpeed); 

        // Rebote
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        Vector3 bounceDirection = -tangent; 
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.AddForce(bounceDirection.normalized * bounceForce, ForceMode.Impulse);
        }
        
        // Stun y Cooldown
        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine()
    {
        _isStunned = true;
        
        // Invulnerabilidad dura stunTime + 0.5s extra de seguridad
        StartCoroutine(InvulnerabilityRoutine(stunTime + 0.5f)); 
        
        yield return new WaitForSeconds(stunTime);
        
        _isStunned = false;
        
        StartCoroutine(RestoreSpeedRoutine());
    }

    IEnumerator RestoreSpeedRoutine()
    {
        float recoveryDuration = 0.5f; 
        float startTime = Time.time;
        float startSpeed = baseSpeed;
        float targetSpeed = _preStunBaseSpeed;
        
        while (Time.time < startTime + recoveryDuration)
        {
            float t = (Time.time - startTime) / recoveryDuration;
            
            baseSpeed = Mathf.Lerp(startSpeed, targetSpeed, t);
            
            yield return null;
        }
        
        baseSpeed = targetSpeed;
    }

    IEnumerator InvulnerabilityRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        _isInvulnerable = false; 
    }

    public void ResetBot()
    {
        _splineProgress = 0f;
        
        _lateralPosition = Random.Range(-0.3f, 0.3f);
        _lateralBias = Random.Range(-1.0f, 1.0f); 
        
        _currentSpeed = Random.Range(minSpeed, maxSpeed);
        
        InitializeSplinePosition(); 
    }

    // -----------------------------------------------------------------------
    //                                GIZMOS
    // -----------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (splineContainer == null || !Application.isPlaying) return;
        
        // Draw a sphere thats the range of obstacle detection
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sphereCastRadius);

        // Draw future point based on prediction distance
        float futureProgress = Mathf.Clamp01(_splineProgress + (distancePrediction / splineContainer.Spline.GetLength()));
        Vector3 futurePoint = splineContainer.EvaluatePosition(futureProgress);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, futurePoint);
    }
}