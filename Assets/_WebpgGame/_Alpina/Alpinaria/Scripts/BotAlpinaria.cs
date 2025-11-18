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
    [SerializeField] private float agility = 5f; // Nunca se utiliza
    [SerializeField] private float accuracy = 0.8f;
    [SerializeField] private float distancePrediction = 5f;
    [SerializeField] private DifficultyType difficulty = DifficultyType.Normal; 
    
    [Header("Behavior Toggles")]
    [SerializeField] private bool edgeAvoidance = true;
    [SerializeField] private bool findOptimalLine = true;
    [SerializeField] private bool makeMistakes = true;
    
    [Header("Obstacle Interaction")]
    [SerializeField] private float stunTime = 0.5f; 
    [SerializeField] private float bounceForce = 5f;
    
    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothness = 10f;
    [SerializeField] private float positionSmoothness = 8f;
    
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
    
    private bool _canMove = false;
    private bool _isStunned = false;

    // -----------------------------------------------------------------------
    //                            UNITY LIFECYCLE
    // -----------------------------------------------------------------------

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        // Safety check
        if (splineContainer == null || rb == null)
        {
            Debug.LogError($"Bot {gameObject.name} is missing a critical reference (SplineContainer or Rigidbody). Disabling script.");
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
        // Set a persistent random speed factor for this specific bot
        _personalityFactor = Random.Range(0.7f, 1.3f);
        
        ConfigureDifficulty();
        
        // Apply personality factor to base speed
        _currentSpeed = Random.Range(minSpeed, maxSpeed);
        
        InitializeSplinePosition();
    }

    void InitializeSplinePosition()
    {
        // Find nearest progress point on spline
        _splineProgress = FindNearestProgress(transform.position);
        
        // Calculate lateral position based on where the bot started (important for race start placement)
        _lateralPosition = CalculateInitialLateralPosition();
        
        // Set initial position immediately (Fixes being under the slide)
        transform.position = CalculateSplinePosition();
        
        // Set initial rotation
        UpdatePositionAndRotation(); 
    }

    float CalculateInitialLateralPosition()
    {
        float3 splinePos = splineContainer.EvaluatePosition(_splineProgress);
        float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 up = splineContainer.EvaluateUpVector(_splineProgress);
        
        // Calculate the 'right' vector (perpendicular to tangent and up)
        float3 right = math.normalize(math.cross(tangent, up));
        
        // Project the position onto the right vector to find lateral distance
        Vector3 offsetFromSpline = transform.position - (Vector3)splinePos;
        float lateralDistance = Vector3.Dot(offsetFromSpline, right);
        
        // Normalize the distance to the -1 to 1 range
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
    
    void MakeDecisions() // Decitions -> MakeDecisions
    {
        float desiredMovement = 0f;
        
        if (findOptimalLine)
        {
            desiredMovement += CalculateOptimalLineMovement();
        }
        
        if (edgeAvoidance)
        {
            desiredMovement += AvoidEdgeMovement();
        }
        
        if (makeMistakes)
        {
            desiredMovement += SimulateErrors();
        }
        
        desiredMovement += _lateralBias * 0.5f;
        // Apply accuracy factor to the combined desired movement
        float finalMovement = desiredMovement * accuracy;
        
        _lateralPosition += finalMovement * lateralSpeed * Time.fixedDeltaTime;
        _lateralPosition = Mathf.Clamp(_lateralPosition, -1f, 1f);
    }

    float CalculateOptimalLineMovement()
    {
        float futureProgress = _splineProgress + (distancePrediction / splineContainer.Spline.GetLength());
        futureProgress = Mathf.Clamp01(futureProgress);
        
        float3 currentTangent = splineContainer.EvaluateTangent(_splineProgress);
        float3 futureTangent = splineContainer.EvaluateTangent(futureProgress);
        
        // Cross product indicates the direction of turn (and intensity)
        Vector3 cross = Vector3.Cross(currentTangent, futureTangent);
        float curveIntensity = cross.magnitude;
        
        if (curveIntensity > 0.1f)
        {
            bool curveToTheRight = cross.y > 0;
            
            // Try to move to the inside of the curve
            if (curveToTheRight)
            {
                // If on the right side, move left (-1) if not already inside/center
                return _lateralPosition > -0.5f ? -1f : 0f;
            }
            else // Curve to the left
            {
                // If on the left side, move right (+1) if not already inside/center
                return _lateralPosition < 0.5f ? 1f : 0f;
            }
        }
        
        // If track is straight, return to center
        if (Mathf.Abs(_lateralPosition) > 0.2f)
        {
            return -Mathf.Sign(_lateralPosition) * 0.5f; // Move towards center
        }
        
        return 0f;
    }

    float AvoidEdgeMovement()
    {
        if (_lateralPosition > 0.8f) // Too far right
        {
            return -2f; // Strong push to the left
        }
        else if (_lateralPosition < -0.8f) // Too far left
        {
            return 2f; // Strong push to the right
        }
        
        return 0f;
    }
    
    float SimulateErrors()
    {
        if (Time.time > _nextErrorTime)
        {
            // Error magnitude is inverse to accuracy
            _currentError = Random.Range(-1f, 1f) * (1f - accuracy); 
            _nextErrorTime = Time.time + Random.Range(1f, 3f);
        }
        
        // Apply only a fraction of the error to the lateral movement
        return _currentError * 0.3f; 
    }

    // -----------------------------------------------------------------------
    //                         SPLINE MOVEMENT EXECUTION
    // -----------------------------------------------------------------------

    void AdvanceInSpline()
    {
        float splineLength = splineContainer.Spline.GetLength();
        
        // Introduce small, constant speed variation to make the bot less predictable
        _currentSpeed = Mathf.Lerp(
            _currentSpeed, 
            baseSpeed + Random.Range(-speedVariation, speedVariation),
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
        
        // Calculate and apply rotation
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
        
        // Calculate the cross product and perform Anti-NaN check
        float3 crossProduct = math.cross(tangent, up);
        float3 right;
        
        if (math.lengthsq(crossProduct) < 0.0001f)
        {
            right = new float3(1f, 0f, 0f); 
        }
        else
        {
            right = math.normalize(crossProduct);
        }

        // Calculate offsets
        float3 lateralOffset = right * _lateralPosition * (trackWidth / 2f);
        float3 heightOffsetVector = up * heightOffset;
        
        return splinePos + lateralOffset + heightOffsetVector;
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
    //                         RACE MANAGER CALLBACKS / INTERACTION
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

    public void ResetBot()
    {
        _splineProgress = 0f;
        
        _lateralPosition = Random.Range(-0.3f, 0.3f);
        _lateralBias = Random.Range(-1.0f, 1.0f); // Un valor entre -1 y 1
        
        _currentSpeed = Random.Range(minSpeed, maxSpeed);
        
        InitializeSplinePosition();
    }
    /// <summary>
    /// Implements the obstacle hit logic: slowdown, bounce, and stun.
    /// </summary>
    /// <param name="forwardSlowdownFactor">Factor to reduce the base speed by.</param>
    public void HitObstacle(float forwardSlowdownFactor = 0.5f)
    {
        // if (_isStunned) return;
        //
        // // Slowdown: Reduce the base speed of the bot
        // baseSpeed *= forwardSlowdownFactor;
        // baseSpeed = Mathf.Max(minSpeed, baseSpeed); // Ensure speed doesn't drop below minSpeed
        //
        // //Stop and apply impulse opposite to the direction of travel
        // float3 tangent = splineContainer.EvaluateTangent(_splineProgress);
        // Vector3 bounceDirection = -tangent; 
        //
        // if (rb != null)
        // {
        //     rb.linearVelocity = Vector3.zero; 
        //     rb.AddForce(bounceDirection.normalized * bounceForce, ForceMode.Impulse);
        // }
        //
        // // 3. Stun: Block decisions and movement execution for a short time
        // StartCoroutine(StunRoutine());
    }

    /// <summary>
    /// Coroutine to handle the stun duration.
    /// </summary>
    IEnumerator StunRoutine()
    {
        _isStunned = true;
        yield return new WaitForSeconds(stunTime);
        _isStunned = false;
    }

    // -----------------------------------------------------------------------
    //                           PUBLIC ACCESSORS
    // -----------------------------------------------------------------------

    public float GetProgress()
    {
        return _splineProgress;
    }

    public void AdjustSpeed(float multiplier)
    {
        baseSpeed *= multiplier;
    }

    public bool HasFinished() // End -> HasFinished
    {
        return _splineProgress >= 0.999f;
    }

    // -----------------------------------------------------------------------
    //                                GIZMOS
    // -----------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (splineContainer == null || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        float futureProgress = Mathf.Clamp01(_splineProgress + (distancePrediction / splineContainer.Spline.GetLength()));
        Vector3 futurePoint = splineContainer.EvaluatePosition(futureProgress);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, futurePoint);
    }
}