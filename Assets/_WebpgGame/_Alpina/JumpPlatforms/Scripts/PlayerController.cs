using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Salto Direccional")]
    public float jumpForce = 15f;
    public float lateralJumpForce = 8f; // Fuerza lateral del salto
    public float jumpCooldown = 0.3f;
    
    [Header("Movimiento")]
    public float moveSpeed = 10f;
    public float maxSpeed = 8f;
    public float moveRange = 5f;
    public float airControl = 0.5f;
    
    [Header("Estabilidad")]
    public float stabilityForce = 50f;
    public float maxTiltAngle = 15f;
    
    [Header("Detección de Suelo")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.3f;
    
    [Header("Referencias")]
    public Animator animator;
    public Transform model3D;
    
    [Header("Touch Controls")]
    public TouchJoystick movementJoystick; // Para movimiento Y salto direccional
    public TouchButton jumpButton;         // Para salto hacia adelante
    
    [Header("Animaciones")]
    public float animationSmoothTime = 0.1f;
    public float movementThreshold = 0.1f;
    public bool showDebug = false;
    
    // Components
    private Rigidbody rb;
    
    // States
    private float lastJumpTime = 0f;
    private bool isGrounded = false;
    private bool isAlive = true;
    
    // Input and Animation
    private float horizontalInput = 0f;
    private float currentAnimVelocity = 0f;
    private float velocityDampening;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        rb.freezeRotation = false;
        rb.angularDamping = 10f;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        if (animator == null)
            animator = GetComponent<Animator>();
            
        Debug.Log("🎮 Player Controller initialized!");
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        GetInput();
        HandleJumpInput();
        UpdateAnimations();
    }
    
    void GetInput()
    {
        horizontalInput = 0f;
        
        // 1. Teclado
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontalInput = 1f;
        
        // 2. Movement Joystick
        if (movementJoystick != null && movementJoystick.IsActive())
        {
            float touchInput = movementJoystick.GetHorizontalInput();
            if (Mathf.Abs(touchInput) > 0.1f)
            {
                horizontalInput = touchInput;
            }
        }
    }
    
    void HandleJumpInput()
    {
        bool jumpPressed = false;
        
        // 1. Teclado - Spacebar
        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;
        
        // 2. Touch Jump Button  
        if (jumpButton != null && jumpButton.IsPressed())
            jumpPressed = true;
        
        // 3. Ejecutar salto direccional
        if (jumpPressed && CanJump())
        {
            JumpWithDirection();
        }
    }
    
    bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime) >= jumpCooldown;
    }
    
    void JumpWithDirection()
    {
        // SALTO DIRECCIONAL
        Vector3 jumpDirection = Vector3.up * jumpForce;
        
        // Agregar componente lateral según input
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            jumpDirection += Vector3.right * horizontalInput * lateralJumpForce;
        }
        
        // Reset velocidad Y y aplicar salto
        Vector3 currentVel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVel.x * 0.5f, 0f, currentVel.z);
        rb.AddForce(jumpDirection, ForceMode.Impulse);
        
        lastJumpTime = Time.time;
        
        // Rotar hacia dirección de salto
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            RotateCharacter(horizontalInput);
        }
        
        if (showDebug)
        {
            Debug.Log($"🚀 Directional Jump! Direction: {jumpDirection} | Input: {horizontalInput:F2}");
        }
        
        // Animation trigger
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Velocity para Idle/Movement
        float targetVelocity = Mathf.Abs(horizontalInput) > movementThreshold ? 1f : 0f;
        
        currentAnimVelocity = Mathf.SmoothDamp(
            currentAnimVelocity, 
            targetVelocity, 
            ref velocityDampening, 
            animationSmoothTime
        );
        
        animator.SetFloat("Velocity", currentAnimVelocity);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        
        if (showDebug)
        {
            Debug.Log($"🎭 Anim: {currentAnimVelocity:F2} | Input: {horizontalInput:F2} | Grounded: {isGrounded}");
        }
    }
    
    void FixedUpdate()
    {
        if (!isAlive) return;
        
        CheckGrounded();
        HandleAirMovement();
        ApplyStabilityForce();
        CheckBounds();
    }
    
    void HandleAirMovement()
    {
        // Control menor en el aire
        if (!isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            Vector3 airForce = Vector3.right * horizontalInput * moveSpeed * airControl;
            rb.AddForce(airForce, ForceMode.Force);
            
            // Limitar velocidad lateral
            Vector3 velocity = rb.linearVelocity;
            velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
            rb.linearVelocity = velocity;
        }
    }
    
    void RotateCharacter(float direction)
    {
        if (model3D != null && Mathf.Abs(direction) > 0.1f)
        {
            float targetRotation = direction > 0 ? 90f : -90f;
            Quaternion targetRot = Quaternion.Euler(0, targetRotation, 0);
            model3D.rotation = Quaternion.Lerp(model3D.rotation, targetRot, Time.fixedDeltaTime * 10f);
        }
    }
    
    void ApplyStabilityForce()
    {
        Vector3 currentRotation = transform.eulerAngles;
        float xAngle = currentRotation.x > 180 ? currentRotation.x - 360 : currentRotation.x;
        float zAngle = currentRotation.z > 180 ? currentRotation.z - 360 : currentRotation.z;
        
        if (Mathf.Abs(xAngle) > maxTiltAngle || Mathf.Abs(zAngle) > maxTiltAngle)
        {
            Vector3 correctionTorque = Vector3.zero;
            
            if (Mathf.Abs(xAngle) > maxTiltAngle)
                correctionTorque.x = -xAngle * stabilityForce;
                
            if (Mathf.Abs(zAngle) > maxTiltAngle)
                correctionTorque.z = -zAngle * stabilityForce;
            
            rb.AddTorque(correctionTorque, ForceMode.Force);
        }
        
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
    }
    
    void CheckGrounded()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        RaycastHit hit;
        
        bool centerGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance, groundLayer);
        bool leftGrounded = Physics.Raycast(rayOrigin + Vector3.left * 0.3f, Vector3.down, groundCheckDistance, groundLayer);
        bool rightGrounded = Physics.Raycast(rayOrigin + Vector3.right * 0.3f, Vector3.down, groundCheckDistance, groundLayer);
        
        isGrounded = centerGrounded || leftGrounded || rightGrounded;
        
        // Debug visual
        Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        Debug.DrawRay(rayOrigin + Vector3.left * 0.3f, Vector3.down * groundCheckDistance, leftGrounded ? Color.green : Color.red);
        Debug.DrawRay(rayOrigin + Vector3.right * 0.3f, Vector3.down * groundCheckDistance, rightGrounded ? Color.green : Color.red);
    }
    
    void CheckBounds()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -moveRange, moveRange);
        transform.position = pos;
        
        if (pos.y < -20f)
        {
            Die();
        }
    }
    
    public void Die()
    {
        if (!isAlive) return;
        
        isAlive = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        currentAnimVelocity = 0f;
        if (animator != null)
            animator.SetFloat("Velocity", 0f);
        
        Debug.Log("💀 Player died!");
    }
    
    public void Restart()
    {
        isAlive = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
        
        lastJumpTime = 0f;
        currentAnimVelocity = 0f;
        horizontalInput = 0f;
        
        if (animator != null)
            animator.SetFloat("Velocity", 0f);
        
        Debug.Log(" Player restarted!");
    }
    
    // Propiedades públicas
    public bool IsAlive => isAlive;
    public bool IsGroundedPublic => isGrounded;
    public float CurrentAnimationVelocity => currentAnimVelocity;
    public bool IsMoving => Mathf.Abs(horizontalInput) > movementThreshold;
    public Vector3 CurrentVelocity => rb.linearVelocity;
    
    // Debug
    void OnGUI()
    {
        if (!showDebug || !Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("🎮 Player Controller Debug");
        GUILayout.Label($"Input: {horizontalInput:F2}");
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"Velocity: {rb.linearVelocity}");
        GUILayout.Label($"Can Jump: {CanJump()}");
        GUILayout.Label($"Animation: {currentAnimVelocity:F2}");
        GUILayout.EndArea();
    }
}